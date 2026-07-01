using Akwaba.Domain.Common;
using Akwaba.Desktop.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.EntityFrameworkCore;

namespace Akwaba.Desktop.ViewModels;

public partial class TableauBordViewModel(DepotLocal depot) : ObservableObject
{
    [ObservableProperty] private int nbChambres;
    [ObservableProperty] private int nbOccupees;
    [ObservableProperty] private int nbArrivees;
    [ObservableProperty] private int nbDeparts;
    [ObservableProperty] private string tauxOccupation = "0 %";

    public async Task ChargerAsync()
    {
        var auj = DateOnly.FromDateTime(DateTime.Now);
        NbChambres = await depot.Db.Chambres.CountAsync(c => c.Active);
        NbOccupees = await depot.Db.Sejours.CountAsync(s =>
            s.Statut == StatutSejour.EnCours && s.DateArrivee <= auj && s.DateDepart > auj);
        NbArrivees = await depot.Db.Sejours.CountAsync(s => s.Statut == StatutSejour.Reserve && s.DateArrivee == auj);
        NbDeparts = await depot.Db.Sejours.CountAsync(s => s.Statut == StatutSejour.EnCours && s.DateDepart == auj);
        TauxOccupation = NbChambres > 0 ? $"{100 * NbOccupees / NbChambres} %" : "—";
    }
}
