@echo off
:: Batch wrapper to run UpdateService.ps1 as Administrator
:: EN: This script launches PowerShell with elevated privileges and passes all arguments.
:: FR: Ce script lance PowerShell avec les privilèges élevés et transmet tous les arguments.

set "SCRIPT_DIR=%~dp0"
set "PS_SCRIPT=%SCRIPT_DIR%UpdateService.ps1"

echo.
echo ======================================================
echo   WiimoteGun Service Update (Admin Wrapper)
echo ======================================================
echo.

:: Check for Administrator privileges
net session >nul 2>&1
if %errorLevel% == 0 (
    echo [OK] Running as Administrator.
) else (
    echo [!] ERROR: Please run this Batch file as Administrator.
    echo [!] FR: Veuillez executer ce fichier Batch en tant qu'Administrateur.
    echo.
    pause
    exit /b 1
)

:: Run the PowerShell script with arguments
echo [INFO] Executing PowerShell script: %PS_SCRIPT%
echo [INFO] Arguments passed: %*
echo.

powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%PS_SCRIPT%" %*

echo.
echo [INFO] Script execution finished with code %errorLevel%.
pause
