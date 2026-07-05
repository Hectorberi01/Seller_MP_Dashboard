# syntax=docker/dockerfile:1

# ============================================================
#  Étape 1 — Build du dashboard Blazor WebAssembly (net9.0)
# ============================================================
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Restauration en couche cachée : ne se relance que si le .csproj change.
COPY Seller_MP_Dashboard.csproj ./
RUN dotnet restore Seller_MP_Dashboard.csproj

# Copie du reste des sources puis publication (sortie statique optimisée).
COPY . .
RUN dotnet publish Seller_MP_Dashboard.csproj -c Release -o /app/publish

# ============================================================
#  Étape 2 — Service statique via Caddy (HTTPS automatique)
# ============================================================
FROM caddy:2-alpine

# Fichiers publiés (index.html, _framework/, css/, img/, lib/…).
COPY --from=build /app/publish/wwwroot /srv

# Config Caddy interne (sert /srv en HTTP sur :80, derrière le Caddy frontal).
COPY deploy/Caddyfile /etc/caddy/Caddyfile

EXPOSE 80
