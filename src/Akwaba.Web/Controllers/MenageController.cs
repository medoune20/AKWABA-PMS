using System.Security.Claims;
using Akwaba.Application.Services;
using Akwaba.Domain.Common;
using Akwaba.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;

namespace Akwaba.Web.Controllers;

public class MenageController(AkwabaDbContext db, ServiceHousekeeping service) : ControleurApp(db)
{
    public async Task<IActionResult> Index()
    {
        ViewBag.Chambres = await service.ChambresAsync();
        ViewBag.Taches = await service.TachesActivesAsync();
        ViewBag.Personnel = await GetPersonnel();
        return View();
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Assigner(Guid chambreId, Guid assigneeId, string nom)
    {
        try { await service.AssignerAsync(chambreId, assigneeId, nom); }
        catch (InvalidOperationException ex) { TempData["Err"] = ex.Message; }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Marquer(Guid chambreId, StatutMenage statut)
    {
        try { await service.MarquerAsync(chambreId, statut); }
        catch (InvalidOperationException ex) { TempData["Err"] = ex.Message; }
        return RedirectToAction(nameof(Index));
    }

    private async Task<List<(Guid Id, string Nom)>> GetPersonnel()
    {
        var tid = Guid.Parse(User.FindFirst("tenant")!.Value);
        var users = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
            .ToListAsync(Db.Users.Where(u => u.TenantId == tid));
        return users.Select(u => (u.Id, u.NomComplet)).ToList();
    }
}
