<#
.SYNOPSIS
    Update WiimoteGun Helper Service (Stop -> Replace EXE -> Start).
    EN: This script stops the service, copies the new executable from 'update_service' to the target path, and restarts it.
    FR: Ce script arrête le service, copie le nouvel exécutable depuis 'update_service' vers la cible, et le redémarre.
#>

[CmdletBinding()]
param (
    # EN: Target is the service EXE in the installation folder / FR: Cible est l'EXE du service installé
    [string]$ServicePath = "$PSScriptRoot\WiimoteGun.Service.exe"
)

# EN: Source is ALWAYS in the 'update_service' subfolder relative to the script
# FR: La source est TOUJOURS dans le sous-dossier 'update_service' relatif au script
$SourcePath = "$PSScriptRoot\update_service\WiimoteGun.Service.exe"

function Show-Pause {
    Write-Host "`nAppuyez sur une touche pour fermer cette fenêtre..." -ForegroundColor Yellow
    $null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
}

try {
    # -----------------------------------------------------------------------------
    # Admin Check (EN: Check for Administrator privileges / FR: Vérifie les privilèges Admin)
    # -----------------------------------------------------------------------------
    if (-not ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
        Write-Host "CRITICAL: This script MUST be run as Administrator!" -ForegroundColor Red
        Write-Host "FR: Ce script DOIT être exécuté en tant qu'Administrateur !" -ForegroundColor Red
        Show-Pause
        exit 1
    }

    $ServiceName = "WiimoteGunHelper"

    Write-Host "`n=== [ WiimoteGun Service Update Tool ] ===" -ForegroundColor Cyan
    Write-Host "Target Service: $ServiceName" -ForegroundColor White
    Write-Host "Service Location: $ServicePath" -ForegroundColor Gray
    Write-Host "Update Source: $SourcePath" -ForegroundColor Gray
    Write-Host "------------------------------------------"

    # 1. Stop the Service (EN: Stop / FR: Arrêt)
    if (Get-Service $ServiceName -ErrorAction SilentlyContinue) {
        Write-Host "[1/4] Stopping service $ServiceName..." -ForegroundColor Yellow
        Stop-Service $ServiceName -Force -ErrorAction SilentlyContinue
        
        # Wait for completion (EN: Wait for stop / FR: Attente de l'arrêt)
        $waitCount = 0
        while (((Get-Service $ServiceName).Status -ne 'Stopped') -and ($waitCount -lt 15)) {
            Write-Host "Waiting for service to stop... ($waitCount)"
            Start-Sleep -Seconds 1
            $waitCount++
        }
        
        if ((Get-Service $ServiceName).Status -ne 'Stopped') {
            Write-Host "WARNING: Service failed to stop gracefully. Killing process..." -ForegroundColor Red
            $proc = Get-Process "WiimoteGun.Service" -ErrorAction SilentlyContinue
            if ($proc) { $proc | Stop-Process -Force }
        }
        else {
            Write-Host "Service stopped successfully." -ForegroundColor Green
        }
    }
    else {
        Write-Host "INFO: Service $ServiceName is not installed or not found." -ForegroundColor Gray
    }

    # 2. Cleanup Processes (EN: Kill lingers / FR: Nettoyage processus)
    Write-Host "[2/4] Cleaning up lingering processes..." -ForegroundColor Yellow
    $lingering = Get-Process "WiimoteGun.Service" -ErrorAction SilentlyContinue
    if ($lingering) {
        $lingering | Stop-Process -Force
        Write-Host "Process terminated." -ForegroundColor Green
    }
    else {
        Write-Host "No lingering processes found." -ForegroundColor Green
    }

    # 3. Replace the Executable (EN: Replace EXE / FR: Remplacement EXE)
    Write-Host "[3/4] Replacing executable..." -ForegroundColor Yellow
    if (Test-Path $SourcePath) {
        try {
            Copy-Item -Path $SourcePath -Destination $ServicePath -Force -ErrorAction Stop
            Write-Host "Success: Service executable updated." -ForegroundColor Green
        }
        catch {
            Write-Host "ERROR: Could not replace file. Is it still locked by another app?" -ForegroundColor Red
            Write-Host $_.Exception.Message -ForegroundColor Red
            Show-Pause
            exit 1
        }
    }
    else {
        Write-Host "ERROR: Source file NOT FOUND at: $SourcePath" -ForegroundColor Red
        Write-Host "Ensure the folder '$PSScriptRoot\update_service\' contains the new WiimoteGun.Service.exe" -ForegroundColor Gray
        Show-Pause
        exit 1
    }

    # 4. Start the Service (EN: Start / FR: Démarrage)
    if (Get-Service $ServiceName -ErrorAction SilentlyContinue) {
        Write-Host "[4/4] Restarting service $ServiceName..." -ForegroundColor Yellow
        Start-Service $ServiceName
        Write-Host "Service started successfully." -ForegroundColor Green
    }
    else {
        Write-Host "SKIP: Service not installed, cannot start." -ForegroundColor Gray
    }

    Write-Host "`nDONE! Update process complete." -ForegroundColor Cyan
    Show-Pause
}
catch {
    Write-Host "`nCRITICAL ERROR: $($_.Exception.Message)" -ForegroundColor Red
    Show-Pause
    exit 1
}
