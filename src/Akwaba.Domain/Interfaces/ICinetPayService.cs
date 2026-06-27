using Akwaba.Domain.Common;

namespace Akwaba.Domain.Interfaces;

public record DemandePaiement(Guid FolioId, int MontantFcfa, MoyenPaiement Moyen, string Description, string CleIdempotence);
public record ResultatPaiement(bool Succes, StatutPaiement Statut, string RefExterne, string? UrlPaiement, string? Message);

/// <summary>
/// Agrégateur de paiement mobile (Orange Money / MTN / Wave / Moov) via CinetPay.
/// Implémentation bac à sable fournie ; l'intégration réelle se branche sur la même interface.
/// </summary>
public interface ICinetPayService
{
    Task<ResultatPaiement> InitierAsync(DemandePaiement demande, CancellationToken ct = default);
    Task<ResultatPaiement> VerifierAsync(string refExterne, CancellationToken ct = default);
}
