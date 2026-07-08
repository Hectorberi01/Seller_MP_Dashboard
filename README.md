# HbaExpress Pro — Espace Vendeur

Interface Blazor **partagée** entre le **Web**, le **mobile** et le **desktop**,
selon le modèle recommandé par Microsoft (Razor Class Library + hôtes).

```
HbaExpress.sln
├── HbaExpress.Shared            (RCL — toute l'UI + services communs)
│   ├── App.razor, _Imports.razor
│   ├── Pages/ Components/ Layout/
│   ├── Api/ Models/ Services/   → HbaExpressServiceCollectionExtensions.AddSellerDashboard()
│   └── wwwroot/ (css, js, img)  → exposé sous _content/HbaExpress.Shared/
│
├── HbaExpress.Web               (Blazor WebAssembly — hôte web)
│   ├── Program.cs               → AddSellerDashboard(baseUrl)
│   └── wwwroot/ (index.html, appsettings.json)
│
└── HbaExpress.MobileDesktop     (.NET MAUI Blazor Hybrid — Android/iOS/macOS/Windows)
    ├── MauiProgram.cs           → AddSellerDashboard(ApiBaseUrl)
    ├── App.xaml, MainPage.xaml  → BlazorWebView hébergeant le App partagé
    └── Platforms/ (Android, iOS, MacCatalyst, Windows)
```

> Le `RootNamespace` de la RCL est resté `Seller_MP_Dashboard` (pas de réécriture
> des espaces de noms). Les services (Auth, HttpClient+Bearer, Toasts) sont
> enregistrés une seule fois via `AddSellerDashboard(...)`, appelé par les deux hôtes.

---

## 1. Prérequis

- **.NET SDK 9.0** — installer via l'**installeur officiel** Microsoft
  (https://dotnet.microsoft.com/download/dotnet/9.0), **pas** via Homebrew
  (Homebrew ne fournit pas les workloads MAUI).
- Mobile/desktop : `dotnet workload install maui`
  - **Android** : SDK Android (installable via `dotnet build -t:InstallAndroidDependencies` ou Android Studio) + un émulateur ou un appareil.
  - **iOS / macCatalyst** : **Xcode** — la version doit correspondre à celle exigée
    par le SDK .NET iOS installé (ex. SDK `26.5` ⇒ Xcode `26.5`). Vérifier avec
    `xcodebuild -version` ; changer de version avec `sudo xcode-select -s /Applications/Xcode-XX.app`.
  - **Windows** : build sur une machine Windows (Visual Studio + charge MAUI).

---

## 2. Lancer en développement

### Web (WebAssembly)
```bash
dotnet run --project HbaExpress.Web
```

### Desktop / mobile (MAUI) — **cibler le projet MAUI**
```bash
# macOS desktop (Mac Catalyst) — nécessite le bon Xcode
dotnet build HbaExpress.MobileDesktop -f net9.0-maccatalyst -t:Run

# Simulateur iOS (macOS + Xcode)
dotnet build HbaExpress.MobileDesktop -f net9.0-ios -t:Run

# Android (émulateur lancé ou appareil branché en débogage USB)
dotnet build HbaExpress.MobileDesktop -f net9.0-android -t:Run

# Windows (sur une machine Windows)
dotnet build HbaExpress.MobileDesktop -f net9.0-windows10.0.19041.0 -t:Run
```

> **Ne pas** lancer `dotnet build/publish -f net9.0-ios` sans préciser le projet :
> il tenterait de compiler l'hôte Web (browser-wasm) pour iOS et échouerait.

### Android — installer le SDK si manquant (erreur XA5207)
```bash
dotnet build HbaExpress.MobileDesktop -t:InstallAndroidDependencies -f net9.0-android \
  -p:AndroidSdkDirectory=$HOME/Library/Developer/Xamarin/android-sdk-macosx \
  -p:AcceptAndroidSDKLicenses=True
```

---

## 3. Configuration de l'URL du BFF

- **Web** : `HbaExpress.Web/wwwroot/appsettings.json` → `Api:BaseUrl`.
- **MAUI** : constante `ApiBaseUrl` dans `HbaExpress.MobileDesktop/MauiProgram.cs`.

En Hybrid, les appels partent du **HttpClient .NET natif** → **aucune contrainte
CORS**. En Web (WASM), le BFF doit autoriser l'origine du site en **CORS**.

---

## 4. Déploiement Web

Le dossier `deploy/` construit `HbaExpress.Web` (RCL incluse) et le sert en statique
via **Caddy**, derrière le Caddy frontal du VPS. Voir `deploy/README.md`.

```bash
cd deploy/ansible
ansible-galaxy collection install -r requirements.yml   # une fois
ansible-playbook site.yml
```

Prérequis : DNS `web-seller.hba-marketplace.fr` → IP du VPS, ports 80/443 ouverts,
et le domaine ajouté au Caddy frontal (`reverse_proxy web-seller:80`).

---

## 5. Déploiement / distribution mobile & desktop

Résumé ci-dessous ; procédure détaillée (test/beta) dans
[`HbaExpress.MobileDesktop/DISTRIBUTION.md`](HbaExpress.MobileDesktop/DISTRIBUTION.md).

### Android
```bash
# Clé de signature (une fois)
keytool -genkeypair -v -keystore hbaexpress.keystore -alias hba \
  -keyalg RSA -keysize 2048 -validity 10000

# APK/AAB signé
dotnet publish HbaExpress.MobileDesktop -f net9.0-android -c Release \
  -p:AndroidKeyStore=true -p:AndroidSigningKeyStore=hbaexpress.keystore \
  -p:AndroidSigningKeyAlias=hba \
  -p:AndroidSigningStorePass=MDP -p:AndroidSigningKeyPass=MDP
```
Sortie : `HbaExpress.MobileDesktop/bin/Release/net9.0-android/publish/*-Signed.apk` (et `.aab`).
- **Test rapide** : partager l'APK (les testeurs autorisent « sources inconnues »).
- **Play Console** (test interne) ou **Firebase App Distribution** : uploader l'AAB/APK.

### iOS (TestFlight) — compte **Apple Developer** requis
```bash
dotnet publish HbaExpress.MobileDesktop -f net9.0-ios -c Release \
  -p:RuntimeIdentifier=ios-arm64 -p:BuildIpa=true \
  -p:CodesignKey="Apple Distribution: NOM (TEAMID)" \
  -p:CodesignProvision="NOM_DU_PROFIL_AppStore"
```
Sortie : `HbaExpress.MobileDesktop/bin/Release/net9.0-ios/ios-arm64/publish/*.ipa`.
1. Créer l'App ID (`fr.hbamarketplace.seller`) et l'app dans **App Store Connect**.
2. Certificat **Apple Distribution** (`security find-identity -v -p codesigning` pour le nom exact) + **profil App Store**.
3. Uploader l'`.ipa` via **Transporter** → **TestFlight** → ajouter les testeurs.

### macOS (Mac Catalyst)
```bash
dotnet publish HbaExpress.MobileDesktop -f net9.0-maccatalyst -c Release
```
Distribution directe (`.app`/`.pkg` signés) ou App Store.

### Windows
Sur une machine Windows : package **MSIX** → Microsoft Store ou installation directe.

---

## 6. Notes & bonnes pratiques

- `dotnet build HbaExpress.sln` compile Shared + Web (le MAUI exige les workloads).
- Assets partagés servis sous `_content/HbaExpress.Shared/…` (css, js, img) —
  déjà référencés dans les `index.html` et les composants.
- Session : restaurée dans `App.razor` (Web + MAUI). Sur MAUI, `AuthState` est
  **Singleton** (partagé avec le handler HTTP) et les appels `localStorage` sont
  best-effort (session en mémoire si l'interop JS n'est pas disponible).
- **Icône/splash MAUI** : `Resources/AppIcon` et `Resources/Splash` sont des SVG
  provisoires (fond vert + éclair) — remplacer par le vrai logo avant publication.
- Incrémenter `ApplicationDisplayVersion` / `ApplicationVersion` (csproj MAUI) à
  chaque build de test/store.
- Le projet MAUI est exclu du build Docker (`.dockerignore`).
