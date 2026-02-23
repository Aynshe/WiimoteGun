# Changelog

All notable changes to Wiimote4Guns will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/).

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
