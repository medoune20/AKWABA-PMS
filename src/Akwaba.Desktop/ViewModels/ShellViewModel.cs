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

    public ShellViewModel(ServiceAuth auth, ServiceSync sync, ServiceConnectivite conn,
        DepotLocal depot, SessionPoste session)
    {
        _auth = auth; _sync = sync; _conn = conn; _depot = depot; _session = session;

        EstEnLigne = _conn.EstEnLigne;
        MajStatut();
        _conn.EtatChange += en => Application.Current.Dispatcher.Invoke(() =>
        {
            EstEnLigne = en; MajStatut();
        });
        _sync.Journal += msg => Application.Current.Dispatcher.Invoke(() =>
            Journal = $"{DateTime.Now:HH:mm:ss}  {msg}\n" + Journal);

        SynchroniserCommand = new AsyncRelayCommand(SynchroniserAsync, () => EstEnLigne && EstConnecte);
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

    public IAsyncRelayCommand SynchroniserCommand { get; }

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
            ApresConnexion();
            await SynchroniserAsync();
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
            ApresConnexion();
        }
        finally { Occupe = false; }
    }

    private void ApresConnexion()
    {
        EstConnecte = _session.Connecte;
        NomComplet = _session.NomComplet;
        TenantNom = _session.TenantNom;
        SynchroniserCommand.NotifyCanExecuteChanged();
        MajEnAttente();
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
        }
        finally { Occupe = false; }
    }

    public void Deconnecter()
    {
        _session.Fermer();
        EstConnecte = false; NomComplet = ""; TenantNom = "";
        SynchroniserCommand.NotifyCanExecuteChanged();
    }

    private void MajStatut()
    {
        StatutConnectivite = EstEnLigne ? "En ligne" : "Hors ligne";
        SynchroniserCommand?.NotifyCanExecuteChanged();
    }

    public void MajEnAttente() => NbEnAttente = _depot.NbEnAttente();
}
