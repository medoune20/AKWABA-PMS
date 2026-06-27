namespace Akwaba.Domain.Entites;

/// <summary>Journal horodaté des actions sensibles.</summary>
public class Audit
{
    public long Id { get; set; }
    public Guid? TenantId { get; set; }
    public Guid? UtilisateurId { get; set; }
    public string UtilisateurNom { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string? Cible { get; set; }
    public string? Details { get; set; }
    public DateTime Horodatage { get; set; } = DateTime.UtcNow;
}
