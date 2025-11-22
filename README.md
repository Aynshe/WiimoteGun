# Wiimote2Guns 🎮

Multi-player Wiimote to Virtual Mouse/Keyboard adapter with Interception driver support.

Fork of [WiimoteGun](https://github.com/fcaruso/WiimoteGun) with extensive enhancements for multi-player support and LED layouts.

## ✨ Features

- **Multi-Wiimote Support**: Connect up to 4 Wiimotes simultaneously
- **Dual Connection Mode**: Supports both Bluetooth and DolphinBar Mode 4
- **Dual Mode**: 2-player stable mode / 4-player experimental mode
- **Per-Player Calibration**: Independent screen calibration for each player
- **LED Layout Support**: Wiimote Bar (default), Gun4IR Diamond, Retroshooter (4-Corners)
- **Virtual HID**: Uses Interception driver for unique RawInput device IDs per player
- **Nunchuk Support**: Full Nunchuk detection (hotplug and coldplug)
- **IR Visualizer**: Real-time IR camera visualization tool
- **TeknoParrot Compatible**: Advanced keyboard routing options for lightgun games

> [!WARNING]
> **Gun4IR Diamond / Retroshooter tracking is currently experimental.**  
> The tracking algorithm for non-standard LED layouts (Gun4IR Diamond, Retroshooter 4-Corners) is actively being improved. You may experience inaccuracies or "wave" distortion during horizontal movements. Wiimote Bar mode is fully stable and recommended for production use.

## 🎯 Requirements

- Windows 10/11 (64-bit)
- **Bluetooth adapter** (for Bluetooth mode) OR **Mayflash DolphinBar** (Mode 4)
- **Interception Driver** (included, installation required)

### For 4-Player Mode (Experimental)
- 4 physical USB keyboards
- 4 physical USB mice

> [!NOTE]
> Some keyboards are detected by Interception as both a keyboard and a mouse (composite device). In this case, you may need fewer physical devices (e.g., one keyboard and one mouse might be enough for 2 players).

## 📦 Installation

1. **Download** the latest release
2. **Extract** to your desired location
3. **Install Interception Driver**:
   - Right-click WiimoteGun tray icon → Options → Install Drivers
   - OR manually run `WiimoteGunDriver\\command line installer\\install-interception.exe` as Administrator
4. **Restart your PC**
5. Launch `WiimoteGun.exe`

## 🎮 Quick Start

### Connecting Wiimotes
- **2 Players**: Press 1+2 on each Wiimote → Auto-assigned as P1/P2
- **3-4 Players**: Enable "4 Players Mode" in Options first

### Calibration
1. **Long press HOME button** on Wiimote
2. **Aim at calibration points** displayed on screen
3. **Press A or B** to confirm each point
4. **Press ESC** to exit calibration

### Controls
- **HOME**: Toggle modes (Mouse/Keyboard/Disabled)
- **HOME (long press)**: Calibrate
- **Right-click tray icon**: Settings & mappings

## 🔧 Configuration

### LED Layout Selection
Choose your LED bar type in Options:
- **Wiimote Bar** (Recommended): Standard 2-LED sensor bar
- **Gun4IR Diamond**: 4 LEDs in diamond pattern (⚠️ Experimental)
- **Retroshooter (4-Corners)**: 4 LEDs at screen corners (⚠️ Untested)

### Button Mappings
- Right-click tray icon → **Open Mappings**
- Use player dropdown for per-player configurations
- Each player can have unique button assignments

### Monitor Selection
- Choose which screen to track in **Options**
- Calibration is saved per-player and per-monitor

## 🐛 Troubleshooting

### Wiimotes won't connect
- Ensure Bluetooth is on
- Unpair Wiimotes from Windows Bluetooth settings first
- Check if Interception driver is installed (`C:\\Windows\\System32\\drivers\\keyboard.sys` exists)

### Mouse doesn't work in games
- Verify Interception driver installation
- Restart PC after driver installation
- Check `WiimoteGun_Log.txt` for errors

### Tracking accuracy issues (Gun4IR / Retroshooter)
- Re-calibrate carefully, aiming precisely at each point
- Ensure all 4 LEDs are visible at all times
- If tracking is unstable, switch to **Wiimote Bar** mode
- Report issues on GitHub with screenshots of IR Visualizer

### Keyboard inputs not working in TeknoParrot
- Enable `KeyboardDebugMode` in `settings.cfg` to diagnose
- See detailed guide: [TeknoParrot Troubleshooting](docs/TEKNOPARROT_TROUBLESHOOTING.md)

## 📚 Documentation

- [TeknoParrot Troubleshooting Guide](docs/TEKNOPARROT_TROUBLESHOOTING.md)
- [Interception Installation Guide](docs/INTERCEPTION_INSTALL.md)
- [Technical Documentation](docs/README.md)

## 📝 Recent Changes

- 🚧 **Gun4IR Diamond support** (Experimental, tracking improvements ongoing)
- 🚧 **Retroshooter 4-Corners support** (Untested)
- ✅ Per-player LED layout selection
- ✅ 5-point calibration for Gun4IR/Retroshooter
- ✅ Per-player screen calibration
- ✅ DolphinBar multi-Wiimote support
- ✅ TeknoParrot keyboard routing

## 📄 License

Same as original WiimoteGun project.

## 🙏 Credits

- **Original Author**: [f.caruso](https://github.com/fcaruso/WiimoteGun)
- **v2.x Fork**: Aynshe - RetroBat Team (2024-2025)
- **WiimoteLib.Net**: [Robert Jordan](https://github.com/trigger-segfault/WiimoteLib.Net)
- **Interception**: [oblita](http://www.oblita.com/interception.html)

## 🔗 Links

- [Original WiimoteGun](https://github.com/fcaruso/WiimoteGun)
- [RetroBat Project](https://www.retrobat.org/)
