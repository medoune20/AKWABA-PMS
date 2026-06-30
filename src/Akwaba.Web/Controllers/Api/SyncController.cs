using System.Text.Json;
using Akwaba.Domain.Entites;
using Akwaba.Domain.Interfaces;
using Akwaba.Infrastructure.Persistence;
using Akwaba.Sync;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Akwaba.Web.Controllers.Api;

[ApiController]
[Route("api/sync")]
[Authorize(AuthenticationSchemes = "Bearer")]
public class SyncController(AkwabaDbContext db, ITenantContext tenant) : ControllerBase
{
    private static readonly JsonSerializerOptions J = new();

    [HttpGet("ping"), AllowAnonymous]
    public IActionResult Ping() => Ok(new { ok = true, utc = DateTime.UtcNow });

    /// <summary>Renvoie tous les enregistrements modifiés depuis l'horodatage fourni (curseur delta).</summary>
    [HttpGet("pull")]
    public async Task<ActionResult<PullResponse>> Pull([FromQuery] DateTime depuis)
    {
        var maintenant = DateTime.UtcNow;
        var els = new List<ElementSync>();
        void Ajouter(string entite, object dto, DateTime maj) =>
            els.Add(new ElementSync(entite, JsonSerializer.Serialize(dto, J), false, maj));

        foreach (var c in await db.Clients.Where(x => x.ModifieLe > depuis).ToListAsync())
            Ajouter("Client", Map(c), c.ModifieLe);
        foreach (var r in await db.Reservations.Where(x => x.ModifieLe > depuis).ToListAsync())
            Ajouter("Reservation", Map(r), r.ModifieLe);
        foreach (var s in await db.Sejours.Where(x => x.ModifieLe > depuis).ToListAsync())
            Ajouter("Sejour", Map(s), s.ModifieLe);
        foreach (var f in await db.Folios.Where(x => x.ModifieLe > depuis).ToListAsync())
            Ajouter("Folio", Map(f), f.ModifieLe);
        foreach (var l in await db.LignesFolio.Where(x => x.ModifieLe > depuis).ToListAsync())
            Ajouter("LigneFolio", Map(l), l.ModifieLe);
        foreach (var p in await db.Paiements.Where(x => x.ModifieLe > depuis).ToListAsync())
            Ajouter("Paiement", Map(p), p.ModifieLe);
        foreach (var sc in await db.SessionsCaisse.Where(x => x.ModifieLe > depuis).ToListAsync())
            Ajouter("SessionCaisse", Map(sc), sc.ModifieLe);
        // Référentiels (lecture seule côté poste)
        foreach (var tc in await db.TypesChambre.Where(x => x.ModifieLe > depuis).ToListAsync())
            Ajouter("TypeChambre", Map(tc), tc.ModifieLe);
        foreach (var ch in await db.Chambres.Where(x => x.ModifieLe > depuis).ToListAsync())
            Ajouter("Chambre", Map(ch), ch.ModifieLe);
        foreach (var ta in await db.Tarifs.Where(x => x.ModifieLe > depuis).ToListAsync())
            Ajouter("Tarif", Map(ta), ta.ModifieLe);

        return Ok(new PullResponse(maintenant, els));
    }

    /// <summary>Applique les opérations locales du poste (upsert idempotent, dernière écriture gagne).</summary>
    [HttpPost("push")]
    public async Task<ActionResult<PushResponse>> Push([FromBody] PushRequest req)
    {
        var tid = tenant.TenantId ?? throw new InvalidOperationException("Tenant absent du jeton.");
        var resultats = new List<PushResultat>();

        await db.DansTransactionAsync<object?>(async () =>
        {
            foreach (var op in req.Operations)
            {
                try
                {
                    await AppliquerAsync(op, tid);
                    resultats.Add(new PushResultat(op.Id, true, null));
                }
                catch (Exception ex)
                {
                    resultats.Add(new PushResultat(op.Id, false, ex.Message));
                }
            }
            await db.SaveChangesAsync();
            return null;
        });

        return Ok(new PushResponse(DateTime.UtcNow, resultats));
    }

    private async Task AppliquerAsync(OperationSync op, Guid tid)
    {
        switch (op.Entite)
        {
            case "Client":
            {
                var d = Des<ClientDto>(op.DonneesJson);
                var e = await db.Clients.FirstOrDefaultAsync(x => x.Id == d.Id);
                if (op.Supprime) { if (e is not null) db.Clients.Remove(e); return; }
                if (e is null) db.Clients.Add(new Client { Id = d.Id, TenantId = tid, NomComplet = d.NomComplet, Telephone = d.Telephone, Email = d.Email, TypePiece = d.TypePiece, NumPiece = d.NumPiece });
                else if (op.ModifieLe >= e.ModifieLe) { e.NomComplet = d.NomComplet; e.Telephone = d.Telephone; e.Email = d.Email; e.TypePiece = d.TypePiece; e.NumPiece = d.NumPiece; }
                break;
            }
            case "Reservation":
            {
                var d = Des<ReservationDto>(op.DonneesJson);
                var e = await db.Reservations.FirstOrDefaultAsync(x => x.Id == d.Id);
                if (op.Supprime) { if (e is not null) db.Reservations.Remove(e); return; }
                if (e is null) db.Reservations.Add(new Reservation { Id = d.Id, TenantId = tid, ClientId = d.ClientId, Canal = d.Canal, Statut = (Domain.Common.StatutReservation)d.Statut });
                else if (op.ModifieLe >= e.ModifieLe) { e.Canal = d.Canal; e.Statut = (Domain.Common.StatutReservation)d.Statut; }
                break;
            }
            case "Sejour":
            {
                var d = Des<SejourDto>(op.DonneesJson);
                var e = await db.Sejours.FirstOrDefaultAsync(x => x.Id == d.Id);
                if (op.Supprime) { if (e is not null) db.Sejours.Remove(e); return; }
                if (e is null) db.Sejours.Add(new Sejour { Id = d.Id, TenantId = tid, ReservationId = d.ReservationId, ChambreId = d.ChambreId, DateArrivee = d.DateArrivee, DateDepart = d.DateDepart, NbAdultes = d.NbAdultes, PrixNuitFcfa = d.PrixNuitFcfa, Statut = (Domain.Common.StatutSejour)d.Statut });
                else if (op.ModifieLe >= e.ModifieLe) { e.DateArrivee = d.DateArrivee; e.DateDepart = d.DateDepart; e.NbAdultes = d.NbAdultes; e.PrixNuitFcfa = d.PrixNuitFcfa; e.Statut = (Domain.Common.StatutSejour)d.Statut; }
                break;
            }
            case "Folio":
            {
                var d = Des<FolioDto>(op.DonneesJson);
                var e = await db.Folios.FirstOrDefaultAsync(x => x.Id == d.Id);
                if (op.Supprime) { if (e is not null) db.Folios.Remove(e); return; }
                if (e is null) db.Folios.Add(new Folio { Id = d.Id, TenantId = tid, SejourId = d.SejourId, Statut = (Domain.Common.StatutFolio)d.Statut });
                else if (op.ModifieLe >= e.ModifieLe) e.Statut = (Domain.Common.StatutFolio)d.Statut;
                break;
            }
            case "LigneFolio":
            {
                var d = Des<LigneFolioDto>(op.DonneesJson);
                var e = await db.LignesFolio.FirstOrDefaultAsync(x => x.Id == d.Id);
                if (op.Supprime) { if (e is not null) db.LignesFolio.Remove(e); return; }
                if (e is null) db.LignesFolio.Add(new LigneFolio { Id = d.Id, TenantId = tid, FolioId = d.FolioId, Libelle = d.Libelle, Categorie = (Domain.Common.CategorieLigne)d.Categorie, Quantite = d.Quantite, MontantFcfa = d.MontantFcfa });
                else if (op.ModifieLe >= e.ModifieLe) { e.Libelle = d.Libelle; e.Quantite = d.Quantite; e.MontantFcfa = d.MontantFcfa; }
                break;
            }
            case "Paiement":
            {
                var d = Des<PaiementDto>(op.DonneesJson);
                var e = await db.Paiements.FirstOrDefaultAsync(x => x.Id == d.Id);
                // Idempotent : un paiement existant n'est jamais écrasé (fusion non destructive).
                if (e is null && !op.Supprime)
                    db.Paiements.Add(new Paiement { Id = d.Id, TenantId = tid, FolioId = d.FolioId, SessionId = d.SessionId, Moyen = (Domain.Common.MoyenPaiement)d.Moyen, MontantFcfa = d.MontantFcfa, RefExterne = d.RefExterne, Statut = (Domain.Common.StatutPaiement)d.Statut });
                break;
            }
            case "SessionCaisse":
            {
                var d = Des<SessionCaisseDto>(op.DonneesJson);
                var e = await db.SessionsCaisse.FirstOrDefaultAsync(x => x.Id == d.Id);
                if (e is null) db.SessionsCaisse.Add(new SessionCaisse { Id = d.Id, TenantId = tid, UtilisateurId = d.UtilisateurId, UtilisateurNom = d.UtilisateurNom, OuverteLe = d.OuverteLe, FermeeLe = d.FermeeLe, FondsFcfa = d.FondsFcfa, EcartFcfa = d.EcartFcfa });
                else if (op.ModifieLe >= e.ModifieLe) { e.FermeeLe = d.FermeeLe; e.EcartFcfa = d.EcartFcfa; }
                break;
            }
            default:
                throw new InvalidOperationException($"Entité non synchronisable : {op.Entite}");
        }
    }

    private static T Des<T>(string json) => JsonSerializer.Deserialize<T>(json, J)
        ?? throw new InvalidOperationException("Charge JSON invalide.");

    // ---------- entité -> DTO ----------
    private static ClientDto Map(Client c) => new(c.Id, c.TenantId, c.NomComplet, c.Telephone, c.Email, c.TypePiece, c.NumPiece, c.ModifieLe);
    private static ReservationDto Map(Reservation r) => new(r.Id, r.TenantId, r.ClientId, r.Canal, (int)r.Statut, r.ModifieLe);
    private static SejourDto Map(Sejour s) => new(s.Id, s.TenantId, s.ReservationId, s.ChambreId, s.DateArrivee, s.DateDepart, s.NbAdultes, s.PrixNuitFcfa, (int)s.Statut, s.ModifieLe);
    private static FolioDto Map(Folio f) => new(f.Id, f.TenantId, f.SejourId, (int)f.Statut, f.ModifieLe);
    private static LigneFolioDto Map(LigneFolio l) => new(l.Id, l.TenantId, l.FolioId, l.Libelle, (int)l.Categorie, l.Quantite, l.MontantFcfa, l.ModifieLe);
    private static PaiementDto Map(Paiement p) => new(p.Id, p.TenantId, p.FolioId, p.SessionId, (int)p.Moyen, p.MontantFcfa, p.RefExterne, (int)p.Statut, p.ModifieLe);
    private static SessionCaisseDto Map(SessionCaisse s) => new(s.Id, s.TenantId, s.UtilisateurId, s.UtilisateurNom, s.OuverteLe, s.FermeeLe, s.FondsFcfa, s.EcartFcfa, s.ModifieLe);
    private static TypeChambreDto Map(TypeChambre t) => new(t.Id, t.TenantId, t.Libelle, t.Capacite, t.Description, t.ModifieLe);
    private static ChambreDto Map(Chambre c) => new(c.Id, c.TenantId, c.TypeChambreId, c.Numero, c.Etage, (int)c.StatutMenage, c.Active, c.ModifieLe);
    private static TarifDto Map(Tarif t) => new(t.Id, t.TenantId, t.TypeChambreId, t.LibelleSaison, t.DateDebut, t.DateFin, t.PrixNuitFcfa, t.ModifieLe);
}
