using System.Text;
using Akwaba.Application.Services;
using Akwaba.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Akwaba.Web.Controllers;

[Authorize(Roles = "GERANT,COMPTABLE")]
public class RapportsController(AkwabaDbContext db, ServiceRapports rapports) : ControleurApp(db)
{
    public async Task<IActionResult> Index(DateOnly? du, DateOnly? au)
    {
        var d = du ?? new DateOnly(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
        var a = au ?? DateOnly.FromDateTime(DateTime.UtcNow);
        ViewBag.Du = d; ViewBag.Au = a;
        ViewBag.Revenus = await rapports.RevenusParCategorieAsync(d, a);
        ViewBag.Moyens = await rapports.EncaissementsParMoyenAsync(d, a);
        ViewBag.CaTotal = await rapports.CaTotalAsync(d, a);
        return View();
    }

    public async Task<IActionResult> ExportCsv(DateOnly du, DateOnly au)
    {
        var csv = await rapports.ExportEncaissementsCsvAsync(du, au);
        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv)).ToArray();
        return File(bytes, "text/csv", $"encaissements_{du:yyyyMMdd}_{au:yyyyMMdd}.csv");
    }
}
