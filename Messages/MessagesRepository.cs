using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace UvA.Workflow.Security.Messages;

public class MessagesRepository(IMongoDatabase database)
{
    private readonly IMongoCollection<Message> _messagesCollection =
        database.GetCollection<Message>("messages");

    private static FilterDefinitionBuilder<Message> Filter => Builders<Message>.Filter; 
    
    public async Task Create(Message message, CancellationToken ct) 
        => await _messagesCollection.InsertOneAsync(message, cancellationToken: ct);

    public async Task<ICollection<Message>> GetMessages(string instanceId, CancellationToken ct)
    {
        if (!ObjectId.TryParse(instanceId, out var instanceObjectId))
            return [];
        
        var cursor = await _messagesCollection
            .FindAsync(Filter.Eq("InstanceId", instanceObjectId), cancellationToken: ct) ;
        return await cursor.ToListAsync(ct);
    }
    
    public async Task<Message?> AddMessageItem(string instanceId, string messageId,
        MessageItem item, CancellationToken ct)
    {
        if (!ObjectId.TryParse(messageId, out var objectId) || !ObjectId.TryParse(instanceId, out var instanceObjectId))
            return null;
        
        var message = await _messagesCollection.FindOneAndUpdateAsync(
            Filter.And(Filter.Eq("InstanceId", instanceObjectId), Filter.Eq("_id", objectId)),
            Builders<Message>.Update.Push(m => m.Items, item),
            cancellationToken: ct
        );
        
        message.Items = message.Items.Append(item).ToArray();
        
        return message;
    }
    
    public async Task AddMessage(Message message, CancellationToken ct)
    {
        await _messagesCollection.InsertOneAsync(message, cancellationToken: ct);
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

    public string? Body { get; set; }
    
    public ItemKind Kind { get; set; }
}

public enum ItemKind
{
    Message,
    Close
}