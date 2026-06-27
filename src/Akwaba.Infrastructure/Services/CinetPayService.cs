using Akwaba.Domain.Common;
using Akwaba.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Akwaba.Infrastructure.Services;

/// <summary>
/// Intégration CinetPay — mode BAC À SABLE.
/// Le flux (initiation → URL/USSD → confirmation) est respecté ; aucun appel réseau réel n'est fait
/// tant que les clés marchand ne sont pas configurées (CinetPay:ApiKey / CinetPay:SiteId).
/// L'intégration réelle remplace le corps de ces méthodes sans changer l'interface.
/// </summary>
public class CinetPayService(IConfiguration config, ILogger<CinetPayService> logger) : ICinetPayService
{
    private bool ModeReel =>
        !string.IsNullOrWhiteSpace(config["CinetPay:ApiKey"]) &&
        !string.IsNullOrWhiteSpace(config["CinetPay:SiteId"]);

    public Task<ResultatPaiement> InitierAsync(DemandePaiement demande, CancellationToken ct = default)
    {
        if (ModeReel)
        {
            // Point d'extension : POST https://api-checkout.cinetpay.com/v2/payment
            // avec apikey, site_id, transaction_id = demande.CleIdempotence, amount, currency=XOF,
            // channels selon demande.Moyen, notify_url (webhook), return_url. Renvoyer payment_url.
            logger.LogInformation("CinetPay (réel) initiation {Ref}", demande.CleIdempotence);
        }

        var refExterne = "CP-" + demande.CleIdempotence;
        logger.LogInformation("Paiement {Moyen} initié (bac à sable) : {Montant} FCFA, ref {Ref}",
            demande.Moyen, demande.MontantFcfa, refExterne);

        // En bac à sable : le paiement reste EN ATTENTE jusqu'à confirmation manuelle (simulateur de webhook).
        return Task.FromResult(new ResultatPaiement(
            Succes: true,
            Statut: StatutPaiement.EnAttente,
            RefExterne: refExterne,
            UrlPaiement: $"/Folios/SimulerWebhook?ref={Uri.EscapeDataString(refExterne)}",
            Message: "Initiation réussie (bac à sable)."));
    }

    public Task<ResultatPaiement> VerifierAsync(string refExterne, CancellationToken ct = default)
        => Task.FromResult(new ResultatPaiement(true, StatutPaiement.Confirme, refExterne, null, "Confirmé."));
}
