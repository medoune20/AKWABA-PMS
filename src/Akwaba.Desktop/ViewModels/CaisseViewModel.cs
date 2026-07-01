using System.Collections.ObjectModel;
using Akwaba.Desktop.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Akwaba.Desktop.ViewModels;

public partial class CaisseViewModel : ObservableObject
{
    private readonly ServiceCaisseLocale _caisse;
    private Guid _sessionId;

    public CaisseViewModel(ServiceCaisseLocale caisse)
    {
        _caisse = caisse;
        OuvrirCommand = new AsyncRelayCommand(OuvrirAsync, () => !SessionOuverte);
        FermerCommand = new AsyncRelayCommand(FermerAsync, () => SessionOuverte);
    }

    public ObservableCollection<LignePaiementVue> Paiements { get; } = new();

    [ObservableProperty] private bool sessionOuverte;
    [ObservableProperty] private string entete = "Aucune session ouverte";
    [ObservableProperty] private int fondsInitial;
    [ObservableProperty] private int montantCompte;
    [ObservableProperty] private int totalEspeces;
    [ObservableProperty] private string message = "";

    public IAsyncRelayCommand OuvrirCommand { get; }
    public IAsyncRelayCommand FermerCommand { get; }

    public async Task ChargerAsync()
    {
        var s = await _caisse.SessionOuverteAsync();
        SessionOuverte = s is not null;
        Paiements.Clear();
        if (s is null) { Entete = "Aucune session ouverte"; TotalEspeces = 0; }
        else
        {
            _sessionId = s.Id;
            Entete = $"Session ouverte le {s.OuverteLe.ToLocalTime():dd/MM HH:mm} — fonds {s.FondsFcfa:N0} FCFA";
            TotalEspeces = await _caisse.TotalEspecesAsync(s.Id);
            foreach (var p in await _caisse.PaiementsSessionAsync(s.Id))
                Paiements.Add(new LignePaiementVue(p.Moyen.ToString(), p.MontantFcfa, p.CreeLe));
        }
        OuvrirCommand.NotifyCanExecuteChanged();
        FermerCommand.NotifyCanExecuteChanged();
    }

    private async Task OuvrirAsync()
    {
        var s = await _caisse.OuvrirAsync(FondsInitial);
        _sessionId = s.Id; Message = "Session ouverte."; await ChargerAsync();
    }

    private async Task FermerAsync()
    {
        await _caisse.FermerAsync(_sessionId, MontantCompte);
        Message = $"Session clôturée. Écart = {(MontantCompte - (FondsInitial + TotalEspeces)):N0} FCFA (indicatif).";
        await ChargerAsync();
    }
}
