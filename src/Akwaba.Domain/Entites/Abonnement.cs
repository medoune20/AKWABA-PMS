using Akwaba.Domain.Common;

namespace Akwaba.Domain.Entites;

/// <summary>Formule SaaS souscrite par un hôtel.</summary>
public class Abonnement : EntiteBase
{
    public string Plan { get; set; } = "STANDARD";
    public DateOnly Debut { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);
    public DateOnly? Fin { get; set; }
    public string StatutPaie { get; set; } = "A_JOUR";
    public int MontantMensuelFcfa { get; set; }
}
