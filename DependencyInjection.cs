using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Web;
using UvA.Workflow.Security.Messages;
using UvA.Workflow.Security.Pdf;
using UvA.Workflow.Security.Users;
using UvA.Workflow.Users;

namespace UvA.Workflow.Security;

public static class DependencyInjection
{
    public static IServiceCollection AddSecurity(this IServiceCollection services, IConfiguration config)
    {
        services.AddScoped<MessagesRepository>();
        services.AddScoped<TokenService>();
        services.AddScoped<PdfService>();
        services.AddScoped<GraphService>();
        services.AddScoped<IUserService, EntraUserService>();
        
        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddMicrosoftIdentityWebApi(config)
            .EnableTokenAcquisitionToCallDownstreamApi()
            .AddMicrosoftGraph(config.GetSection("AzureAd"))
            .AddInMemoryTokenCaches();
        
        return services;
    }
}