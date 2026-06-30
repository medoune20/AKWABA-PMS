# AKWABA Desktop — Client bureau hors-ligne

Application Windows (WPF / .NET 10) de **réception & caisse** fonctionnant **hors ligne**, qui se
synchronise avec la plateforme web AKWABA (`lp2medoune.com/gestionhotel`).

## État d'avancement

- **Lot 0 — API de synchronisation (serveur)** : fait. Endpoints `/api/auth/login`, `/api/sync/ping|pull|push` (JWT).
- **Lot 1 — Socle desktop & base locale** : fait. Projet WPF, base SQLite locale, connexion en ligne + déverrouillage hors ligne (PBKDF2 en cache).
- **Lot 2 — Moteur de synchronisation** : fait. Outbox, push idempotent, pull delta par curseur.
- **Lot 3 — Réception (clients, plan, check-in/out, note)** : à venir (prochain incrément).
- **Lot 4 — Caisse (session, encaissement espèces/carte)** : à venir.
- **Lot 5 — Packaging MSIX** : à venir.

La version actuelle est une **tranche verticale fonctionnelle** : on se connecte (en ligne ou hors
ligne), on voit l'état de connectivité et la file d'attente, et on synchronise manuellement. Les
écrans métier (réception/caisse) s'ajouteront par-dessus ce socle.

## Prérequis (poste Windows)

- Windows 10/11.
- **.NET 10 SDK** avec la charge de travail Desktop (WPF) — installé via Visual Studio 2022+
  (« Développement .NET Desktop ») ou `dotnet workload`.

## Compiler et lancer

```powershell
# Depuis la racine du dépôt, sur Windows :
dotnet restore AkwabaDesktop.sln
dotnet build  AkwabaDesktop.sln -c Release
dotnet run    --project src\Akwaba.Desktop\Akwaba.Desktop.csproj -c Release
```

> ⚠️ `AkwabaDesktop.sln` ne se compile **que sous Windows** (WPF). La solution serveur
> `AkwabaPMS.sln` reste indépendante et continue de se construire sous Linux/Docker.

## Utilisation

1. **Première connexion (en ligne)** : saisir l'e-mail + mot de passe d'un compte hôtel
   (ex. `reception@ivoire-palace.ci` / `Akwaba#2026`). Le poste obtient un jeton, met le compte en
   cache (mot de passe haché PBKDF2) et effectue une première synchronisation.
2. **Déverrouillage hors ligne** : une fois le compte mis en cache, le bouton
   « Déverrouiller (hors ligne) » permet d'ouvrir une session sans réseau.
3. **Synchronisation** : automatique au retour en ligne, ou manuelle via « Synchroniser maintenant ».

## Configuration

- **URL de l'API** : `https://lp2medoune.com/gestionhotel/` par défaut (constante `App.UrlApiDefaut`).
- **Base locale** : `%LOCALAPPDATA%\AkwabaDesktop\akwaba-local.db`.

## Sécurité

- Mot de passe jamais stocké en clair : hachage **PBKDF2-HMAC-SHA256** (100 000 itérations) pour le
  déverrouillage hors ligne.
- Transport HTTPS (l'API est derrière Caddy/TLS).
- Chiffrement de la base locale (SQLCipher) prévu en v2.

## Dépendance serveur

Le serveur doit exposer l'API de synchronisation (commit incluant `Akwaba.Sync` + contrôleurs API).
Définir une clé JWT robuste en production via la variable d'environnement `Jwt__Key` sur le conteneur
`gestionhotel-app`. Après mise à jour du schéma (`ModifieLe`), supprimer l'ancienne base
`data/akwaba.db` avant de relancer.
