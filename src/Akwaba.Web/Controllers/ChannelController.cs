using Akwaba.Application.Services;
using Akwaba.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Akwaba.Web.Controllers;

[Authorize(Roles = "GERANT,RECEPTIONNISTE")]
public class ChannelController(AkwabaDbContext db, ServiceImport import) : ControleurApp(db)
{
    public IActionResult Index() => View();

    [HttpPost, ValidateAntiForgeryToken]
    [RequestSizeLimit(5_000_000)]
    public async Task<IActionResult> Importer(IFormFile? fichier, string? colle)
    {
        string contenu;
        if (fichier is { Length: > 0 })
        {
            using var reader = new StreamReader(fichier.OpenReadStream());
            contenu = await reader.ReadToEndAsync();
        }
        else if (!string.IsNullOrWhiteSpace(colle))
        {
            contenu = colle;
        }
        else
        {
            TempData["Err"] = "Fournissez un fichier CSV ou collez des lignes.";
            return RedirectToAction(nameof(Index));
        }

        var res = await import.ImporterReservationsCsvAsync(contenu);
        ViewBag.Resultat = res;
        return View(nameof(Index));
    }
}
