using CoreBooks.DataAccesses.Data;
using CoreBooks.DataAccesses.Repository.IRepository;
using CoreBooks.Models;
using CoreBooks.Models.ViewModels;
using CoreBooks.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Reflection.Metadata.Ecma335;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace CoreBooks.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles =SD.Role_Admin)]
    public class CompanyController : Controller
    {
        private readonly IUnitOfWork  _unitOfWork;
        public CompanyController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        public IActionResult Index()
        {
            List<Company> companieslist = _unitOfWork.Company.GetAll().ToList();
            return View(companieslist);
        }
        public IActionResult Upsert(int? id)
        {

            Company company = new Company();
            
            if(id==null || id == 0)
            {
                return View(company);
            }
            else
            {
                company = _unitOfWork.Company.Get(u => u.CompanyId == id);
                return View(company);
            }
        }
        [HttpPost]
        public IActionResult Upsert(Company company)
        {
            if (ModelState.IsValid)
            {
                if (company.CompanyId == 0)
                {
                    _unitOfWork.Company.Add(company);
                    _unitOfWork.Save();
                    TempData["success"] = "Company Created Successfully";
                }
                else
                {
                    _unitOfWork.Company.Update(company);
                    _unitOfWork.Save();
                    TempData["success"] = "Company Updated Successfully";
                }
                return RedirectToAction("Index");
            }
            return View(company);
        }
        #region API Calls
        [HttpGet]
        public IActionResult GetAll()
        {
            List<Company> companieslist = _unitOfWork.Company.GetAll().ToList();
            return Json(new {data= companieslist});
        }
        [HttpDelete]
        public IActionResult Delete(int? id)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }
            else
            {
                var companytobeDeleted = _unitOfWork.Company.Get(u => u.CompanyId == id);
                if (companytobeDeleted == null)
                {
                    return Json(new { success = false, message = "Error while Deleting" });
                }                
                    _unitOfWork.Company.Remove(companytobeDeleted);
                    _unitOfWork.Save();
                    return Json(new { success = true, message = "Company Deleted Successfully" });
            }
            #endregion
        }
    }
}
