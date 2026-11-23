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

function Assert-DirectoryExists([string] $Path, [string] $Label) {
    if (-not (Test-Path -LiteralPath $Path -PathType Container)) {
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
    $stream = [IO.File]::OpenRead($Path)
    try {
        $hasher = [Security.Cryptography.SHA256]::Create()
        try { $bytes = $hasher.ComputeHash($stream) }
        finally { $hasher.Dispose() }
    }
    finally { $stream.Dispose() }
    return ([BitConverter]::ToString($bytes)).Replace('-', '').ToLowerInvariant()
}

function Get-GitMetadata {
    $branch = (& git -C $script:RepositoryRoot rev-parse --abbrev-ref HEAD).Trim()
    $commit = (& git -C $script:RepositoryRoot rev-parse HEAD).Trim()
    $status = (& git -C $script:RepositoryRoot status --porcelain) -join "`n"
    return [pscustomobject]@{ Branch = $branch; Commit = $commit; Dirty = [bool]$status }
}

function Get-PinnedReferenceDefinitions([string] $Path = (Join-Path $script:RepositoryRoot 'scripts\loader-reference-pins.json')) {
    Assert-FileExists $Path 'Reference pin manifest'
    $document = Get-Content -LiteralPath $Path -Raw | ConvertFrom-Json
    $info = Get-Content -LiteralPath (Join-Path $script:RepositoryRoot 'Info.json') -Raw | ConvertFrom-Json
    $loader = @($document.references | Where-Object { $_.Name -eq 'UnityModManager' })
    if ($loader.Count -ne 1 -or $loader[0].Version -ne ($info.ManagerVersion + '.0')) {
        throw 'Info.json ManagerVersion must match the pinned Unity Mod Manager baseline.'
    }
    return @($document.references)
}

function Get-PinnedReferenceDirectory { return (Join-Path $script:RepositoryRoot 'artifacts\references') }

# Ensures every pinned loader reference is present and byte-exact; a hash mismatch fails the build.
function Initialize-PinnedReferences {
    & (Join-Path $PSScriptRoot 'Get-PinnedLoaderReferences.ps1') -ReferenceDirectory (Get-PinnedReferenceDirectory)
}

function Get-PinnedReferenceIdentities {
    $directory = Get-PinnedReferenceDirectory
    foreach ($reference in (Get-PinnedReferenceDefinitions)) {
        $path = Join-Path $directory $reference.File
        if ((Get-Sha256 $path) -ne $reference.Sha256) {
            throw "Pinned reference $($reference.File) does not match its expected SHA-256."
        }
        [ordered]@{
            file = $reference.File
            version = $reference.Version
            assembly_version = [Reflection.AssemblyName]::GetAssemblyName($path).Version.ToString()
            sha256 = $reference.Sha256
            source = $reference.Url
        }
    }
}

# Refuse to package an old build whose manifest was merely lowered after compilation.
function Assert-PinnedBuildReceipt($Receipt, [string] $DllPath) {
    if ($Receipt.dll_sha256 -ne (Get-Sha256 $DllPath)) { throw 'Build receipt does not match the mod DLL; rebuild first.' }
    if (-not $Receipt.PSObject.Properties['loader_references']) { throw 'Build receipt has no pinned loader references; rebuild first.' }
    $pins = @(Get-PinnedReferenceDefinitions)
    $recorded = @($Receipt.loader_references)
    if ($recorded.Count -ne $pins.Count) { throw 'Build receipt has an unexpected loader reference count; rebuild first.' }
    foreach ($pin in $pins) {
        $matches = @($recorded | Where-Object { $_.file -eq $pin.File })
        if ($matches.Count -ne 1 -or $matches[0].version -ne $pin.Version -or
            $matches[0].sha256 -ne $pin.Sha256 -or $matches[0].source -ne $pin.Url) {
            throw "Build receipt does not match pinned reference $($pin.File); rebuild first."
        }
    }
}

# Inspect packaged metadata as well as the build receipt: Install.ps1 also accepts
# a supplied ZIP and therefore calls Validate-Package.ps1 without Package.ps1.
function Assert-PinnedAssemblyReferences([byte[]] $Bytes) {
    if ($PSVersionTable.PSEdition -eq 'Core') {
        Add-Type -AssemblyName System.Reflection.Metadata
        $stream = [IO.MemoryStream]::new($Bytes, $false)
        $reader = [System.Reflection.PortableExecutable.PEReader]::new($stream)
        try {
            $metadata = [System.Reflection.Metadata.PEReaderExtensions]::GetMetadataReader($reader)
            $references = @($metadata.AssemblyReferences | ForEach-Object {
                $reference = $metadata.GetAssemblyReference($_)
                [pscustomobject]@{
                    Name = $metadata.GetString($reference.Name)
                    Version = $reference.Version
                }
            })
        }
        finally { $reader.Dispose(); $stream.Dispose() }
    }
    else {
        $assembly = [Reflection.Assembly]::ReflectionOnlyLoad($Bytes)
        $references = @($assembly.GetReferencedAssemblies())
    }
    foreach ($pin in (Get-PinnedReferenceDefinitions)) {
        $matches = @($references | Where-Object { $_.Name -ceq $pin.Name })
        if ($matches.Count -ne 1 -or $matches[0].Version.ToString() -cne $pin.Version) {
            throw "Packaged DLL must reference pinned $($pin.Name) $($pin.Version); lowering Info.json alone is not sufficient."
        }
    }
}
