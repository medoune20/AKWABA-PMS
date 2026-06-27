using Microsoft.AspNetCore.Identity;

namespace Akwaba.Infrastructure.Persistence;

/// <summary>Utilisateur applicatif (étend Identity) rattaché à un hôtel.</summary>
public class AppliUtilisateur : IdentityUser<Guid>
{
    public Guid? TenantId { get; set; }   // null = compte plateforme (super-admin / modérateur)
    public string NomComplet { get; set; } = string.Empty;
    public string Theme { get; set; } = "clair";
}

/// <summary>Rôle applicatif (Identity).</summary>
public class AppliRole : IdentityRole<Guid>
{
    public AppliRole() { }
    public AppliRole(string nom) : base(nom) { }
}
