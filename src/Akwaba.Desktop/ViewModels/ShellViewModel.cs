using System.Windows;
using Akwaba.Desktop.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Akwaba.Desktop.ViewModels;

public partial class ShellViewModel : ObservableObject
{
    private readonly ServiceAuth _auth;
    private readonly ServiceSync _sync;
    private readonly ServiceConnectivite _conn;
    private readonly DepotLocal _depot;
    private readonly SessionPoste _session;

    private readonly TableauBordViewModel _vmTableau;
    private readonly ReservationsViewModel _vmReservations;
    private readonly DetailSejourViewModel _vmDetail;
    private readonly CaisseViewModel _vmCaisse;

    public ShellViewModel(ServiceAuth auth, ServiceSync sync, ServiceConnectivite conn,
        DepotLocal depot, SessionPoste session,
        TableauBordViewModel vmTableau, ReservationsViewModel vmReservations,
        DetailSejourViewModel vmDetail, CaisseViewModel vmCaisse)
    {
        _auth = auth; _sync = sync; _conn = conn; _depot = depot; _session = session;
        _vmTableau = vmTableau; _vmReservations = vmReservations; _vmDetail = vmDetail; _vmCaisse = vmCaisse;

        _vmReservations.OuvrirDetailDemande = async id => await NaviguerDetailAsync(id);
        _vmDetail.RetourDemande = async () => await NaviguerReservationsAsync();

        EstEnLigne = _conn.EstEnLigne;
        MajStatut();
        _conn.EtatChange += en => Application.Current.Dispatcher.Invoke(() => { EstEnLigne = en; MajStatut(); });
        _sync.Journal += msg => Application.Current.Dispatcher.Invoke(() =>
            Journal = $"{DateTime.Now:HH:mm:ss}  {msg}\n" + Journal);

        SynchroniserCommand = new AsyncRelayCommand(SynchroniserAsync, () => EstEnLigne && EstConnecte);
        TableauCommand = new AsyncRelayCommand(NaviguerTableauAsync);
        ReservationsCommand = new AsyncRelayCommand(NaviguerReservationsAsync);
        CaisseCommand = new AsyncRelayCommand(NaviguerCaisseAsync);
        MajEnAttente();
    }

    [ObservableProperty] private bool estEnLigne;
    [ObservableProperty] private string statutConnectivite = "Hors ligne";
    [ObservableProperty] private bool estConnecte;
    [ObservableProperty] private string email = "";
    [ObservableProperty] private string nomComplet = "";
    [ObservableProperty] private string tenantNom = "";
    [ObservableProperty] private string messageErreur = "";
    [ObservableProperty] private int nbEnAttente;
    [ObservableProperty] private string journal = "";
    [ObservableProperty] private bool occupe;
    [ObservableProperty] private object? pageCourante;

    public IAsyncRelayCommand SynchroniserCommand { get; }
    public IAsyncRelayCommand TableauCommand { get; }
    public IAsyncRelayCommand ReservationsCommand { get; }
    public IAsyncRelayCommand CaisseCommand { get; }

    private string DeviceId()
    {
        var id = _depot.Param("DeviceId");
        if (string.IsNullOrEmpty(id))
        {
            id = Guid.NewGuid().ToString("N");
            _depot.DefinirParamAsync("DeviceId", id).GetAwaiter().GetResult();
        }
        return id;
    }

    public async Task ConnexionEnLigneAsync(string motDePasse)
    {
        MessageErreur = ""; Occupe = true;
        try
        {
            var (ok, msg) = await _auth.ConnexionEnLigneAsync(Email.Trim(), motDePasse, DeviceId());
            if (!ok) { MessageErreur = msg ?? "Échec de connexion."; return; }
            await ApresConnexionAsync();
            await SynchroniserAsync();
            await NaviguerTableauAsync();
        }
        finally { Occupe = false; }
    }

    public async Task DeverrouillerAsync(string motDePasse)
    {
        MessageErreur = ""; Occupe = true;
        try
        {
            var (ok, msg) = await _auth.DeverrouillerHorsLigneAsync(Email.Trim(), motDePasse);
            if (!ok) { MessageErreur = msg ?? "Échec."; return; }
            await ApresConnexionAsync();
            await NaviguerTableauAsync();
        }
        finally { Occupe = false; }
    }

    private async Task ApresConnexionAsync()
    {
        EstConnecte = _session.Connecte;
        NomComplet = _session.NomComplet;
        TenantNom = _session.TenantNom;
        SynchroniserCommand.NotifyCanExecuteChanged();
        MajEnAttente();
        await Task.CompletedTask;
    }

    public async Task SynchroniserAsync()
    {
        if (!EstEnLigne || !EstConnecte) return;
        Occupe = true;
        try
        {
            var (_, message) = await _sync.SynchroniserAsync();
            MessageErreur = message;
            MajEnAttente();
            if (PageCourante is TableauBordViewModel) await _vmTableau.ChargerAsync();
            if (PageCourante is ReservationsViewModel) await _vmReservations.ChargerAsync();
            if (PageCourante is CaisseViewModel) await _vmCaisse.ChargerAsync();
        }
        finally { Occupe = false; }
    }

    private async Task NaviguerTableauAsync() { await _vmTableau.ChargerAsync(); PageCourante = _vmTableau; }
    private async Task NaviguerReservationsAsync() { await _vmReservations.ChargerAsync(); PageCourante = _vmReservations; }
    private async Task NaviguerCaisseAsync() { await _vmCaisse.ChargerAsync(); PageCourante = _vmCaisse; }
    private async Task NaviguerDetailAsync(Guid sejourId) { await _vmDetail.ChargerAsync(sejourId); PageCourante = _vmDetail; }

    public void Deconnecter()
    {
        _session.Fermer();
        EstConnecte = false; NomComplet = ""; TenantNom = ""; PageCourante = null;
        SynchroniserCommand.NotifyCanExecuteChanged();
    }

    private void MajStatut()
    {
        StatutConnectivite = EstEnLigne ? "En ligne" : "Hors ligne";
        SynchroniserCommand?.NotifyCanExecuteChanged();
    }

    public void MajEnAttente() => NbEnAttente = _depot.NbEnAttente();
}
