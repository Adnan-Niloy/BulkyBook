namespace BulkyBook.DataAccess.Repository.Interface
{
    public interface IUnitOfWork
    {
        ICategoryRepository CategoryRepository { get; }

        ICoverTypeRepository CoverTypeRepository { get; }

        void Save();
    }
}
