using CoreBooks.DataAccesses.Repository.IRepository;
using CoreBooks.Models;
using CoreBooks.Models.ViewModels;
using CoreBooks.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Stripe.Checkout;
using System.Reflection.Metadata.Ecma335;
using System.Security.Claims;
using static System.Net.WebRequestMethods;

namespace CoreBooks.Areas.Customer.Controllers
{
    [Area("Customer")]
    [Authorize]
    public class CartController : Controller
    {

        private readonly IUnitOfWork _unitOfWork;
        [BindProperty]
        public ShoppingCartMV ShoppingCartMV { get; set; }
        public CartController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        public IActionResult Index()
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;
            ShoppingCartMV = new()
            {
                ShoppingCartList = _unitOfWork.ShoppingCart.GetAll(u => u.ApplicationUserId == userId, includeproperties: "Product"),
                orderHeader = new()

            };
            foreach (var cart in ShoppingCartMV.ShoppingCartList)
            {
                cart.Price = GetPriceBasedOnQuantity(cart);
                ShoppingCartMV.orderHeader.OrderTotal += (cart.Price * cart.Count);
            }
            return View(ShoppingCartMV);
        }
        public IActionResult Summary()
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;
            ShoppingCartMV = new()
            {
                ShoppingCartList = _unitOfWork.ShoppingCart.GetAll(u => u.ApplicationUserId == userId, includeproperties: "Product"),
                orderHeader = new()

            };

            ShoppingCartMV.orderHeader.ApplicationUser = _unitOfWork.ApplicationUser.Get(u => u.Id == userId);

            ShoppingCartMV.orderHeader.Name = ShoppingCartMV.orderHeader.ApplicationUser.Name;
            ShoppingCartMV.orderHeader.PhoneNumber = ShoppingCartMV.orderHeader.ApplicationUser.PhoneNumber;
            ShoppingCartMV.orderHeader.StreetAddress = ShoppingCartMV.orderHeader.ApplicationUser.StreetAddress;
            ShoppingCartMV.orderHeader.City = ShoppingCartMV.orderHeader.ApplicationUser.City;
            ShoppingCartMV.orderHeader.State = ShoppingCartMV.orderHeader.ApplicationUser.State;
            ShoppingCartMV.orderHeader.PostalCode = ShoppingCartMV.orderHeader.ApplicationUser.PostalCode;

            foreach (var cart in ShoppingCartMV.ShoppingCartList)
            {
                cart.Price = GetPriceBasedOnQuantity(cart);
                ShoppingCartMV.orderHeader.OrderTotal += (cart.Price * cart.Count);
            }
            return View(ShoppingCartMV);
        }
        [HttpPost]
        [ActionName("Summary")]
        public IActionResult SummaryPost()
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;
            ShoppingCartMV.ShoppingCartList = _unitOfWork.ShoppingCart.GetAll(u => u.ApplicationUserId == userId, includeproperties: "Product");

            ShoppingCartMV.orderHeader.OrderDate = DateTime.Now;
            ShoppingCartMV.orderHeader.ApplicationUserId = userId;

            ApplicationUser applicationUser = _unitOfWork.ApplicationUser.Get(u => u.Id == userId);

            foreach (var cart in ShoppingCartMV.ShoppingCartList)
            {
                cart.Price = GetPriceBasedOnQuantity(cart);
                ShoppingCartMV.orderHeader.OrderTotal += (cart.Price * cart.Count);
            }

            if (applicationUser.CompanyId.GetValueOrDefault() == 0)
            {
                //Its a regular User
                ShoppingCartMV.orderHeader.OrderStatus = SD.StatusPending;
                ShoppingCartMV.orderHeader.PaymentStatus = SD.StatusPending;
            }
            else
            {
                //its a company user
                ShoppingCartMV.orderHeader.OrderStatus = SD.StatusApproved;
                ShoppingCartMV.orderHeader.PaymentStatus = SD.PaymentStatusDelayedPayment;
            }
            _unitOfWork.OrderHeader.Add(ShoppingCartMV.orderHeader);
            _unitOfWork.Save();

            foreach (var cart in ShoppingCartMV.ShoppingCartList)
            {
                OrderDetail orderDetail = new()
                {
                    ProductId = cart.ProductId,
                    OrderHeaderId = ShoppingCartMV.orderHeader.Id,
                    Count = cart.Count,
                    Price = cart.Price,
                };
                _unitOfWork.OrderDetail.Add(orderDetail);
                _unitOfWork.Save();
            }

            if (applicationUser.CompanyId.GetValueOrDefault() == 0)
            {
                //regular person and need to capture payment
                //stripe logic
                var domain = Request.Scheme+ "://" + Request.Host.Value+"/";
                var options = new Stripe.Checkout.SessionCreateOptions
                {
                    
                    SuccessUrl = domain + $"Customer/Cart/OrderConfirmation?id={ShoppingCartMV.orderHeader.Id}",
                    CancelUrl= domain + $"Customer/Cart/Index",
                    LineItems = new List<SessionLineItemOptions>(),
                    Mode = "payment",
                };
                foreach (var item in ShoppingCartMV.ShoppingCartList)
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
                Session session= service.Create(options);
                _unitOfWork.OrderHeader.UpdateStripePaymentId(ShoppingCartMV.orderHeader.Id,session.Id,session.PaymentIntentId);
                _unitOfWork.Save();
                Response.Headers.Add("Location", session.Url);
                return new StatusCodeResult(303);
            }
            return RedirectToAction(nameof(OrderConfirmation), new { id = ShoppingCartMV.orderHeader.Id });
        }

        public IActionResult OrderConfirmation(int id)
        {
            OrderHeader orderheader = _unitOfWork.OrderHeader.Get(u => u.Id == id,includeproperties:"ApplicationUser");
            if (orderheader.PaymentStatus != SD.PaymentStatusDelayedPayment)
            {
                //its a regular customer
                var service = new SessionService();
                Session session = service.Get(orderheader.SessionId);
                if (session.PaymentStatus.ToLower() == "paid")
                {
                    _unitOfWork.OrderHeader.UpdateStripePaymentId(id,session.Id,session.PaymentIntentId);
                    _unitOfWork.OrderHeader.UpdateOrderStatus(id, SD.StatusApproved, SD.PaymentStatusApproved);
                    _unitOfWork.Save();
                }
                HttpContext.Session.Clear();
            }
            List<ShoppingCart> shoppingcart = _unitOfWork.ShoppingCart
                .GetAll(u=>u.ApplicationUserId==orderheader.ApplicationUserId).ToList();
            _unitOfWork.ShoppingCart.RemoveRange(shoppingcart);
            _unitOfWork.Save();
            return View(id);
        }

        public IActionResult Plus(int cartId)
        {
            var FromDbCart = _unitOfWork.ShoppingCart.Get(u => u.Id == cartId);
            FromDbCart.Count += 1;
            _unitOfWork.ShoppingCart.Update(FromDbCart);
            _unitOfWork.Save();
            return RedirectToAction(nameof(Index));
        }
        public IActionResult Minus(int cartId)
        {
            var FromDbCart = _unitOfWork.ShoppingCart.Get(u => u.Id == cartId, tracked:true);

            if (FromDbCart.Count <= 1)
            {
                HttpContext.Session.SetInt32(SD.SessionCart, _unitOfWork.ShoppingCart
                 .GetAll(u => u.ApplicationUserId == FromDbCart.ApplicationUserId).Count() - 1);
                _unitOfWork.ShoppingCart.Remove(FromDbCart);
            }
            else
            {
                FromDbCart.Count -= 1;
                _unitOfWork.ShoppingCart.Update(FromDbCart);
            }

            _unitOfWork.Save();
            return RedirectToAction(nameof(Index));
        }
        public IActionResult Remove(int cartId)
        {
            var FromDbCart = _unitOfWork.ShoppingCart.Get(u => u.Id == cartId, tracked: true);
            HttpContext.Session.SetInt32(SD.SessionCart, _unitOfWork.ShoppingCart
                  .GetAll(u => u.ApplicationUserId == FromDbCart.ApplicationUserId).Count()-1);
            _unitOfWork.ShoppingCart.Remove(FromDbCart);
            _unitOfWork.Save();
            return RedirectToAction(nameof(Index));
        }
        public double GetPriceBasedOnQuantity(ShoppingCart shoppingCart)
        {
            if (shoppingCart.Count <= 50)
            {
                return shoppingCart.Product.Price;
            }
            else
            {
                if (shoppingCart.Count <= 100)
                {
                    return shoppingCart.Product.Price50;
                }
                else
                {
                    return shoppingCart.Product.Price100;
                }
            }

        }
    }
}
