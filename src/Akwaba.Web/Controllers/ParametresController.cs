using Akwaba.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Akwaba.Web.Controllers;

[Authorize(Roles = "GERANT")]
public class ParametresController(AkwabaDbContext db) : ControleurApp(db)
{
    public async Task<IActionResult> Index()
    {
        var tid = Guid.Parse(User.FindFirst("tenant")!.Value);
        ViewBag.Tenant = await Db.Tenants.FirstOrDefaultAsync(t => t.Id == tid);
        ViewBag.Utilisateurs = await Db.Users.Where(u => u.TenantId == tid).ToListAsync();
        ViewBag.References = await Db.DonneesReference.Where(r => r.TenantId == null).ToListAsync();
        return View();
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EnregistrerHoraires(TimeOnly checkIn, TimeOnly checkOut)
    {
        var tid = Guid.Parse(User.FindFirst("tenant")!.Value);
        var t = await Db.Tenants.FirstOrDefaultAsync(x => x.Id == tid);
        if (t is not null)
        {
            t.HeureCheckIn = checkIn; t.HeureCheckOut = checkOut;
            await Db.SaveChangesAsync();
            TempData["Ok"] = "Paramètres enregistrés.";
        }
        return RedirectToAction(nameof(Index));
    }
}
