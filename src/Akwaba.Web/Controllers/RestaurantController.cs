using System.Security.Claims;
using Akwaba.Application.Services;
using Akwaba.Domain.Common;
using Akwaba.Domain.Entites;
using Akwaba.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Akwaba.Web.Controllers;

public class RestaurantController(AkwabaDbContext db, ServicePos pos, ServiceCaisse caisse) : ControleurApp(db)
{
    public async Task<IActionResult> Index(Guid? commande)
    {
        ViewBag.Ouvertes = await pos.CommandesOuvertesAsync();
        ViewBag.Carte = await pos.CarteAsync();
        ViewBag.SejoursEnCours = await Db.Sejours.Include(s => s.Chambre)
            .Include(s => s.Reservation!).ThenInclude(r => r.Client)
            .Where(s => s.Statut == StatutSejour.EnCours)
            .ToListAsync();
        if (commande is Guid id) ViewBag.Commande = await pos.ChargerCommandeAsync(id);
        return View();
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Creer(string? table, Guid? sejourId)
    {
        var cmd = await pos.CreerCommandeAsync(string.IsNullOrWhiteSpace(table) ? null : table, sejourId);
        return RedirectToAction(nameof(Index), new { commande = cmd.Id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Ajouter(Guid commandeId, Guid produitId)
    {
        try { await pos.AjouterProduitAsync(commandeId, produitId); }
        catch (InvalidOperationException ex) { TempData["Err"] = ex.Message; }
        return RedirectToAction(nameof(Index), new { commande = commandeId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Quantite(Guid ligneId, Guid commandeId, int delta)
    {
        await pos.ChangerQuantiteAsync(ligneId, delta);
        return RedirectToAction(nameof(Index), new { commande = commandeId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EnvoyerNote(Guid commandeId)
    {
        try { await pos.EnvoyerSurNoteAsync(commandeId); TempData["Ok"] = "Commande imputée sur la note de la chambre."; }
        catch (InvalidOperationException ex) { TempData["Err"] = ex.Message; }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Encaisser(Guid commandeId, MoyenPaiement moyen)
    {
        var session = await caisse.SessionOuverteAsync();
        if (session is null && moyen == MoyenPaiement.Especes)
        {
            TempData["Err"] = "Ouvrez une session de caisse pour encaisser des espèces.";
            return RedirectToAction(nameof(Index), new { commande = commandeId });
        }
        try { await pos.EncaisserDirectAsync(commandeId, moyen, session?.Id); TempData["Ok"] = "Commande encaissée."; }
        catch (InvalidOperationException ex) { TempData["Err"] = ex.Message; }
        return RedirectToAction(nameof(Index));
    }

    // --- Gestion de la carte (gérant) ---
    [Authorize(Roles = "GERANT")]
    public async Task<IActionResult> Carte()
    {
        ViewBag.Categories = await Db.CategoriesProduit.OrderBy(c => c.Ordre).ToListAsync();
        return View(await Db.Produits.Include(p => p.Categorie).OrderBy(p => p.Libelle).ToListAsync());
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "GERANT")]
    public async Task<IActionResult> CreerCategorie(string libelle, int ordre)
    {
        if (!string.IsNullOrWhiteSpace(libelle))
        {
            Db.CategoriesProduit.Add(new CategorieProduit { Libelle = libelle, Ordre = ordre });
            await Db.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Carte));
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "GERANT")]
    public async Task<IActionResult> CreerProduit(Guid categorieProduitId, string libelle, int prix)
    {
        if (!string.IsNullOrWhiteSpace(libelle))
        {
            Db.Produits.Add(new Produit { CategorieProduitId = categorieProduitId, Libelle = libelle, PrixFcfa = prix });
            await Db.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Carte));
    }
}
