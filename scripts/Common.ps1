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

# The bytes Git checks out for <commit>:<path> in this repository (its filters and line-ending rules applied).
function Get-GitCheckoutBytes([string] $Commit, [string] $Path) {
    $start = [Diagnostics.ProcessStartInfo]::new('git')
    $start.Arguments = "-C `"$($script:RepositoryRoot)`" cat-file --filters `"$($Commit):$($Path.Replace('\', '/'))`""
    $start.UseShellExecute = $false; $start.RedirectStandardOutput = $true; $start.RedirectStandardError = $true
    $process = [Diagnostics.Process]::Start($start)
    $buffer = [IO.MemoryStream]::new()
    try {
        $errorTask = $process.StandardError.ReadToEndAsync()
        $process.StandardOutput.BaseStream.CopyTo($buffer)
        $process.WaitForExit()
        if ($process.ExitCode -ne 0) { throw "git cat-file --filters $($Commit):$Path failed: $($errorTask.Result)" }
        return , $buffer.ToArray()
    }
    finally { $buffer.Dispose(); $process.Dispose() }
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
    if (-not ($info.ManagerVersion -is [string])) { throw 'Info.json ManagerVersion must be a JSON string.' }
    # The product compiles against exactly these loader assemblies; the same canonical set is enforced by
    # scripts\Get-PinnedLoaderReferences.ps1 (which MSBuild runs), so no pin can be dropped or renamed unchecked.
    $canonical = [ordered]@{ 'UnityModManager' = 'UnityModManager.dll'; '0Harmony12' = '0Harmony12.dll' }
    $references = @($document.references)
    if ($references.Count -ne $canonical.Count) { throw "The pin file must define exactly: $($canonical.Keys -join ', ')." }
    foreach ($name in $canonical.Keys) {
        $match = @($references | Where-Object { $_.Name -is [string] -and $_.Name -ceq $name })
        if ($match.Count -ne 1 -or -not ($match[0].File -is [string]) -or $match[0].File -cne $canonical[$name] -or
            -not ($match[0].Version -is [string]) -or -not ($match[0].Sha256 -is [string])) {
            throw "The pin file must define $name exactly once as $($canonical[$name])."
        }
    }
    $loader = @($references | Where-Object { $_.Name -ceq 'UnityModManager' })
    if ($loader[0].Version -cne ($info.ManagerVersion + '.0')) {
        throw 'Info.json ManagerVersion must match the pinned Unity Mod Manager baseline.'
    }
    return $references
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

# Reads a compiler-input list written by the WriteCompilerReferenceList target (the intermediate directory, then every Csc
# argument) into path-free identities in a canonical order: kind reference/compile/file-option with the file's name
# (repository-relative, obj:<name> for generated files in the intermediate directory, otherwise the file name) and
# SHA-256; kind option for every other argument verbatim; kind output for output locations (file name only).
function ConvertFrom-CompilerInputList([string] $ListPath) {
    $rootPrefix = [IO.Path]::GetFullPath($script:RepositoryRoot).TrimEnd('\') + '\'
    $lines = @(Get-Content -LiteralPath $ListPath -Encoding UTF8 | Where-Object { $_ })
    if ($lines.Count -lt 4 -or -not $lines[0].StartsWith('intermediate|') -or -not $lines[1].StartsWith('project|') -or -not $lines[2].StartsWith('compiler|')) { throw "Compiler input list is malformed: $ListPath" }
    $objPrefix = [IO.Path]::GetFullPath($lines[0].Substring('intermediate|'.Length)).TrimEnd('\') + '\'
    $projectDirectory = $lines[1].Substring('project|'.Length)
    # Csc arguments name files relative to the project directory.
    function Resolve-InputPath([string] $Path) { return [IO.Path]::GetFullPath([IO.Path]::Combine($projectDirectory, $Path.Trim('"'))) }
    function Get-InputName([string] $Path) {
        $full = Resolve-InputPath $Path
        # MSBuild's generated target-framework attribute file lives in the intermediate or the temporary directory.
        if ([IO.Path]::GetFileName($full) -like '*.AssemblyAttributes.cs') { return 'generated:' + [IO.Path]::GetFileName($full) }
        if ($full.StartsWith($objPrefix, [StringComparison]::OrdinalIgnoreCase)) { return 'obj:' + $full.Substring($objPrefix.Length).Replace('\', '/') }
        if ($full.StartsWith($rootPrefix, [StringComparison]::OrdinalIgnoreCase)) { return $full.Substring($rootPrefix.Length).Replace('\', '/') }
        return [IO.Path]::GetFileName($full)
    }
    function Get-InputDigest([string] $Path) { $full = Resolve-InputPath $Path; if (Test-Path -LiteralPath $full -PathType Leaf) { return Get-Sha256 $full } else { return 'absent' } }
    $outputSwitches = @('out', 'pdb', 'refout', 'doc', 'errorlog', 'generatedfilesout')
    $fileSwitches = @('win32res', 'win32icon', 'win32manifest', 'keyfile', 'ruleset', 'appconfig', 'analyzer', 'a', 'additionalfile', 'analyzerconfig', 'embed', 'sourcelink', 'addmodule')
    $resourceSwitches = @('resource', 'res', 'linkresource', 'linkres')
    # The effective compiler binary is an input. Without /noconfig the compiler would also read the csc.rsp beside it,
    # whose contents can name references, resources and options the receipt cannot bind; that mode is refused.
    $compiler = $lines[2].Substring('compiler|'.Length)
    $compilerEntry = [ordered]@{ kind = 'compiler'; name = [IO.Path]::GetFileName($compiler); sha256 = $(if (Test-Path -LiteralPath $compiler -PathType Leaf) { Get-Sha256 $compiler } else { 'absent' }) }
    $cscArgs = @($lines[3..($lines.Count - 1)] | ForEach-Object { if (-not $_.StartsWith('arg|')) { throw "Compiler input list is malformed: $_" }; $_.Substring(4) })
    if ($cscArgs -cnotcontains '/noconfig') { throw 'Unsupported compiler mode: without /noconfig the compiler reads the default csc.rsp beside it, which the build receipt cannot bind; build with NoConfig enabled (the project default).' }
    $implicit = @()
    $entries = foreach ($line in @($cscArgs | ForEach-Object { 'arg|' + $_ })) {
        if (-not $line.StartsWith('arg|')) { throw "Compiler input list is malformed: $line" }
        $arg = $line.Substring(4)
        # Forms whose material files the inventory cannot name are refused, not recorded as text: an explicit response
        # file (its contents can name further sources, resources or references), linked (embedded-interop) references
        # and wildcard source recursion.
        if ($arg.StartsWith('@')) { throw "Unsupported compiler input: the response file $arg can name files the build receipt cannot bind; remove it from the project or local configuration." }
        $m = [regex]::Match($arg, '^[/-]([A-Za-z0-9_+-]+?)(?::(.*))?$')
        if (-not $m.Success) { [ordered]@{ kind = 'compile'; name = Get-InputName $arg; sha256 = Get-InputDigest $arg }; continue }
        $switch = $m.Groups[1].Value.ToLowerInvariant(); $value = $m.Groups[2].Value
        if (@('link', 'l', 'recurse') -ccontains $switch) { throw "Unsupported compiler input: $arg (linked references and wildcard source recursion are not supported by the build receipt); remove it from the project or local configuration." }
        if ($outputSwitches -ccontains $switch) { [ordered]@{ kind = 'output'; name = "/$switch" + ':' + [IO.Path]::GetFileName($value.Trim('"')); sha256 = '' } }
        elseif ($switch -ceq 'reference' -or $switch -ceq 'r') {
            $alias, $path = if ($value -match '^[A-Za-z_][A-Za-z0-9_]*=') { $value -split '=', 2 } else { $null, $value }
            [ordered]@{ kind = 'reference'; name = $(if ($alias) { "$alias=" } else { '' }) + (Get-InputName $path); sha256 = Get-InputDigest $path }
        }
        elseif ($resourceSwitches -ccontains $switch) {
            $path, $rest = $value -split ',', 2
            [ordered]@{ kind = 'file-option'; name = "/$switch" + ':' + (Get-InputName $path) + $(if ($rest) { ',' + $rest } else { '' }); sha256 = Get-InputDigest $path }
        }
        elseif ($fileSwitches -ccontains $switch -and $value) { [ordered]@{ kind = 'file-option'; name = "/$switch" + ':' + (Get-InputName $value); sha256 = Get-InputDigest $value } }
        else { [ordered]@{ kind = 'option'; name = $arg.Replace($objPrefix.TrimEnd('\'), 'obj:'); sha256 = '' } }
    }
    # Argument order is kept: the inventory is the compiler, any implicit response file, then every argument as passed.
    return @(@($compilerEntry) + $implicit + @($entries))
}

# Structural comparison of two compiler inventories: the same number of entries and, entry by entry in order, the same
# kind, name and digest (each a string, compared ordinally). Returns $null when equal, otherwise the first difference.
function Get-CompilerInventoryDifference($Recorded, $Current) {
    $a = @($Recorded); $b = @($Current)
    if ($a.Count -ne $b.Count) { return "entry count $($a.Count) vs $($b.Count)" }
    for ($i = 0; $i -lt $a.Count; $i++) {
        foreach ($field in @('kind', 'name', 'sha256')) {
            $x = if ($a[$i] -is [Collections.IDictionary]) { $a[$i][$field] } else { $a[$i].$field }
            $y = if ($b[$i] -is [Collections.IDictionary]) { $b[$i][$field] } else { $b[$i].$field }
            if (-not ($x -is [string]) -or -not ($y -is [string]) -or -not [string]::Equals($x, $y, [StringComparison]::Ordinal)) { return "entry $i $field" }
        }
    }
    return $null
}

# The bounded local build configuration. The receipt's inventory comes from an MSBuild evaluation that skips the compiler,
# so anything that could act only during a real compile is refused instead of trusted, at Build-Local and at every receipt
# admission (package, qualification):
# - the ignored GamePath.props may only set the three path properties (no conditions, imports, items or targets);
# - no Directory.Build.props/.targets/.rsp above the repository, and none inside it that Git does not track, nor an
#   untracked project .user file;
# - no explicit Import in an applicable Directory.Build.props/.targets or project .user file (the imported file's
#   settings could change compilation without changing a fingerprinted input); an empty ImportGroup imports nothing;
# - no import selector (DirectoryBuildTargetsPath, ImportDirectoryBuildTargets, CustomAfterMicrosoftCommonTargets, ...)
#   assigned in the environment or in the tracked configuration (the applicable Directory.Build files, the project
#   .user file, the project file and the repository's LoaderReferences.targets), because the receipt follows the
#   conventional lookup: a PropertyGroup property of that name, or a -p:/-property: switch in Directory.Build.rsp
#   (mentions in comments, conditions, other values or item metadata set nothing and are allowed);
# - no import in the project file beyond its fixed baseline imports (Microsoft.Common.props, GamePath.props,
#   LoaderReferences.targets, Microsoft.CSharp.targets);
# - no environment variable that selects additional MSBuild imports;
# - no per-user MSBuild ImportBefore/ImportAfter extension file in a location the selected MSBuild toolset imports.
# Machine-wide extension files that ship with the installed MSBuild are part of the toolchain and are not inventoried.
# Response-file reading as MSBuild 17.14 does it (src/MSBuild/XMake.cs GatherResponseFileSwitch,
# GatherCommandLineSwitches, ExtractSwitchParameters, ProcessPropertySwitch; src/Shared/QuotingUtilities.cs), limited to
# what the configuration admission needs: which property names a Directory.Build.rsp assigns, and whether it names a
# further response file. A line is skipped only when its first non-whitespace character is '#'; each other line has its
# environment variables expanded and is split into arguments at unquoted whitespace.
function ConvertFrom-MSBuildQuotedText([string] $Text) {
    # QuotingUtilities.Unquote: removes the quoting double-quotes; returns the text and how many were removed.
    $out = New-Object Text.StringBuilder
    $quoted = $false; $backslashes = 0; $removed = 0
    for ($i = 0; $i -lt $Text.Length; $i++) {
        $c = $Text[$i]
        if ($c -eq '\') { $backslashes++; continue }
        if ($c -eq '"') {
            [void]$out.Append([char]92, [int][Math]::Floor($backslashes / 2))
            if (($backslashes % 2) -eq 0) {
                if ($quoted -and $i -lt ($Text.Length - 1) -and $Text[$i + 1] -eq '"') { [void]$out.Append('"'); $i++ }
                $quoted = -not $quoted; $removed++
            }
            else { [void]$out.Append('"') }
            $backslashes = 0; continue
        }
        [void]$out.Append([char]92, $backslashes); [void]$out.Append($c); $backslashes = 0
    }
    [void]$out.Append([char]92, $backslashes)
    return [pscustomobject]@{ Text = $out.ToString(); Removed = $removed }
}
function Split-MSBuildUnquoted([string] $Text, [char[]] $Separators = @(), [bool] $Unquote = $false) {
    # QuotingUtilities.SplitUnquoted (no split limit, empty pieces discarded): splits at separators (whitespace when none)
    # outside double quotes, keeping the quotes unless $Unquote.
    $pieces = New-Object Collections.Generic.List[string]
    $current = New-Object Text.StringBuilder
    $quoted = $false; $backslashes = 0
    for ($i = 0; $i -lt $Text.Length; $i++) {
        $c = $Text[$i]
        if ($c -eq '\') { [void]$current.Append('\'); $backslashes++; continue }
        if ($c -eq '"') {
            [void]$current.Append('"')
            if (($backslashes % 2) -eq 0) {
                if ($quoted -and $i -lt ($Text.Length - 1) -and $Text[$i + 1] -eq '"') { [void]$current.Append('"'); $i++ }
                $quoted = -not $quoted
            }
            $backslashes = 0; continue
        }
        $isSeparator = if ($Separators.Count -eq 0) { [char]::IsWhiteSpace($c) } else { $Separators -contains $c }
        if (-not $quoted -and $isSeparator) { $pieces.Add($current.ToString()); [void]$current.Clear() } else { [void]$current.Append($c) }
        $backslashes = 0
    }
    $pieces.Add($current.ToString())
    return @($pieces | ForEach-Object { if ($Unquote) { (ConvertFrom-MSBuildQuotedText $_).Text } else { $_ } } | Where-Object { $_.Length -gt 0 })
}
function Get-ResponseFileSwitchFacts([string] $Path) {
    # The property names assigned by -p:/-property: (also with -- or /), and any argument naming a further response file.
    $names = New-Object Collections.Generic.List[string]
    $nested = New-Object Collections.Generic.List[string]
    foreach ($rawLine in @(Get-Content -LiteralPath $Path)) {
        $line = ([string]$rawLine).TrimStart()
        if ($line.StartsWith('#', [StringComparison]::Ordinal)) { continue }
        foreach ($argument in @(Split-MSBuildUnquoted ([Environment]::ExpandEnvironmentVariables($line)))) {
            $unquotedArgument = ConvertFrom-MSBuildQuotedText $argument
            $unquoted = $unquotedArgument.Text
            if ($unquoted.Length -eq 0) { continue }
            if ($unquoted.StartsWith('@', [StringComparison]::Ordinal)) { $nested.Add($unquoted); continue }
            if (-not ($unquoted.StartsWith('-', [StringComparison]::Ordinal) -or $unquoted.StartsWith('/', [StringComparison]::Ordinal))) { continue }
            $parameterIndicator = $unquoted.IndexOf(':')
            if ($parameterIndicator -lt 0) { continue }
            $indicatorLength = if ($unquoted.StartsWith('--', [StringComparison]::Ordinal)) { 2 } else { 1 }
            $switchName = $unquoted.Substring($indicatorLength, $parameterIndicator - $indicatorLength)
            if (@('p', 'property') -notcontains $switchName.ToLowerInvariant()) { continue }
            # ExtractSwitchParameters: quoting that starts in the switch name continues into the parameters.
            $quotedIndicator = $argument.IndexOf(':')
            $nameRemoved = (ConvertFrom-MSBuildQuotedText ($argument.Substring(0, $quotedIndicator))).Removed
            if (($nameRemoved % 2) -eq 0) { $parameters = $argument.Substring($quotedIndicator) }
            else {
                $terminal = $argument.IndexOf('"', $quotedIndicator + 1)
                if (($unquotedArgument.Removed - $nameRemoved) -le 1 -and ($terminal -eq -1 -or $terminal -eq ($argument.Length - 1))) { $parameters = $unquoted.Substring($parameterIndicator) }
                else { $parameters = ':"' + $argument.Substring($quotedIndicator + 1) }
            }
            # The property switch's parameters: split at unquoted ';' and ',' and unquoted; the name precedes the first '='.
            foreach ($assignment in @(Split-MSBuildUnquoted ($parameters.Substring(1)) @([char]';', [char]',') $true)) { $names.Add(($assignment -split '=', 2)[0]) }
        }
    }
    return [pscustomobject]@{ PropertyNames = @($names); NestedResponseFiles = @($nested) }
}

function Assert-LocalBuildConfiguration {
    $root = $script:RepositoryRoot
    $props = Join-Path $root 'GamePath.props'
    if (-not (Test-Path -LiteralPath $props -PathType Leaf)) { throw 'GamePath.props is missing (run scripts\Initialize-GamePath.ps1).' }
    [xml]$xml = Get-Content -LiteralPath $props -Raw
    $project = $xml.DocumentElement
    $allowedNames = @('KingmakerInstallDir', 'KingmakerManagedDir', 'UnityModManagerDir')
    # Conditions may test only $(Configuration) and $(Platform), which the evaluation and the production compile share;
    # values may refer only to the path properties, the configuration and this file's directory. No property functions,
    # item or metadata references.
    $textAllowed = { param([string] $Text, [string[]] $References)
        if ($Text -match '[@%]\(' -or $Text -match '\$\(\s*\[') { return $false }
        foreach ($m in [regex]::Matches($Text, '\$\(([^)]*)\)')) { if ($References -cnotcontains $m.Groups[1].Value.Trim()) { return $false } }
        return $true }
    $conditionOk = { param($Node) foreach ($attribute in @($Node.Attributes)) { if ($attribute.Name -cne 'Condition' -or -not (& $textAllowed $attribute.Value @('Configuration', 'Platform'))) { return $false } }; return $true }
    $groups = @($project.ChildNodes | Where-Object { $_.NodeType -eq 'Element' })
    $ok = $project.LocalName -ceq 'Project' -and @($project.Attributes | Where-Object { @('xmlns', 'ToolsVersion') -cnotcontains $_.Name }).Count -eq 0 -and
        @($groups | Where-Object { $_.LocalName -cne 'PropertyGroup' -or -not (& $conditionOk $_) }).Count -eq 0
    if ($ok) {
        foreach ($property in @($groups | ForEach-Object { @($_.ChildNodes | Where-Object { $_.NodeType -eq 'Element' }) })) {
            if ($allowedNames -cnotcontains $property.LocalName -or -not (& $conditionOk $property) -or @($property.ChildNodes | Where-Object { $_.NodeType -eq 'Element' }).Count -ne 0 -or
                -not (& $textAllowed $property.InnerText ($allowedNames + @('Configuration', 'Platform', 'MSBuildThisFileDirectory')))) { $ok = $false }
        }
    }
    if (-not $ok) { throw "GamePath.props may only set $($allowedNames -join ', ') (conditions on Configuration or Platform only; no imports, items, targets, property functions or other properties)." }
    # Only the configuration MSBuild imports for the production project applies (Get-ApplicableBuildConfiguration). An
    # applicable file outside the checkout, or untracked inside it, is refused; tracked ones are fingerprinted inputs. An
    # explicit Import in one of them is refused: the imported file (for example a parent Directory.Build.targets reached
    # with GetPathOfFileAbove) is not a fingerprinted input, so its settings could change the production compile.
    $selectors = @('DirectoryBuildPropsPath', 'DirectoryBuildTargetsPath', 'ImportDirectoryBuildProps', 'ImportDirectoryBuildTargets',
            'CustomBeforeMicrosoftCommonProps', 'CustomAfterMicrosoftCommonProps', 'CustomBeforeMicrosoftCommonTargets', 'CustomAfterMicrosoftCommonTargets',
            'CustomBeforeMicrosoftCSharpProps', 'CustomAfterMicrosoftCSharpProps', 'CustomBeforeMicrosoftCSharpTargets', 'CustomAfterMicrosoftCSharpTargets',
            'MSBuildUserExtensionsPath', 'ImportUserLocationsByWildcardBeforeMicrosoftCommonProps', 'ImportUserLocationsByWildcardAfterMicrosoftCommonProps',
            'ImportUserLocationsByWildcardBeforeMicrosoftCommonTargets', 'ImportUserLocationsByWildcardAfterMicrosoftCommonTargets')
    $rootPrefix = [IO.Path]::GetFullPath($root).TrimEnd('\') + '\'
    $projectFile = [IO.Path]::GetFullPath((Join-Path $root 'src\KingmakerDiceRoller\KingmakerDiceRoller.csproj'))
    $loaderTargets = [IO.Path]::GetFullPath((Join-Path $root 'LoaderReferences.targets'))
    # The project file's fixed baseline imports; any other import there is a file the receipt does not track.
    $baselineImports = @('$(MSBuildToolsPath)\Microsoft.Common.props', '..\..\GamePath.props', '..\..\LoaderReferences.targets', '$(MSBuildToolsPath)\Microsoft.CSharp.targets')
    foreach ($file in @(@(Get-ApplicableBuildConfiguration) + @($projectFile, $loaderTargets))) {
        if (-not $file.StartsWith($rootPrefix, [StringComparison]::OrdinalIgnoreCase)) { throw "An outside $([IO.Path]::GetFileName($file)) applies to the build: $file" }
        $relative = $file.Substring($rootPrefix.Length).Replace('\', '/')
        $tracked = @(& git -C $root ls-files -- $relative)
        if ($LASTEXITCODE -ne 0) { throw 'git ls-files failed while checking the local build configuration.' }
        if ($tracked.Count -eq 0) { throw "Untracked MSBuild configuration files apply to the build: $relative" }
        if ($file -like '*.rsp') {
            # The property names the response file assigns, read with MSBuild's own argument and quoting rules
            # (Get-ResponseFileSwitchFacts). A further response file could assign more and is not bound by the receipt.
            $facts = Get-ResponseFileSwitchFacts $file
            if ($facts.NestedResponseFiles.Count -ne 0) { throw "Unsupported nested response file $($facts.NestedResponseFiles[0]) in ${relative}: the build receipt cannot bind it; remove it." }
            foreach ($name in $facts.PropertyNames) {
                if ($selectors -contains $name) { throw "Unsupported MSBuild import selector $name in ${relative}: the build receipt follows the conventional lookup; remove it." }
            }
            continue
        }
        [xml]$configuration = Get-Content -LiteralPath $file -Raw
        # A property declaration is an element directly inside a PropertyGroup; item metadata, values, conditions and
        # comments that mention a selector set nothing.
        foreach ($element in @($configuration.SelectNodes("//*[local-name()='PropertyGroup']/*"))) {
            if ($selectors -contains $element.LocalName) { throw "Unsupported MSBuild import selector $($element.LocalName) in ${relative}: the build receipt follows the conventional lookup; remove it." }
        }
        $imports = @($configuration.SelectNodes("//*[local-name()='Import']"))
        # Separator direction is normalized for the comparison (MSBuild accepts either); the four baseline imports only.
        if ($file -eq $projectFile) { $imports = @($imports | Where-Object { $baselineImports -notcontains $_.GetAttribute('Project').Replace('/', '\') }) }
        if ($imports.Count -ne 0) {
            throw "Unsupported MSBuild import in ${relative}: the build receipt cannot bind the imported file ($($imports[0].GetAttribute('Project'))); put the settings in a tracked applicable file itself."
        }
    }
    foreach ($name in $selectors) {
        if (-not [string]::IsNullOrEmpty([Environment]::GetEnvironmentVariable($name))) { throw "The environment variable $name selects MSBuild imports; unset it before building." }
    }
    # Per-user hooks: MSBuild imports $(MSBuildUserExtensionsPath)\$(MSBuildToolsVersion)\{Imports\Microsoft.Common.props,
    # Microsoft.Common.targets, Microsoft.CSharp.targets}\Import{Before,After}\* for the toolset that builds the project.
    # Hooks of other toolset versions are not imported. If the toolset version cannot be read from the selected MSBuild's
    # path, every ImportBefore/ImportAfter file under the per-user folder is refused.
    $userExtensions = Join-Path $env:LOCALAPPDATA 'Microsoft\MSBuild'
    if (Test-Path -LiteralPath $userExtensions -PathType Container) {
        # The selected MSBuild path is normalized first (MSBUILD_EXE_PATH may use forward slashes or relative segments).
        $toolsVersion = if ([IO.Path]::GetFullPath((Resolve-MSBuild)) -match '\\MSBuild\\(Current|\d+\.\d+)\\Bin\\') { $Matches[1] } else { $null }
        # @(...) around the whole if: an if statement that outputs nothing yields $null, not an empty array.
        $hooks = @(if ($toolsVersion) {
            foreach ($hookDirectory in @('Imports\Microsoft.Common.props', 'Microsoft.Common.targets', 'Microsoft.CSharp.targets')) {
                foreach ($when in @('ImportBefore', 'ImportAfter')) {
                    $directory = Join-Path $userExtensions (Join-Path $toolsVersion (Join-Path $hookDirectory $when))
                    if (Test-Path -LiteralPath $directory -PathType Container) { Get-ChildItem -LiteralPath $directory -File -Force }
                }
            }
        } else { Get-ChildItem -LiteralPath $userExtensions -Recurse -File -ErrorAction SilentlyContinue | Where-Object { $_.FullName -match '\\Import(Before|After)\\' } })
        if ($hooks.Count -gt 0) { throw "Per-user MSBuild extension files apply to the build: $(@($hooks | ForEach-Object { $_.FullName }) -join ', ')" }
    }
}

# The configuration MSBuild imports for the production project: for each of Directory.Build.props,
# Directory.Build.targets and Directory.Build.rsp the first file found walking up from the project directory (one further
# up is shadowed; files beside other folders are not imported), and the project's own .csproj.user. Full paths.
function Get-ApplicableBuildConfiguration {
    $projectDirectory = Join-Path $script:RepositoryRoot 'src\KingmakerDiceRoller'
    $applicable = @()
    foreach ($name in @('Directory.Build.props', 'Directory.Build.targets', 'Directory.Build.rsp')) {
        $dir = $projectDirectory
        while ($dir) {
            if (Test-Path -LiteralPath (Join-Path $dir $name) -PathType Leaf) { $applicable += [IO.Path]::GetFullPath((Join-Path $dir $name)); break }
            $parent = Split-Path -Parent $dir
            if (-not $parent -or $parent -eq $dir) { break }
            $dir = $parent
        }
    }
    $userFile = Join-Path $projectDirectory 'KingmakerDiceRoller.csproj.user'
    if (Test-Path -LiteralPath $userFile -PathType Leaf) { $applicable += [IO.Path]::GetFullPath($userFile) }
    return $applicable
}

# Validates the shape of a recorded compiler inventory (as read back from JSON).
function Assert-CompilerInventoryShape($Inventory) {
    $entries = @($Inventory)
    if ($entries.Count -eq 0) { throw 'Build receipt compiler_references is empty; rebuild first.' }
    foreach ($entry in $entries) {
        foreach ($field in @('kind', 'name', 'sha256')) {
            if ($null -eq $entry -or -not $entry.PSObject.Properties[$field] -or -not ($entry.$field -is [string])) { throw "Build receipt compiler input field $field must be a string; rebuild first." }
        }
        if ($entry.name -match '[\x00-\x1f]') { throw "Build receipt compiler input name contains a control character: $($entry.kind); rebuild first." }
        $valid = if ($entry.kind -ceq 'option' -or $entry.kind -ceq 'output') { $entry.sha256 -ceq '' -and $entry.name.Length -gt 0 } else {
            @('reference', 'compile', 'file-option', 'compiler', 'response-file') -ccontains $entry.kind -and ($entry.sha256 -cmatch '^[0-9a-f]{64}$' -or $entry.sha256 -ceq 'absent') }
        if (-not $valid) { throw "Build receipt compiler input is malformed: $($entry.kind) $($entry.name); rebuild first." }
    }
    if (@($entries | Where-Object { $_.kind -ceq 'reference' }).Count -eq 0) { throw 'Build receipt compiler inventory names no references; rebuild first.' }
    # An input the helper cannot identify (a missing compiler, response file, source or file-valued input) refuses reuse.
    if (@($entries | Where-Object { $_.sha256 -ceq 'absent' }).Count -ne 0) { throw "Build receipt compiler inventory has an unidentified input ($(@($entries | Where-Object { $_.sha256 -ceq 'absent' } | ForEach-Object { $_.name }) -join ', ')); rebuild first." }
}

# The compiler inputs for a configuration as MSBuild itself evaluates them: the exact Csc command line from a
# SkipCompilerExecution pass through CoreCompile in a fresh intermediate directory (references, sources including generated
# ones, every option, file-valued inputs by content). Needs the game assemblies and pins, not the runtime loader.
function Get-CompilerReferenceIdentities([string] $Configuration) {
    Assert-LocalBuildConfiguration
    $id = [Guid]::NewGuid().ToString('N')
    $list = Join-Path ([IO.Path]::GetTempPath()) ("kdr-compiler-references-$id.txt")
    $obj = Join-Path ([IO.Path]::GetTempPath()) ("kdr-compiler-references-$id\")
    $project = Join-Path $script:RepositoryRoot 'src\KingmakerDiceRoller\KingmakerDiceRoller.csproj'
    $previous = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    try {
        $output = $null | & (Resolve-MSBuild) $project /nologo /v:q /nr:false '/t:PrepareResources;Compile' /p:SkipCompilerExecution=true `
            "/p:Configuration=$Configuration" /p:Platform=AnyCPU "/p:CompilerReferenceListPath=$list" "/p:IntermediateOutputPath=$obj" "/p:OutputPath=$($obj)bin" 2>&1 | ForEach-Object { "$_" }
        if ($LASTEXITCODE -ne 0 -or -not (Test-Path -LiteralPath $list)) { throw "Could not resolve the compiler references: $($output -join ' ')" }
        return @(ConvertFrom-CompilerInputList $list)
    }
    finally {
        $ErrorActionPreference = $previous
        Remove-Item -LiteralPath $list -Force -ErrorAction SilentlyContinue
        if ([IO.Directory]::Exists($obj)) { [IO.Directory]::Delete($obj, $true) }
    }
}

function Get-BuildInputFingerprint([string] $Configuration = 'Release', $CompilerInputs = $null) {
    $root = $script:RepositoryRoot
    $paths = @('src', 'Directory.Build.props', 'Directory.Build.targets', 'Directory.Build.rsp', '.editorconfig', 'Info.json', 'LoaderReferences.targets',
        'scripts/loader-reference-pins.json', 'scripts/Get-PinnedLoaderReferences.ps1', 'scripts/Build-Local.ps1', 'scripts/Common.ps1')
    $files = @(& git -C $root ls-files --cached --others --exclude-standard -- @paths)
    if ($LASTEXITCODE -ne 0) { throw 'git ls-files failed while fingerprinting build inputs.' }
    # A Directory.Build.props/.targets/.rsp at the root or below src that MSBuild does not import for the production
    # project (beside another folder, or shadowed) is not a build input; the applicable ones stay
    # (Get-ApplicableBuildConfiguration), the same rule as the configuration check.
    $rootPrefix = [IO.Path]::GetFullPath($root).TrimEnd('\') + '\'
    $applicable = @(Get-ApplicableBuildConfiguration | ForEach-Object { if ($_.StartsWith($rootPrefix, [StringComparison]::OrdinalIgnoreCase)) { $_.Substring($rootPrefix.Length).Replace('\', '/') } })
    $files = @($files | Where-Object { $_ -notmatch '^(src/(.+/)?)?Directory\.Build\.(props|targets|rsp)$' -or $applicable -contains $_ })
    $lines = @(foreach ($file in ($files | Sort-Object -Unique)) {
        $full = Join-Path $root $file
        if (Test-Path -LiteralPath $full -PathType Leaf) { $file + "`t" + (Get-Sha256 $full) }
    })
    if ($null -eq $CompilerInputs) { $CompilerInputs = @(Get-CompilerReferenceIdentities $Configuration) }
    foreach ($input in @($CompilerInputs)) { $lines += 'compiler-' + [string]$input.kind + ':' + [string]$input.name + "`t" + [string]$input.sha256 }
    $hasher = [Security.Cryptography.SHA256]::Create()
    try { $digest = $hasher.ComputeHash([Text.Encoding]::UTF8.GetBytes(($lines -join "`n"))) }
    finally { $hasher.Dispose() }
    return ([BitConverter]::ToString($digest)).Replace('-', '').ToLowerInvariant()
}

# Refuse to package or qualify a DLL that is not exactly the output of the current inputs and configuration,
# including an old build whose manifest was merely lowered after compilation.
function Assert-PinnedBuildReceipt($Receipt, [string] $DllPath, [string] $Configuration) {
    foreach ($field in @('dll_sha256', 'loader_references', 'build_inputs_sha256', 'configuration')) {
        if (-not $Receipt.PSObject.Properties[$field]) { throw "Build receipt has no $field; rebuild first." }
    }
    # Types first: an array or Boolean would make PowerShell's comparison operators filter or coerce instead of compare.
    foreach ($field in @('dll_sha256', 'build_inputs_sha256')) {
        if (-not ($Receipt.$field -is [string]) -or $Receipt.$field -cnotmatch '^[0-9a-f]{64}$') { throw "Build receipt $field must be a lowercase SHA-256 string; rebuild first." }
    }
    if (-not ($Receipt.configuration -is [string])) { throw 'Build receipt configuration must be a string; rebuild first.' }
    foreach ($field in @('commit', 'dirty')) { if (-not $Receipt.PSObject.Properties[$field]) { throw "Build receipt has no $field; rebuild first." } }
    if (-not ($Receipt.commit -is [string]) -or $Receipt.commit -cnotmatch '^[0-9a-f]{40}$') { throw 'Build receipt commit must be a 40-character lowercase commit SHA; rebuild first.' }
    if (-not ($Receipt.dirty -is [bool])) { throw 'Build receipt dirty must be a JSON Boolean; rebuild first.' }
    foreach ($entry in @($Receipt.loader_references)) {
        foreach ($field in @('file', 'version', 'assembly_version', 'sha256', 'source')) {
            if ($null -eq $entry -or -not $entry.PSObject.Properties[$field] -or -not ($entry.$field -is [string])) { throw "Build receipt loader reference field $field must be a string; rebuild first." }
        }
    }
    if ($Receipt.dll_sha256 -ne (Get-Sha256 $DllPath)) { throw 'Build receipt does not match the mod DLL; rebuild first.' }
    if ($Configuration -and $Receipt.configuration -cne $Configuration) { throw "Build receipt is for configuration $($Receipt.configuration), not $Configuration; rebuild first." }
    # The receipt's recorded compiler inventory (captured from the production compile) must be well formed and equal the
    # inventory MSBuild evaluates now; the fingerprint is then computed from that same inventory.
    if (-not $Receipt.PSObject.Properties['compiler_references']) { throw 'Build receipt has no compiler_references; rebuild first.' }
    Assert-CompilerInventoryShape $Receipt.compiler_references
    $currentInventory = @(Get-CompilerReferenceIdentities $Receipt.configuration)
    $difference = Get-CompilerInventoryDifference $Receipt.compiler_references $currentInventory
    if ($difference) {
        throw "Build inputs changed since this DLL was built: the receipt compiler inputs differ from the inputs MSBuild resolves now ($difference; references, sources or compiler settings); rebuild first."
    }
    if ($Receipt.build_inputs_sha256 -cne (Get-BuildInputFingerprint $Receipt.configuration $currentInventory)) { throw 'Build inputs changed since this DLL was built (source, manifest or loader pins); rebuild first.' }
    $pins = @(Get-PinnedReferenceDefinitions)
    $recorded = @($Receipt.loader_references)
    if ($recorded.Count -ne $pins.Count) { throw 'Build receipt has an unexpected loader reference count; rebuild first.' }
    foreach ($pin in $pins) {
        $matches = @($recorded | Where-Object { $_.file -ceq $pin.File })
        if ($matches.Count -ne 1 -or $matches[0].version -cne $pin.Version -or $matches[0].assembly_version -cne $pin.Version -or
            $matches[0].sha256 -cne $pin.Sha256 -or $matches[0].source -cne $pin.Url) {
            throw "Build receipt does not match pinned reference $($pin.File); rebuild first."
        }
    }
}

# Reads a DLL's MVID and AssemblyRef table without executing it. Windows PowerShell has no metadata reader, and a
# reflection-only load refuses a second assembly with the same identity in one process (validating two packages
# in one host would fail), so it reads the metadata in a separate process that loads nothing else.
function Get-AssemblyMetadataIdentity([byte[]] $Bytes) {
    if ($PSVersionTable.PSEdition -eq 'Core') {
        Add-Type -AssemblyName System.Reflection.Metadata
        $stream = [IO.MemoryStream]::new($Bytes, $false)
        $reader = [System.Reflection.PortableExecutable.PEReader]::new($stream)
        try {
            $metadata = [System.Reflection.Metadata.PEReaderExtensions]::GetMetadataReader($reader)
            return [pscustomobject]@{
                Mvid = $metadata.GetGuid($metadata.GetModuleDefinition().Mvid).ToString()
                References = @($metadata.AssemblyReferences | ForEach-Object {
                    $reference = $metadata.GetAssemblyReference($_)
                    [pscustomobject]@{ Name = $metadata.GetString($reference.Name); Version = $reference.Version.ToString() }
                })
            }
        }
        finally { $reader.Dispose(); $stream.Dispose() }
    }
    $temporary = Join-Path ([IO.Path]::GetTempPath()) ('kdr-metadata-' + [Guid]::NewGuid().ToString('N') + '.dll')
    [IO.File]::WriteAllBytes($temporary, $Bytes)
    try {
        $script = "`$a = [Reflection.Assembly]::ReflectionOnlyLoad([IO.File]::ReadAllBytes('" + $temporary.Replace("'", "''") + "')); " +
            "[pscustomobject]@{ Mvid = `$a.ManifestModule.ModuleVersionId.ToString(); References = @(`$a.GetReferencedAssemblies() | " +
            "ForEach-Object { [pscustomobject]@{ Name = `$_.Name; Version = `$_.Version.ToString() } }) } | ConvertTo-Json -Compress -Depth 4"
        $encoded = [Convert]::ToBase64String([Text.Encoding]::Unicode.GetBytes($script))
        $previous = $ErrorActionPreference
        $ErrorActionPreference = 'Continue'
        try { $json = & powershell.exe -NoLogo -NoProfile -NonInteractive -EncodedCommand $encoded 2>&1 | Out-String }
        finally { $ErrorActionPreference = $previous }
        if ($LASTEXITCODE -ne 0) { throw "Packaged DLL metadata could not be read: $json" }
        $identity = $json | ConvertFrom-Json
        return [pscustomobject]@{ Mvid = [string]$identity.Mvid; References = @($identity.References) }
    }
    finally { Remove-Item -LiteralPath $temporary -Force -ErrorAction SilentlyContinue }
}

# Inspect packaged metadata as well as the build receipt: Install.ps1 also accepts
# a supplied ZIP and therefore calls Validate-Package.ps1 without Package.ps1.
function Assert-PinnedAssemblyReferences([byte[]] $Bytes) {
    $identity = Get-AssemblyMetadataIdentity $Bytes
    $references = @($identity.References)
    $observed = @()
    foreach ($pin in (Get-PinnedReferenceDefinitions)) {
        $matches = @($references | Where-Object { $_.Name -ceq $pin.Name })
        if ($matches.Count -ne 1 -or [string]$matches[0].Version -cne $pin.Version) {
            throw "Packaged DLL must reference pinned $($pin.Name) $($pin.Version); lowering Info.json alone is not sufficient."
        }
        $observed += [ordered]@{ name = $pin.Name; version = [string]$matches[0].Version }
    }
    # Returned for the validation report, so it names the exact DLL whose references were inspected.
    return [pscustomobject]@{ Mvid = $identity.Mvid; LoaderReferences = $observed }
}

# Validates a private copy of a package and extracts that same copy, so the bytes installed are the bytes that
# passed validation even if the original file changes afterwards. Returns the validated packaged DLL SHA-256.
# The private copy, its validation report and their cleanup are preflight work, not installation: they run under
# Install.ps1 -WhatIf as well (only live Mods changes stay behind ShouldProcess).
function Expand-ValidatedPackage([string] $PackagePath, [string] $Destination) {
    $WhatIfPreference = $false
    $private = Join-Path ([IO.Path]::GetTempPath()) ('KingmakerDiceRoller-package-' + [Guid]::NewGuid().ToString('N'))
    [void][IO.Directory]::CreateDirectory($private)
    try {
        $copy = Join-Path $private 'package.zip'
        [IO.File]::Copy($PackagePath, $copy)
        $report = Join-Path $private 'validation.json'
        & (Join-Path $PSScriptRoot 'Validate-Package.ps1') -PackagePath $copy -ReportPath $report
        $validated = Get-Content -LiteralPath $report -Raw | ConvertFrom-Json
        Add-Type -AssemblyName System.IO.Compression.FileSystem
        [IO.Compression.ZipFile]::ExtractToDirectory($copy, $Destination)
        $dll = Join-Path $Destination 'KingmakerDiceRoller\KingmakerDiceRoller.dll'
        Assert-FileExists $dll 'Extracted mod DLL'
        if ((Get-Sha256 $dll) -cne [string]$validated.dll_sha256) { throw 'Extracted DLL does not match the validated package.' }
        return [string]$validated.dll_sha256
    }
    finally { if ([IO.Directory]::Exists($private)) { [IO.Directory]::Delete($private, $true) } }
}
