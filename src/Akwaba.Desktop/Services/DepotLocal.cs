using Akwaba.Domain.Common;
using Akwaba.Desktop.Local;
using Microsoft.EntityFrameworkCore;

namespace Akwaba.Desktop.Services;

/// <summary>Accès à la base locale. Toute mutation métier passe par ici : elle écrit l'entité
/// et dépose une opération dans l'outbox pour synchronisation ultérieure.</summary>
public class DepotLocal(ContexteLocal db)
{
    public ContexteLocal Db => db;

    public async Task EnregistrerAsync<T>(string entite, T entity, bool supprime = false)
        where T : Akwaba.Domain.Common.EntiteBase
    {
        entity.ModifieLe = DateTime.UtcNow;
        var existant = await db.Set<T>().FindAsync(entity.Id);
        if (supprime)
        {
            if (existant is not null) db.Set<T>().Remove(existant);
        }
        else if (existant is null) db.Set<T>().Add(entity);
        else db.Entry(existant).CurrentValues.SetValues(entity);

        db.Outbox.Add(new OperationLocale
        {
            Entite = entite,
            EntiteId = entity.Id,
            Supprime = supprime,
            DonneesJson = Mappage.VersJson(entite, entity),
            ModifieLeUtc = entity.ModifieLe
        });
        await db.SaveChangesAsync();
    }

    public int NbEnAttente() => db.Outbox.Count();

    // Requêtes de lecture pour les écrans
    public Task<List<Local.OperationLocale>> OperationsAsync() =>
        db.Outbox.OrderBy(o => o.Id).ToListAsync();

    public string? Param(string cle) => db.Parametres.Find(cle)?.Valeur;

    public async Task DefinirParamAsync(string cle, string valeur)
    {
        var p = await db.Parametres.FindAsync(cle);
        if (p is null) db.Parametres.Add(new ParametrePoste { Cle = cle, Valeur = valeur });
        else p.Valeur = valeur;
        await db.SaveChangesAsync();
    }
}
