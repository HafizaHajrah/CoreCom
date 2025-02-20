using System.Diagnostics;
using System.Security.Claims;
using CoreBooks.DataAccesses.Repository.IRepository;
using CoreBooks.Models;
using CoreBooks.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreBooks.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IUnitOfWork _unitOfWork;
        public HomeController(ILogger<HomeController> logger, IUnitOfWork unitOfWork)
        {
            _logger = logger;
            _unitOfWork = unitOfWork;
        }

        public IActionResult Index()
        {
            
            IEnumerable<Product> productlist = _unitOfWork.Product.GetAll(includeproperties: "Category");
            return View(productlist);
        }
        public IActionResult Details(int productID)
        {
            ShoppingCart Cart = new()
            {
                Product = _unitOfWork.Product.Get(u => u.ProductId == productID, includeproperties: "Category"),
                Count = 1,
                ProductId = productID
            };

            return View(Cart);
        }
        [HttpPost]
        [Authorize]
        public IActionResult Details(ShoppingCart shoppingcart)
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;
            shoppingcart.ApplicationUserId = userId;

            var cartfromdb = _unitOfWork.ShoppingCart.Get(u => u.ApplicationUserId == userId &&
                u.ProductId == shoppingcart.ProductId);

            if (cartfromdb != null)
            {
                if (cartfromdb.Count < shoppingcart.Count)
                {
                    cartfromdb.Count += shoppingcart.Count;
                }
                else if (cartfromdb.Count > shoppingcart.Count)
                {
                    cartfromdb.Count -= shoppingcart.Count;
                }
                _unitOfWork.ShoppingCart.Update(cartfromdb);
                _unitOfWork.Save();
                TempData["success"] = "Cart Updated Successfully";
            }
            else
            {
                _unitOfWork.ShoppingCart.Add(shoppingcart);
                _unitOfWork.Save();
                TempData["success"] = "Item Added Successfully";
            }
            HttpContext.Session.SetInt32(SD.SessionCart, _unitOfWork.ShoppingCart
                .GetAll(u => u.ApplicationUserId == userId).Count());
            return RedirectToAction(nameof(Index));
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
