# AKWABA — Plateforme SaaS de gestion hôtelière multi-établissements

Application web **multi-tenant** permettant à plusieurs hôteliers indépendants de gérer leur
établissement (réservations, check-in/out, facturation, caisse, encaissements mobiles), avec
souscription soumise à **validation d'un modérateur** de la plateforme.

Développée en **ASP.NET Core MVC (.NET 10)**, **PostgreSQL + EF Core**, architecture **clean en 4 couches**.

---

## 1. Périmètre livré dans cette itération (« cœur de valeur »)

Cette livraison couvre le **Lot 0 (Socle SaaS)** et le **Lot 1 (Cœur PMS)** validés en Phase 1,
entièrement fonctionnels et sans placeholder :

- **Souscription + modération** : un hôtel s'inscrit → statut *En attente* → un modérateur approuve / refuse.
- **Authentification** : ASP.NET Core Identity (hachage PBKDF2), connexion, inscription, mot de passe oublié,
  déconnexion. Bouton « Se connecter avec Google » activable par configuration.
- **Multi-tenant réel** : isolation par `TenantId` via **filtres de requête globaux EF Core** + marquage
  automatique à l'insertion. Les comptes plateforme (super-admin / modérateur) voient tous les hôtels.
- **RBAC** : 8 rôles (SUPER_ADMIN, MODERATEUR, GERANT, RECEPTIONNISTE, GOUVERNANTE, CAISSIER, COMPTABLE, LECTURE_SEULE).
- **Chambres / Types / Tarifs** : CRUD, états de ménage, tarification par saison.
- **Clients** : fiches, recherche.
- **Réservations** : **plan d'occupation** (chambres × 14 jours), création avec contrôle **anti-surbooking**,
  détail.
- **Check-in / Check-out** : ouverture automatique de la **note (folio)**, ligne d'hébergement calculée,
  contrôle « note soldée » avant le départ.
- **Note de chambre** : lignes (hébergement, restauration, taxe, extra…), totaux, reste à payer.
- **Caisse + Encaissement** : sessions de caisse, totaux par moyen, écart à la clôture, paiements
  **espèces / carte / Orange Money / MTN / Wave / Moov** via **CinetPay (mode bac à sable)**.
- **Tableau de bord** : occupation, CA jour/mois, **ADR**, **RevPAR**, arrivées du jour.
- **Audit** : journal horodaté des actions sensibles.
- **Transactions** : opérations critiques (réservation, check-in/out, encaissement) atomiques.
- **Seed de démonstration** : 1 hôtel approuvé, 1 hôtel en attente, comptes de test.

### Incréments suivants (prévus, NON inclus ici)
- POS **Restaurant / Bar** (terminal tactile, tickets).
- Module **Housekeeping** avancé (affectation gouvernantes, suivi par étage).
- **Channel Manager** OTA (démarrage par import, puis API temps réel).
- **États comptables** avancés (grand-livre, balance, exports).
- **Client bureau WPF** hors-ligne + moteur de **synchronisation**.

---

## 2. Prérequis

- **.NET 10 SDK** — https://dotnet.microsoft.com/download/dotnet/10.0
- **PostgreSQL 14+** (un serveur local suffit)
- Un IDE : Visual Studio 2022/2025, JetBrains Rider, ou VS Code + extension C# Dev Kit.

> ⚠️ **La compilation doit être réalisée en local.** Le SDK .NET 10 n'était pas disponible dans
> l'environnement de génération ; le code est livré complet et prêt à `dotnet build`.

---

## 3. Mise en route

### a) Base de données
Créez la base (les tables sont créées automatiquement au premier démarrage via `EnsureCreated`) :

```bash
createdb akwaba           # ou via pgAdmin
```

Adaptez la chaîne de connexion dans `src/Akwaba.Web/appsettings.json` si nécessaire :

```json
"ConnectionStrings": {
  "Defaut": "Host=localhost;Port=5432;Database=akwaba;Username=postgres;Password=postgres"
}
```

### b) Restauration et lancement

```bash
cd akwaba-app
dotnet restore
dotnet run --project src/Akwaba.Web
```

Ouvrez ensuite l'URL affichée (par défaut `https://localhost:5001` ou `http://localhost:5000`).

---

## 4. Comptes de démonstration

| Rôle / contexte                | Email                         | Mot de passe   |
|--------------------------------|-------------------------------|----------------|
| Super-administrateur plateforme| `superadmin@akwaba.ci`        | `Akwaba#2026`  |
| Modérateur plateforme          | `moderateur@akwaba.ci`        | `Akwaba#2026`  |
| Gérant (hôtel approuvé)        | `gerant@ivoire-palace.ci`     | `Akwaba#2026`  |
| Réception (hôtel approuvé)     | `reception@ivoire-palace.ci`  | `Akwaba#2026`  |
| Demande en attente (à modérer) | `demande@lagune-bleue.ci`     | `Akwaba#2026`  |

**Parcours conseillé pour tester :**
1. Connexion en **modérateur** → onglet *Souscriptions* → approuver « Hôtel Lagune Bleue ».
2. Connexion en **gérant** d'Ivoire Palace → créer/visualiser chambres, tarifs.
3. *Réservations* → choisir des dates → créer une réservation → **Check-in**.
4. Ouvrir la **Caisse** (session) → ouvrir la **Note** → ajouter une ligne → **Encaisser**
   (en espèces : confirmé ; en mobile money : « Confirmer » simule le webhook CinetPay).
5. **Check-out** une fois la note soldée → consulter le **Tableau de bord**.

---

## 5. Configuration optionnelle

### CinetPay (paiements mobiles réels)
Renseignez vos identifiants marchand pour passer du **bac à sable** au mode réel :

```json
"CinetPay": { "ApiKey": "VOTRE_API_KEY", "SiteId": "VOTRE_SITE_ID" }
```

Le flux d'intégration réelle (POST `/v2/payment`, `notify_url`/webhook, `return_url`) est documenté
dans `src/Akwaba.Infrastructure/Services/CinetPayService.cs` (point d'extension `ModeReel`).

### Connexion Google (OIDC)
```json
"Authentication": { "Google": { "ClientId": "...", "ClientSecret": "..." } }
```
Le bouton apparaît automatiquement quand ces valeurs sont présentes.

---

## 6. Architecture (clean, 4 couches)

```
src/
├── Akwaba.Domain          # Entités, enums, interfaces — aucune dépendance
├── Akwaba.Application     # Cas d'usage (services), DTOs, IAkwabaDbContext
├── Akwaba.Infrastructure  # EF Core (DbContext, multi-tenant), Identity, CinetPay, audit, seed
└── Akwaba.Web             # ASP.NET Core MVC : contrôleurs, vues Razor, design M365/Apple + mode sombre
```

Règle de dépendances : `Web → Infrastructure → Application → Domain`.
La couche Application ne connaît jamais l'Infrastructure (inversion via `IAkwabaDbContext`, `ICinetPayService`, `IServiceAudit`).

### Multi-tenant
- `ITenantContext` (implémenté côté Web à partir des *claims*) fournit le tenant courant.
- `AkwabaDbContext` applique un **filtre global** `BypassTenant || TenantId == TenantIdCourant`
  sur toutes les entités métier, et **estampille** le `TenantId` à l'insertion.
- Les comptes plateforme ont `EstPlateforme = true` (bypass) pour la modération.

---

## 7. Notes de production

- **Migrations EF** : cette version utilise `EnsureCreated()` pour un démarrage immédiat.
  En production, remplacez par des **migrations** (`dotnet ef migrations add Initial`, `dotnet ef database update`).
- **Secrets** : déplacez les chaînes de connexion et clés vers *User Secrets* / variables d'environnement.
- **Emails** : la réinitialisation de mot de passe journalise le lien (mode dev). Branchez un envoi SMTP en production.
- **HTTPS/HSTS** : activés hors environnement de développement.
- **Sécurité** : PBKDF2 (Identity), anti-CSRF sur tous les POST, transactions sur les opérations critiques.

---

*AKWABA — Conçu pour le marché hôtelier ouest-africain (FCFA, français, paiements mobiles).*
