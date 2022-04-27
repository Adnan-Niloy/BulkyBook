using BulkyBook.DataAccess.Data;
using BulkyBook.DataAccess.Repository.Interface;
using BulkyBook.Models;

namespace BulkyBook.DataAccess.Repository.Implementation
{
    public class ProductRepository : Repository<Product>, IProductRepository
    {
        private readonly ApplicationDbContext _db;

        public ProductRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }

        public void Update(Product product)
        {
            var productFromDb = _db.Products.FirstOrDefault(c => c.Id == product.Id);

            if (productFromDb != null)
            {
                productFromDb.Title = product.Title;
                productFromDb.Description = product.Description;
                productFromDb.ISBN = product.ISBN;
                productFromDb.Price = product.Price;
                productFromDb.ListPrice = product.ListPrice;
                productFromDb.Price50 = product.Price50;
                productFromDb.Price100 = product.Price100;
                productFromDb.Author = product.Author;
                productFromDb.CategoryId = product.CategoryId;
                productFromDb.CoverTypeId = product.CoverTypeId;

                if (product.ImageUrl != null)
                    productFromDb.ImageUrl = product.ImageUrl;
            }

            _db.Update(product);
        }
    }
}
