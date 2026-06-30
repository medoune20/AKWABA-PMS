namespace Akwaba.Desktop.Services;

/// <summary>État de la session courante du poste (utilisateur connecté).</summary>
public class SessionPoste
{
    public bool Connecte { get; private set; }
    public bool AuthentifieEnLigne { get; private set; }
    public string Email { get; private set; } = "";
    public Guid TenantId { get; private set; }
    public string TenantNom { get; private set; } = "";
    public Guid UtilisateurId { get; private set; }
    public string NomComplet { get; private set; } = "";
    public string[] Roles { get; private set; } = [];

    public void Ouvrir(string email, Guid tenantId, string tenantNom, Guid userId,
        string nomComplet, string[] roles, bool enLigne)
    {
        Email = email; TenantId = tenantId; TenantNom = tenantNom; UtilisateurId = userId;
        NomComplet = nomComplet; Roles = roles; AuthentifieEnLigne = enLigne; Connecte = true;
    }

    public bool EstDansRole(string role) => Roles.Contains(role);
    public void Fermer() { Connecte = false; AuthentifieEnLigne = false; Roles = []; }
}
