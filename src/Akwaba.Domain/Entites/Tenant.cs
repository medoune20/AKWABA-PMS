using Akwaba.Domain.Common;

namespace Akwaba.Domain.Entites;

/// <summary>Un hôtel abonné à la plateforme. Unité d'isolation multi-tenant.</summary>
public class Tenant
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Nom { get; set; } = string.Empty;
    public string SousDomaine { get; set; } = string.Empty;
    public string Ville { get; set; } = "Abidjan";
    public string? Telephone { get; set; }
    public TimeOnly HeureCheckIn { get; set; } = new(14, 0);
    public TimeOnly HeureCheckOut { get; set; } = new(12, 0);
    public StatutTenant Statut { get; set; } = StatutTenant.EnAttente;
    public string? MotifDecision { get; set; }
    public DateTime CreeLe { get; set; } = DateTime.UtcNow;

    public ICollection<Abonnement> Abonnements { get; set; } = new List<Abonnement>();
}
