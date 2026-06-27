using Akwaba.Application.Services;
using Akwaba.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Akwaba.Web.Controllers;

[Authorize(Roles = "SUPER_ADMIN,MODERATEUR")]
public class AdminController(AkwabaDbContext db, ServiceTenant tenants) : Controller
{
    public async Task<IActionResult> Souscriptions()
        => View(await tenants.DemandesEnAttenteAsync());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Approuver(Guid id)
    {
        await tenants.ApprouverAsync(id);
        TempData["Ok"] = "Hôtel approuvé.";
        return RedirectToAction(nameof(Souscriptions));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Refuser(Guid id, string motif)
    {
        await tenants.RefuserAsync(id, motif ?? "Non précisé");
        TempData["Ok"] = "Demande refusée.";
        return RedirectToAction(nameof(Souscriptions));
    }

    [Authorize(Roles = "SUPER_ADMIN")]
    public async Task<IActionResult> Hotels() => View(await tenants.TousAsync());

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "SUPER_ADMIN")]
    public async Task<IActionResult> BasculerSuspension(Guid id)
    {
        await tenants.BasculerSuspensionAsync(id);
        return RedirectToAction(nameof(Hotels));
    }

    public async Task<IActionResult> Audit()
        => View(await db.Audits.OrderByDescending(a => a.Horodatage).Take(200).ToListAsync());
}
