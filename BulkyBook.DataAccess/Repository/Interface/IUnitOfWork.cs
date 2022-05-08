namespace BulkyBook.DataAccess.Repository.Interface
{
    public interface IUnitOfWork
    {
        ICategoryRepository CategoryRepository { get; }

        ICoverTypeRepository CoverTypeRepository { get; }

        IProductRepository ProductRepository { get; }

        ICompanyRepository CompanyRepository { get; }

        IShoppingCartRepository ShoppingCartRepository { get; }

        IApplicationUserRepository ApplicationUserRepository { get; }

        IOrderHeaderRepository OrderHeaderRepository { get; }

        IOrderDetailRepository OrderDetailRepository { get; }

        void Save();
    }
}
