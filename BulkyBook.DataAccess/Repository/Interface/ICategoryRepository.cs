using BulkyBook.Models;

namespace BulkyBook.DataAccess.Repository.Interface
{
    public interface ICategoryRepository : IRepository<Category>
    {
        void Update(Category category);
    }
}
