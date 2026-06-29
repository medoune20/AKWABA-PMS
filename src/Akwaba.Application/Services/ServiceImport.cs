using Akwaba.Application.Interfaces;
using Akwaba.Domain.Common;
using Akwaba.Domain.Entites;
using Microsoft.EntityFrameworkCore;

namespace Akwaba.Application.Services;

public record ResultatImport(int Crees, int Ignores, List<string> Erreurs);

/// <summary>
/// Channel Manager — démarrage par import.
/// Importe des réservations OTA depuis un CSV : client;telephone;chambre;arrivee;depart;canal
/// (dates au format yyyy-MM-dd). Crée les clients manquants et les réservations associées.
/// </summary>
public class ServiceImport(IAkwabaDbContext db, ServiceReservation reservations)
{
    public async Task<ResultatImport> ImporterReservationsCsvAsync(string contenu, CancellationToken ct = default)
    {
        var erreurs = new List<string>();
        int crees = 0, ignores = 0;
        var lignes = contenu.Replace("\r", "").Split('\n', StringSplitOptions.RemoveEmptyEntries);

        for (int i = 0; i < lignes.Length; i++)
        {
            var ligne = lignes[i].Trim();
            if (i == 0 && ligne.ToLowerInvariant().Contains("client")) continue; // en-tête
            var champs = ligne.Split(';');
            if (champs.Length < 5) { erreurs.Add($"Ligne {i + 1} : format invalide ({champs.Length} colonnes)."); continue; }

            var nom = champs[0].Trim();
            var tel = champs[1].Trim();
            var numChambre = champs[2].Trim();
            var canal = champs.Length >= 6 ? champs[5].Trim() : "OTA";

            if (!DateOnly.TryParse(champs[3].Trim(), out var arrivee) ||
                !DateOnly.TryParse(champs[4].Trim(), out var depart))
            { erreurs.Add($"Ligne {i + 1} : dates invalides."); continue; }

            var chambre = await db.Chambres.FirstOrDefaultAsync(c => c.Numero == numChambre, ct);
            if (chambre is null) { erreurs.Add($"Ligne {i + 1} : chambre '{numChambre}' introuvable."); ignores++; continue; }

            var client = await db.Clients.FirstOrDefaultAsync(c => c.NomComplet == nom && c.Telephone == tel, ct);
            if (client is null)
            {
                client = new Client { NomComplet = nom, Telephone = tel };
                db.Clients.Add(client);
                await db.SaveChangesAsync(ct);
            }

            try
            {
                await reservations.CreerAsync(new Dtos.CreationReservation(
                    client.Id, chambre.Id, arrivee, depart, 1, canal), ct);
                crees++;
            }
            catch (InvalidOperationException ex)
            {
                erreurs.Add($"Ligne {i + 1} : {ex.Message}"); ignores++;
            }
        }
        return new ResultatImport(crees, ignores, erreurs);
    }
}
