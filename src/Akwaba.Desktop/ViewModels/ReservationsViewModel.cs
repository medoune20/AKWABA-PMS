using System.Collections.ObjectModel;
using Akwaba.Domain.Common;
using Akwaba.Domain.Entites;
using Akwaba.Desktop.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;

namespace Akwaba.Desktop.ViewModels;

public partial class ReservationsViewModel : ObservableObject
{
    private readonly DepotLocal _depot;
    private readonly ServiceReceptionLocale _reception;

    public ReservationsViewModel(DepotLocal depot, ServiceReceptionLocale reception)
    {
        _depot = depot; _reception = reception;
        CheckInCommand = new AsyncRelayCommand<LigneSejourVue>(CheckInAsync);
        CheckOutCommand = new AsyncRelayCommand<LigneSejourVue>(CheckOutAsync);
        OuvrirNoteCommand = new RelayCommand<LigneSejourVue>(l => { if (l is not null) OuvrirDetailDemande?.Invoke(l.SejourId); });
        CreerCommand = new AsyncRelayCommand(CreerAsync, () => ChambreChoisie is not null && !string.IsNullOrWhiteSpace(NomClient));
    }

    /// <summary>Callback branché par le Shell pour ouvrir le détail d'un séjour.</summary>
    public Action<Guid>? OuvrirDetailDemande { get; set; }

    public ObservableCollection<LigneSejourVue> Sejours { get; } = new();
    public ObservableCollection<ChambreVue> Chambres { get; } = new();

    [ObservableProperty] private string message = "";
    [ObservableProperty] private bool formulaireVisible;

    // Formulaire nouvelle réservation
    [ObservableProperty] private string nomClient = "";
    [ObservableProperty] private string? telClient;
    [ObservableProperty] private ChambreVue? chambreChoisie;
    [ObservableProperty] private DateTime dateArrivee = DateTime.Today;
    [ObservableProperty] private DateTime dateDepart = DateTime.Today.AddDays(1);
    [ObservableProperty] private int prixNuit;

    public IAsyncRelayCommand<LigneSejourVue> CheckInCommand { get; }
    public IAsyncRelayCommand<LigneSejourVue> CheckOutCommand { get; }
    public IRelayCommand<LigneSejourVue> OuvrirNoteCommand { get; }
    public IAsyncRelayCommand CreerCommand { get; }

    partial void OnChambreChoisieChanged(ChambreVue? value) { if (value is not null) PrixNuit = value.PrixIndicatif; CreerCommand.NotifyCanExecuteChanged(); }
    partial void OnNomClientChanged(string value) => CreerCommand.NotifyCanExecuteChanged();

    public void BasculerFormulaire() => FormulaireVisible = !FormulaireVisible;

    public async Task ChargerAsync()
    {
        Sejours.Clear(); Chambres.Clear();

        var chambres = await _depot.Db.Chambres.Where(c => c.Active).ToListAsync();
        var types = await _depot.Db.TypesChambre.ToListAsync();
        foreach (var c in chambres.OrderBy(c => c.Numero))
        {
            var t = types.FirstOrDefault(t => t.Id == c.TypeChambreId);
            var prix = await _depot.Db.Tarifs.Where(x => x.TypeChambreId == c.TypeChambreId)
                .OrderByDescending(x => x.DateDebut).Select(x => x.PrixNuitFcfa).FirstOrDefaultAsync();
            Chambres.Add(new ChambreVue(c.Id, c.Numero, t?.Libelle ?? "—", prix));
        }

        var sejours = await _depot.Db.Sejours
            .Where(s => s.Statut == StatutSejour.Reserve || s.Statut == StatutSejour.EnCours)
            .OrderBy(s => s.DateArrivee).ToListAsync();
        foreach (var s in sejours)
        {
            var resa = await _depot.Db.Reservations.FirstOrDefaultAsync(r => r.Id == s.ReservationId);
            var client = resa is null ? null : await _depot.Db.Clients.FirstOrDefaultAsync(c => c.Id == resa.ClientId);
            var ch = chambres.FirstOrDefault(c => c.Id == s.ChambreId);
            var folio = await _depot.Db.Folios.FirstOrDefaultAsync(f => f.SejourId == s.Id);
            Sejours.Add(new LigneSejourVue(s.Id, folio?.Id ?? Guid.Empty,
                client?.NomComplet ?? "Client", ch?.Numero ?? "—", s.DateArrivee, s.DateDepart, s.Statut));
        }
    }

    private async Task CreerAsync()
    {
        try
        {
            await _reception.CreerReservationAsync(null, NomClient.Trim(), TelClient,
                ChambreChoisie!.Id, DateOnly.FromDateTime(DateArrivee), DateOnly.FromDateTime(DateDepart),
                1, PrixNuit);
            Message = "Réservation créée.";
            NomClient = ""; TelClient = null; ChambreChoisie = null; FormulaireVisible = false;
            await ChargerAsync();
        }
        catch (Exception ex) { Message = ex.Message; }
    }

    private async Task CheckInAsync(LigneSejourVue? l)
    {
        if (l is null) return;
        await _reception.CheckInAsync(l.SejourId); Message = "Arrivée enregistrée."; await ChargerAsync();
    }

    private async Task CheckOutAsync(LigneSejourVue? l)
    {
        if (l is null) return;
        await _reception.CheckOutAsync(l.SejourId); Message = "Départ enregistré."; await ChargerAsync();
    }
}
