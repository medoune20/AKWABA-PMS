# Déploiement AKWABA sur lp2medoune.com (`/opt/lp2m`)

Cible : serveur **Ubuntu/Debian** avec accès `sudo`, domaine **lp2medoune.com** pointant déjà
sur l'IP du serveur (enregistrement DNS A/AAAA). L'app tourne en service `systemd` sur le port
local **5000**, derrière **Nginx** qui assure le HTTPS.

---

## 1. Prérequis (une seule fois, sur le serveur)

```bash
# .NET 10 SDK (méthode Microsoft pour Ubuntu)
sudo apt-get update
sudo apt-get install -y dotnet-sdk-10.0 git nginx
# Si le paquet dotnet-sdk-10.0 n'est pas trouvé, utiliser le script officiel :
#   wget https://dot.net/v1/dotnet-install.sh -O dotnet-install.sh
#   sudo bash dotnet-install.sh --channel 10.0 --install-dir /usr/share/dotnet
#   sudo ln -sf /usr/share/dotnet/dotnet /usr/bin/dotnet
dotnet --version   # doit afficher 10.x
```

## 2. Déploiement applicatif (script fourni)

Le script clone le dépôt, publie en Release vers `/opt/lp2m`, installe le service et le démarre.

```bash
sudo mkdir -p /opt/lp2m
# récupère deploy.sh (via le dépôt) puis :
sudo bash /opt/lp2m-src/deploy/deploy.sh
```

> Première fois, le dépôt n'existe pas encore localement. Soit tu le clones d'abord
> (`sudo git clone https://github.com/medoune20/AKWABA-PMS.git /opt/lp2m-src`) puis tu lances
> `sudo bash /opt/lp2m-src/deploy/deploy.sh`, soit tu copies `deploy.sh` à la main et tu l'exécutes.

Vérifie que l'app répond en local :
```bash
curl -I http://127.0.0.1:5000
```
Tu dois obtenir un `HTTP/1.1 302` (redirection vers la page de connexion) — c'est normal.

## 3. Nginx (reverse proxy)

```bash
sudo cp /opt/lp2m-src/deploy/nginx-lp2medoune.conf /etc/nginx/sites-available/lp2medoune
sudo ln -sf /etc/nginx/sites-available/lp2medoune /etc/nginx/sites-enabled/lp2medoune
sudo nginx -t && sudo systemctl reload nginx
```
Le site doit déjà répondre en HTTP : http://lp2medoune.com

## 4. HTTPS (Let's Encrypt)

```bash
sudo apt-get install -y certbot python3-certbot-nginx
sudo certbot --nginx -d lp2medoune.com -d www.lp2medoune.com
```
Certbot adapte automatiquement la config Nginx pour le 443 + redirection 80→443.
Le renouvellement est automatique (timer systemd).

## 5. Vérifications

- https://lp2medoune.com → page de connexion AKWABA.
- Connexion de démo : `gerant@ivoire-palace.ci` / `Akwaba#2026`.
- Logs en direct : `sudo journalctl -u akwaba -f`
- Redémarrer : `sudo systemctl restart akwaba`

---

## Mises à jour ultérieures

Pour déployer une nouvelle version après un `git push` :
```bash
sudo bash /opt/lp2m-src/deploy/deploy.sh
```
(le script refait fetch + reset + publish + restart)

---

## Notes importantes

- **Base de données** : SQLite par défaut (`/opt/lp2m/data/akwaba.db`), créée au premier
  démarrage avec les données de démo. Pour un usage multi-utilisateurs intensif, passer à
  **PostgreSQL** (voir le README principal, section « Passer à PostgreSQL »).
- **Comptes de démo** : pense à les supprimer ou changer leurs mots de passe avant une mise en
  production réelle (ils sont créés par le seed).
- **Secrets** : renseigne `CinetPay` et `Authentication:Google` via
  `/opt/lp2m/appsettings.Production.json` (déployé avec l'app) ou des variables d'environnement
  dans le fichier `akwaba.service`. Ne mets jamais de vraies clés dans le dépôt Git.
- **Clés Data Protection** : persistées dans `/opt/lp2m/keys` (cookies stables après redémarrage) ;
  ce dossier appartient à `www-data` et ne doit pas être supprimé.
- **Pare-feu** : ouvre les ports 80 et 443 (`sudo ufw allow 'Nginx Full'`).

## Alternative : Docker

Un `Dockerfile` est fourni à la racine de `deploy/`. Build & run :
```bash
docker build -t akwaba -f deploy/Dockerfile .
docker run -d --name akwaba -p 127.0.0.1:5000:5000 \
  -v /opt/lp2m/data:/app/data -v /opt/lp2m/keys:/app/keys \
  -e ASPNETCORE_ENVIRONMENT=Production akwaba
```
Puis le même Nginx en reverse proxy vers `127.0.0.1:5000`.
