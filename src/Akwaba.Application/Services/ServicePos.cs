using Akwaba.Application.Interfaces;
using Akwaba.Domain.Common;
using Akwaba.Domain.Entites;
using Akwaba.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Akwaba.Application.Services;

/// <summary>Point de vente restaurant/bar : carte, commandes, imputation sur note ou encaissement direct.</summary>
public class ServicePos(IAkwabaDbContext db, IServiceAudit audit)
{
    public Task<List<CategorieProduit>> CarteAsync(CancellationToken ct = default) =>
        db.CategoriesProduit.Include(c => c.Produits.Where(p => p.Actif))
          .OrderBy(c => c.Ordre).ToListAsync(ct);

    public Task<Commande?> ChargerCommandeAsync(Guid id, CancellationToken ct = default) =>
        db.Commandes.Include(c => c.Lignes).Include(c => c.Sejour!).ThenInclude(s => s.Chambre)
          .FirstOrDefaultAsync(c => c.Id == id, ct);

    public Task<List<Commande>> CommandesOuvertesAsync(CancellationToken ct = default) =>
        db.Commandes.Include(c => c.Lignes)
          .Where(c => c.Statut == StatutCommande.Ouverte)
          .OrderBy(c => c.Numero).ToListAsync(ct);

    public async Task<Commande> CreerCommandeAsync(string? table, Guid? sejourId, CancellationToken ct = default)
    {
        var numero = (await db.Commandes.MaxAsync(c => (int?)c.Numero, ct) ?? 0) + 1;
        var cmd = new Commande { Numero = numero, Table = table, SejourId = sejourId };
        db.Commandes.Add(cmd);
        await db.SaveChangesAsync(ct);
        return cmd;
    }

    public async Task AjouterProduitAsync(Guid commandeId, Guid produitId, CancellationToken ct = default)
    {
        var cmd = await db.Commandes.Include(c => c.Lignes).FirstOrDefaultAsync(c => c.Id == commandeId, ct)
            ?? throw new InvalidOperationException("Commande introuvable.");
        if (cmd.Statut != StatutCommande.Ouverte) throw new InvalidOperationException("Commande clôturée.");
        var produit = await db.Produits.FirstOrDefaultAsync(p => p.Id == produitId, ct)
            ?? throw new InvalidOperationException("Produit introuvable.");

        var existante = cmd.Lignes.FirstOrDefault(l => l.ProduitId == produitId);
        if (existante is not null) existante.Quantite++;
        else db.LignesCommande.Add(new LigneCommande
        {
            CommandeId = commandeId, ProduitId = produitId,
            Libelle = produit.Libelle, PrixUnitaireFcfa = produit.PrixFcfa, Quantite = 1
        });
        await db.SaveChangesAsync(ct);
    }

    public async Task ChangerQuantiteAsync(Guid ligneId, int delta, CancellationToken ct = default)
    {
        var ligne = await db.LignesCommande.FirstOrDefaultAsync(l => l.Id == ligneId, ct);
        if (ligne is null) return;
        ligne.Quantite += delta;
        if (ligne.Quantite <= 0) db.LignesCommande.Remove(ligne);
        await db.SaveChangesAsync(ct);
    }

    /// <summary>Impute la commande sur la note de la chambre (ajoute une ligne Restauration au folio).</summary>
    public async Task EnvoyerSurNoteAsync(Guid commandeId, CancellationToken ct = default)
    {
        await db.DansTransactionAsync<object?>(async () =>
        {
            var cmd = await db.Commandes.Include(c => c.Lignes)
                .FirstOrDefaultAsync(c => c.Id == commandeId, ct)
                ?? throw new InvalidOperationException("Commande introuvable.");
            if (cmd.SejourId is null) throw new InvalidOperationException("Aucune chambre associée à cette commande.");

            var folio = await db.Folios.FirstOrDefaultAsync(f => f.SejourId == cmd.SejourId && f.Statut == StatutFolio.Ouvert, ct)
                ?? throw new InvalidOperationException("Aucune note ouverte pour ce séjour (check-in requis).");

            db.LignesFolio.Add(new LigneFolio
            {
                FolioId = folio.Id,
                Libelle = $"Restaurant — commande #{cmd.Numero}",
                Categorie = CategorieLigne.Restauration,
                Quantite = 1,
                MontantFcfa = cmd.TotalFcfa
            });
            cmd.Statut = StatutCommande.EnvoyeeSurNote;
            await db.SaveChangesAsync(ct);
            await audit.TracerAsync("POS_ENVOI_NOTE", cmd.Id.ToString(), $"#{cmd.Numero} : {cmd.TotalFcfa} FCFA", ct);
            return null;
        }, ct);
    }

    /// <summary>Encaisse directement la commande (sans note de chambre).</summary>
    public async Task EncaisserDirectAsync(Guid commandeId, MoyenPaiement moyen, Guid? sessionId, CancellationToken ct = default)
    {
        await db.DansTransactionAsync<object?>(async () =>
        {
            var cmd = await db.Commandes.Include(c => c.Lignes)
                .FirstOrDefaultAsync(c => c.Id == commandeId, ct)
                ?? throw new InvalidOperationException("Commande introuvable.");
            if (!cmd.Lignes.Any()) throw new InvalidOperationException("Commande vide.");

            db.Paiements.Add(new Paiement
            {
                FolioId = null, SessionId = sessionId, Moyen = moyen,
                MontantFcfa = cmd.TotalFcfa, Statut = StatutPaiement.Confirme
            });
            cmd.Statut = StatutCommande.Payee;
            await db.SaveChangesAsync(ct);
            await audit.TracerAsync("POS_ENCAISSEMENT", cmd.Id.ToString(), $"#{cmd.Numero} {moyen} : {cmd.TotalFcfa} FCFA", ct);
            return null;
        }, ct);
    }
}
