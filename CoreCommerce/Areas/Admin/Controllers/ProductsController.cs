using CoreBooks.DataAccesses.Repository;
using CoreBooks.DataAccesses.Repository.IRepository;
using CoreBooks.Models;
using CoreBooks.Models.ViewModels;
using CoreBooks.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.DotNet.Scaffolding.Shared;
using NuGet.ProjectModel;
using System.Linq;
using System.Reflection.Metadata.Ecma335;

namespace CoreBooks.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class ProductsController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IWebHostEnvironment _webHostEnvironment;
        public ProductsController(IUnitOfWork unitOfWork, IWebHostEnvironment webHostEnvironment)
        {
            _unitOfWork= unitOfWork;
            _webHostEnvironment= webHostEnvironment;
        }
        public IActionResult Index()
        {
            List<Product> productlist=_unitOfWork.Product.GetAll(includeproperties:"Category").ToList();
            
            return View(productlist);
        }
        public IActionResult Upsert(int? id) 
        {
            ProductMV productMV = new()
            {
                CategoryList = _unitOfWork.Category.GetAll().Select(u => new SelectListItem
                {
                    Text = u.Name,
                    Value = u.Id.ToString()
                }),
                product = new Product()
            };
            if (id == null || id == 0)
            {
                return View(productMV);
            }
            else
            {
                productMV.product=_unitOfWork.Product.Get(u=>u.ProductId==id);
                return View(productMV);
            }
        }
        [HttpPost]
        public IActionResult Upsert(ProductMV producmv,IFormFile? file)
        {
            string wwwRootPath=_webHostEnvironment.WebRootPath;
            if (ModelState.IsValid)
            {
                if (file != null)
                {
              
                    string extension= Path.GetExtension(file.FileName);
                    string fileName = Guid.NewGuid().ToString() + extension;
                    if (!string.IsNullOrEmpty(producmv.product.ImgUrl))
                    {
                        var oldImgPath= Path.Combine(wwwRootPath, producmv.product.ImgUrl.TrimStart('\\'));
                        if (System.IO.File.Exists(oldImgPath)) 
                        {
                            System.IO.File.Delete(oldImgPath);
                        }
                    }
                    string filePath = Path.Combine(wwwRootPath, @"images\product");
                    using (var filestream = new FileStream(Path.Combine(filePath, fileName),FileMode.Create))
                    {
                        file.CopyTo(filestream);
                    }
                    producmv.product.ImgUrl = @"\images\product\" + fileName;
                }
               if(producmv.product.ProductId==0)
               {
                    _unitOfWork.Product.Add(producmv.product);
               }
                else
                {
                    _unitOfWork.Product.Update(producmv.product);
                }
                _unitOfWork.Save();
                TempData["success"] = "Product Created Successfully";
                return RedirectToAction("Index");
            }
            else
            {
                producmv.CategoryList = _unitOfWork.Category.GetAll().Select(u => new SelectListItem
                {
                    Text = u.Name,
                    Value = u.Id.ToString()
                });              
                return View(producmv);
            }
        }
     
        #region API Calls
        [HttpGet]
        public IActionResult GetAll()
        {
            List<Product> productlist = _unitOfWork.Product.GetAll(includeproperties:"Category").ToList();
            return Json(new {data=productlist});
        }
        [HttpDelete]
        public IActionResult Delete(int? id)
        {
            var producttobedeleted = _unitOfWork.Product.Get(u=>u.ProductId==id);
            if (producttobedeleted == null)
            {
                return Json(new { successs = false, message = "Error while Deleting" });
            }
            var oldImgPath = Path.Combine(_webHostEnvironment.WebRootPath, producttobedeleted.ImgUrl.TrimStart('\\'));
            if (System.IO.File.Exists(oldImgPath))
            {
                System.IO.File.Delete(oldImgPath);
            }
            _unitOfWork.Product.Remove(producttobedeleted);
            _unitOfWork.Save();
            return Json(new { success = true, message = "Deleted Successfully" });
        }
        #endregion
    }
}
