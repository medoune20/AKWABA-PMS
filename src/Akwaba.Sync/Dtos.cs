namespace Akwaba.Sync;

// ---------- Authentification ----------
public record LoginRequest(string Email, string MotDePasse, string DeviceId);
public record LoginResponse(string AccessToken, DateTime ExpireLe, Guid TenantId,
    string TenantNom, Guid UtilisateurId, string NomComplet, string[] Roles);

// ---------- Enveloppe de synchronisation ----------
/// <summary>Une opération locale à pousser vers le serveur (idempotente par CleIdempotence).</summary>
public record OperationSync(
    string Entite,            // "Client","Reservation","Sejour","Folio","LigneFolio","Paiement","SessionCaisse"
    Guid Id,
    bool Supprime,
    string DonneesJson,       // JSON du DTO d'entité ci-dessous
    string CleIdempotence,
    DateTime ModifieLe);

public record PushRequest(List<OperationSync> Operations);
public record PushResultat(Guid Id, bool Applique, string? Message);
public record PushResponse(DateTime ServeurUtc, List<PushResultat> Resultats);

/// <summary>Un enregistrement renvoyé par le serveur lors du pull.</summary>
public record ElementSync(string Entite, string DonneesJson, bool Supprime, DateTime ModifieLe);
public record PullResponse(DateTime ServeurUtc, List<ElementSync> Elements);

// ---------- DTOs d'entités (scalaires uniquement, pas de navigation) ----------
public record ClientDto(Guid Id, Guid TenantId, string NomComplet, string? Telephone,
    string? Email, string? TypePiece, string? NumPiece, DateTime ModifieLe);

public record ReservationDto(Guid Id, Guid TenantId, Guid ClientId, string Canal, int Statut, DateTime ModifieLe);

public record SejourDto(Guid Id, Guid TenantId, Guid ReservationId, Guid ChambreId,
    DateOnly DateArrivee, DateOnly DateDepart, short NbAdultes, int PrixNuitFcfa, int Statut, DateTime ModifieLe);

public record FolioDto(Guid Id, Guid TenantId, Guid SejourId, int Statut, DateTime ModifieLe);

public record LigneFolioDto(Guid Id, Guid TenantId, Guid FolioId, string Libelle,
    int Categorie, int Quantite, int MontantFcfa, DateTime ModifieLe);

public record PaiementDto(Guid Id, Guid TenantId, Guid? FolioId, Guid? SessionId,
    int Moyen, int MontantFcfa, string? RefExterne, int Statut, DateTime ModifieLe);

public record SessionCaisseDto(Guid Id, Guid TenantId, Guid UtilisateurId, string UtilisateurNom,
    DateTime OuverteLe, DateTime? FermeeLe, int FondsFcfa, int? EcartFcfa, DateTime ModifieLe);

// Référentiels (pull uniquement)
public record ChambreDto(Guid Id, Guid TenantId, Guid TypeChambreId, string Numero,
    short? Etage, int StatutMenage, bool Active, DateTime ModifieLe);
public record TypeChambreDto(Guid Id, Guid TenantId, string Libelle, short Capacite, string? Description, DateTime ModifieLe);
public record TarifDto(Guid Id, Guid TenantId, Guid TypeChambreId, string LibelleSaison,
    DateOnly DateDebut, DateOnly DateFin, int PrixNuitFcfa, DateTime ModifieLe);
