using Akwaba.Domain.Common;

namespace Akwaba.Application.Dtos;

public record IndicateursTableauBord(
    int ChambresTotal, int ChambresOccupees, double TauxOccupation,
    int ArriveesDuJour, int DepartsDuJour, int ChambresANettoyer,
    long CaDuJourFcfa, long CaDuMoisFcfa, int AdrFcfa, int RevParFcfa);

public record CreationReservation(
    Guid ClientId, Guid ChambreId, DateOnly Arrivee, DateOnly Depart,
    short NbAdultes, string Canal);

public record ResultatEncaissement(bool Succes, string Message, Guid? PaiementId, string? UrlPaiement);
