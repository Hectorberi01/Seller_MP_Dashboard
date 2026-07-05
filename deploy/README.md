# Déploiement — Dashboard vendeur HbaExpress Pro

Application Blazor WebAssembly (statique) servie par **Caddy** avec HTTPS
automatique, sur le domaine **web-seller.hba-marketplace.fr**.

## Contenu

- `Dockerfile` — build multi-stage : compile le WASM (SDK .NET 9) puis sert les
  fichiers statiques via Caddy.
- `Caddyfile` — configuration Caddy (domaine, HTTPS Let's Encrypt, fallback SPA,
  cache des assets, en-têtes de sécurité).
- `docker-compose.yml` — un service `web-seller` exposé en 80/443, certificats
  persistés dans des volumes.
- `ansible/` — déploiement automatisé sur le VPS `193.168.145.162`.

## Prérequis

1. Un enregistrement **DNS A** : `web-seller.hba-marketplace.fr` → `193.168.145.162`.
2. Ports **80** et **443** ouverts sur le VPS (nécessaires au challenge ACME).
3. Accès SSH root au VPS et, en local, `ansible` + `rsync`.

## Déploiement via Ansible (recommandé)

```bash
cd deploy/ansible
ansible-galaxy collection install -r requirements.yml   # ansible.posix (une fois)
ansible-playbook site.yml
```

Le playbook installe Docker, synchronise les sources, construit l'image et
démarre le conteneur. Caddy émet ensuite le certificat TLS automatiquement.

## Déploiement manuel (sur le VPS)

```bash
cd /opt/web-seller          # ou le dossier où sont copiées les sources
docker compose -f deploy/docker-compose.yml up -d --build
```

## Notes

- L'URL du BFF appelée par le dashboard est définie dans
  `wwwroot/appsettings.json` (`Api:BaseUrl`). Assurez-vous que ce BFF autorise
  l'origine `https://web-seller.hba-marketplace.fr` (CORS).
- Adresse e-mail ACME : modifiable dans `Caddyfile` (bloc global `email`).
- Pour changer de domaine : éditer `Caddyfile` (bloc de site) et relancer.
