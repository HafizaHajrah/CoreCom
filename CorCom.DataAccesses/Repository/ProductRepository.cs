using CoreBooks.DataAccesses.Data;
using CoreBooks.DataAccesses.Repository.IRepository;
using CoreBooks.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace CoreBooks.DataAccesses.Repository
{
    public class ProductRepository :  Repository<Product>,IProductRepository
    {
        private ApplicationDbContext _db;
        public ProductRepository(ApplicationDbContext db):base(db) 
        {
            _db = db;
        }

        public void Update(Product product)
        {
            var productfromdb = _db.Products.FirstOrDefault(u => u.ProductId == product.ProductId);
            if (productfromdb != null) { 
                productfromdb.Title= product.Title;
                productfromdb.Description= product.Description;
                productfromdb.CategoryId= product.CategoryId;
                productfromdb.Price50= product.Price50;
                productfromdb.Price= product.Price;
                productfromdb.ListPrice= product.ListPrice;
                productfromdb.Price100= product.Price100;
                productfromdb.ISBN= product.ISBN;
                productfromdb.Author= product.Author;
                if (product.ImgUrl != null) 
                { 
                    productfromdb.ImgUrl= product.ImgUrl;
                }

            }

        }
    }
}
