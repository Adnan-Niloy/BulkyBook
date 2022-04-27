namespace BulkyBook.DataAccess.Repository.Interface
{
    public interface IUnitOfWork
    {
        ICategoryRepository CategoryRepository { get; }

        ICoverTypeRepository CoverTypeRepository { get; }

        IProductRepository ProductRepository { get; }

        void Save();
    }
}
