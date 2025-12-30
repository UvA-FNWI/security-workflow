using MongoDB.Bson;
using MongoDB.Driver;
using UvA.Workflow.Entities.Domain;
using UvA.Workflow.Users;
using UvA.Workflow.WorkflowInstances;

namespace UvA.Workflow.Security.Extensions;

public static class InstanceRepositoryExtensions
{
    private static FilterDefinitionBuilder<WorkflowInstance> Filter => Builders<WorkflowInstance>.Filter;
    
    public static async Task<IEnumerable<RoleSearchResult>> GetByRoles(this IWorkflowInstanceRepository instanceRepository, string workflowDefinition,
        string[] roles, string username, CancellationToken ct)
    {
        var filter = Filter.Or(roles.Select(r => Filter.Eq($"Properties.{r}.Username", new BsonString(username))));
        
        var res = await instanceRepository.GetByWorkflowDefinition(workflowDefinition, filter, ct);
        
        return res.SelectMany(i => roles.Select(r =>
        {
            var value = i.Properties.GetValueOrDefault(r);
            var users = ObjectContext.GetValue(value, new PropertyDefinition {Type = "[User]"}) as User[];
            return users?.Any(u => u.UserName == username) == true ? new RoleSearchResult(r, i, users) : null;
        }).Where(r => r != null)).ToList()!;
    }

    public record RoleSearchResult(string Role, WorkflowInstance Instance, User[] Users)
    {
        public string? ExternalId => Instance.Properties.GetValueOrDefault("ExternalId")?.AsString;
    }

    public static async Task<WorkflowInstance?> GetByExternalId(this IWorkflowInstanceRepository instanceRepository,
        string workflowDefinition, string externalId, CancellationToken ct)
    {
        var result = await instanceRepository.GetByWorkflowDefinition(
            workflowDefinition,
            Filter.Eq(i => i.Properties["ExternalId"], externalId),
            ct
        );
        return result.FirstOrDefault();
    }
}