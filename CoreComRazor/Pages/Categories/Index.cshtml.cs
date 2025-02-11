using CoreComRazor.Data;
using CoreComRazor.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Runtime.CompilerServices;

namespace CoreComRazor.Pages.Categories
{
    public class IndexModel : PageModel
    {
        private readonly RazorDbContext _db;
        public List<Category> CategoryList { get; set; }
        public IndexModel(RazorDbContext db)
        {
            _db = db;
        }
        public void OnGet()
        {
            CategoryList=_db.Categories.ToList();   
        }
    }
}
