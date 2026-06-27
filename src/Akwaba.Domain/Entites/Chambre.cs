using Akwaba.Domain.Common;

namespace Akwaba.Domain.Entites;

/// <summary>Chambre physique d'un hôtel.</summary>
public class Chambre : EntiteBase
{
    public Guid TypeChambreId { get; set; }
    public TypeChambre? TypeChambre { get; set; }
    public string Numero { get; set; } = string.Empty;
    public short? Etage { get; set; }
    public StatutMenage StatutMenage { get; set; } = StatutMenage.Propre;
    public bool Active { get; set; } = true;
}
