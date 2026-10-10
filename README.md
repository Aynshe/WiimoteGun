# Wiimote4Guns 🎮

Multi-player Wiimote to Virtual Mouse/Keyboard/GamePad adapter.

Fork of [WiimoteGun](https://github.com/fcaruso/WiimoteGun) with extensive enhancements for multi-player support and LED layouts.

## ✨ Features

- **Multi-Wiimote Support**: Connect up to 4 Wiimotes simultaneously
- **Dual Connection Mode**: Supports both Bluetooth and DolphinBar Mode 4
- **Multiplayer**: Compatible with up to 4 players
- **Per-Player Calibration**: Independent screen calibration for each player
- **LED Layout Support**: Wiimote Bar (default), Gun4IR Diamond ✅, Retroshooter 4-Corners ✅
- **Virtual HID**: New **RawInput (UMDF2)** driver (default, based on [HIDMaestro](https://github.com/hifihedgehog/HIDMaestro), silently installed by the service) or legacy vmulti (optional) — unique RawInput device IDs per player
- **Nunchuk Support**: Full Nunchuk detection (hotplug and coldplug)
- **IR Visualizer**: Real-time IR camera visualization tool
- **GamePad Mode**: Emulate a DirectInput gamepad per player (e.g. for PCSX2 dual lightgun support)
- **4:3 Aspect Ratio Support**: Dedicated modes for 4:3 games centered on widescreen monitors (Mouse 4:3 / GamePad 4:3)

## 🆕 New Input Driver — RawInput (UMDF2) / HIDMaestro

**Wiimote4Guns now ships a NEW input driver** that progressively replaces the legacy vmulti kernel driver:

- **Why the change**: Microsoft will deprecate drivers signed with legacy certificates starting with **Windows 11 26H02** — the vmulti driver may be blocked there. The new driver should NOT be affected and works the same way.
- **What**: [HIDMaestro](https://github.com/hifihedgehog/HIDMaestro)-based **UMDF2** virtual HID devices (mouse + keyboard + GamePad per player, unique RawInput device IDs per player), installed **silently by the Wiimote4Guns Service** — no test mode, no UAC, no reboot. Source code of the bundled fork: [hifihedgehog/HIDMaestro](https://github.com/hifihedgehog/HIDMaestro).
- **Status**: the new driver is **IN TESTING PHASE**. vmulti remains available as an optional install (Setup Wizard: explicit opt-in checkbox + confirmation) and will eventually be removed, or kept only for Windows 10 and Windows 11 versions below 26H02.
- **GamePads**: in RawInput (UMDF2) mode each player also gets a virtual GamePad — DirectInput (« GamePad Wiimote4Guns Pn ») or **XInput** (Xbox 360 compatible, no ViGEmBus needed), selectable per player in the GamePad mappings or from the tile modal.
- **Prerequisite**: the UMDF2 input host (HmHost) requires the **.NET 10 Runtime (x64)** — the app checks the machine at RawInput (UMDF2) activation and proposes the official download when missing.
- **New users**: « RawInput (UMDF2) » is selected by default when the vmulti driver is not installed.

## 🎯 Requirements

- Windows 10/11 (64-bit)
- **Bluetooth adapter** (for Bluetooth mode) OR **Mayflash DolphinBar** (Mode 4)
- **Wiimote4Guns Service** (required — installed by the Setup Wizard)
- **Input driver**: « RawInput (UMDF2) » recommended & default (installed silently by the service; requires the **.NET 10 Runtime x64**) — or legacy **vmulti** (optional, Windows 10 / Windows 11 < 26H02)

### Wiimote Compatibility

> [!WARNING]
> **Wiimote v2 (post-2011, "RVL-CNT-01-TR") pairing DOES work with the Microsoft Bluetooth stack, but ONLY via the RED SYNC button** (under the battery cover). The "1 + 2" pairing method does NOT work for Wiimote v2.
> **The Nunchuk is NOT hot-pluggable**: to use it, plug the Nunchuk **BEFORE pairing the Wiimote** (hotplug detection is not reliable after pairing).

**Recommended Wiimotes:**
- ✅ Official Nintendo Wiimotes manufactured **before November 2011** (pair via **1 + 2**)
- ✅ Wiimote v2 (2011+) via the **red SYNC button** (see warning above)
- ✅ Any Wiimote via **Mayflash DolphinBar Mode 4** (bypasses Bluetooth issues)
- ⚠️ Clone/third-party Wiimotes: **NOT TESTED** (may work via DolphinBar only)

**If your Wiimote v2 won't pair via Bluetooth:**
- **Easiest solution**: use the **red SYNC button** (not 1 + 2), and plug the Nunchuk before pairing
- **Alternative**: Use **Mayflash DolphinBar** in Mode 4 (recommended)
- **Advanced solution**: Install Toshiba Bluetooth Stack (driver signature issues, not recommended)
  - Guide: [TouchMote Wiimote TR Setup](https://touchmote.net/wiimotetr)

> [!NOTE]
> Wiimote v2 firmware changed its Bluetooth pairing behavior after 2011: it requires the sync-button method instead of the "1 + 2" hold. The DolphinBar works around this by using its own HID protocol.  
> Compatibility information source: [TouchMote](https://touchmote.net)

### For 4-Player Mode
- The input driver provides **4 virtual mice/keyboards** (one set per player) — RawInput (UMDF2) by default, or vmulti




## 📦 Installation

1. **Download** the latest release
2. **Extract** to your desired location
3. **Launch** `WiimoteGun.exe`
4. **Follow the Setup Wizard**: only the **Wiimote4Guns Service** is required — the new « RawInput (UMDF2) » driver installs silently through it (no PC restart needed); the legacy vmulti driver is optional (opt-in checkbox)
5. If prompted for the **.NET 10 Runtime (x64)** (UMDF2 mode), install it from the official Microsoft page

> **Note**: To uninstall the legacy vmulti driver later, use the Setup Wizard (« Uninstall All »).

## 🎮 Quick Start

### Connecting Wiimotes
- Wiimote v1 (pre-2011): Press **1 + 2** on each Wiimote to connect.
- Wiimote v2 (2011+): Use the **red SYNC button** (under the battery cover).
- They will be automatically assigned to the next available Player slot (P1, P2, P3, P4).

### Calibration
1. **Long press HOME button** on Wiimote
2. **Aim at calibration points** displayed on screen
3. **Press A or B** to confirm each point
4. **Press ESC** to exit calibration

### Controls
- **HOME**: Cycle modes — Mouse → Mouse 4:3 → GamePad → Disabled (the Mouse FPS / GamePad 4:3 / GamePad FPS variants appear when enabled in Options)
- **HOME (long press)**: Calibrate
- **HOME + PLUS**: Open the **Wiimote4Guns interface** — the same interface as the Windows tray icon, as a fullscreen overlay (toggles show/hide)
- **PLUS (long press, default 4 s)**: Open the **tile modal** — load a GamePad profile on the fly, swap the GamePad API **DInput ↔ XInput** (GamePad mode), and more, without leaving the game (delay configurable via `EsTileHotkeyDelayMs` in settings.cfg)
- **HOME + D-Pad** (Bluetooth) / **MINUS + D-Pad** (Mayflash DolphinBar): adjust the dynamic perspective offset **in-game** (auto-saved on release)
- **Off-screen + MINUS + PLUS (3 s)**: Manual Disable — disables the player's virtual device
- **Right-click tray icon**: Settings & mappings

## 🔧 Configuration

### LED Layout Selection
Choose your LED bar type in Options:
- **Wiimote Bar**: Standard 2-LED sensor bar
- **Gun4IR Diamond**: 4 LEDs in diamond pattern
- **Retroshooter (4-Corners)**: 4 LEDs at screen corners

> [!TIP]
> **The 4-LED layouts are the best option and work WITHOUT calibration** (Gun4IR Diamond, Retroshooter 4-Corners). The standard 2-LED Wiimote Bar works fine but requires the usual calibration.
> FR : **Les layouts 4 LED sont le meilleur choix et fonctionnent SANS calibration**. La barre 2 LED standard fonctionne aussi, avec la calibration habituelle.

### Button Mappings
- Right-click tray icon → **Open Mappings**
- Use player dropdown for per-player configurations
- Each player can have unique button assignments

### Monitor Selection
- Choose which screen to track in **Options**
- Calibration is saved per-player and per-monitor

### Gestures & Reload
**Off-Screen Reload**:
- **On Click**: Triggers reload when you click while off-screen
- **Automatic**: Triggers reload immediately when aiming off-screen

**Motion Gestures (Shake / Grenade)**:
> [!WARNING]
> **In Development / Untested**: Motion features require verification (specifically with Wiimote Plus).

### Auto-Load Profile per Executable
Automatically load specific profiles when launching games:
- Enable **"⚙️ Auto-load for this executable"** checkbox in Button Mapping overlay
- Links current profile to the detected game executable
- Profile loads automatically when the game starts
- Managed via **Button Mapping** overlay

### Hotkeys (Global Keyboard Shortcuts)
Configure system-wide hotkeys for quick actions:
- **Calibrate**: Trigger calibration for current player
- **Toggle Overlay**: Open/close the configuration overlay
- **Reload Profile**: Reload current remap profile
- Access via **Button Mapping** → **⚡ Hotkeys** button

### Rumble/Vibration (Per-Player)
Configure haptic feedback for each player:
- **Enable/Disable**: Toggle rumble on weapon fire
- **Intensity**: Adjust vibration strength (0-100%)
- **Duration**: Set rumble duration in milliseconds (50-1000ms)
- Configured in **Assign Wiimote** page

### 🎮 GamePad Mode
Mode for emulators like **PCSX2** or games requiring separate controllers for each player.
- **DInput or XInput**: Each Wiimote is seen as a unique Virtual GamePad — **DirectInput** (default) or **XInput** (Xbox 360 compatible; ViGEmBus in vmulti mode, built-in XUSB in UMDF2 mode), per player.
- **Analog Mapping**: Map IR tracking to Left or Right stick.
- **Nunchuk Integration**: Use Nunchuk joystick for movement or as a Digital D-Pad.
- **Automatic Profile Updates**: Automatically updates the DirectInput/XInput indices in **PCSX2** and **DuckStation** input profiles, and patches the Wiimote device names in **Dolphin** profiles (`Device = DInput/0/...`) according to the selected driver mode.
  - Profiles must have the **`-wiimotegun`** tag (e.g., `game-wiimotegun.ini`).
  - A default **`gamepad-wiimotegun.ini`** is automatically generated if missing.
  - Ensures accurate mapping even when Windows changes "Joy" numbers.
- **Configuration**: Right-click tray → **Open Mappings** → **GamePad Mapping** tab — or **long-press PLUS (default 4 s)** to open the tile modal (load a profile on the fly, swap DInput ↔ XInput).

### 🖼️ 4:3 Aspect Ratio Mode
Designed for retrogaming in 4:3 (centered) on widescreen (16:9, 21:9) monitors.
- **Automatic Scaling**: Stretches the IR tracking area to match the 4:3 game box only.
- **Accurate Edges**: The Wiimote "off-screen" detection and edge tracking will perfectly match the 4:3 game borders.
- **Modes**: Cycle through Mouse → **Mouse 4:3** → GamePad → **GamePad 4:3** using the HOME button.

*FR: Le mode 4:3 adapte le tracking IR pour les jeux centrés sur écrans larges. La zone de visée est automatiquement limitée aux bordures de la "box" 4:3 du jeu.*

*FR: Mode GamePad pour PCSX2 (Dual Lightgun) : Chaque Wiimote est émulée comme une manette DirectInput indépendante avec axes analogiques pour la visée.*




### UI Zoom (Global Window/Font Scale)
Adjust the size of **every window, page and text** of the application from the Wiimote4Guns home page:

- Use the **− / +** buttons (centered above the home title) — zoom from **80% to 150%** in 10% steps
- The interface reopens immediately at the new scale; every other window (Setup Wizard, tile modal, dialogs…) picks the zoom up at its next open
- Scaling happens at window **creation** time (never live on an open window) and every window is clamped to the screen — nothing can end up off-screen
- Persisted in `settings.cfg` (`UiScalePercent`, default 100%)

*FR: Le zoom UI − / + de la page d'accueil agrandit ou réduit TOUTES les fenêtres, pages et textes (80–150 %), sans jamais déborder de l'écran.*

### Player Slot Locking (Advanced)
**Lock a Wiimote to a player slot** — the key feature for mixing real lightguns and Wiimotes:

- **Lock a slot** so a Wiimote always takes the SAME player number (P1..P4) when connecting
- Lets a **real lightgun (e.g. Gun4IR) own the Player 1 slot** — or two real lightguns on P1 + P2 — while other players use Wiimote4Guns Wiimotes on the remaining slots
- Configured in **Assign Wiimote** → **🔓 Lock** button per player

*FR: Verrouille une Wiimote sur un slot joueur — indispensable pour mélanger vrais lightguns et Wiimotes : un Gun4IR réel peut occuper le slot Joueur 1 (ou deux lightguns réels en P1 + P2) pendant que d'autres joueurs utilisent des Wiimotes Wiimote4Guns sur les slots restants.*

## 🐛 Troubleshooting

### Wiimotes won't connect
- Ensure Bluetooth is on
- Unpair Wiimotes from Windows Bluetooth settings first
- Ensure the Wiimote4Guns Service is installed and running (Setup Wizard) and an input driver is active (« RawInput (UMDF2) » by default; legacy vmulti optional)

### Mouse doesn't work in games
- Verify the Service + input driver installation (Setup Wizard / Options)
- In UMDF2 mode, check that the .NET 10 Runtime (x64) is installed (the app checks it at mode activation; the service log shows an explicit diagnostic when the input host cannot start)
- Check `WiimoteGun.log` for errors

### Tracking accuracy issues (Gun4IR / Retroshooter)
- Re-calibrate carefully, aiming precisely at each point
- Ensure all 4 LEDs are visible at all times
- If tracking is unstable, switch to **Wiimote Bar** mode
- Report issues on GitHub with screenshots of IR Visualizer

## 🎮 Emulator Compatibility & Conflict Resolution

WiimoteGun includes a built-in monitor to prevent conflicts with emulators that take exclusive control of Wiimotes (Dolphin, Cemu).

### Automatic Restart System
When **Dolphin.exe** or **Cemu.exe** is detected:
1. WiimoteGun automatically **restarts** to release Wiimote control.
2. While the emulator is running, WiimoteGun stays in "passive" mode (Wiimotes disconnected).
3. When the emulator closes, WiimoteGun **restarts again** to reclaim and reconnect the Wiimotes.

### Configuration
You can control this behavior in `settings.cfg` (generated after first run):
- `RestartOnDolphin`: `true` (default) - Auto-restart when Dolphin starts/stops
- `RestartOnCemu`: `true` (default) - Auto-restart when Cemu starts/stops

## 💻 Command Line & PATH

WiimoteGun automatically adds itself to your user **PATH** environment variable.
You can run it from any command prompt using:

```cmd
wiimotegun.exe [arguments]
```

### Arguments
- `-refresh`: Reloads configuration and restarts the running instance
- `-remap "subfolder/profile.remap"`: Loads a specific remap profile (**hot-reload without restart**)
- `-esgamestart [es-args]`: EmulationStation **game-start hook** — passes the EmulationStation command line so Wiimote4Guns detects the launched game and auto-loads the matching profile (configured as `WiimoteGun.exe -esgamestart %*` in ES; also usable as a simple argument)
- `-esgameend`: EmulationStation **game-end hook** — clears the detected game state (configured as `WiimoteGun.exe -esgameend %*` in ES; also usable as a simple argument)

## 🎮 Remap Profiles

WiimoteGun supports **remap profiles** to quickly switch between different button mappings for different games.

### Profile Storage
Profiles are stored in:
- `[RetroBatPath]/user/WiimoteGunRemap/` (if RetroBat is installed)
- `./RemapProfiles/` (fallback if RetroBat not found)

The RetroBat path is automatically detected from the registry: `HKEY_CURRENT_USER\Software\RetroBat` → `LatestKnownInstallPath`

### Default Profile
If `default.remap` exists at the root of the remap directory, it will be **automatically loaded** at startup instead of using `settings.cfg` mappings.

### Hot-Reload via Command Line (No Restart)
```cmd
wiimotegun.exe -remap "mygames/doom.remap"
```

This command:
1. **If WiimoteGun is running**: Sends IPC message → **instantly reloads** the profile without restarting
2. **If WiimoteGun is closed**: Starts WiimoteGun with the profile loaded
3. Shows a tray notification confirming the profile was loaded

**Priority**: `-remap` argument > `default.remap` > `settings.cfg`

### UI Profile Management
Open **Button Mapping** → **Profiles** tab:

- **Save Profile**: Enter a name and save current mappings (all 4 players)
- **Load Profile**: Select from dropdown and load instantly
- **Delete Profile**: Remove unwanted profiles
- **New Folder**: Organize profiles by game/genre in subfolders
- **Refresh**: Update the list of available profiles

### Notifications
WiimoteGun displays tray notifications when profiles are loaded:
- At startup: `"Remap profile loaded: [name] (command line)"` or `"Remap profile loaded: [name] (default.remap)"`
- Hot-reload: `"Remap profile loaded: [name]"`

## 🚧 Developer Features (Non-Functional / To Be Developed)

> [!CAUTION]
> **THESE FEATURES ARE NOT FUNCTIONAL.**
> They are unfinished prototypes or placeholders that **require development work**.
> They are disabled by default and should only be enabled by developers intending to implement them.

### Enabling Dev Gestures (For Development Only)
1. Close WiimoteGun
2. Open `settings.cfg` (located in the application folder) in a text editor
3. Find `<EnableDevGestures>false</EnableDevGestures>`
4. Change to `<EnableDevGestures>true</EnableDevGestures>`
5. Save and restart WiimoteGun

### Available Dev Gestures & Features
- **Shake Reload**: Reload by shaking Wiimote or Nunchuk
  - ⚠️ **Status**: Not working. Logic needs to be implemented.
- **Grenade Gesture**: Throw grenade with "pump" motion
  - ⚠️ **Status**: Not working. Motion detection algorithms need to be written.
- **Gyro Aiming (FPS Mode)**: Use Wiimote gyroscope for camera control
  - ⚠️ **Status**: Non-functional. Gyro data mapping to mouse input is incomplete.
- **3D Gyro Visualizer**: Real-time 3D visualization of Wiimote/Nunchuk orientation
  - Displays both Wiimote and Nunchuk orientation when connected
  - Accessible via IR Visualizer page

> [!WARNING]
> Do not enable these features expecting them to improve your gameplay. They are placeholders for future development.



## 📄 License

Same as original WiimoteGun project.

## 🙏 Credits

- **Original Author**: [f.caruso](https://github.com/fcaruso/WiimoteGun)
- **v2.x Fork**: Aynshe - RetroBat Team (2025)
- **WiimoteLib.Net**: [Robert Jordan](https://github.com/trigger-segfault/WiimoteLib.Net)
- **HIDMaestro**: [hifihedgehog](https://github.com/hifihedgehog/HIDMaestro) — new UMDF2 virtual input driver (bundled fork)
- **vmulti**: [djpnewton](https://github.com/djpnewton/vmulti/) — legacy virtual HID driver (optional)
- **EcoTUIODriver**: [ecologylab](https://github.com/ecologylab/EcoTUIODriver)

## 🔗 Links

- [Original WiimoteGun](https://github.com/fcaruso/WiimoteGun)
- [RetroBat Project](https://www.retrobat.org/)
