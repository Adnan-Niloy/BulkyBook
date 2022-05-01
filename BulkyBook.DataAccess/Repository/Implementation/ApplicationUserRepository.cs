using BulkyBook.DataAccess.Data;
using BulkyBook.DataAccess.Repository.Interface;
using BulkyBook.Models;

namespace BulkyBook.DataAccess.Repository.Implementation
{
    public class ApplicationUserRepository : Repository<ApplicationUser>, IApplicationUserRepository
    {
        private readonly ApplicationDbContext _db;

        public ApplicationUserRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }


    }
}
