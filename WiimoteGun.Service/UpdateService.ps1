<#
.SYNOPSIS
    Update WiimoteGun Helper Service (Stop -> Replace EXE -> Deploy HmHost -> Start).
    EN: This script stops the service, copies the new executable from 'update_service'
        to the target path, deploys the HmHost folder (UMDF2/HIDMaestro input host)
        when packaged, and restarts the service.
    FR: Ce script arrête le service, copie le nouvel exécutable depuis 'update_service'
        vers la cible, déploie le dossier HmHost (hôte d'entrée UMDF2/HIDMaestro)
        quand il est embarqué, puis redémarre le service.

    [3.1.0.0-fix] GENERIC FIX-ARCHIVE MECHANISM (EN/FR: mécanisme générique d'archive correctif)
    EN: When a specific release needs a fix that the normal app auto-update cannot
        deliver (same version number), publish an asset named
        'updateFix_v<version>.7z' on the same prerelease and set $DefaultFixUrl below.
        The script:
          1. SKIPS the fix when it is already applied (marker file, or the HmHost
             folder already deployed next to the service - once the service is at the
             fix version with its payload, the fix never re-applies).
          2. Downloads the archive, extracts it (7za shipped with the app, or 7-Zip).
          3. Applies its content:
             - root FILES  -> app directory (e.g. the FIXED WiimoteUpdate.ps1 - the
               running app exe is skipped if locked)
             - 'WiimoteGun.Service\update_service\' -> merged into the live
               'update_service\' staging (service exe, UpdateService.ps1, HmHost\)
             - any other payload -> app directory (folders) / staging (service files)
          4. Continues with the NORMAL service update (exe + HmHost + restart).
        The script can be launched directly FROM 'update_service\' (manual fix run):
        it detects that, installs itself to the live location, and applies everything.
        When a NEWER UpdateService.ps1 sits in 'update_service\', the live script
        copies it over itself and RE-LAUNCHES (self-update).
    FR: Quand une release précise a besoin d'un correctif que l'auto-update app ne peut
        pas livrer (même numéro de version), publiez un asset nommé
        'updateFix_v<version>.7z' sur la même prerelease et renseignez $DefaultFixUrl
        ci-dessous. Le script :
          1. SAUTE le correctif quand il est déjà appliqué (fichier marqueur, ou
             dossier HmHost déjà déployé à côté du service - une fois le service à la
             version du correctif avec sa charge, le correctif ne se ré-applique plus).
          2. Télécharge l'archive, l'extrait (7za livré avec l'app, ou 7-Zip).
          3. Applique son contenu :
             - FICHIERS racine -> dossier de l'app (ex. le WiimoteUpdate.ps1 CORRIGÉ -
               l'exe de l'app en cours est sauté s'il est verrouillé)
             - 'WiimoteGun.Service\update_service\' -> fusionné dans le staging
               'update_service\' vivant (exe service, UpdateService.ps1, HmHost\)
             - toute autre charge -> dossier de l'app (dossiers) / staging (fichiers service)
          4. Poursuit avec la mise à jour NORMALE du service (exe + HmHost + restart).
        Le script peut être lancé directement DEPUIS 'update_service\' (exécution
        manuelle du correctif) : il le détecte, s'installe à l'emplacement vivant et
        applique tout. Quand un UpdateService.ps1 PLUS RÉCENT se trouve dans
        'update_service\', le script vivant le copie par-dessus lui-même et SE RELANCE.
#>

[CmdletBinding()]
param (
    # EN: Target is the service EXE in the installation folder / FR: Cible est l'EXE du service installé
    [string]$ServicePath = "$PSScriptRoot\WiimoteGun.Service.exe",

    # [3.1.0.0-fix] EN: Optional direct-download URL of a fix archive (.7z). When set
    #     (or when $DefaultFixUrl below is filled), the fix step runs before the
    #     normal service update.
    #     FR: URL de téléchargement direct optionnelle d'une archive correctif (.7z).
    #     Quand elle est définie (ou quand $DefaultFixUrl ci-dessous est remplie),
    #     l'étape correctif s'exécute avant la mise à jour normale du service.
    [string]$FixUrl = ""
)

# [3.1.0.0-fix] EN: DEFAULT fix archive URL. Fill it once the asset is uploaded to the
#     prerelease ('updateFix_v<version>.7z'). Leave empty to disable the fix step.
#     FR: URL PAR DÉFAUT de l'archive correctif. Renseignez-la une fois l'asset mis en
#     ligne sur la prerelease (« updateFix_v<version>.7z »). Laisser vide pour
#     désactiver l'étape correctif.
$DefaultFixUrl = ""   # e.g. "https://github.com/Aynshe/WiimoteGun/releases/download/<TAG>/updateFix_v3.1.0.0.7z"
if ([string]::IsNullOrWhiteSpace($FixUrl)) { $FixUrl = $DefaultFixUrl }

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

    # -----------------------------------------------------------------------------
    # [3.1.0.0-fix] EN: Live directory resolution + self-install from the staging.
    #     The script normally lives next to the service EXE (live). It can also be
    #     launched directly FROM 'update_service\' (manual fix run): detect that,
    #     resolve the live directory as the parent, and install this copy to the live
    #     location so the app's future prompts run the new script.
    #     FR: Résolution du dossier vivant + auto-install depuis le staging. Le script
    #     vit normalement à côté de l'EXE du service (vivant). Il peut aussi être
    #     lancé directement DEPUIS « update_service\ » (exécution manuelle du
    #     correctif) : le détecter, résoudre le dossier vivant comme le parent, et
    #     installer cette copie à l'emplacement vivant pour que les prochains
    #     prompts de l'app exécutent le nouveau script.
    # -----------------------------------------------------------------------------
    $LiveDir = $PSScriptRoot
    $runningFromStaging = ((Split-Path -Leaf $PSScriptRoot) -eq "update_service")
    if ($runningFromStaging) {
        $LiveDir = Split-Path -Parent $PSScriptRoot
        Write-Host "[self] Running from the 'update_service' staging - live dir resolved to: $LiveDir" -ForegroundColor Cyan
        try {
            Copy-Item -LiteralPath $PSCommandPath -Destination (Join-Path $LiveDir "UpdateService.ps1") -Force
            Write-Host "[self] Installed this script to the live location." -ForegroundColor Green
        }
        catch {
            Write-Host "[self] WARNING: could not install this script to the live location: $($_.Exception.Message)" -ForegroundColor Yellow
        }
        # EN/FR: When launched from the staging, the target service is the LIVE exe
        if ($ServicePath -eq "$PSScriptRoot\WiimoteGun.Service.exe") {
            $ServicePath = Join-Path $LiveDir "WiimoteGun.Service.exe"
        }
    }

    $ServiceUpdateDir = Join-Path $LiveDir "update_service"
    $SourcePath = Join-Path $ServiceUpdateDir "WiimoteGun.Service.exe"
    $AppDir = Split-Path -Parent $LiveDir

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
        try { $oldServiceVersion = (Get-Item $ServicePath).VersionInfo.FileVersion } catch { }
    }

    Write-Host "`n=== [ WiimoteGun Service Update Tool ] ===" -ForegroundColor Cyan
    Write-Host "Target Service: $ServiceName"
    Write-Host "Service Location: $ServicePath" -ForegroundColor Gray
    Write-Host "Update Source: $SourcePath" -ForegroundColor Gray
    Write-Host "------------------------------------------"

    # -----------------------------------------------------------------------------
    # [3.1.0.0-fix] EN: Fix-ARCHIVE step (generic). Guards first:
    #       1. marker file  -> fix already applied -> skip
    #       2. HmHost folder already deployed next to the service -> for a fix that
    #          carries the driver payload, that means the fix content is already in
    #          place (once the service is at the fix version WITH its payload, the
    #          fix never re-applies) -> write the marker, skip
    #     FR: Étape d'ARCHIVE CORRECTIF (générique). Garde-fous d'abord :
    #       1. fichier marqueur -> correctif déjà appliqué -> sauter
    #       2. dossier HmHost déjà déployé à côté du service -> pour un correctif
    #          qui embarque la charge du pilote, cela signifie que le contenu est
    #          déjà en place (une fois le service à la version du correctif AVEC sa
    #          charge, le correctif ne se ré-applique plus) -> écrire le marqueur, sauter
    # -----------------------------------------------------------------------------
    if (-not [string]::IsNullOrWhiteSpace($FixUrl)) {
        $fixName = [IO.Path]::GetFileName(($FixUrl -split '/')[-1])
        $fixMarkerDir = Join-Path $env:ProgramData "WiimoteGun\fixes"
        $fixMarker = Join-Path $fixMarkerDir ($fixName + ".applied")

        if (Test-Path -LiteralPath $fixMarker) {
            Write-Host "[fix] '$fixName' already applied (marker) - skipping the fix step." -ForegroundColor Gray
        }
        elseif (Test-Path -LiteralPath (Join-Path $LiveDir "HmHost")) {
            Write-Host "[fix] HmHost payload already deployed next to the service - fix considered applied, writing the marker." -ForegroundColor Gray
            try {
                New-Item -ItemType Directory -Path $fixMarkerDir -Force | Out-Null
                Set-Content -LiteralPath $fixMarker -Value (Get-Date -Format "yyyy-MM-dd HH:mm:ss") -Encoding ASCII
            } catch { }
        }
        else {
            Write-Host "[fix] Fix archive URL: $FixUrl" -ForegroundColor Cyan

            # EN: Locate the extraction binary (7za.exe shipped with the app, or system 7-Zip)
            #     FR: Localiser le binaire d'extraction (7za.exe livré avec l'app, ou 7-Zip système)
            $sevenZip = $null
            $local7za = Join-Path $AppDir "7za.exe"
            if (Test-Path -LiteralPath $local7za) { $sevenZip = $local7za }
            if (-not $sevenZip) {
                foreach ($name in @("7z.exe", "7za.exe")) {
                    $cmd = Get-Command $name -ErrorAction SilentlyContinue
                    if ($cmd) { $sevenZip = $cmd.Source; break }
                }
            }
            if (-not $sevenZip) {
                foreach ($p in @("$env:ProgramFiles\7-Zip\7z.exe", "${env:ProgramFiles(x86)}\7-Zip\7z.exe")) {
                    if (Test-Path -LiteralPath $p) { $sevenZip = $p; break }
                }
            }
            if (-not $sevenZip) {
                Write-Host "WARNING: no 7-Zip found - cannot extract the fix archive (fix step skipped)." -ForegroundColor Yellow
            }
            else {
                [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
                $fixTemp = Join-Path $env:TEMP ("Wiimote4Guns_Fix_" + [guid]::NewGuid().ToString("N"))
                $fixArchive = Join-Path $fixTemp $fixName
                $fixExtract = Join-Path $fixTemp "extracted"
                try {
                    New-Item -ItemType Directory -Path $fixTemp -Force | Out-Null
                    New-Item -ItemType Directory -Path $fixExtract -Force | Out-Null
                    Write-Host "[fix] Downloading fix archive..." -ForegroundColor Yellow
                    Invoke-WebRequest -Uri $FixUrl -OutFile $fixArchive -UseBasicParsing -TimeoutSec 300
                    $szOut = & $sevenZip "x" "-y" "-o$fixExtract" $fixArchive 2>&1
                    if ($LASTEXITCODE -ne 0) {
                        Write-Host "WARNING: fix archive extraction failed (code $LASTEXITCODE) - fix step skipped." -ForegroundColor Yellow
                        $szOut | ForEach-Object { Write-Host "  $_" -ForegroundColor DarkGray }
                    }
                    else {
                        # EN: Resolve the content root (single top folder -> descend into it)
                        #     FR: Résoudre la racine du contenu (dossier unique au sommet -> y descendre)
                        $rootItems = Get-ChildItem -LiteralPath $fixExtract
                        $fixRoot = $fixExtract
                        if ($rootItems.Count -eq 1 -and $rootItems[0].PSIsContainer) {
                            $fixRoot = $rootItems[0].FullName
                        }
                        Write-Host "[fix] Applying fix content from: $fixRoot" -ForegroundColor Yellow

                        foreach ($fixItem in (Get-ChildItem -LiteralPath $fixRoot)) {
                            if ($fixItem.PSIsContainer) {
                                if ($fixItem.Name -eq "WiimoteGun.Service") {
                                    New-Item -ItemType Directory -Path $ServiceUpdateDir -Force | Out-Null
                                    foreach ($svcItem in (Get-ChildItem -LiteralPath $fixItem.FullName)) {
                                        if (-not $svcItem.PSIsContainer) {
                                            # EN: Service file (exe, ps1) -> staging
                                            #     FR: Fichier service (exe, ps1) -> staging
                                            Copy-Item -LiteralPath $svcItem.FullName -Destination (Join-Path $ServiceUpdateDir $svcItem.Name) -Force
                                            Write-Host "  fix staged (service): $($svcItem.Name) -> update_service\" -ForegroundColor DarkCyan
                                        }
                                        elseif ($svcItem.Name -eq "update_service") {
                                            # EN: THE fix's own staging (service exe + UpdateService.ps1 + HmHost\)
                                            #     -> merge its CONTENT into the live staging
                                            #     FR: Le staging PROPRE au correctif (exe service + UpdateService.ps1 + HmHost\)
                                            #     -> fusionner son CONTENU dans le staging vivant
                                            foreach ($stagedItem in (Get-ChildItem -LiteralPath $svcItem.FullName)) {
                                                Copy-Item -LiteralPath $stagedItem.FullName -Destination $ServiceUpdateDir -Recurse -Force
                                                if ($stagedItem.PSIsContainer) {
                                                    Write-Host "  fix staged (folder): $($stagedItem.Name)\ -> update_service\" -ForegroundColor DarkCyan
                                                }
                                                else {
                                                    Write-Host "  fix staged: $($stagedItem.Name) -> update_service\" -ForegroundColor DarkCyan
                                                }
                                            }
                                        }
                                        else {
                                            # EN: Any other service folder (e.g. HmHost directly) -> staging, recursively
                                            #     FR: Tout autre dossier service (ex. HmHost directement) -> staging, récursivement
                                            Copy-Item -LiteralPath $svcItem.FullName -Destination $ServiceUpdateDir -Recurse -Force
                                            Write-Host "  fix staged (service folder): $($svcItem.Name)\ -> update_service\" -ForegroundColor DarkCyan
                                        }
                                    }
                                }
                                else {
                                    # EN/FR: App-level folder -> recursive merge into the app directory
                                    Copy-Item -LiteralPath $fixItem.FullName -Destination $AppDir -Recurse -Force
                                    Write-Host "  fix merged folder: $($fixItem.Name)\" -ForegroundColor DarkGray
                                }
                            }
                            else {
                                # EN: App file (the FIXED WiimoteUpdate.ps1, exe.config, etc.)
                                #     The RUNNING app exe may be locked - copy best-effort.
                                #     FR: Fichier de l'app (le WiimoteUpdate.ps1 CORRIGÉ, exe.config, etc.)
                                #     L'exe de l'app EN COURS peut être verrouillé - copie au mieux.
                                try {
                                    Copy-Item -LiteralPath $fixItem.FullName -Destination $AppDir -Force -ErrorAction Stop
                                    Write-Host "  fix updated: $($fixItem.Name)" -ForegroundColor DarkGray
                                }
                                catch {
                                    Write-Host "  fix SKIPPED (locked?): $($fixItem.Name)" -ForegroundColor Yellow
                                }
                            }
                        }

                        # EN: Write the fix marker so this fix never re-applies on this machine
                        #     FR: Écrire le marqueur du correctif pour qu'il ne se ré-applique plus
                        try {
                            New-Item -ItemType Directory -Path $fixMarkerDir -Force | Out-Null
                            Set-Content -LiteralPath $fixMarker -Value (Get-Date -Format "yyyy-MM-dd HH:mm:ss") -Encoding ASCII
                        } catch { }

                        Write-Host "[fix] Done - the normal service update below applies the staged content." -ForegroundColor Green
                    }
                }
                catch {
                    Write-Host "WARNING: fix archive step failed: $($_.Exception.Message)" -ForegroundColor Yellow
                }
                finally {
                    try { Remove-Item -LiteralPath $fixTemp -Recurse -Force -ErrorAction SilentlyContinue } catch { }
                }
            }
        }
    }

    # -----------------------------------------------------------------------------
    # [3.1.0.0-fix] EN: SELF-UPDATE + RELAUNCH. If the staging holds a NEWER
    #     UpdateService.ps1 than the live one (delivered by a fix archive or a future
    #     app update), copy it over the live script and RELAUNCH the live script with
    #     the same target so the new logic runs - then exit. This is the generic
    #     delivery path: the script only needs to run ONCE (old or manual) for the
    #     new version to take over.
    #     FR: AUTO-UPDATE + RELANCE. Si le staging contient un UpdateService.ps1 PLUS
    #     RÉCENT que le vivant (livré par une archive correctif ou une future mise à
    #     jour app), le copier par-dessus le script vivant et RELANCER le script vivant
    #     avec la même cible pour que la nouvelle logique s'exécute - puis sortir.
    #     C'est le chemin de livraison générique : le script n'a besoin d'être exécuté
    #     qu'UNE FOIS (ancien ou manuel) pour que la nouvelle version prenne le relais.
    # -----------------------------------------------------------------------------
    $liveScript = Join-Path $LiveDir "UpdateService.ps1"
    $stagedScript = Join-Path $ServiceUpdateDir "UpdateService.ps1"
    if (-not $runningFromStaging -and (Test-Path -LiteralPath $stagedScript) -and (Test-Path -LiteralPath $liveScript)) {
        $stagedTime = (Get-Item -LiteralPath $stagedScript).LastWriteTime
        $liveTime = (Get-Item -LiteralPath $liveScript).LastWriteTime
        if ($stagedTime -gt $liveTime) {
            Write-Host "[self] A NEWER UpdateService.ps1 is staged - self-updating and relaunching..." -ForegroundColor Cyan
            try {
                Copy-Item -LiteralPath $stagedScript -Destination $liveScript -Force
                Start-Process -FilePath "powershell.exe" -ArgumentList @("-NoProfile", "-ExecutionPolicy", "Bypass", "-File", "`"$liveScript`"", "-ServicePath", "`"$ServicePath`"")
                Write-Host "[self] Relaunched the updated script - this window can be closed." -ForegroundColor Green
                Start-Sleep -Seconds 2
                exit 0
            }
            catch {
                Write-Host "[self] WARNING: relaunch failed ($($_.Exception.Message)) - continuing with the current script." -ForegroundColor Yellow
            }
        }
    }

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
        $HmHostSource = Join-Path $ServiceUpdateDir "HmHost"
        $HmHostDest = Join-Path $LiveDir "HmHost"
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
        Write-Host "The service staging is empty - run the app once (it stages service files) or apply the fix archive." -ForegroundColor Gray
        Show-Pause
        exit 1
    }

    # [V57k] EN: When the update replaced a pre-3.0.0.24 service, write the pending
    #     wizard flag (HKLM, writable here as admin). The non-admin app reads it at its
    #     next start, re-shows the Setup Wizard once, and asks the service (SYSTEM) to
    #     clear it via the WIZARD_ACK pipe command.
    #     FR: Quand la mise à jour a remplacé un service antérieur à 3.0.0.24, écrire
    #     le flag de wizard en attente (HKLM, inscriptible ici en tant qu'admin).
    #     L'app non-admin le lit à son prochain démarrage, ré-affiche le Setup Wizard
    #     une fois, et demande au service (SYSTEM) de l'effacer via la commande pipe
    #     WIZARD_ACK.
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

    # 4. Restart the Service (EN: Start / FR: Démarrage)
    Write-Host "[4/4] Restarting service $ServiceName..." -ForegroundColor Yellow
    if (Get-Service $ServiceName -ErrorAction SilentlyContinue) {
        Start-Service $ServiceName
        $svc = Get-Service $ServiceName
        if ($svc.Status -eq 'Running') {
            Write-Host "Service started successfully." -ForegroundColor Green
        }
        else {
            Write-Host "WARNING: Service is not running after start (status: $($svc.Status))" -ForegroundColor Yellow
        }
    }
    else {
        Write-Host "INFO: Service not installed - skipping start." -ForegroundColor Gray
    }

    # [V57k-FIX2] EN: Relaunch Wiimote4Guns AUTOMATICALLY, DE-ELEVATED (explorer launches
    #     the helper .cmd at the user's normal integrity level). The existing -refresh IPC
    #     makes a running instance restart itself; a non-running app simply starts.
    #     FR: Relance Wiimote4Guns AUTOMATIQUEMENT, DÉSÉLEVÉE (explorer lance le .cmd
    #     helper au niveau d'intégrité normal de l'utilisateur). L'IPC -refresh existant
    #     fait se redémarrer une instance en cours ; une app absente démarre simplement.
    $appExe = Join-Path $AppDir "WiimoteGun.exe"
    if (Test-Path $appExe) {
        try {
            $restartCmd = Join-Path $env:TEMP "WiimoteGun_AutoRestart.cmd"
            Set-Content -Path $restartCmd -Value "@echo off`r`nstart `"`" `"$appExe`" -refresh" -Encoding ASCII
            Start-Process explorer.exe -ArgumentList "`"$restartCmd`""
            Write-Host "Wiimote4Guns is restarting automatically (NOT as administrator)." -ForegroundColor Green
        }
        catch {
            Write-Host "WARNING: automatic restart failed: $($_.Exception.Message)" -ForegroundColor Yellow
        }
    }

    Write-Host "`nDONE! Update process complete." -ForegroundColor Cyan
    Show-Pause
}
catch {
    Write-Host "`nCRITICAL ERROR: $($_.Exception.Message)" -ForegroundColor Red
    Show-Pause
    exit 1
}
