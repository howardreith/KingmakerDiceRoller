Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$script:RepositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path

function Get-RepositoryRoot {
    return $script:RepositoryRoot
}

function Assert-FileExists([string] $Path, [string] $Label) {
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        throw "$Label was not found: $Path"
    }
}

function Resolve-PythonCommand {
    $python = Get-Command python -ErrorAction SilentlyContinue
    if ($python) { return [pscustomobject]@{ Executable = $python.Source; Prefix = @() } }
    $python3 = Get-Command python3 -ErrorAction SilentlyContinue
    if ($python3) { return [pscustomobject]@{ Executable = $python3.Source; Prefix = @() } }
    $py = Get-Command py -ErrorAction SilentlyContinue
    if ($py) { return [pscustomobject]@{ Executable = $py.Source; Prefix = @('-3') } }
    throw 'Python 3 is required for deterministic repository tooling.'
}

function Invoke-RepositoryPython([string[]] $Arguments) {
    $command = Resolve-PythonCommand
    $prefix = @($command.Prefix)
    & $command.Executable @prefix @Arguments
    if ($LASTEXITCODE -ne 0) { throw "Python command failed with exit code $LASTEXITCODE." }
}

function Resolve-MSBuild {
    if ($env:MSBUILD_EXE_PATH -and (Test-Path -LiteralPath $env:MSBUILD_EXE_PATH)) { return $env:MSBUILD_EXE_PATH }
    $command = Get-Command msbuild.exe -ErrorAction SilentlyContinue
    if ($command) { return $command.Source }
    $vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
    if (Test-Path -LiteralPath $vswhere) {
        $found = & $vswhere -latest -products * -requires Microsoft.Component.MSBuild -find 'MSBuild\**\Bin\MSBuild.exe' | Select-Object -First 1
        if ($found -and (Test-Path -LiteralPath $found)) { return $found }
    }
    throw 'MSBuild.exe was not found. Install Visual Studio Build Tools with .NET Framework 4.7.2 targeting support.'
}

function Get-KingmakerConfiguration([string] $PropsPath = (Join-Path $script:RepositoryRoot 'GamePath.props')) {
    Assert-FileExists $PropsPath 'GamePath.props'
    [xml] $xml = Get-Content -LiteralPath $PropsPath -Raw
    $group = @($xml.Project.PropertyGroup) | Where-Object { $_.KingmakerInstallDir } | Select-Object -First 1
    if (-not $group) { throw 'GamePath.props does not define KingmakerInstallDir.' }
    $install = [Environment]::ExpandEnvironmentVariables([string]$group.KingmakerInstallDir)
    $managed = [string]$group.KingmakerManagedDir
    if (-not $managed) { $managed = Join-Path $install 'Kingmaker_Data\Managed' }
    $managed = $managed.Replace('$(KingmakerInstallDir)', $install)
    $umm = [string]$group.UnityModManagerDir
    if (-not $umm) { $umm = Join-Path $managed 'UnityModManager' }
    $umm = $umm.Replace('$(KingmakerManagedDir)', $managed).Replace('$(KingmakerInstallDir)', $install)
    $config = [pscustomobject]@{
        InstallDir = [IO.Path]::GetFullPath($install)
        ManagedDir = [IO.Path]::GetFullPath($managed)
        UnityModManagerDir = [IO.Path]::GetFullPath($umm)
        ModsDir = [IO.Path]::GetFullPath((Join-Path $install 'Mods'))
    }
    Assert-FileExists (Join-Path $config.ManagedDir 'Assembly-CSharp.dll') 'Kingmaker Assembly-CSharp.dll'
    Assert-FileExists (Join-Path $config.UnityModManagerDir 'UnityModManager.dll') 'UnityModManager.dll'
    Assert-FileExists (Join-Path $config.UnityModManagerDir '0Harmony12.dll') '0Harmony12.dll'
    return $config
}

function Get-Sha256([string] $Path) {
    Assert-FileExists $Path 'Hash input'
    return (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash.ToLowerInvariant()
}

function Get-GitMetadata {
    $branch = (& git -C $script:RepositoryRoot rev-parse --abbrev-ref HEAD).Trim()
    $commit = (& git -C $script:RepositoryRoot rev-parse HEAD).Trim()
    $status = (& git -C $script:RepositoryRoot status --porcelain) -join "`n"
    return [pscustomobject]@{ Branch = $branch; Commit = $commit; Dirty = [bool]$status }
}
