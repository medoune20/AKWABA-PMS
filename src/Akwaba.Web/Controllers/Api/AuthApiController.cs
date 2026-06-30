using Akwaba.Domain.Common;
using Akwaba.Infrastructure.Persistence;
using Akwaba.Sync;
using Akwaba.Web.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Akwaba.Web.Controllers.Api;

[ApiController]
[Route("api/auth")]
[AllowAnonymous]
public class AuthApiController(
    UserManager<AppliUtilisateur> users,
    AkwabaDbContext db,
    ServiceJeton jetons) : ControllerBase
{
    /// <summary>Authentifie un utilisateur d'hôtel et renvoie un JWT pour la synchronisation.</summary>
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest req)
    {
        var user = await users.FindByEmailAsync(req.Email);
        if (user is null || !await users.CheckPasswordAsync(user, req.MotDePasse))
            return Unauthorized(new { message = "Identifiants invalides." });

        if (user.TenantId is not Guid tid)
            return Unauthorized(new { message = "Compte non rattaché à un hôtel." });

        var tenant = await db.Tenants.FirstOrDefaultAsync(t => t.Id == tid);
        if (tenant is null || tenant.Statut != StatutTenant.Approuve)
            return Unauthorized(new { message = "Hôtel non approuvé ou suspendu." });

        var roles = await users.GetRolesAsync(user);
        var (token, expire) = jetons.Creer(user, roles);
        return Ok(new LoginResponse(token, expire, tid, tenant.Nom, user.Id,
            user.NomComplet, roles.ToArray()));
    }
}
