using Akwaba.Domain.Common;

namespace Akwaba.Domain.Entites;

/// <summary>Occupation d'une chambre sur une plage de dates.</summary>
public class Sejour : EntiteBase
{
    public Guid ReservationId { get; set; }
    public Reservation? Reservation { get; set; }
    public Guid ChambreId { get; set; }
    public Chambre? Chambre { get; set; }
    public DateOnly DateArrivee { get; set; }
    public DateOnly DateDepart { get; set; }
    public short NbAdultes { get; set; } = 1;
    public int PrixNuitFcfa { get; set; }
    public StatutSejour Statut { get; set; } = StatutSejour.Reserve;

    public Folio? Folio { get; set; }

    /// <summary>Nombre de nuitées du séjour.</summary>
    public int NombreNuits => Math.Max(0, DateDepart.DayNumber - DateArrivee.DayNumber);
}
