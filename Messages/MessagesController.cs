using Microsoft.AspNetCore.Mvc;

namespace UvA.Workflow.Security.Messages;

[ApiController]
[Route("[controller]")]
public class MessagesController(MessagesRepository repository) : ControllerBase
{
    [HttpGet("{instanceId}")]
    public async Task<IEnumerable<MessageDto>> GetMessages(string instanceId, CancellationToken ct)
    {
        var messages = await repository.GetMessages(instanceId, ct);
        return messages.Select(MessageDto.Create);
    }
}

public record MessageDto(string Id)
{
    public static MessageDto Create(Message message) => new(message.Id);
}