# Changelog

All notable changes to Wiimote4Guns will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/).


## [3.1.6.1] - 2026-10-18

- Standalone W4G_Setup.exe installer - Setup update reliability : WiimoteGun.exe is closed before extracting (a running app locks its own files), the running WiimoteGun.Service.exe is never overwritten (the service is updated by its own elevated path), files in use are skipped and logged instead of aborting the install half-way
- Off-Screen Reload (checkbox + Trigger/Auto dropdown) is now PER PLAYER, even on the default profile: each player overrides the global, the per-player choice survives profile reloads, and changing the global in Options > Gestures re-applies it to every player
- Reload Rumble, its intensity and its style are now per player as well, with the same rules

---

## [3.1.6.0] - 2026-10-17

- Layout Two WiimoteBar (Or Blancom) was previously removed due to issues with the calibration mechanism. Re-introduced now, it works better and allows pairing with a Blacom in a two-bar layout by switching the Wiimote to Player 2. In my opinion, the accuracy here is no better than with a single sensor bar. To be tested...
- Hybrid-PnP mode available.
- Fisheye or wide-angle lens recommended.
- "Hybrid PnP (alpha test)" on Wiimote Bar and Two Wiimote Bars
- Distance and tilt compensation (opt-in, per layout)
- Declared LED geometry: accurate metric distance + precision display
- Live gun-to-screen distance before and after calibration
- Replaces the PowerShell script, no PowerShell dependency
- Proper service staging (running service files staged, applied at next start)
- Legacy WiimoteUpdate.ps1 cleanup on first run
- Self-update of the updater via rename trick
- Automatic .NET 10 Runtime download + silent install on START
- UMDF2 driver activation triggered directly from the wizard
- Progress dialog during runtime install, HmHost startup notice
- Windows reboot advice if tracking does not activate after 10s
- Welcome dialog survives the UMDF2 restart, shows on first real start
- "? Help" button on the home page (top-left, zoom-aware)
- Changelog dialog after each update: FR/EN toggle, GitHub link, local markdown

## [3.1.0.0-fix] - 2026-10-09

> **EN/FR**: Correctifs post-release 3.1.0.0 livrés SANS changement de version via l'asset
> `updateFix_v3.1.0.0.7z` (même prerelease) — l'auto-update app ne re-télécharge pas une
> version identique, la réparation passe par `UpdateService.ps1` (téléchargement du fix intégré).

### Fixed
- **Crash au lancement après mise à jour 3.1.0.0 (FileLoadException System.Resources.Extensions)** — voir le bullet `[3.1.0.0-hotfix]` ci-dessous pour le détail : `WiimoteGun.exe.config` (redirect `0.0.0.0-9.0.0.0 → 8.0.0.0` + supportedRuntime v4.8) n'était **jamais déployé** (App.config absent du csproj).
- **`WiimoteUpdate.ps1` : le dossier `HmHost\` du service n'était jamais stagé** — le script sautait TOUS les sous-dossiers de `WiimoteGun.Service` lors du routage vers `update_service\`, donc l'hôte UMDF2/HIDMaestro et ses profils n'étaient jamais mis à jour côté service (« INFO: No HmHost folder in update package » — gamepads UMDF2 absents). Fix : staging récursif de tous les sous-dossiers sauf `update_service` imbriqué.
- **`UpdateService.ps1` : nouvelle étape « fix archive »** — paramètre `-FixUrl` (ou `$DefaultFixUrl` au début du script) : télécharge `updateFix_v3.1.0.0.7z`, l'extrait comme la mise à jour normale, applique ses fichiers app (exe.config, WiimoteUpdate.ps1 corrigé…) et staggé la charge du service (HmHost inclus) dans `update_service\` PUIS déroule la mise à jour normale — **une seule passe admin répare tout** (installs cassées : HmHost manquant + exe.config manquant + script cassé). (`WiimoteGun\Service\UpdateService.ps1`, `WiimoteGun\WiimoteUpdate.ps1`)

## [3.1.0.0-V57h..V57q] - 2026-10-08

> **EN**: Major feature release — the UMDF2/HIDMaestro driver becomes the default input
>     driver with full GamePad support. Everything from V57h (dedicated gamepad names,
>     Dolphin profile patching, kb-only mouse hiding, reboot persistence) through V57i
>     (.NET 10 runtime prerequisite + activation check/prompt), V57j (activation
>     confirmation + proposed restart), V57k (Setup Wizard rework: service-only required,
>     VMulti optional with opt-in checkbox + bilingual confirmation, wizard re-show after
>     service update, UMDF2 default for new users, auto-restart after UpdateService.ps1),
>     V57l/V57m (driver documentation + Interception-era cleanup), V57n..V57o5 (global UI
>     zoom −/+ 80-150% with all scaling fixes + DirectInput OEM name self-repair),
>     V57p (mouse wheel suppression on dropdowns) and V57q (automatic es_input.cfg
>     registration for EmulationStation auto-detection). The 3.0.x → 3.1.0.0 bump
>     reflects the driver change and the feature scale. See each bullet for its tag.
> **FR**: Version majeure de fonctionnalités — le pilote UMDF2/HIDMaestro devient le
>     pilote d'entrée par défaut avec le support GamePad complet. Tout ce qui va de V57h
>     (noms gamepads dédiés, patch des profils Dolphin, masquage souris kb-only,
>     persistance reboot) à V57i (runtime .NET 10 prérequis + check/prompt à l'activation),
>     V57j (confirmation d'activation + restart proposé), V57k (refonte Setup Wizard :
>     service seul requis, VMulti optionnel avec case d'adhésion + confirmation bilingue,
>     ré-affichage du wizard après update du service, UMDF2 par défaut pour les nouveaux
>     utilisateurs, redémarrage auto après UpdateService.ps1), V57l/V57m (documentation
>     pilote + purge de l'ère Interception), V57n..V57o5 (zoom UI global −/+ 80-150 % avec
>     tous les correctifs de scaling + auto-réparation du nom OEM DirectInput), V57p
>     (suppression molette sur les listes déroulantes) et V57q (enregistrement automatique
>     es_input.cfg pour la détection auto par EmulationStation). Le bump 3.0.x → 3.1.0.0
>     reflète le changement de pilote et l'ampleur des fonctionnalités. Voir le tag
>     d'itération sur chaque puce.

### Changed
- **V57m: Nettoyage de l'ère Interception + corrections README** : suppression des **boutons « ⚙️ Devices »** de la page Assign Wiimote et du dialogue `PlayerDeviceDialog` associé (sélecteur par joueur de clavier/souris réels ou pilote virtuel — reliquat non fonctionnel de l'ancien pilote Interception) ; la vraie fonctionnalité de la page — le **verrouillage de slot** (`btnLock`, `Options.GetLockedSlot/SetLockedSlot`) — est conservée : elle permet de réserver un slot joueur pour un **vrai lightgun (ex. Gun4IR) en P1 — voire deux en P1+P2 — à côté de joueurs Wiimote4Guns**. README : cycle HOME corrigé (le mode Keyboard n'existe plus dans le cycle [V55] ; Mouse → Mouse 4:3 → GamePad → Disabled + variantes FPS/4:3 si activées), section Device Assignment réécrite en « Player Slot Locking » (mention Auto-Lock VMulti retirée), layout LED sans « Recommended » + **4 LED = meilleur choix et SANS calibration**, modal tuiles (PLUS ~3 s : profil à la volée + swap DInput↔XInput), Requirements/.NET 10/Installation/GamePad Mode/Troubleshooting alignés sur le nouveau pilote. (`AssignControl.cs/.Designer.cs`, `PlayerDeviceDialog` supprimés, `WiimoteGun.csproj`, `README.md`)
- **V57k: Setup Wizard refondu — le service est la SEULE composante requise, le pilote VMulti devient OPTIONNEL** (premier lancement + ré-affichage post-update) :
  - **Seul le service reste obligatoire** : le bouton « Start Wiimote4Guns » s'active dès que le service est installé (plus besoin du pilote) ; le statut VMulti « non installé » n'est plus rouge mais orange « ○ Not installed (optional) » ; la page d'accueil bilingue explique que le mode recommandé « RawInput (UMDF2) » s'installe silencieusement via le service (sans test mode, sans UAC) et que le pilote vmulti (legacy) est désormais optionnel. La **désinstallation VMulti** reste disponible (« Uninstall All ») quand le pilote est installé.
  - **Installation VMulti à double garde** : une **case d'adhésion devant le bouton** (« I understand - unlock the VMulti install button / Je comprends - déverrouiller l'installation VMulti ») verrouille le bouton « Install Driver » (grisé, libellé « Install (locked) ») ; une fois cochée, un **dialogue de confirmation bilingue avec petit bouton FR/EN** (bascule textes + boutons) doit être accepté avant l'installation : il précise que le NOUVEAU pilote « RawInput (UMDF2) » est disponible, fonctionne pareil et ne devrait PAS être bloqué par Microsoft à compter de **Windows 11 26H02** (dépréciation des pilotes aux anciens certificats) ; que le nouveau pilote est **EN PHASE DE TEST** ; que VMulti sera à terme supprimé ou réservé à Windows 10 / Windows 11 < 26H02.
  - **Ré-affichage unique du wizard après mise à jour du service** : quand le WiimoteGun.Service passe d'une version **inférieure à 3.0.0.24** à **3.0.0.24+** (UpdateService.ps1 + redémarrage du service), le Setup Wizard est ré-affiché **une fois** au démarrage suivant de l'app pour présenter le nouveau contenu. **[V57k-FIX, test user]** la détection par version-vue (`Options.LastServiceVersionSeen`) échouait quand app+service étaient mis à jour en une fois (la nouvelle app n'avait jamais vu l'ancien service) ; le mécanisme fiable est désormais **UpdateService.ps1 lui-même** — il capture l'ancienne version AVANT le remplacement et, si < 3.0.0.24, écrit `HKLM\SOFTWARE\WiimoteGun\ShowSetupWizardPending=1` ; l'app (non-admin, lecture HKLM) consomme le flag au démarrage (`ShowSetupWizard=true` + Save) puis demande au service SYSTEM de l'effacer (nouvelle commande pipe `WIZARD_ACK` → `HmHostSupervisor.ClearSetupWizardPending` — l'app non-admin ne peut pas écrire HKLM). **[V57k-FIX2] Relance automatique de l'app après l'update** : le script relance Wiimote4Guns **automatiquement et désélevé** via `explorer.exe` + un `.cmd` temporaire (explorer ne transmet pas les arguments) et l'IPC `-refresh` existant — une instance en cours se redémarre elle-même (batch de restart non-admin), une app absente démarre simplement ; dans les deux cas le flag est consommé et le wizard s'ouvre une fois (repli message manuel si l'app est introuvable). Le parsage de version ne fait jamais échouer le script après une mise à jour réussie ; une installation fraîche n'écrit pas le flag (le défaut `ShowSetupWizard` couvre les premiers lancements). La détection `LastServiceVersionSeen` reste en filet de sécurité secondaire. (`UpdateService.ps1`, `Program.cs`, `PipeServer.cs`, `HmHostSupervisor.cs`)
  - **Nouveaux utilisateurs : « RawInput (UMDF2) » par défaut** — quand le service est installé et que le pilote VMulti ne l'est PAS, le wizard sélectionne automatiquement le mode « RawInput (UMDF2) » (un mode « RawInput (VMulti) » sauvegardé ciblerait un pilote absent) et l'indique dans le statut ; s'applique aussi au ré-affichage post-update. Le wizard porte l'information complète sur le nouveau pilote (recommandé, phase de test, avenir de VMulti). **[V57k-FIX]** la case d'adhésion porte un libellé anglais seul (« I understand - unlock the VMulti install button ») : le texte bilingue initial était masqué sous les boutons Install/Uninstall. (`SetupWizard.cs`, `SetupWizard.Designer.cs`, `Program.cs`, `ServiceClient.cs`, `Options.cs`)
- **V57h: Noms dédiés pour les gamepads UMDF2 + patch des profils Dolphin selon le mode pilote + souris réellement masquée en mode GamePad + persistance UMDF2 au reboot** :
  - **Renommage des gamepads DInput UMDF2** (décision user, révoque le nommage vmulti de V57g) : profils `wiimotegun-gp-p1..p4` → productString/name **« GamePad Wiimote4Guns P1..P4 »**. VID uniques/joueur et PID 0xBA1D inchangés (le filtre `FindVMultiGamepadIndex` par VID et l'index DInput DuckStation/PCSX2 ne dépendent pas du nom).
  - **Patch des `Device = DInput/0/<nom> HID` dans TOUS les `*-wiimotegun.ini` de Dolphin** (templates codés en dur ET profils créés par l'utilisateur — énumération par motif `*-wiimotegun.ini`, joueur déduit du nom de fichier `P<n>`) selon le mode pilote sélectionné : **VMulti → `DInput/0/vmulti{a|b|c|d} HID`** ; **UMDF2 → `DInput/0/GamePad Wiimote4Guns P<n> HID`**. Passe **idempotente rejouée à chaque `UpdateProfiles`** (connexion/déconnexion wiimote, changements de devices, bascules de mode) : un changement de mode pilote est toujours répercuté, dans les deux sens. Le masquage Dolphin suit désormais **uniquement les joueurs DInput** (un joueur XInput seul ne démasque pas des profils pointant un device DInput inexistant dans ce mode — cohérent avec le log V57g). (`Core\EmulatorProfileAutomator.cs` : `PatchDolphinDeviceNames`)
  - **Souris UMDF2 EFFECTIVEMENT désactivée au passage en mode GamePad (parité vmulti COL03), sauf hybride actif** — nouveau mécanisme **kb-only** : un devnode UMDF2 unique ne peut pas retirer une collection, donc `REMOVE_MOUSE_Pn` route vers HmHost `HIDE_MOUSE_P:<p>` qui **échange le device du joueur souris+clavier → clavier-seul à la MÊME clé d'identité « Pn »** (SDK issue #60 : un profil différent à la même clé rafraîchit le descripteur aux mêmes chemins). `ENABLE_Pn` (`ACTIVATE_P`) restaure le device mkb complet. Couvre AUSSI les modes **Keyboardpad** (clavier requis, souris masquée = sémantique vmulti exacte) et **Disabled**. Frames souris ignorées par HmHost tant que le device est kb-only (rapport 0x03 non déclaré), frame souris du watchdog purgée au swap, clavier + watchdog KEYS inchangés (continuité de frappe à travers le swap). Nouveaux profils **`wiimotegun-kb-p1..p4.json`** (collection clavier boot vmulti 0x07 octet pour octet, **PID 0xBA1E** ≠ 0xBACC : le clavier devient le Col01 de ce device et ne doit pas matcher le chemin lightgun ES `VID_001F&PID_BACC&Col01`). (`HmHost\Program.cs` : `HideMouseForPlayer` + flag `_kbOnly` + resets dans tous les teardowns ; `PipeServer.cs` `RouteMouseRemove` ; `HmHostSupervisor.cs` `HideMouse` ; `HmHost.csproj` 4 None entries)
  - **Fix « pilotes vmulti activés à la connexion wiimote après un reboot » (bug user)** : l'app déclenche `HM_ACTIVATE` en tâche de fond au démarrage ; une wiimote reconnectée pouvait envoyer `ENABLE_P` AVANT la fin de l'activation → le service voyait `IsActive()=false` → la commande tombait sur le chemin devcon vmulti legacy. **Double correctif** : (a) **app** : garde-fou au flux de connexion — si mode UMDF2 et `!HmIsActive()` → `HmActivate()` (idempotent) AVANT `EnablePlayer` ; (b) **service** : **persistance de l'état UMDF2** dans `HKLM\SOFTWARE\WiimoteGun\Umdf2Active` (écrit par `Activate`/effacé par `Deactivate`+`EnsureStopped`) et `ServiceMain.OnStart` → `HmHostSupervisor.ReapplyPersistedState()` réactive l'hôte en tâche de fond au boot (aucun device créé : à la demande par wiimote connectée, design V57e) → le mode survit aux reboots et l'état du superviseur est correct avant même le lancement de l'app. (`Core\WiiMoteController.cs`, `WiimoteGun.Service\ServiceMain.cs`, `HmHostSupervisor.cs`)
  - **[V57j] Confirmation d'activation + restart proposé (demande user)** : après une activation UMDF2 réussie (sélection OU Apply), au lieu du flux silencieux « WaitCursor puis rien » : dialogue de **succès** (« activé avec succès — le mode prend effet à la prochaine connexion de Wiimote ou au redémarrage ») puis **question « Redémarrer Wiimote4Guns maintenant ? »** — Oui : le mode fraîchement validé est **sauvegardé AVANT le redémarrage** (sinon le batch — qui recharge les options depuis le XML — annulerait la sélection) puis relance via le batch existant (attend la sortie du process, libère le mutex, relance ; la nouvelle instance réactive HmHost silencieusement et crée les devices par wiimote connectée) ; Non : comportement actuel (effet à la prochaine connexion). (`OptionsControl.cs`, `Program.cs` `RestartApplication` public)
  - **[V57j] Vérif du nom des devices gamepad dans HmHost (durcissement fantôme)** : `ActivateGamepad` mémorise le product string du profil à la création et **recrée le device à la même identité « GPn » si le profil courant diffère** — un renommage de device dans une mise à jour s'applique automatiquement au prochain passage GamePad, sans redémarrer HmHost (le nom d'un devnode est figé à sa création ; le vieux nom colle sinon). (`HmHost\Program.cs`)

### Added
- **[3.1.0.0-hotfix] App.config ENFIN déployé + redirect de binding System.Resources.Extensions (corrige le crash au lancement après mise à jour)** : VS2022's GenerateResource grave les manifests .resources compilés avec la version de System.Resources.Extensions embarquée DANS MSBuild (**6.0.0.0**), alors que le package NuGet déployé (8.0.0) fournit l'assembly **8.0.0.0** — sans redirection, chaque appel ResourceManager (icônes tray de `Resources.get_gray()`) crashait en `FileLoadException 0x80131040` dès le démarrage de la 3.1.0.0 (le zip apportait le SRE 8.0.0.0 par-dessus des resources exigeant 6.0.0.0). De plus, l'App.config n'avait **JAMAIS été inclus au csproj** : aucun `WiimoteGun.exe.config` n'a jamais été déployé (le `supportedRuntime` restait à v4.5.1, inappliqué). Fix : `<None Include="App.config" />` + redirect `System.Resources.Extensions 0.0.0.0-9.0.0.0 → 8.0.0.0` + `supportedRuntime` corrigé en v4.8. Le service n'est pas exposé (aucune ressource embarquée). (`WiimoteGun\App.config`, `WiimoteGun.csproj`)
- **V57n: Zoom UI global (− / + sur la page d'accueil, 80–150 %)** : boutons **− / +** avec pourcentage en bas à droite de la page d'accueil Wiimote4Guns → toutes les fenêtres, pages et textes de l'application suivent le zoom. Mécanisme **scale à la CRÉATION** (jamais de re-scale live d'une fenêtre ouverte — zéro corruption possible) : chaque fenêtre appelle `UiScaler.ApplyForm` en fin de construction → **bornes d'abord** (`Form.Scale` natif récursif) **puis polices** (parcours par référence : les polices héritées suivent le parent, les explicites sont scalées exactement une fois — registre d'idempotence `ConditionalWeakTable` qui permet de re-appliquer les polices à chaque changement de page après `LoadData` sans jamais doubler) **puis bornage à la zone de travail de l'écran** (jamais hors écran). Le changement de zoom sauvegarde `Options.UiScalePercent` (80–150, pas 10, défaut 100) et **rouvre l'interface dans le même mode** (fenêtré/plein écran) ; toutes les autres fenêtres prennent le zoom à leur prochaine ouverture. Points sensibles traités : `ProfileOverlay.ShowPage` scale ses constantes codées en dur (COMPACT/EXTENDED_WIDTH, 840, offsets, coins arrondis) pour qu'un changement de page ne ramène jamais la fenêtre à la taille Designer ; la modal tuiles scale son cadre AVANT la construction des tuiles et les tuiles sont **zoom-aware à la création** (`UiScaler.S`/`Factor`) pour rester justes quand `PopulateTiles` se rejoue après navigation ; les 3 forms legacy qui réassignent `Font = SystemFonts.MessageBoxFont` après `InitializeComponent` appliquent le zoom APRÈS cette réassignation. Couvert : interface complète (toutes les pages + lignes runtime), Setup Wizard, modal tuiles + picker système, et 14 dialogues standards. **Volontairement exclus** (surfaces dessinées à métrices fixes — un scale mécanique les désalignerait) : CurseurCrosshair, toasts de notification, CalibrateForm, OffsetAdjustmentOverlay, VirtualKeyboard — à traiter séparément si besoin. (`UI\UiScaler.cs` NOUVEAU, `Options.cs`, `Program.cs`, `HomeControl.*`, `ProfileOverlay.cs`, `EsProfileTileDialog.cs`, `SetupWizard.cs`, 14 dialogues, `WiimoteGun.csproj`)
- **Runtime .NET 10 = prérequis machine (décision user [V57i]) + vérification à l'activation UMDF2 avec prompt de téléchargement** : HmHost est délibérément **framework-dependent** (PAS de déploiement self-contained) — l'utilisateur installe le runtime .NET 10 une fois par machine. À la sélection du mode « RawInput (UMDF2) » (Options), l'app **vérifie la machine AVANT de proposer le mode** (`WiimoteGun.Core.DotNetRuntimeChecker` : sonde sans spawn de processus suivant l'ordre de résolution de l'apphost — `DOTNET_ROOT(_X64)` → `HKLM\SOFTWARE\dotnet\Setup\InstalledVersions\x64\InstallLocation` → `%ProgramFiles%\dotnet` — et cherchant `shared\Microsoft.NETCore.App\10.*` release, previews/RC exclues). **[V57i-FIX, testé réel]** les dossiers des runtimes partagés sont nommés **SANS le préfixe `v`** (`10.0.12`, preuve `dotnet --list-runtimes`) — le glob initial `v10.*` ne trouvait RIEN et faisait échouer la vérification sur les machines qui ONT .NET 10 ; les deux formes sont testées + **repli dernier recours `dotnet --list-runtimes`** (ligne `Microsoft.NETCore.App 10.`) pour ne jamais rater une installation. **Si le runtime .NET 10 est déjà installé, le prompt ne s'affiche JAMAIS** et le flux UMDF2 normal suit ; s'il manque, un dialogue bilingue explique le **BON package** — « **.NET Runtime 10.0.x — Windows — x64** » (PAS le SDK, PAS « ASP.NET Core Runtime », PAS « .NET Desktop Runtime ») avec le lien officiel `https://dotnet.microsoft.com/download/dotnet/10.0` (colonne « Run apps - Runtime » → bouton « x64 ») — et propose : **[Oui]** ouvrir la page de téléchargement officielle (la sélection du mode est annulée, l'utilisateur re-sélectionne après installation), **[Non]** continuer quand même (l'activation affichera alors le diagnostic d'échec explicite), **[Annuler]** abandonner. Vérifié par exécution de la classe compilée sur la machine de dev (runtime 10.0.12 présent → `True` ; repli spawn → ligne trouvée). (`WiimoteGun\Core\DotNetRuntimeChecker.cs` NOUVEAU, `UI\Modern\Controls\OptionsControl.cs`)
- **Diagnostic runtime .NET 10 manquant pour HmHost** : HmHost est lancé par le service SYSTEM en **session 0** — le dialogue GUI apphost « You must install .NET » ne peut JAMAIS s'afficher pour l'utilisateur ; sans runtime, l'hôte meurt silencieusement en boucle. Le superviseur journalise désormais un message explicite quand HmHost.exe meurt avec les codes apphost `0x80008082/0x80008083` (runtime .NET absent/incompatible) : *« .NET 10 RUNTIME MISSING: install the .NET Runtime 10.0.x Windows x64 (https://dotnet.microsoft.com/download/dotnet/10.0 - column 'Run apps - Runtime', NOT the SDK) »* — dans le log du service ET dans la réponse de HM_ACTIVATE (affichée par le dialogue d'échec d'activation de l'app). (`HmHostSupervisor.cs` : `DescribeHostExit`)

### Verified
- **Mise à jour auto vs HmHost en cours** : `UpdateService.ps1` stoppait déjà le service (dont le `OnStop` → `EnsureStopped` → QUIT + Kill 3 s) PUIS **tue explicitement tout HmHost.exe résiduel** (bloc [V57d] l.79-85) AVANT `Remove-Item`/`Copy-Item` du dossier `HmHost\` — le remplacement ne peut pas être bloqué par un HmHost en cours d'exécution. Aucun changement nécessaire.

- **Version synchronisée 3.0.0.24** — WiimoteGun app, WiimoteGun.Service et HmHost. (`AssemblyInfo.cs` des deux projets, `HmHost.csproj` + `HostVersion`)

---

## [3.0.0.23-V57g] - 2026-10-07

### Added
- **V57g: GamePads DInput + XInput en mode « RawInput (UMDF2) » via HIDMaestro** — le mode UMDF2 disposait des souris/claviers mais PAS de gamepads (le mode GamePad reposait sur vmulti Col06/ViGEmBus, absents d'une machine UMDF2 pure). HIDMaestro crée les gamepads des DEUX apis : **DInput** (descripteur **Joystick vmulti Col06 repris OCTET POUR OCTET**, rapport 0x06 : Throttle, X/Y signés, Hat neutre, Rx/Ry, 16 boutons DPad bits 12-15 → bindings guncon2 DuckStation/PCSX2 et noms de devices Dolphin inchangés) et **XInput** (profil **xbox-360-wired + companion XUSB** `HMXInput.dll`/`hidmaestro_xusb.inf` — déjà installés par le `InstallDriver` existant, ZÉRO installation supplémentaire : pad visible par `XInputGetState/Ex` slots 0-3, invisible DInput comme le vrai matériel xusb22 = parité ViGEm Xbox360 exacte, **aucune dépendance ViGEmBus en mode UMDF2**) :
  - **Profils `HmHost\profiles\wiimotegun-gp-p1..p4.json`** : un par joueur, VID uniques vmulti (0x001F/0x002F/0x003F/0x004F), productStrings `vmultia/vmultib/vmultic/vmultid` (mêmes noms Dolphin), **PID distinct 0xBA1D** (≠ 0xBACC mkb : le chemin lightgun `VID_001F&PID_BACC&Col01` d'EmulationStation ne matche jamais le gamepad ; le filtre `FindVMultiGamepadIndex` PID>=0xBA1C le reconnaît sans changement). **`wiimotegun-x360-p1..p4.json`** : copies verbatim du profil SDK xbox-360-wired (VID 0x045E/PID 0x028E, companion XUSB conservé via IsXboxBranded).
  - **HmHost** : nouvelles commandes `ACTIVATE_GP:<p>:<DINPUT|XINPUT>` (création/échange à l'index épinglé 4+p-1, clé d'identité « GPn » stable — motif SDK LiveSwap, chemins identiques à travers les swaps), `DEACTIVATE_GP:<p>` (idempotent), frames fire-and-forget `GP:<p>:<throttle>:<x>:<y>:<hat>:<rx>:<ry>:<buttons>` (rapport 0x06 verbatim via `SubmitRawExtendedReport`) et `GPX:<p>:<lx>:<ly>:<rx>:<ry>:<lt>:<rt>:<hat>:<buttons>` (via `HMGamepadState`/`SubmitState : le SDK packe le buffer GIP + le rapport natif X360). **Watchdog gamepad** (re-soumission du dernier état tant que tenu/récent, parité KB V57e2) ; `DEACTIVATE_P/ALL/EXCEPT/DEACTIVATE/CleanupAll` suppriment AUSSI le gamepad du joueur (le gp ne survit jamais au device joueur) ; `STATUS` devient `OK ACTIVE GP=1D,2X,3-,4-`. (`HmHost\Program.cs`, `HmHost\HmHost.csproj`)
  - **Service** : `ENABLE_GAMEPAD_Pn`/`REMOVE_GAMEPAD_Pn` routés vers HmHost quand le mode UMDF2 est actif (sinon devcon vmulti inchangé) ; nouvelle forme suffixée `ENABLE_GAMEPAD_Pn:DINPUT|XINPUT` (l'api voyage avec la commande — seule l'app connaît la case profil/bascule modale), forme legacy sans suffixe routée aussi (DINPUT par défaut). (`PipeServer.cs`, `HmHostSupervisor.cs`)
  - **App `HmGamepad` (NOUVEAU, `IVirtualGamepad`)** : pilote les 2 apis — DInput = reflet bit-exact de `VMultiGamepadReport` ; XInput = bits `HMButton`/octant `HMHat` + **inversion Y identique à ViGEmGamepad** (comportement de visée inchangé) ; `Connect()` = commande de création + attente du device via `HM_STATUS` (poll ~3 s max) + **détection du slot XInput par mark-and-poll** (sonde A + coin stick gauche dans une fenêtre LX privée par joueur P1 −32768/P2 −16384/P3 0/P4 +16384 ±6000, exécutée AVANT que la boucle de jeu n'émette : zéro course ; les slots XInput suivent l'ordre de CRÉATION, pas le n° joueur). (`Core\HmGamepad.cs`, `Services\ServiceClient.cs` `HmFrameClient.SendGamepadRaw/SendGamepadXInput`)
  - **`Program.SetGamePadOutputApi` (FONCTION PARTAGÉE)** : le bouton SWICTH de la modale (appui long PLUS → onglet GamePad), la case « Use XInput » des mappings GamePad et tout changement programmatique passent par LA MÊME fonction (un seul écrivain, une seule sauvegarde, un seul chemin de bascule runtime `GamePadOutputApiMismatch → ReinitGamepadOutput` pour les 3 backends vmulti/ViGEm/UMDF2). Libellés dynamiques : modale « DInput (UMDF2) » / case « Use XInput (UMDF2/XUSB|ViGEmBus) ». (`Program.cs`, `EsProfileTileDialog.cs`, `GamePadMappingControl.cs`, `WiiMoteController.cs`)
  - **Routage index émulateurs XInput (créé — l'existant ne routait QUE DInput-n, XInput était inhibé)** : `EmulatorProfileAutomator` collecte les slots XInput par joueur (en plus des index DInput), la liste des joueurs actifs devient l'UNION DInput∪XInput (plus d'inhibition pour un joueur XInput), `[InputSources] XInput = true` quand un joueur XInput est actif, et les bindings guncon2 sont réécrits `DInput-n/ButtonN → XInput-<slot>/<token traduit>` via des tables de traduction par émulateur (`DuckStationXInputMap`/`Pcsx2XInputMap` — ordre wButtons pour les boutons, gâchettes en axes analogiques, sticks même layout positionnel). **Dolphin reste DInput-only** (backend wiimote émulée) : profils inhibés + log explicite. **NOTE : les tokens XInput de DuckStation/PCSX2 sont à vérifier sur cible** (binder une entrée guncon2 à la main avec une manette XInput physique et lire le profil écrit) — tables = point de correction unique. (`Core\EmulatorProfileAutomator.cs`)
  - **Cycle de vie = parité souris rawinput (spéc user)** : le device gamepad n'existe QUE pendant que la wiimote est en mode GamePad ; supprimé au retour en mode Mouse (`PersistentGamePads` contourné en UMDF2), à la déconnexion (via `DEACTIVATE_P` étendu), à la fermeture/crash de l'app (cleanup HmHost existant).
  - **Fix latent « clic bloqué » au switch Souris→GamePad** : le device souris mkb reste vivant mais inerte en GamePad non-hybride (parité COL03 non stricte) — un bouton tenu au moment du switch restait bloqué À VIE (le watchdog re-soumettait la dernière frame `mm[1]≠0`) ; désormais une **frame souris NEUTRE** est envoyée au switch de mode et à la bascule hybride→non-hybride. (`WiiMoteController.cs`)
- **Version synchronisée 3.0.0.23** — WiimoteGun app, WiimoteGun.Service et HmHost. (`AssemblyInfo.cs` des deux projets, `HmHost.csproj`)

---

## [3.0.0.22-V57d] - 2026-10-04

### Added
- **V57d: Nouveau 3e mode d'input « RawInput (UMDF2) » — remplacement vmulti sans signature Microsoft, sans test mode, sans UAC** — Préparation au durcissement Windows 11 26H2 qui bloquera à terme les pilotes kernel aux vieilles signatures cross-signées (les vmulti a/b/c/d). Le nouveau mode repose sur **HIDMaestro** (minidriver HID **UMDF2** hébergé par le `mshidumdf.sys` inbox de Windows, certificat **auto-signé trusté machine** Root + TrustedPublisher — aucune signature Microsoft/EV requise, aucun pilote kernel tiers) et fournit **4×(souris absolue + clavier) HID par joueur avec identités DURABLES P1..P4 et VID UNIQUE PAR JOUEUR** (les VID vmulti exacts 0x001F/0x002F/0x003F/0x004F — mmes device paths/container ids chaque session — exigence RawInput/DemulShooter/émulateurs) :
  - **Identification par joueur (mêmes schémas que vmulti)** : 4 profils `wiimotegun-mkb-p1..p4`, un VID UNIQUE par joueur = les VID vmulti exacts (0x001F/0x002F/0x003F/0x004F) → hardware IDs `HID\VID_001F&UP:0001_U:0002` (souris) / `HID\VID_001F&UP:0001_U:0006` (clavier) IDENTIQUES au schéma vmulti : la détection/autoconfig RetroBat et DemulShooter est conservée et reste stable sur toutes les machines (l'instance ID généré par Windows n'est plus nécessaire à l'identification). Un KB virtuel séparé par wiimote est embarqué dans chaque device (collection clavier Col02, la souris en Col01 — uniquement les collections utiles, aucun doublon dans les émulateurs).
  - **`HmHost\` (NOUVEAU projet .NET 10 x64)** : hôte console du SDK HIDMaestro — install idempotent du pilote, création des 4 controllers avec clés d'identité « P1 »..« P4 », soumission des rapports **verbatim** (frames vmulti complètes : souris absolue report 0x03 + clavier report 0x07 UNIQUEMENT — descripteurs repris OCTET POUR OCTET de `vmulti.h` ; la souris relative report 0x04 a été RETIRÉE : inutilisée, elle créait un doublon de souris par joueur dans les émulateurs), **watchdog de re-soumission (150 ms)** qui re-soumet la dernière frame de chaque joueur tant qu'une entrée est tenue ou récente (un relâchement de touche/bouton perdu ne reste jamais bloqué — les rapports identiques consécutifs sont sans effet pour mouhid/kbdhid), pipe nommé `WiimoteGunHmHost` (protocole ASCII `PING/ACTIVATE/DEACTIVATE/STATUS/QUIT` + frames `MOUSE:/MOUSER:/KEYS:` fire-and-forget), mutex single-instance, log `HmHost.log`. (`HmHost\HmHost.csproj`, `HmHost\Program.cs`, `HmHost\profiles\wiimotegun-mkb-p1..p4.json`)
  - **Service** : `HmHostSupervisor` lance/surveille/arrête HmHost **en SYSTEM via le service → AUCUN UAC, aucun reboot** (restart automatique si crash tant que le mode est actif). Pipe `WiimoteGunService` : nouvelles commandes `HM_ACTIVATE`/`HM_DEACTIVATE`/`HM_STATUS` — **seules commandes avec réponse** (une ligne `OK …`/`ERR …` ; toutes les commandes historiques restent one-way et rétrocompatibles) ; cleanup HmHost ajouté à la sortie/crash client (`UNREGISTER_CLIENT`, ClientWatcher) et à l'arrêt du service. (`HmHostSupervisor.cs`, `PipeServer.cs`, `WiimoteGun.Service.csproj`)
  - **App** : 3e choix dans Options > General — dropdown désormais « SendInput » / « RawInput (VMulti) » / « RawInput (UMDF2) » (mapping libellé↔enum) ; à la sélection UMDF2, dialogue de validation (installation pilote + certificat auto-signé via le service, sans UAC) puis activation immédiate avec retour d'état — échec → retour au mode précédent avec message clair ; sortie du mode → désactivation propre ; tooltip bilingue complet. Au démarrage de l'app, activation silencieuse en arrière-plan si le mode est sélectionné. (`Options.cs` enum `MouseMode.RawInputUmdf = 2`, `OptionsControl.cs`, `OptionsControl.Designer.cs`, `Program.cs`)
  - **Périphériques virtuels app** : `VirtualHmMouse` (clone de `VirtualVMultiMouse` : scaling 0-65535→0-32767, boutons + edge trigger gâchette, rapports relatifs sbyte, ResetAll) et `VirtualHmKeyboard` (clone de `VirtualVMultiKeyboard` : mapping InputKey, modificateurs, conversion scancodes HID, 6-key rollover) — frames envoyées via `HmFrameClient` (connexion pipe PERSISTANTE vers HmHost, auto-reconnexion silencieuse, frames abandonnées si l'hôte est absent, alerte log throttlée). Factory `WiiMoteController` : 3e branche, notification de connexion et gestion de la fenêtre cachée mises à jour. (`Core\VirtualHmMouse.cs`, `Core\VirtualHmKeyboard.cs`, `Services\ServiceClient.cs`, `Core\WiiMoteController.cs`, `WiimoteGun.csproj`)
  - **Replis intacts** : SendInput et RawInput (VMulti) inchangés ; échec d'activation UMDF2 → le mode précédent est conservé (jamais de blocage) ; service trop ancien → la mécanique existante de mise à jour du service s'applique.
  - **`Uninstall-Umdf2Host.ps1`** : uninstallateur provisoire (phase de test) — arrête HmHost, supprime les device nodes HIDMaestro, les packages de pilote (`hidmaestro.inf` + `hidmaestro_xusb.inf`) et le certificat `HIDMaestroTestCert` (magasins LocalMachine Root + TrustedPublisher). Réinstallation propre = sélectionner à nouveau « RawInput (UMDF2) ».
  - **[V57e] Parité vmulti « devices à la demande » + fiabilisation clavier** :
    - `HmHost` `ACTIVATE` ne crée plus AUCUN device (pilote prêt seulement) ; nouvelles commandes `ACTIVATE_P:n` / `DEACTIVATE_P:n` / `DEACTIVATE_P_ALL` / `DEACTIVATE_P_EXCEPT:n1,n2` créent/suppriment le device du joueur à la demande (identité « Pn » stable à travers les cycles, comme le flux enable/disable vmulti) → **les devices n'existent QUE pour les wiimotes connectées** : EmulationStation ne voit jamais de lightguns « fantômes », et la connexion d'une wiimote active SA souris virtuelle.
    - Service : `ENABLE_Pn` / `DISABLE_Pn` / `REMOVE_MOUSE_ALL` / `REMOVE_MOUSE_EXCEPT` routés vers HmHost quand le mode UMDF2 est actif (sinon devcon vmulti inchangé). App : `WiiMoteController` envoie `EnablePlayer` aussi en mode RawInputUmdf.
    - Clavier fiable : **battement d'état 500 ms** dans `VirtualHmKeyboard` (re-envoi complet app→HmHost, thread-safe) + watchdog 150 ms HmHost (re-soumission hôte→device) → un relâchement perdu ne peut plus rester bloqué ; kbdhid/mouhid différencient les rapports consécutifs, donc les rapports identiques ne produisent aucun événement.
  - **[V57e3] Déconnexion → suppression du device** : en mode RawInputUmdf, `WiiMoteController` envoie `DisablePlayer` à la déconnexion de la wiimote → le service → HmHost supprime le controller du joueur (identité conservée, recréation avec les MÊMES chemins à la reconnexion). Le mode vmulti garde son comportement « persistent ».
  - **[V57f] Détection « comme les autres lightguns » (VID/PID dans les chemins) + renommage** :
    - Patch du SDK HIDMaestro (source MIT locale, commit exact v1.10.1) : `DeviceNodeCreator` dérive désormais l'énumérateur ROOT du VID/PID du profil (`ROOT\VID_001F&PID_BACC\HM_<token>`) au lieu du générique « HIDClass » → enfants `HID\VID_001F&PID_BACC&Col01/Col02` et chemins d'interface RawInput `\\?\HID#VID_001F&PID_BACC&Col01#...` : **EmulationStation et DemulShooter identifient les guns par VID/PID exactement comme les Sinden/GUN4IR/Aimtrak**, stable sur toutes les machines (plus de dépendance aux instance IDs Windows). Même schéma que la branche Xbox legacy du SDK ; sweeps/ownership inchangés (énumérateurs VID_* déjà gérés avec preuve).
    - SDK rebuild local avec payload **octet pour octet identique** à la release (extraction des ressources embarquées du DLL officiel, manifest hash inchangé → InstallDriver fast-path conservé, AUCUNE réinstallation du pilote). Les anciens devices « HIDCLASS » sont balayés automatiquement par le ghost cleanup et recréés sous le nouveau nom.
    - Renommage des périphériques : **« Wiimote4Guns P1..P4 »** (ex « Wiimote4Guns Virtual Device Pn ») dans les profils `wiimotegun-mkb-p1..p4` (`name` + `productString`) ; chemins/identités P1-P4 inchangés → aucun re-lock DemulShooter.
    - `Uninstall-Umdf2Host.ps1` : patterns étendus aux formes `ROOT\VID_00(1|2|3|4)F&PID_BACC` / `HID\VID_00(1|2|3|4)F&PID_BACC`.
- **Version synchronisée 3.0.0.22** — WiimoteGun app et WiimoteGun.Service. (`AssemblyInfo.cs` des deux projets)

---

## [3.0.0.21-V56h] - 2026-10-03

### Fixed
- **V56h: Bug de quotation dans le lancement de la mise à jour (test réel)** — Premier test de mise à jour live échoué : `AppDomain.CurrentDomain.BaseDirectory` finit par un antislash, donc `-AppDir "S:\...\Wiimote4Guns\"` rendait le guillemet final **échappé** (`\"`) dans le parsing Windows/PowerShell → la valeur absorbait ` -WaitPid 24836` (`App directory: S:\...\Wiimote4Guns" -WaitPid 24836`) → `Test-Path` échouait. **Correctifs** :
  - **`AppUpdateLauncher.cs`** : antislash final retiré du dossier app + helper `QuoteArg` (double les antislashes finaux avant le guillemet fermant — règle MSVCRT) pour tous les arguments.
  - **`WiimoteUpdate.ps1`** : normalisation défensive de `$AppDir` (retrait de tout à partir du premier guillemet parasite) → les **anciennes builds déjà déployées** (lanceur bogué) peuvent quand même se mettre à jour vers la nouvelle release ; l'attente de sortie de l'app s'exécute désormais **toujours par nom de processus** (le `-WaitPid` peut avoir été absorbé par le bug → `$WaitPid = 0`) avec kill de sécurité des instances récalcitrantes. (`AppUpdateLauncher.cs`, `WiimoteUpdate.ps1`)
- **Version synchronisée 3.0.0.21** — WiimoteGun app et WiimoteGun.Service. (`AssemblyInfo.cs` des deux projets)

---

## [3.0.0.20-V56g] - 2026-10-03

### Changed
- **V56g: Mise à jour sans 7-Zip installé — repli sur 7za.exe local** — Le script de mise à jour (`WiimoteUpdate.ps1`) utilise en priorité le **7za.exe standalone livré avec l'app** (même dossier) : la mise à jour directe fonctionne même quand 7-Zip n'est PAS installé sur la machine. Ordre de recherche : 1) `7za.exe` du dossier de l'app ; 2) `7z.exe`/`7za.exe` du PATH ; 3) dossiers Program Files de 7-Zip. Le `7za.exe` est désormais livré avec l'app (Content csproj, v25.01 — même binaire que le dossier déployé) et **mis à jour pendant le process de copier/remplacer** : comme il peut être EN COURS D'EXÉCUTION au moment de l'extraction (verrouillé), il est traité EN DERNIER avec l'astuce du renommage (un exe en cours peut être renommé sous Windows : ancien → `7za.exe.old`, nouveau posé, ancien nettoyé au mieux) — les archives de release n'ont qu'à inclure le nouveau `7za.exe` à leur racine pour qu'il se mette à jour. Message d'erreur explicite si aucun outil d'extraction n'est trouvé. Syntaxe ps1 validée (Parser .NET). (`WiimoteUpdate.ps1`, `7za.exe` + `WiimoteGun.csproj`)
- **Version synchronisée 3.0.0.20** — WiimoteGun app et WiimoteGun.Service. (`AssemblyInfo.cs` des deux projets)

---

## [3.0.0.19-V56f] - 2026-10-03

### Fixed
- **V56f: État hors-ligne distinct + bug HasChecked corrigé** — Réponse au retour « pas de connexion internet ? » :
  - **Non-blocage garanti** : la vérification tourne sur un thread `IsBackground` avec timeouts HTTP 10s — l'app ne bloque jamais, même sans internet.
  - **Bug corrigé** : `HasChecked` était positionné au LANCEMENT du check (pas à sa complétion) → le voyant Home pouvait afficher un vert « Up to date » trompeur pendant que la vérification était en cours ou échouait. `_checkCompleted` est désormais positionné dans un `finally` sur TOUS les chemins de sortie (résultat, pas d'asset, parse invalide, exception).
  - **Nouvel état OFFLINE** (`AppUpdateChecker.Offline`) : échec de connexion (pas d'internet / DNS / timeout / proxy, via `WebException`) → le voyant Home affiche un point **ORANGE** + « **Offline** » au lieu d'un vert trompeur ; « Update now » désactivé (la page des releases reste accessible). La notification tuile ne s'affiche pas hors-ligne (aucune info de mise à jour). (`AppUpdateChecker.cs`, `HomeControl.cs`)
- **Version synchronisée 3.0.0.19** — WiimoteGun app et WiimoteGun.Service. (`AssemblyInfo.cs` des deux projets)

---

## [3.0.0.18-V56e] - 2026-10-03

### Added
- **V56e: Mise à jour de l'app Wiimote4Guns — détection GitHub, notification tuile, voyant Home, mise à jour directe** — Fonctionnalité complète d'auto-mise à jour :
  - **Détection** (`Core/AppUpdateChecker.cs`) : vérification UNE FOIS par session en arrière-plan via l'API GitHub `repos/Aynshe/WiimoteGun/releases` (liste — `/releases/latest` renvoie 404 car toutes les releases sont des pré-releases) ; asset préfixe **« Wiimote4Guns_ »** uniquement (les « Update_for_... » sont exclus), extension d'archive retirée AVANT le parsing de version (bug trouvé en test réel : « .7z » fusionnait dans la version « 2.3.5.3.7 »), version = chiffres après le dernier « v », plafonnée à 4 composantes ; MAJ dispo si > version en cours (ex. actuel : asset v2.3.5.3 < v3.0.0.18 → à jour).
  - **Notification tuile** : réutilise `Program.Notify`/`NotifyForm` — durée paramétrable (6s pour la MAJ) et **empilement vertical** (une tuile descend sous celles déjà affichées). Programmée 1×/session **20s après la première connexion Wiimote**, **jamais pendant un game-start ES** (revérification toutes les 30s tant qu'un jeu tourne, fenêtre de grâce 10 min).
  - **Voyant Home** (`HomeControl`) : contrôles **Designer VS** (norme Concepteur de vue) — point ROUGE + « Update available: vX » / point VERT + « Up to date » / gris « Checking... » pendant la vérification (sondage 2s) ; panneau cliquable (curseur main) → menu « Open the release page » (navigateur) / « Update now » (désactivé si à jour).
  - **Mise à jour directe** (`Services/AppUpdateLauncher.cs` + **`WiimoteUpdate.ps1`** nouveau, livré en Content avec l'app) : script PowerShell exécuté avec le **compte utilisateur en cours (jamais admin)** dans une console visible — attend la sortie de l'app (PID, 30s + kill de sécurité), télécharge le .7z, l'extrait via 7-Zip (PATH/Program Files), **n'utilise que le CONTENU du dossier créé par l'archive**, copie tout dans le dossier de l'app (y compris le script lui-même : une release future peut le modifier), **SAUF le dossier `WiimoteGun.Service` dont les fichiers sont routés vers `update_service`** (l'exe service ACTIF n'est jamais écrasé ; le mécanisme admin existant les applique), nettoie le temporaire puis **relance WiimoteGun.exe**. L'app se désenregistre d'abord du service (`UNREGISTER_CLIENT:UPDATE` — le CrashWatchdog ne la « relance » pas en pleine mise à jour) via `Program.ExitApplicationForUpdate()`.
  - Validation : syntaxe ps1 (Parser .NET), parsing de version sur les assets réels du dépôt (2.3.5.3 / 0.0.0.0 / 2.3.5.1 / .zip), flux complet simulé contre l'API live. (`AppUpdateChecker.cs` + `AppUpdateLauncher.cs` + `WiimoteUpdate.ps1` + `NotifyForm.cs` + `Program.cs` + `WiimoteControllerManager.cs` + `HomeControl.cs`/.Designer + csproj)
- **Version synchronisée 3.0.0.18** — WiimoteGun app et WiimoteGun.Service. (`AssemblyInfo.cs` des deux projets)

---

## [3.0.0.17-V56d] - 2026-10-02

### Fixed
- **V56d: Info-bulles de la page mapping GamePad absentes via l'icône manette (overlay)** — Les info-bulles « avaient tendance à ne pas s'afficher » sur la page GamePad ouverte via l'icône manette (contrôle créé au démarrage de l'overlay, avant tout handle, et réutilisé — son ToolTip partagé survivait aux cycles répétés de destruction/recréation des handles de lignes de `LoadData()`), alors que l'accès secondaire « Configure... » (instance fraîche dans un dialogue modal) les affichait toujours. Cause structurelle : contrairement à la page mapping Souris, qui crée un **ToolTip neuf à chaque reconstruction** de lignes (motif qui n'a jamais eu le problème), la page GamePad utilisait un unique ToolTip partagé né à la construction. **Correctif** : `RecreateToolTip()` appelé à chaque `LoadData()` — le composant ToolTip est recréé (délais 300ms/8s/100ms), les info-bulles statiques ré-enregistrées (chkIsEmulator/chkIsFolder/chkIsSystem/btnApply/chkAutoLoad par défaut), les dynamiques (lblDetectedApp, chkAutoLoad) étant ré-enregistrées par `UpdateStatusLabels`/`CheckAutoLoadStatus` sur le composant neuf. (`GamePadMappingControl.cs`)
- **Version synchronisée 3.0.0.17** — WiimoteGun app et WiimoteGun.Service. (`AssemblyInfo.cs` des deux projets)

---

## [3.0.0.16-V56c] - 2026-10-02

### Fixed
- **V56c: Encodage de la modale tuiles réparé (mojibake)** — Les émojis de la modale de chargement des profils s'affichaient en hiéroglyphes : les éditions PowerShell de la V56b avaient lu le fichier UTF-8 comme ANSI puis l'avaient réécrit en UTF-8 (double-encodage). Réparation par roundtrip inverse (UTF-8→CP1252→UTF-8) : accents et 6 émojis restaurés (« 🌐 All profiles » etc.), zéro « ? » résiduel, fichier réécrit en UTF-8 avec BOM. Le fichier Designer des options est intact (round-trip ANSI byte-préservant, émojis « 🤌 » vérifiés). (`EsProfileTileDialog.cs`)
- **V56c: Durcissement ViGEm/XInput — plus de double-action dans la modale** — Avec `Use XInput (ViGEmBus)` activé, les boutons A/B de la wiimote atteignaient le gamepad virtuel ET le sondage XInput de la modale → risque de double activation (native + poll). A/B sont désormais supprimés à la source pendant que la modale est ouverte (comme le DPad depuis V52) ; les manettes XInput physiques ne sont pas affectées. (`WiiMoteController.cs`)
- **Version synchronisée 3.0.0.16** — WiimoteGun app et WiimoteGun.Service. (`AssemblyInfo.cs` des deux projets)

---

## [3.0.0.15-V56b] - 2026-10-02

### Fixed
- **V56b: CAUSE RACINE DU CRASH IDENTIFIÉE ET ÉLIMINÉE — corruption de tas par le sondage DInput de la modale tuiles (joyGetPosEx)** — Analyse du dump WER de l'utilisateur (crash 2026-10-01 16:33:21, 7s après l'ouverture de la 2e modale tuiles, pendant Virtua Cop en mode verrouillé Mouse43 + profil GamePad chargé depuis la modale) : la stack complète montre la corruption détectée dans `joyGetPosEx → winmm!joyOpen → dinput!JoyReg_GetConfig → CM_Get_DevNode_Registry_PropertyW → cfgmgr32!SpInfFreeInfFile → RtlFreeHeap` (FAILURE_BUCKET: `HEAP_CORRUPTION_c0000374_cfgmgr32.dll!SpInfFreeInfFile`), appelée depuis la pompe de messages WinForms = le timer 50ms de `EsProfileTileDialog.PollControllers`. Le dump de la machine dev (17:32, mêmes tests de modale) confirme la même classe c0000374. Mécanisme : l'API joystick **winmm héritée** (années 90, jamais conçue pour du 20 Hz) martelée sur 16 IDs — dont la majorité fantômes — force à chaque appel le chemin coûteux registre/INF (`CJoyReg_GetConfig` → cfgmgr32 SpInf) qui corrompt les structures INF du tas ; le tas le détecte plus tard et tue le processus (c0000374, silencieux). **Correctif : suppression totale du sondage DInput `joyGetPosEx` de la modale** (méthode, P/Invoke, struct JOYINFOEX et constantes supprimés). La navigation reste intégralement couverte par : la Wiimote physique (c'est elle qui ouvre la modale — câblage `NotifyWiimote*`), les manettes XInput (`XInputGetState`, API moderne sûre), et les gamepads VMulti pilotés par la Wiimote ne doublonnent jamais par conception (DPad supprimé à la source pendant que la modale est ouverte). Seule perte : la navigation par un joystick DInput physique pur (cas marginal, la Wiimote reste requise pour ouvrir la modale). (`EsProfileTileDialog.cs`)
- **Version synchronisée 3.0.0.15** — WiimoteGun app et WiimoteGun.Service. (`AssemblyInfo.cs` des deux projets)

---

## [3.0.0.14-V56a] - 2026-10-01

### Fixed
- **V56a: Fire rumble GamePad — parité complète avec le trigger du mode souris** — La vibration du bouton Fire sélectionné en mode GamePad était une impulsion unique (faible). Désormais **parité totale** avec la vibration de gâchette du mode souris : l'intensité et la durée (ms) viennent d'« Assign wiimotes » (`TriggerWeaponRumble` lit `GetRumbleDurationMs`/`GetRumbleIntensity`), et la **répétition continue** tourne tant que le bouton de tir est maintenu à l'écran quand `AllowContinuousRumble` est activé (`GetRumbleRepetitionMs`), avec arrêt au relâchement ; le désactivement dans Assign (`GetEnableWeaponRumble`) est respecté. `RumbleRepetitionCallback` accepte la source GamePad (`_gpFireHeldRumble`) en plus de `_isTriggerPressed && _hasIRSensor`. (`WiiMoteController.cs`)
- **V56a: TC Bi-Pedal (pedal left/right) vibre à la planque** — Le mode planque TC bi-pédal ne déclenchait pas la vibration recharge. Comme pour la planque TC à bouton unique : la vibration se déclenche à l'**entrée en planque** (transition ON→OFF écran), au moment où l'entrée pédale est RELÂCHÉE (= se planquer = rechargement TC), une fois par transition — **en mode Souris et en mode GamePad** (trackers `_mouseBiPedalWasOnScreen` / `_gpBiPedalWasOnScreen`). (`WiiMoteController.cs`)
- **Version synchronisée 3.0.0.14** — WiimoteGun app et WiimoteGun.Service. (`AssemblyInfo.cs` des deux projets)

---

## [3.0.0.13-V56] - 2026-10-01

### Added
- **V56: Vibrations GamePad (trigger + reload physiques) avec boutons configurables + TC Cover** — Le mode GamePad reçoit désormais les effets de vibration :
  - **Nouvelle ligne « Fire Button »** (page remap GamePad, section Output Mode) : sélection du bouton physique de tir (défaut **B** Wiimote) — son appui **à l'écran** déclenche la vibration d'arme (même fonction que la vibration de gâchette en mode souris, gated sur l'option de vibration par joueur) ; les appuis hors écran relèvent du reload hors écran (mode Trigger) qui a sa propre vibration.
  - **Nouvelle ligne « Reload Button »** (dans la section Off-Screen Reload, défaut **bouton 2**) : son appui déclenche la **vibration de recharge** à l'écran comme hors écran, que le Off-Screen Reload soit activé ou non — gated sur le réglage global Options > Gestures > Reload Rumble (ON par défaut) + l'override par profil Reload Rumble. Les modes Off-Screen Trigger (A/B) et **Auto** du GamePad vibrent déjà (V55y) — l'attache au bouton 2 par défaut est assurée pour le bouton reload.
  - **TC Cover (Souris + GamePad)** : la vibration de recharge se déclenche à l'**entrée en planque** (transition ON→OFF écran) — l'entrée TC étant inversée, c'est le relâchement hors écran du bouton TC qui correspond au rechargement en jeu ; une vibration par entrée en planque. (`WiiMoteController.cs`, `GamePadMappings.cs`, `GamePadMappingControl.cs`)
- **Version synchronisée 3.0.0.13** — WiimoteGun app et WiimoteGun.Service. (`AssemblyInfo.cs` des deux projets)

---

## [3.0.0.12-V55z] - 2026-10-01

### Fixed
- **V55z: Curseur d'intensité plafonné à 10 (bug Designer)** — Le TrackBar d'intensité de Options > Gestures n'avait pas `Maximum = 100` : le défaut WinForms (**10**) plafonnait la valeur sauvegardée à ≤10 % → réglage perçu « on/off », pas de gradation fine, 100 % impossible, et les styles Short/Long écrasés au plancher 15 ms (« ne fait rien », « très court/très faible »). Le TrackBar va désormais de 0 à 100. (`OptionsControl.Designer.cs`)
- **V55z: Intensité à échelle perceptible (0,25×→3×)** — L'intensité applique désormais un multiplicateur 0,25×..3× sur les impulsions ON du motif (au lieu de 0..1×) : toute la plage du curseur est perceptible et 100 % est clairement le maximum. À 60 % par défaut, Ratchet pulse à ~85 ms, Short ~230 ms, Long ~800 ms ; à 100 %, 3× ces valeurs. (`WiiMoteController.cs`)

### Changed
- **V55z: UI 100 % anglaise** — Le style s'appelle « Ratchet (mechanical) » dans toutes les listes (Options, mapping Souris, mapping GamePad) ; seules les bulles d'info restent bilingues EN/FR. (`OptionsControl.Designer.cs`, `MappingControl.cs`, `GamePadMappingControl.cs`)
- **V55z: Nouveau style « Custom » — créez votre motif de tics** — Options > Gestures : trois nouveaux paramètres visibles quand le style Custom est sélectionné : **Ticks (Custom)** (graduation horizontale 1-10, un tic par cran), **Tic ON (ms)** (10-500) et **Tic OFF (ms)** (10-500) — le motif joue N tics ON/OFF selon vos réglages. Contrôles instanciés dans le Designer VS (norme Concepteur de vue, BeginInit/EndInit), visibilité conditionnelle au style Custom, zone expérimentale décalée +130px. Le style Custom est aussi sélectionnable par profil (pages Souris/GamePad, utilise les paramètres tics globaux). Nouvelles options `ReloadRumbleCustomTicks` (défaut 4), `ReloadRumbleCustomOnMs` (60), `ReloadRumbleCustomOffMs` (90). (`Options.cs`, `OptionsControl.cs` + Designer, `WiiMoteController.cs`, `MappingControl.cs`, `GamePadMappingControl.cs`)
- **Version synchronisée 3.0.0.12** — WiimoteGun app et WiimoteGun.Service. (`AssemblyInfo.cs` des deux projets)

---

## [3.0.0.11-V55y] - 2026-10-01

### Added
- **V55y: Vibration de rechargement (Reload Rumble) — corrigée, paramétrable, styles, overrides par profil** — Nouveau moteur de vibration dédié aux recharges, séparé de la vibration de tir :
  - **Bug corrigé** : l'ancienne vibration de reload partageait le drapeau `_isRumbling` de la vibration d'arme et le verrouillait AVANT la vérification `IsConnected` — une vibration demandée pendant une déconnexion transitoire verrouillait le drapeau pour toujours (« deux longues vibrations puis plus rien », silence total ensuite). Le nouveau séquenceur (`_reloadRumbleTimer` + motif ON/OFF) ne verrouille JAMAIS : chaque chemin se termine par moteur arrêté + drapeau libéré ; le latch de `TriggerWeaponRumble` est aussi corrigé.
  - **Styles** : « Crique-Clique (mécanique) » par défaut — clic-clic-clic puis taloche simulant le rechargement d'une arme ; « Court » (impulsion unique) ; « Long » (continue).
  - **Intensité 0-100%** : met à l'échelle les impulsions ON du motif (le moteur de la Wiimote étant binaire, l'intensité passe par la largeur d'impulsion, plancher 15ms).
  - **Déclencheurs** : séquence Auto hors écran, redirection gâchette hors écran, appui du bouton reload physique (mapping clic droit, à l'écran comme hors écran) et shake reload — **que le Off-Screen Reload soit activé ou non** — + recharges hors écran GamePad (Trigger/Auto). La vibration prend la main sur la vibration d'arme en cours (jamais de conflit moteur).
  - **Options > Gestures** : case « Reload Rumble » **activée par défaut**, curseur d'intensité et liste de style — contrôles ajoutés dans le **Designer VS** (modifiables dans le Concepteur de vue, norme V55o ; zone expérimentale décalée +130px).
  - **Overrides par profil** : pages mapping **Souris** (case + combos Style/Intensité) et **GamePad** (Reload Rumble + Style + Intensité via les rangées existantes) ; valeurs « Default » = suivre le global ; réinitialisation des overrides sur le profil par défaut conservée. (`WiiMoteController.cs`, `Options.cs`, `PlayerMappings.cs`, `GamePadMappings.cs`, `OptionsControl.cs` + Designer, `MappingControl.cs`, `GamePadMappingControl.cs`)
- **Version synchronisée 3.0.0.11** — WiimoteGun app et WiimoteGun.Service. (`AssemblyInfo.cs` des deux projets)

---

## [3.0.0.10-V55x] - 2026-10-01

### Changed
- **V55x: LocalDumps WER écrits DANS le dossier de l'app** — Les dumps de crash ne vont plus dans `C:\ProgramData\WiimoteGun\CrashDumps` : la configuration WER LocalDumps est faite au premier `REGISTER_CLIENT` (chemin de l'exe résolu) et cible `<dossier app>\CrashDumps` (ex. `S:\RetroBat\plugins\Wiimote4Guns\CrashDumps`) — l'app n'écrit jamais hors de son propre dossier. La clé registre HKLM LocalDumps reste nécessaire (exigence Windows : c'est WER/WerFault, pas l'app, qui écrit le dump). Idempotent, une fois par cycle de vie du service. (`CrashWatchdog.cs` service)
- **Version synchronisée 3.0.0.10** — WiimoteGun app et WiimoteGun.Service. (`AssemblyInfo.cs` des deux projets)

---

## [3.0.0.9-V55w] - 2026-10-01

### Diagnostics
- **V55w: Cause du crash identifiée — corruption de tas native (c0000374) + capture de dumps pour analyse** — Le crash rapporté par l'utilisateur (modale tuiles profils, appui long PLUS, parfois passée derrière le jeu) est confirmé par le journal d'événements Windows : **APPCRASH WiimoteGun.exe, exception `c0000374` (STATUS_HEAP_CORRUPTION)**, détecté dans ntdll — sur DEUX versions (3.5.6.1 déployée et 3.0.0.8 dev, reproduction locale à 11:36:04). C'est une corruption de tas **native** : aucune exception managée n'est levée, d'où l'absence totale de trace dans WiimoteGun.log (les logs continuent ensuite car les scripts ES tournent en instance simplifiée). La corruption est détectée tardivement par le gestionnaire de tas → le moment du crash (modale ouverte) n'indique PAS le site du bug. Audit statique des chemins natifs à haute fréquence (WiimoteLib HID, VMulti reports, joyGetPosEx/XInput de la modale) : marshaling borné correct — la cause racine exige le dump mémoire. Le service active désormais **WER LocalDumps** pour WiimoteGun.exe (dumps complets dans `C:\ProgramData\WiimoteGun\CrashDumps`, 3 conservés) : le prochain crash sera capturé et analysable avec WinDbg (`!heap -p -a` identifie le corrupteur exact). Le CrashWatchdog V55v relance automatiquement l'app après ce type de crash (processus mort sans unregister → relance utilisateur non-admin). (`CrashWatchdog.cs` service)
- **Version synchronisée 3.0.0.9** — WiimoteGun app et WiimoteGun.Service. (`AssemblyInfo.cs` des deux projets)

---

## [3.0.0.8-V55v] - 2026-10-01

### Added
- **V55v: Watchdog crash/gel via le service (relance JAMAIS en admin)** — Nouveau `CrashWatchdog` dans le service : surveille le client WiimoteGun enregistré via `REGISTER_CLIENT:PID` et le relance **uniquement en tant qu'utilisateur interactif de la console** (`WTSQueryUserToken` + `CreateProcessAsUser`, jeton utilisateur filtré d'intégrité moyenne — jamais SYSTEM, jamais élevé). Détection **CRASH** : le processus s'est terminé sans `UNREGISTER_CLIENT` → relance. Détection **GEL** (« ne répond pas ») : le fichier de battement `WiimoteGun.heartbeat` (écrit toutes les 5s **par le thread UI** de l'app via `AppHeartbeat`, timer WinForms) est périmé depuis 90s → kill + relance. Les sorties volontaires (UNREGISTER_CLIENT, y compris le flux `:RESTART` de mise à jour) désarment le watchdog ; grâce de 30s après enregistrement ; protection anti-boucle (max 5 redémarrages, compteur remis à zéro après 2 min de vie) ; nettoyage des périphériques virtuels existant conservé via le ClientWatcher. (`CrashWatchdog.cs` service, `AppHeartbeat.cs` app, `PipeServer.cs`, `Program.cs`)
- **V55v: Diagnostics du gel de sélection de jeu** — Trace INFO avant l'ouverture du dialogue fichier de jeu (OpenFileDialog) et du dossier (FolderBrowserEx) : si l'app gèle dans le namespace shell (lecteur réseau déconnecté, extension shell — rapport utilisateur : gel à la sélection du fichier de jeu), la dernière ligne du log identifie le dialogue en cause. Le CrashWatchdog ci-dessus relance automatiquement l'app dans ce scénario. (`EsScriptIntegration.cs`)

### Fixed
- **V55v: BT-Reset seulement sur preuves d'une tentative réelle** — Le watchdog BT ne demande PLUS un reset simplement parce qu'« aucune Wiimote n'est connectée » (l'utilisateur peut avoir éteint sa Wiimote). Le reset n'est envoyé QUE sur preuves depuis l'armement : (1) une tentative de connexion a échoué (nouvel abonnement à `WiimoteManager.ConnectionFailed`), (2) un inquiry Bluetooth **réel** (≥300ms, pas une énumération instantanée/cache de périphérique mémorisé) a trouvé un périphérique discoverable = une Wiimote était en mode appairage, ou (3) la pile radio est **coincée** (V53 : `BtWedgeSuspicions ≥ 8`, exposé publiquement) — le BT-Reset automatisant le correctif manuel officiel. Sans preuve : reset ignoré + réarmement, log explicite. (`WiimoteControllerManager.cs`, `WiimoteManager.Tasks.cs`)
- **Version synchronisée 3.0.0.8** — WiimoteGun app et WiimoteGun.Service. (`AssemblyInfo.cs` des deux projets)

---

## [3.0.0.7-V55u] - 2026-10-01

### Changed
- **V55u: Redirection gâchette → reload illimitée hors écran** — Quand le Off-Screen Reload est activé (Trigger, global ou par profil), **chaque impulsion physique** de la gâchette hors écran est redirigée vers l'input reload (Mouse Right) **sans limite de 1× par session** : l'état redirigé reflète l'état physique de la gâchette (appui et maintien), avec log + vibration au front montant de chaque nouvel appui. **Seul le mode « Auto » du Off-Screen Reload reste limité à 1× par session hors écran** (séquence automatique au passage hors écran, cooldown 250ms). (`WiiMoteController.cs`)
- **Version synchronisée 3.0.0.7** — WiimoteGun app et WiimoteGun.Service. (`AssemblyInfo.cs` des deux projets)

---

## [3.0.0.6-V55t] - 2026-10-01

### Changed
- **V55t: Hors écran — SEULE la gâchette est verrouillée, le bouton reload physique est libre et illimité** — Restructuration unifiée du bloc off-screen du mode Souris (branche non-TC) : la **gâchette** (Mouse Left = tir railshooter) reste **toujours verrouillée** hors écran (redirection 1× par session vers l'input reload si Off-Screen Reload est activé, anti-accident au retour écran conservé) ; le **bouton physique de reload** (Mouse Right) passe désormais **librement et sans limite d'impulsions** hors écran, **que le Off-Screen Reload soit activé ou non** (l'ancien code le consommait dans la limite 1× par session hors écran, ou le bloquait totalement quand l'option était désactivée) ; le clic milieu passe également. Le mode Planque TC (Time Crisis) garde son blocage total inchangé (tout est masqué en planque). (`WiiMoteController.cs`)
- **Version synchronisée 3.0.0.6** — WiimoteGun app et WiimoteGun.Service. (`AssemblyInfo.cs` des deux projets)

---

## [3.0.0.5-V55s] - 2026-10-01

### Fixed
- **V55s: Trigger (Mouse Left) bloqué hors écran** — Régression : en visant HORS écran, le clic trigger (bouton mappé Mouse Left = tir railshooter) passait brut jusqu'à la souris virtuelle et déclenchait le tir, lorsque ni le mode Planque TC ni le Off-Screen Reload n'étaient actifs (branche `else` manquante dans le bloc off-screen). Désormais, hors écran : **toutes les sorties souris (Left/Middle/Right) sont strictement bloquées** — le seul cas où un appui hors écran produit une action est le Off-Screen Reload actif (Options > Gestures global OU override par profil), qui redirige la gâchette vers l'input reload (Mouse Right, 1x par session hors écran). Anti-accident conservé : une gâchette maintenue hors écran ne tire pas au retour à l'écran (`_suppressLeftClickUntilRelease`). (`WiiMoteController.cs`)
- **Version synchronisée 3.0.0.5** — WiimoteGun app et WiimoteGun.Service. (`AssemblyInfo.cs` des deux projets)

---

## [3.0.0.4-V55r] - 2026-10-01

### Fixed
- **V55r: BT-Reset ne laisse plus de périphériques désactivés** — Le cycle V55 (`disable =Bluetooth` / `enable =Bluetooth`) désactivait **explicitement tous** les périphériques de la classe Bluetooth (nœuds enfants BTHENUM/BTHLEENUM : périphériques appairés, RFCOMM, PAN…), et le `enable` global échouait sur certains enfants (parent pas encore redémarré) → ils restaient **désactivés en code 22 persistant** dans le gestionnaire de périphériques, devant être réactivés un par un manuellement. Nouveau cycle dans `ResetBluetoothAdapter()` : **Étape 0 (réparation)** — `enable =Bluetooth` pour réactiver les nœuds laissés désactivés par les anciens resets bogués ; **Étape 1** — identification des **radios uniquement** (instance IDs hors `BTHENUM\`/`BTHLEENUM\`, parsing de `devcon find =Bluetooth`) ; **Étape 2** — cycle disable/enable **radio seule** (les enfants suivent leur parent automatiquement, aucun code disable rémanent) ; **Étape 3 (vérification)** — `devcon status =Bluetooth` + jusqu'à 3 passes de réparation `enable =Bluetooth` tant qu'il reste des périphériques désactivés, ERROR explicite avec instructions si des nœuds restent désactivés. Cooldown 30s conservé. (`DriverController.cs` service)
- **V55r: Watchdog BT avec grâce croissante + plafond de tentatives** — Le watchdog client se ré-armait au délai brut (20s chez l'utilisateur) immédiatement après un reset, martelant une pile BT en cours de réinitialisation. Désormais : réarmement avec **période de grâce croissante** (+30s par tentative : délai configuré, puis +30s, +60s), **plafond de 3 resets consécutifs** sans aucune connexion Wiimote avant arrêt définitif du watchdog (ERROR explicite : replug dongle / piles Wiimote), compteur remis à zéro dès qu'une Wiimote se connecte. (`WiimoteControllerManager.cs`)
- **Version synchronisée 3.0.0.4** — WiimoteGun app et WiimoteGun.Service. (`AssemblyInfo.cs` des deux projets)

---

## [3.0.0.3-V55q] - 2026-10-01

### Fixed
- **V55q: Porte d'enregistrement mémoire corrigée** — Un profil de paramètres de jeu entre désormais en mémoire **uniquement s'il est détecté AVEC le tag `-wiimotegun`** (il a reçu le tag au moins une fois : d'un cycle de masquage WiimoteGun d'une version précédente, ou manuellement par l'utilisateur), après vérification de sécurité du contenu. La V55p enregistrait à tort les fichiers **non taggés** au contenu wiimotegun valide et les taggait immédiatement (ex. `SLUS-12345_12345678.ini`, `12345.ini` en test). Un fichier jamais taggé n'est **jamais enregistré ni touché**, quel que soit son contenu : aucun tag ajouté, aucun retrait tenté. Le processus de tag au démarrage (sécurité anti-crash : restaurer l'état hors GamePad des fichiers gérés) s'applique uniquement aux profils **présents en mémoire**. Les GameSettings générés automatiquement par WiimoteGun (51 jeux Dolphin) restent enregistrés à leur création. (`EmulatorProfileAutomator.cs`, `TagMemoryStore.cs`)
- **Version synchronisée 3.0.0.3** — WiimoteGun app et WiimoteGun.Service. (`AssemblyInfo.cs` des deux projets)

---

## [3.0.0.2-V55p] - 2026-09-30

### Added
- **V55p: Mémoire persistante des profils taggés -wiimotegun (PCSX2 / Dolphin / DuckStation)** — Nouveau fichier `wiimotegun_memory.json` (dans le dossier remap, à côté de `game_profile_mappings.json`) qui enregistre les profils gamesettings gérés par le tag `-wiimotegun`. Un profil entre en mémoire uniquement s'il est **détecté taggé** (il a reçu le tag au moins une fois) ou **détecté avec un contenu wiimotegun valide** (nouveau `.ini` ajouté par l'utilisateur). Une fois enregistré, il est **géré à vie** : la mémoire est la source de vérité et prime sur la vérification de contenu (un fichier édité/réécrit par l'émulateur continue d'être masqué en mode Wiimote/Souris et démasqué en mode GamePad). Un profil jamais détecté est **ignoré pour toujours** : aucun tag ajouté, aucun retrait tenté. Les sections sont **séparées par émulateur** (PCSX2 / Dolphin / DuckStation) et le fichier est **clé par service** (`Services.EmulatorTagManager`), extensible pour tout futur service de l'app ; les clés/services inconnus sont préservés à la sauvegarde. Rechargement automatique si le fichier est modifié externalement (pattern V52b), backup `.bak` avant réparation d'un fichier corrompu, prune des entrées dont le fichier a été supprimé (dossier scanné uniquement), enregistrement direct des GameSettings Dolphin générés automatiquement. (`JsonLite.cs` nouveau, `TagMemoryStore.cs` nouveau, `EmulatorProfileAutomator.cs`)
- **Version synchronisée 3.0.0.2** — WiimoteGun app et WiimoteGun.Service. (`AssemblyInfo.cs` des deux projets)

---

## [3.0.0.1-V55] - 2026-09-29

### Added
- **Version synchronisée 3.0.0.1** — WiimoteGun app et WiimoteGun.Service passent tous les deux en `3.0.0.1`. Au démarrage, `CheckAndPromptServiceUpdate()` détecte `3.0.0.1 > installedVersion` et propose la mise à jour automatique via `UpdateService.ps1`. (`AssemblyInfo.cs` des deux projets)
- **V55-D: Off-Screen Reload GamePad** — ComboBox Off/Trigger/Auto dans le profil GamePad. `Trigger` = appui A/B hors écran pour recharger une fois. `Auto` = reload automatique à la sortie de l'écran. (`GamePadMappings.cs`, `GamePadMappingControl.cs`, `WiiMoteController.cs`)
- **V55-E: ComboBox Trigger/Auto (souris)** — ComboBox `Trigger/Auto` à côté de la CheckBox Off-Screen Reload dans le panneau de mapping souris, avec override par profil. (`PlayerMappings.cs`, `MappingControl.cs`, `WiiMoteController.cs`)
- **V55-I: TC Pédale Bi-directionnelle (Souris & GamePad)** — Option `TC Bi-Pedal` avec DEUX ComboBoxes de sélection de bouton (Pedal Left et Pedal Right, défaut D-Pad Left/Right avec choix étendu). Maintien de direction à l'écran, relâche hors écran, bascule d'une pédale à l'autre. (`PlayerMappings.cs`, `GamePadMappings.cs`, `MappingControl.cs`, `GamePadMappingControl.cs`, `WiiMoteController.cs`)
- **V55-J: Lock Mode au Game-Start ES** — Option `Lock active mode on game launch (game-start)` placée dans `Options > ES Scripts`. Verrouille le mode Wiimote (souris/gamepad) au lancement d'un jeu ES, empêche le swap Home accidentel. Se déverrouille automatiquement au game-end. (`Options.cs`, `WiiMoteController.LockMode()`, `Program.cs`, `OptionsControl.cs`)
- **BT Auto-Reset via Service** — Option `BT Auto-Reset (Service)` + délai placée dans `Options > General` (alignée à côté de FPS Mode, sans chevauchement). Si aucune Wiimote ne se connecte dans le délai configuré (défaut 60s), le `WiimoteGun Helper Service` reçoit la commande `BT_RESET` via pipe nommé et effectue un cycle disable/enable de l'adaptateur Bluetooth Windows. Cooldown 30s côté service. Nécessite le service installé. (`DriverController.cs`, `PipeServer.cs`, `ServiceClient.cs`, `WiimoteControllerManager.cs`, `Options.cs`, `OptionsControl.cs`)

### Changed
- **V55-B/C: TC Cover corrigé (souris + gamepad)** — La logique est maintenant **inversée** : viser l'ÉCRAN maintient le bouton TC (sortie de planque) ; viser HORS ÉCRAN relâche (retour en planque). (`WiiMoteController.cs`)
- **V55-F: Template Mapping souris restauré** — Les colonnes Wiimote et Nunchuk démarrent en haut à Y=15 sans chevauchement. La section "Off-Screen Reload / TC Cover / Bi-Pedal" est proprement placée en bas sous les réglages de sensibilité avec défilement vertical complet. (`MappingControl.cs`)
- **V55-G: Boutons Save/Load réorganisés** — Les boutons `💾 Save` et `📂 Load` sont déplacés à droite du bouton `Delete` (même ligne), taille réduite à 90×25. (`MappingControl.Designer.cs`)
- **V55-H: Tooltip "Auto Off-Screen" mis à jour** — Mentionne maintenant que les profils de mapping ont leur propre ComboBox Trigger/Auto. (`OptionsControl.cs`)
- **V55-K: Keyboardpad supprimé du swap Home** — Le mode `Keyboardpad` n'est plus dans la séquence de swap du bouton Home ; il ne génère plus non plus de notification sonore. (`WiiMoteController.cs`)
- **GamePad Layout** — `IR as Mouse in Hybrid Mode` déplacé au-dessus de `Hybrid Toggle`. (`GamePadMappingControl.cs`)

### Fixed
- **TC Tooltip corrected** — Les tooltips de la CheckBox TC Cover dans `MappingControl.cs` et `GamePadMappingControl.cs` reflètent maintenant la logique corrigée V55.
- **Notification bloc else restauré** — Le bloc `else` de la notification de changement de mode restauré.

---

## [2.3.5.0] - 2026-06-15

### Added
- **AZERTY Shifted Key Mapping** - Added support for selecting shifted AZERTY layout keys (digits/OEM) separately in key list (depending on emulator compatibility).

### Fixed
- **Input Simulation Compatibility** - Fixed keyboard simulation compatibility by sending proper hardware scan codes and supporting modifier key simulation.

## [2.3.4.0] - 2026-06-15

### Added
- **Native Options Launch** - The options menu now opens automatically after clicking 'Start Wiimote4Guns' on the first launch, and as long as the 'Don't show this wizard on startup' checkbox is not checked.

### Changed
- **Tray Menu Cleanup** - Removed the deprecated "Options (legacy)" entry from the system tray context menu.

### Fixed
- **Driver Detection Setup Wizard** - Replaced the blocked external `devcon.exe` check with a robust, native C# detection using SetupAPI to verify VMulti driver installation without running external processes.
- **Uninstall State Accuracy** - Fixed a false positive issue where uninstalled drivers were still detected as installed due to lingering `.sys` files or Driver Store registry keys.

## [2.3.3.2] - 2026-03-08

### Fixed
- **Default Remap Generation** - Fixed an issue where `default.remap` could be auto-generated with "polluted" settings from the current session. It now always uses Factory Default values.
- **Initial Mapping Logic** - Re-implemented the default button mapping (P1=5, P2=6, P3=7, P4=8) using a centralized `PlayerMappings.CreateDefault(index)` method to ensure consistency. Corrected P3/P4 mapping errors (was 8/5, now 7/8).
- **Startup Logging** - Added explicit logging of WiimoteGun version and installed Service version in `WiimoteGun.log` at every startup for easier diagnostics.

## [2.3.3.1] - 2026-03-08

### Fixed
- **Cursor Tracking Freeze** - Fixed a bug where the cursor would freeze or block during calibration/re-connection in `WiimoteBar` mode due to invalid interpolation states.
- **Apply/Restart Driver Activation** - Resolved a critical issue where Wiimote drivers were not re-activated after an "Apply/Restart" action. Fixed by adding `UnregisterClient` on exit and increasing IPC timeout to 10s to handle heavy service cleanup.

## [2.3.3.0] - 2026-02-28

### Added
- **Auto-Start with RetroBat** - Added support for automatically launching WiimoteGun at RetroBat startup via a generated script in the EmulationStation start scripts folder.

### Changed
- **Four Corners Tracking Logic** - Four Corners now only uses Automatic (Dynamic) mode for calibration. This method is more reliable but requires a sufficient distance to maintain tracking for all four LEDs across the whole screen. Using a fisheye lens is recommended for close-range setups.

### Removed
- **Manual Calibration for Four Corners** - Disabled manual calibration mode for the 4 Corners layout due to tracking stability issues at close range.
- **Two Wiimote Sensor Bar Layout** - removed the "Two Wiimote Bars" option from the selection menu. This configuration proved too difficult to track accurately at short distances.


## [2.3.2.2] - 2026-02-23

### Added
- Service Update Automation: Implemented `UpdateService.ps1` for automated service maintenance.
- Service Log Rotation: Added 1.5MB limit for `WiimoteGunService.log` with automatic backup to `.bak`.
- Direct PowerShell Execution: Improved update script reliability by launching PS1 directly with elevated privileges.
- Auto-Pause: Added user prompt at the end of update script to verify results.

## [2.3.2.1] - 2026-02-23

### Fixed
- **Lost GamePad Mappings** - Fixed a critical bug where Wiimote IR and Nunchuk Joystick axes were reset to 'None' when saving or applying a profile.
- **VMulti Reconnection Loop** - Resolved an infinite loop in `WiiMoteController.cs` causing log spam and performance issues when switching modes.
- **Gyro Visualizer Z-Order** - Fixed the 3D visualizer appearing behind the borderless overlay; it is now forced to the foreground.
- **Variable Shadowing (CS0136)** - Renamed duplicate variables in the Nunchuk accelerometer block to resolve compilation errors.
- **UI & Code Cleanup** - Removed unused event handlers and simplified object initialization in `OptionsControl.cs`.

## [2.3.2.0] - 2026-02-23

### Added
- **FPS Mode (Alpha DEV)** - Unsatisfactory results (low frequency polling/stutter issues with current IR data).
- **XInput Player Mode** - Added support for XInput mode via ViGEmBus drivers (driver installation required). Can be enabled per profile; DInput remains the default.
- **VMulti Resource Management** - Optimized HID handle sharing and fixed race conditions in the shared client pool.
- **Improved Device Detection** - Faster startup by using static HID enumeration for availability checks.
- **Hybrid GamePad Mode** - Ability to combine virtual gamepad inputs with mouse reports.
- **Experimental Motion Gestures** - Support for Wiimote/Nunchuk accelerometer/gyroscope mapping.
- **GamePad Profile Management** - Save/load and auto-link remap profiles.

## [2.3.1.0] - 2026-02-18

### Fixed
- **MotionPlus + Nunchuk Hotplug** - Fixed a critical issue where hotplugging a Nunchuk into an active MotionPlus would fail to map the accelerometer or cause erratic joystick input.
- **Linked Calibration** - Resolved a race condition where the Nunchuk calibration was being overwritten by MotionPlus passthrough garbage data (`0xA40040` remapping).
- **Passthrough Stability** - Improved the initialization sequence for extensions connected via MotionPlus to ensure correct data parsing pattern (`00 00 00 ...`).

## [2.3.0.0] - 2026-02-16

### Added
- **Full Dolphin Automation** - Automatic generation of `GameSettings` files for 51 games (based on internal GameID -> Name mapping).
- **Automatic Wiimote Profiles** - Dynamic creation of global Dolphin profiles (`P1-wiimotegun.ini` to `P4-wiimotegun.ini`) if missing.
- **Unified Masking Logic** - Standardized behavior across Dolphin, PCSX2, and DuckStation: custom config files are now **ACTIVE** in GamePad mode and **MASKED** (`-wiimotegun`) in Wiimote/Mouse mode to prevent conflicts.
- **GamePad Mode Bypass** - Process monitor optimization to ignore restarts when launching Dolphin or Cemu if a controller is in GamePad mode.

### Fixed
- **Dolphin Indexing** - Corrected indexing in `GameSettings` files (switched from 0-3 to 1-4 for `WiimoteSource` and `WiimoteProfile`).
- **Game Mappings** - Updated `RGSE8P` to `GHOSTSQUAD` and added support for `SC2E8P` (The Conduit v2).
- **PCSX2/DuckStation Internal Inhibition** - Refined INI content modification logic to disable specific devices without requiring global profile renaming.

## [2.2.3.0] - 2026-02-15

### Added
- **Manual paths for Dolphin & Cemu** - Added support for standalone instances of Dolphin and Cemu in the Options menu.
- **Dynamic Standalone UI** - Direct emulator paths are now locked/unlocked based on the Standalone mode toggle, with clear auto-detection hints when disabled.

### Fixed
- **Standalone Profile Indexing** - Fixed a bug where PCSX2 and DuckStation profiles wouldn't update if the emulator folder was selected directly (non-Retrobat structure). 
- **UI Contextual Cleanup** - The "GamePad Mapping" button in the overlay is now conditionally hidden if GamePad swap mode is disabled in options.

## [2.2.2.10] - 2026-02-14

### Fixed
- Fixed initial mouse movement delay (standardized time source related to High Performance Timer option and moved connection vibration to background initialization).
- Optimized application restart delay (decoupled VMulti initialization from main thread and reduced service queue congestion).

## [2.2.2.9] - 2026-02-13

### Added
- **High-Precision Virtual Polling (Hypersampling)** - Complete overhaul of the upsampling mechanism to bypass the native 100Hz Wiimote limitation.
- **High-Resolution System Timer** - Integrated `timeBeginPeriod(1)` to force Windows into 1ms timer resolution, resolving the previous ~150Hz cap.
- **MultimediaTimer Integration** - Migrated from `System.Threading.Timer` to `MultimediaTimer` for hardware-interrupt level precision and stability.
- **Smart Rate Limiting** - New global report synchronization (`_lastAnyReportTime`) that targets a precise total frequency (e.g. exactly 250Hz, 500Hz) instead of being additive.
- **Native Poll Matching** - Automatic threshold logic that skips virtual reports when set to 100Hz or below, strictly matching native Wiimote performance for "Default" settings.
- **Experimental IR Extrapolation** - New prediction logic to compensate for Wiimote latency, providing a more responsive cursor at higher movement speeds. Toggleable and adjustable in Options > General.
- **Dual Action Hotkeys** - Complete redesign of the hotkey system to support two independent actions (Short vs Long press) on the same button combination.
- **Hotkey Sharing Refactor** - New system allowing individual sharing of each Player 1 shortcut. Other players inherit shared keys with the ability to define their own overrides.

### Fixed
- **Alt+F4 Hotkey** - Fixed an issue where modifier keys were not correctly processed, preventing Alt+F4 from working.

## [2.2.2] - 2026-02-13

### Added
- **Bidirectional Player Swap** - New UI controls to hot-swap Wiimotes between player slots (e.g. Move P1 to P2) without disconnection. The system handles driver re-initialization seamlessly.
- **Player Slot Locking** - Ability to lock specific player slots (e.g. P1) to reserve them for external devices like Gun4IR or Sinden. Wiimotes will automatically skip locked slots during connection.
- **IR Tracking Optimizations** - Two optional performance enhancements: EMA Smoothing (configurable strength 1-10) to reduce cursor micro-jitter, and High Performance Timers (DateTime.UtcNow) to reduce internal latency. Both disabled by default, toggleable in Options > General.
- **Homography Cache** - Optional optimization for static calibration modes (WiimoteBar, Gun4IR, FourCorners) that caches the projection matrix to avoid recalculating it every frame, saving significant CPU cycles.
- **Smooth Overlay Tracking** - The calibration overlay now follows the IR pointer in real-time (100Hz) even after releasing the D-pad, providing continuous visual feedback.
- **Extended Offset Range** - Increased the software offset adjustment range from +/- 100 to +/- 200 to accommodate more varied setups and screen sizes.
- **Extended Feedback Phase** - The overlay now maintains IR tracking during the 10-second fade-out period after releasing the modifier button.
- **GamePad Mode Automation** - Complete automation for DuckStation and PCSX2. The system now dynamically updates emulator profiles to map the correct DirectInput indices as Wiimotes connect/disconnect or switch modes.
- **Dynamic Profile Tagging** - Automatic inhibition of guns in emulator profiles (using `-wiimotegun` tags) when Wiimotes are in Mouse mode, ensuring they don't interfere with standard gamepad inputs.
- **Real-time Sync** - Profile updates are now triggered instantly upon Wiimote mode switches (GamePad <-> Mouse).

### Fixed
- **Hotkey Logic Improvements** - Fixed modifier keys (Home/Minus) blocking native inputs. They are now only suppressed when part of an active combo, allowing normal usage otherwise.
- **Emulator Process Access (Access Denied)** - Fixed a critical issue where WiimoteGun would disconnect if a monitored emulator (RetroArch, MAME, etc.) was launched with administrator privileges. The process status check is now safely handled.
- **Wiimote Rumble Regression** - Fixed an issue where the rumble would occasionally become continuous. The stop timer is now persistent to prevent premature garbage collection.
- **Real-time Log Rotation** - Log files are now rotated mid-run when they exceed 1.5MB, preventing disk space issues during long sessions with high debug output.
- **RetroBat Discovery** - Robust detection of the `emulators` folder using the RetroBat registry path, with support for symbolic links and junctions.

## [2.2.1] - 2026-02-05

### Added
- **4:3 Aspect Ratio Correction** - New "Mouse 4:3" and "GamePad 4:3" modes to support games running in 4:3 centered on widescreen monitors.
- **GamePad Default Optimization** - Updated default GamePad IR settings to `Linearity: 1.3` and `Overscan: 0.05` for improved out-of-the-box accuracy.

## [2.2.0] - 2026-02-03

### Added
- **Native Virtual GamePad Support (DirectInput)** - Fully integrated 4-player virtual gamepads for emulator compatibility (e.g. PCSX2 Dual Lightgun).
- **DInput Index Stabilization** - Added "Stabilize GamePad Indices" option to keep virtual gamepads enabled even when Wiimotes are disconnected, preventing index shifting in emulators.
- **Interception Driver Removal** - Removed dependency on the Interception driver; all keyboard and gamepad reports are now handled natively via VMulti.
- **Analog IR Stick Mapping** - Ability to map IR tracking directly to Left or Right analog sticks for better controller-ready game support.
- **GamePad Mapping UI** - New modern UI tab for configuring per-player analog mappings and digital buttons.

## [2.1.0] - 2025-12-09

### Added
- **Native 4-Player VMulti Support** - Fully implemented virtual HID driver for 4 independent players
- **Gun4IR Diamond Validation** - LED layout validated and production-ready
- **Retroshooter Validation** - 4-Corners LED layout validated and production-ready
- **Auto-Lock Improvements** - Removed deprecated VID restrictions for VMulti devices
- **New Overlay Menu** - Complete redesign with sidebar navigation and categorized settings
- **Enhanced Options** - Added dedicated "Players" page with Mac Address locking and device management

### Changed
- **Default Rumble Intensity** - Lowered from 75% to 50% for better out-of-box experience
- **Permissive Calibration** - Feature is now disabled and hidden by default (requires manual activation)
- **Driver Installation** - Streamlined install/cleanup scripts into single elevated session
- **UI Improvements** - Fixed overlay layout shift on option selection

## [2.0.3] - 2025-11-21

### Added
- **Per-player screen calibration** - Each player can now calibrate independently from their position
- Calibration properties for each player (P1-P4) with separate Top, Left, CenterX, CenterY values
- Helper methods `GetCalibrationForPlayer()` and `SetCalibrationForPlayer()` in `Options.cs`
- Automatic migration from legacy global calibration to per-player calibration

### Changed
- `ScreenPositionCalculator` now accepts `playerIndex` parameter for player-specific calibration
- Calibration points are now per-instance instead of static/global

### Fixed
- **Critical bug**: Removed `static` keyword from calibration points to prevent shared calibration between players
- Players at different positions (left/right, near/far) now have accurate independent calibration

## [2.0.2] - 2025-11-21

### Added
- **DolphinBar multi-Wiimote support** - Multiple Wiimotes can now be connected simultaneously via Mayflash DolphinBar Mode 4
- HID path-based unique identification for DolphinBar devices
- Automatic detection and routing between Bluetooth (MAC-based) and DolphinBar (HID path-based) modes

### Changed
- Wiimote duplicate detection now uses `UniqueId` property instead of MAC address only
- Enhanced `WiimoteDeviceInfo` with automatic identification fallback for devices without valid MAC addresses

### Fixed
- "Wiimote already connected" error when connecting multiple Wiimotes via DolphinBar
- Device identification for DolphinBar devices that report MAC address as `00:00:00:00:00:00`

## [2.0.1] - 2025-11-21

### Added
- Advanced keyboard routing options for TeknoParrot/RetroBat compatibility
- `KeyboardDebugMode` configuration option for detailed input diagnostics
- `ForceKeyboardDeviceIdP1/P2/P3/P4` settings to manually specify keyboard Device IDs
- Comprehensive TeknoParrot troubleshooting documentation
- Developer testing guides for keyboard routing diagnostics
- Documentation organization in `docs/` folder

### Changed
- Reorganized technical documentation into `docs/` directory
- Updated main README with advanced configuration options
- Enhanced logging in `VirtualInterceptionKeyboard` (optional, disabled by default)

### Fixed
- Clarified Interception driver behavior (reuses physical keyboards vs creating virtual ones)
- Documented keyboard Device ID routing for games using RawInput API

## [2.0.0] - 2024-2025

### Added
- Multi-Wiimote support (up to 4 players)
- Per-player button mappings
- Interception driver integration replacing vMulti for keyboards
- MAC-based Wiimote assignment preferences
- 4-player experimental mode
- Shared keyboard option
- Nunchuk coldplug detection
- First-launch welcome dialog
- IR visualizer tool
- Nunchuk-only mode (virtual analog stick)

### Changed
- Migrated from vMulti to Interception driver for keyboard input
- Improved connection stability
- Enhanced Bluetooth pairing workflow

### Fixed
- Persistent rumble issue
- Nunchuk detection on coldplug
- Multiple player assignment conflicts

## [1.0.0] - Original

- Initial WiimoteGun implementation by f.caruso
- Basic Wiimote to mouse/keyboard mapping
- Single player support
- vMulti driver integration
