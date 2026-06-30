namespace Akwaba.Domain.Common;

/// <summary>
/// Base commune à toutes les entités métier multi-tenant.
/// Porte l'identifiant, l'appartenance à un hôtel (TenantId) et l'horodatage de création.
/// </summary>
public abstract class EntiteBase
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Hôtel propriétaire de la donnée. Filtré automatiquement (voir DbContext).</summary>
    public Guid TenantId { get; set; }

    public DateTime CreeLe { get; set; } = DateTime.UtcNow;

    /// <summary>Horodatage UTC de dernière modification (curseur de synchronisation delta).</summary>
    public DateTime ModifieLe { get; set; } = DateTime.UtcNow;
}
