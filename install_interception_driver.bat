@echo off
echo ============================================================
echo  Interception Driver Installation
echo  Installation du driver Interception
echo ============================================================
echo.

REM Check if running as administrator
net session >nul 2>&1
if %errorLevel% neq 0 (
    echo ERROR: This script must be run as Administrator!
    echo ERREUR: Ce script doit etre lance en tant qu'Administrateur!
    echo.
    echo Right-click and select "Run as Administrator"
    echo Clic droit et selectionnez "Executer en tant qu'administrateur"
    pause
    exit /b 1
)

echo Running from directory: %~dp0
echo.

cd /d "%~dp0WiimoteGun\bin\Debug\WiimoteGunDriver\command line installer"

if not exist "install-interception.exe" (
    echo ERROR: install-interception.exe not found!
    echo ERREUR: install-interception.exe introuvable!
    echo.
    echo Expected path: %CD%\install-interception.exe
    pause
    exit /b 1
)

echo Installing Interception driver...
echo Installation du driver Interception...
echo.

install-interception.exe /install

echo.
echo ============================================================
echo  Installation completed!
echo  Installation terminee!
echo ============================================================
echo.
echo IMPORTANT: You MUST restart your PC for changes to take effect.
echo IMPORTANT: Vous DEVEZ redemarrer votre PC pour que les changements prennent effet.
echo.

pause
