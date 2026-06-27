using Akwaba.Domain.Common;

namespace Akwaba.Domain.Entites;

/// <summary>Fiche d'un client (voyageur) de l'hôtel.</summary>
public class Client : EntiteBase
{
    public string NomComplet { get; set; } = string.Empty;
    public string? Telephone { get; set; }
    public string? Email { get; set; }
    public string? TypePiece { get; set; }
    public string? NumPiece { get; set; }

    public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
}
