using Akwaba.Application.Dtos;
using Akwaba.Application.Interfaces;
using Akwaba.Domain.Common;
using Akwaba.Domain.Entites;
using Akwaba.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Akwaba.Application.Services;

/// <summary>Gestion de la note de chambre et de l'encaissement (mobile via CinetPay ou espèces).</summary>
public class ServiceFolio(IAkwabaDbContext db, ICinetPayService cinetpay, IServiceAudit audit)
{
    public Task<Folio?> ChargerAsync(Guid folioId, CancellationToken ct = default) =>
        db.Folios
          .Include(f => f.Lignes)
          .Include(f => f.Paiements)
          .Include(f => f.Sejour!).ThenInclude(s => s.Chambre)
          .Include(f => f.Sejour!).ThenInclude(s => s.Reservation!).ThenInclude(r => r.Client)
          .FirstOrDefaultAsync(f => f.Id == folioId, ct);

    public async Task AjouterLigneAsync(Guid folioId, string libelle, CategorieLigne cat, int quantite, int montant, CancellationToken ct = default)
    {
        var folio = await db.Folios.FirstOrDefaultAsync(f => f.Id == folioId, ct)
            ?? throw new InvalidOperationException("Note introuvable.");
        if (folio.Statut != StatutFolio.Ouvert)
            throw new InvalidOperationException("Note clôturée.");

        db.LignesFolio.Add(new LigneFolio
        {
            FolioId = folioId, Libelle = libelle, Categorie = cat,
            Quantite = quantite, MontantFcfa = montant
        });
        await db.SaveChangesAsync(ct);
        await audit.TracerAsync("AJOUT_LIGNE_NOTE", folioId.ToString(), $"{libelle} : {montant} FCFA", ct);
    }

    /// <summary>
    /// Encaisse un montant. Espèces/carte = confirmé immédiatement.
    /// Mobile money = initiation CinetPay (statut en attente jusqu'au webhook).
    /// Idempotence via RefExterne unique.
    /// </summary>
    public async Task<ResultatEncaissement> EncaisserAsync(
        Guid folioId, int montant, MoyenPaiement moyen, Guid? sessionId, CancellationToken ct = default)
    {
        if (montant <= 0)
            return new(false, "Montant invalide.", null, null);

        return await db.DansTransactionAsync(async () =>
        {
            var folio = await db.Folios.Include(f => f.Paiements).Include(f => f.Lignes)
                .FirstOrDefaultAsync(f => f.Id == folioId, ct)
                ?? throw new InvalidOperationException("Note introuvable.");

            var paiement = new Paiement
            {
                FolioId = folioId,
                SessionId = sessionId,
                Moyen = moyen,
                MontantFcfa = montant,
                Statut = StatutPaiement.Confirme
            };

            string? url = null;
            bool mobile = moyen is MoyenPaiement.OrangeMoney or MoyenPaiement.MtnMomo
                                or MoyenPaiement.Wave or MoyenPaiement.MoovMoney;
            if (mobile)
            {
                var cle = $"{folioId:N}-{DateTime.UtcNow:yyyyMMddHHmmssfff}";
                var res = await cinetpay.InitierAsync(
                    new DemandePaiement(folioId, montant, moyen, "Règlement note AKWABA", cle), ct);
                paiement.RefExterne = res.RefExterne;
                paiement.Statut = res.Statut;     // EnAttente en bac à sable
                url = res.UrlPaiement;
            }

            db.Paiements.Add(paiement);
            await db.SaveChangesAsync(ct);
            await audit.TracerAsync("ENCAISSEMENT", folioId.ToString(), $"{moyen} : {montant} FCFA", ct);

            var msg = mobile
                ? "Paiement mobile initié : en attente de confirmation."
                : "Paiement encaissé.";
            return new ResultatEncaissement(true, msg, paiement.Id, url);
        }, ct);
    }

    /// <summary>Confirme un paiement mobile (simulé ici ; en réel = webhook CinetPay).</summary>
    public async Task ConfirmerPaiementAsync(Guid paiementId, CancellationToken ct = default)
    {
        var paiement = await db.Paiements.FirstOrDefaultAsync(p => p.Id == paiementId, ct)
            ?? throw new InvalidOperationException("Paiement introuvable.");
        if (paiement.Statut == StatutPaiement.Confirme) return;   // idempotent
        paiement.Statut = StatutPaiement.Confirme;
        await db.SaveChangesAsync(ct);
        await audit.TracerAsync("CONFIRMER_PAIEMENT", paiementId.ToString(), null, ct);
    }
}
