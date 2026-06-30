using Akwaba.Desktop.Local;
using Akwaba.Sync;
using Microsoft.EntityFrameworkCore;

namespace Akwaba.Desktop.Services;

/// <summary>Moteur de synchronisation : pousse l'outbox (idempotent) puis tire les deltas du serveur.</summary>
public class ServiceSync(SyncClient client, ContexteLocal db, DepotLocal depot)
{
    public event Action<string>? Journal;
    public DateTime? DerniereSync { get; private set; }

    public async Task<(bool ok, string message)> SynchroniserAsync(CancellationToken ct = default)
    {
        if (!client.EstAuthentifie)
            return (false, "Connexion en ligne requise pour synchroniser (jeton absent).");

        try
        {
            await PousserAsync(ct);
            await TirerAsync(ct);
            DerniereSync = DateTime.UtcNow;
            await depot.DefinirParamAsync("DerniereSyncUtc", DerniereSync.Value.ToString("O"));
            return (true, "Synchronisation réussie.");
        }
        catch (Exception ex)
        {
            Journal?.Invoke($"Échec : {ex.Message}");
            return (false, ex.Message);
        }
    }

    private async Task PousserAsync(CancellationToken ct)
    {
        var ops = await db.Outbox.OrderBy(o => o.Id).ToListAsync(ct);
        if (ops.Count == 0) { Journal?.Invoke("Aucune opération locale à envoyer."); return; }

        var req = new PushRequest(ops.Select(o => new OperationSync(
            o.Entite, o.EntiteId, o.Supprime, o.DonneesJson, o.CleIdempotence, o.ModifieLeUtc)).ToList());

        var rep = await client.PushAsync(req, ct) ?? throw new Exception("Réponse push vide.");
        var echecs = rep.Resultats.Where(r => !r.Applique).Select(r => r.Id).ToHashSet();

        foreach (var o in ops)
        {
            if (echecs.Contains(o.EntiteId)) { o.Tentatives++; continue; }
            db.Outbox.Remove(o);
        }
        await db.SaveChangesAsync(ct);
        Journal?.Invoke($"{ops.Count - echecs.Count} opération(s) envoyée(s){(echecs.Count > 0 ? $", {echecs.Count} en échec" : "")}.");
    }

    private async Task TirerAsync(CancellationToken ct)
    {
        var depuis = DateTime.TryParse(depot.Param("CurseurPull"), null,
            System.Globalization.DateTimeStyles.RoundtripKind, out var c) ? c : new DateTime(2000, 1, 1);

        var rep = await client.PullAsync(depuis, ct) ?? throw new Exception("Réponse pull vide.");
        foreach (var el in rep.Elements)
            Mappage.Appliquer(db, el);
        await db.SaveChangesAsync(ct);
        await depot.DefinirParamAsync("CurseurPull", rep.ServeurUtc.ToString("O"));
        Journal?.Invoke($"{rep.Elements.Count} enregistrement(s) reçu(s) du serveur.");
    }
}
