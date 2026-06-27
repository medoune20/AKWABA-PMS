namespace Akwaba.Domain.Interfaces;

/// <summary>Journalisation des actions sensibles.</summary>
public interface IServiceAudit
{
    Task TracerAsync(string action, string? cible = null, string? details = null, CancellationToken ct = default);
}
