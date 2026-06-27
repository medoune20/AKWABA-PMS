using System.Security.Claims;
using Akwaba.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace Akwaba.Web.Infrastructure;

/// <summary>Ajoute le tenant et le nom complet aux claims de l'utilisateur connecté.</summary>
public class FabriqueClaims(
    UserManager<AppliUtilisateur> userManager,
    RoleManager<AppliRole> roleManager,
    IOptions<IdentityOptions> options)
    : UserClaimsPrincipalFactory<AppliUtilisateur, AppliRole>(userManager, roleManager, options)
{
    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(AppliUtilisateur user)
    {
        var identity = await base.GenerateClaimsAsync(user);
        if (user.TenantId is Guid tid)
            identity.AddClaim(new Claim("tenant", tid.ToString()));
        identity.AddClaim(new Claim("nom_complet", user.NomComplet));
        return identity;
    }
}
