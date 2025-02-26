using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreBooks.Models.ViewModels
{
    public class RoleManagementMV
    {     
        public ApplicationUser applicationUser { get; set; }
        public IEnumerable<SelectListItem>  CompanyList { get; set; }
        public IEnumerable<SelectListItem> rolesList { get; set; }
       
    }
}
