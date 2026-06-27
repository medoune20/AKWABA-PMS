using Akwaba.Domain.Common;
using Akwaba.Domain.Entites;
using Akwaba.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Akwaba.Web.Controllers;

public class ChambresController(AkwabaDbContext db) : ControleurApp(db)
{
    public async Task<IActionResult> Index()
    {
        var chambres = await Db.Chambres.Include(c => c.TypeChambre)
            .OrderBy(c => c.Numero).ToListAsync();
        ViewBag.Types = await Db.TypesChambre.OrderBy(t => t.Libelle).ToListAsync();
        return View(chambres);
    }

    [Authorize(Roles = "GERANT,SUPER_ADMIN")]
    public async Task<IActionResult> Types()
    {
        var types = await Db.TypesChambre.Include(t => t.Tarifs).OrderBy(t => t.Libelle).ToListAsync();
        return View(types);
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "GERANT")]
    public async Task<IActionResult> CreerType(string libelle, short capacite, string? description)
    {
        if (!string.IsNullOrWhiteSpace(libelle))
        {
            Db.TypesChambre.Add(new TypeChambre { Libelle = libelle, Capacite = capacite, Description = description });
            await Db.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Types));
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "GERANT")]
    public async Task<IActionResult> CreerTarif(Guid typeChambreId, int prix, DateOnly debut, DateOnly fin, string saison)
    {
        Db.Tarifs.Add(new Tarif
        {
            TypeChambreId = typeChambreId, PrixNuitFcfa = prix,
            DateDebut = debut, DateFin = fin, LibelleSaison = string.IsNullOrWhiteSpace(saison) ? "Standard" : saison
        });
        await Db.SaveChangesAsync();
        return RedirectToAction(nameof(Types));
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "GERANT")]
    public async Task<IActionResult> CreerChambre(Guid typeChambreId, string numero, short etage)
    {
        if (!string.IsNullOrWhiteSpace(numero))
        {
            Db.Chambres.Add(new Chambre { TypeChambreId = typeChambreId, Numero = numero, Etage = etage });
            await Db.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangerEtatMenage(Guid id, StatutMenage statut)
    {
        var c = await Db.Chambres.FirstOrDefaultAsync(x => x.Id == id);
        if (c is not null) { c.StatutMenage = statut; await Db.SaveChangesAsync(); }
        return RedirectToAction(nameof(Index));
    }
}
