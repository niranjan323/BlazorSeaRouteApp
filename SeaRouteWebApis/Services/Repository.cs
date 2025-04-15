using SeaRouteWebApis.Context;
using SeaRouteWebApis.Interfaces;

namespace SeaRouteWebApis.Services;

public class Repository<T> : IRepository<T> where T : class , new()
{
    private readonly DbRecordInserter _dbRecordInserter;

    public Repository(DbRecordInserter dbRecordInserter)
    {
        _dbRecordInserter = dbRecordInserter;
    }

    public void Insert(T entity)
    {
        _dbRecordInserter.Insert(entity);
    }

    public void Update(T entity)
    {
        _dbRecordInserter.Update(entity);
    }

    public T Get(int id)
    {
        return _dbRecordInserter.Get<T>(id);
    }

    public List<T> GetAll()
    {
        return _dbRecordInserter.GetAll<T>();
    }
}
