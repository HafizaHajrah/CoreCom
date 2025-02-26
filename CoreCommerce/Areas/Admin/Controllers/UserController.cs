using CoreBooks.DataAccesses.Data;
using CoreBooks.Models;
using CoreBooks.Models.ViewModels;
using CoreBooks.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace CoreBooks.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class UserController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<IdentityUser> _userManager;
        public UserController(ApplicationDbContext db, UserManager<IdentityUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }
        public IActionResult Index()
        {
            return View();
        }
        public IActionResult RoleManagement(string userId)
        {
            var RoleID = _db.UserRoles.FirstOrDefault(u => u.UserId == userId).RoleId;
            RoleManagementMV rolemanagemnt = new RoleManagementMV()
            {
                applicationUser = _db.ApplicationUsers.Include(u => u.company).FirstOrDefault(u => u.Id == userId),
                CompanyList = _db.Companies.Select(u => new SelectListItem
                {
                    Text = u.CompanyName,
                    Value = u.CompanyId.ToString(),
                }),
                rolesList = _db.Roles.Select(i => new SelectListItem
                {
                    Text = i.Name,
                    Value = i.Name
                })
            };
            rolemanagemnt.applicationUser.Role = _db.Roles.FirstOrDefault(u => u.Id == RoleID).Name;
            return View(rolemanagemnt);
        }
        [HttpPost]
        public IActionResult RoleManagement(RoleManagementMV rolemv)
        {
            string userRoleID = _db.UserRoles.FirstOrDefault(u => u.UserId == rolemv.applicationUser.Id).RoleId;
            string oldrolesfromdb = _db.Roles.FirstOrDefault(u => u.Id == userRoleID).Name;
            if (!(rolemv.applicationUser.Role == oldrolesfromdb))
            {
                //current role is change
                ApplicationUser Applicationuser = _db.ApplicationUsers.FirstOrDefault(u => u.Id == rolemv.applicationUser.Id);

                if (rolemv.applicationUser.Role == SD.Role_Company)
                {
                    Applicationuser.CompanyId = rolemv.applicationUser.CompanyId;
                }
                //if old user was company user so in case of becoming other user companyId from the user table 
                //should be removed
                if (oldrolesfromdb == SD.Role_Company)
                {
                    Applicationuser.CompanyId = null;
                }

                _db.SaveChanges();

                _userManager.RemoveFromRoleAsync(Applicationuser, oldrolesfromdb).GetAwaiter().GetResult();
                _userManager.AddToRoleAsync(Applicationuser, rolemv.applicationUser.Role).GetAwaiter().GetResult();
            }
            return RedirectToAction("Index");
        }
        #region API CALLS
        public IActionResult GetAll()
        {
            List<ApplicationUser> userlist = _db.ApplicationUsers.Include(u => u.company).ToList();

            var userRoles = _db.UserRoles.ToList();
            var roles = _db.Roles.ToList();
            foreach (var user in userlist)
            {
                string roleId = userRoles.FirstOrDefault(u => u.UserId == user.Id).RoleId;
                user.Role = roles.FirstOrDefault(u => u.Id == roleId).Name;
                if (user.company == null)
                {
                    user.company = new() { CompanyName = "" };
                }
            }
            return Json(new { data = userlist });
        }
        [HttpPost]
        public IActionResult LockUnlock([FromBody] string id)
        {
            var objfromDb = _db.ApplicationUsers.FirstOrDefault(u => u.Id == id);
            if (objfromDb == null)
            {
                return Json(new { success = false, message = "Error while Locking/Unlocking" });
            }
            if (objfromDb.LockoutEnd != null && objfromDb.LockoutEnd > DateTime.Now)
            {
                //user is currently locked and needed to be unlocked
                objfromDb.LockoutEnd = DateTime.Now;
            }
            else
            {
                objfromDb.LockoutEnd = DateTime.Now.AddYears(1000);
            }
            _db.SaveChanges();
            return Json(new { success = true, message = "Operation Successful" });
        }
        #endregion
    }
}
