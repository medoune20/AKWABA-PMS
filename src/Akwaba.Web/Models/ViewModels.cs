using System.ComponentModel.DataAnnotations;

namespace Akwaba.Web.Models;

public class ConnexionVm
{
    [Required, EmailAddress] public string Email { get; set; } = "";
    [Required, DataType(DataType.Password)] public string MotDePasse { get; set; } = "";
    public bool SeSouvenir { get; set; }
}

public class InscriptionVm
{
    [Required, Display(Name = "Nom de l'hôtel")] public string NomHotel { get; set; } = "";
    [Required, RegularExpression("^[a-z0-9-]{3,63}$", ErrorMessage = "Lettres minuscules, chiffres et tirets uniquement.")]
    [Display(Name = "Sous-domaine")] public string SousDomaine { get; set; } = "";
    [Required] public string Ville { get; set; } = "Abidjan";
    [Display(Name = "Téléphone")] public string? Telephone { get; set; }
    [Required, Display(Name = "Votre nom complet")] public string NomComplet { get; set; } = "";
    [Required, EmailAddress] public string Email { get; set; } = "";
    [Required, DataType(DataType.Password), MinLength(8)]
    [Display(Name = "Mot de passe")] public string MotDePasse { get; set; } = "";
}

public class ReinitialiserVm
{
    [Required, EmailAddress] public string Email { get; set; } = "";
    [Required] public string Token { get; set; } = "";
    [Required, DataType(DataType.Password), MinLength(8)]
    [Display(Name = "Nouveau mot de passe")] public string MotDePasse { get; set; } = "";
}

public class CreationReservationVm
{
    [Required] public DateOnly Arrivee { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    [Required] public DateOnly Depart { get; set; } = DateOnly.FromDateTime(DateTime.Today.AddDays(1));
    public Guid? ClientId { get; set; }
    public Guid? ChambreId { get; set; }
    public short NbAdultes { get; set; } = 1;
    public string Canal { get; set; } = "DIRECT";
}
