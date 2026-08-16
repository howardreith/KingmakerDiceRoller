[CmdletBinding(SupportsShouldProcess=$true)]
param([string] $PackagePath)
. (Join-Path $PSScriptRoot 'Common.ps1')
$root = Get-RepositoryRoot
$config = Get-KingmakerConfiguration
if (-not $PackagePath) {
    $PackagePath = Get-ChildItem -LiteralPath (Join-Path $root 'artifacts\packages') -Filter 'KingmakerDiceRoller-*.zip' -File | Sort-Object LastWriteTimeUtc -Descending | Select-Object -First 1 -ExpandProperty FullName
}
Assert-FileExists $PackagePath 'Package ZIP'
Add-Type -AssemblyName System.IO.Compression.FileSystem
$archive = [IO.Compression.ZipFile]::OpenRead($PackagePath)
try {
    $allowed = @(
        'KingmakerDiceRoller/KingmakerDiceRoller.dll','KingmakerDiceRoller/Info.json','KingmakerDiceRoller/LICENSE',
        'KingmakerDiceRoller/THIRD-PARTY-NOTICES.md','KingmakerDiceRoller/README.md',
        'KingmakerDiceRoller/licenses/UPSTREAM-WOTR-DICE-ROLLER-MIT.txt'
    )
    $entries = @($archive.Entries | Where-Object { $_.FullName -and -not $_.FullName.EndsWith('/') } | ForEach-Object { $_.FullName.Replace('\','/') })
    foreach ($entry in $entries) {
        if ($entry.Contains('..') -or -not $entry.StartsWith('KingmakerDiceRoller/') -or $entry -notin $allowed) { throw "Unsafe or unexpected package entry: $entry" }
    }
    foreach ($required in $allowed) { if ($required -notin $entries) { throw "Package entry missing: $required" } }
}
finally { $archive.Dispose() }
$temporary = Join-Path ([IO.Path]::GetTempPath()) ('KingmakerDiceRoller-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Force -Path $temporary | Out-Null
try {
    [IO.Compression.ZipFile]::ExtractToDirectory($PackagePath, $temporary)
    $source = Join-Path $temporary 'KingmakerDiceRoller'
    $target = Join-Path $config.ModsDir 'KingmakerDiceRoller'
    if ($PSCmdlet.ShouldProcess($target, 'Install Kingmaker Dice Roller')) {
        New-Item -ItemType Directory -Force -Path $config.ModsDir | Out-Null
        if (Test-Path -LiteralPath $target) {
            $stamp = [DateTime]::Now.ToString('yyyyMMdd-HHmmss')
            $backup = Join-Path $config.InstallDir "ModBackups\KingmakerDiceRoller\$stamp"
            New-Item -ItemType Directory -Force -Path (Split-Path -Parent $backup) | Out-Null
            Move-Item -LiteralPath $target -Destination $backup
            Write-Host "Previous installation backed up to $backup"
        }
        Copy-Item -LiteralPath $source -Destination $target -Recurse
        Write-Host "Installed only: $target"
    }
}
finally { if (Test-Path -LiteralPath $temporary) { Remove-Item -LiteralPath $temporary -Recurse -Force } }
