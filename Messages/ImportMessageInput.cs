using UvA.Workflow.Users;

namespace UvA.Workflow.Security.Messages;

public record ImportMessageInput(string? QuestionName, MessageItemInput[] Items)
{
    public async Task<Message> ToMessage(string instanceId, IUserService userService, CancellationToken ct)
    {
        var list = new List<MessageItem>();
        foreach (var item in Items)
        {
            var user = await userService.AddOrUpdateUser(item.User.UserName, item.User.DisplayName, item.User.Email, ct);
            list.Add(new MessageItem
            {
                DateTime = item.DateTime,
                AuthorId = user.Id,
                Body = item.Body,
                Kind = item.Kind
            });
        }

        return new Message
        {
            InstanceId = instanceId,
            QuestionName = QuestionName,
            Items = list.ToArray()
        };
    }
}

public record MessageItemInput(DateTime DateTime, UserSearchResult User, string? Body, ItemKind Kind);