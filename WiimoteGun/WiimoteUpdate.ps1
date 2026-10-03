<#
.SYNOPSIS
    WiimoteGun self-update (download release .7z -> extract -> replace -> restart).
    EN: Updates the WiimoteGun APP with the current USER account (never admin).
        Waits for the app to exit, downloads the release archive, extracts it
        (the archive creates a folder named after itself - only its CONTENT is
        used), copies everything into the app directory EXCEPT the running
        service files (those are routed to "WiimoteGun.Service\update_service"
        so the existing admin self-update mechanism applies them later), then
        restarts WiimoteGun.exe as the current user.
    FR: Mise a jour de l'APP WiimoteGun avec le compte UTILISATEUR en cours
        (jamais admin). Attend la sortie de l'app, telecharge l'archive de
        release, l'extrait (l'archive cree un dossier a son nom - seul son
        CONTENU est utilise), copie tout dans le dossier de l'app SAUF les
        fichiers du service actif (routes vers "WiimoteGun.Service\update_service"
        afin que le mecanisme d'auto-update admin existant les applique plus
        tard), puis relance WiimoteGun.exe avec l'utilisateur en cours.
    NOTE: This script is part of the app release: a future release may ship an
          updated copy of itself (the copy step below overwrites this file).
          (EN/FR: Ce script fait partie de la release : une release future peut
          livrer une copie modifiee de lui-meme - l'etape de copie ci-dessous
          ecrase ce fichier.)
#>

[CmdletBinding()]
param (
    # EN: Direct download URL of the release asset (.7z) / FR: URL de telechargement direct de l'asset (.7z)
    [Parameter(Mandatory = $true)]
    [string]$DownloadUrl,

    # EN: App installation directory (default: script directory) / FR: Dossier d'installation de l'app
    [string]$AppDir = $PSScriptRoot,

    # EN: PID of the app instance to wait for before replacing files / FR: PID de l'instance a attendre avant de remplacer les fichiers
    [int]$WaitPid = 0
)

function Show-Pause {
    Write-Host "`nPress any key to close this window... (Appuyez sur une touche pour fermer)" -ForegroundColor Yellow
    $null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
}

# [V56h] EN: Defensive $AppDir normalization. Older app builds pass a BROKEN -AppDir:
#         BaseDirectory ends with a backslash, so the closing quote became escaped (\")
#         and the following arguments (" -WaitPid 12345") were swallowed INTO the value.
#         Strip everything from the first stray quote to recover the real directory.
#         FR: Normalisation défensive de $AppDir. Les anciennes builds de l'app passent
#         un -AppDir CASSÉ : BaseDirectory finit par un antislash, le guillemet fermant
#         devenait échappé (\") et les arguments suivants (« -WaitPid 12345 ») étaient
#         absorbés DANS la valeur. On retire tout à partir du premier guillemet parasite
#         pour récupérer le vrai dossier.
$AppDir = "$AppDir".Trim()
$strayQuote = $AppDir.IndexOf('"')
if ($strayQuote -ge 0) {
    Write-Host "[V56h] Recovering malformed -AppDir (stray quote)..." -ForegroundColor Yellow
    $AppDir = $AppDir.Substring(0, $strayQuote).Trim()
}
$AppDir = $AppDir.TrimEnd('\')
if (-not (Test-Path -LiteralPath $AppDir)) {
    Write-Host "ERROR: App directory not found: '$AppDir'" -ForegroundColor Red
    Show-Pause
    exit 1
}

function Find-7Zip {
    # EN: Locate an extraction binary, in priority order:
    #       1. 7za.exe shipped WITH the app ($AppDir) - works even without 7-Zip installed
    #       2. 7z.exe / 7za.exe on the PATH
    #       3. The usual 7-Zip install folders
    #     FR: Localiser un binaire d'extraction, par ordre de priorite :
    #       1. 7za.exe livre AVEC l'app ($AppDir) - fonctionne meme sans 7-Zip installe
    #       2. 7z.exe / 7za.exe dans le PATH
    #       3. Les dossiers d'installation habituels de 7-Zip
    $local = Join-Path $AppDir "7za.exe"
    if (Test-Path $local) { return $local }

    foreach ($name in @("7z.exe", "7za.exe")) {
        $cmd = Get-Command $name -ErrorAction SilentlyContinue
        if ($cmd) { return $cmd.Source }
    }
    foreach ($p in @("$env:ProgramFiles\7-Zip\7z.exe", "${env:ProgramFiles(x86)}\7-Zip\7z.exe")) {
        if (Test-Path $p) { return $p }
    }
    return $null
}

try {
    Write-Host "`n=== [ Wiimote4Guns App Update ] ===" -ForegroundColor Cyan
    Write-Host "App directory: $AppDir" -ForegroundColor Gray
    Write-Host "Download URL : $DownloadUrl" -ForegroundColor Gray
    Write-Host "------------------------------------------"

    # EN: 0. Sanity checks / FR: Verifications de base
    if (-not (Test-Path (Join-Path $AppDir "WiimoteGun.exe"))) {
        Write-Host "ERROR: WiimoteGun.exe not found in '$AppDir' - aborting." -ForegroundColor Red
        Show-Pause
        exit 1
    }

    # EN: 1. Wait for the app to exit (its files are locked while running).
    #         ALWAYS wait by process name (even without -WaitPid): older app builds
    #         passed a broken -AppDir that swallowed "-WaitPid", so $WaitPid may be 0.
    #     FR: Attendre la sortie de l'app (ses fichiers sont verrouilles pendant
    #         l'execution). TOUJOURS attendre par nom de processus (meme sans
    #         -WaitPid) : les anciennes builds de l'app passaient un -AppDir casse
    #         qui absorbait « -WaitPid », donc $WaitPid peut valoir 0.
    Write-Host "[1/6] Waiting for WiimoteGun to exit..." -ForegroundColor Yellow
    if ($WaitPid -gt 0) {
        $proc = Get-Process -Id $WaitPid -ErrorAction SilentlyContinue
        if ($proc) {
            $proc | Wait-Process -Timeout 30 -ErrorAction SilentlyContinue
        }
    }
    # EN/FR: Fallback: wait for any WiimoteGun process by name
    $left = Get-Process "WiimoteGun" -ErrorAction SilentlyContinue
    if ($left) {
        Write-Host "Still running - waiting up to 15s more..." -ForegroundColor Gray
        $left | Wait-Process -Timeout 15 -ErrorAction SilentlyContinue
    }
    # EN/FR: Last resort: kill lingers (the update script relaunches the app anyway)
    $lingering = Get-Process "WiimoteGun" -ErrorAction SilentlyContinue
    if ($lingering) {
        Write-Host "WARNING: WiimoteGun still running - killing process to unlock files." -ForegroundColor Red
        $lingering | Stop-Process -Force
    }
    Write-Host "WiimoteGun exited." -ForegroundColor Green

    # EN: 2. Download the release archive / FR: Telecharger l'archive de release
    Write-Host "[2/6] Downloading release archive..." -ForegroundColor Yellow
    [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
    $tempDir = Join-Path $env:TEMP ("Wiimote4Guns_Update_" + [guid]::NewGuid().ToString("N"))
    New-Item -ItemType Directory -Path $tempDir -Force | Out-Null
    $archivePath = Join-Path $tempDir "release.7z"

    try {
        Invoke-WebRequest -Uri $DownloadUrl -OutFile $archivePath -UseBasicParsing -TimeoutSec 300
        Write-Host "Downloaded: $([math]::Round((Get-Item $archivePath).Length / 1MB, 1)) MB" -ForegroundColor Green
    }
    catch {
        Write-Host "ERROR: Download failed: $($_.Exception.Message)" -ForegroundColor Red
        Show-Pause
        exit 1
    }

    # EN: 3. Locate the extraction binary (7za.exe shipped with the app, or system 7-Zip)
    #     FR: Localiser le binaire d'extraction (7za.exe livre avec l'app, ou 7-Zip systeme)
    $sevenZip = Find-7Zip
    if (-not $sevenZip) {
        Write-Host "ERROR: No extraction tool found (no 7za.exe next to the app, no 7-Zip installed)." -ForegroundColor Red
        Write-Host "Place 7za.exe in '$AppDir' or install 7-Zip, or update manually from:" -ForegroundColor Gray
        Write-Host "https://github.com/Aynshe/WiimoteGun/releases" -ForegroundColor Gray
        Show-Pause
        exit 1
    }
    Write-Host "[3/6] Extracting with: $sevenZip" -ForegroundColor Yellow
    $extractDir = Join-Path $tempDir "extracted"
    New-Item -ItemType Directory -Path $extractDir -Force | Out-Null

    $szOutput = & $sevenZip "x" "-y" "-o$extractDir" $archivePath 2>&1
    if ($LASTEXITCODE -ne 0) {
        Write-Host "ERROR: 7-Zip extraction failed (code $LASTEXITCODE):" -ForegroundColor Red
        $szOutput | ForEach-Object { Write-Host "  $_" -ForegroundColor DarkGray }
        Show-Pause
        exit 1
    }

    # EN: 4. Resolve the content root: the archive creates a folder named after
    #        itself - use its CONTENT (single top folder -> descend into it)
    #     FR: Resoudre la racine du contenu : l'archive cree un dossier a son
    #        nom - utiliser son CONTENU (dossier unique au sommet -> y descendre)
    $rootItems = Get-ChildItem -LiteralPath $extractDir
    $contentRoot = $extractDir
    if ($rootItems.Count -eq 1 -and $rootItems[0].PSIsContainer) {
        $contentRoot = $rootItems[0].FullName
    }
    Write-Host "Update content root: $contentRoot" -ForegroundColor Gray

    # EN: 5. Copy the content into the app directory
    #     - Top-level FILES   -> copy over the app files (incl. WiimoteUpdate.ps1
    #       itself: a future release can ship an updated script)
    #     - Top-level FOLDERS -> recursive merge, EXCEPT "WiimoteGun.Service"
    #     - "WiimoteGun.Service" folder -> its files go to
    #       "WiimoteGun.Service\update_service" (the RUNNING service exe must NOT
    #       be overwritten; the existing admin self-update mechanism applies them)
    #     FR: Copier le contenu dans le dossier de l'app
    #     - FICHIERS du sommet -> ecraser les fichiers de l'app (y compris
    #       WiimoteUpdate.ps1 lui-meme : une release future peut livrer un script modifie)
    #     - DOSSIERS du sommet -> fusion recursive, SAUF "WiimoteGun.Service"
    #     - Le dossier "WiimoteGun.Service" -> ses fichiers vont dans
    #       "WiimoteGun.Service\update_service" (l'exe du service ACTIF ne doit PAS
    #       etre ecrase ; le mecanisme d'auto-update admin existant les applique)
    Write-Host "[5/6] Updating files..." -ForegroundColor Yellow
    $updatedCount = 0
    $serviceUpdateDir = Join-Path $AppDir "WiimoteGun.Service\update_service"

    # EN: 7za.exe may be RUNNING right now (it extracted this archive) and is locked:
    #     process it LAST with the rename trick (a running exe CAN be renamed).
    #     FR: 7za.exe est peut-etre EN COURS (il a extrait cette archive) et est
    #     verrouille : le traiter EN DERNIER avec l'astuce du renommage (un exe en
    #     cours peut etre RENOMME).
    $sevenZipName = [IO.Path]::GetFileName($sevenZip)

    foreach ($item in (Get-ChildItem -LiteralPath $contentRoot)) {
        if ($item.PSIsContainer) {
            if ($item.Name -eq "WiimoteGun.Service") {
                # EN/FR: Route service files to the update_service staging folder (never overwrite the running exe)
                New-Item -ItemType Directory -Path $serviceUpdateDir -Force | Out-Null
                foreach ($svcItem in (Get-ChildItem -LiteralPath $item.FullName)) {
                    if ($svcItem.PSIsContainer) { continue } # Skip nested folders (incl. a shipped update_service)
                    Copy-Item -LiteralPath $svcItem.FullName -Destination (Join-Path $serviceUpdateDir $svcItem.Name) -Force
                    $updatedCount++
                    Write-Host "  staged (service): $($svcItem.Name) -> update_service\" -ForegroundColor DarkCyan
                }
            }
            else {
                Copy-Item -LiteralPath $item.FullName -Destination $AppDir -Recurse -Force
                $updatedCount++
                Write-Host "  merged folder: $($item.Name)\" -ForegroundColor DarkGray
            }
        }
        elseif ($item.Name -eq $sevenZipName) {
            # EN/FR: Handled last (see below)
        }
        else {
            Copy-Item -LiteralPath $item.FullName -Destination $AppDir -Force
            $updatedCount++
            Write-Host "  updated: $($item.Name)" -ForegroundColor DarkGray
        }
    }

    # EN: Update the extraction tool itself (7za.exe) last — it may currently be
    #     running from $AppDir and locked. Rename the running copy aside (allowed by
    #     Windows), place the new one, then clean the old file best-effort.
    #     FR: Mettre a jour l'outil d'extraction (7za.exe) en dernier - il tourne
    #     peut-etre depuis $AppDir et est verrouille. Renommer la copie en cours
    #     (autorise par Windows), poser le neuf, puis nettoyer l'ancien au mieux.
    $newTool = Join-Path $contentRoot $sevenZipName
    $targetTool = Join-Path $AppDir $sevenZipName
    if (Test-Path -LiteralPath $newTool) {
        try {
            Copy-Item -LiteralPath $newTool -Destination $targetTool -Force -ErrorAction Stop
            Write-Host "  updated: $sevenZipName" -ForegroundColor DarkGray
        }
        catch {
            try {
                $oldTool = "$targetTool.old"
                if (Test-Path $oldTool) { Remove-Item -LiteralPath $oldTool -Force -ErrorAction SilentlyContinue }
                Rename-Item -LiteralPath $targetTool -NewName "$sevenZipName.old" -ErrorAction Stop
                Copy-Item -LiteralPath $newTool -Destination $targetTool -Force -ErrorAction Stop
                Write-Host "  updated (renamed running copy): $sevenZipName" -ForegroundColor DarkGray
            }
            catch {
                Write-Host "  WARNING: could not update $sevenZipName (locked): $($_.Exception.Message)" -ForegroundColor Yellow
            }
        }
        $updatedCount++
    }
    Write-Host "Done: $updatedCount items processed." -ForegroundColor Green

    # EN: 6. Cleanup temp + restart the app as the CURRENT USER (this script already
    #        runs under the user account - plain Start-Process, no elevation)
    #     FR: Nettoyage temporaire + relance de l'app avec l'UTILISATEUR EN COURS
    #        (ce script tourne deja sous le compte utilisateur - Start-Process
    #        simple, sans elevation)
    Write-Host "[6/6] Cleaning up and restarting WiimoteGun..." -ForegroundColor Yellow
    try { Remove-Item -LiteralPath $tempDir -Recurse -Force -ErrorAction SilentlyContinue } catch { }

    $exePath = Join-Path $AppDir "WiimoteGun.exe"
    Start-Process -FilePath $exePath -WorkingDirectory $AppDir
    Write-Host "WiimoteGun restarted." -ForegroundColor Green

    Write-Host "`nDONE! Update complete." -ForegroundColor Cyan
    Write-Host "NOTE: service files were staged in 'WiimoteGun.Service\update_service'." -ForegroundColor Gray
    Write-Host "The app will offer to apply them (admin) on next startup." -ForegroundColor Gray
    Start-Sleep -Seconds 3
}
catch {
    Write-Host "`nCRITICAL ERROR: $($_.Exception.Message)" -ForegroundColor Red
    Show-Pause
    exit 1
}
