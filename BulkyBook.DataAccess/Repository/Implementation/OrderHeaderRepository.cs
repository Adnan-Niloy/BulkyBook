using BulkyBook.DataAccess.Data;
using BulkyBook.DataAccess.Repository.Interface;
using BulkyBook.Models;

namespace BulkyBook.DataAccess.Repository.Implementation
{
    public class OrderHeaderRepository : Repository<OrderHeader>, IOrderHeaderRepository
    {
        private readonly ApplicationDbContext _db;

        public OrderHeaderRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }

        public void Update(OrderHeader orderHeader)
        {
            _db.Update(orderHeader);
        }

        public void UpdateStatus(int id, string orderStatus, string? paymentStatus = null)
        {
            var orderHeaderFromDb = _db.OrderHeaders.FirstOrDefault(c => c.Id == id);

            if (orderHeaderFromDb == null) return;

            orderHeaderFromDb.OrderStatus = orderStatus;

            if (paymentStatus != null)
                orderHeaderFromDb.PaymentStatus = paymentStatus;
        }

        public void UpdateStripePaymentId(int id, string sessionId, string paymentIntentId)
        {
            var orderHeaderFromDb = _db.OrderHeaders.FirstOrDefault(c => c.Id == id);

            if (orderHeaderFromDb == null) return;

            orderHeaderFromDb.SessionId = sessionId;
            orderHeaderFromDb.PaymentIntentId = paymentIntentId;
        }
    }
}
