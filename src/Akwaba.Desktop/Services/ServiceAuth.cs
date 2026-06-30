using Akwaba.Desktop.Local;
using Akwaba.Sync;
using Microsoft.EntityFrameworkCore;

namespace Akwaba.Desktop.Services;

/// <summary>Connexion en ligne (obtention du JWT) et déverrouillage hors ligne (hash en cache).</summary>
public class ServiceAuth(SyncClient client, ContexteLocal db, SessionPoste session, DepotLocal depot)
{
    public async Task<(bool ok, string? message)> ConnexionEnLigneAsync(string email, string mdp, string deviceId)
    {
        var rep = await client.LoginAsync(new LoginRequest(email, mdp, deviceId));
        if (rep is null) return (false, "Identifiants invalides ou hôtel non approuvé.");

        // Mise en cache pour déverrouillage hors ligne ultérieur
        var compte = await db.Comptes.FindAsync(email) ?? new CompteCache { Email = email };
        compte.HashMdp = ServiceSecuriteLocale.Hacher(mdp);
        compte.TenantId = rep.TenantId; compte.TenantNom = rep.TenantNom;
        compte.UtilisateurId = rep.UtilisateurId; compte.NomComplet = rep.NomComplet;
        compte.RolesCsv = string.Join(',', rep.Roles);
        if (db.Entry(compte).State == EntityState.Detached) db.Comptes.Add(compte);
        await db.SaveChangesAsync();
        await depot.DefinirParamAsync("TenantId", rep.TenantId.ToString());

        session.Ouvrir(email, rep.TenantId, rep.TenantNom, rep.UtilisateurId, rep.NomComplet, rep.Roles, enLigne: true);
        return (true, null);
    }

    public async Task<(bool ok, string? message)> DeverrouillerHorsLigneAsync(string email, string mdp)
    {
        var compte = await db.Comptes.FindAsync(email);
        if (compte is null) return (false, "Ce compte n'a jamais été connecté en ligne sur ce poste.");
        if (!ServiceSecuriteLocale.Verifier(mdp, compte.HashMdp)) return (false, "Mot de passe incorrect.");

        session.Ouvrir(email, compte.TenantId, compte.TenantNom, compte.UtilisateurId, compte.NomComplet,
            compte.RolesCsv.Split(',', StringSplitOptions.RemoveEmptyEntries), enLigne: false);
        return (true, null);
    }

    public Task<List<string>> ComptesCachesAsync() => db.Comptes.Select(c => c.Email).ToListAsync();
}
