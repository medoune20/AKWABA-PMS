using Akwaba.Application.Interfaces;
using Akwaba.Domain.Common;
using Akwaba.Domain.Entites;
using Akwaba.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Akwaba.Application.Services;

/// <summary>Sessions de caisse : ouverture, clôture, totaux par moyen.</summary>
public class ServiceCaisse(IAkwabaDbContext db, IServiceAudit audit)
{
    public Task<SessionCaisse?> SessionOuverteAsync(CancellationToken ct = default) =>
        db.SessionsCaisse.Include(s => s.Paiements)
          .Where(s => s.FermeeLe == null)
          .OrderByDescending(s => s.OuverteLe)
          .FirstOrDefaultAsync(ct);

    public async Task<SessionCaisse> OuvrirAsync(Guid utilisateurId, string nom, int fonds, CancellationToken ct = default)
    {
        var existante = await SessionOuverteAsync(ct);
        if (existante is not null)
            throw new InvalidOperationException("Une session de caisse est déjà ouverte.");

        var session = new SessionCaisse { UtilisateurId = utilisateurId, UtilisateurNom = nom, FondsFcfa = fonds };
        db.SessionsCaisse.Add(session);
        await db.SaveChangesAsync(ct);
        await audit.TracerAsync("OUVERTURE_CAISSE", session.Id.ToString(), $"Fonds {fonds} FCFA", ct);
        return session;
    }

    public async Task FermerAsync(Guid sessionId, int montantCompte, CancellationToken ct = default)
    {
        var session = await db.SessionsCaisse.Include(s => s.Paiements)
            .FirstOrDefaultAsync(s => s.Id == sessionId, ct)
            ?? throw new InvalidOperationException("Session introuvable.");

        var attenduEspeces = session.FondsFcfa + session.Paiements
            .Where(p => p.Moyen == MoyenPaiement.Especes && p.Statut == StatutPaiement.Confirme)
            .Sum(p => p.MontantFcfa);

        session.FermeeLe = DateTime.UtcNow;
        session.EcartFcfa = montantCompte - attenduEspeces;
        await db.SaveChangesAsync(ct);
        await audit.TracerAsync("CLOTURE_CAISSE", sessionId.ToString(), $"Écart {session.EcartFcfa} FCFA", ct);
    }
}
