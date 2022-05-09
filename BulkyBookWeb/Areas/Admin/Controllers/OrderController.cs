using BulkyBook.DataAccess.Repository.Interface;
using BulkyBook.Models;
using BulkyBook.Models.ViewModels;
using BulkyBook.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
        [ValidateAntiForgeryToken]
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
