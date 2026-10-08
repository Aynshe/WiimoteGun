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

    # [V57k] EN: Capture the version of the OLD service exe BEFORE the replacement -
    #     the only reliable place that knows it. If it is older than 3.0.0.24, a
    #     pending flag is written so the app re-shows the Setup Wizard once at its
    #     next start (the app consumes and clears the flag through the service).
    #     FR: Capture la version de l'ANCIEN exe du service AVANT le remplacement -
    #     seul endroit fiable la connaissant. Si elle est inférieure à 3.0.0.24, un
    #     flag en attente est écrit pour que l'app ré-affiche le Setup Wizard une fois
    #     à son prochain démarrage (l'app consomme et efface le flag via le service).
    $oldServiceVersion = $null
    if (Test-Path $ServicePath) {
        $oldServiceVersion = (Get-Item $ServicePath).VersionInfo.FileVersion
        Write-Host "Installed service version (before update): $oldServiceVersion" -ForegroundColor Gray
    }

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

    # [V57d] EN: Kill a lingering HmHost (UMDF2/HIDMaestro input host child process) too
    #     FR: Tuer aussi un HmHost résiduel (processus enfant hôte d'entrée UMDF2/HIDMaestro)
    $lingeringHm = Get-Process "HmHost" -ErrorAction SilentlyContinue
    if ($lingeringHm) {
        $lingeringHm | Stop-Process -Force
        Write-Host "HmHost process terminated." -ForegroundColor Green
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

        # [V57d] EN: Deploy the HmHost folder (UMDF2/HIDMaestro input host) next to the
        #     installed service when it is packaged - required by the RawInput (UMDF2) mode.
        #     FR: Déployer le dossier HmHost (hôte d'entrée UMDF2/HIDMaestro) à côté du
        #     service installé quand il est embarqué - requis par le mode RawInput (UMDF2).
        $HmHostSource = "$PSScriptRoot\update_service\HmHost"
        $ServiceDir = Split-Path -Parent $ServicePath
        $HmHostDest = Join-Path $ServiceDir "HmHost"
        if (Test-Path $HmHostSource) {
            try {
                if (Test-Path $HmHostDest) { Remove-Item -LiteralPath $HmHostDest -Recurse -Force -ErrorAction SilentlyContinue }
                Copy-Item -Path $HmHostSource -Destination $HmHostDest -Recurse -Force -ErrorAction Stop
                Write-Host "Success: HmHost (UMDF2 input host) deployed to $HmHostDest" -ForegroundColor Green
            }
            catch {
                Write-Host "WARNING: Could not deploy the HmHost folder ($($_.Exception.Message))" -ForegroundColor Yellow
                Write-Host "FR: Le dossier HmHost n'a pas pu etre deploye - le mode RawInput (UMDF2) ne fonctionnera pas." -ForegroundColor Yellow
            }
        }
        else {
            Write-Host "INFO: No HmHost folder in update package (RawInput UMDF2 mode will not be available)." -ForegroundColor Gray
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

    # [V57k] EN: When the update replaced a pre-3.0.0.24 service, write the pending
    #     wizard flag (HKLM, writable here as admin). The non-admin app reads it at its
    #     next start, re-shows the Setup Wizard once, and asks the service (SYSTEM) to
    #     clear it via the WIZARD_ACK pipe command. Version parse failures never fail the
    #     script (the update itself already succeeded).
    #     FR: Quand la mise à jour a remplacé un service antérieur à 3.0.0.24, écrire
    #     le flag de wizard en attente (HKLM, inscriptible ici en tant qu'admin). L'app
    #     non-admin le lit à son prochain démarrage, ré-affiche le Setup Wizard une
    #     fois, et demande au service (SYSTEM) de l'effacer via la commande pipe
    #     WIZARD_ACK. Un échec de parsage de version ne fait jamais échouer le script
    #     (la mise à jour elle-même a déjà réussi).
    $needsWizardFlag = $false
    if ($oldServiceVersion) {
        try { $needsWizardFlag = ([version]$oldServiceVersion -lt [version]'3.0.0.24') } catch { $needsWizardFlag = $false }
    }
    if ($needsWizardFlag) {
        try {
            $regKey = [Microsoft.Win32.Registry]::LocalMachine.CreateSubKey("SOFTWARE\WiimoteGun")
            $regKey.SetValue("ShowSetupWizardPending", 1, [Microsoft.Win32.RegistryValueKind]::DWord)
            $regKey.Close()
            Write-Host "Setup Wizard pending flag written (service was $oldServiceVersion < 3.0.0.24)." -ForegroundColor Yellow
        }
        catch {
            Write-Host "WARNING: could not write the Setup Wizard pending flag ($($_.Exception.Message))" -ForegroundColor Yellow
        }
    }

    # [V57k-FIX2] EN: Relaunch Wiimote4Guns AUTOMATICALLY, DE-ELEVATED. The script runs
    #     elevated, and the user explicitly requires a NON-ADMIN app: explorer launches
    #     the helper .cmd at the user's normal integrity level (explorer does not forward
    #     arguments, hence the temporary .cmd wrapper). The existing -refresh IPC does the
    #     rest: a running instance restarts itself (restart batch), a non-running app
    #     simply starts - either way the pending wizard flag is consumed at the new start
    #     and the Setup Wizard opens once.
    #     FR: Relance Wiimote4Guns AUTOMATIQUEMENT, DÉSÉLEVÉE. Le script tourne élevé et
    #     l'utilisateur exige une app NON-ADMIN : explorer lance le .cmd helper au niveau
    #     d'intégrité normal de l'utilisateur (explorer ne transmet pas les arguments,
    #     d'où le .cmd temporaire). L'IPC -refresh existant fait le reste : une instance
    #     en cours se redémarre (batch de restart), une app absente démarre simplement -
    #     dans les deux cas le flag wizard en attente est consommé au nouveau démarrage
    #     et le Setup Wizard s'ouvre une fois.
    $appExe = Join-Path (Split-Path $PSScriptRoot -Parent) "WiimoteGun.exe"
    if (-not (Test-Path $appExe)) { $appExe = Join-Path $PSScriptRoot "WiimoteGun.exe" }
    if (Test-Path $appExe) {
        try {
            $restartCmd = Join-Path $env:TEMP "WiimoteGun_AutoRestart.cmd"
            Set-Content -Path $restartCmd -Value "@echo off`r`nstart `"`" `"$appExe`" -refresh" -Encoding ASCII
            Start-Process explorer.exe -ArgumentList "`"$restartCmd`""
            Write-Host "Wiimote4Guns is restarting automatically (NOT as administrator) - the Setup Wizard will open once." -ForegroundColor Green
            Write-Host "FR: Wiimote4Guns redemarre automatiquement (SANS administrateur) - le Setup Wizard s'ouvrira une fois." -ForegroundColor Green
        }
        catch {
            Write-Host "WARNING: automatic restart failed - restart Wiimote4Guns manually (do NOT run it as administrator): $($_.Exception.Message)" -ForegroundColor Yellow
        }
    }
    else {
        Write-Host "Restart Wiimote4Guns now (do NOT run it as administrator) - the Setup Wizard will open once." -ForegroundColor Yellow
        Write-Host "FR: Redemarrez Wiimote4Guns maintenant (SANS 'Executer en tant qu'administrateur') - le Setup Wizard s'ouvrira une fois." -ForegroundColor Yellow
    }

    Write-Host "`nDONE! Update process complete." -ForegroundColor Cyan
    Show-Pause
}
catch {
    Write-Host "`nCRITICAL ERROR: $($_.Exception.Message)" -ForegroundColor Red
    Show-Pause
    exit 1
}
