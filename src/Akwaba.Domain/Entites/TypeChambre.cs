using Akwaba.Domain.Common;

namespace Akwaba.Domain.Entites;

/// <summary>Catégorie de chambres (Standard, Deluxe, Suite...).</summary>
public class TypeChambre : EntiteBase
{
    public string Libelle { get; set; } = string.Empty;
    public short Capacite { get; set; } = 2;
    public string? Description { get; set; }

    public ICollection<Chambre> Chambres { get; set; } = new List<Chambre>();
    public ICollection<Tarif> Tarifs { get; set; } = new List<Tarif>();
}
