using BulkyBook.Models;

namespace BulkyBook.DataAccess.Repository.Interface
{
    public interface ICompanyRepository : IRepository<Company>
    {
        void Update(Company company);
    }
}
