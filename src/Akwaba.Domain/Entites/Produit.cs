using Akwaba.Domain.Common;

namespace Akwaba.Domain.Entites;

/// <summary>Article vendu au restaurant / bar.</summary>
public class Produit : EntiteBase
{
    public Guid CategorieProduitId { get; set; }
    public CategorieProduit? Categorie { get; set; }
    public string Libelle { get; set; } = string.Empty;
    public int PrixFcfa { get; set; }
    public bool Actif { get; set; } = true;
}
