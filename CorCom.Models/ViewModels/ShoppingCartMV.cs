using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreBooks.Models.ViewModels
{
    public class ShoppingCartMV
    {
        public IEnumerable<ShoppingCart> ShoppingCartList { get; set; }
        public OrderHeader orderHeader { get; set; }
       
    }
}
