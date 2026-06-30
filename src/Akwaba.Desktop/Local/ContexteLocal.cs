using Akwaba.Domain.Entites;
using Microsoft.EntityFrameworkCore;

namespace Akwaba.Desktop.Local;

/// <summary>Base SQLite locale du poste. Stocke un sous-ensemble à plat des entités du domaine
/// (sans navigation) plus les tables de synchronisation.</summary>
public class ContexteLocal(DbContextOptions<ContexteLocal> options) : DbContext(options)
{
    public DbSet<Client> Clients => Set<Client>();
    public DbSet<Reservation> Reservations => Set<Reservation>();
    public DbSet<Sejour> Sejours => Set<Sejour>();
    public DbSet<Chambre> Chambres => Set<Chambre>();
    public DbSet<TypeChambre> TypesChambre => Set<TypeChambre>();
    public DbSet<Tarif> Tarifs => Set<Tarif>();
    public DbSet<Folio> Folios => Set<Folio>();
    public DbSet<LigneFolio> LignesFolio => Set<LigneFolio>();
    public DbSet<Paiement> Paiements => Set<Paiement>();
    public DbSet<SessionCaisse> SessionsCaisse => Set<SessionCaisse>();

    public DbSet<OperationLocale> Outbox => Set<OperationLocale>();
    public DbSet<ParametrePoste> Parametres => Set<ParametrePoste>();
    public DbSet<CompteCache> Comptes => Set<CompteCache>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        // On ne stocke que les colonnes scalaires : on ignore toutes les navigations.
        b.Entity<Client>().Ignore(x => x.Reservations);
        b.Entity<Reservation>(e => { e.Ignore(x => x.Client); e.Ignore(x => x.Sejours); });
        b.Entity<Sejour>(e => { e.Ignore(x => x.Reservation); e.Ignore(x => x.Chambre); e.Ignore(x => x.Folio); });
        b.Entity<Chambre>().Ignore(x => x.TypeChambre);
        b.Entity<TypeChambre>(e => { e.Ignore(x => x.Chambres); e.Ignore(x => x.Tarifs); });
        b.Entity<Tarif>().Ignore(x => x.TypeChambre);
        b.Entity<Folio>(e => { e.Ignore(x => x.Sejour); e.Ignore(x => x.Lignes); e.Ignore(x => x.Paiements); });
        b.Entity<LigneFolio>().Ignore(x => x.Folio);
        b.Entity<Paiement>(e => { e.Ignore(x => x.Folio); e.Ignore(x => x.Session); });
        b.Entity<SessionCaisse>().Ignore(x => x.Paiements);

        b.Entity<ParametrePoste>().HasKey(x => x.Cle);
        b.Entity<CompteCache>().HasKey(x => x.Email);
        base.OnModelCreating(b);
    }
}
