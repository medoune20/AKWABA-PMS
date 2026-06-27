using Akwaba.Application.Services;
using Akwaba.Domain.Common;
using Akwaba.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Akwaba.Web.Controllers;

public class FoliosController(AkwabaDbContext db, ServiceFolio service, ServiceCaisse caisse) : ControleurApp(db)
{
    public async Task<IActionResult> Detail(Guid id)
    {
        var folio = await service.ChargerAsync(id);
        if (folio is null) return NotFound();
        return View(folio);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AjouterLigne(Guid id, string libelle, CategorieLigne categorie, int quantite, int montant)
    {
        try { await service.AjouterLigneAsync(id, libelle, categorie, Math.Max(1, quantite), montant); }
        catch (InvalidOperationException ex) { TempData["Err"] = ex.Message; }
        return RedirectToAction(nameof(Detail), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Encaisser(Guid id, int montant, MoyenPaiement moyen)
    {
        var session = await caisse.SessionOuverteAsync();
        if (session is null && moyen == MoyenPaiement.Especes)
        {
            TempData["Err"] = "Ouvrez une session de caisse avant d'encaisser des espèces.";
            return RedirectToAction(nameof(Detail), new { id });
        }
        var res = await service.EncaisserAsync(id, montant, moyen, session?.Id);
        if (!res.Succes) TempData["Err"] = res.Message;
        else if (res.UrlPaiement is not null) { TempData["Ok"] = res.Message; return Redirect(res.UrlPaiement); }
        else TempData["Ok"] = res.Message;
        return RedirectToAction(nameof(Detail), new { id });
    }

    /// <summary>Simulateur de webhook CinetPay (bac à sable) : confirme le dernier paiement en attente.</summary>
    [HttpGet]
    public async Task<IActionResult> SimulerWebhook(string @ref)
    {
        var paiement = await Db.Paiements
            .Where(p => p.RefExterne == @ref && p.Statut == StatutPaiement.EnAttente)
            .OrderByDescending(p => p.CreeLe).FirstOrDefaultAsync();
        if (paiement is not null)
        {
            await service.ConfirmerPaiementAsync(paiement.Id);
            TempData["Ok"] = "Paiement mobile confirmé (webhook simulé).";
            return RedirectToAction(nameof(Detail), new { id = paiement.FolioId });
        }
        TempData["Err"] = "Aucun paiement en attente pour cette référence.";
        return RedirectToAction("Index", "Accueil");
    }
}
