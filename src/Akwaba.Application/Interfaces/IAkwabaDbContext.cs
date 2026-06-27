using Akwaba.Domain.Entites;
using Microsoft.EntityFrameworkCore;

namespace Akwaba.Application.Interfaces;

/// <summary>Abstraction du contexte EF Core exposée à la couche Application.</summary>
public interface IAkwabaDbContext
{
    DbSet<Tenant> Tenants { get; }
    DbSet<Abonnement> Abonnements { get; }
    DbSet<TypeChambre> TypesChambre { get; }
    DbSet<Chambre> Chambres { get; }
    DbSet<Tarif> Tarifs { get; }
    DbSet<Client> Clients { get; }
    DbSet<Reservation> Reservations { get; }
    DbSet<Sejour> Sejours { get; }
    DbSet<Folio> Folios { get; }
    DbSet<LigneFolio> LignesFolio { get; }
    DbSet<Paiement> Paiements { get; }
    DbSet<SessionCaisse> SessionsCaisse { get; }
    DbSet<DonneeReference> DonneesReference { get; }

    Task<int> SaveChangesAsync(CancellationToken ct = default);

    /// <summary>Exécute une action dans une transaction (opérations critiques).</summary>
    Task<T> DansTransactionAsync<T>(Func<Task<T>> action, CancellationToken ct = default);
}
