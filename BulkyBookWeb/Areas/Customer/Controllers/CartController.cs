using BulkyBook.DataAccess.Repository.Interface;
using BulkyBook.Models;
using BulkyBook.Models.ViewModels;
using BulkyBook.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stripe.Checkout;
using System.Security.Claims;

namespace BulkyBookWeb.Areas.Customer.Controllers
{
    [Area("Customer")]
    [Authorize]
    public class CartController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        public CartController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public IActionResult Index()
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

            var shoppingCartViewModel = new ShoppingCartViewModel
            {
                ListCart = _unitOfWork.ShoppingCartRepository.GetAll(c => c.ApplicationUserId == claim.Value,
                    includeProperties: "Product"),
                OrderHeader = new OrderHeader()
            };

            foreach (var cart in shoppingCartViewModel.ListCart)
            {
                cart.Price = GetPriceBasedOnQuantity(cart.Count, cart.Product.Price, cart.Product.Price50,
                    cart.Product.Price100);

                shoppingCartViewModel.OrderHeader.OrderTotal += (cart.Price * cart.Count);
            }

            return View(shoppingCartViewModel);
        }

        public IActionResult Plus(int cartId)
        {
            var cart = _unitOfWork.ShoppingCartRepository.GetFirstOrDefault(c => c.Id == cartId);
            _unitOfWork.ShoppingCartRepository.IncrementCount(cart, 1);
            _unitOfWork.Save();

            return RedirectToAction("Index");
        }

        public IActionResult Minus(int cartId)
        {
            var cart = _unitOfWork.ShoppingCartRepository.GetFirstOrDefault(c => c.Id == cartId);

            if (cart.Count <= 1)
                _unitOfWork.ShoppingCartRepository.Remove(cart);
            else
                _unitOfWork.ShoppingCartRepository.DecrementCount(cart, 1);

            _unitOfWork.Save();

            return RedirectToAction("Index");
        }

        public IActionResult Remove(int cartId)
        {
            var cart = _unitOfWork.ShoppingCartRepository.GetFirstOrDefault(c => c.Id == cartId);
            _unitOfWork.ShoppingCartRepository.Remove(cart);
            _unitOfWork.Save();

            return RedirectToAction("Index");
        }

        public IActionResult Summary()
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

            var shoppingCartViewModel = new ShoppingCartViewModel
            {
                ListCart = _unitOfWork.ShoppingCartRepository.GetAll(c => c.ApplicationUserId == claim.Value,
                    includeProperties: "Product"),
                OrderHeader = new OrderHeader
                {
                    ApplicationUser = _unitOfWork.ApplicationUserRepository.GetFirstOrDefault(c => c.Id == claim.Value)
                }
            };

            shoppingCartViewModel.OrderHeader.Name = shoppingCartViewModel.OrderHeader.ApplicationUser.Name;
            shoppingCartViewModel.OrderHeader.PhoneNumber = shoppingCartViewModel.OrderHeader.ApplicationUser.PhoneNumber;
            shoppingCartViewModel.OrderHeader.StreetAddress = shoppingCartViewModel.OrderHeader.ApplicationUser.StreetAddress;
            shoppingCartViewModel.OrderHeader.City = shoppingCartViewModel.OrderHeader.ApplicationUser.City;
            shoppingCartViewModel.OrderHeader.State = shoppingCartViewModel.OrderHeader.ApplicationUser.State;
            shoppingCartViewModel.OrderHeader.PostalCode = shoppingCartViewModel.OrderHeader.ApplicationUser.PostalCode;

            foreach (var cart in shoppingCartViewModel.ListCart)
            {
                cart.Price = GetPriceBasedOnQuantity(cart.Count, cart.Product.Price, cart.Product.Price50,
                    cart.Product.Price100);

                shoppingCartViewModel.OrderHeader.OrderTotal += (cart.Price * cart.Count);
            }

            return View(shoppingCartViewModel);
        }

        [HttpPost]
        [ActionName("Summary")]
        [ValidateAntiForgeryToken]
        public IActionResult SummaryPost(ShoppingCartViewModel shoppingCartViewModel)
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

            shoppingCartViewModel.ListCart = _unitOfWork.ShoppingCartRepository.GetAll(
                c => c.ApplicationUserId == claim.Value,
                includeProperties: "Product");

            shoppingCartViewModel.OrderHeader.PaymentStatus = SD.PaymentStatusPending;
            shoppingCartViewModel.OrderHeader.OrderStatus = SD.StatusPending;
            shoppingCartViewModel.OrderHeader.OrderDate = DateTime.Now;
            shoppingCartViewModel.OrderHeader.ApplicationUserId = claim.Value;

            foreach (var cart in shoppingCartViewModel.ListCart)
            {
                cart.Price = GetPriceBasedOnQuantity(cart.Count, cart.Product.Price, cart.Product.Price50,
                    cart.Product.Price100);

                shoppingCartViewModel.OrderHeader.OrderTotal += (cart.Price * cart.Count);
            }

            _unitOfWork.OrderHeaderRepository.Add(shoppingCartViewModel.OrderHeader);
            _unitOfWork.Save();

            foreach (var cart in shoppingCartViewModel.ListCart)
            {
                var orderDetail = new OrderDetail
                {
                    ProductId = cart.ProductId,
                    OrderHeaderId = shoppingCartViewModel.OrderHeader.Id,
                    Price = cart.Price,
                    Count = cart.Count
                };
                _unitOfWork.OrderDetailRepository.Add(orderDetail);
                _unitOfWork.Save();
            }

            //stripe settings
            const string domain = "https://localhost:44333";

            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string>
                {
                    "card"
                },
                LineItems = new List<SessionLineItemOptions>(),
                Mode = "payment",
                SuccessUrl = domain + $"/Customer/Cart/OrderConfirmation?id={shoppingCartViewModel.OrderHeader.Id}",
                CancelUrl = domain + $"/Customer/Cart/Index"
            };

            foreach (var item in shoppingCartViewModel.ListCart)
            {
                var sessionLineItem = new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        UnitAmount = (long)(item.Price * 100),
                        Currency = "usd",
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = item.Product.Title
                        }
                    },
                    Quantity = item.Count
                };

                options.LineItems.Add(sessionLineItem);
            }

            var service = new SessionService();
            var session = service.Create(options);

            _unitOfWork.OrderHeaderRepository.UpdateStripePaymentId(shoppingCartViewModel.OrderHeader.Id, session.Id, session.PaymentIntentId);
            _unitOfWork.Save();

            Response.Headers.Add("Location", session.Url);
            return new StatusCodeResult(303);
        }

        public IActionResult OrderConfirmation(int id)
        {
            var orderHeader = _unitOfWork.OrderHeaderRepository.GetFirstOrDefault(c => c.Id == id);

            var service = new SessionService();
            var session = service.Get(orderHeader.SessionId);

            if (session.PaymentStatus.ToLower() == "paid")
            {
                _unitOfWork.OrderHeaderRepository.UpdateStatus(id, SD.StatusApproved, SD.PaymentStatusApproved);
                _unitOfWork.Save();
            }

            var shoppingCarts = _unitOfWork.ShoppingCartRepository
                .GetAll(c => c.ApplicationUserId == orderHeader.ApplicationUserId).ToList();

            _unitOfWork.ShoppingCartRepository.RemoveRange(shoppingCarts);
            _unitOfWork.Save();

            return View(id);
        }


        private double GetPriceBasedOnQuantity(double quantity, double price, double price50, double price100)
        {
            if (quantity <= 50)
                return price;

            return quantity <= 100 ? price50 : price100;
        }
    }
}
