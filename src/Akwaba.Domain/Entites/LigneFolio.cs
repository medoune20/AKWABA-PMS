using Akwaba.Domain.Common;

namespace Akwaba.Domain.Entites;

/// <summary>Ligne de la note de chambre.</summary>
public class LigneFolio : EntiteBase
{
    public Guid FolioId { get; set; }
    public Folio? Folio { get; set; }
    public string Libelle { get; set; } = string.Empty;
    public CategorieLigne Categorie { get; set; } = CategorieLigne.Extra;
    public int Quantite { get; set; } = 1;
    public int MontantFcfa { get; set; }
}
