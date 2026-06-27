namespace Akwaba.Domain.Entites;

/// <summary>Référentiel paramétrable (taxes, modes de paiement, statuts...). TenantId null = global.</summary>
public class DonneeReference
{
    public int Id { get; set; }
    public Guid? TenantId { get; set; }
    public string Categorie { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Libelle { get; set; } = string.Empty;
    public string? Valeur { get; set; }
}
