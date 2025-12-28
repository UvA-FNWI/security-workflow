using Microsoft.Extensions.DependencyInjection;
using UvA.Workflow.Security.Messages;
using UvA.Workflow.Security.Pdf;

namespace UvA.Workflow.Security;

public static class DependencyInjection
{
    public static IServiceCollection AddSecurity(this IServiceCollection services)
    {
        services.AddScoped<MessagesRepository>();
        services.AddScoped<TokenService>();
        services.AddScoped<PdfService>();
        return services;
    }
}