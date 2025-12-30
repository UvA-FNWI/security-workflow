using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using UvA.Workflow.Entities.Domain;
using UvA.Workflow.Security.Extensions;
using UvA.Workflow.Users;
using UvA.Workflow.WorkflowInstances;

namespace UvA.Workflow.Security.Users;

public class EntraUserService(IUserRepository userRepository, IMemoryCache cache,
    IHttpContextAccessor contextAccessor, GraphService graphService,
    ILogger<EntraUserService> logger,
    IWorkflowInstanceRepository instanceRepository) : UserServiceBase(userRepository, cache), IUserService
{
    private const string DepartmentDefinition = "Department";
    private static readonly string[] TargetRoles = ["Viewer", "ISO"]; 
    
    private static Dictionary<string, string[]> Faculties => new()
    {
        ["UvA"] = ["FNWI", "FEB", "FGw", "FMG", "FdR", "BB", "ICTS", "AC", "FS", "StS", "UB"],
        ["HvA"] = ["FDMCI", "FMR", "FBE", "FT", "FOO", "FBSV", "ICTS", "AC", "FS", "SZ", "UB", "BS"],
    };
    
    public Task<IEnumerable<string>> GetRoles(User user, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<UserSearchResult>> FindUsers(string query, CancellationToken cancellationToken)
        => graphService.FindUsers(query, cancellationToken);

    public async Task<User?> GetCurrentUser(CancellationToken ct = default)
    {
        var upn = contextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.Upn);
        if (upn == null) return null;
        var user = await GetUser(upn, ct);
        if (user != null && !IsCached(upn)) 
            await UpdateRoles(user, ct);
        return user;
    }

    private async Task UpdateRoles(User user, CancellationToken ct)
    {
        var roles = GetRoles();

        var current = (await instanceRepository.GetByRoles(DepartmentDefinition, TargetRoles, user.UserName, ct)).ToList();
        foreach (var toDelete in current.Where(c => !roles.Any(r => r.Department == c.ExternalId && r.Role == c.Role)))
        {
            toDelete.Instance.Properties[toDelete.Role] = new BsonArray(
                toDelete.Users.Where(r => r.UserName != user.UserName).Select(u => u.ToBsonDocument())
            );
            await instanceRepository.SaveValue(toDelete.Instance, null, toDelete.Role, ct);
        }

        foreach (var toAddGroup in roles
                     .Where(r => !current.Any(c => c.ExternalId == r.Department && c.Role == r.Role))
                     .GroupBy(r => r.Department))
        {
            var inst = await instanceRepository.GetByExternalId(DepartmentDefinition, toAddGroup.Key, ct);
            if (inst == null)
            {
                logger.LogError("Missing department {code} for {username}", toAddGroup.Key, user.UserName);
                continue;
            }

            foreach (var role in toAddGroup.Select(r => r.Role))
            {
                var users = ObjectContext.GetValue(
                    inst.Properties.GetValueOrDefault(role),
                    new PropertyDefinition { Type = "[User]" }
                ) as User[] ?? [];
                inst.Properties[role] = new BsonArray(users.Append(user).Select(u => u.ToBsonDocument()));
                await instanceRepository.SaveValue(inst, null, role, ct);
            }
        }
    }
    
    private record RolePair(string Role, string Department);

    private ICollection<RolePair> GetRoles()
    {
        var principal = contextAccessor.HttpContext?.User;
        var upn = principal?.FindFirstValue(ClaimTypes.Upn);
        if (upn == null)
            return [];
        var tokenDepts = principal!.FindAll(ClaimTypes.Role)
            .Select(c => c.Value)
            .Select(c => new RolePair(c.Split('.').First(), c.Split('.').Last()))
            .ToList();

        tokenDepts.AddRange(
            tokenDepts
                .Where(r => r.Role is "PO" or "ISO")
                .SelectMany(r => Faculties.Where(e => e.Value.Contains(r.Department)).Select(e => e.Key))
                .Distinct()
                .Select(r => new RolePair("Viewer", r))
                .ToArray()
        );

        return tokenDepts;
    }

    public async Task<IEnumerable<string>> GetRolesOfCurrentUser(CancellationToken ct = default)
    {
        var upn = contextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.Upn);
        if (upn == null) return [];
        if (!IsCached(upn)) 
            await UpdateRoles(await GetUser(upn, ct) ?? throw new InvalidOperationException("User not found"), ct);
        return [];
    }
}