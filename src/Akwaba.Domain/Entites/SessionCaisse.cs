using Akwaba.Domain.Common;

namespace Akwaba.Domain.Entites;

/// <summary>Session de caisse (ouverture/clôture par un caissier).</summary>
public class SessionCaisse : EntiteBase
{
    public Guid UtilisateurId { get; set; }
    public string UtilisateurNom { get; set; } = string.Empty;
    public DateTime OuverteLe { get; set; } = DateTime.UtcNow;
    public DateTime? FermeeLe { get; set; }
    public int FondsFcfa { get; set; }
    public int? EcartFcfa { get; set; }
    public bool Ouverte => FermeeLe == null;

    public ICollection<Paiement> Paiements { get; set; } = new List<Paiement>();
}
