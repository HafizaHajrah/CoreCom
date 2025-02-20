using CoreBooks.DataAccesses.Repository.IRepository;
using CoreBooks.Models;
using CoreBooks.Models.ViewModels;
using CoreBooks.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stripe;
using Stripe.Checkout;
using System.Diagnostics;
using System.Security.Claims;

namespace CoreBooks.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    public class OrderController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        [BindProperty]
        public OrderMV orderMV { get; set; }
        public OrderController(IUnitOfWork unitOfWork)
        {
            _unitOfWork= unitOfWork;
        }

        public IActionResult Index()
        {
            return View();
        }
        public IActionResult Details(int orderId)
        {
            orderMV = new()
            {
                OrderHeaders=_unitOfWork.OrderHeader.Get(u=>u.Id==orderId,includeproperties:"ApplicationUser"),
                OrderDetailList=_unitOfWork.OrderDetail.GetAll(u=>u.OrderHeaderId==orderId,includeproperties:"Product")
            };
            return View(orderMV);
        }
          
       
        [HttpPost]
        [Authorize(Roles =SD.Role_Admin+"," + SD.Role_Employee)]
        public IActionResult UpdateOrderDetails()
        {
            var orderDetailsFromDb = _unitOfWork.OrderHeader.Get(u => u.Id == orderMV.OrderHeaders.Id);
            orderDetailsFromDb.Name = orderMV.OrderHeaders.Name;
            orderDetailsFromDb.PhoneNumber = orderMV.OrderHeaders.PhoneNumber;
            orderDetailsFromDb.StreetAddress = orderMV.OrderHeaders.StreetAddress;
            orderDetailsFromDb.City = orderMV.OrderHeaders.City;
            orderDetailsFromDb.State = orderMV.OrderHeaders.State;
            orderDetailsFromDb.PostalCode = orderMV.OrderHeaders.PostalCode;
            if (!string.IsNullOrEmpty(orderMV.OrderHeaders.Carrier))
            {
                orderDetailsFromDb.Carrier = orderMV.OrderHeaders.Carrier;
            }
            if (!string.IsNullOrEmpty(orderMV.OrderHeaders.TrackingNumber))
            {
                orderDetailsFromDb.TrackingNumber = orderMV.OrderHeaders.TrackingNumber;
            }
            _unitOfWork.OrderHeader.Update(orderDetailsFromDb);
            _unitOfWork.Save();

            TempData["success"] = "Order Details Updated Successfully";

            return RedirectToAction(nameof(Details),new {orderId=orderDetailsFromDb.Id});
        }

        [HttpPost]
        [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
        public IActionResult StartProcessing()
        {
            _unitOfWork.OrderHeader.UpdateOrderStatus(orderMV.OrderHeaders.Id, SD.StatusInProcess);
            _unitOfWork.Save();
            TempData["success"] = "Order Processing Started";

            return RedirectToAction(nameof(Details), new { orderId = orderMV.OrderHeaders.Id });
        }

        [HttpPost]
        [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
        public IActionResult StartShipping()
        {
            var orderdetailsfromDb = _unitOfWork.OrderHeader.Get(u=>u.Id==orderMV.OrderHeaders.Id);
            orderdetailsfromDb.TrackingNumber = orderMV.OrderHeaders.TrackingNumber;
            orderdetailsfromDb.Carrier = orderMV.OrderHeaders.Carrier;
            orderdetailsfromDb.OrderStatus = SD.StatusShipped;
            orderdetailsfromDb.ShippingDate = DateTime.Now;
            if (orderdetailsfromDb.PaymentStatus == SD.PaymentStatusDelayedPayment)
            {
                orderdetailsfromDb.PaymentDueDate = DateOnly.FromDateTime(DateTime.Now.AddDays(30));
            }

            _unitOfWork.OrderHeader.Update(orderdetailsfromDb);
            _unitOfWork.Save();
            TempData["success"] = "Order Shipped Successfully";

            return RedirectToAction(nameof(Details), new { orderId = orderMV.OrderHeaders.Id });
        }

        [HttpPost]
        [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
        public IActionResult CancelOrder()
        {
            var orderdetails = _unitOfWork.OrderHeader.Get(u => u.Id == orderMV.OrderHeaders.Id);
            if (orderdetails.PaymentStatus == SD.PaymentStatusApproved)
            {
                var options = new RefundCreateOptions
                {
                    Reason = RefundReasons.RequestedByCustomer,
                    PaymentIntent = orderdetails.PaymentIntendedId
                };

                var service = new RefundService();
                Refund refund = service.Create(options);

                _unitOfWork.OrderHeader.UpdateOrderStatus(orderdetails.Id, SD.StatusCancelled, SD.StatusRefunded);
            }
            else
            {
                _unitOfWork.OrderHeader.UpdateOrderStatus(orderdetails.Id, SD.StatusCancelled, SD.StatusCancelled);
            }
            _unitOfWork.Save();
            TempData["success"] = "Order Cancelled and Refunded Successfully";

            return RedirectToAction(nameof(Details), new { orderId = orderMV.OrderHeaders.Id });
        }

        [ActionName("Details")]
        [HttpPost]
        [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
        public IActionResult Details_Pay_Now()
        {

            orderMV.OrderHeaders=_unitOfWork.OrderHeader
                .Get(u=>u.Id==orderMV.OrderHeaders.Id,includeproperties:"ApplicationUser");
            orderMV.OrderDetailList = _unitOfWork.OrderDetail
                .GetAll(u => u.OrderHeaderId == orderMV.OrderHeaders.Id, includeproperties: "Product");
            var domain = "https://localhost:7275/";
            var options = new Stripe.Checkout.SessionCreateOptions
            {

                SuccessUrl = domain + $"Admin/Order/PaymentConfirmation?orderHeaderId={orderMV.OrderHeaders.Id}",
                CancelUrl = domain + $"Admin/Order/Details?orderId={orderMV.OrderHeaders.Id}",
                LineItems = new List<SessionLineItemOptions>(),
                Mode = "payment",
            };
            foreach (var item in orderMV.OrderDetailList)
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
            Session session = service.Create(options);
            _unitOfWork.OrderHeader.UpdateStripePaymentId(orderMV.OrderHeaders.Id, session.Id, session.PaymentIntentId);
            _unitOfWork.Save();
            Response.Headers.Add("Location", session.Url);
            return new StatusCodeResult(303);
        }


        public IActionResult PaymentConfirmation(int orderHeaderId)
        {
            OrderHeader orderheader = _unitOfWork.OrderHeader.Get(u => u.Id == orderHeaderId);
            if (orderheader.PaymentStatus == SD.PaymentStatusDelayedPayment)
            {
              
                var service = new SessionService();
                Session session = service.Get(orderheader.SessionId);
                if (session.PaymentStatus.ToLower() == "paid")
                {
                    _unitOfWork.OrderHeader.UpdateStripePaymentId(orderHeaderId, session.Id, session.PaymentIntentId);
                    _unitOfWork.OrderHeader.UpdateOrderStatus(orderHeaderId, orderheader.OrderStatus, SD.PaymentStatusApproved);
                    _unitOfWork.Save();
                }
            }
 
            return View(orderHeaderId);
        }

        #region API Calls
        public IActionResult GetAll(string status)
        {
            IEnumerable<OrderHeader> orderHeaders; 
           if(User.IsInRole(SD.Role_Admin) || User.IsInRole(SD.Role_Employee))
            {
                orderHeaders = _unitOfWork.OrderHeader.GetAll(includeproperties: "ApplicationUser").ToList();
            }
            else
            {
                var claimsIdentity = (ClaimsIdentity)User.Identity;
                var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;
                orderHeaders = _unitOfWork.OrderHeader.GetAll(u => u.ApplicationUserId == userId, includeproperties: "ApplicationUser").ToList();

            }

            switch (status)
            {
                case "pending":
                  orderHeaders = orderHeaders.Where(u => u.PaymentStatus == SD.PaymentStatusDelayedPayment);
                    break;
                case "inprocess":
                    orderHeaders = orderHeaders.Where(s => s.OrderStatus == SD.StatusInProcess);
                    break;
                case "completed":
                    orderHeaders = orderHeaders.Where(s => s.OrderStatus == SD.StatusShipped);
                    break;
                case "approved":
                    orderHeaders = orderHeaders.Where(s => s.OrderStatus == SD.StatusApproved); 
                    break;
                default:
                    break;
            }

            return Json(new {data= orderHeaders });
        }
        #endregion
    }
}

