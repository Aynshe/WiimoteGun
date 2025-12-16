@echo off
REM Disable Unused vmultia Devices (Match Gunmote behavior)
REM Keeps only Col03 (relative mouse for WiimoteGun)

echo ═══════════════════════════════════════════════════════
echo   vmultia - Disable Unused Devices
echo ═══════════════════════════════════════════════════════
echo.
echo This will remove all vmultia devices except Col03
echo.

REM Get devcon.exe path (current directory)
set DEVCON=%~dp0devcon.exe

if not exist "%DEVCON%" (
    echo ERROR: devcon.exe not found in %~dp0
    pause
    exit /b 1
)

echo Removing unused vmultia collections...
echo.

REM Disable Col01, Col02, Col04, Col05, Col06, Col08, Col09 (matching Gunmote)
echo Disabling Col01 (Touchscreen)...
"%DEVCON%" disable "*vmultia*COL01*"

echo Disabling Col02 (Config Device)...
"%DEVCON%" disable "*vmultia*COL02*"

echo Disabling Col03 (first mouse)...
"%DEVCON%" disable "*vmultia*COL03*"

echo Disabling Col04 (Second mouse)...
"%DEVCON%" disable "*vmultia*COL04*"

echo Disabling Col05 (Stylus)...
"%DEVCON%" disable "*vmultia*COL05*"

echo Disabling Col06 (Gamepad)...
"%DEVCON%" disable "*vmultia*COL06*"

echo Disabling Col08 (Provider)...
"%DEVCON%" disable "*vmultia*COL08*"

echo Disabling Col09 (Provider)...
"%DEVCON%" disable "*vmultia*COL09*"

echo.
echo ═══════════════════════════════════════════════════════
echo   Cleanup complete! Only Col03 remains enabled.
echo ═══════════════════════════════════════════════════════
pause
