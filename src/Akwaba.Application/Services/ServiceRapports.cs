using System.Globalization;
using System.Text;
using Akwaba.Application.Interfaces;
using Akwaba.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace Akwaba.Application.Services;

public record LigneRevenu(string Categorie, long MontantFcfa);
public record RevenuParMoyen(MoyenPaiement Moyen, long MontantFcfa);

/// <summary>Rapports comptables simplifiés : revenus par catégorie, encaissements par moyen, exports CSV.</summary>
public class ServiceRapports(IAkwabaDbContext db)
{
    public async Task<List<LigneRevenu>> RevenusParCategorieAsync(DateOnly du, DateOnly au, CancellationToken ct = default)
    {
        var debut = du.ToDateTime(TimeOnly.MinValue);
        var fin = au.ToDateTime(TimeOnly.MaxValue);
        var data = await db.LignesFolio
            .Where(l => l.CreeLe >= debut && l.CreeLe <= fin)
            .GroupBy(l => l.Categorie)
            .Select(g => new { g.Key, Total = g.Sum(x => (long)x.MontantFcfa) })
            .ToListAsync(ct);
        return data.Select(d => new LigneRevenu(d.Key.ToString(), d.Total)).OrderByDescending(d => d.MontantFcfa).ToList();
    }

    public async Task<List<RevenuParMoyen>> EncaissementsParMoyenAsync(DateOnly du, DateOnly au, CancellationToken ct = default)
    {
        var debut = du.ToDateTime(TimeOnly.MinValue);
        var fin = au.ToDateTime(TimeOnly.MaxValue);
        var data = await db.Paiements
            .Where(p => p.Statut == StatutPaiement.Confirme && p.CreeLe >= debut && p.CreeLe <= fin)
            .GroupBy(p => p.Moyen)
            .Select(g => new { g.Key, Total = g.Sum(x => (long)x.MontantFcfa) })
            .ToListAsync(ct);
        return data.Select(d => new RevenuParMoyen(d.Key, d.Total)).OrderByDescending(d => d.MontantFcfa).ToList();
    }

    public async Task<long> CaTotalAsync(DateOnly du, DateOnly au, CancellationToken ct = default)
    {
        var debut = du.ToDateTime(TimeOnly.MinValue);
        var fin = au.ToDateTime(TimeOnly.MaxValue);
        return await db.Paiements
            .Where(p => p.Statut == StatutPaiement.Confirme && p.CreeLe >= debut && p.CreeLe <= fin)
            .SumAsync(p => (long)p.MontantFcfa, ct);
    }

    /// <summary>Export CSV des encaissements confirmés sur la période.</summary>
    public async Task<string> ExportEncaissementsCsvAsync(DateOnly du, DateOnly au, CancellationToken ct = default)
    {
        var debut = du.ToDateTime(TimeOnly.MinValue);
        var fin = au.ToDateTime(TimeOnly.MaxValue);
        var paiements = await db.Paiements
            .Where(p => p.Statut == StatutPaiement.Confirme && p.CreeLe >= debut && p.CreeLe <= fin)
            .OrderBy(p => p.CreeLe).ToListAsync(ct);

        var sb = new StringBuilder();
        sb.AppendLine("Date;Moyen;MontantFCFA;Reference");
        foreach (var p in paiements)
            sb.AppendLine(string.Join(';',
                p.CreeLe.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture),
                p.Moyen, p.MontantFcfa, p.RefExterne ?? ""));
        return sb.ToString();
    }
}
