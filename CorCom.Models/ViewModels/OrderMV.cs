using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreBooks.Models.ViewModels
{
    public class OrderMV
    {
        public OrderHeader OrderHeaders { get; set; }
        public IEnumerable<OrderDetail> OrderDetailList { get; set; }
    }
}
