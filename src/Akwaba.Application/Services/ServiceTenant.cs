using Akwaba.Application.Interfaces;
using Akwaba.Domain.Common;
using Akwaba.Domain.Entites;
using Akwaba.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Akwaba.Application.Services;

/// <summary>Souscription d'un hôtel et modération (approbation / refus / suspension).</summary>
public class ServiceTenant(IAkwabaDbContext db, IServiceAudit audit)
{
    public Task<List<Tenant>> DemandesEnAttenteAsync(CancellationToken ct = default) =>
        db.Tenants.Where(t => t.Statut == StatutTenant.EnAttente).OrderBy(t => t.CreeLe).ToListAsync(ct);

    public Task<List<Tenant>> TousAsync(CancellationToken ct = default) =>
        db.Tenants.OrderBy(t => t.Nom).ToListAsync(ct);

    /// <summary>Crée un hôtel en attente de validation (appelé à l'inscription).</summary>
    public async Task<Tenant> SouscrireAsync(string nom, string sousDomaine, string ville, string? tel, CancellationToken ct = default)
    {
        if (await db.Tenants.AnyAsync(t => t.SousDomaine == sousDomaine, ct))
            throw new InvalidOperationException("Ce sous-domaine est déjà pris.");

        var tenant = new Tenant
        {
            Nom = nom, SousDomaine = sousDomaine, Ville = ville,
            Telephone = tel, Statut = StatutTenant.EnAttente
        };
        db.Tenants.Add(tenant);
        await db.SaveChangesAsync(ct);
        return tenant;
    }

    public async Task ApprouverAsync(Guid tenantId, CancellationToken ct = default)
    {
        var t = await db.Tenants.FirstOrDefaultAsync(x => x.Id == tenantId, ct)
            ?? throw new InvalidOperationException("Hôtel introuvable.");
        t.Statut = StatutTenant.Approuve;
        t.MotifDecision = null;
        db.Abonnements.Add(new Abonnement { TenantId = t.Id, Plan = "STANDARD", MontantMensuelFcfa = 25000 });
        await db.SaveChangesAsync(ct);
        await audit.TracerAsync("APPROUVER_HOTEL", t.Id.ToString(), t.Nom, ct);
    }

    public async Task RefuserAsync(Guid tenantId, string motif, CancellationToken ct = default)
    {
        var t = await db.Tenants.FirstOrDefaultAsync(x => x.Id == tenantId, ct)
            ?? throw new InvalidOperationException("Hôtel introuvable.");
        t.Statut = StatutTenant.Refuse;
        t.MotifDecision = motif;
        await db.SaveChangesAsync(ct);
        await audit.TracerAsync("REFUSER_HOTEL", t.Id.ToString(), motif, ct);
    }

    public async Task BasculerSuspensionAsync(Guid tenantId, CancellationToken ct = default)
    {
        var t = await db.Tenants.FirstOrDefaultAsync(x => x.Id == tenantId, ct)
            ?? throw new InvalidOperationException("Hôtel introuvable.");
        t.Statut = t.Statut == StatutTenant.Suspendu ? StatutTenant.Approuve : StatutTenant.Suspendu;
        await db.SaveChangesAsync(ct);
        await audit.TracerAsync("BASCULER_SUSPENSION", t.Id.ToString(), t.Statut.ToString(), ct);
    }
}
