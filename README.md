# Wiimote4Guns 🎮

Multi-player Wiimote to Virtual Mouse/Keyboard/GamePad adapter.

Fork of [WiimoteGun](https://github.com/fcaruso/WiimoteGun) with extensive enhancements for multi-player support and LED layouts.

## ✨ Features

- **Multi-Wiimote Support**: Connect up to 4 Wiimotes simultaneously
- **Dual Connection Mode**: Supports both Bluetooth and DolphinBar Mode 4
- **Multiplayer**: Compatible with up to 4 players
- **Per-Player Calibration**: Independent screen calibration for each player
- **LED Layout Support**: Wiimote Bar (default), Gun4IR Diamond ✅, Retroshooter 4-Corners ✅
- **Virtual HID**: Uses Virtual HID Driver (vmulti) for unique RawInput device IDs per player
- **Nunchuk Support**: Full Nunchuk detection (hotplug and coldplug)
- **IR Visualizer**: Real-time IR camera visualization tool
- **GamePad Mode**: Emulate a DirectInput gamepad per player (e.g. for PCSX2 dual lightgun support)
- **4:3 Aspect Ratio Support**: Dedicated modes for 4:3 games centered on widescreen monitors (Mouse 4:3 / GamePad 4:3)


> [!NOTE]
> **Gun4IR Diamond and Retroshooter 4-Corners are now validated and fully functional!**  
> Both LED layouts have been tested and confirmed working. All three layouts (Wiimote Bar, Gun4IR Diamond, Retroshooter 4-Corners) are production-ready.

## 🎯 Requirements

- Windows 10/11 (64-bit)
- **Bluetooth adapter** (for Bluetooth mode) OR **Mayflash DolphinBar** (Mode 4)
- **Virtual HID Driver** (included, installation required)

### Wiimote Compatibility

> [!WARNING]
> **Newer Wiimotes (post-2011) are NOT compatible with Microsoft Bluetooth Stack**  
> Wiimotes with serial numbers ending in **Z-C4**, **Z-C6**, or **C-C4** will NOT work via standard Bluetooth.

**Recommended Wiimotes:**
- ✅ Official Nintendo Wiimotes manufactured **before November 2011**
- ✅ Any Wiimote via **Mayflash DolphinBar Mode 4** (bypasses Bluetooth issues)
- ⚠️ Clone/third-party Wiimotes: **NOT TESTED** (may work via DolphinBar only)

**If you have a newer Wiimote (2011+):**
- **Easiest solution**: Use **Mayflash DolphinBar** in Mode 4 (recommended)
- **Advanced solution**: Install Toshiba Bluetooth Stack (driver signature issues, not recommended)
  - Guide: [TouchMote Wiimote TR Setup](https://touchmote.net/wiimotetr)

> [!NOTE]
> This limitation is due to changes in Wiimote firmware after 2011 that are incompatible with Microsoft's Bluetooth stack. The DolphinBar works around this by using its own HID protocol.  
> Compatibility information source: [TouchMote](https://touchmote.net)

### For 4-Player Mode
- **Virtual HID Driver** installed (provides 4 virtual mice/keyboards)




## 📦 Installation

1. **Download** the latest release
2. **Extract** to your desired location
3. **Launch** `WiimoteGun.exe`
4. **Follow the Setup Wizard**: On first launch, a wizard will guide you through driver and service installation
5. **Restart your PC** when prompted

> **Note**: To uninstall drivers later, use the systray menu: Options → Virtual HID Driver → Remove drivers

## 🎮 Quick Start

### Connecting Wiimotes
- Press **1 + 2** on each Wiimote to connect.
- They will be automatically assigned to the next available Player slot (P1, P2, P3, P4).

### Calibration
1. **Long press HOME button** on Wiimote
2. **Aim at calibration points** displayed on screen
3. **Press A or B** to confirm each point
4. **Press ESC** to exit calibration

### Controls
- **HOME**: Toggle modes (Mouse/Keyboard/GamePad/Disabled)
- **HOME (long press)**: Calibrate

- **Right-click tray icon**: Settings & mappings

## 🔧 Configuration

### LED Layout Selection
Choose your LED bar type in Options:
- **Wiimote Bar** (Recommended): Standard 2-LED sensor bar
- **Gun4IR Diamond**: 4 LEDs in diamond pattern
- **Retroshooter (4-Corners)**: 4 LEDs at screen corners

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

### 🎮 GamePad Mode (DirectInput)
Mode for emulators like **PCSX2** or games requiring separate controllers for each player.
- **DInput Support**: Each Wiimote is seen as a unique Virtual GamePad.
- **Analog Mapping**: Map IR tracking to Left or Right stick.
- **Nunchuk Integration**: Use Nunchuk joystick for movement or as a Digital D-Pad.
- **Automatic Profile Updates**: Automatically updates DirectInput indices in **PCSX2** and **DuckStation** input profiles.
  - Profiles must have the **`-wiimotegun`** tag (e.g., `game-wiimotegun.ini`).
  - A default **`gamepad-wiimotegun.ini`** is automatically generated if missing.
  - Ensures accurate mapping even when Windows changes "Joy" numbers.
- **Configuration**: Right-click tray → **Open Mappings** → **GamePad Mapping** tab.

### 🖼️ 4:3 Aspect Ratio Mode
Designed for retrogaming in 4:3 (centered) on widescreen (16:9, 21:9) monitors.
- **Automatic Scaling**: Stretches the IR tracking area to match the 4:3 game box only.
- **Accurate Edges**: The Wiimote "off-screen" detection and edge tracking will perfectly match the 4:3 game borders.
- **Modes**: Cycle through Mouse → **Mouse 4:3** → GamePad → **GamePad 4:3** using the HOME button.

*FR: Le mode 4:3 adapte le tracking IR pour les jeux centrés sur écrans larges. La zone de visée est automatiquement limitée aux bordures de la "box" 4:3 du jeu.*

*FR: Mode GamePad pour PCSX2 (Dual Lightgun) : Chaque Wiimote est émulée comme une manette DirectInput indépendante avec axes analogiques pour la visée.*




### Device Assignment (Advanced)
**Per-Player Mouse/Keyboard**: Assign specific input devices to each player

- Auto-Lock VMulti option for automatic P1/P2/P3/P4 assignment
- Configured in **Assign Wiimote** → **⚙️ Devices** button

## 🐛 Troubleshooting

### Wiimotes won't connect
- Ensure Bluetooth is on
- Unpair Wiimotes from Windows Bluetooth settings first
- Ensure Virtual HID drivers are installed via Options menu

### Mouse doesn't work in games
- Verify Virtual HID driver installation in Options
- Restart PC after driver installation
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
- `-installPlayer1`: Installs Virtual HID driver for Player 1
- `-uninstallPlayer1`: Removes Virtual HID driver for Player 1

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
- **vmulti**: [djpnewton](https://github.com/djpnewton/vmulti/)
- **EcoTUIODriver**: [ecologylab](https://github.com/ecologylab/EcoTUIODriver)

## 🔗 Links

- [Original WiimoteGun](https://github.com/fcaruso/WiimoteGun)
- [RetroBat Project](https://www.retrobat.org/)
