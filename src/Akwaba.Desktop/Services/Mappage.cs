using System.Text.Json;
using Akwaba.Domain.Common;
using Akwaba.Domain.Entites;
using Akwaba.Desktop.Local;
using Akwaba.Sync;

namespace Akwaba.Desktop.Services;

/// <summary>Conversions entité du domaine ↔ DTO de synchronisation (mêmes formats que le serveur).</summary>
public static class Mappage
{
    private static readonly JsonSerializerOptions J = new();

    public static string VersJson(string entite, object e) => entite switch
    {
        "Client" => JsonSerializer.Serialize(Dto((Client)e), J),
        "Reservation" => JsonSerializer.Serialize(Dto((Reservation)e), J),
        "Sejour" => JsonSerializer.Serialize(Dto((Sejour)e), J),
        "Folio" => JsonSerializer.Serialize(Dto((Folio)e), J),
        "LigneFolio" => JsonSerializer.Serialize(Dto((LigneFolio)e), J),
        "Paiement" => JsonSerializer.Serialize(Dto((Paiement)e), J),
        "SessionCaisse" => JsonSerializer.Serialize(Dto((SessionCaisse)e), J),
        _ => throw new InvalidOperationException($"Entité non gérée : {entite}")
    };

    /// <summary>Applique un élément reçu du serveur dans la base locale (sans réenfiler dans l'outbox).</summary>
    public static void Appliquer(ContexteLocal db, ElementSync el)
    {
        switch (el.Entite)
        {
            case "Client": { var d = De<ClientDto>(el.DonneesJson); Upsert(db.Clients, d.Id, el.Supprime, () => new Client { Id = d.Id }, c => { c.TenantId = d.TenantId; c.NomComplet = d.NomComplet; c.Telephone = d.Telephone; c.Email = d.Email; c.TypePiece = d.TypePiece; c.NumPiece = d.NumPiece; c.ModifieLe = d.ModifieLe; }); break; }
            case "Reservation": { var d = De<ReservationDto>(el.DonneesJson); Upsert(db.Reservations, d.Id, el.Supprime, () => new Reservation { Id = d.Id }, x => { x.TenantId = d.TenantId; x.ClientId = d.ClientId; x.Canal = d.Canal; x.Statut = (StatutReservation)d.Statut; x.ModifieLe = d.ModifieLe; }); break; }
            case "Sejour": { var d = De<SejourDto>(el.DonneesJson); Upsert(db.Sejours, d.Id, el.Supprime, () => new Sejour { Id = d.Id }, x => { x.TenantId = d.TenantId; x.ReservationId = d.ReservationId; x.ChambreId = d.ChambreId; x.DateArrivee = d.DateArrivee; x.DateDepart = d.DateDepart; x.NbAdultes = d.NbAdultes; x.PrixNuitFcfa = d.PrixNuitFcfa; x.Statut = (StatutSejour)d.Statut; x.ModifieLe = d.ModifieLe; }); break; }
            case "Folio": { var d = De<FolioDto>(el.DonneesJson); Upsert(db.Folios, d.Id, el.Supprime, () => new Folio { Id = d.Id }, x => { x.TenantId = d.TenantId; x.SejourId = d.SejourId; x.Statut = (StatutFolio)d.Statut; x.ModifieLe = d.ModifieLe; }); break; }
            case "LigneFolio": { var d = De<LigneFolioDto>(el.DonneesJson); Upsert(db.LignesFolio, d.Id, el.Supprime, () => new LigneFolio { Id = d.Id }, x => { x.TenantId = d.TenantId; x.FolioId = d.FolioId; x.Libelle = d.Libelle; x.Categorie = (CategorieLigne)d.Categorie; x.Quantite = d.Quantite; x.MontantFcfa = d.MontantFcfa; x.ModifieLe = d.ModifieLe; }); break; }
            case "Paiement": { var d = De<PaiementDto>(el.DonneesJson); Upsert(db.Paiements, d.Id, el.Supprime, () => new Paiement { Id = d.Id }, x => { x.TenantId = d.TenantId; x.FolioId = d.FolioId; x.SessionId = d.SessionId; x.Moyen = (MoyenPaiement)d.Moyen; x.MontantFcfa = d.MontantFcfa; x.RefExterne = d.RefExterne; x.Statut = (StatutPaiement)d.Statut; x.ModifieLe = d.ModifieLe; }); break; }
            case "SessionCaisse": { var d = De<SessionCaisseDto>(el.DonneesJson); Upsert(db.SessionsCaisse, d.Id, el.Supprime, () => new SessionCaisse { Id = d.Id }, x => { x.TenantId = d.TenantId; x.UtilisateurId = d.UtilisateurId; x.UtilisateurNom = d.UtilisateurNom; x.OuverteLe = d.OuverteLe; x.FermeeLe = d.FermeeLe; x.FondsFcfa = d.FondsFcfa; x.EcartFcfa = d.EcartFcfa; x.ModifieLe = d.ModifieLe; }); break; }
            // Référentiels (lecture seule)
            case "TypeChambre": { var d = De<TypeChambreDto>(el.DonneesJson); Upsert(db.TypesChambre, d.Id, el.Supprime, () => new TypeChambre { Id = d.Id }, x => { x.TenantId = d.TenantId; x.Libelle = d.Libelle; x.Capacite = d.Capacite; x.Description = d.Description; x.ModifieLe = d.ModifieLe; }); break; }
            case "Chambre": { var d = De<ChambreDto>(el.DonneesJson); Upsert(db.Chambres, d.Id, el.Supprime, () => new Chambre { Id = d.Id }, x => { x.TenantId = d.TenantId; x.TypeChambreId = d.TypeChambreId; x.Numero = d.Numero; x.Etage = d.Etage; x.StatutMenage = (StatutMenage)d.StatutMenage; x.Active = d.Active; x.ModifieLe = d.ModifieLe; }); break; }
            case "Tarif": { var d = De<TarifDto>(el.DonneesJson); Upsert(db.Tarifs, d.Id, el.Supprime, () => new Tarif { Id = d.Id }, x => { x.TenantId = d.TenantId; x.TypeChambreId = d.TypeChambreId; x.LibelleSaison = d.LibelleSaison; x.DateDebut = d.DateDebut; x.DateFin = d.DateFin; x.PrixNuitFcfa = d.PrixNuitFcfa; x.ModifieLe = d.ModifieLe; }); break; }
        }
    }

    private static void Upsert<T>(Microsoft.EntityFrameworkCore.DbSet<T> set, Guid id, bool supprime,
        Func<T> creer, Action<T> remplir) where T : class
    {
        var e = set.Find(id);
        if (supprime) { if (e is not null) set.Remove(e); return; }
        if (e is null) { e = creer(); remplir(e); set.Add(e); }
        else remplir(e);
    }

    private static T De<T>(string json) => JsonSerializer.Deserialize<T>(json, J)!;

    // entité -> DTO
    private static ClientDto Dto(Client c) => new(c.Id, c.TenantId, c.NomComplet, c.Telephone, c.Email, c.TypePiece, c.NumPiece, c.ModifieLe);
    private static ReservationDto Dto(Reservation r) => new(r.Id, r.TenantId, r.ClientId, r.Canal, (int)r.Statut, r.ModifieLe);
    private static SejourDto Dto(Sejour s) => new(s.Id, s.TenantId, s.ReservationId, s.ChambreId, s.DateArrivee, s.DateDepart, s.NbAdultes, s.PrixNuitFcfa, (int)s.Statut, s.ModifieLe);
    private static FolioDto Dto(Folio f) => new(f.Id, f.TenantId, f.SejourId, (int)f.Statut, f.ModifieLe);
    private static LigneFolioDto Dto(LigneFolio l) => new(l.Id, l.TenantId, l.FolioId, l.Libelle, (int)l.Categorie, l.Quantite, l.MontantFcfa, l.ModifieLe);
    private static PaiementDto Dto(Paiement p) => new(p.Id, p.TenantId, p.FolioId, p.SessionId, (int)p.Moyen, p.MontantFcfa, p.RefExterne, (int)p.Statut, p.ModifieLe);
    private static SessionCaisseDto Dto(SessionCaisse s) => new(s.Id, s.TenantId, s.UtilisateurId, s.UtilisateurNom, s.OuverteLe, s.FermeeLe, s.FondsFcfa, s.EcartFcfa, s.ModifieLe);
}
