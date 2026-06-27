using Akwaba.Application.Dtos;
using Akwaba.Application.Interfaces;
using Akwaba.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace Akwaba.Application.Services;

/// <summary>Calcul des indicateurs clés (occupation, CA, ADR, RevPAR).</summary>
public class ServiceTableauBord(IAkwabaDbContext db)
{
    public async Task<IndicateursTableauBord> CalculerAsync(CancellationToken ct = default)
    {
        var aujourdhui = DateOnly.FromDateTime(DateTime.UtcNow);
        var debutMois = new DateOnly(aujourdhui.Year, aujourdhui.Month, 1);

        var chambresTotal = await db.Chambres.CountAsync(c => c.Active, ct);

        var sejoursActifs = await db.Sejours
            .Where(s => s.Statut == StatutSejour.EnCours
                     || (s.Statut == StatutSejour.Reserve && s.DateArrivee <= aujourdhui && s.DateDepart > aujourdhui))
            .ToListAsync(ct);
        var occupees = sejoursActifs.Select(s => s.ChambreId).Distinct().Count();

        var arrivees = await db.Sejours.CountAsync(s => s.DateArrivee == aujourdhui && s.Statut != StatutSejour.Annule, ct);
        var departs = await db.Sejours.CountAsync(s => s.DateDepart == aujourdhui && s.Statut != StatutSejour.Annule, ct);
        var aNettoyer = await db.Chambres.CountAsync(c => c.Active && c.StatutMenage == StatutMenage.Sale, ct);

        var caJour = await db.Paiements
            .Where(p => p.Statut == StatutPaiement.Confirme && p.CreeLe >= aujourdhui.ToDateTime(TimeOnly.MinValue))
            .SumAsync(p => (long)p.MontantFcfa, ct);
        var caMois = await db.Paiements
            .Where(p => p.Statut == StatutPaiement.Confirme && p.CreeLe >= debutMois.ToDateTime(TimeOnly.MinValue))
            .SumAsync(p => (long)p.MontantFcfa, ct);

        double taux = chambresTotal == 0 ? 0 : Math.Round(100.0 * occupees / chambresTotal, 1);

        // ADR = revenu hébergement du mois / nuitées vendues ; RevPAR = revenu hébergement / chambres dispo
        var hebergementMois = await db.LignesFolio
            .Where(l => l.Categorie == CategorieLigne.Hebergement && l.CreeLe >= debutMois.ToDateTime(TimeOnly.MinValue))
            .SumAsync(l => (long)l.MontantFcfa, ct);
        var nuitsVendues = await db.LignesFolio
            .Where(l => l.Categorie == CategorieLigne.Hebergement && l.CreeLe >= debutMois.ToDateTime(TimeOnly.MinValue))
            .SumAsync(l => (long)l.Quantite, ct);
        int adr = nuitsVendues == 0 ? 0 : (int)(hebergementMois / nuitsVendues);
        int joursEcoules = Math.Max(1, aujourdhui.Day);
        int revpar = chambresTotal == 0 ? 0 : (int)(hebergementMois / ((long)chambresTotal * joursEcoules));

        return new IndicateursTableauBord(chambresTotal, occupees, taux, arrivees, departs,
            aNettoyer, caJour, caMois, adr, revpar);
    }
}
