using Akwaba.Domain.Common;

namespace Akwaba.Domain.Entites;

/// <summary>Réservation regroupant un ou plusieurs séjours.</summary>
public class Reservation : EntiteBase
{
    public Guid ClientId { get; set; }
    public Client? Client { get; set; }
    public string Canal { get; set; } = "DIRECT";   // DIRECT, BOOKING, EXPEDIA...
    public StatutReservation Statut { get; set; } = StatutReservation.Confirmee;

    public ICollection<Sejour> Sejours { get; set; } = new List<Sejour>();
}
