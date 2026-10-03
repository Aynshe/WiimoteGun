# Changelog

All notable changes to Wiimote4Guns will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/).

## [3.0.0.21] - 2026-10-03

### Added
- **Reload Rumble** — A dedicated, fully configurable rumble engine for every reload: off-screen Auto sequence, off-screen trigger redirect, physical reload button, shake reload and GamePad off-screen reloads — whether Off-Screen Reload is enabled or not. Options > Gestures: enable (ON by default), intensity (0-100%) and styles: **Ratchet** (mechanical), **Short**, **Long** and **Custom** (build your own pattern: 1-10 tics with adjustable Tic ON/OFF durations). Per-profile overrides (enable, style, intensity) on the Mouse AND GamePad mapping pages.
- **GamePad rumble sources** — New "Fire Button" (default B) triggers the weapon rumble on-screen with full parity with the mouse trigger rumble (Assign intensity/duration + continuous repetition while held); new "Reload Button" (default 2) triggers the reload rumble on- and off-screen. TC Cover and TC Bi-Pedal rumble when entering cover (= the TC reload moment).
- **Automatic app updates (GitHub)** — Checks the GitHub releases once per session in the background (never blocks; distinct Offline state). When an update is available: red clickable indicator on the Home page ("Update available: vX" — menu: open the release page / update now; green "Up to date", orange "Offline") and a 6-second tile notification 20s after the first Wiimote connects (never shown during a game; tiles stack downward). The direct update runs with the current user account (never admin): it downloads the release archive, extracts it with the bundled 7za.exe (no 7-Zip installation needed), updates the app files, stages the service files through the existing `update_service` mechanism (the running service is never overwritten) and restarts the app.
- **Crash & hang watchdog (service)** — The service monitors the app via a UI-thread heartbeat: crash or frozen UI ("not responding") → automatic restart as the interactive console user (never admin); voluntary exits and service-driven restarts are never "rescued". Full WER crash dumps are captured inside the app folder (`CrashDumps`) for diagnosis.
- **Persistent tag memory for emulator game settings** — `wiimotegun_memory.json` remembers every `-wiimotegun` gamesettings profile, separated per emulator (PCSX2 / Dolphin / DuckStation) and extensible per service: a profile detected once is managed for life (masked in Wiimote/Mouse mode, active in GamePad mode) even if its content is later rewritten by the emulator; unknown files are never tagged or touched.
- **Off-Screen Reload modes** — **Trigger** (press the fire button off-screen to reload — each press counts) and **Auto** (one automatic reload when leaving the screen), selectable globally and per Mouse/GamePad profile. Off-screen, only the trigger is locked: the physical reload button always stays free.
- **TC Bi-directional pedal** — Two configurable pedal buttons (default D-Pad Left/Right): hold a direction while aiming on-screen, release when off-screen, switch from one pedal to the other — Mouse and GamePad.
- **Lock mode on game launch** — Option in ES Scripts: locks the active mode (Mouse/GamePad) at game-start and unlocks at game-end, preventing accidental Home swaps in game.
- **BT Auto-Reset (service)** — Optional automatic Bluetooth adapter reset when a Wiimote fails to connect. Evidence-gated (a real failed pairing attempt, a Wiimote in pairing mode or a wedged radio — never fires when the Wiimote is simply off), radio-only disable/enable cycle with automatic repair passes, growing retry grace and a 3-attempt cap.

---

## [2.3.5.3] - 2026-08-26

### Added
- **Bluetooth pairing (V2 Wiimote / RVL-CNT-01-TR)** — Support for the red sync button, PIN-less SSP authentication and device name refresh. Automatic pairing of the V2 Wiimote works exclusively via the red sync button (**required on every use** — the 1+2 connection method does not work on this hardware; DolphinBar / Mayflash connection: OK).
- **Off-Screen Reload rumble & input locking** — Keyboard/mouse actions are locked while off-screen and vibration/rumble feedback is implemented for reloads.

### Fixed
- **Continuous/Erratic inputs** — Data flow issues resolved: no more fake Nunchuks or continuous inputs on the V2 Wiimote.
- **Nunchuk hotplug** — The Nunchuk can be unplugged/plugged hot during a session. Connection is mandatory right from pairing when using Motion Plus (unlike V1 without Motion Plus).
- **Off-Screen Reload** — The off-screen reload feature (both manual and automatic modes) was broken and now works, helping bypass games and emulators that do not natively support off-screen reloading. Auto/manual off-screen reload handling rewritten.
- **ID masking & flow** — Corrected the binary mask (`0x000000FFFFFFFFFFL`) to ignore the high `0x01` byte on TR models and eliminate phantom Nunchuks.
- **Hotplug management** — Restored handling via standalone mode `0x04` and the extension insertion bit.
- **Motion Plus configuration migration** — Added a migration version to properly reinitialize the Motion Plus configuration if needed.

Validated: off-screen reload (manual and automatic) verified and working; erratic inputs eliminated on the standalone V2 Wiimote (fully functional without Nunchuk); automatic pairing of the V2 Wiimote via the red sync button (required on every use, 1+2 method non-operational); DolphinBar / Mayflash OK; Nunchuk hotplug operational (required upon pairing with MP, or before/after on V1 without MP).

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
