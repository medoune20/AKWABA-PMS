using Akwaba.Domain.Common;

namespace Akwaba.Domain.Entites;

/// <summary>Catégorie de la carte (Boissons, Plats, Desserts…).</summary>
public class CategorieProduit : EntiteBase
{
    public string Libelle { get; set; } = string.Empty;
    public int Ordre { get; set; }

    public ICollection<Produit> Produits { get; set; } = new List<Produit>();
}
