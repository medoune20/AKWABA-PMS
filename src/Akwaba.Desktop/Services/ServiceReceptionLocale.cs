using Akwaba.Domain.Common;
using Akwaba.Domain.Entites;
using Microsoft.EntityFrameworkCore;

namespace Akwaba.Desktop.Services;

/// <summary>Opérations de réception sur la base locale (création réservation, check-in/out, note).</summary>
public class ServiceReceptionLocale(DepotLocal depot, SessionPoste session)
{
    private ContexteLocalShortcut Db => new(depot);

    /// <summary>Crée client (si nouveau) + réservation + séjour + note, le tout enfilé pour synchro.</summary>
    public async Task<Sejour> CreerReservationAsync(Guid? clientExistantId, string nomClient, string? tel,
        Guid chambreId, DateOnly arrivee, DateOnly depart, short nbAdultes, int prixNuit, string canal = "DIRECT")
    {
        var tid = session.TenantId;
        Guid clientId;
        if (clientExistantId is Guid cid) clientId = cid;
        else
        {
            var client = new Client { Id = Guid.NewGuid(), TenantId = tid, NomComplet = nomClient, Telephone = tel };
            await depot.EnregistrerAsync("Client", client);
            clientId = client.Id;
        }

        var resa = new Reservation { Id = Guid.NewGuid(), TenantId = tid, ClientId = clientId, Canal = canal, Statut = StatutReservation.Confirmee };
        await depot.EnregistrerAsync("Reservation", resa);

        var sejour = new Sejour
        {
            Id = Guid.NewGuid(), TenantId = tid, ReservationId = resa.Id, ChambreId = chambreId,
            DateArrivee = arrivee, DateDepart = depart, NbAdultes = nbAdultes, PrixNuitFcfa = prixNuit,
            Statut = StatutSejour.Reserve
        };
        await depot.EnregistrerAsync("Sejour", sejour);

        var folio = new Folio { Id = Guid.NewGuid(), TenantId = tid, SejourId = sejour.Id, Statut = StatutFolio.Ouvert };
        await depot.EnregistrerAsync("Folio", folio);

        // Ligne d'hébergement initiale (nb nuits x prix)
        var nuits = Math.Max(1, depart.DayNumber - arrivee.DayNumber);
        await depot.EnregistrerAsync("LigneFolio", new LigneFolio
        {
            Id = Guid.NewGuid(), TenantId = tid, FolioId = folio.Id,
            Libelle = $"Hébergement ({nuits} nuit(s))", Categorie = CategorieLigne.Hebergement,
            Quantite = nuits, MontantFcfa = prixNuit * nuits
        });
        return sejour;
    }

    public async Task CheckInAsync(Guid sejourId)
    {
        var s = await Db.Sejours.FirstOrDefaultAsync(x => x.Id == sejourId) ?? throw new("Séjour introuvable.");
        s.Statut = StatutSejour.EnCours;
        await depot.EnregistrerAsync("Sejour", s);
    }

    public async Task CheckOutAsync(Guid sejourId)
    {
        var s = await Db.Sejours.FirstOrDefaultAsync(x => x.Id == sejourId) ?? throw new("Séjour introuvable.");
        s.Statut = StatutSejour.Termine;
        await depot.EnregistrerAsync("Sejour", s);

        var ch = await Db.Chambres.FirstOrDefaultAsync(x => x.Id == s.ChambreId);
        if (ch is not null) { ch.StatutMenage = StatutMenage.Sale; await depot.EnregistrerAsync("Chambre", ch); }
    }

    public async Task AjouterLigneAsync(Guid folioId, string libelle, CategorieLigne cat, int qte, int montantUnitaire)
    {
        await depot.EnregistrerAsync("LigneFolio", new LigneFolio
        {
            Id = Guid.NewGuid(), TenantId = session.TenantId, FolioId = folioId,
            Libelle = libelle, Categorie = cat, Quantite = qte, MontantFcfa = montantUnitaire * qte
        });
    }

    public async Task SolderFolioAsync(Guid folioId)
    {
        var f = await Db.Folios.FirstOrDefaultAsync(x => x.Id == folioId) ?? throw new("Note introuvable.");
        f.Statut = StatutFolio.Solde;
        await depot.EnregistrerAsync("Folio", f);
    }
}

/// <summary>Petit raccourci de lecture vers le contexte local.</summary>
public readonly struct ContexteLocalShortcut(DepotLocal depot)
{
    public IQueryable<Sejour> Sejours => depot.Db.Sejours;
    public IQueryable<Chambre> Chambres => depot.Db.Chambres;
    public IQueryable<Folio> Folios => depot.Db.Folios;
}
