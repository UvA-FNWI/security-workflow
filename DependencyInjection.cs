using Microsoft.Extensions.DependencyInjection;
using UvA.Workflow.Security.Messages;

namespace UvA.Workflow.Security;

public static class DependencyInjection
{
    public static IServiceCollection AddSecurity(this IServiceCollection services)
    {
        services.AddScoped<MessagesRepository>();
        return services;
    }
}