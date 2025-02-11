using CoreComRazor.Models;
using Microsoft.EntityFrameworkCore;

namespace CoreComRazor.Data
{
    public class RazorDbContext:DbContext
    {
        public RazorDbContext(DbContextOptions<RazorDbContext> options):base(options) 
        {
                
        }
        public DbSet<Category> Categories { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Category>().HasData(
                new Category { Id = 1, Name = "SciFi", DisplayOrder = 1 },
                new Category { Id = 2, Name = "Action", DisplayOrder = 2 },
                new Category { Id = 3, Name = "History", DisplayOrder = 3 }
                );
        }
    }
}
