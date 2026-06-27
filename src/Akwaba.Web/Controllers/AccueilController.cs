using Akwaba.Application.Services;
using Akwaba.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Akwaba.Web.Controllers;

public class AccueilController(AkwabaDbContext db, ServiceTableauBord tableauBord) : ControleurApp(db)
{
    public async Task<IActionResult> Index()
    {
        ViewBag.Indicateurs = await tableauBord.CalculerAsync();
        ViewBag.Arrivees = await Db.Sejours
            .Include(s => s.Chambre)
            .Include(s => s.Reservation!).ThenInclude(r => r.Client)
            .Where(s => s.DateArrivee == DateOnly.FromDateTime(DateTime.UtcNow) && s.Statut == Domain.Common.StatutSejour.Reserve)
            .OrderBy(s => s.Chambre!.Numero)
            .ToListAsync();
        return View();
    }

    [AllowAnonymous]
    public IActionResult Erreur() => View();
}
