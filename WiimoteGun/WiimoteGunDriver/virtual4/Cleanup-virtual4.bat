@echo off
REM Disable Unused vmultia Devices
REM (relative mouse for WiimoteGun)

echo ═══════════════════════════════════════════════════════
echo   vmultid - Disable Unused Devices
echo ═══════════════════════════════════════════════════════
echo.
echo This will remove all vmultid devices except Col03
echo.

REM Get devcon.exe path (current directory)
set DEVCON=%~dp0devcon.exe

if not exist "%DEVCON%" (
    echo ERROR: devcon.exe not found in %~dp0
    pause
    exit /b 1
)

echo Removing unused vmultid collections...
echo.

REM Disable Col01, Col02, Col04, Col05, Col06
echo Disabling Col01 (Touchscreen)...
"%DEVCON%" disable "*vmultid*COL01*"

echo Disabling Col02 (Config Device)...
"%DEVCON%" disable "*vmultid*COL02*"

echo Disabling Col03 (first mouse)...
"%DEVCON%" disable "*vmultid*COL03*"

echo Disabling Col04 (Second mouse)...
"%DEVCON%" disable "*vmultid*COL04*"

echo Disabling Col05 (Stylus)...
"%DEVCON%" disable "*vmultid*COL05*"

echo Disabling Col06 (Gamepad)...
"%DEVCON%" disable "*vmultid*COL06*"

REM Keep COL08 (Control channel) enabled - used by WiimoteGun
REM echo Disabling Col08 (Provider)...
REM "%DEVCON%" disable "*vmultid*COL08*"

REM Keep COL09 (if present) enabled
REM echo Disabling Col09 (Provider)...
REM "%DEVCON%" disable "*vmultid*COL09*"

echo.
echo ═══════════════════════════════════════════════════════
echo   Cleanup complete! Only Col03 remains enabled.
echo ═══════════════════════════════════════════════════════
pause
