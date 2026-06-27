using Akwaba.Domain.Common;
using Akwaba.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

namespace Akwaba.Web.Controllers;

/// <summary>
/// Base des contrôleurs « hôtel » : exige l'authentification et un tenant approuvé.
/// Les comptes plateforme sont redirigés vers l'admin ; les comptes en attente vers l'écran d'attente.
/// </summary>
[Authorize]
public abstract class ControleurApp(AkwabaDbContext db) : Controller
{
    protected AkwabaDbContext Db => db;

    public override async Task OnActionExecutionAsync(ActionExecutingContext ctx, ActionExecutionDelegate next)
    {
        if (User.IsInRole("SUPER_ADMIN") || User.IsInRole("MODERATEUR"))
        {
            ctx.Result = RedirectToAction("Souscriptions", "Admin");
            return;
        }

        var tid = Guid.TryParse(User.FindFirst("tenant")?.Value, out var g) ? g : (Guid?)null;
        if (tid is null)
        {
            ctx.Result = RedirectToAction("Connexion", "Compte");
            return;
        }

        var statut = await db.Tenants.Where(t => t.Id == tid).Select(t => t.Statut).FirstOrDefaultAsync();
        if (statut != StatutTenant.Approuve)
        {
            ctx.Result = RedirectToAction("EnAttente", "Compte");
            return;
        }

        await next();
    }
}
