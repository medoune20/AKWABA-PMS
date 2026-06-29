using Akwaba.Domain.Common;

namespace Akwaba.Domain.Entites;

/// <summary>Affectation et suivi d'entretien d'une chambre (housekeeping).</summary>
public class TacheMenage : EntiteBase
{
    public Guid ChambreId { get; set; }
    public Chambre? Chambre { get; set; }
    public Guid? AssigneeId { get; set; }
    public string? AssigneeNom { get; set; }
    public StatutMenage Statut { get; set; } = StatutMenage.Sale;
    public string? Note { get; set; }
    public DateTime? TermineeLe { get; set; }
}
