using Microsoft.AspNetCore.Mvc;
using UvA.Workflow.Users;

namespace UvA.Workflow.Security.Messages;

[ApiController]
[Route("[controller]")]
public class MessagesController(MessagesRepository repository, IUserRepository userRepository,
    IUserService userService) : ControllerBase
{
    [HttpGet("{instanceId}")]
    public async Task<IEnumerable<MessageDto>> GetMessages(string instanceId, CancellationToken ct)
    {
        var messages = await repository.GetMessages(instanceId, ct);
        return await ToDto(messages, ct);
    }

    private async Task<IEnumerable<MessageDto>> ToDto(ICollection<Message> messages, CancellationToken ct)
    {
        var userIds = messages.SelectMany(m => m.Items).Select(i => i.AuthorId).Distinct();
        var users = (await userRepository.GetByIds(userIds.ToList(), ct)).ToDictionary(u => u.Id);
        
        return messages.Select(m => MessageDto.Create(m, users));
    }

    [HttpPost("{instanceId}")]
    public async Task<MessageDto> AddMessage(string instanceId, MessageInput input, CancellationToken ct)
    {
        var user = await userService.GetCurrentUser(ct);
        if (user == null) throw new UnauthorizedAccessException();

        var item = new MessageItem
        {
            AuthorId = user.Id,
            DateTime = DateTime.Now,
            Kind = input.Kind,
            Body = input.Body
        };

        Message? message;
        if (input.ReplyToId != null)
        {
            message = await repository.AddMessageItem(instanceId, input.ReplyToId, item, ct);
            if (message == null) throw new InvalidOperationException();
        }
        else
        {
            message = new Message
            {
                InstanceId = instanceId,
                QuestionName = input.QuestionName,
                Items = [item]
            };
            await repository.AddMessage(message, ct);
        }
        return (await ToDto([message], ct)).First();
    }
}

public record MessageInput(
    string? QuestionName,
    string? ReplyToId,
    ItemKind Kind = ItemKind.Message,
    string? Body = null
);

public record MessageDto(string Id, string? QuestionName, bool IsClosed, MessageItemDto[] Items)
{
    public static MessageDto Create(Message message, Dictionary<string, User> users) => new(
        message.Id,
        message.QuestionName,
        message.Items.LastOrDefault()?.Kind == ItemKind.Close,
        message.Items.Select(i => MessageItemDto.Create(i, users)).ToArray()    
    );
}

public record UserDto(string Id, string DisplayName)
{
    public static UserDto Create(User user) => new(user.Id, user.DisplayName);
}

public record MessageItemDto(DateTime DateTime, UserDto User, string? Body)
{
    public static MessageItemDto Create(MessageItem item, Dictionary<string, User> users) => new(
        item.DateTime, 
        UserDto.Create(users[item.AuthorId]), 
        item.Body
    );
}