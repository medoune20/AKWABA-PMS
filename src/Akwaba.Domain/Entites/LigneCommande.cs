using Akwaba.Domain.Common;

namespace Akwaba.Domain.Entites;

/// <summary>Ligne d'une commande restaurant/bar.</summary>
public class LigneCommande : EntiteBase
{
    public Guid CommandeId { get; set; }
    public Commande? Commande { get; set; }
    public Guid ProduitId { get; set; }
    public string Libelle { get; set; } = string.Empty;
    public int PrixUnitaireFcfa { get; set; }
    public int Quantite { get; set; } = 1;
    public int MontantFcfa => PrixUnitaireFcfa * Quantite;
}
