namespace Akwaba.Domain.Interfaces;

/// <summary>Identité de l'utilisateur authentifié pour la requête courante.</summary>
public interface IUtilisateurCourant
{
    Guid? Id { get; }
    string Nom { get; }
}
