using CoreBooks.DataAccesses.Data;
using CoreBooks.Models;
using CoreBooks.Utility;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace CoreBooks.DataAccesses.DbInitializer
{
    public class DbInitializer : IDbInitializer
    {
        
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _db;
        public DbInitializer(
          UserManager<IdentityUser> userManager,
          RoleManager<IdentityRole> roleManager,
          ApplicationDbContext db)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _db =db;         
        }

        public void Initialize()
        {
            //migrations which are pending will automatially be applied
            try{
                if (_db.Database.GetPendingMigrations().Count() > 0)
                {
                    _db.Database.Migrate();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Migration Error: {ex.Message}");
            }
            //create roles if they are not created 
            if (!_roleManager.RoleExistsAsync(SD.Role_Customer).GetAwaiter().GetResult())
            {
                _roleManager.CreateAsync(new IdentityRole(SD.Role_Customer)).GetAwaiter().GetResult();
                _roleManager.CreateAsync(new IdentityRole(SD.Role_Admin)).GetAwaiter().GetResult();
                _roleManager.CreateAsync(new IdentityRole(SD.Role_Employee)).GetAwaiter().GetResult();
                _roleManager.CreateAsync(new IdentityRole(SD.Role_Company)).GetAwaiter().GetResult();

                //if roles are not created then creating the role of Admin
                _userManager.CreateAsync(new ApplicationUser
                {
                    UserName = "mainadmin@gmail.com",
                    Email="mainadmin@gmail.com",
                    Name="Hafiza Hajrah",
                    PhoneNumber="0837387383",
                    StreetAddress="Lahore Pakistan",
                    State="Punjab",
                    PostalCode="54000",
                    City="Lahore",
                },"ADMIN@23oct").GetAwaiter().GetResult();

                ApplicationUser user = _db.ApplicationUsers.FirstOrDefault(u => u.Email == "mainadmin@gmail.com");
                _userManager.AddToRoleAsync(user, SD.Role_Admin).GetAwaiter().GetResult();

            }
            return;
        }
    }
}
