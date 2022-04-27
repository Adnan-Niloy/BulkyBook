using BulkyBook.Models;

namespace BulkyBook.DataAccess.Repository.Interface
{
    public interface ICoverTypeRepository : IRepository<CoverType>
    {
        void Update(CoverType coverType);
    }
}
