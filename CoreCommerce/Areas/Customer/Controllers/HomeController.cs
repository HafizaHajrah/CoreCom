using System.Diagnostics;
using CoreBooks.DataAccesses.Repository.IRepository;
using CoreBooks.Models;
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
            Product product = _unitOfWork.Product.Get(u=>u.ProductId==productID,includeproperties: "Category");
            return View(product);
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
