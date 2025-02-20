using CoreBooks.DataAccesses.Data;
using CoreBooks.DataAccesses.Repository.IRepository;
using CoreBooks.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreBooks.DataAccesses.Repository
{
    public class CompanyRepository : Repository<Company>, ICompanyRepository
    {
        private readonly ApplicationDbContext _db;
        public CompanyRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }

        public void Update(Company company)
        {
            var companyfromdb= _db.Companies.FirstOrDefault(u=>u.CompanyId==company.CompanyId);
            if (companyfromdb != null) { 
                companyfromdb.CompanyId = company.CompanyId;
                companyfromdb.PhoneNumber = company.PhoneNumber;
                companyfromdb.City = company.City;
                companyfromdb.CompanyName= company.CompanyName;
                companyfromdb.StreetAddress = company.StreetAddress;
                companyfromdb.State = company.State;
                companyfromdb.PostalCode= company.PostalCode;
            }
        }
    }
}
