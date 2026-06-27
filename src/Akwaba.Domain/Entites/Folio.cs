using Akwaba.Domain.Common;

namespace Akwaba.Domain.Entites;

/// <summary>Note de chambre unique d'un séjour.</summary>
public class Folio : EntiteBase
{
    public Guid SejourId { get; set; }
    public Sejour? Sejour { get; set; }
    public StatutFolio Statut { get; set; } = StatutFolio.Ouvert;

    public ICollection<LigneFolio> Lignes { get; set; } = new List<LigneFolio>();
    public ICollection<Paiement> Paiements { get; set; } = new List<Paiement>();

    public int TotalFcfa => Lignes.Sum(l => l.MontantFcfa);
    public int TotalPayeFcfa => Paiements.Where(p => p.Statut == StatutPaiement.Confirme).Sum(p => p.MontantFcfa);
    public int ResteFcfa => TotalFcfa - TotalPayeFcfa;
}
