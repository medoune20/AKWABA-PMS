using Akwaba.Domain.Common;

namespace Akwaba.Desktop.ViewModels;

public record LigneSejourVue(Guid SejourId, Guid FolioId, string Client, string Chambre,
    DateOnly Arrivee, DateOnly Depart, StatutSejour Statut)
{
    public string StatutTexte => Statut switch
    {
        StatutSejour.Reserve => "Réservé",
        StatutSejour.EnCours => "En cours",
        StatutSejour.Termine => "Terminé",
        StatutSejour.Annule => "Annulé",
        _ => Statut.ToString()
    };
    public string Periode => $"{Arrivee:dd/MM} → {Depart:dd/MM}";
    public bool PeutCheckIn => Statut == StatutSejour.Reserve;
    public bool PeutCheckOut => Statut == StatutSejour.EnCours;
}

public record LigneNoteVue(string Libelle, string Categorie, int Quantite, int MontantFcfa)
{
    public string Montant => $"{MontantFcfa:N0} FCFA";
}

public record LignePaiementVue(string Moyen, int MontantFcfa, DateTime Le)
{
    public string Montant => $"{MontantFcfa:N0} FCFA";
    public string Heure => Le.ToLocalTime().ToString("dd/MM HH:mm");
}

public record ChambreVue(Guid Id, string Numero, string Type, int PrixIndicatif)
{
    public override string ToString() => $"Ch. {Numero} — {Type}";
}
