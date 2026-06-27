namespace Akwaba.Domain.Interfaces;

/// <summary>
/// Fournit l'hôtel (tenant) courant de la requête. Implémenté côté Web.
/// EstPlateforme = true pour un super-admin / modérateur (voit tous les tenants).
/// </summary>
public interface ITenantContext
{
    Guid? TenantId { get; }
    bool EstPlateforme { get; }
}
