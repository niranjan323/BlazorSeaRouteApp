using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SeaRouteWebApis.Context
{
    public class DbRecordInserter
    {
        private readonly ILogger<DbRecordInserter> _logger;

        public DbRecordInserter(ILogger<DbRecordInserter> logger)
        {
            _logger = logger;
        }

        public void Insert<T>(T entity) where T : class
        {
            _logger.LogInformation($"Mock insert: Pretending to insert a single record of type {typeof(T).Name}.");
            Console.WriteLine("Insert successful.");
        }

        public void Update<T>(T entity) where T : class
        {
            _logger.LogInformation($"Mock update: Pretending to update a record of type {typeof(T).Name}.");
            Console.WriteLine("Update successful.");
        }

        public T Get<T>(int id) where T : class, new()
        {
            _logger.LogInformation($"Mock get: Pretending to retrieve a record of type {typeof(T).Name} with ID {id}.");
            return new T(); // Return dummy data
        }

        public async Task InsertRecordsToDbAsync<T>(List<T> records) where T : class
        {
            _logger.LogInformation($"Mock insert async: Pretending to insert {records.Count} records of type {typeof(T).Name}.");
            await Task.Delay(100); // Simulate async delay
            Console.WriteLine("Async insert successful.");
        }

        public bool InsertRecordsToDb<T>(List<T> records) where T : class
        {
            _logger.LogInformation($"Mock insert: Pretending to insert {records.Count} records of type {typeof(T).Name}.");
            Console.WriteLine("Insert successful.");
            return true;
        }

        public List<T> GetAll<T>() where T : class, new()
        {
            _logger.LogInformation($"Mock get all: Pretending to retrieve all records of type {typeof(T).Name}.");
            return new List<T> { new T() }; // Return dummy list
        }
    }
}
