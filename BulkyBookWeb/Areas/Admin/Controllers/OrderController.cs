using BulkyBook.DataAccess.Repository.Interface;
using BulkyBook.Models;
using BulkyBook.Models.ViewModels;
using BulkyBook.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stripe;
using Stripe.Checkout;
using System.Security.Claims;

namespace BulkyBookWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    public class OrderController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        public OrderController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Details(int orderId)
        {
            var orderViewModel = new OrderViewModel
            {
                OrderHeader =
                    _unitOfWork.OrderHeaderRepository.GetFirstOrDefault(c => c.Id == orderId,
                        includeProperties: "ApplicationUser"),
                OrderDetails =
                    _unitOfWork.OrderDetailRepository.GetAll(c => c.OrderHeaderId == orderId,
                        includeProperties: "Product")
            };

            return View(orderViewModel);
        }

        [HttpPost]
        [ActionName("Details")]
        [ValidateAntiForgeryToken]
        public IActionResult DetailsPayNow(OrderViewModel orderViewModel)
        {
            orderViewModel.OrderHeader =
                _unitOfWork.OrderHeaderRepository.GetFirstOrDefault(c => c.Id == orderViewModel.OrderHeader.Id,
                    includeProperties: "ApplicationUser");

            orderViewModel.OrderDetails =
                _unitOfWork.OrderDetailRepository.GetAll(c => c.OrderHeaderId == orderViewModel.OrderHeader.Id,
                    includeProperties: "Product");

            const string domain = "https://localhost:44333";

            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string>
                {
                    "card"
                },
                LineItems = new List<SessionLineItemOptions>(),
                Mode = "payment",
                SuccessUrl = domain + $"/admin/Order/PaymentConfirmation?orderHeaderId={orderViewModel.OrderHeader.Id}",
                CancelUrl = domain + $"/admin/Order/Details?orderId={orderViewModel.OrderHeader.Id}"
            };

            foreach (var item in orderViewModel.OrderDetails)
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

            _unitOfWork.OrderHeaderRepository.UpdateStripePaymentId(orderViewModel.OrderHeader.Id,
                session.Id, session.PaymentIntentId);
            _unitOfWork.Save();

            Response.Headers.Add("Location", session.Url);
            return new StatusCodeResult(303);
        }

        public IActionResult PaymentConfirmation(int orderHeaderId)
        {
            var orderHeader = _unitOfWork.OrderHeaderRepository.GetFirstOrDefault(c => c.Id == orderHeaderId);

            if (orderHeader.PaymentStatus == SD.PaymentStatusDelayedPayment)
            {
                var service = new SessionService();
                var session = service.Get(orderHeader.SessionId);

                if (session.PaymentStatus.ToLower() == "paid")
                {
                    _unitOfWork.OrderHeaderRepository.UpdateStatus(orderHeaderId, orderHeader.OrderStatus, SD.PaymentStatusApproved);
                    _unitOfWork.Save();
                }
            }

            return View(orderHeaderId);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
        public IActionResult UpdateOrderDetail(OrderViewModel orderViewModel)
        {
            var orderHeaderFromDb =
                _unitOfWork.OrderHeaderRepository.GetFirstOrDefault(c => c.Id == orderViewModel.OrderHeader.Id, tracked: false);

            orderHeaderFromDb.Name = orderViewModel.OrderHeader.Name;
            orderHeaderFromDb.PhoneNumber = orderViewModel.OrderHeader.PhoneNumber;
            orderHeaderFromDb.StreetAddress = orderViewModel.OrderHeader.StreetAddress;
            orderHeaderFromDb.City = orderViewModel.OrderHeader.City;
            orderHeaderFromDb.State = orderViewModel.OrderHeader.State;
            orderHeaderFromDb.PostalCode = orderViewModel.OrderHeader.PostalCode;

            if (orderViewModel.OrderHeader.Carrier != null)
            {
                orderHeaderFromDb.Carrier = orderViewModel.OrderHeader.Carrier;
            }

            if (orderViewModel.OrderHeader.TrackingNumber != null)
            {
                orderHeaderFromDb.TrackingNumber = orderViewModel.OrderHeader.TrackingNumber;
            }

            _unitOfWork.OrderHeaderRepository.Update(orderHeaderFromDb);
            _unitOfWork.Save();

            TempData["Success"] = "Order Details Updated Successfully.";
            return RedirectToAction("Details", "Order", new { orderId = orderHeaderFromDb.Id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
        public IActionResult StartProcessing(OrderViewModel orderViewModel)
        {
            _unitOfWork.OrderHeaderRepository.UpdateStatus(orderViewModel.OrderHeader.Id, SD.StatusInProcess);
            _unitOfWork.Save();

            TempData["Success"] = "Order Status Updated Successfully.";
            return RedirectToAction("Details", "Order", new { orderId = orderViewModel.OrderHeader.Id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
        public IActionResult ShipOrder(OrderViewModel orderViewModel)
        {
            var orderHeaderFromDb =
                _unitOfWork.OrderHeaderRepository.GetFirstOrDefault(c => c.Id == orderViewModel.OrderHeader.Id, tracked: false);

            orderHeaderFromDb.TrackingNumber = orderViewModel.OrderHeader.TrackingNumber;
            orderHeaderFromDb.Carrier = orderViewModel.OrderHeader.Carrier;
            orderHeaderFromDb.OrderStatus = SD.StatusShipped;
            orderHeaderFromDb.ShippingDate = DateTime.Now;

            if (orderHeaderFromDb.PaymentStatus == SD.PaymentStatusDelayedPayment)
                orderHeaderFromDb.PaymentDueDate = DateTime.Now.AddDays(30);

            _unitOfWork.OrderHeaderRepository.Update(orderHeaderFromDb);
            _unitOfWork.Save();

            TempData["Success"] = "Order Shipped Successfully.";
            return RedirectToAction("Details", "Order", new { orderId = orderViewModel.OrderHeader.Id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
        public IActionResult CancelOrder(OrderViewModel orderViewModel)
        {
            var orderHeaderFromDb =
                _unitOfWork.OrderHeaderRepository.GetFirstOrDefault(c => c.Id == orderViewModel.OrderHeader.Id, tracked: false);

            if (orderHeaderFromDb.PaymentStatus == SD.PaymentStatusApproved)
            {
                var option = new RefundCreateOptions
                {
                    Reason = RefundReasons.RequestedByCustomer,
                    PaymentIntent = orderHeaderFromDb.PaymentIntentId
                };

                var service = new RefundService();
                var refund = service.Create(option);

                _unitOfWork.OrderHeaderRepository.UpdateStatus(orderHeaderFromDb.Id, SD.StatusCancelled, SD.StatusRefunded);
            }
            else
            {
                _unitOfWork.OrderHeaderRepository.UpdateStatus(orderHeaderFromDb.Id, SD.StatusCancelled, SD.StatusCancelled);
            }

            _unitOfWork.Save();

            TempData["Success"] = "Order Cancelled Successfully.";
            return RedirectToAction("Details", "Order", new { orderId = orderViewModel.OrderHeader.Id });
        }

        #region API Calls

        [HttpGet]
        public IActionResult GetAll(string status)
        {
            IEnumerable<OrderHeader> orderHeaders;

            if (User.IsInRole(SD.Role_Admin) || User.IsInRole(SD.Role_Employee))
            {
                orderHeaders = _unitOfWork.OrderHeaderRepository.GetAll(includeProperties: "ApplicationUser");
            }
            else
            {
                var claimsIdentity = (ClaimsIdentity)User.Identity;
                var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
                orderHeaders = _unitOfWork.OrderHeaderRepository.GetAll(c => c.ApplicationUserId == claim.Value,
                    includeProperties: "ApplicationUser");
            }

            switch (status)
            {
                case "pending":
                    orderHeaders = orderHeaders.Where(c => c.PaymentStatus == SD.PaymentStatusDelayedPayment);
                    break;
                case "inProcess":
                    orderHeaders = orderHeaders.Where(c => c.OrderStatus == SD.StatusInProcess);
                    break;
                case "completed":
                    orderHeaders = orderHeaders.Where(c => c.OrderStatus == SD.StatusShipped);
                    break;
                case "approved":
                    orderHeaders = orderHeaders.Where(c => c.OrderStatus == SD.StatusApproved);
                    break;
            }

            return Json(new { data = orderHeaders });
        }
        #endregion
    }
}
