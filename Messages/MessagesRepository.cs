using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace UvA.Workflow.Security.Messages;

public class MessagesRepository(IMongoDatabase database)
{
    private readonly IMongoCollection<Message> _messagesCollection =
        database.GetCollection<Message>("messages");
    
    public async Task Create(Message message, CancellationToken ct) 
        => await _messagesCollection.InsertOneAsync(message, cancellationToken: ct);

    public async Task<IEnumerable<Message>> GetMessages(string instanceId, CancellationToken ct)
    {
        var cursor = await _messagesCollection
            .FindAsync(m => m.InstanceId == instanceId, cancellationToken: ct) ;
        return await cursor.ToListAsync(ct);
    }
}

public class Message
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = null!;

    [BsonRepresentation(BsonType.ObjectId)]
    public string InstanceId { get; set; } = null!;
    public string? QuestionName { get; set; }

    public MessageItem[] Items { get; set; } = [];
}

public class MessageItem
{
    public DateTime DateTime { get; set; }

    [BsonRepresentation(BsonType.ObjectId)]
    public string AuthorId { get; set; } = null!;

    public string Body { get; set; } = null!;
}