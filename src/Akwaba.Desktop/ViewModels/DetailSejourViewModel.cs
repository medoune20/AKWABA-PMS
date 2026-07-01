using System.Collections.ObjectModel;
using Akwaba.Domain.Common;
using Akwaba.Desktop.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;

namespace Akwaba.Desktop.ViewModels;

public partial class DetailSejourViewModel : ObservableObject
{
    private readonly DepotLocal _depot;
    private readonly ServiceReceptionLocale _reception;
    private readonly ServiceCaisseLocale _caisse;
    private readonly ServiceConnectivite _conn;
    private Guid _sejourId, _folioId;

    public DetailSejourViewModel(DepotLocal depot, ServiceReceptionLocale reception,
        ServiceCaisseLocale caisse, ServiceConnectivite conn)
    {
        _depot = depot; _reception = reception; _caisse = caisse; _conn = conn;
        AjouterLigneCommand = new AsyncRelayCommand(AjouterLigneAsync,
            () => !string.IsNullOrWhiteSpace(NouvLibelle) && NouvMontant > 0);
        EncaisserEspecesCommand = new AsyncRelayCommand(() => EncaisserAsync(MoyenPaiement.Especes), PeutEncaisser);
        EncaisserCarteCommand = new AsyncRelayCommand(() => EncaisserAsync(MoyenPaiement.Carte), PeutEncaisser);
        SolderCommand = new AsyncRelayCommand(SolderAsync, () => Solde <= 0);
    }

    public Action? RetourDemande { get; set; }
    public IRelayCommand RetourCommand => new RelayCommand(() => RetourDemande?.Invoke());

    public ObservableCollection<LigneNoteVue> Lignes { get; } = new();
    public ObservableCollection<LignePaiementVue> Paiements { get; } = new();

    [ObservableProperty] private string titre = "";
    [ObservableProperty] private int total;
    [ObservableProperty] private int paye;
    [ObservableProperty] private int solde;
    [ObservableProperty] private string message = "";

    [ObservableProperty] private string nouvLibelle = "";
    [ObservableProperty] private int nouvMontant;
    [ObservableProperty] private int montantEncaisse;

    public IAsyncRelayCommand AjouterLigneCommand { get; }
    public IAsyncRelayCommand EncaisserEspecesCommand { get; }
    public IAsyncRelayCommand EncaisserCarteCommand { get; }
    public IAsyncRelayCommand SolderCommand { get; }

    private bool PeutEncaisser() => Solde > 0 && MontantEncaisse > 0;

    partial void OnNouvLibelleChanged(string v) => AjouterLigneCommand.NotifyCanExecuteChanged();
    partial void OnNouvMontantChanged(int v) => AjouterLigneCommand.NotifyCanExecuteChanged();
    partial void OnMontantEncaisseChanged(int v) { EncaisserEspecesCommand.NotifyCanExecuteChanged(); EncaisserCarteCommand.NotifyCanExecuteChanged(); }

    public async Task ChargerAsync(Guid sejourId)
    {
        _sejourId = sejourId;
        var sejour = await _depot.Db.Sejours.FirstOrDefaultAsync(s => s.Id == sejourId);
        var folio = await _depot.Db.Folios.FirstOrDefaultAsync(f => f.SejourId == sejourId);
        if (sejour is null || folio is null) { Message = "Séjour/note introuvable."; return; }
        _folioId = folio.Id;

        var resa = await _depot.Db.Reservations.FirstOrDefaultAsync(r => r.Id == sejour.ReservationId);
        var client = resa is null ? null : await _depot.Db.Clients.FirstOrDefaultAsync(c => c.Id == resa.ClientId);
        var ch = await _depot.Db.Chambres.FirstOrDefaultAsync(c => c.Id == sejour.ChambreId);
        Titre = $"{client?.NomComplet ?? "Client"} — Ch. {ch?.Numero ?? "—"} ({sejour.DateArrivee:dd/MM} → {sejour.DateDepart:dd/MM})";

        Lignes.Clear();
        var lignes = await _depot.Db.LignesFolio.Where(l => l.FolioId == folio.Id).ToListAsync();
        foreach (var l in lignes)
            Lignes.Add(new LigneNoteVue(l.Libelle, l.Categorie.ToString(), l.Quantite, l.MontantFcfa));
        Total = lignes.Sum(l => l.MontantFcfa);

        Paiements.Clear();
        var pmts = await _depot.Db.Paiements.Where(p => p.FolioId == folio.Id).OrderBy(p => p.CreeLe).ToListAsync();
        foreach (var p in pmts)
            Paiements.Add(new LignePaiementVue(p.Moyen.ToString(), p.MontantFcfa, p.CreeLe));
        Paye = pmts.Sum(p => p.MontantFcfa);

        Solde = Total - Paye;
        MontantEncaisse = Solde > 0 ? Solde : 0;
        NotifierCommandes();
    }

    private async Task AjouterLigneAsync()
    {
        await _reception.AjouterLigneAsync(_folioId, NouvLibelle.Trim(), CategorieLigne.Extra, 1, NouvMontant);
        NouvLibelle = ""; NouvMontant = 0;
        await ChargerAsync(_sejourId);
    }

    private async Task EncaisserAsync(MoyenPaiement moyen)
    {
        try
        {
            var sess = await _caisse.SessionOuverteAsync();
            if (sess is null) { Message = "Ouvrez d'abord une session de caisse."; return; }
            await _caisse.EncaisserAsync(_folioId, moyen, MontantEncaisse, sess.Id, _conn.EstEnLigne);
            Message = "Encaissement enregistré.";
            await ChargerAsync(_sejourId);
        }
        catch (Exception ex) { Message = ex.Message; }
    }

    private async Task SolderAsync()
    {
        await _reception.SolderFolioAsync(_folioId);
        Message = "Note soldée.";
    }

    private void NotifierCommandes()
    {
        EncaisserEspecesCommand.NotifyCanExecuteChanged();
        EncaisserCarteCommand.NotifyCanExecuteChanged();
        SolderCommand.NotifyCanExecuteChanged();
    }
}
