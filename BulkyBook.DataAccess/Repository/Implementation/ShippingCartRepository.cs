using BulkyBook.DataAccess.Data;
using BulkyBook.DataAccess.Repository.Interface;
using BulkyBook.Models;

namespace BulkyBook.DataAccess.Repository.Implementation
{
    public class ShippingCartRepository : Repository<ShoppingCart>, IShoppingCartRepository
    {
        private readonly ApplicationDbContext _db;

        public ShippingCartRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }


    }
}
