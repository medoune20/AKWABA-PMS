#!/usr/bin/env bash
# Déploiement AKWABA sur /opt/lp2m — à exécuter sur le serveur (Ubuntu/Debian), en sudo.
set -euo pipefail

APP_DIR=/opt/lp2m
REPO_DIR=/opt/lp2m-src
REPO_URL=https://github.com/medoune20/AKWABA-PMS.git

echo ">>> 1. Récupération / mise à jour du code"
if [ -d "$REPO_DIR/.git" ]; then
  git -C "$REPO_DIR" fetch --all && git -C "$REPO_DIR" reset --hard origin/main
else
  git clone "$REPO_URL" "$REPO_DIR"
fi

echo ">>> 2. Publication (Release) vers $APP_DIR"
mkdir -p "$APP_DIR" "$APP_DIR/data" "$APP_DIR/keys"
dotnet publish "$REPO_DIR/src/Akwaba.Web/Akwaba.Web.csproj" -c Release -o "$APP_DIR"

echo ">>> 3. Droits pour le service (www-data)"
chown -R www-data:www-data "$APP_DIR"

echo ">>> 4. Service systemd"
cp "$REPO_DIR/deploy/akwaba.service" /etc/systemd/system/akwaba.service
systemctl daemon-reload
systemctl enable akwaba
systemctl restart akwaba
sleep 2
systemctl --no-pager status akwaba | head -n 12

echo ">>> Terminé. Test local :"
echo "    curl -I http://127.0.0.1:5000"
