using Akwaba.Domain.Common;

namespace Akwaba.Domain.Entites;

/// <summary>Encaissement rattaché à une note et à une session de caisse.</summary>
public class Paiement : EntiteBase
{
    public Guid? FolioId { get; set; }
    public Folio? Folio { get; set; }
    public Guid? SessionId { get; set; }
    public SessionCaisse? Session { get; set; }
    public MoyenPaiement Moyen { get; set; }
    public int MontantFcfa { get; set; }
    public string? RefExterne { get; set; }     // réf. transaction CinetPay (idempotence)
    public StatutPaiement Statut { get; set; } = StatutPaiement.Confirme;
}
