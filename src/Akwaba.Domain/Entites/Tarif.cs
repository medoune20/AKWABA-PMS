using Akwaba.Domain.Common;

namespace Akwaba.Domain.Entites;

/// <summary>Prix d'un type de chambre sur une période (saison).</summary>
public class Tarif : EntiteBase
{
    public Guid TypeChambreId { get; set; }
    public TypeChambre? TypeChambre { get; set; }
    public string LibelleSaison { get; set; } = "Standard";
    public DateOnly DateDebut { get; set; }
    public DateOnly DateFin { get; set; }
    public int PrixNuitFcfa { get; set; }
}
