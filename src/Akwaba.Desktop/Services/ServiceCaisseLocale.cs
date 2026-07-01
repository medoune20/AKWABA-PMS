using Akwaba.Domain.Common;
using Akwaba.Domain.Entites;
using Microsoft.EntityFrameworkCore;

namespace Akwaba.Desktop.Services;

/// <summary>Caisse locale : session, encaissement (espèces/carte hors ligne), clôture.</summary>
public class ServiceCaisseLocale(DepotLocal depot, SessionPoste session)
{
    public Task<SessionCaisse?> SessionOuverteAsync() =>
        depot.Db.SessionsCaisse
            .Where(s => s.UtilisateurId == session.UtilisateurId && s.FermeeLe == null)
            .OrderByDescending(s => s.OuverteLe)
            .FirstOrDefaultAsync();

    public async Task<SessionCaisse> OuvrirAsync(int fondsFcfa)
    {
        var s = new SessionCaisse
        {
            Id = Guid.NewGuid(), TenantId = session.TenantId,
            UtilisateurId = session.UtilisateurId, UtilisateurNom = session.NomComplet,
            OuverteLe = DateTime.UtcNow, FondsFcfa = fondsFcfa
        };
        await depot.EnregistrerAsync("SessionCaisse", s);
        return s;
    }

    /// <summary>Encaisse un paiement. Hors ligne, seuls Espèces et Carte sont autorisés.</summary>
    public async Task<Paiement> EncaisserAsync(Guid? folioId, MoyenPaiement moyen, int montant, Guid sessionId,
        bool estEnLigne)
    {
        if (!estEnLigne && moyen is not (MoyenPaiement.Especes or MoyenPaiement.Carte))
            throw new InvalidOperationException("Le paiement mobile n'est possible qu'en ligne.");
        if (montant <= 0) throw new InvalidOperationException("Montant invalide.");

        var p = new Paiement
        {
            Id = Guid.NewGuid(), TenantId = session.TenantId, FolioId = folioId, SessionId = sessionId,
            Moyen = moyen, MontantFcfa = montant, Statut = StatutPaiement.Confirme
        };
        await depot.EnregistrerAsync("Paiement", p);
        return p;
    }

    public Task<List<Paiement>> PaiementsSessionAsync(Guid sessionId) =>
        depot.Db.Paiements.Where(p => p.SessionId == sessionId).OrderByDescending(p => p.CreeLe).ToListAsync();

    public async Task<int> TotalEspecesAsync(Guid sessionId) =>
        await depot.Db.Paiements
            .Where(p => p.SessionId == sessionId && p.Moyen == MoyenPaiement.Especes)
            .SumAsync(p => p.MontantFcfa);

    public async Task FermerAsync(Guid sessionId, int montantCompte)
    {
        var s = await depot.Db.SessionsCaisse.FirstOrDefaultAsync(x => x.Id == sessionId)
            ?? throw new("Session introuvable.");
        var especes = await TotalEspecesAsync(sessionId);
        s.FermeeLe = DateTime.UtcNow;
        s.EcartFcfa = montantCompte - (s.FondsFcfa + especes);
        await depot.EnregistrerAsync("SessionCaisse", s);
    }
}
