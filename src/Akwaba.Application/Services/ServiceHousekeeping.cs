using Akwaba.Application.Interfaces;
using Akwaba.Domain.Common;
using Akwaba.Domain.Entites;
using Akwaba.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Akwaba.Application.Services;

/// <summary>Suivi d'entretien des chambres : affectation et changement d'état.</summary>
public class ServiceHousekeeping(IAkwabaDbContext db, IServiceAudit audit)
{
    /// <summary>Chambres groupées par étage avec leur état de ménage et l'affectation en cours.</summary>
    public async Task<List<Chambre>> ChambresAsync(CancellationToken ct = default) =>
        await db.Chambres.Include(c => c.TypeChambre)
            .OrderBy(c => c.Etage).ThenBy(c => c.Numero).ToListAsync(ct);

    public async Task<Dictionary<Guid, TacheMenage>> TachesActivesAsync(CancellationToken ct = default)
    {
        var taches = await db.TachesMenage
            .Where(t => t.TermineeLe == null)
            .ToListAsync(ct);
        return taches.GroupBy(t => t.ChambreId).ToDictionary(g => g.Key, g => g.OrderByDescending(t => t.CreeLe).First());
    }

    public async Task AssignerAsync(Guid chambreId, Guid assigneeId, string nom, CancellationToken ct = default)
    {
        var chambre = await db.Chambres.FirstOrDefaultAsync(c => c.Id == chambreId, ct)
            ?? throw new InvalidOperationException("Chambre introuvable.");
        db.TachesMenage.Add(new TacheMenage
        {
            ChambreId = chambreId, AssigneeId = assigneeId, AssigneeNom = nom,
            Statut = StatutMenage.EnCours
        });
        chambre.StatutMenage = StatutMenage.EnCours;
        await db.SaveChangesAsync(ct);
        await audit.TracerAsync("MENAGE_ASSIGNATION", chambre.Numero, nom, ct);
    }

    public async Task MarquerAsync(Guid chambreId, StatutMenage statut, CancellationToken ct = default)
    {
        var chambre = await db.Chambres.FirstOrDefaultAsync(c => c.Id == chambreId, ct)
            ?? throw new InvalidOperationException("Chambre introuvable.");
        chambre.StatutMenage = statut;

        var tache = await db.TachesMenage
            .Where(t => t.ChambreId == chambreId && t.TermineeLe == null)
            .OrderByDescending(t => t.CreeLe).FirstOrDefaultAsync(ct);
        if (tache is not null)
        {
            tache.Statut = statut;
            if (statut is StatutMenage.Propre or StatutMenage.HorsService) tache.TermineeLe = DateTime.UtcNow;
        }
        await db.SaveChangesAsync(ct);
        await audit.TracerAsync("MENAGE_ETAT", chambre.Numero, statut.ToString(), ct);
    }
}
