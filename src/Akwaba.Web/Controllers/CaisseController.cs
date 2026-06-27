using System.Security.Claims;
using Akwaba.Application.Services;
using Akwaba.Domain.Common;
using Akwaba.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Akwaba.Web.Controllers;

public class CaisseController(AkwabaDbContext db, ServiceCaisse service) : ControleurApp(db)
{
    public async Task<IActionResult> Index()
    {
        var session = await service.SessionOuverteAsync();
        if (session is not null)
        {
            ViewBag.Totaux = session.Paiements
                .Where(p => p.Statut == StatutPaiement.Confirme)
                .GroupBy(p => p.Moyen)
                .ToDictionary(g => g.Key, g => g.Sum(p => p.MontantFcfa));
        }
        ViewBag.Historique = await Db.SessionsCaisse
            .Where(s => s.FermeeLe != null)
            .OrderByDescending(s => s.OuverteLe).Take(10).ToListAsync();
        return View(session);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Ouvrir(int fonds)
    {
        var id = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var nom = User.FindFirstValue("nom_complet") ?? User.Identity!.Name!;
        try { await service.OuvrirAsync(id, nom, fonds); }
        catch (InvalidOperationException ex) { TempData["Err"] = ex.Message; }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Fermer(Guid id, int montantCompte)
    {
        try { await service.FermerAsync(id, montantCompte); TempData["Ok"] = "Caisse clôturée."; }
        catch (InvalidOperationException ex) { TempData["Err"] = ex.Message; }
        return RedirectToAction(nameof(Index));
    }
}
