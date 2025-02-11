using CoreComRazor.Data;
using CoreComRazor.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CoreComRazor.Pages.Categories
{
    [BindProperties]
    public class DeleteModel : PageModel
    {
        private readonly RazorDbContext _db;
        public Category? category { get; set; }
        public DeleteModel(RazorDbContext db)
        {
            _db = db;
        }
        public IActionResult OnGet(int? id)
        {
            if(id == null || id == 0)
            {
                return NotFound();  
            }
            category = _db.Categories.FirstOrDefault(c => c.Id == id);
            if (category == null) { 
                return NotFound();
            }
            return Page();
        }
        public IActionResult OnPost(int? id) 
        {
            //Category? obj=_db.Categories.Find(Category.Id);
           category= _db.Categories.Find(id);
            if (category == null) {
                return NotFound();
            } 
            _db.Categories.Remove(category);
            _db.SaveChanges();
            TempData["success"] = "Category Deleted Successfully";
            return RedirectToPage("Index");  
        }
    }
}
