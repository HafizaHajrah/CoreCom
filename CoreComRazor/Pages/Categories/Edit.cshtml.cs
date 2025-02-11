using Azure;
using CoreComRazor.Data;
using CoreComRazor.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.Reflection.Metadata.Ecma335;

namespace CoreComRazor.Pages.Categories
{
    [BindProperties]
    public class EditModel : PageModel
    {
        private readonly RazorDbContext _db;
        public Category category { get; set; }
        public EditModel(RazorDbContext db)
        {
            _db = db;
        }
        public void OnGet(int? id)
        {
            if(id != null && id!=0)
            {
                category = _db.Categories.FirstOrDefault(u => u.Id == id);
            }                           
        }
        public IActionResult OnPost()
        {
            if (ModelState.IsValid && category!=null)
            {
                _db.Categories.Update(category);
                _db.SaveChanges();
                TempData["success"] = "Category Updated Successfully";
                return RedirectToPage("Index");
            }
            return Page();
        }
    }
}
