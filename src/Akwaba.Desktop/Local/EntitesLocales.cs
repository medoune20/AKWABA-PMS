namespace Akwaba.Desktop.Local;

/// <summary>File d'attente persistante des opérations à pousser vers le serveur.</summary>
public class OperationLocale
{
    public long Id { get; set; }
    public string Entite { get; set; } = "";
    public Guid EntiteId { get; set; }
    public bool Supprime { get; set; }
    public string DonneesJson { get; set; } = "";
    public string CleIdempotence { get; set; } = Guid.NewGuid().ToString("N");
    public DateTime ModifieLeUtc { get; set; } = DateTime.UtcNow;
    public DateTime CreeLeUtc { get; set; } = DateTime.UtcNow;
    public int Tentatives { get; set; }
}

/// <summary>Paramètres clé/valeur du poste (curseur de pull, hôtel appairé, dernière synchro…).</summary>
public class ParametrePoste
{
    public string Cle { get; set; } = "";
    public string? Valeur { get; set; }
}

/// <summary>Compte mis en cache pour le déverrouillage hors ligne (mot de passe haché PBKDF2).</summary>
public class CompteCache
{
    public string Email { get; set; } = "";
    public string HashMdp { get; set; } = "";
    public Guid TenantId { get; set; }
    public string TenantNom { get; set; } = "";
    public Guid UtilisateurId { get; set; }
    public string NomComplet { get; set; } = "";
    public string RolesCsv { get; set; } = "";
}
