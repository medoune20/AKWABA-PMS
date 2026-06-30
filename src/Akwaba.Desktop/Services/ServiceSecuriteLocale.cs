using System.Security.Cryptography;

namespace Akwaba.Desktop.Services;

/// <summary>
/// Hachage PBKDF2-HMAC-SHA256 du mot de passe pour le déverrouillage hors ligne.
/// Le hachage est calculé lors d'une connexion en ligne réussie, puis mis en cache local ;
/// hors ligne, on vérifie le mot de passe saisi contre ce hachage. Jamais de mot de passe en clair.
/// </summary>
public static class ServiceSecuriteLocale
{
    private const int Iterations = 100_000;
    private const int TailleSel = 16;
    private const int TailleCle = 32;

    public static string Hacher(string motDePasse)
    {
        var sel = RandomNumberGenerator.GetBytes(TailleSel);
        var cle = Rfc2898DeriveBytes.Pbkdf2(motDePasse, sel, Iterations, HashAlgorithmName.SHA256, TailleCle);
        var combine = new byte[TailleSel + TailleCle];
        Buffer.BlockCopy(sel, 0, combine, 0, TailleSel);
        Buffer.BlockCopy(cle, 0, combine, TailleSel, TailleCle);
        return Convert.ToBase64String(combine);
    }

    public static bool Verifier(string motDePasse, string hashStocke)
    {
        try
        {
            var combine = Convert.FromBase64String(hashStocke);
            if (combine.Length != TailleSel + TailleCle) return false;
            var sel = combine.AsSpan(0, TailleSel).ToArray();
            var attendu = combine.AsSpan(TailleSel, TailleCle).ToArray();
            var calcule = Rfc2898DeriveBytes.Pbkdf2(motDePasse, sel, Iterations, HashAlgorithmName.SHA256, TailleCle);
            return CryptographicOperations.FixedTimeEquals(calcule, attendu);
        }
        catch { return false; }
    }
}
