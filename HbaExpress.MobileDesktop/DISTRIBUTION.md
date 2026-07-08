# HbaExpress Pro — Lancer sur téléphone & distribuer en test

L'app pointe sur le BFF **staging** (`MauiProgram.cs → ApiBaseUrl`). Pour une
vraie prod, change cette URL avant de publier.

---

## 1. Lancer sur un téléphone (dev)

### Android (le plus simple)
1. Sur le téléphone : *Paramètres → À propos → appuyer 7× sur « Numéro de build »*
   pour activer les **Options développeur**, puis active **Débogage USB**.
2. Branche le téléphone en USB, autorise l'ordinateur.
3. Vérifie qu'il est vu : `adb devices` (adb est dans
   `~/Library/Developer/Xamarin/android-sdk-macosx/platform-tools/`).
4. Lance :
   ```bash
   dotnet build HbaExpress.MobileDesktop -f net9.0-android -t:Run
   ```

### iPhone (nécessite un compte Apple Developer)
1. Branche l'iPhone, ouvre **Xcode** une fois pour qu'il « prépare » l'appareil.
2. Renseigne ton **Team de signature** (Apple ID) — voir §3.
3. Lance :
   ```bash
   dotnet build HbaExpress.MobileDesktop -f net9.0-ios -t:Run \
     -p:RuntimeIdentifier=ios-arm64
   ```

---

## 2. Distribuer en TEST — Android

### Option A — APK partagé directement (le plus rapide)
1. Créer une clé de signature (une seule fois) :
   ```bash
   keytool -genkeypair -v -keystore hbaexpress.keystore -alias hba \
     -keyalg RSA -keysize 2048 -validity 10000
   ```
2. Générer l'APK signé :
   ```bash
   dotnet publish HbaExpress.MobileDesktop -f net9.0-android -c Release \
     -p:AndroidKeyStore=true \
     -p:AndroidSigningKeyStore=hbaexpress.keystore \
     -p:AndroidSigningKeyAlias=hba \
     -p:AndroidSigningStorePass=TON_MDP \
     -p:AndroidSigningKeyPass=TON_MDP
   ```
   → APK dans `bin/Release/net9.0-android/publish/*-Signed.apk`.
3. Envoie l'APK aux testeurs (WhatsApp, e-mail, lien). Sur le téléphone :
   autoriser « Installer des applications inconnues », puis ouvrir l'APK.

### Option B — Google Play, piste de test interne (propre, jusqu'à 100 testeurs)
1. Compte **Google Play Console** (25 $ une fois).
2. Générer un **.aab** (même commande que ci-dessus, Play préfère l'AAB) :
   le publish produit aussi `*-Signed.aab`.
3. Play Console → créer l'app → **Tests → Test interne** → uploader l'AAB →
   ajouter les e-mails des testeurs → partager le **lien d'installation**.

### Option C — Firebase App Distribution (beta simple, Android + iOS)
- Crée un projet Firebase, installe le CLI, puis :
  ```bash
  firebase appdistribution:distribute chemin/vers/app-Signed.apk \
    --app <APP_ID_FIREBASE> --groups "testeurs"
  ```
- Les testeurs reçoivent un e-mail avec le lien d'installation.

---

## 3. Distribuer en TEST — iOS (TestFlight)

Prérequis : **Apple Developer Program** (99 $/an).

1. Dans **App Store Connect**, crée l'app (Bundle ID = `fr.hbamarketplace.seller`).
2. Configure la signature de distribution (certificat + profil) — le plus simple
   est de laisser **Xcode** gérer la signature automatique (Automatically manage
   signing) avec ton Team.
3. Génère l'archive `.ipa` :
   ```bash
   dotnet publish HbaExpress.MobileDesktop -f net9.0-ios -c Release \
     -p:RuntimeIdentifier=ios-arm64 \
     -p:CodesignKey="Apple Distribution: TON NOM (TEAMID)" \
     -p:CodesignProvision="Nom du profil de provisioning"
   ```
   → `.ipa` dans `bin/Release/net9.0-ios/publish/`.
4. Envoie l'`.ipa` à App Store Connect avec **Transporter** (app Mac gratuite) ou :
   ```bash
   xcrun altool --upload-app -f *.ipa -t ios \
     --apiKey <KEY_ID> --apiIssuer <ISSUER_ID>
   ```
5. App Store Connect → **TestFlight** → ajouter des testeurs (internes = ton
   équipe, ou externes via un lien public après une brève revue Apple).

> Sans compte payant : tu peux installer sur **tes propres appareils** en
> signature « développeur » (ad-hoc), mais pas partager largement.

---

## 4. Avant de publier

- Remplace l'icône/splash provisoires (`Resources/AppIcon`, `Resources/Splash`,
  actuellement un éclair vert) par le vrai logo HbaExpress.
- Vérifie `ApiBaseUrl` (staging vs prod) dans `MauiProgram.cs`.
- Incrémente `ApplicationDisplayVersion` / `ApplicationVersion` (csproj) à chaque
  build de test.
