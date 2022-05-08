using BulkyBook.Models;

namespace BulkyBook.DataAccess.Repository.Interface
{
    public interface IOrderDetailRepository : IRepository<OrderDetail>
    {
        void Update(OrderDetail orderDetail);
    }
}
