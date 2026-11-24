[CmdletBinding()]
param(
    # Optional local copies of the two downloaded fixtures; otherwise they are downloaded. Both are hash-checked and
    # only ever read: the suite works on private copies.
    [string] $LegacyReleaseZip,
    [string] $WrongLoaderPackage,
    [string] $ReportPath
)
# Failure-boundary tests for the pinned UMM 0.31.1 compile baseline, run on the committed HEAD in a temporary clone
# whose path contains a space. Every build, package and validation happens in that clone or in its own PowerShell /
# MSBuild process, so this repository's artifacts (including any package already in artifacts\packages) and the
# owner's installation are never written. Uses real files, archives, MSBuild and this repository's own scripts.
. (Join-Path $PSScriptRoot 'Common.ps1')
$source = Get-RepositoryRoot
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem

# Genuine published release, compiled against UnityModManager 0.33.0.0 (its manifest says 0.32.0).
$legacyUrl = 'https://github.com/howardreith/KingmakerDiceRoller/releases/download/v0.2.0/KingmakerDiceRoller-0.2.0.zip'
$legacySha256 = '43f99fabcdd31192ecd5deb46533c2004fa64b59a432c4b094b85290f6dadaad'
# Known distinct loader fixture (never the owner's install): the author's UnityModManager 0.32.4 NuGet package.
$wrongLoaderUrl = 'https://api.nuget.org/v3-flatcontainer/unitymodmanager/0.32.4/unitymodmanager.0.32.4.nupkg'
$wrongLoaderPackageSha256 = 'a8438e921ca7b0e5805f5a31c196ae65c90f236543885bea0146457d082c9ec9'
$wrongLoaderDllSha256 = '1387468bc3af41c50fe51859a3bb7af4922891aa8f13a6187e7a348ceaabfd88'
$modFolder = 'KingmakerDiceRoller'
$Configuration = 'Release'

$commit = (& git -C $source rev-parse HEAD).Trim()
if ([bool](& git -C $source status --porcelain)) { Write-Host "NOTE: uncommitted changes are not tested; the suite tests committed HEAD $commit." }
$sourcePackagesBefore = @(Get-ChildItem -LiteralPath (Join-Path $source 'artifacts\packages') -File -ErrorAction SilentlyContinue | ForEach-Object { $_.Name + '=' + (Get-Sha256 $_.FullName) })
$temp = Join-Path ([IO.Path]::GetTempPath()) ('kdr-loader-baseline-' + [Guid]::NewGuid().ToString('N'))
$base = Join-Path $temp 'clone with space'
$inputs = Join-Path $temp 'inputs'
New-Item -ItemType Directory -Path $inputs -Force | Out-Null
$cases = [Collections.Generic.List[object]]::new()
$msbuild = Resolve-MSBuild
$config = Get-KingmakerConfiguration

# Each check runs in a fresh Windows PowerShell process with an empty, closed stdin. Every output record is turned
# into text with its own ToString(), so stderr messages are never re-wrapped at console width by Out-String.
function ConvertTo-PlainText($Records) { return (@($Records | ForEach-Object { "$_" }) -join "`n") }
function Invoke-Isolated([string[]] $Arguments) {
    $previous = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    try { $records = $null | & powershell.exe -NoLogo -NoProfile -NonInteractive -ExecutionPolicy Bypass @Arguments 2>&1 }
    finally { $ErrorActionPreference = $previous }
    return [pscustomobject]@{ ExitCode = $LASTEXITCODE; Output = (ConvertTo-PlainText $records) }
}
function Invoke-Script([string] $Text) {
    $wrapped = "try { $Text } catch { [Console]::Out.WriteLine('ERROR: ' + `$_.Exception.Message); exit 1 }"
    return Invoke-Isolated @('-EncodedCommand', [Convert]::ToBase64String([Text.Encoding]::Unicode.GetBytes($wrapped)))
}
function Invoke-MSBuildIsolated([string[]] $Arguments) {
    $previous = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    try { $output = ConvertTo-PlainText ($null | & $msbuild /nologo /v:m /nr:false @Arguments 2>&1) }
    finally { $ErrorActionPreference = $previous }
    return [pscustomobject]@{ ExitCode = $LASTEXITCODE; Output = $output }
}
function Add-Case([string] $Name, [bool] $Passed, [string] $Detail) {
    $cases.Add([ordered]@{ name = $Name; passed = $Passed; detail = $Detail })
    if ($Passed) { Write-Host "PASS $Name :: $Detail" } else { Write-Host "FAIL $Name :: $Detail" }
}
function Get-Reason([string] $Output) {
    $line = @($Output -split "`r?`n" | Where-Object { $_ -match '^ERROR: |: error ' } | Select-Object -First 1)
    if (-not $line) { $line = @($Output -split "`r?`n" | Where-Object { $_ -match '(?i)must|mismatch|missing|does not|unexpected|exactly|changed|contains|rebuild|refus|not valid' -and $_ -notmatch '^PASS ' } | Select-Object -First 1) }
    if (-not $line) { $line = @($Output -split "`r?`n" | Where-Object { $_ -match '(?i)passed|Package:|SHA-256' } | Select-Object -First 1) }
    if ($line) { return $line[0].Trim() } else { return ($Output.Trim() -split "`r?`n" | Select-Object -Last 1) }
}
function Expect-Rejected([string] $Name, $Result, [string] $Pattern) {
    $ok = $Result.ExitCode -ne 0 -and ($Result.Output -replace '\s+', ' ') -match $Pattern
    $detail = if ($ok) { 'rejected: ' + (Get-Reason $Result.Output) } else { "exit $($Result.ExitCode); expected /$Pattern/; output: $($Result.Output.Trim())" }
    Add-Case $Name $ok $detail
}
function Expect-Accepted([string] $Name, $Result, [string] $Pattern) {
    $ok = $Result.ExitCode -eq 0 -and (-not $Pattern -or ($Result.Output -replace '\s+', ' ') -match $Pattern)
    $detail = if ($ok -and $Pattern) { 'accepted: ' + [regex]::Match($Result.Output, $Pattern).Value } elseif ($ok) { 'accepted' } else { "exit $($Result.ExitCode); output: $($Result.Output.Trim())" }
    Add-Case $Name $ok $detail
}
function Copy-Pins([string] $From, [string] $Destination) {
    New-Item -ItemType Directory -Force -Path $Destination | Out-Null
    foreach ($file in @('UnityModManager.dll', '0Harmony12.dll')) { Copy-Item -LiteralPath (Join-Path $From $file) -Destination $Destination }
}
function Edit-Zip([string] $From, [string] $Destination, [scriptblock] $Edit) {
    Copy-Item -LiteralPath $From -Destination $Destination
    $archive = [IO.Compression.ZipFile]::Open($Destination, [IO.Compression.ZipArchiveMode]::Update)
    try { & $Edit $archive } finally { $archive.Dispose() }
}
function Set-ZipEntryText($Archive, [string] $Name, [string] $Text) {
    $Archive.GetEntry($Name).Delete()
    $writer = [IO.StreamWriter]::new($Archive.CreateEntry($Name).Open(), [Text.UTF8Encoding]::new($false))
    try { $writer.Write($Text) } finally { $writer.Dispose() }
}
function Get-ZipEntryText($Archive, [string] $Name) {
    $reader = [IO.StreamReader]::new($Archive.GetEntry($Name).Open(), [Text.Encoding]::UTF8, $true)
    try { return $reader.ReadToEnd() } finally { $reader.Dispose() }
}
function Set-ZipEntryFile($Archive, [string] $Name, [string] $Path) {
    $existing = $Archive.GetEntry($Name)
    if ($existing) { $existing.Delete() }
    [IO.Compression.ZipFileExtensions]::CreateEntryFromFile($Archive, $Path, $Name) | Out-Null
}
function Get-CompilerReferences([string] $Output, [string] $File) {
    $line = @($Output -split "`r?`n" | Where-Object { $_ -match 'csc\.exe' }) -join "`n"
    return @([regex]::Matches($line, '/reference:(?:"([^"]+)"|(\S+))') | ForEach-Object {
        if ($_.Groups[1].Success) { $_.Groups[1].Value } else { $_.Groups[2].Value } } | Where-Object { [IO.Path]::GetFileName($_) -ieq $File })
}
function Edit-Json([string] $Path, [scriptblock] $Edit) {
    $j = Get-Content -LiteralPath $Path -Raw | ConvertFrom-Json
    & $Edit $j
    $j | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $Path -Encoding UTF8
}
function Quote([string] $Value) { return "'" + $Value.Replace("'", "''") + "'" }

try {
    # Private, verified inputs (the originals are only read).
    $legacyZip = Join-Path $inputs 'legacy-v0.2.0.zip'
    if ($LegacyReleaseZip) { Copy-Item -LiteralPath $LegacyReleaseZip -Destination $legacyZip } else { Invoke-WebRequest -Uri $legacyUrl -OutFile $legacyZip -UseBasicParsing }
    if ((Get-Sha256 $legacyZip) -ne $legacySha256) { throw 'Legacy release ZIP hash mismatch.' }
    $legacyOriginalSha = if ($LegacyReleaseZip) { Get-Sha256 $LegacyReleaseZip } else { $null }
    $wrongPackage = Join-Path $inputs 'unitymodmanager.0.32.4.nupkg'
    if ($WrongLoaderPackage) { Copy-Item -LiteralPath $WrongLoaderPackage -Destination $wrongPackage } else { Invoke-WebRequest -Uri $wrongLoaderUrl -OutFile $wrongPackage -UseBasicParsing }
    if ((Get-Sha256 $wrongPackage) -ne $wrongLoaderPackageSha256) { throw 'UnityModManager 0.32.4 package hash mismatch.' }
    $wrongDir = Join-Path $inputs 'umm-0.32.4'
    New-Item -ItemType Directory -Path $wrongDir | Out-Null
    $wrongUmm = Join-Path $wrongDir 'UnityModManager.dll'
    $archive = [IO.Compression.ZipFile]::OpenRead($wrongPackage)
    try { [IO.Compression.ZipFileExtensions]::ExtractToFile($archive.GetEntry('lib/net35/UnityModManager.dll'), $wrongUmm) }
    finally { $archive.Dispose() }
    if ((Get-Sha256 $wrongUmm) -ne $wrongLoaderDllSha256) { throw 'UnityModManager 0.32.4 DLL hash mismatch.' }
    $legacyDll = Join-Path $inputs 'legacy-v0.2.0.dll'
    $archive = [IO.Compression.ZipFile]::OpenRead($legacyZip)
    try { [IO.Compression.ZipFileExtensions]::ExtractToFile($archive.GetEntry("$modFolder/KingmakerDiceRoller.dll"), $legacyDll) }
    finally { $archive.Dispose() }
    $installedUmm = Join-Path $config.UnityModManagerDir 'UnityModManager.dll'
    $ownerMeasurement = [ordered]@{ version = [Reflection.AssemblyName]::GetAssemblyName($installedUmm).Version.ToString(); sha256 = Get-Sha256 $installedUmm }
    Write-Host "Owner-installed UnityModManager (measured only, not used as a fixture): $($ownerMeasurement.version) $($ownerMeasurement.sha256)"

    # Temporary clone of the committed HEAD under a path with a space; seed its ignored cache and local game paths.
    & git clone -q --no-hardlinks $source $base
    if ($LASTEXITCODE -ne 0) { throw 'git clone failed.' }
    & git -C $base checkout -q --detach $commit
    # Disposable instrumented script copies created by some cases are local to the clone and never part of its status.
    Add-Content -LiteralPath (Join-Path $base '.git/info/exclude') -Value 'scripts/*-instrumented.ps1'
    Copy-Item -LiteralPath (Join-Path $source 'GamePath.props') -Destination $base
    $references = Join-Path $base 'artifacts\references'
    Copy-Pins (Get-PinnedReferenceDirectory) $references
    $project = Join-Path $base 'src\KingmakerDiceRoller\KingmakerDiceRoller.csproj'
    $dll = Join-Path $base "artifacts\bin\$Configuration\KingmakerDiceRoller\KingmakerDiceRoller.dll"
    $receiptPath = Join-Path $base 'artifacts\build-provenance.json'
    $build = Invoke-Isolated @('-File', (Join-Path $base 'scripts\Build-Local.ps1'))
    if ($build.ExitCode -ne 0) { throw "Clone build failed: $($build.Output)" }
    $firstDllSha = Get-Sha256 $dll
    $firstReceipt = Join-Path $inputs 'first-build-provenance.json'
    Copy-Item -LiteralPath $receiptPath -Destination $firstReceipt
    $receipt = Get-Content -LiteralPath $receiptPath -Raw | ConvertFrom-Json
    Add-Case 'clone-build-from-detached-commit' ($receipt.commit -eq $commit -and -not $receipt.dirty -and $receipt.dll_sha256 -eq $firstDllSha) "receipt commit $($receipt.commit) dirty $($receipt.dirty) dll $firstDllSha inputs $($receipt.build_inputs_sha256)"
    $verifier = Join-Path $base 'scripts\Get-PinnedLoaderReferences.ps1'
    $pinnedUmm = [IO.Path]::GetFullPath((Join-Path $references 'UnityModManager.dll'))
    $pinnedHarmony = [IO.Path]::GetFullPath((Join-Path $references '0Harmony12.dll'))

    # 1. Reference cache: genuine bytes pass; corrupt, missing and distinct-loader bytes fail clearly.
    Expect-Accepted 'cache-genuine-verified' (Invoke-Isolated @('-File', $verifier, '-VerifyOnly', '-ReferenceDirectory', $references)) ''
    $corrupt = Join-Path $temp 'cache corrupt'
    Copy-Pins $references $corrupt
    $bytes = [IO.File]::ReadAllBytes((Join-Path $corrupt 'UnityModManager.dll'))
    $bytes[$bytes.Length - 1] = $bytes[$bytes.Length - 1] -bxor 0xFF
    [IO.File]::WriteAllBytes((Join-Path $corrupt 'UnityModManager.dll'), $bytes)
    Expect-Rejected 'cache-corrupt-rejected' (Invoke-Isolated @('-File', $verifier, '-VerifyOnly', '-ReferenceDirectory', $corrupt)) 'hash mismatch'
    $missing = Join-Path $temp 'cache missing'
    Copy-Pins $references $missing
    Remove-Item -LiteralPath (Join-Path $missing 'UnityModManager.dll')
    Expect-Rejected 'cache-missing-rejected' (Invoke-Isolated @('-File', $verifier, '-VerifyOnly', '-ReferenceDirectory', $missing)) 'reference missing'
    $wrongCache = Join-Path $temp 'cache wrong loader'
    Copy-Pins $references $wrongCache
    Copy-Item -LiteralPath $wrongUmm -Destination $wrongCache -Force
    Expect-Rejected 'cache-umm-0.32.4-fixture-rejected' (Invoke-Isolated @('-File', $verifier, '-VerifyOnly', '-ReferenceDirectory', $wrongCache)) 'hash mismatch'

    # 2. Pin definition and manifest contract, on copies of the clone's real scripts.
    function New-ManifestFixture([string] $Name, [scriptblock] $Edit) {
        $fixture = Join-Path $temp $Name
        New-Item -ItemType Directory -Force -Path (Join-Path $fixture 'scripts') | Out-Null
        Copy-Item -LiteralPath $verifier -Destination (Join-Path $fixture 'scripts')
        Copy-Item -LiteralPath (Join-Path $base 'scripts\loader-reference-pins.json') -Destination (Join-Path $fixture 'scripts')
        Copy-Item -LiteralPath (Join-Path $base 'Info.json') -Destination $fixture
        & $Edit $fixture
        return (Join-Path $fixture 'scripts\Get-PinnedLoaderReferences.ps1')
    }
    $f = New-ManifestFixture 'manifest control' { param($d) }
    Expect-Accepted 'manifest-control-accepted' (Invoke-Isolated @('-File', $f, '-VerifyOnly', '-ReferenceDirectory', $references)) ''
    $f = New-ManifestFixture 'pin harmony dropped' { param($d) Edit-Json (Join-Path $d 'scripts\loader-reference-pins.json') { param($j) $j.references = @($j.references | Where-Object Name -ne '0Harmony12') } }
    Expect-Rejected 'pin-definition-without-harmony12-rejected' (Invoke-Isolated @('-File', $f, '-VerifyOnly', '-ReferenceDirectory', $references)) 'must define exactly'
    $f = New-ManifestFixture 'pin harmony renamed' { param($d) Edit-Json (Join-Path $d 'scripts\loader-reference-pins.json') { param($j) @($j.references | Where-Object Name -eq '0Harmony12')[0].Name = 'Harmony12' } }
    Expect-Rejected 'pin-definition-misnamed-harmony12-rejected' (Invoke-Isolated @('-File', $f, '-VerifyOnly', '-ReferenceDirectory', $references)) 'must define 0Harmony12 exactly once'
    $f = New-ManifestFixture 'manifest raised' { param($d) Edit-Json (Join-Path $d 'Info.json') { param($j) $j.ManagerVersion = '0.33.0' } }
    Expect-Rejected 'manifest-above-compiler-reference-rejected' (Invoke-Isolated @('-File', $f, '-VerifyOnly', '-ReferenceDirectory', $references)) 'ManagerVersion must equal'
    $f = New-ManifestFixture 'manifest array' { param($d) Edit-Json (Join-Path $d 'Info.json') { param($j) $j.ManagerVersion = @('0.31.1') } }
    Expect-Rejected 'manifest-array-version-rejected' (Invoke-Isolated @('-File', $f, '-VerifyOnly', '-ReferenceDirectory', $references)) 'ManagerVersion must equal'
    $f = New-ManifestFixture 'pin raised' { param($d) Edit-Json (Join-Path $d 'scripts\loader-reference-pins.json') { param($j) @($j.references | Where-Object Name -eq 'UnityModManager')[0].Version = '0.33.0.0' } }
    Expect-Rejected 'compiler-pin-above-minimum-rejected' (Invoke-Isolated @('-File', $f, '-VerifyOnly', '-ReferenceDirectory', $references)) 'must equal the declared minimum'

    # 3. Direct MSBuild of the production project: corrupt cache refused; genuine cache compiles with exactly the
    #    pinned files as csc inputs (the clone path contains a space, so csc quotes them).
    $obj = Join-Path $temp 'direct-obj\'
    $out = Join-Path $temp 'direct-bin\'
    Expect-Rejected 'msbuild-corrupt-cache-rejected' (Invoke-MSBuildIsolated @($project, '/t:Build', "/p:Configuration=$Configuration",
        "/p:PinnedLoaderReferenceDir=$corrupt", "/p:IntermediateOutputPath=$obj", "/p:OutputPath=$out")) 'hash mismatch'
    $direct = Invoke-MSBuildIsolated @($project, '/t:Rebuild', '/v:n', "/p:Configuration=$Configuration", "/p:IntermediateOutputPath=$obj", "/p:OutputPath=$out")
    $umm = @(Get-CompilerReferences $direct.Output 'UnityModManager.dll')
    $h12 = @(Get-CompilerReferences $direct.Output '0Harmony12.dll')
    $ok = $direct.ExitCode -eq 0 -and $umm.Count -eq 1 -and $umm[0] -ieq $pinnedUmm -and $h12.Count -eq 1 -and $h12[0] -ieq $pinnedHarmony
    Add-Case 'msbuild-direct-compiles-against-pinned-inputs' $ok ("exit {0}; csc UnityModManager: {1}; csc 0Harmony12: {2}" -f $direct.ExitCode, ($umm -join ' | '), ($h12 -join ' | '))

    # 4. Incremental (IDE-style) builds: after an up-to-date build, changing only the manifest or a pin is refused by
    #    an ordinary incremental Build; restoring the genuine inputs is accepted again.
    $iobj = Join-Path $temp 'incremental-obj\'
    $iout = Join-Path $temp 'incremental-bin\'
    $inc = @($project, '/t:Build', "/p:Configuration=$Configuration", "/p:IntermediateOutputPath=$iobj", "/p:OutputPath=$iout")
    Expect-Accepted 'incremental-build-genuine-accepted' (Invoke-MSBuildIsolated $inc) ''
    Expect-Accepted 'incremental-build-up-to-date-accepted' (Invoke-MSBuildIsolated $inc) ''
    Edit-Json (Join-Path $base 'Info.json') { param($j) $j.ManagerVersion = '0.33.0' }
    Expect-Rejected 'incremental-build-after-manifest-only-change-rejected' (Invoke-MSBuildIsolated $inc) 'ManagerVersion must equal'
    & git -C $base checkout -q -- Info.json
    Edit-Json (Join-Path $base 'scripts\loader-reference-pins.json') { param($j) @($j.references | Where-Object Name -eq '0Harmony12')[0].Sha256 = ('0' * 64) }
    Expect-Rejected 'incremental-build-after-pin-only-change-rejected' (Invoke-MSBuildIsolated $inc) 'hash mismatch'
    & git -C $base checkout -q -- scripts/loader-reference-pins.json
    Expect-Accepted 'incremental-build-after-restore-accepted' (Invoke-MSBuildIsolated $inc) ''

    # 5. Resolved-input guard: only the canonical verified files qualify, by path and by assembly identity.
    $stray = Join-Path $temp 'stray copy'
    Copy-Pins $references $stray
    Copy-Item -LiteralPath $pinnedUmm -Destination (Join-Path $references 'UnityModManager.exe')
    Copy-Item -LiteralPath $pinnedUmm -Destination (Join-Path $references 'umm-alias.dll')
    $targets = Join-Path $base 'LoaderReferences.targets'
    function New-ProbeProject([string] $Name, [string] $UmmPath) {
        $path = Join-Path $temp "probe $Name.csproj"
        $text = @"
<Project ToolsVersion="15.0" DefaultTargets="Build" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
  <Import Project="`$(MSBuildExtensionsPath)\`$(MSBuildToolsVersion)\Microsoft.Common.props" Condition="Exists('`$(MSBuildExtensionsPath)\`$(MSBuildToolsVersion)\Microsoft.Common.props')" />
  <PropertyGroup>
    <TargetFrameworkVersion>v4.7.2</TargetFrameworkVersion>
    <OutputType>Library</OutputType>
    <OutputPath>$temp\probe bin\$Name\</OutputPath>
    <IntermediateOutputPath>$temp\probe obj\$Name\</IntermediateOutputPath>
    <ResolveAssemblyReferenceIgnoreTargetFrameworkAttributeVersionMismatch>true</ResolveAssemblyReferenceIgnoreTargetFrameworkAttributeVersionMismatch>
  </PropertyGroup>
  <Import Project="$targets" />
  <ItemGroup>
    <Reference Include="UnityModManager"><HintPath>$UmmPath</HintPath><Private>False</Private></Reference>
    <Reference Include="0Harmony12"><HintPath>$pinnedHarmony</HintPath><Private>False</Private></Reference>
  </ItemGroup>
  <Import Project="`$(MSBuildToolsPath)\Microsoft.CSharp.targets" />
</Project>
"@
        [IO.File]::WriteAllText($path, $text)
        return $path
    }
    Expect-Accepted 'resolved-input-pinned-accepted' (Invoke-MSBuildIsolated @((New-ProbeProject 'pinned' $pinnedUmm), '/t:ResolveReferences;FindReferenceAssembliesForReferences')) ''
    Expect-Rejected 'resolved-input-identical-copy-elsewhere-rejected' (Invoke-MSBuildIsolated @((New-ProbeProject 'stray' (Join-Path $stray 'UnityModManager.dll')), '/t:ResolveReferences;FindReferenceAssembliesForReferences')) 'must be the verified files'
    Expect-Rejected 'resolved-input-same-stem-file-in-pinned-dir-rejected' (Invoke-MSBuildIsolated @((New-ProbeProject 'samestem' (Join-Path $references 'UnityModManager.exe')), '/t:ResolveReferences;FindReferenceAssembliesForReferences')) 'must be the verified files'
    Expect-Rejected 'resolved-input-renamed-loader-in-pinned-dir-rejected' (Invoke-MSBuildIsolated @((New-ProbeProject 'alias' (Join-Path $references 'umm-alias.dll')), '/t:ResolveReferences;FindReferenceAssembliesForReferences')) 'must be the verified files'
    Expect-Rejected 'resolved-input-umm-0.32.4-fixture-rejected' (Invoke-MSBuildIsolated @((New-ProbeProject 'wrong' $wrongUmm), '/t:ResolveReferences;FindReferenceAssembliesForReferences')) 'must be the verified files'
    # A reference assembly substituted after resolution: ReferencePath stays valid, the Csc input does not.
    $substituted = (New-ProbeProject 'substituted' $pinnedUmm)
    $substitution = @"
  <Target Name="SubstituteLoaderReferenceAssembly" AfterTargets="ResolveAssemblyReferences" BeforeTargets="FindReferenceAssembliesForReferences">
    <ItemGroup>
      <ReferencePath Condition="'%(Filename)' == 'UnityModManager'"><ReferenceAssembly>$(Join-Path $stray 'UnityModManager.dll')</ReferenceAssembly></ReferencePath>
    </ItemGroup>
  </Target>
</Project>
"@
    [IO.File]::WriteAllText($substituted, ([IO.File]::ReadAllText($substituted).Replace('</Project>', $substitution)))
    Expect-Rejected 'resolved-input-substituted-reference-assembly-rejected' (Invoke-MSBuildIsolated @($substituted, '/t:ResolveReferences;FindReferenceAssembliesForReferences')) 'must be the verified files'
    Remove-Item -LiteralPath (Join-Path $references 'UnityModManager.exe'), (Join-Path $references 'umm-alias.dll')

    # 6. Build receipts bind the DLL bytes, configuration, material inputs and pinned identities; packaging and
    #    qualification refuse to reuse a DLL after any material input changes, and accept a matching rebuild.
    $common = Join-Path $base 'scripts\Common.ps1'
    function Test-Receipt([string] $ReceiptFile, [string] $DllFile, [string] $Config) {
        return Invoke-Script ". $(Quote $common); Assert-PinnedBuildReceipt (Get-Content -LiteralPath $(Quote $ReceiptFile) -Raw | ConvertFrom-Json) $(Quote $DllFile) $(Quote $Config)"
    }
    $package = Join-Path $base 'scripts\Package.ps1'
    $marker = 'src\KingmakerDiceRoller\Domain\DomainAssemblyMarker.cs'
    Expect-Accepted 'receipt-matching-build-accepted' (Test-Receipt $receiptPath $dll 'Release') ''
    Expect-Rejected 'receipt-other-configuration-rejected' (Test-Receipt $receiptPath $dll 'Debug') 'not Debug'
    foreach ($mutation in @(
        @{ Name = 'production-source'; File = $marker; Text = "`r`n// loader-baseline test edit`r`n" },
        @{ Name = 'reference-helper'; File = 'scripts\Get-PinnedLoaderReferences.ps1'; Text = "`r`n# loader-baseline test edit`r`n" },
        @{ Name = 'msbuild-guard'; File = 'LoaderReferences.targets'; Text = "`r`n<!-- loader-baseline test edit -->`r`n" },
        @{ Name = 'project-file'; File = 'src\KingmakerDiceRoller\KingmakerDiceRoller.csproj'; Text = "`r`n<!-- loader-baseline test edit -->`r`n" })) {
        [IO.File]::AppendAllText((Join-Path $base $mutation.File), $mutation.Text)
        Expect-Rejected "package-reuse-after-$($mutation.Name)-change-rejected" (Invoke-Isolated @('-File', $package)) 'Build inputs changed'
        & git -C $base checkout -q -- ($mutation.File.Replace('\', '/'))
    }
    [IO.File]::AppendAllText((Join-Path $base $marker), "`r`n// loader-baseline test edit`r`n")
    Expect-Rejected 'qualify-package-reuse-after-source-change-rejected' (Invoke-Isolated @('-File', (Join-Path $base 'scripts\Qualify.ps1'), '-Package')) 'Build inputs changed'
    & git -C $base checkout -q -- ($marker.Replace('\', '/'))
    $packaged = Invoke-Isolated @('-File', $package)
    $zipMatch = [regex]::Match($packaged.Output, 'Package: (.+\.zip)')
    Add-Case 'package-after-restoring-identical-inputs-accepted' ($packaged.ExitCode -eq 0 -and $zipMatch.Success) ('accepted: ' + (Get-Reason $packaged.Output))
    if (-not $zipMatch.Success) { throw "Package.ps1 failed: $($packaged.Output)" }
    $candidateZip = Join-Path $inputs 'candidate.zip'
    Copy-Item -LiteralPath $zipMatch.Groups[1].Value.Trim() -Destination $candidateZip

    # A changed build (different IL) checked against the first receipt.
    [IO.File]::AppendAllText((Join-Path $base $marker), "`r`nnamespace KingmakerDiceRoller.Domain { internal static class LoaderBaselineTestMarker { } }`r`n")
    $rebuild = Invoke-Isolated @('-File', (Join-Path $base 'scripts\Build-Local.ps1'))
    $secondDllSha = Get-Sha256 $dll
    Add-Case 'rebuild-after-source-change-accepted' ($rebuild.ExitCode -eq 0 -and $secondDllSha -ne $firstDllSha) "exit $($rebuild.ExitCode); first DLL $firstDllSha; changed-source DLL $secondDllSha"
    $second = Invoke-Isolated @('-File', $package)
    $secondMatch = [regex]::Match($second.Output, 'Package: (.+\.zip)')
    Add-Case 'package-after-matching-rebuild-accepted' ($second.ExitCode -eq 0 -and $secondMatch.Success) ('accepted: ' + (Get-Reason $second.Output))
    $secondZip = Join-Path $inputs 'changed-source-candidate.zip'
    if ($secondMatch.Success) { Copy-Item -LiteralPath $secondMatch.Groups[1].Value.Trim() -Destination $secondZip }
    Expect-Rejected 'receipt-of-first-build-for-changed-dll-rejected' (Test-Receipt $firstReceipt $dll 'Release') 'does not match the mod DLL'
    $validator = Join-Path $base 'scripts\Validate-Package.ps1'
    Expect-Rejected 'archived-dll-vs-first-receipt-rejected' (Invoke-Isolated @('-File', $validator, '-PackagePath', $secondZip, '-ExpectedDllSha256', $firstDllSha)) 'does not match the build receipt'
    & git -C $base checkout -q -- ($marker.Replace('\', '/'))
    $rebuild = Invoke-Isolated @('-File', (Join-Path $base 'scripts\Build-Local.ps1'))
    Add-Case 'rebuild-of-committed-source-reproduces-first-dll' ($rebuild.ExitCode -eq 0 -and (Get-Sha256 $dll) -eq $firstDllSha) "DLL $(Get-Sha256 $dll)"
    $conflict = Join-Path $inputs 'receipt-conflicting-version.json'
    Copy-Item -LiteralPath $receiptPath -Destination $conflict
    Edit-Json $conflict { param($j) @($j.loader_references | Where-Object file -eq 'UnityModManager.dll')[0].assembly_version = '0.33.0.0' }
    Expect-Rejected 'receipt-conflicting-observed-version-rejected' (Test-Receipt $conflict $dll 'Release') 'does not match pinned reference UnityModManager\.dll'
    $noRefs = Join-Path $inputs 'receipt-no-refs.json'
    Get-Content -LiteralPath $receiptPath -Raw | ConvertFrom-Json | Select-Object -Property * -ExcludeProperty loader_references | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $noRefs -Encoding UTF8
    Expect-Rejected 'receipt-without-loader-references-rejected' (Test-Receipt $noRefs $dll 'Release') 'no loader_references'
    $otherLoader = Join-Path $inputs 'receipt-other-loader.json'
    Copy-Item -LiteralPath $receiptPath -Destination $otherLoader
    Edit-Json $otherLoader { param($j) @($j.loader_references | Where-Object file -eq 'UnityModManager.dll')[0].sha256 = $wrongLoaderDllSha256 }
    Expect-Rejected 'receipt-recording-other-loader-rejected' (Test-Receipt $otherLoader $dll 'Release') 'does not match pinned reference UnityModManager\.dll'


    # 6b. Wrapper and resolved managed-reference changes also invalidate reuse; malformed receipt fields are refused.
    [IO.File]::AppendAllText((Join-Path $base 'scripts\Build-Local.ps1'), "`r`n# loader-baseline test edit`r`n")
    Expect-Rejected 'package-reuse-after-build-wrapper-change-rejected' (Invoke-Isolated @('-File', $package)) 'Build inputs changed'
    & git -C $base checkout -q -- scripts/Build-Local.ps1
    $managedCopy = Join-Path $temp 'managed copy'
    New-Item -ItemType Directory -Path (Join-Path $managedCopy 'UnityModManager') -Force | Out-Null
    [xml] $projectXml = Get-Content -LiteralPath $project -Raw
    foreach ($hint in @($projectXml.SelectNodes("//*[local-name()='Reference']/*[local-name()='HintPath']") | ForEach-Object { $_.InnerText } | Where-Object { $_.StartsWith('$(KingmakerManagedDir)') })) {
        $name = $hint.Substring('$(KingmakerManagedDir)'.Length).TrimStart('\')
        Copy-Item -LiteralPath (Join-Path $config.ManagedDir $name) -Destination (Join-Path $managedCopy $name)
    }
    Copy-Item -LiteralPath $pinnedUmm, $pinnedHarmony -Destination (Join-Path $managedCopy 'UnityModManager')
    $gamePath = Join-Path $base 'GamePath.props'
    $gamePathOriginal = [IO.File]::ReadAllText($gamePath)
    [IO.File]::WriteAllText($gamePath, $gamePathOriginal.Replace('</Project>', "  <PropertyGroup Condition=`"'`$(Configuration)' == 'Release'`"><KingmakerManagedDir>$managedCopy</KingmakerManagedDir></PropertyGroup>`r`n</Project>"))
    $resolvedList = Join-Path $inputs 'resolved-references.txt'
    Invoke-MSBuildIsolated @($project, '/t:ResolveReferences;FindReferenceAssembliesForReferences', '/p:Configuration=Release', "/p:CompilerReferenceListPath=$resolvedList", "/p:IntermediateOutputPath=$(Join-Path $temp 'resolve-obj\')") | Out-Null
    $uiResolved = @(Get-Content -LiteralPath $resolvedList | Where-Object { $_.StartsWith('reference|') } | ForEach-Object { $_.Substring('reference|'.Length) } | Where-Object { [IO.Path]::GetFileName($_) -eq 'UnityEngine.UI.dll' })
    Add-Case 'compiler-resolves-release-only-managed-directory' ($uiResolved.Count -eq 1 -and $uiResolved[0].StartsWith($managedCopy)) ("Release Csc input for UnityEngine.UI.dll: $($uiResolved -join ', ') (GamePath.props first group still names the installation)")
    Expect-Accepted 'package-with-identical-managed-references-elsewhere-accepted' (Invoke-Isolated @('-File', $package)) 'Package validation passed'
    $uiBytes = [IO.File]::ReadAllBytes((Join-Path $managedCopy 'UnityEngine.UI.dll'))
    $uiBytes[$uiBytes.Length - 1] = $uiBytes[$uiBytes.Length - 1] -bxor 0x01
    [IO.File]::WriteAllBytes((Join-Path $managedCopy 'UnityEngine.UI.dll'), $uiBytes)
    Expect-Rejected 'package-reuse-after-resolved-managed-reference-change-rejected' (Invoke-Isolated @('-File', $package)) '(Build inputs changed|compiler inputs differ)'
    [IO.File]::WriteAllText($gamePath, $gamePathOriginal)
    # Only the overflow-checking option changes (imported from GamePath.props): the stale package is refused, the receipt
    # of a matching rebuild records the effective option, and the rebuilt DLL differs; removing it needs a rebuild again.
    [IO.File]::WriteAllText($gamePath, $gamePathOriginal.Replace('</Project>', "  <PropertyGroup><CheckForOverflowUnderflow>true</CheckForOverflowUnderflow></PropertyGroup>`r`n</Project>"))
    Expect-Rejected 'package-reuse-after-overflow-checking-change-rejected' (Invoke-Isolated @('-File', $package)) 'compiler inputs differ'
    $checkedBuild = Invoke-Isolated @('-File', (Join-Path $base 'scripts\Build-Local.ps1'))
    $checkedReceipt = Get-Content -LiteralPath $receiptPath -Raw | ConvertFrom-Json
    $checkedSha = Get-Sha256 $dll
    $effective = @($checkedReceipt.compiler_references | Where-Object { $_.kind -eq 'setting' -and $_.name -like 'CheckForOverflowUnderflow=*' } | ForEach-Object name)
    Add-Case 'rebuild-with-overflow-checking-records-effective-option' ($checkedBuild.ExitCode -eq 0 -and $effective -ceq 'CheckForOverflowUnderflow=true' -and $checkedSha -ne $firstDllSha) ("exit $($checkedBuild.ExitCode); receipt records $($effective -join ', '); DLL $checkedSha (committed configuration $firstDllSha)" + $(if ($checkedBuild.ExitCode) { '; ' + (Get-Reason $checkedBuild.Output) } else { '' }))
    Expect-Accepted 'package-after-matching-overflow-checking-rebuild-accepted' (Invoke-Isolated @('-File', $package)) 'Package validation passed'
    [IO.File]::WriteAllText($gamePath, $gamePathOriginal)
    Expect-Rejected 'package-reuse-after-overflow-checking-removed-rejected' (Invoke-Isolated @('-File', $package)) 'compiler inputs differ'
    $rebuild = Invoke-Isolated @('-File', (Join-Path $base 'scripts\Build-Local.ps1'))
    Add-Case 'rebuild-without-overflow-checking-reproduces-first-dll' ($rebuild.ExitCode -eq 0 -and (Get-Sha256 $dll) -eq $firstDllSha) "DLL $(Get-Sha256 $dll)"
    $savedReceipt = Join-Path $inputs 'genuine-receipt.json'
    Copy-Item -LiteralPath $receiptPath -Destination $savedReceipt -Force
    # The receipt's reference inventory equals the /reference list of the direct normal-verbosity compile above.
    $cscLine = @($direct.Output -split "`r?`n" | Where-Object { $_ -match 'csc\.(exe|dll)' }) -join "`n"
    $basePrefix = [IO.Path]::GetFullPath($base).TrimEnd('\') + '\'
    $cscRefs = @([regex]::Matches($cscLine, '/reference:(?:"([^"]+)"|(\S+))') | ForEach-Object { if ($_.Groups[1].Success) { $_.Groups[1].Value } else { $_.Groups[2].Value } } | ForEach-Object {
        $n = if ($_.StartsWith($basePrefix, [StringComparison]::OrdinalIgnoreCase)) { $_.Substring($basePrefix.Length).Replace('\', '/') } else { [IO.Path]::GetFileName($_) }
        $n + '|' + (Get-Sha256 $_) } | Sort-Object)
    $receiptRefs = @((Get-Content -LiteralPath $receiptPath -Raw | ConvertFrom-Json).compiler_references | Where-Object { $_.kind -eq 'reference' } | ForEach-Object { $_.name + '|' + $_.sha256 } | Sort-Object)
    Add-Case 'receipt-reference-inventory-equals-csc-inputs' ($cscRefs.Count -gt 0 -and ($cscRefs -join ';') -ceq ($receiptRefs -join ';')) ("$($cscRefs.Count) Csc /reference inputs; receipt records $($receiptRefs.Count); equal: $(($cscRefs -join ';') -ceq ($receiptRefs -join ';'))")
    Edit-Json $receiptPath { param($j) @($j.compiler_references | Where-Object { $_.kind -eq 'reference' })[0].sha256 = ('0' * 64) }
    Expect-Rejected 'package-receipt-with-changed-reference-digest-rejected' (Invoke-Isolated @('-File', $package)) 'compiler inputs differ'
    Copy-Item -LiteralPath $savedReceipt -Destination $receiptPath -Force
    Get-Content -LiteralPath $savedReceipt -Raw | ConvertFrom-Json | Select-Object -Property * -ExcludeProperty compiler_references | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $receiptPath -Encoding UTF8
    Expect-Rejected 'package-receipt-without-compiler-inventory-rejected' (Invoke-Isolated @('-File', $package)) 'no compiler_references'
    Copy-Item -LiteralPath $savedReceipt -Destination $receiptPath -Force
    Expect-Accepted 'package-genuine-receipt-with-inventory-accepted' (Invoke-Isolated @('-File', $package)) 'Package validation passed'
    foreach ($bad in @(
        @{ Name = 'dll-digest-array'; Edit = { param($j) $j.dll_sha256 = @($j.dll_sha256) } },
        @{ Name = 'dll-digest-boolean'; Edit = { param($j) $j.dll_sha256 = $true } },
        @{ Name = 'inputs-digest-array'; Edit = { param($j) $j.build_inputs_sha256 = @($j.build_inputs_sha256, ('0' * 64)) } },
        @{ Name = 'configuration-array'; Edit = { param($j) $j.configuration = @('Release') } },
        @{ Name = 'loader-version-array'; Edit = { param($j) @($j.loader_references)[0].version = @(@($j.loader_references)[0].version) } })) {
        Copy-Item -LiteralPath $savedReceipt -Destination $receiptPath -Force
        Edit-Json $receiptPath $bad.Edit
        Expect-Rejected "package-receipt-$($bad.Name)-rejected" (Invoke-Isolated @('-File', $package)) 'must be (a lowercase SHA-256 string|a string)'
    }
    Copy-Item -LiteralPath $savedReceipt -Destination $receiptPath -Force
    Expect-Accepted 'package-genuine-receipt-accepted' (Invoke-Isolated @('-File', $package)) 'Package validation passed'

    # 6c. Package records come from the staged DLL, receipt bytes and validated archive, even if the build output changes
    #     after staging; ordinary packages name the producing build, also when identical inputs are packaged later.
    # Ordinary packaging leaves its inputs alone.
    $dllBefore = Get-Sha256 $dll; $receiptBefore = Get-Sha256 $receiptPath
    Invoke-Isolated @('-File', $package) | Out-Null
    Add-Case 'ordinary-package-preserves-build-output-and-receipt' ((Get-Sha256 $dll) -eq $dllBefore -and (Get-Sha256 $receiptPath) -eq $receiptBefore) "DLL $dllBefore and receipt unchanged"
    # Disposable instrumented copies (the committed scripts contain no test hooks).
    function New-InstrumentedScript([string] $Name, [string] $After, [string] $Inject) {
        $text = [IO.File]::ReadAllText((Join-Path $base "scripts\$Name.ps1"))
        if ($text.IndexOf($After) -lt 0) { throw "Instrumentation anchor not found in $Name.ps1: $After" }
        $path = Join-Path $base "scripts\$Name-instrumented.ps1"
        $index = $text.IndexOf($After) + $After.Length
        [IO.File]::WriteAllText($path, $text.Substring(0, $index) + "`r`n" + $Inject + "`r`n" + $text.Substring($index))
        return $path
    }
    $packageAfterStaging = New-InstrumentedScript 'Package' '$stagedDllSha = Get-Sha256 $stagedDll' ('Copy-Item -LiteralPath ' + (Quote $legacyDll) + ' -Destination $dll -Force')
    $mixed = Invoke-Isolated @('-File', $packageAfterStaging, '-Candidate')
    $candidateDir = Join-Path $base ('artifacts\candidates\' + $commit)
    $m = if (Test-Path -LiteralPath (Join-Path $candidateDir 'package-manifest.json')) { Get-Content -LiteralPath (Join-Path $candidateDir 'package-manifest.json') -Raw | ConvertFrom-Json } else { $null }
    $ok = $mixed.ExitCode -eq 0 -and $m -and (Get-Sha256 $dll) -ne $firstDllSha -and $m.dll_sha256 -eq $firstDllSha -and
        (Get-Sha256 (Join-Path $candidateDir 'KingmakerDiceRoller.dll')) -eq $firstDllSha -and
        (Get-Sha256 (Join-Path $candidateDir 'build-provenance.json')) -eq (Get-Sha256 $savedReceipt) -and
        $m.package_sha256 -eq (Get-Sha256 (Join-Path $candidateDir 'KingmakerDiceRoller-0.2.0.zip'))
    Add-Case 'candidate-package-records-frozen-after-build-output-replaced' $ok ("build output replaced after staging (now $(Get-Sha256 $dll)); manifest DLL $($m.dll_sha256); loose DLL and receipt copy match; ZIP $($m.package_sha256)")
    Copy-Item -LiteralPath (Join-Path $candidateDir 'KingmakerDiceRoller.dll') -Destination $dll -Force
    [IO.File]::AppendAllText((Join-Path $base 'docs\UMM-COMPATIBILITY.md'), "`n<!-- loader-baseline test: documentation-only commit -->`n")
    & git -C $base -c user.name=loader-baseline-test -c user.email=loader-baseline-test@invalid commit -q -am 'test: documentation-only commit'
    $laterCommit = (& git -C $base rev-parse HEAD).Trim()
    $later = Invoke-Isolated @('-File', $package)
    $m = Get-Content -LiteralPath (Join-Path $base 'artifacts\packages\package-manifest.json') -Raw | ConvertFrom-Json
    $ok = $later.ExitCode -eq 0 -and $m.producing_commit -eq $commit -and $m.packaging_commit -eq $laterCommit -and $m.dll_sha256 -eq $firstDllSha
    Add-Case 'package-names-producing-build-across-commits' $ok ("producing_commit $($m.producing_commit); packaging_commit $($m.packaging_commit); DLL $($m.dll_sha256)")
    Expect-Rejected 'candidate-refuses-earlier-commit-receipt-with-identical-dll' (Invoke-Isolated @('-File', $package, '-Candidate')) 'matching build from the exact clean current commit'
    $refreshed = Join-Path $inputs 'receipt-claiming-later-commit.json'
    Copy-Item -LiteralPath $savedReceipt -Destination $refreshed -Force
    Edit-Json $refreshed { param($j) $j.commit = $laterCommit }
    $packageRefresh = New-InstrumentedScript 'Package' '$loaderReceipt = [Text.Encoding]::UTF8.GetString($receiptBytes).TrimStart([char]0xFEFF) | ConvertFrom-Json' ('Copy-Item -LiteralPath ' + (Quote $refreshed) + ' -Destination $loaderReceiptPath -Force')
    Expect-Rejected 'candidate-refuses-when-receipt-refreshed-during-packaging' (Invoke-Isolated @('-File', $packageRefresh, '-Candidate')) 'matching build from the exact clean current commit'
    Remove-Item -LiteralPath $packageRefresh -Force
    foreach ($bad in @(@{ Name = 'commit-array'; Edit = { param($j) $j.commit = @($j.commit) } }, @{ Name = 'dirty-string'; Edit = { param($j) $j.dirty = 'false' } })) {
        Copy-Item -LiteralPath $savedReceipt -Destination $receiptPath -Force
        Edit-Json $receiptPath $bad.Edit
        Expect-Rejected "package-receipt-$($bad.Name)-rejected" (Invoke-Isolated @('-File', $package, '-Candidate')) '(commit must be a 40-character|dirty must be a JSON Boolean)'
    }
    Invoke-Isolated @('-File', (Join-Path $base 'scripts\Build-Local.ps1')) | Out-Null
    Expect-Accepted 'candidate-accepted-after-build-at-current-commit' (Invoke-Isolated @('-File', $package, '-Candidate')) 'Package validation passed'
    & git -C $base reset -q --hard $commit
    Copy-Item -LiteralPath $savedReceipt -Destination $receiptPath -Force

    # 7. Standalone validator on real archives: each case in its own process, and both archives in one host.
    $v = { param($zip) Invoke-Isolated @('-File', $validator, '-PackagePath', $zip) }
    Expect-Accepted 'package-candidate-accepted' (& $v $candidateZip) 'Packaged DLL SHA-256: [0-9a-f]{64}; MVID: [0-9a-f-]{36}; loader references: UnityModManager 0\.31\.1\.0, 0Harmony12 1\.2\.0\.1'
    Expect-Accepted 'package-candidate-matches-receipt-dll' (Invoke-Isolated @('-File', $validator, '-PackagePath', $candidateZip, '-ExpectedDllSha256', $firstDllSha)) 'Package validation passed'
    $p = Join-Path $inputs 'extra-harmony.zip'; Edit-Zip $candidateZip $p { param($a) Set-ZipEntryFile $a "$modFolder/0Harmony12.dll" $pinnedHarmony }
    Expect-Rejected 'package-extra-harmony-rejected' (& $v $p) 'exactly \d+ files'
    $p = Join-Path $inputs 'swap-loader.zip'; Edit-Zip $candidateZip $p { param($a) $a.GetEntry("$modFolder/README.md").Delete(); Set-ZipEntryFile $a "$modFolder/UnityModManager.dll" $pinnedUmm }
    Expect-Rejected 'package-swapped-in-loader-rejected' (& $v $p) 'Unexpected package entry: KingmakerDiceRoller/UnityModManager\.dll'
    $p = Join-Path $inputs 'swap-game.zip'; Edit-Zip $candidateZip $p { param($a) $a.GetEntry("$modFolder/README.md").Delete(); Set-ZipEntryFile $a "$modFolder/Assembly-CSharp-firstpass.dll" (Join-Path $config.ManagedDir 'Assembly-CSharp-firstpass.dll') }
    Expect-Rejected 'package-swapped-in-game-assembly-rejected' (& $v $p) 'Unexpected package entry'
    $p = Join-Path $inputs 'directory-with-data.zip'; Edit-Zip $candidateZip $p { param($a) $w = [IO.StreamWriter]::new($a.CreateEntry("$modFolder/extra/").Open()); try { $w.Write('inert marker') } finally { $w.Dispose() } }
    Expect-Rejected 'package-data-bearing-directory-record-rejected' (& $v $p) 'data-bearing package directory record'
    $p = Join-Path $inputs 'own-directory-with-data.zip'; Edit-Zip $candidateZip $p { param($a) $w = [IO.StreamWriter]::new($a.CreateEntry("$modFolder/").Open()); try { $w.Write('inert marker') } finally { $w.Dispose() } }
    Expect-Rejected 'package-own-directory-record-with-data-rejected' (& $v $p) 'data-bearing package directory record'
    # A level-0 DEFLATE empty directory record: the fixture writer's exit status and the finished archive are checked
    # (six files plus the directory; method 8; compressed size above two bytes; zero decoded bytes) before validating.
    $py = Resolve-PythonCommand
    function New-Level0DirectoryFixture([string] $From, [string] $To) {
        $writer = "import sys,zipfile,shutil; shutil.copyfile(sys.argv[1],sys.argv[2]); z=zipfile.ZipFile(sys.argv[2],'a'); i=zipfile.ZipInfo('KingmakerDiceRoller/licenses/'); z.writestr(i,b'',compress_type=zipfile.ZIP_DEFLATED,compresslevel=0); z.close(); z=zipfile.ZipFile(sys.argv[2]); d=[x for x in z.infolist() if x.filename.endswith('/')]; f=[x for x in z.infolist() if not x.filename.endswith('/')]; print('FIXTURE files=%d dirs=%s' % (len(f), ';'.join('%s,%d,%d,%d' % (x.filename,x.compress_type,x.compress_size,x.file_size) for x in d)))"
        $previous = $ErrorActionPreference; $ErrorActionPreference = 'Continue'
        try { $out = (& $py.Executable @($py.Prefix) -I -c $writer $From $To 2>&1 | ForEach-Object { "$_" }) -join ' '; $exit = $LASTEXITCODE }
        finally { $ErrorActionPreference = $previous }
        $m = [regex]::Match($out, 'FIXTURE files=(\d+) dirs=KingmakerDiceRoller/licenses/,(\d+),(\d+),(\d+)$')
        $ok = $exit -eq 0 -and $m.Success -and [int]$m.Groups[1].Value -eq 6 -and [int]$m.Groups[2].Value -eq 8 -and [int]$m.Groups[3].Value -gt 2 -and [int]$m.Groups[4].Value -eq 0
        return [pscustomobject]@{ Ok = $ok; Exit = $exit; Description = $out.Trim() }
    }
    $p = Join-Path $inputs 'deflate-level0-directory.zip'
    $fixture = New-Level0DirectoryFixture $candidateZip $p
    $r = if ($fixture.Ok) { & $v $p } else { $null }
    Add-Case 'package-level0-deflate-empty-directory-accepted' ($fixture.Ok -and $r.ExitCode -eq 0 -and $r.Output -match 'Package validation passed') ("writer exit $($fixture.Exit): $($fixture.Description); " + $(if (-not $fixture.Ok) { 'fixture invalid (case fails)' } elseif ($r.ExitCode -eq 0) { 'accepted' } else { Get-Reason $r.Output }))
    $failedFixture = New-Level0DirectoryFixture (Join-Path $inputs 'no such source.zip') (Join-Path $inputs 'never-written.zip')
    Add-Case 'level0-fixture-generation-failure-fails-the-case' (-not $failedFixture.Ok -and $failedFixture.Exit -ne 0) ("writer exit $($failedFixture.Exit); fixture accepted as valid: $($failedFixture.Ok)")
    # An exclusively held input: the copy failure is reported and no kdr-validate-* directory is left behind.
    $held = Join-Path $inputs 'held.zip'
    Copy-Item -LiteralPath $candidateZip -Destination $held
    $validateDirsBefore = @(Get-ChildItem -LiteralPath ([IO.Path]::GetTempPath()) -Directory -Filter 'kdr-validate-*' | ForEach-Object Name | Sort-Object) -join ';'
    $lock = [IO.File]::Open($held, [IO.FileMode]::Open, [IO.FileAccess]::Read, [IO.FileShare]::None)
    try { $r = & $v $held } finally { $lock.Dispose() }
    $validateDirsAfter = @(Get-ChildItem -LiteralPath ([IO.Path]::GetTempPath()) -Directory -Filter 'kdr-validate-*' | ForEach-Object Name | Sort-Object) -join ';'
    Add-Case 'validator-copy-failure-reported-and-cleaned' ($r.ExitCode -ne 0 -and ($r.Output -replace '\s+', ' ') -match 'being used by another process' -and $validateDirsBefore -ceq $validateDirsAfter) ("exit $($r.ExitCode): " + (Get-Reason $r.Output) + "; kdr-validate-* directories unchanged: $($validateDirsBefore -ceq $validateDirsAfter)")
    $callerZip = Join-Path $inputs 'caller-path.zip'
    Copy-Item -LiteralPath $candidateZip -Destination $callerZip
    $validatorSwap = New-InstrumentedScript 'Validate-Package' '[IO.File]::Copy($PackagePath, $inspected)' ('[IO.File]::Copy(' + (Quote $secondZip) + ', $PackagePath, $true)')
    $mixReport = Join-Path $inputs 'mix-report.json'
    $mix = Invoke-Isolated @('-File', $validatorSwap, '-PackagePath', $callerZip, '-ReportPath', $mixReport)
    $mr = if (Test-Path -LiteralPath $mixReport) { Get-Content -LiteralPath $mixReport -Raw | ConvertFrom-Json } else { $null }
    $ok = $mix.ExitCode -eq 0 -and $mr -and $mr.package_sha256 -eq (Get-Sha256 $candidateZip) -and $mr.dll_sha256 -eq $firstDllSha -and (Get-Sha256 $callerZip) -eq (Get-Sha256 $secondZip)
    Add-Case 'validator-report-pairs-zip-digest-with-its-own-dll' $ok ("caller file replaced after the snapshot: report ZIP $($mr.package_sha256) with DLL $($mr.dll_sha256)")
    Remove-Item -LiteralPath $validatorSwap -Force
    Expect-Rejected 'validator-explicit-empty-expected-digest-rejected' (Invoke-Script "& $(Quote $validator) -PackagePath $(Quote $candidateZip) -ExpectedDllSha256 ''") 'ExpectedDllSha256 must be a lowercase SHA-256 string'
    $p = Join-Path $inputs 'harmony-as-readme.zip'; Edit-Zip $candidateZip $p { param($a) Set-ZipEntryFile $a "$modFolder/README.md" $pinnedHarmony }
    Expect-Rejected 'package-harmony-bytes-under-readme-name-rejected' (& $v $p) 'binary content: KingmakerDiceRoller/README\.md'
    $p = Join-Path $inputs 'loader-as-licence.zip'; Edit-Zip $candidateZip $p { param($a) Set-ZipEntryFile $a "$modFolder/licenses/UPSTREAM-WOTR-DICE-ROLLER-MIT.txt" $pinnedUmm }
    Expect-Rejected 'package-loader-bytes-under-licence-name-rejected' (& $v $p) 'binary content'
    foreach ($bad in @(@{ Name = 'array'; Value = '["0.31.1"]' }, @{ Name = 'boolean'; Value = 'true' }, @{ Name = 'number'; Value = '0.31' })) {
        $p = Join-Path $inputs "manager-$($bad.Name).zip"
        $replacement = '"ManagerVersion": ' + $bad.Value
        Edit-Zip $candidateZip $p { param($a) $t = Get-ZipEntryText $a "$modFolder/Info.json"; Set-ZipEntryText $a "$modFolder/Info.json" ($t -replace '"ManagerVersion":\s*"0\.31\.1"', $replacement) }
        Expect-Rejected "package-manager-version-$($bad.Name)-rejected" (& $v $p) 'does not match the 0\.31\.1 baseline'
    }
    $legacyRef = Invoke-Script "([Reflection.Assembly]::ReflectionOnlyLoad([IO.File]::ReadAllBytes($(Quote $legacyDll))).GetReferencedAssemblies() | Where-Object Name -eq 'UnityModManager').Version.ToString()"
    Add-Case 'legacy-release-references-umm-0.33.0' ($legacyRef.ExitCode -eq 0 -and $legacyRef.Output.Trim() -eq '0.33.0.0') ("genuine v0.2.0 ZIP $legacySha256; its DLL references UnityModManager " + $legacyRef.Output.Trim())
    $lowered = Join-Path $inputs 'legacy-lowered-manifest.zip'
    Edit-Zip $legacyZip $lowered { param($a)
        $t = Get-ZipEntryText $a "$modFolder/Info.json"
        $c = $t -replace '"ManagerVersion":\s*"[0-9.]+"', '"ManagerVersion": "0.31.1"'
        if ($c -eq $t) { throw 'Legacy manifest was not lowered.' }
        Set-ZipEntryText $a "$modFolder/Info.json" $c }
    Expect-Rejected 'package-legacy-0.33.0-dll-with-lowered-manifest-rejected' (& $v $lowered) 'must reference pinned UnityModManager 0\.31\.1\.0'
    foreach ($order in @(@($candidateZip, $lowered), @($lowered, $candidateZip))) {
        $text = "foreach (`$zip in @($(Quote $order[0]), $(Quote $order[1]))) { try { & $(Quote $validator) -PackagePath `$zip | Out-Null; 'ACCEPTED ' + [IO.Path]::GetFileName(`$zip) } catch { 'REJECTED ' + [IO.Path]::GetFileName(`$zip) + ': ' + `$_.Exception.Message } }"
        $r = Invoke-Script $text
        $ok = $r.Output -match 'ACCEPTED candidate\.zip' -and $r.Output -match 'REJECTED legacy-lowered-manifest\.zip: Packaged DLL must reference pinned UnityModManager 0\.31\.1\.0'
        Add-Case "same-host-validation-$([IO.Path]::GetFileNameWithoutExtension($order[0]))-first" $ok ($r.Output.Trim() -replace "`r?`n", ' | ')
    }

    # 8. Install route: Install.ps1 extracts only through Expand-ValidatedPackage (private validated copy).
    $r = Invoke-Script ". $(Quote $common); Expand-ValidatedPackage $(Quote $candidateZip) $(Quote (Join-Path $temp 'expand ok'))"
    Add-Case 'expand-validated-package-candidate' ($r.ExitCode -eq 0 -and $r.Output -match $firstDllSha) ('returned validated DLL SHA-256: ' + ($r.Output.Trim() -split "`r?`n" | Select-Object -Last 1))
    $r = Invoke-Script ". $(Quote $common); Expand-ValidatedPackage $(Quote $lowered) $(Quote (Join-Path $temp 'expand legacy'))"
    $ok = $r.ExitCode -ne 0 -and $r.Output -match 'must reference pinned UnityModManager' -and -not (Test-Path -LiteralPath (Join-Path $temp 'expand legacy\KingmakerDiceRoller'))
    Add-Case 'expand-validated-package-legacy-rejected' $ok ('rejected before extraction: ' + (Get-Reason $r.Output))
    $install = [IO.File]::ReadAllText((Join-Path $base 'scripts\Install.ps1'))
    $ok = $install.Contains('Expand-ValidatedPackage $PackagePath $temporary') -and -not $install.Contains('ExtractToDirectory') -and
        $install.IndexOf('Expand-ValidatedPackage') -lt $install.IndexOf('$config.ModsDir')
    Add-Case 'install-uses-validated-private-copy' $ok 'Install.ps1 extracts only via Expand-ValidatedPackage, before any live Mods path (Install.ps1 itself is not executed)'

    # The real Install.ps1 -WhatIf against a disposable configuration: valid ZIP reaches the target preview, an invalid
    # ZIP is refused, temporary files are cleaned up, and the disposable Mods folder is unchanged.
    $fakeGame = Join-Path $temp 'disposable game'
    $fakeManaged = Join-Path $fakeGame 'Kingmaker_Data\Managed'
    New-Item -ItemType Directory -Path (Join-Path $fakeManaged 'UnityModManager'), (Join-Path $fakeGame 'Mods\OtherMod') -Force | Out-Null
    Copy-Item -LiteralPath (Join-Path $config.ManagedDir 'Assembly-CSharp.dll') -Destination $fakeManaged
    Copy-Item -LiteralPath $pinnedUmm, $pinnedHarmony -Destination (Join-Path $fakeManaged 'UnityModManager')
    [IO.File]::WriteAllText((Join-Path $fakeGame 'Mods\OtherMod\Info.json'), '{"Id":"OtherMod"}')
    [IO.File]::WriteAllText($gamePath, "<Project xmlns=`"http://schemas.microsoft.com/developer/msbuild/2003`"><PropertyGroup><KingmakerInstallDir>$fakeGame</KingmakerInstallDir><KingmakerManagedDir>$fakeManaged</KingmakerManagedDir><UnityModManagerDir>$(Join-Path $fakeManaged 'UnityModManager')</UnityModManagerDir></PropertyGroup></Project>")
    $modsBefore = @(Get-ChildItem -LiteralPath (Join-Path $fakeGame 'Mods') -Recurse -Force | ForEach-Object { $_.FullName + '|' + $(if ($_.PSIsContainer) { 'dir' } else { Get-Sha256 $_.FullName }) }) -join ';'
    $tempBefore = @(Get-ChildItem -LiteralPath ([IO.Path]::GetTempPath()) -Directory -Filter 'KingmakerDiceRoller-*' | ForEach-Object Name) -join ';'
    $installScript = Join-Path $base 'scripts\Install.ps1'
    $w = Invoke-Isolated @('-File', $installScript, '-PackagePath', $candidateZip, '-WhatIf')
    $flat = $w.Output -replace '\s+', ' '
    $ok = $w.ExitCode -eq 0 -and $flat -match 'WhatIf completed' -and $flat -match [regex]::Escape((Join-Path $fakeGame 'Mods\KingmakerDiceRoller'))
    Add-Case 'install-whatif-valid-package-previews-target' $ok ('accepted: ' + (@($w.Output -split "`n" | Where-Object { $_ -match 'What if|WhatIf' }) -join ' | '))
    $w = Invoke-Isolated @('-File', $installScript, '-PackagePath', $lowered, '-WhatIf')
    Expect-Rejected 'install-whatif-invalid-package-rejected' $w 'must reference pinned UnityModManager 0\.31\.1\.0'
    $modsAfter = @(Get-ChildItem -LiteralPath (Join-Path $fakeGame 'Mods') -Recurse -Force | ForEach-Object { $_.FullName + '|' + $(if ($_.PSIsContainer) { 'dir' } else { Get-Sha256 $_.FullName }) }) -join ';'
    $tempAfter = @(Get-ChildItem -LiteralPath ([IO.Path]::GetTempPath()) -Directory -Filter 'KingmakerDiceRoller-*' | ForEach-Object Name) -join ';'
    Add-Case 'install-whatif-leaves-mods-and-temp-unchanged' ($modsBefore -eq $modsAfter -and $tempBefore -eq $tempAfter) "disposable Mods unchanged; no leftover KingmakerDiceRoller-* temporary folders"
    [IO.File]::WriteAllText($gamePath, $gamePathOriginal)

    # 9. Inputs and this repository's outputs are preserved.
    $legacyOk = (Get-Sha256 $legacyZip) -eq $legacySha256 -and (-not $LegacyReleaseZip -or (Get-Sha256 $LegacyReleaseZip) -eq $legacyOriginalSha)
    Add-Case 'legacy-input-preserved' $legacyOk "private copy and any supplied original still $legacySha256"
    $sourcePackagesAfter = @(Get-ChildItem -LiteralPath (Join-Path $source 'artifacts\packages') -File -ErrorAction SilentlyContinue | ForEach-Object { $_.Name + '=' + (Get-Sha256 $_.FullName) })
    Add-Case 'repository-package-outputs-untouched' (($sourcePackagesBefore -join ';') -eq ($sourcePackagesAfter -join ';')) ('artifacts\packages before = after: ' + ($sourcePackagesBefore -join ', '))

    $failed = @($cases | Where-Object { -not $_.passed })
    $reportFile = Join-Path $inputs 'candidate-validation.json'
    Invoke-Isolated @('-File', $validator, '-PackagePath', $candidateZip, '-ReportPath', $reportFile) | Out-Null
    $candidateReport = Get-Content -LiteralPath $reportFile -Raw | ConvertFrom-Json
    $summary = [ordered]@{
        schema = 'kdr.loader-baseline-tests.v4'
        source_commit = $commit
        clone_build_receipt = [ordered]@{ commit = $receipt.commit; dirty = $receipt.dirty; configuration = $receipt.configuration; dll_sha256 = $receipt.dll_sha256; build_inputs_sha256 = $receipt.build_inputs_sha256; loader_references = $receipt.loader_references }
        candidate_package_sha256 = Get-Sha256 $candidateZip
        candidate_validation = [ordered]@{ dll_sha256 = $candidateReport.dll_sha256; dll_mvid = $candidateReport.dll_mvid; manager_version = $candidateReport.manager_version; dll_loader_references = $candidateReport.dll_loader_references }
        legacy_release_zip_sha256 = $legacySha256
        wrong_loader_fixture = "UnityModManager 0.32.4.0 $wrongLoaderDllSha256 (NuGet package $wrongLoaderPackageSha256)"
        owner_install_measured = $ownerMeasurement
        passed = $cases.Count - $failed.Count
        total = $cases.Count
        cases = @($cases)
    }
    if ($ReportPath) { $summary | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $ReportPath -Encoding UTF8 }
    $summary.Remove('cases')
    Write-Host ($summary | ConvertTo-Json -Depth 6)
    Write-Host ("RESULT {0}/{1} loader-baseline checks passed" -f ($cases.Count - $failed.Count), $cases.Count)
    if ($failed.Count -gt 0) { throw "$($failed.Count) loader-baseline check(s) failed." }
}
finally {
    Remove-Item -LiteralPath $temp -Recurse -Force -ErrorAction SilentlyContinue
}
