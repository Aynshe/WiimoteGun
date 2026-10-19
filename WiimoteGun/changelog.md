# Wiimote4Guns Changelog

## v3.1.6.1

### [EN] W4G_Setup.exe — standalone installer
- Standalone W4G_Setup.exe installer - Setup update reliability : WiimoteGun.exe is closed before extracting, the running WiimoteGun.Service.exe is never overwritten (The service is updated at a later stage if necessary), files in use are skipped and logged instead of aborting the install half-way

### [EN] Off-Screen Reload — now per player
- The Off-Screen Reload checkbox and the Trigger/Auto dropdown are now PER PLAYER, even on the default profile
- Each player overrides the global; changing the global in Options > Gestures re-applies it to every player
- The per-player choice survives profile reloads (auto default load, game start, manual load)

### [EN] Reload Rumble — now per player
- Reload Rumble, its intensity and its style are now per player too, with the same rules

### [EN] AUTO Off-Screen Reload
- **AUTO Off-Screen Reload suppressed** when an excluded executable is in the foreground: `emulationstation.exe` (RetroBat) and `explorer.exe` (Windows). Custom executables can be added via **Options > Gestures** (file browser, enable checkbox, remove button, auto-save).
- *Reminder:* Auto Off-Screen Reload is useful for games/systems without native support. Enabling it per-game via a custom profile is recommended. If left enabled globally, it shouldn't cause conflicts as it simply triggers the `m_right` command. If an issue occurs on a specific game using the global "default" profile, you can disable in a custom profile. Note that off-screen vibration currently only works when this feature is active.

### [EN] UI Stability & Startup
- Fixed `WiimoteGun.exe` silently closing (~30-60s) when a startup dialog (Welcome / Changelog / Wizard) was opened: `CrashWatchdog` heartbeat now starts immediately after service registration so startup dialogs can stay open without triggering false restarts.

### [EN] Fix UMDF2 Information Popup Duplication
- Fixed the "UMDF2 first start" information popup (10s wait + Windows restart advice) appearing at EVERY app restart while a Wiimote was still connected: it was tied to the connect-time host re-activation guard (the Wiimote auto-reconnect beats the startup host activation). It now displays ONCE per installation via the persisted `Umdf2FirstStartNoticeShown` flag: the first stop/restart of `Wiimote4Guns` while a Wiimote is active shows the information one last time and sets the flag.

### [FR] W4G_Setup.exe — installeur autonome
- Fiabilité de la mise à jour par le setup : WiimoteGun.exe est fermé avant l'extraction, le WiimoteGun.Service.exe en cours n'est jamais écrasé (Le service est mis à jour ultérieurement, si nécessaire), les fichiers utilisés sont ignorés et tracés au lieu d'annuler l'installation à mi-chemin

### [FR] Reload hors écran — désormais par joueur
- La case Off-Screen Reload et la liste Trigger/Auto sont désormais PAR JOUEUR, même sur le profil par défaut
- Chaque joueur surcharge le global ; un changement du global dans Options > Gestures le réapplique à tous les joueurs
- Le choix par joueur survit aux rechargements de profil (chargement auto du default, game start, chargement manuel)

### [FR] Vibration de rechargement — désormais par joueur
- La vibration de rechargement, son intensité et son style sont désormais par joueur aussi, avec les mêmes règles

### [FR] Rechargement automatique hors écran
- **Désactivation du rechargement hors écran (AUTO Off-Screen)** lorsqu'un exécutable exclu est au premier plan : `emulationstation.exe` (RetroBat) et `explorer.exe` (Windows). Ajout d'exécutables personnalisés via **Options > Gestures** (sélecteur, case d'activation, suppression, sauvegarde auto).
- *Rappel :* Le rechargement hors écran automatique est utile pour les jeux/systèmes qui ne le proposent pas d'origine. Il est conseillé de l'activer au cas par cas via un profil personnalisé par jeu. Si vous le laissez activé en global, il ne devrait pas y avoir de conflit car il se contente de déclencher la commande `m_right`. En cas de défaut sur un jeu avec le profil global "default", vous pouvez le désactiver via un profil personnalisé. Notez aussi que la vibration hors écran fonctionne actuellement uniquement si cette fonction est active.

### [FR] Stabilité de l'UI & Démarrage
- Correction de la fermeture silencieuse de `WiimoteGun.exe` (~30-60s) lors de l'ouverture d'une fenêtre de démarrage (Bienvenue / Changelog / Assistant) : le heartbeat de `CrashWatchdog` démarre désormais immédiatement après l'enregistrement du service pour permettre aux fenêtres de rester ouvertes sans déclencher de faux redémarrages.

### [FR] Correction de l'affichage répétitif du pop-up UMDF2
- Correction de la boîte de dialogue d'information "UMDF2 premier démarrage" (attente de 10s + conseil de redémarrage Windows) qui s'affichait à chaque redémarrage de l'application si une Wiimote était connectée. Elle ne s'affiche désormais qu'une fois par installation via le flag `Umdf2FirstStartNoticeShown` : le premier arrêt/redémarrage de `Wiimote4Guns` avec une Wiimote active affiche le message une dernière fois et valide le flag.

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
