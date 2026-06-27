using System.Security.Claims;
using Akwaba.Domain.Interfaces;

namespace Akwaba.Web.Infrastructure;

/// <summary>Résout l'hôtel courant et l'utilisateur à partir des claims de la requête.</summary>
public class ContexteTenant(IHttpContextAccessor accessor) : ITenantContext, IUtilisateurCourant
{
    private ClaimsPrincipal? User => accessor.HttpContext?.User;

    public Guid? TenantId =>
        Guid.TryParse(User?.FindFirstValue("tenant"), out var id) ? id : null;

    public bool EstPlateforme =>
        User?.IsInRole("SUPER_ADMIN") == true || User?.IsInRole("MODERATEUR") == true;

    Guid? IUtilisateurCourant.Id =>
        Guid.TryParse(User?.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    string IUtilisateurCourant.Nom =>
        User?.FindFirstValue("nom_complet") ?? User?.Identity?.Name ?? "Système";
}
