using Akwaba.Application.Dtos;
using Akwaba.Application.Interfaces;
using Akwaba.Domain.Common;
using Akwaba.Domain.Entites;
using Akwaba.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Akwaba.Application.Services;

/// <summary>Cycle de vie d'une réservation : disponibilité, création, check-in, check-out.</summary>
public class ServiceReservation(IAkwabaDbContext db, IServiceAudit audit)
{
    /// <summary>Chambres disponibles sur une plage (aucun séjour non annulé en chevauchement).</summary>
    public async Task<List<Chambre>> ChambresDisponiblesAsync(DateOnly arrivee, DateOnly depart, CancellationToken ct = default)
    {
        if (depart <= arrivee)
            return new();

        var occupees = await db.Sejours
            .Where(s => s.Statut != StatutSejour.Annule
                     && s.DateArrivee < depart && s.DateDepart > arrivee)
            .Select(s => s.ChambreId)
            .ToListAsync(ct);

        return await db.Chambres
            .Include(c => c.TypeChambre)
            .Where(c => c.Active && !occupees.Contains(c.Id))
            .OrderBy(c => c.Numero)
            .ToListAsync(ct);
    }

    /// <summary>Tarif applicable à un type de chambre à une date (sinon 0).</summary>
    public async Task<int> PrixNuitAsync(Guid typeChambreId, DateOnly date, CancellationToken ct = default)
    {
        var tarif = await db.Tarifs
            .Where(t => t.TypeChambreId == typeChambreId && t.DateDebut <= date && t.DateFin >= date)
            .OrderByDescending(t => t.DateDebut)
            .FirstOrDefaultAsync(ct);
        return tarif?.PrixNuitFcfa ?? 0;
    }

    public async Task<Reservation> CreerAsync(CreationReservation cmd, CancellationToken ct = default)
    {
        return await db.DansTransactionAsync(async () =>
        {
            var chambre = await db.Chambres.Include(c => c.TypeChambre)
                .FirstOrDefaultAsync(c => c.Id == cmd.ChambreId, ct)
                ?? throw new InvalidOperationException("Chambre introuvable.");

            // Contrôle anti-surbooking
            var dispo = await ChambresDisponiblesAsync(cmd.Arrivee, cmd.Depart, ct);
            if (dispo.All(c => c.Id != cmd.ChambreId))
                throw new InvalidOperationException("La chambre n'est plus disponible sur ces dates.");

            var prix = await PrixNuitAsync(chambre.TypeChambreId, cmd.Arrivee, ct);

            var reservation = new Reservation { ClientId = cmd.ClientId, Canal = cmd.Canal };
            var sejour = new Sejour
            {
                Reservation = reservation,
                ChambreId = cmd.ChambreId,
                DateArrivee = cmd.Arrivee,
                DateDepart = cmd.Depart,
                NbAdultes = cmd.NbAdultes,
                PrixNuitFcfa = prix,
                Statut = StatutSejour.Reserve
            };
            reservation.Sejours.Add(sejour);
            db.Reservations.Add(reservation);
            await db.SaveChangesAsync(ct);

            await audit.TracerAsync("CREER_RESERVATION", reservation.Id.ToString(),
                $"{chambre.Numero} du {cmd.Arrivee} au {cmd.Depart}", ct);
            return reservation;
        }, ct);
    }

    /// <summary>Check-in : ouvre la note et y inscrit l'hébergement (prix × nuits).</summary>
    public async Task CheckInAsync(Guid sejourId, CancellationToken ct = default)
    {
        await db.DansTransactionAsync<object?>(async () =>
        {
            var sejour = await db.Sejours.Include(s => s.Chambre)
                .FirstOrDefaultAsync(s => s.Id == sejourId, ct)
                ?? throw new InvalidOperationException("Séjour introuvable.");
            if (sejour.Statut != StatutSejour.Reserve)
                throw new InvalidOperationException("Séjour déjà traité.");

            sejour.Statut = StatutSejour.EnCours;
            if (sejour.Chambre is not null) sejour.Chambre.StatutMenage = StatutMenage.Sale;

            var folio = new Folio { SejourId = sejour.Id, Statut = StatutFolio.Ouvert };
            folio.Lignes.Add(new LigneFolio
            {
                Folio = folio,
                Libelle = $"Hébergement {sejour.NombreNuits} nuit(s)",
                Categorie = CategorieLigne.Hebergement,
                Quantite = sejour.NombreNuits,
                MontantFcfa = sejour.PrixNuitFcfa * sejour.NombreNuits
            });
            db.Folios.Add(folio);
            await db.SaveChangesAsync(ct);

            await audit.TracerAsync("CHECK_IN", sejour.Id.ToString(), sejour.Chambre?.Numero, ct);
            return null;
        }, ct);
    }

    /// <summary>Check-out : exige une note soldée, clôt le séjour et libère la chambre.</summary>
    public async Task CheckOutAsync(Guid sejourId, CancellationToken ct = default)
    {
        await db.DansTransactionAsync<object?>(async () =>
        {
            var sejour = await db.Sejours
                .Include(s => s.Chambre)
                .Include(s => s.Folio!).ThenInclude(f => f.Lignes)
                .Include(s => s.Folio!).ThenInclude(f => f.Paiements)
                .FirstOrDefaultAsync(s => s.Id == sejourId, ct)
                ?? throw new InvalidOperationException("Séjour introuvable.");
            if (sejour.Statut != StatutSejour.EnCours)
                throw new InvalidOperationException("Le séjour n'est pas en cours.");
            if (sejour.Folio is null || sejour.Folio.ResteFcfa > 0)
                throw new InvalidOperationException("La note doit être soldée avant le départ.");

            sejour.Statut = StatutSejour.Termine;
            sejour.Folio.Statut = StatutFolio.Solde;
            if (sejour.Chambre is not null) sejour.Chambre.StatutMenage = StatutMenage.Sale;
            await db.SaveChangesAsync(ct);

            await audit.TracerAsync("CHECK_OUT", sejour.Id.ToString(), sejour.Chambre?.Numero, ct);
            return null;
        }, ct);
    }
}
