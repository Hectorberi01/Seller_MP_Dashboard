# syntax=docker/dockerfile:1

# ============================================================
#  Étape 1 — Build de l'hôte Web (HbaExpress.Web + RCL HbaExpress.Shared)
# ============================================================
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Restauration en couche cachée : Web + RCL partagée (le projet MAUI est ignoré
# via .dockerignore car il n'est pas requis pour le déploiement web).
COPY HbaExpress.Web/HbaExpress.Web.csproj HbaExpress.Web/
COPY HbaExpress.Shared/HbaExpress.Shared.csproj HbaExpress.Shared/
RUN dotnet restore HbaExpress.Web/HbaExpress.Web.csproj

# Copie du reste des sources puis publication (sortie statique optimisée).
COPY . .
RUN dotnet publish HbaExpress.Web/HbaExpress.Web.csproj -c Release -o /app/publish

# ============================================================
#  Étape 2 — Service statique via Caddy (HTTPS automatique)
# ============================================================
FROM caddy:2-alpine

# Fichiers publiés (index.html, _framework/, css/, img/, lib/…).
COPY --from=build /app/publish/wwwroot /srv

# Config Caddy interne (sert /srv en HTTP sur :80, derrière le Caddy frontal).
COPY deploy/Caddyfile /etc/caddy/Caddyfile

EXPOSE 80
