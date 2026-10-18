# Wiimote4Guns Changelog

## v3.1.6.1

### [EN] W4G_Setup.exe — standalone installer
- Standalone W4G_Setup.exe installer - Setup update reliability : WiimoteGun.exe is closed before extracting (a running app locks its own files), the running WiimoteGun.Service.exe is never overwritten (the service is updated by its own elevated path), files in use are skipped and logged instead of aborting the install half-way

### [EN] Off-Screen Reload — now per player
- The Off-Screen Reload checkbox and the Trigger/Auto dropdown are now PER PLAYER, even on the default profile
- Each player overrides the global; changing the global in Options > Gestures re-applies it to every player
- The per-player choice survives profile reloads (auto default load, game start, manual load)

### [EN] Reload Rumble — now per player
- Reload Rumble, its intensity and its style are now per player too, with the same rules

### [FR] W4G_Setup.exe — installeur autonome
- Fiabilité de la mise à jour par le setup : WiimoteGun.exe est fermé avant l'extraction (une app en cours verrouille ses propres fichiers), le WiimoteGun.Service.exe en cours n'est jamais écrasé (le service est mis à jour par sa propre voie élevée), les fichiers utilisés sont ignorés et tracés au lieu d'annuler l'installation à mi-chemin

### [FR] Reload hors écran — désormais par joueur
- La case Off-Screen Reload et la liste Trigger/Auto sont désormais PAR JOUEUR, même sur le profil par défaut
- Chaque joueur surcharge le global ; un changement du global dans Options > Gestures le réapplique à tous les joueurs
- Le choix par joueur survit aux rechargements de profil (chargement auto du default, game start, chargement manuel)

### [FR] Vibration de rechargement — désormais par joueur
- La vibration de rechargement, son intensité et son style sont désormais par joueur aussi, avec les mêmes règles

---

## v3.1.6.0

### [EN] Two Wiimote Bars (Or Blacom) layout
- This layout was previously removed due to issues with the calibration mechanism. Re-introduced now, it works better and allows pairing with a Blacom in a two-bar layout by switching the Wiimote to Player 2. In my opinion, the accuracy here is no better than with a single sensor bar. To be tested...
- Hybrid-PnP mode available.
- Fisheye or wide-angle lens recommended.

### [EN] Hybrid PnP checkboxes
- "Hybrid PnP (alpha test)" on Wiimote Bar and Two Wiimote Bars
- Distance and tilt compensation (opt-in, per layout)
- Declared LED geometry: accurate metric distance + precision display
- Live gun-to-screen distance before and after calibration

### [EN] Native updater (update_W4G.exe)
- Replaces the PowerShell script, no PowerShell dependency
- Proper service staging (running service files staged, applied at next start)
- Legacy WiimoteUpdate.ps1 cleanup on first run
- Self-update of the updater via rename trick

### [EN] Setup Wizard first-run
- Automatic .NET 10 Runtime download + silent install on START
- UMDF2 driver activation triggered directly from the wizard
- Progress dialog during runtime install, HmHost startup notice
- Windows reboot advice if tracking does not activate after 10s

### [EN] Application
- Welcome dialog survives the UMDF2 restart, shows on first real start
- "? Help" button on the home page (top-left, zoom-aware)
- Changelog dialog after each update: FR/EN toggle, GitHub link, local markdown

### [FR] Two Wiimote Bars (Or Blacom)
- Ce layout avait été retiré en raison de difficultés avec le mécanisme de calibration. De nouveau proposé, il fonctionne mieux et permet de le coupler avec un Blacom en layout deux barres, en passant la Wiimote en joueur 2. La précision ici, selon moi, n'est pas meilleure qu'avec une seule sensor bar. À tester...
- Mode Hybride-PnP disponible.
- Lentille fisheye ou grand-angle conseillée.

### [FR] Cases Hybrid PnP
- « Hybrid PnP (alpha test) » sur Wiimote Bar et Two Wiimote Bars
- Compensation distance/inclinaison (optionnel, par layout)
- Géométrie LED déclarée : distance métrique exacte + précision
- Distance gun-écran en direct avant et après la calibration

### [FR] Updateur natif (update_W4G.exe)
- Remplace le script PowerShell, sans dépendance PowerShell
- Staging service correct (fichiers service stagés, appliqués au prochain démarrage)
- Suppression du script WiimoteUpdate.ps1 legacy au premier lancement
- Auto-mise à jour de l'updateur via le rename trick

### [FR] Setup Wizard premier lancement
- Téléchargement + installation silencieuse du Runtime .NET 10 au START
- Activation du pilote UMDF2 déclenchée directement depuis le wizard
- Dialogue de progression pendant l'installation, message démarrage HmHost
- Conseil de redémarrage Windows si le tracking ne s'active pas après 10s

### [FR] Application
- Le WelcomeDialog survit au restart UMDF2, s'affiche au premier vrai démarrage
- Bouton « ? Help » sur la page d'accueil (coin haut gauche, zoom-aware)
- Dialogue changelog après chaque mise à jour : toggle FR/EN, lien GitHub, markdown local

---

## v3.1.5.10

### [EN] Native updater
- update_W4G.exe is a standalone native executable (no PowerShell)
- Waits for the app to exit, downloads and extracts the release archive
- Service files staged to update_service, applied at next app start
- Version regex back to v<digits> (no dot after v)

### [FR] Updateur natif
- update_W4G.exe est un exécutable natif autonome (sans PowerShell)
- Attend la sortie de l'app, télécharge et extrait l'archive de release
- Fichiers service stagés dans update_service, appliqués au prochain démarrage
- Regex de version de retour à v<chiffres> (sans point après le v)

---

## v3.1.0.0

### [EN] New RawInput (UMDF2) driver
- Replaces the legacy vmulti kernel driver (installed silently, no reboot)
- Requires the free .NET Runtime 10 x64 (checked and prompted at activation)
- vmulti stays optional for Windows 10 / Win11 below 26H02
- Virtual GamePads per player with dedicated names (P1-P4)
- Setup Wizard opens once after updating from a previous version
- Global UI zoom +/- on the home page (80-150%)
- Misc fixes: mouse hidden in GamePad mode, no stuck button, reboots keep UMDF2

### [FR] Nouveau pilote RawInput (UMDF2)
- Remplace le pilote kernel vmulti legacy (installé silencieusement, sans reboot)
- Nécessite le Runtime .NET 10 x64 gratuit (vérifié et proposé à l'activation)
- vmulti reste optionnel pour Windows 10 / Win11 antérieur à 26H02
- Manettes virtuelles par joueur avec noms dédiés (P1-P4)
- Le Setup Wizard s'ouvre une fois après une mise à jour depuis une version antérieure
- Zoom UI global +/- sur la page d'accueil (80-150%)
- Corrections diverses : souris masquée en mode GamePad, pas de bouton bloqué, les reboots conservent l'UMDF2

---

## v3.0.0.24

### [EN]
- UMDF2 default input mode with service-managed installation
- Virtual gamepads visible as real controllers in Windows
- UI zoom global -/+ on the home page (80-150%)
- Setup Wizard: service required, VMulti optional with opt-in checkbox
- Automatic es_input.cfg registration for EmulationStation

### [FR]
- Mode d'entrée UMDF2 par défaut avec installation gérée par le service
- Manettes virtuelles visibles comme de vraies manettes dans Windows
- Zoom UI global -/+ sur la page d'accueil (80-150%)
- Setup Wizard : service requis, VMulti optionnel avec case d'adhésion
- Enregistrement automatique de es_input.cfg pour EmulationStation

---

## v3.0.0.23

### [EN]
- Dedicated GamePad names (GamePad Wiimote4Guns P1-P4)
- Dolphin profile auto-patching by input mode
- Keyboard-only mode hides the mouse (parity with VMulti COL03)
- UMDF2 mode persists across reboots (HKLM flag + service guard)
- XInput support (native XUSB + mark-and-poll slot detection)

### [FR]
- Noms de manettes dédiés (GamePad Wiimote4Guns P1-P4)
- Patch automatique des profils Dolphin par mode d'entrée
- Mode clavier seul masque la souris (parité avec VMulti COL03)
- Le mode UMDF2 persiste aux reboots (flag HKLM + garde du service)
- Support XInput (XUSB natif + détection de slot mark-and-poll)

---

## v3.0.0.21

### [EN]
- Auto-update via WiimoteUpdate.ps1 (download, extract, apply)
- Service self-update through UpdateService.ps1 (admin elevation)
- Configuration settings persisted in settings.cfg

### [FR]
- Mise à jour auto via WiimoteUpdate.ps1 (téléchargement, extraction, application)
- Auto-mise à jour du service via UpdateService.ps1 (élévation admin)
- Paramètres de configuration persistés dans settings.cfg
