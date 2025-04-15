namespace SeaRouteWebApis.Interfaces
{
    public interface IRepository<T>
    {
        void Insert(T entity);
        void Update(T entity);
        T Get(int id);

        List<T> GetAll();
    }
}
