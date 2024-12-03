using MongoDB.Bson;

namespace Ticket.Models;

public class KafkaErrorLogModel
{
    public ObjectId Id { get; set; }
    public string Topic { get; set; }
    public string Message { get; set; }
    public string Error { get; set; }
}