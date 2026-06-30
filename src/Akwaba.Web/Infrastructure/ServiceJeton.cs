using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Akwaba.Infrastructure.Persistence;
using Microsoft.IdentityModel.Tokens;

namespace Akwaba.Web.Infrastructure;

/// <summary>Émet les jetons JWT pour le client bureau (synchronisation).</summary>
public class ServiceJeton(IConfiguration config)
{
    public (string token, DateTime expire) Creer(AppliUtilisateur user, IEnumerable<string> roles)
    {
        var cle = config["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key manquant.");
        var creds = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(cle)), SecurityAlgorithms.HmacSha256);
        var expire = DateTime.UtcNow.AddHours(double.TryParse(config["Jwt:DureeHeures"], out var h) ? h : 8);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new("nom_complet", user.NomComplet),
        };
        if (user.TenantId is Guid tid) claims.Add(new Claim("tenant", tid.ToString()));
        foreach (var r in roles) claims.Add(new Claim(ClaimTypes.Role, r));

        var token = new JwtSecurityToken(
            issuer: config["Jwt:Issuer"], audience: config["Jwt:Audience"],
            claims: claims, expires: expire, signingCredentials: creds);
        return (new JwtSecurityTokenHandler().WriteToken(token), expire);
    }
}
