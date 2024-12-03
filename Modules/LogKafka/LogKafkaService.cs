using MongoDB.Driver;
using Ticket.Models;

namespace Ticket.Modules.LogKafka;

public interface ILogKafkaService
{
    Task LogErrorAsync(KafkaErrorLogModel model);
}

public class LogKafkaService:ILogKafkaService
{
    private readonly IMongoCollection<KafkaErrorLogModel> _collection;
    public LogKafkaService(IMongoDatabase database) 
    {
        _collection = database.GetCollection<KafkaErrorLogModel>("SeatStatus");;
    }
    public Task LogErrorAsync(KafkaErrorLogModel model)
    {
         return _collection.InsertOneAsync(model);
    }
}