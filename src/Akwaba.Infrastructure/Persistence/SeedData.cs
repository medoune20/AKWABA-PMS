using Akwaba.Domain.Common;
using Akwaba.Domain.Entites;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Akwaba.Infrastructure.Persistence;

/// <summary>Crée la base et insère un jeu de données de démonstration cohérent.</summary>
public static class SeedData
{
    public static readonly string[] Roles =
        { "SUPER_ADMIN", "MODERATEUR", "GERANT", "RECEPTIONNISTE", "GOUVERNANTE", "CAISSIER", "COMPTABLE", "LECTURE_SEULE" };

    public static async Task InitialiserAsync(IServiceProvider sp)
    {
        var db = sp.GetRequiredService<AkwabaDbContext>();
        var userMgr = sp.GetRequiredService<UserManager<AppliUtilisateur>>();
        var roleMgr = sp.GetRequiredService<RoleManager<AppliRole>>();

        await db.Database.EnsureCreatedAsync();

        foreach (var r in Roles)
            if (!await roleMgr.RoleExistsAsync(r))
                await roleMgr.CreateAsync(new AppliRole(r));

        // Référentiels globaux
        if (!await db.DonneesReference.AnyAsync())
        {
            db.DonneesReference.AddRange(
                new DonneeReference { Categorie = "DEVISE", Code = "XOF", Libelle = "Franc CFA", Valeur = "FCFA" },
                new DonneeReference { Categorie = "TAXE", Code = "TVA", Libelle = "TVA", Valeur = "18" },
                new DonneeReference { Categorie = "MODE_PAIEMENT", Code = "OM", Libelle = "Orange Money" },
                new DonneeReference { Categorie = "MODE_PAIEMENT", Code = "MTN", Libelle = "MTN Mobile Money" },
                new DonneeReference { Categorie = "MODE_PAIEMENT", Code = "WAVE", Libelle = "Wave" },
                new DonneeReference { Categorie = "MODE_PAIEMENT", Code = "MOOV", Libelle = "Moov Money" });
            await db.SaveChangesAsync();
        }

        // Comptes plateforme
        await CreerUtilisateur(userMgr, "superadmin@akwaba.ci", "Akwaba#2026", "Super Administrateur", null, "SUPER_ADMIN");
        await CreerUtilisateur(userMgr, "moderateur@akwaba.ci", "Akwaba#2026", "Modérateur Plateforme", null, "MODERATEUR");

        // Hôtel de démonstration (approuvé)
        if (!await db.Tenants.AnyAsync(t => t.SousDomaine == "ivoire-palace"))
        {
            var tenant = new Tenant
            {
                Nom = "Ivoire Palace Hôtel",
                SousDomaine = "ivoire-palace",
                Ville = "Abidjan",
                Telephone = "+225 27 22 00 00 00",
                Statut = StatutTenant.Approuve
            };
            db.Tenants.Add(tenant);
            db.Abonnements.Add(new Abonnement { TenantId = tenant.Id, Plan = "STANDARD", MontantMensuelFcfa = 25000 });

            var tStd = new TypeChambre { TenantId = tenant.Id, Libelle = "Standard", Capacite = 2, Description = "Chambre confortable" };
            var tDlx = new TypeChambre { TenantId = tenant.Id, Libelle = "Deluxe", Capacite = 2, Description = "Vue lagune" };
            var tSte = new TypeChambre { TenantId = tenant.Id, Libelle = "Suite", Capacite = 4, Description = "Salon séparé" };
            db.TypesChambre.AddRange(tStd, tDlx, tSte);

            var anneeDebut = new DateOnly(DateTime.UtcNow.Year, 1, 1);
            var anneeFin = new DateOnly(DateTime.UtcNow.Year, 12, 31);
            db.Tarifs.AddRange(
                new Tarif { TenantId = tenant.Id, TypeChambreId = tStd.Id, DateDebut = anneeDebut, DateFin = anneeFin, PrixNuitFcfa = 35000 },
                new Tarif { TenantId = tenant.Id, TypeChambreId = tDlx.Id, DateDebut = anneeDebut, DateFin = anneeFin, PrixNuitFcfa = 55000 },
                new Tarif { TenantId = tenant.Id, TypeChambreId = tSte.Id, DateDebut = anneeDebut, DateFin = anneeFin, PrixNuitFcfa = 95000 });

            for (int etage = 1; etage <= 3; etage++)
                for (int n = 1; n <= 4; n++)
                {
                    var type = etage == 3 ? tSte : (n <= 2 ? tStd : tDlx);
                    db.Chambres.Add(new Chambre
                    {
                        TenantId = tenant.Id, TypeChambreId = type.Id,
                        Numero = $"{etage}0{n}", Etage = (short)etage,
                        StatutMenage = StatutMenage.Propre
                    });
                }

            db.Clients.AddRange(
                new Client { TenantId = tenant.Id, NomComplet = "Konan Yao", Telephone = "+225 07 07 07 07 07", TypePiece = "CNI", NumPiece = "CI00123" },
                new Client { TenantId = tenant.Id, NomComplet = "Aïcha Traoré", Telephone = "+225 05 05 05 05 05", TypePiece = "Passeport", NumPiece = "20PA98765" });

            await db.SaveChangesAsync();

            await CreerUtilisateur(userMgr, "gerant@ivoire-palace.ci", "Akwaba#2026", "Gérant Ivoire Palace", tenant.Id, "GERANT");
            await CreerUtilisateur(userMgr, "reception@ivoire-palace.ci", "Akwaba#2026", "Réception Ivoire Palace", tenant.Id, "RECEPTIONNISTE");
        }

        // Hôtel en attente de validation (pour tester la modération)
        if (!await db.Tenants.AnyAsync(t => t.SousDomaine == "lagune-bleue"))
        {
            var pending = new Tenant
            {
                Nom = "Hôtel Lagune Bleue",
                SousDomaine = "lagune-bleue",
                Ville = "Grand-Bassam",
                Telephone = "+225 21 30 00 00 00",
                Statut = StatutTenant.EnAttente
            };
            db.Tenants.Add(pending);
            await db.SaveChangesAsync();
            await CreerUtilisateur(userMgr, "demande@lagune-bleue.ci", "Akwaba#2026", "Propriétaire Lagune Bleue", pending.Id, "GERANT");
        }
    }

    private static async Task CreerUtilisateur(
        UserManager<AppliUtilisateur> mgr, string email, string mdp, string nom, Guid? tenantId, string role)
    {
        if (await mgr.FindByEmailAsync(email) is not null) return;
        var u = new AppliUtilisateur
        {
            UserName = email, Email = email, EmailConfirmed = true,
            NomComplet = nom, TenantId = tenantId
        };
        var res = await mgr.CreateAsync(u, mdp);
        if (res.Succeeded) await mgr.AddToRoleAsync(u, role);
    }
}
