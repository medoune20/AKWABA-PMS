using Akwaba.Application.Dtos;
using Akwaba.Application.Services;
using Akwaba.Domain.Common;
using Akwaba.Infrastructure.Persistence;
using Akwaba.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Akwaba.Web.Controllers;

public class ReservationsController(AkwabaDbContext db, ServiceReservation service) : ControleurApp(db)
{
    /// <summary>Plan d'occupation : chambres en lignes, 14 jours en colonnes.</summary>
    public async Task<IActionResult> Index(DateOnly? debut)
    {
        var d = debut ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var fin = d.AddDays(14);
        ViewBag.Debut = d;
        ViewBag.Jours = Enumerable.Range(0, 14).Select(i => d.AddDays(i)).ToList();
        ViewBag.Chambres = await Db.Chambres.Include(c => c.TypeChambre).OrderBy(c => c.Numero).ToListAsync();
        ViewBag.Sejours = await Db.Sejours
            .Include(s => s.Reservation!).ThenInclude(r => r.Client)
            .Where(s => s.Statut != StatutSejour.Annule && s.DateArrivee < fin && s.DateDepart > d)
            .ToListAsync();
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> Creer(DateOnly? arrivee, DateOnly? depart)
    {
        var vm = new CreationReservationVm
        {
            Arrivee = arrivee ?? DateOnly.FromDateTime(DateTime.Today),
            Depart = depart ?? DateOnly.FromDateTime(DateTime.Today.AddDays(1))
        };
        await RemplirListes(vm.Arrivee, vm.Depart);
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Creer(CreationReservationVm vm)
    {
        if (vm.Depart <= vm.Arrivee)
            ModelState.AddModelError(nameof(vm.Depart), "La date de départ doit suivre l'arrivée.");
        if (vm.ClientId is null) ModelState.AddModelError(nameof(vm.ClientId), "Client requis.");
        if (vm.ChambreId is null) ModelState.AddModelError(nameof(vm.ChambreId), "Chambre requise.");

        if (!ModelState.IsValid)
        {
            await RemplirListes(vm.Arrivee, vm.Depart);
            return View(vm);
        }
        try
        {
            var resa = await service.CreerAsync(new CreationReservation(
                vm.ClientId!.Value, vm.ChambreId!.Value, vm.Arrivee, vm.Depart, vm.NbAdultes, vm.Canal));
            TempData["Ok"] = "Réservation créée.";
            return RedirectToAction(nameof(Detail), new { id = resa.Id });
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            await RemplirListes(vm.Arrivee, vm.Depart);
            return View(vm);
        }
    }

    public async Task<IActionResult> Detail(Guid id)
    {
        var resa = await Db.Reservations
            .Include(r => r.Client)
            .Include(r => r.Sejours).ThenInclude(s => s.Chambre)
            .Include(r => r.Sejours).ThenInclude(s => s.Folio)
            .FirstOrDefaultAsync(r => r.Id == id);
        if (resa is null) return NotFound();
        return View(resa);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CheckIn(Guid sejourId, Guid reservationId)
    {
        try { await service.CheckInAsync(sejourId); TempData["Ok"] = "Check-in effectué, note ouverte."; }
        catch (InvalidOperationException ex) { TempData["Err"] = ex.Message; }
        return RedirectToAction(nameof(Detail), new { id = reservationId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CheckOut(Guid sejourId, Guid reservationId)
    {
        try { await service.CheckOutAsync(sejourId); TempData["Ok"] = "Check-out effectué."; }
        catch (InvalidOperationException ex) { TempData["Err"] = ex.Message; }
        return RedirectToAction(nameof(Detail), new { id = reservationId });
    }

    private async Task RemplirListes(DateOnly a, DateOnly d)
    {
        ViewBag.Clients = await Db.Clients.OrderBy(c => c.NomComplet).ToListAsync();
        ViewBag.ChambresDispo = await service.ChambresDisponiblesAsync(a, d);
    }
}
