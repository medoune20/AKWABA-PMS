using System.Globalization;

namespace Akwaba.Web.Infrastructure.Helpers;

/// <summary>Formatage monétaire FCFA (séparateur de milliers par espace, sans décimale).</summary>
public static class Fcfa
{
    private static readonly CultureInfo Ci = CultureInfo.GetCultureInfo("fr-FR");
    public static string Format(int montant) => montant.ToString("#,0", Ci).Replace(",", " ") + " FCFA";
    public static string Format(long montant) => montant.ToString("#,0", Ci).Replace(",", " ") + " FCFA";
}
