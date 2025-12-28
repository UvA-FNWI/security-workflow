using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using UvA.Workflow.Tools;

namespace UvA.Workflow.Security.Pdf;

public class TokenService(IConfiguration config)
{
    private const string TokenIssuer = "workflow";
    private readonly SymmetricSecurityKey _signingKey = new(Encoding.ASCII.GetBytes(config["FileKey"]!));
    
    public string GenerateVerifier(string id, string type, string[]? allowedForms = null)
    {
        var claims = new Dictionary<string, object>
        {
            ["id"] = id,
            ["type"] = type
        };
        if (allowedForms != null)
            claims.Add("allowedForms", allowedForms.ToSeparatedString(separator: ","));
        
        var handler = new JwtSecurityTokenHandler();
        return handler.CreateEncodedJwt(new SecurityTokenDescriptor
        {
            Expires = DateTime.UtcNow.AddHours(1),
            Issuer = TokenIssuer,
            SigningCredentials = new SigningCredentials(_signingKey, SecurityAlgorithms.HmacSha512Signature),
            Claims = claims
        });
    }

    private Task<TokenValidationResult> GetToken(string token)
    {
        var handler = new JwtSecurityTokenHandler();
        return handler.ValidateTokenAsync(token, new TokenValidationParameters
        {
            IssuerSigningKey = _signingKey,
            ValidateIssuer = true,
            ValidateAudience = false,
            ValidIssuer = TokenIssuer
        });
    }

    public async Task<bool> IsValid(string instanceId, string token, string type)
    {
        var result = await GetToken(token); 
        return result.IsValid
               && result.Claims["id"]?.ToString() == instanceId.ToString()
               && result.Claims["type"]?.ToString() == type;
    }

    public async Task<string[]> GetAllowedForms(string token)
    {
        var result = await GetToken(token);
        return result.Claims["allowedForms"]?.ToString()?.Split(',') ?? [];
    }
}