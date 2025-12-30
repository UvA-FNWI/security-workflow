using Microsoft.Graph;
using UvA.Workflow.Users;

namespace UvA.Workflow.Security.Users;

public class GraphService(GraphServiceClient client)
{
    public async Task<IEnumerable<UserSearchResult>> FindUsers(string query, CancellationToken cancellationToken)
    {
        var resp = await client.Users.GetAsync(config =>
        {
            config.QueryParameters.Select = ["displayName", "userPrincipalName", "mail"];
            config.QueryParameters.Search = $"\"displayName:{query}\"";
            config.Headers.Add("ConsistencyLevel", "eventual");
        }, cancellationToken);
        return resp?.Value?
            .Where(u => u.UserPrincipalName != null && u.DisplayName != null && u.Mail != null)
            .Select(u => new UserSearchResult(u.UserPrincipalName!, u.DisplayName!, u.Mail!)) ?? [];
    }
}