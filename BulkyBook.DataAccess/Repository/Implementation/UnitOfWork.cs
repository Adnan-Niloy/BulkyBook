using BulkyBook.DataAccess.Data;
using BulkyBook.DataAccess.Repository.Interface;

namespace BulkyBook.DataAccess.Repository.Implementation
{
    public class UnitOfWork : IUnitOfWork
    {
        public ICategoryRepository CategoryRepository { get; private set; }

        public ICoverTypeRepository CoverTypeRepository { get; private set; }

        public IProductRepository ProductRepository { get; private set; }

        public ICompanyRepository CompanyRepository { get; private set; }

        public IShoppingCartRepository ShoppingCartRepository { get; private set; }

        public IApplicationUserRepository ApplicationUserRepository { get; private set; }

        private readonly ApplicationDbContext _db;

        public UnitOfWork(ApplicationDbContext db)
        {
            _db = db;
            CategoryRepository = new CategoryRepository(_db);
            CoverTypeRepository = new CoverTypeRepository(_db);
            ProductRepository = new ProductRepository(_db);
            CompanyRepository = new CompanyRepository(_db);
            ShoppingCartRepository = new ShippingCartRepository(_db);
            ApplicationUserRepository = new ApplicationUserRepository(_db);
        }

        public void Save()
        {
            _db.SaveChanges();
        }
    }
}
