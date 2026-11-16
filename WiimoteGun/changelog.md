Wiimote4Guns Changelog

## v3.1.7.3

### [EN] ".remap" loading improvements
- **Profile auto-load (per-emulator mappings)**: a game-scoped mapping (game name sent by EmulationStation) is no longer rejected when the emulator exe is reached through a different path (symlink, other root) - the ES game name is the identity, the path is now only a scoring bonus. The stored path is automatically refreshed to the detected one on the first match (saved + logged).
- **ES system is taken into account**: the ES system (from the `system-selected` event, or derived from the rom path `roms\<system>\<game>` when ES does not send it) is now part of the mapping match - a mapping created for another system is never loaded, and its stored path is never rewritten. Two games with the same name in two systems no longer cross-load.
- **game-end always restores `default.remap`**: even when the emulator exe keeps running after the game ends (TeknoParrot / pcsx2 shells), the default profile is restored instead of staying stuck on the game's profile; a new game on the SAME emulator exe now loads its own dedicated profile (no more "Already loaded for <exe>" lock).
- When a mapping path is corrected for the running game, the profile is applied immediately (no more "load by hand or relaunch the game"), with a dedicated notification "Mapping path corrected for {exe} - profile loaded automatically". A stale "Profile manually loaded - Auto-load locked" state no longer blocks this load.
- Exe-generic mappings (no ES game name) keep the strict same-path rule: different instances of the same exe never cross-load.

### [FR] Améliorations du chargement des ".remap"
- **Chargement auto des profils (mappings par émulateur)** : un mapping associé à un jeu (nom de jeu envoyé par EmulationStation) n'est plus rejeté quand l'exe de l'émulateur est atteint par un chemin différent (symlink, autre racine) — le nom de jeu ES est l'identité, le chemin n'est plus qu'un bonus de score. Le chemin stocké est automatiquement remis à celui détecté au premier match (sauvegardé + loggué).
- **Le système ES est pris en compte** : le système ES (événement `system-selected`, ou dérivé du chemin de la rom `roms\<système>\<jeu>` quand ES ne l'envoie pas) fait partie du matching des mappings — un mapping créé pour un autre système n'est jamais chargé et son chemin stocké n'est jamais réécrit. Deux jeux de même nom dans deux systèmes ne se chargent plus mutuellement.
- **Le game-end restaure toujours `default.remap`** : même si l'exe de l'émulateur continue de tourner après la fin du jeu (shells TeknoParrot / pcsx2), le profil par défaut est restauré au lieu de rester bloqué sur le profil du jeu ; un nouveau jeu sur le MÊME exe d'émulateur charge maintenant son profil dédié (plus de verrou « Already loaded for <exe> »).
- Après correction du chemin du mapping, le profil est appliqué immédiatement (plus besoin de charger à la main ni de relancer le jeu), avec une notification dédiée « Mapping path corrected for {exe} - profile loaded automatically ». Un état « Profile manually loaded - Auto-load locked » obsolète ne bloque plus ce chargement.
- Les mappings exe-génériques (sans nom de jeu ES) gardent la règle stricte du même chemin : différentes instances d'un même exe ne se chargent pas mutuellement.

## v3.1.7.2

### [EN] Various / Fix-UMDF2
- When you update from a version older than 3.1.7.2, **Virtual Polling (upsampling)** and **IR smoothing (EMA)** are switched ON with the values 500 Hz for V1 and V2, smoothing EMA 1, so the new fluidity can be compared with the previous behaviour. Both can be turned off at any time.
- The Wiimote v2 model **"Nintendo RVL-CNT-01-TR"** (MotionPlus Inside) remains less fluid than the V1 — its Bluetooth report stream still arrives in bursts... there is currently no solution for this.
- HmHost (UMDF2 virtual mouse/keyboard host): fixed the virtual mouse not coming back without a Windows reboot when its device node was left wedged (pending removal / "unknown device" code 1 zombie). HmHost now verifies that each virtual device really reached the started state (root node + HID child `DN_STARTED`) before reporting success, evicts wedged nodes, and when needed recreates the device under a rotated identity key (P1 -> P1R) so a fresh node is used - no reboot required. It reports `ERR NOT_STARTED` instead of a silent dead device. The HIDMaestro SDK diag/timing logs are mirrored into `HmHost.log` for support.
- Various adjustments.

### [FR] Divers / Fix-UMDF2
- À la mise à jour depuis une version antérieure à 3.1.7.2, le **Virtual Polling (upsampling)** et le **lissage IR (EMA)** sont ACTIVÉS avec les valeurs 500 Hz pour la V1 et la V2, lissage EMA 1, afin de comparer la fluidité au comportement précédent. Tous deux peuvent être désactivés à tout moment.
- Le modèle de Wiimote v2 **« Nintendo RVL-CNT-01-TR »** (MotionPlus Inside) reste moins fluide que la V1 — son flux de rapports Bluetooth arrive toujours par salves.. pas de solution actuellement.
- HmHost (hôte UMDF2 souris/clavier virtuels) : corrige le cas où la souris virtuelle ne revenait pas sans redémarrer Windows quand son nœud de périphérique était resté bloqué (suppression refusée / zombie « périphérique inconnu » code 1). HmHost vérifie désormais que chaque périphérique virtuel a bien atteint l'état démarré (nœud racine + fils HID `DN_STARTED`) avant de répondre succès, évacue les nœuds bloqués et, si besoin, recrée le périphérique sous une clé d'identité rotatée (P1 -> P1R) pour utiliser un nœud neuf - sans redémarrage. Il répond `ERR NOT_STARTED` au lieu d'un périphérique mort silencieux. Les logs diag/timing du SDK HIDMaestro sont miroités dans `HmHost.log` pour le support.
- Divers ajustements.

---

## v3.1.7.1

### [EN] Aim & calibration
- **New "Projective distance correction" option (Hybrid PnP)** — disabled by default. It compensates the distance in a different way: the IR measurement is first brought back to its "calibration-distance equivalent" before the mapping, so the aim keeps its height better when you walk towards the screen or away from it. To test it: enable it, move, and compare with the previous behaviour — **if the result is not what you expect, disable it** (your previous behaviour comes back instantly, no recalibration needed).
- **The mouse cursor is locked at the center of the screen during calibration** — it no longer wanders over the targets, and it is unlocked as soon as the calibration is finished or aborted.

### [FR] Visée & calibration
- **Nouvelle option « Projective distance correction » (Hybride PnP)** — désactivée par défaut. Elle compense la distance autrement : la mesure IR est d'abord ramenée à son « équivalent à la distance de calibration », ce qui permet à la visée de mieux tenir sa hauteur quand on avance vers l'écran ou qu'on s'en éloigne. Pour la tester : activez-la, déplacez-vous, comparez avec le comportement précédent — **si le résultat n'est pas celui attendu, désactivez-la** (le comportement précédent revient aussitôt, sans recalibration).
- **Le curseur souris est verrouillé au centre de l'écran pendant la calibration** — il n'erre plus sur les cibles, et il est déverrouillé dès que la calibration est terminée ou annulée.

---

## v3.1.7.0

### [EN] Output - game-driven rumble (new tab in Options)
- New "Output" tab in Options: Wiimote4Guns can receive the game's outputs through **OutputHooker** and turn them into rumble - each shot is felt, and every hit taken gives an impact rumble.
- **What it needs**: the outputs exist only when the emulator exposes them to OutputHooker **natively**, or through **DemulShooter** (RetroBat: install DemulShooter, then enable it and the network/Windows outputs in the emulator options, e.g. "model2"). OutputHooker itself is installed and activated from **Wiimote4Guns > Options > Output** (Download button + auto-start with a Wiimote).
- Simple to configure: the commands are provided in a **default.ini**, so per-game ini files are usually unnecessary. If a game exposes a different tag, add its command in its own ini or in default.ini (a documentation file, `output_tags.md`, is installed next to the app).
- "Rumble only during a game" option: no more rumble in the menus / on the desktop.
- OutputHooker is managed from the same tab: installed version vs the latest GitHub release, Download/Update (your `OutputHooker.ini` and ini folder are kept), and automatic start with a Wiimote (stopped when the last one disconnects).
- Per player and per game profile: the "Rumble Output" button on the mapping page opens a small window to enable/disable it and set the force and the length (ms) of the fire and damage rumble. A game needs its own settings? They are saved in its profile.
- Damage feedback: the 4 LEDs of the Wiimote flash briefly when you are hit, then the player's own LED comes back.
- UI: the "Ticks" controls in Options > Gestures are visible again, and the new Output tab follows the interface zoom.

### [FR] Output - vibration pilotée par le jeu (nouvel onglet dans Options)
- Nouvel onglet "Output" dans Options : Wiimote4Guns peut recevoir les outputs du jeu via **OutputHooker** et les transformer en vibration - chaque tir est ressenti, et chaque impact reçu déclenche une vibration de dégâts.
- **Ce qu'il faut pour en profiter** : les outputs n'existent que si l'émulateur les expose à OutputHooker **nativement**, ou via **DemulShooter** (RetroBat : installer DemulShooter, puis l'activer ainsi que les outputs réseau/Windows dans les options de l'émulateur, ex. "model2"). OutputHooker s'installe et s'active depuis **Wiimote4Guns > Options > Output** (bouton Download + démarrage automatique avec une Wiimote).
- Simple à configurer : les commandes sont fournies dans un **default.ini**, les fichiers ini par jeu ne sont donc généralement pas nécessaires. Si un jeu expose un tag différent, ajoutez sa commande dans son propre ini ou dans default.ini (un fichier de documentation, `output_tags.md`, est installé à côté de l'application).
- Option "Rumble only during a game" : plus de vibration dans les menus / sur le bureau.
- OutputHooker est géré depuis le même onglet : version installée comparée à la dernière release GitHub, Download/Update (votre `OutputHooker.ini` et votre dossier ini sont conservés), et démarrage automatique avec une Wiimote (arrêt à la dernière déconnexion).
- Par joueur et par profil de jeu : le bouton "Rumble Output" de la page mapping ouvre une petite fenêtre pour l'activer/le désactiver et régler la force et la durée (ms) des vibrations de tir et de dégâts. Un jeu demande ses propres réglages ? Ils sont enregistrés dans son profil.
- Retour des dégâts : les 4 LEDs de la Wiimote clignotent brièvement quand vous êtes touché, puis la LED du joueur revient.
- UI : les réglages "Ticks" dans Options > Gestes sont de nouveau visibles, et le nouvel onglet Output suit le zoom de l'interface.

---

## v3.1.6.2

### [EN] DolphinBar Stability & Controller Fixes
- DolphinBar Mayflash stability (flaky workaround): fewer disconnect/reconnect loops (progressive retry backoff on phantom HID ports, a transient write failure no longer force-disconnects the controller, the Bluetooth watchdog ignores DolphinBar HID failures, and the VMulti devcon cleanup is skipped when no VMulti driver is present).
- Nunchuk (RVL-4): the joystick axis now works without the MotionPlus adapter. No regression on older Nunchuks.
- TC Cover: firing while in cover (off-screen) now changes the weapon — the trigger is no longer locked off-screen in TC Cover mode.

### [EN] Off-Screen Reload Adjustments
- In AUTO Off-Screen Reload the physical trigger no longer fires a reload while off-screen (only the single auto-reload runs).
- Options > Gestures: the "Auto Off-Screen" checkbox is replaced by a Trigger/Auto dropdown, matching the per-player mapping page.

### [EN] UI & Window Management
- Profile / system / folder pickers and the Off-Screen Reload excluded-executables manager now open above the always-on-top overlay (Home+Plus) instead of hiding behind it.

### [FR] Stabilité de la DolphinBar & Correctifs Contrôleurs
- Stabilité de la DolphinBar Mayflash (contournement - aléatoire) : réduction des boucles de déconnexion/reconnexion (temporisation progressive en cas d'essais sur des ports HID fantômes, un échec d'écriture transitoire ne déconnecte plus de force le contrôleur, le watchdog Bluetooth ignore les pannes HID de la DolphinBar, et le nettoyage devcon de VMulti est ignoré lorsqu'aucun pilote VMulti n'est présent).
- Nunchuk (RVL-4) : l'axe du joystick fonctionne désormais sans nécessiter l'adaptateur MotionPlus. Pas de régression sur les anciens Nunchuks.
- TC Cover : tirer à couvert (hors écran) permet désormais de changer d'arme — la gâchette n'est plus bloquée hors écran en mode TC Cover.

### [FR] Ajustements du Rechargement Hors Écran
- En rechargement AUTO hors écran, la gâchette physique ne déclenche plus de rechargement supplémentaire hors écran (seul l'auto-rechargement unique est exécuté).
- Options > Gestures : la case à cocher "Auto Off-Screen" est remplacée par un menu déroulant Trigger/Auto, aligné avec la page de configuration par joueur.

### [FR] UI & Gestion des Fenêtres
- Les sélecteurs de profil / système / dossier et le gestionnaire d'exécutables exclus du rechargement hors écran s'ouvrent désormais au-dessus de l'overlay toujours au premier plan (Home+Plus) au lieu de se cacher derrière.

---

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
