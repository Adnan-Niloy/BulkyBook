using BulkyBook.Models;

namespace BulkyBook.DataAccess.Repository.Interface
{
    public interface IProductRepository : IRepository<Product>
    {
        void Update(Product product);
    }
}
