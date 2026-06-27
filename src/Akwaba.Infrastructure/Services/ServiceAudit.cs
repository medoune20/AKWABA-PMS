using Akwaba.Domain.Entites;
using Akwaba.Domain.Interfaces;
using Akwaba.Infrastructure.Persistence;

namespace Akwaba.Infrastructure.Services;

/// <summary>Écrit les actions sensibles dans le journal d'audit.</summary>
public class ServiceAudit(AkwabaDbContext db, ITenantContext tenant, IUtilisateurCourant utilisateur) : IServiceAudit
{
    public async Task TracerAsync(string action, string? cible = null, string? details = null, CancellationToken ct = default)
    {
        db.Audits.Add(new Audit
        {
            TenantId = tenant.TenantId,
            UtilisateurId = utilisateur.Id,
            UtilisateurNom = utilisateur.Nom,
            Action = action,
            Cible = cible,
            Details = details
        });
        await db.SaveChangesAsync(ct);
    }
}
