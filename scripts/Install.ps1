[CmdletBinding(SupportsShouldProcess=$true)]
param([string] $PackagePath)

. (Join-Path $PSScriptRoot 'Common.ps1')
$root = Get-RepositoryRoot
$config = Get-KingmakerConfiguration
if (-not $PackagePath) {
    $PackagePath = Get-ChildItem -LiteralPath (Join-Path $root 'artifacts\packages') -Filter 'KingmakerDiceRoller-*.zip' -File |
        Sort-Object LastWriteTimeUtc -Descending |
        Select-Object -First 1 -ExpandProperty FullName
}
Assert-FileExists $PackagePath 'Package ZIP'
$PackagePath = [IO.Path]::GetFullPath($PackagePath)
& (Join-Path $PSScriptRoot 'Validate-Package.ps1') -PackagePath $PackagePath

$running = @(Get-Process -Name 'Kingmaker' -ErrorAction SilentlyContinue)
if ($running.Count -gt 0) { throw 'Pathfinder: Kingmaker is running. Exit the game before installing the mod.' }

Add-Type -AssemblyName System.IO.Compression.FileSystem
$temporary = Join-Path ([IO.Path]::GetTempPath()) ('KingmakerDiceRoller-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Force -Path $temporary | Out-Null
try {
    [IO.Compression.ZipFile]::ExtractToDirectory($PackagePath, $temporary)
    $source = Join-Path $temporary 'KingmakerDiceRoller'
    Assert-DirectoryExists $source 'Extracted mod directory'
    $sourceDll = Join-Path $source 'KingmakerDiceRoller.dll'
    Assert-FileExists $sourceDll 'Extracted mod DLL'
    $sourceDllHash = Get-Sha256 $sourceDll

    $target = Join-Path $config.ModsDir 'KingmakerDiceRoller'
    if (-not $PSCmdlet.ShouldProcess($target, 'Transactionally install Kingmaker Dice Roller')) {
        Write-Host 'WhatIf completed: package validation passed and no live files were changed.'
        return
    }

    New-Item -ItemType Directory -Force -Path $config.ModsDir | Out-Null
    $transactionId = [Guid]::NewGuid().ToString('N')
    $staging = Join-Path $config.ModsDir ('.KingmakerDiceRoller.install.' + $transactionId)
    $backup = $null
    $targetCommitted = $false
    try {
        New-Item -ItemType Directory -Force -Path $staging | Out-Null
        foreach ($item in Get-ChildItem -LiteralPath $source -Force) {
            Copy-Item -LiteralPath $item.FullName -Destination $staging -Recurse -Force
        }
        $stagedDll = Join-Path $staging 'KingmakerDiceRoller.dll'
        Assert-FileExists $stagedDll 'Staged mod DLL'
        if ((Get-Sha256 $stagedDll) -ne $sourceDllHash) { throw 'Staged DLL hash does not match the validated package.' }

        if (Test-Path -LiteralPath $target) {
            $stamp = [DateTime]::Now.ToString('yyyyMMdd-HHmmss')
            $backup = Join-Path $config.InstallDir "ModBackups\KingmakerDiceRoller\$stamp-$transactionId"
            New-Item -ItemType Directory -Force -Path (Split-Path -Parent $backup) | Out-Null
            Move-Item -LiteralPath $target -Destination $backup
        }

        Move-Item -LiteralPath $staging -Destination $target
        $targetCommitted = $true
        $installedDll = Join-Path $target 'KingmakerDiceRoller.dll'
        Assert-FileExists $installedDll 'Installed mod DLL'
        if ((Get-Sha256 $installedDll) -ne $sourceDllHash) { throw 'Installed DLL hash does not match the validated package.' }
    }
    catch {
        $failure = $_
        if (Test-Path -LiteralPath $staging) { Remove-Item -LiteralPath $staging -Recurse -Force }
        if ($targetCommitted -and (Test-Path -LiteralPath $target)) { Remove-Item -LiteralPath $target -Recurse -Force }
        if ($backup -and (Test-Path -LiteralPath $backup) -and -not (Test-Path -LiteralPath $target)) {
            Move-Item -LiteralPath $backup -Destination $target
        }
        throw "Installation failed and rollback was attempted: $($failure.Exception.Message)"
    }

    Write-Host "Installed only: $target"
    Write-Host "DLL SHA-256: $sourceDllHash"
    if ($backup) { Write-Host "Previous installation backed up to $backup" }
}
finally {
    if (Test-Path -LiteralPath $temporary) { Remove-Item -LiteralPath $temporary -Recurse -Force }
}
