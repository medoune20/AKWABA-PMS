using Akwaba.Application.Interfaces;
using Akwaba.Domain.Common;
using Akwaba.Domain.Entites;
using Akwaba.Domain.Interfaces;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Akwaba.Infrastructure.Persistence;

/// <summary>
/// Contexte EF Core : Identity + entités métier.
/// Isolation multi-tenant assurée par des filtres de requête globaux et le marquage
/// automatique du TenantId à l'insertion.
/// </summary>
public class AkwabaDbContext(DbContextOptions<AkwabaDbContext> options, ITenantContext tenantContext)
    : IdentityDbContext<AppliUtilisateur, AppliRole, Guid>(options), IAkwabaDbContext
{
    // Exposés pour les filtres globaux (réévalués par requête via paramétrage EF).
    public Guid? TenantIdCourant => tenantContext.TenantId;
    public bool BypassTenant => tenantContext.EstPlateforme;

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<Abonnement> Abonnements => Set<Abonnement>();
    public DbSet<TypeChambre> TypesChambre => Set<TypeChambre>();
    public DbSet<Chambre> Chambres => Set<Chambre>();
    public DbSet<Tarif> Tarifs => Set<Tarif>();
    public DbSet<Client> Clients => Set<Client>();
    public DbSet<Reservation> Reservations => Set<Reservation>();
    public DbSet<Sejour> Sejours => Set<Sejour>();
    public DbSet<Folio> Folios => Set<Folio>();
    public DbSet<LigneFolio> LignesFolio => Set<LigneFolio>();
    public DbSet<Paiement> Paiements => Set<Paiement>();
    public DbSet<SessionCaisse> SessionsCaisse => Set<SessionCaisse>();
    public DbSet<DonneeReference> DonneesReference => Set<DonneeReference>();
    public DbSet<CategorieProduit> CategoriesProduit => Set<CategorieProduit>();
    public DbSet<Produit> Produits => Set<Produit>();
    public DbSet<Commande> Commandes => Set<Commande>();
    public DbSet<LigneCommande> LignesCommande => Set<LigneCommande>();
    public DbSet<TacheMenage> TachesMenage => Set<TacheMenage>();
    public DbSet<Audit> Audits => Set<Audit>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);

        // ---- Filtres globaux multi-tenant ----
        b.Entity<Abonnement>().HasQueryFilter(e => BypassTenant || e.TenantId == TenantIdCourant);
        b.Entity<TypeChambre>().HasQueryFilter(e => BypassTenant || e.TenantId == TenantIdCourant);
        b.Entity<Chambre>().HasQueryFilter(e => BypassTenant || e.TenantId == TenantIdCourant);
        b.Entity<Tarif>().HasQueryFilter(e => BypassTenant || e.TenantId == TenantIdCourant);
        b.Entity<Client>().HasQueryFilter(e => BypassTenant || e.TenantId == TenantIdCourant);
        b.Entity<Reservation>().HasQueryFilter(e => BypassTenant || e.TenantId == TenantIdCourant);
        b.Entity<Sejour>().HasQueryFilter(e => BypassTenant || e.TenantId == TenantIdCourant);
        b.Entity<Folio>().HasQueryFilter(e => BypassTenant || e.TenantId == TenantIdCourant);
        b.Entity<LigneFolio>().HasQueryFilter(e => BypassTenant || e.TenantId == TenantIdCourant);
        b.Entity<Paiement>().HasQueryFilter(e => BypassTenant || e.TenantId == TenantIdCourant);
        b.Entity<SessionCaisse>().HasQueryFilter(e => BypassTenant || e.TenantId == TenantIdCourant);
        b.Entity<CategorieProduit>().HasQueryFilter(e => BypassTenant || e.TenantId == TenantIdCourant);
        b.Entity<Produit>().HasQueryFilter(e => BypassTenant || e.TenantId == TenantIdCourant);
        b.Entity<Commande>().HasQueryFilter(e => BypassTenant || e.TenantId == TenantIdCourant);
        b.Entity<LigneCommande>().HasQueryFilter(e => BypassTenant || e.TenantId == TenantIdCourant);
        b.Entity<TacheMenage>().HasQueryFilter(e => BypassTenant || e.TenantId == TenantIdCourant);

        // ---- Index TenantId (performance par hôtel) ----
        b.Entity<Chambre>().HasIndex(e => e.TenantId);
        b.Entity<Sejour>().HasIndex(e => new { e.ChambreId, e.DateArrivee, e.DateDepart });
        b.Entity<Tenant>().HasIndex(e => e.SousDomaine).IsUnique();
        b.Entity<Chambre>().HasIndex(e => new { e.TenantId, e.Numero }).IsUnique();
        b.Entity<Paiement>().HasIndex(e => new { e.TenantId, e.RefExterne }).IsUnique()
            .HasFilter("RefExterne IS NOT NULL");

        // ---- Relations clés ----
        b.Entity<Sejour>().HasOne(s => s.Folio).WithOne(f => f.Sejour!).HasForeignKey<Folio>(f => f.SejourId);
        b.Entity<Audit>().HasKey(a => a.Id);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        // Marquage automatique du TenantId à l'insertion (anti-oubli).
        if (TenantIdCourant is Guid tid)
        {
            foreach (var entree in ChangeTracker.Entries<EntiteBase>())
                if (entree.State == EntityState.Added && entree.Entity.TenantId == Guid.Empty)
                    entree.Entity.TenantId = tid;
        }
        // Horodatage de modification pour la synchronisation delta
        foreach (var entree in ChangeTracker.Entries<EntiteBase>())
            if (entree.State is EntityState.Added or EntityState.Modified)
                entree.Entity.ModifieLe = DateTime.UtcNow;
        return await base.SaveChangesAsync(ct);
    }


    public async Task<T> DansTransactionAsync<T>(Func<Task<T>> action, CancellationToken ct = default)
    {
        if (Database.CurrentTransaction is not null)
            return await action();   // déjà dans une transaction

        await using var tx = await Database.BeginTransactionAsync(ct);
        try
        {
            var resultat = await action();
            await tx.CommitAsync(ct);
            return resultat;
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }
}
