namespace BulkyBook.DataAccess.Repository.Interface
{
    public interface IUnitOfWork
    {
        ICategoryRepository CategoryRepository { get; }

        void Save();
    }
}
