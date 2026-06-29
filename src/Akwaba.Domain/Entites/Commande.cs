using Akwaba.Domain.Common;

namespace Akwaba.Domain.Entites;

/// <summary>Commande restaurant/bar (table ou imputée à une chambre).</summary>
public class Commande : EntiteBase
{
    public int Numero { get; set; }
    public string? Table { get; set; }
    public Guid? SejourId { get; set; }   // si imputée à une note de chambre
    public Sejour? Sejour { get; set; }
    public StatutCommande Statut { get; set; } = StatutCommande.Ouverte;

    public ICollection<LigneCommande> Lignes { get; set; } = new List<LigneCommande>();
    public int TotalFcfa => Lignes.Sum(l => l.MontantFcfa);
}
