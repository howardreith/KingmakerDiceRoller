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
    Copy-Item -LiteralPath (Join-Path $base 'artifacts\contracts\respec-contracts.json') -Destination (Join-Path $inputs 'respec-contracts-other-build.json')
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
    Invoke-MSBuildIsolated @($project, '/t:Compile', '/p:SkipCompilerExecution=true', '/p:Configuration=Release', "/p:OutputPath=$(Join-Path $temp 'resolve-bin')", "/p:CompilerReferenceListPath=$resolvedList", "/p:IntermediateOutputPath=$(Join-Path $temp 'resolve-obj\')") | Out-Null
    $uiResolved = @(Get-Content -LiteralPath $resolvedList | Where-Object { $_.StartsWith('arg|/reference:') } | ForEach-Object { $_.Substring('arg|/reference:'.Length).Trim('"') } | Where-Object { [IO.Path]::GetFileName($_) -eq 'UnityEngine.UI.dll' })
    Add-Case 'compiler-resolves-release-only-managed-directory' ($uiResolved.Count -eq 1 -and $uiResolved[0].StartsWith($managedCopy)) ("Release Csc input for UnityEngine.UI.dll: $($uiResolved -join ', ') (GamePath.props first group still names the installation)")
    Expect-Accepted 'package-with-identical-managed-references-elsewhere-accepted' (Invoke-Isolated @('-File', $package)) 'Package validation passed'
    $uiBytes = [IO.File]::ReadAllBytes((Join-Path $managedCopy 'UnityEngine.UI.dll'))
    $uiBytes[$uiBytes.Length - 1] = $uiBytes[$uiBytes.Length - 1] -bxor 0x01
    [IO.File]::WriteAllBytes((Join-Path $managedCopy 'UnityEngine.UI.dll'), $uiBytes)
    Expect-Rejected 'package-reuse-after-resolved-managed-reference-change-rejected' (Invoke-Isolated @('-File', $package)) '(Build inputs changed|compiler inputs differ)'
    [IO.File]::WriteAllText($gamePath, $gamePathOriginal)
    # Bounded local configuration: GamePath.props may only set the path properties. An overflow setting that applies only
    # to a real compile (conditioned on SkipCompilerExecution), and an unconditional one, are refused at package admission
    # and at Build-Local; an untracked Directory.Build.targets and an import-selecting environment variable are refused too.
    $configCases = @(
        @{ Name = 'production-only-overflow-setting'; Text = $gamePathOriginal.Replace('</Project>', "  <PropertyGroup Condition=`"'`$(SkipCompilerExecution)' != 'true'`"><CheckForOverflowUnderflow>true</CheckForOverflowUnderflow></PropertyGroup>`r`n</Project>") },
        @{ Name = 'unconditional-overflow-setting'; Text = $gamePathOriginal.Replace('</Project>', "  <PropertyGroup><CheckForOverflowUnderflow>true</CheckForOverflowUnderflow></PropertyGroup>`r`n</Project>") })
    foreach ($case in $configCases) {
        [IO.File]::WriteAllText($gamePath, $case.Text)
        Expect-Rejected "package-with-gamepath-$($case.Name)-refused" (Invoke-Isolated @('-File', $package)) 'GamePath\.props may only set'
        Expect-Rejected "build-with-gamepath-$($case.Name)-refused" (Invoke-Isolated @('-File', (Join-Path $base 'scripts\Build-Local.ps1'))) 'GamePath\.props may only set'
    }
    [IO.File]::WriteAllText($gamePath, $gamePathOriginal)
    $untrackedTargets = Join-Path $base 'src\Directory.Build.targets'
    [IO.File]::WriteAllText($untrackedTargets, "<Project><PropertyGroup Condition=`"'`$(SkipCompilerExecution)' != 'true'`"><CheckForOverflowUnderflow>true</CheckForOverflowUnderflow></PropertyGroup></Project>")
    Expect-Rejected 'package-with-untracked-directory-build-targets-refused' (Invoke-Isolated @('-File', $package)) 'Untracked MSBuild configuration files apply to the build: src/Directory\.Build\.targets'
    Remove-Item -LiteralPath $untrackedTargets
    $env:DirectoryBuildTargetsPath = Join-Path $inputs 'selected.targets'
    try { Expect-Rejected 'package-with-import-selecting-environment-variable-refused' (Invoke-Isolated @('-File', $package)) 'environment variable DirectoryBuildTargetsPath selects MSBuild imports' }
    finally { Remove-Item Env:\DirectoryBuildTargetsPath }
    Expect-Accepted 'package-after-local-configuration-restored-accepted' (Invoke-Isolated @('-File', $package)) 'Package validation passed'
    # R6-F1: configuration MSBuild does not import for the production project does not stop reuse or a valid build: a
    # parent Directory.Build.props above the checkout (shadowed by the tracked root one; it would enable overflow
    # checking), untracked Directory.Build.targets beside other folders (tools\ and src\Scratch\), and a per-user hook of
    # a toolset version this build does not use (15.0, in a disposable user profile). First the existing DLL and its
    # receipt are packaged without rebuilding; then Build-Local reproduces the first DLL and its package is accepted. A
    # hook in the selected toolset's location (Current) is refused; the applicable untracked src\Directory.Build.targets
    # stays refused (package-with-untracked-directory-build-targets-refused above).
    $shadowedProps = Join-Path $temp 'Directory.Build.props'
    [IO.File]::WriteAllText($shadowedProps, "<Project><PropertyGroup><CheckForOverflowUnderflow>true</CheckForOverflowUnderflow></PropertyGroup></Project>")
    $siblingTargets = Join-Path $base 'tools\Directory.Build.targets'
    [IO.File]::WriteAllText($siblingTargets, "<Project><PropertyGroup><CheckForOverflowUnderflow>true</CheckForOverflowUnderflow></PropertyGroup></Project>")
    $scratchTargets = Join-Path $base 'src\Scratch\Directory.Build.targets'
    New-Item -ItemType Directory -Path (Split-Path -Parent $scratchTargets) -Force | Out-Null
    [IO.File]::WriteAllText($scratchTargets, "<Project><PropertyGroup><CheckForOverflowUnderflow>true</CheckForOverflowUnderflow></PropertyGroup></Project>")
    $profileLocal = Join-Path $temp 'disposable user profile\AppData\Local'
    $inactiveHook = Join-Path $profileLocal 'Microsoft\MSBuild\15.0\Microsoft.Common.targets\ImportAfter\Overflow.targets'
    $activeHook = Join-Path $profileLocal 'Microsoft\MSBuild\Current\Microsoft.Common.targets\ImportAfter\Overflow.targets'
    New-Item -ItemType Directory -Path (Split-Path -Parent $inactiveHook) -Force | Out-Null
    [IO.File]::WriteAllText($inactiveHook, "<Project><PropertyGroup><CheckForOverflowUnderflow>true</CheckForOverflowUnderflow></PropertyGroup></Project>")
    $savedLocalAppData = $env:LOCALAPPDATA
    $env:LOCALAPPDATA = $profileLocal
    try {
        $receiptBeforeReuse = Get-Sha256 $receiptPath; $dllBeforeReuse = Get-Sha256 $dll
        $reused = Invoke-Isolated @('-File', $package)
        Add-Case 'package-reusing-the-existing-receipt-with-unrelated-configuration-accepted' ($reused.ExitCode -eq 0 -and $reused.Output -match 'Package validation passed' -and $dllBeforeReuse -eq $firstDllSha -and (Get-Sha256 $dll) -eq $firstDllSha -and (Get-Sha256 $receiptPath) -eq $receiptBeforeReuse) ("no rebuild: DLL $dllBeforeReuse (first DLL $firstDllSha); receipt unchanged $((Get-Sha256 $receiptPath) -eq $receiptBeforeReuse); package exit $($reused.ExitCode); " + (Get-Reason $reused.Output))
        $unrelated = Invoke-Isolated @('-File', (Join-Path $base 'scripts\Build-Local.ps1'))
        Add-Case 'build-with-unrelated-configuration-and-inactive-user-hook-accepted' ($unrelated.ExitCode -eq 0 -and (Get-Sha256 $dll) -eq $firstDllSha) ("exit $($unrelated.ExitCode); DLL $(Get-Sha256 $dll) (first DLL $firstDllSha)" + $(if ($unrelated.ExitCode) { '; ' + (Get-Reason $unrelated.Output) } else { '' }))
        Expect-Accepted 'package-after-build-with-unrelated-configuration-accepted' (Invoke-Isolated @('-File', $package)) 'Package validation passed'
        New-Item -ItemType Directory -Path (Split-Path -Parent $activeHook) -Force | Out-Null
        [IO.File]::WriteAllText($activeHook, "<Project><PropertyGroup><CheckForOverflowUnderflow>true</CheckForOverflowUnderflow></PropertyGroup></Project>")
        Expect-Rejected 'package-with-user-hook-of-the-selected-toolset-refused' (Invoke-Isolated @('-File', $package)) 'Per-user MSBuild extension files apply to the build: .*\\Current\\Microsoft\.Common\.targets\\ImportAfter\\Overflow\.targets'
        Remove-Item -LiteralPath $activeHook
    }
    finally { $env:LOCALAPPDATA = $savedLocalAppData }
    Remove-Item -LiteralPath $shadowedProps, $siblingTargets
    Remove-Item -LiteralPath (Split-Path -Parent $scratchTargets) -Recurse -Force
    Remove-Item -LiteralPath (Join-Path $temp 'disposable user profile') -Recurse -Force
    # R4-F1: a tracked root Directory.Build.targets with a production-only overflow setting (conditioned on a real
    # compile, so the evaluated inventory does not show it) added after the clean build: package admission refuses the old
    # receipt (the file is a fingerprinted build input) and Build-Local refuses the build (the production compile's inputs
    # differ from the evaluated ones, and it records no receipt, so the DLL it compiled is refused at packaging too); with
    # the file removed, a rebuild reproduces the first DLL and its receipt is accepted.
    $rootTargets = Join-Path $base 'Directory.Build.targets'
    [IO.File]::WriteAllText($rootTargets, "<Project><PropertyGroup Condition=`"'`$(SkipCompilerExecution)' != 'true'`"><CheckForOverflowUnderflow>true</CheckForOverflowUnderflow></PropertyGroup></Project>")
    & git -C $base add -- Directory.Build.targets
    Expect-Rejected 'package-reuse-after-tracked-root-targets-production-setting-rejected' (Invoke-Isolated @('-File', $package)) 'Build inputs changed'
    $receiptBeforeRefusedBuild = Get-Sha256 $receiptPath
    Expect-Rejected 'build-with-tracked-root-targets-production-setting-refused' (Invoke-Isolated @('-File', (Join-Path $base 'scripts\Build-Local.ps1'))) 'different compiler inputs'
    Add-Case 'refused-build-records-no-receipt' ((Get-Sha256 $receiptPath) -eq $receiptBeforeRefusedBuild) "receipt unchanged after the refused build: $((Get-Sha256 $receiptPath) -eq $receiptBeforeRefusedBuild)"
    & git -C $base rm -q --cached -- Directory.Build.targets
    Remove-Item -LiteralPath $rootTargets
    Expect-Rejected 'package-of-the-refused-builds-dll-rejected' (Invoke-Isolated @('-File', $package)) 'does not match the mod DLL'
    $recovered = Invoke-Isolated @('-File', (Join-Path $base 'scripts\Build-Local.ps1'))
    Add-Case 'rebuild-after-tracked-root-targets-removed-reproduces-first-dll' ($recovered.ExitCode -eq 0 -and (Get-Sha256 $dll) -eq $firstDllSha) "exit $($recovered.ExitCode); DLL $(Get-Sha256 $dll)"
    Expect-Accepted 'package-after-tracked-root-targets-removed-accepted' (Invoke-Isolated @('-File', $package)) 'Package validation passed'
    # R7-F1: an applicable configuration file that imports another file is refused, because the imported file is not a
    # fingerprinted input. A tracked root Directory.Build.targets imports the parent Directory.Build.targets above the
    # checkout (GetPathOfFileAbove); the clean build's receipt is current before the root file is added. Ordinary packaging
    # refuses the old receipt, also after only the parent's production-only overflow setting changes, and Build-Local
    # refuses before compiling (DLL and receipt unchanged). With the root file removed the receipt is accepted again
    # without rebuilding; the shadowed-parent control above stays accepted.
    $parentTargets = Join-Path $temp 'Directory.Build.targets'
    [IO.File]::WriteAllText($parentTargets, "<Project></Project>")
    [IO.File]::WriteAllText($rootTargets, "<Project><Import Project=`"`$([MSBuild]::GetPathOfFileAbove('Directory.Build.targets', '`$(MSBuildThisFileDirectory)../'))`" /></Project>")
    & git -C $base add -- Directory.Build.targets
    Expect-Rejected 'package-with-tracked-root-targets-importing-a-parent-refused' (Invoke-Isolated @('-File', $package)) 'Unsupported MSBuild import in Directory\.Build\.targets'
    [IO.File]::WriteAllText($parentTargets, "<Project><PropertyGroup Condition=`"'`$(SkipCompilerExecution)' != 'true'`"><CheckForOverflowUnderflow>true</CheckForOverflowUnderflow></PropertyGroup></Project>")
    Expect-Rejected 'package-reuse-after-imported-parent-production-setting-change-refused' (Invoke-Isolated @('-File', $package)) 'Unsupported MSBuild import in Directory\.Build\.targets'
    $dllBeforeImport = Get-Sha256 $dll; $receiptBeforeImport = Get-Sha256 $receiptPath
    Expect-Rejected 'build-with-tracked-root-targets-importing-a-parent-refused' (Invoke-Isolated @('-File', (Join-Path $base 'scripts\Build-Local.ps1'))) 'Unsupported MSBuild import in Directory\.Build\.targets'
    Add-Case 'refused-import-build-left-dll-and-receipt' ((Get-Sha256 $dll) -eq $dllBeforeImport -and (Get-Sha256 $receiptPath) -eq $receiptBeforeImport -and $dllBeforeImport -eq $firstDllSha) "DLL $(Get-Sha256 $dll) (first DLL $firstDllSha); receipt unchanged $((Get-Sha256 $receiptPath) -eq $receiptBeforeImport)"
    & git -C $base rm -q --cached -- Directory.Build.targets
    Remove-Item -LiteralPath $rootTargets, $parentTargets
    Expect-Accepted 'package-after-importing-root-targets-removed-accepted' (Invoke-Isolated @('-File', $package)) 'Package validation passed'
    # Effective compiler options from the tracked Directory.Build.props (here edited in the disposable checkout). Only the
    # overflow-checking option changes: the stale package is refused, the receipt of a matching rebuild records the
    # effective option, and the rebuilt DLL differs; removing it needs a rebuild again.
    $dirProps = Join-Path $base 'Directory.Build.props'
    $dirPropsOriginal = [IO.File]::ReadAllText($dirProps)
    [IO.File]::WriteAllText($dirProps, $dirPropsOriginal.Replace('</Project>', "  <PropertyGroup><CheckForOverflowUnderflow>true</CheckForOverflowUnderflow></PropertyGroup>`r`n</Project>"))
    Expect-Rejected 'package-reuse-after-overflow-checking-change-rejected' (Invoke-Isolated @('-File', $package)) 'compiler inputs differ'
    $checkedBuild = Invoke-Isolated @('-File', (Join-Path $base 'scripts\Build-Local.ps1'))
    $checkedReceipt = Get-Content -LiteralPath $receiptPath -Raw | ConvertFrom-Json
    $checkedSha = Get-Sha256 $dll
    $effective = @($checkedReceipt.compiler_references | Where-Object { $_.kind -eq 'option' -and $_.name -like '/checked*' } | ForEach-Object name)
    Add-Case 'rebuild-with-overflow-checking-records-effective-option' ($checkedBuild.ExitCode -eq 0 -and $effective -ceq '/checked+' -and $checkedSha -ne $firstDllSha) ("exit $($checkedBuild.ExitCode); receipt records $($effective -join ', '); DLL $checkedSha (committed configuration $firstDllSha)" + $(if ($checkedBuild.ExitCode) { '; ' + (Get-Reason $checkedBuild.Output) } else { '' }))
    Expect-Accepted 'package-after-matching-overflow-checking-rebuild-accepted' (Invoke-Isolated @('-File', $package)) 'Package validation passed'
    [IO.File]::WriteAllText($dirProps, $dirPropsOriginal)
    Expect-Rejected 'package-reuse-after-overflow-checking-removed-rejected' (Invoke-Isolated @('-File', $package)) 'compiler inputs differ'
    $rebuild = Invoke-Isolated @('-File', (Join-Path $base 'scripts\Build-Local.ps1'))
    Add-Case 'rebuild-without-overflow-checking-reproduces-first-dll' ($rebuild.ExitCode -eq 0 -and (Get-Sha256 $dll) -eq $firstDllSha) "DLL $(Get-Sha256 $dll)"
    # Any effective compiler option counts (the exact Csc command line), for example BaseAddress from Directory.Build.props.
    [IO.File]::WriteAllText($dirProps, $dirPropsOriginal.Replace('</Project>', "  <PropertyGroup><BaseAddress>0x11000000</BaseAddress></PropertyGroup>`r`n</Project>"))
    Expect-Rejected 'package-reuse-after-base-address-change-rejected' (Invoke-Isolated @('-File', $package)) 'compiler inputs differ'
    $b = Invoke-Isolated @('-File', (Join-Path $base 'scripts\Build-Local.ps1'))
    $br = Get-Content -LiteralPath $receiptPath -Raw | ConvertFrom-Json
    $baseOption = @($br.compiler_references | Where-Object { $_.kind -eq 'option' -and $_.name -like '/baseaddress:*' } | ForEach-Object name)
    Add-Case 'rebuild-with-base-address-records-it' ($b.ExitCode -eq 0 -and $baseOption -ceq '/baseaddress:0x11000000' -and (Get-Sha256 $dll) -ne $firstDllSha) ("exit $($b.ExitCode); receipt option $($baseOption -join ', '); DLL $(Get-Sha256 $dll)")
    Expect-Accepted 'package-after-base-address-rebuild-accepted' (Invoke-Isolated @('-File', $package)) 'Package validation passed'
    # A file-valued input: an external Win32Resource replaced at the same path invalidates the receipt by content.
    function New-ResFile([string] $Path, [byte[]] $Payload) {
        $ms = [IO.MemoryStream]::new(); $w = [IO.BinaryWriter]::new($ms)
        foreach ($h in @(@(0, 0), @($Payload.Length, 10))) {
            $w.Write([int]$h[0]); $w.Write([int]32); $w.Write([uint16]0xFFFF); $w.Write([uint16]$h[1]); $w.Write([uint16]0xFFFF); $w.Write([uint16]$(if ($h[1]) { 1 } else { 0 }))
            $w.Write([int]0); $w.Write([uint16]$(if ($h[1]) { 0x30 } else { 0 })); $w.Write([uint16]$(if ($h[1]) { 0x0409 } else { 0 })); $w.Write([int]0); $w.Write([int]0)
            if ($h[1]) { $w.Write($Payload); while ($ms.Length % 4) { $w.Write([byte]0) } }
        }
        $w.Flush(); [IO.File]::WriteAllBytes($Path, $ms.ToArray()); $w.Dispose()
    }
    $resDir = Join-Path $temp 'win32 resource'; New-Item -ItemType Directory -Path $resDir | Out-Null
    $res = Join-Path $resDir 'external.res'
    New-ResFile $res ([Text.Encoding]::ASCII.GetBytes('loader-baseline resource A'))
    [IO.File]::WriteAllText($dirProps, $dirPropsOriginal.Replace('</Project>', "  <PropertyGroup><Win32Resource>$([Security.SecurityElement]::Escape($res))</Win32Resource></PropertyGroup>`r`n</Project>"))
    $r1 = Invoke-Isolated @('-File', (Join-Path $base 'scripts\Build-Local.ps1'))
    $resReceipt = Get-Content -LiteralPath $receiptPath -Raw | ConvertFrom-Json
    $resEntry = @($resReceipt.compiler_references | Where-Object { $_.kind -eq 'file-option' -and $_.name -like '/win32res:*' })
    $dllA = Get-Sha256 $dll
    Add-Case 'rebuild-with-external-win32-resource-records-its-content' ($r1.ExitCode -eq 0 -and $resEntry.Count -eq 1 -and $resEntry[0].sha256 -eq (Get-Sha256 $res)) ("exit $($r1.ExitCode); receipt $($resEntry.name) $($resEntry.sha256); DLL $dllA")
    Expect-Accepted 'package-with-external-win32-resource-accepted' (Invoke-Isolated @('-File', $package)) 'Package validation passed'
    New-ResFile $res ([Text.Encoding]::ASCII.GetBytes('loader-baseline resource B, replaced at the same path'))
    Expect-Rejected 'package-reuse-after-win32-resource-replaced-rejected' (Invoke-Isolated @('-File', $package)) 'compiler inputs differ'
    $r2 = Invoke-Isolated @('-File', (Join-Path $base 'scripts\Build-Local.ps1'))
    Add-Case 'rebuild-after-win32-resource-replaced-changes-dll' ($r2.ExitCode -eq 0 -and (Get-Sha256 $dll) -ne $dllA) ("exit $($r2.ExitCode); DLL $(Get-Sha256 $dll) (previous resource: $dllA)")
    Expect-Accepted 'package-after-win32-resource-rebuild-accepted' (Invoke-Isolated @('-File', $package)) 'Package validation passed'
    # R5-F3: an explicit compiler response file (naming the same external resource) is an unsupported form, refused at
    # package admission and at Build-Local, never recorded as text.
    $rsp = Join-Path $resDir 'compiler.rsp'
    [IO.File]::WriteAllText($rsp, "/win32res:`"$res`"`r`n")
    [IO.File]::WriteAllText($dirProps, $dirPropsOriginal.Replace('</Project>', "  <PropertyGroup><CompilerResponseFile>$([Security.SecurityElement]::Escape($rsp))</CompilerResponseFile></PropertyGroup>`r`n</Project>"))
    Expect-Rejected 'package-with-compiler-response-file-refused' (Invoke-Isolated @('-File', $package)) 'Unsupported compiler input: the response file'
    Expect-Rejected 'build-with-compiler-response-file-refused' (Invoke-Isolated @('-File', (Join-Path $base 'scripts\Build-Local.ps1'))) 'Unsupported compiler input: the response file'
    [IO.File]::WriteAllText($dirProps, $dirPropsOriginal)
    # R5-F3: the implicit default response file mode is refused, not partly modelled. A disposable copy of the Roslyn
    # compiler whose default csc.rsp names the external resource, selected with NoConfig disabled: the build still passes
    # /noconfig (Microsoft.CSharp.Core.targets gives the Csc task NoConfig="true"), so the csc.rsp is never read and the
    # build reproduces the first DLL (the resource would change it). The inventory reader refuses that run's own compiler
    # list with /noconfig removed, the only way the implicit mode could reach a receipt.
    $rspCompiler = Join-Path $temp 'compiler with default response file'
    New-Item -ItemType Directory -Path $rspCompiler -Force | Out-Null
    Copy-Item -Path (Join-Path (Join-Path (Split-Path -Parent $msbuild) 'Roslyn') '*') -Destination $rspCompiler -Recurse -Force
    [IO.File]::WriteAllText((Join-Path $rspCompiler 'csc.rsp'), "/win32res:`"$res`"`r`n")
    [IO.File]::WriteAllText($dirProps, $dirPropsOriginal.Replace('</Project>', "  <PropertyGroup><CscToolPath>$([Security.SecurityElement]::Escape($rspCompiler))</CscToolPath><CscToolExe>csc.exe</CscToolExe><NoConfig>false</NoConfig><UseSharedCompilation>false</UseSharedCompilation></PropertyGroup>`r`n</Project>"))
    $rspBuild = Invoke-Isolated @('-File', (Join-Path $base 'scripts\Build-Local.ps1'))
    $rspInventory = @((Get-Content -LiteralPath $receiptPath -Raw | ConvertFrom-Json).compiler_references)
    Add-Case 'default-response-file-is-not-read-because-noconfig-is-always-passed' ($rspBuild.ExitCode -eq 0 -and (Get-Sha256 $dll) -eq $firstDllSha -and @($rspInventory | Where-Object { $_.kind -eq 'option' -and $_.name -ceq '/noconfig' }).Count -eq 1 -and @($rspInventory | Where-Object { $_.name -like '/win32res*' }).Count -eq 0) ("build exit $($rspBuild.ExitCode); DLL $(Get-Sha256 $dll) (first DLL $firstDllSha); receipt has /noconfig: $(@($rspInventory | Where-Object { $_.name -ceq '/noconfig' }).Count -eq 1)")
    $rspList = Join-Path $inputs 'default-response-file-compiler-list.txt'
    Invoke-MSBuildIsolated @($project, '/t:PrepareResources;Compile', '/p:SkipCompilerExecution=true', '/p:Configuration=Release', "/p:OutputPath=$(Join-Path $temp 'rsp-bin')", "/p:CompilerReferenceListPath=$rspList", "/p:IntermediateOutputPath=$(Join-Path $temp 'rsp-obj\')") | Out-Null
    $withoutNoConfig = Join-Path $inputs 'default-response-file-compiler-list-without-noconfig.txt'
    [IO.File]::WriteAllLines($withoutNoConfig, [string[]]@(Get-Content -LiteralPath $rspList | Where-Object { $_ -cne 'arg|/noconfig' }))
    $reader = Invoke-Script (". $(Quote (Join-Path $base 'scripts\Common.ps1')); `$null = ConvertFrom-CompilerInputList $(Quote $rspList); [Console]::Out.WriteLine('WITH-NOCONFIG READ'); `$null = ConvertFrom-CompilerInputList $(Quote $withoutNoConfig); [Console]::Out.WriteLine('WITHOUT-NOCONFIG READ')")
    Add-Case 'inventory-without-noconfig-refused' ($reader.Output -match 'WITH-NOCONFIG READ' -and $reader.Output -match 'Unsupported compiler mode: without /noconfig' -and $reader.Output -notmatch 'WITHOUT-NOCONFIG READ') ($reader.Output.Trim() -replace "`r?`n", ' | ')
    [IO.File]::WriteAllText($dirProps, $dirPropsOriginal)
    $rebuild = Invoke-Isolated @('-File', (Join-Path $base 'scripts\Build-Local.ps1'))
    Add-Case 'rebuild-without-imported-options-reproduces-first-dll' ($rebuild.ExitCode -eq 0 -and (Get-Sha256 $dll) -eq $firstDllSha) "DLL $(Get-Sha256 $dll)"
    # A document restaged after the staging copy cannot reach a published package, and the refusal keeps the previously
    # published package and records unchanged.
    Invoke-Isolated @('-File', $package) | Out-Null
    $published = @((Join-Path $base "artifacts\packages\KingmakerDiceRoller-0.2.0.zip"), (Join-Path $base 'artifacts\packages\package-manifest.json'), (Join-Path $base 'artifacts\packages\package-validation.json'))
    $before = @($published | ForEach-Object { if (Test-Path -LiteralPath $_) { Get-Sha256 $_ } else { 'missing' } }) -join ';'
    $packageText = [IO.File]::ReadAllText($package)
    $stageAnchor = "    foreach (`$entry in `$documents.Keys) { [IO.File]::WriteAllBytes((Join-Path `$stage `$documents[`$entry]), [byte[]]`$documentBytes[`$entry]) }"
    if ($packageText.IndexOf($stageAnchor) -lt 0) { throw 'Package.ps1 staging anchor not found.' }
    $restage = Join-Path $base 'scripts\Package-instrumented.ps1'
    [IO.File]::WriteAllText($restage, $packageText.Replace($stageAnchor, $stageAnchor + "`r`n    [IO.File]::AppendAllText((Join-Path `$stage 'README.md'), ""`r`n<!-- restaged after the staging copy -->`r`n"")"))
    Expect-Rejected 'package-document-restaged-before-archive-rejected' (Invoke-Isolated @('-File', $restage)) 'archived README\.md differs'
    Remove-Item -LiteralPath $restage
    $after = @($published | ForEach-Object { if (Test-Path -LiteralPath $_) { Get-Sha256 $_ } else { 'missing' } }) -join ';'
    Add-Case 'restage-refusal-preserves-published-package-and-records' ($before -notmatch 'missing' -and $before -ceq $after) "published ZIP, manifest and validation report unchanged: $($before -ceq $after)"
    $leftovers = @(Get-ChildItem -LiteralPath (Join-Path $base 'artifacts') -Directory -Force -Filter '.package-*').Count
    Add-Case 'package-private-staging-removed-after-refusal' ($leftovers -eq 0) "private package folders left: $leftovers"
    # The receipt names the effective compiler first (the Csc tool MSBuild runs, by content), then the arguments in order.
    $inv = @((Get-Content -LiteralPath $receiptPath -Raw | ConvertFrom-Json).compiler_references)
    $roslynCsc = Join-Path (Split-Path -Parent $msbuild) 'Roslyn\csc.exe'
    Add-Case 'receipt-records-effective-compiler-first' ($inv.Count -gt 0 -and $inv[0].kind -eq 'compiler' -and $inv[0].sha256 -eq (Get-Sha256 $roslynCsc) -and $inv[1].kind -eq 'option' -and $inv[1].name -eq '/noconfig') ("first entries: $($inv[0].kind) $($inv[0].name) $($inv[0].sha256); $($inv[1].kind) $($inv[1].name)")
    # A failure during publication restores the previously published ZIP and records exactly.
    Invoke-Isolated @('-File', $package) | Out-Null
    $before = @($published | ForEach-Object { Get-Sha256 $_ }) -join ';'
    $packageText = [IO.File]::ReadAllText($package)
    $publishAnchor = '                    [IO.File]::Move($pair[0], $pair[1]); $placed.Add($pair[1])'
    if ($packageText.IndexOf($publishAnchor) -lt 0) { throw 'Package.ps1 publication anchor not found.' }
    $failingPublish = Join-Path $base 'scripts\Package-instrumented.ps1'
    [IO.File]::WriteAllText($failingPublish, $packageText.Replace($publishAnchor, $publishAnchor + "`r`n                    if (`$pair[1] -ceq `$zip) { throw 'injected failure after the new ZIP was placed' }"))
    Expect-Rejected 'package-publication-failure-rejected' (Invoke-Isolated @('-File', $failingPublish)) 'injected failure after the new ZIP was placed'
    Remove-Item -LiteralPath $failingPublish
    $after = @($published | ForEach-Object { if (Test-Path -LiteralPath $_) { Get-Sha256 $_ } else { 'missing' } }) -join ';'
    Add-Case 'publication-failure-restores-previous-package-set' ($before -ceq $after) "published ZIP, manifest and validation report restored: $($before -ceq $after)"
    # R5-F1: the existing validation report held open without delete sharing during ordinary repackaging of a distinct
    # valid replacement (README changed): the publication fails during backup acquisition and all three public files keep
    # their hashes; after the handle closes, a retry succeeds.
    $readmePath = Join-Path $base 'README.md'; $readmeOriginal = [IO.File]::ReadAllBytes($readmePath)
    [IO.File]::AppendAllText($readmePath, "`r`n<!-- distinct replacement -->`r`n")
    $holder = [IO.File]::Open((Join-Path $base 'artifacts\packages\package-validation.json'), [IO.FileMode]::Open, [IO.FileAccess]::Read, [IO.FileShare]::Read)
    try { $held = Invoke-Isolated @('-File', $package) } finally { $holder.Dispose() }
    $afterHeld = @($published | ForEach-Object { if (Test-Path -LiteralPath $_) { Get-Sha256 $_ } else { 'missing' } }) -join ';'
    Add-Case 'held-validation-report-keeps-previous-package-set' ($held.ExitCode -ne 0 -and $afterHeld -ceq $before) ("exit $($held.ExitCode); all three public hashes unchanged: $($afterHeld -ceq $before); " + (Get-Reason $held.Output))
    $retry = Invoke-Isolated @('-File', $package)
    $set = @($published | ForEach-Object { Get-Sha256 $_ })
    $rm = Get-Content -LiteralPath $published[1] -Raw | ConvertFrom-Json; $rv = Get-Content -LiteralPath $published[2] -Raw | ConvertFrom-Json
    Add-Case 'retry-after-handle-closed-publishes-the-replacement' ($retry.ExitCode -eq 0 -and $set[0] -ne ($before -split ';')[0] -and $rm.package_sha256 -eq $set[0] -and $rv.package_sha256 -eq $set[0]) "exit $($retry.ExitCode); ZIP $($set[0])"
    [IO.File]::WriteAllBytes($readmePath, $readmeOriginal)
    # Two overlapping ordinary publishers: both finish, one publication at a time, and the surviving ZIP,
    # validation report and manifest agree with each other.
    $logA = Join-Path $inputs 'publisher-a.txt'; $logB = Join-Path $inputs 'publisher-b.txt'
    $pa = Start-Process -FilePath powershell.exe -ArgumentList @('-NoLogo', '-NoProfile', '-NonInteractive', '-ExecutionPolicy', 'Bypass', '-File', ('"' + $package + '"')) -RedirectStandardOutput $logA -PassThru
    $pb = Start-Process -FilePath powershell.exe -ArgumentList @('-NoLogo', '-NoProfile', '-NonInteractive', '-ExecutionPolicy', 'Bypass', '-File', ('"' + $package + '"')) -RedirectStandardOutput $logB -PassThru
    $null = $pa.Handle; $null = $pb.Handle   # keeps the exit codes readable after the processes end
    $pa.WaitForExit(); $pb.WaitForExit()
    $set = @($published | ForEach-Object { if (Test-Path -LiteralPath $_) { Get-Sha256 $_ } else { 'missing' } })
    $rm = Get-Content -LiteralPath $published[1] -Raw | ConvertFrom-Json; $rv = Get-Content -LiteralPath $published[2] -Raw | ConvertFrom-Json
    Add-Case 'overlapping-publishers-leave-an-agreeing-set' ($pa.ExitCode -eq 0 -and $pb.ExitCode -eq 0 -and $rm.package_sha256 -eq $set[0] -and $rv.package_sha256 -eq $set[0] -and $rm.dll_sha256 -eq $rv.dll_sha256 -and -not (Test-Path -LiteralPath (Join-Path $base 'artifacts\packages\.publish.lock'))) "exits $($pa.ExitCode)/$($pb.ExitCode); ZIP $($set[0]); manifest $($rm.package_sha256); validation $($rv.package_sha256)"
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
    # R4-F2: two reference records folded into one multiline name (equal under the former delimiter text) are refused.
    Edit-Json $receiptPath { param($j)
        $list = [Collections.ArrayList]@($j.compiler_references)
        $i = [Array]::FindIndex([object[]]$list.ToArray(), [Predicate[object]]{ param($e) $e.kind -eq 'reference' })
        $list[$i].name = $list[$i].name + '|' + $list[$i].sha256 + "`n" + $list[$i + 1].kind + '|' + $list[$i + 1].name
        $list[$i].sha256 = $list[$i + 1].sha256
        $list.RemoveAt($i + 1)
        $j.compiler_references = @($list) }
    Expect-Rejected 'package-receipt-with-folded-multiline-reference-name-rejected' (Invoke-Isolated @('-File', $package)) '(control character|entry count)'
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
    $laterCandidateDir = Join-Path $base ('artifacts\candidates\' + $laterCommit)
    # R5-F2: a required report unavailable: nothing is published and no commit directory is left; the retry below succeeds.
    $respec = Join-Path $base 'artifacts\contracts\respec-contracts.json'; $respecBytes = [IO.File]::ReadAllBytes($respec)
    Remove-Item -LiteralPath $respec
    $r = Invoke-Isolated @('-File', $package, '-Candidate')
    Add-Case 'candidate-with-missing-report-leaves-no-commit-directory' ($r.ExitCode -ne 0 -and -not (Test-Path -LiteralPath $laterCandidateDir)) ("exit $($r.ExitCode); commit directory present: $(Test-Path -LiteralPath $laterCandidateDir)")
    # R5-F5: a genuine contract report from another disposable build (the changed-source DLL) is refused before publication.
    Copy-Item -LiteralPath (Join-Path $inputs 'respec-contracts-other-build.json') -Destination $respec
    $r = Invoke-Isolated @('-File', $package, '-Candidate')
    Add-Case 'candidate-with-another-builds-report-refused' ($r.ExitCode -ne 0 -and $r.Output -match 'respec-contracts\.json does not describe this build \(dll_sha256' -and -not (Test-Path -LiteralPath $laterCandidateDir)) (Get-Reason $r.Output)
    [IO.File]::WriteAllBytes($respec, $respecBytes)
    # R5-F4: README.md changed after clean admission, before the documents are read: refused (the candidate's documents
    # must be the admitted commit's checkout bytes); nothing is published.
    $readmeEdit = New-InstrumentedScript 'Package' '$documentDigests = [ordered]@{}' "[IO.File]::AppendAllText((Join-Path `$root 'README.md'), ""`r`n<!-- edited after admission -->`r`n"")"
    $r = Invoke-Isolated @('-File', $readmeEdit, '-Candidate')
    Remove-Item -LiteralPath $readmeEdit -Force
    & git -C $base checkout -q -- README.md
    Add-Case 'candidate-readme-edited-after-admission-refused' ($r.ExitCode -ne 0 -and $r.Output -match "candidate's README\.md differs from the admitted commit" -and -not (Test-Path -LiteralPath $laterCandidateDir)) (Get-Reason $r.Output)
    # R5-F2: a competing same-commit publisher that creates the destination first keeps its files; this one is refused.
    $race = New-InstrumentedScript 'Package' '        New-Item -ItemType Directory -Force -Path (Split-Path -Parent $packageDir) | Out-Null' "New-Item -ItemType Directory -Path `$packageDir | Out-Null; [IO.File]::WriteAllText((Join-Path `$packageDir 'winner.txt'), 'winner')"
    $r = Invoke-Isolated @('-File', $race, '-Candidate')
    Remove-Item -LiteralPath $race -Force
    $winner = @(Get-ChildItem -LiteralPath $laterCandidateDir -Force | ForEach-Object { $_.Name })
    Add-Case 'competing-candidate-publisher-keeps-the-winners-files' ($r.ExitCode -ne 0 -and $r.Output -match 'nothing was overwritten' -and ($winner -join ',') -eq 'winner.txt') ("exit $($r.ExitCode); destination holds: $($winner -join ', ')")
    Remove-Item -LiteralPath $laterCandidateDir -Recurse -Force
    Expect-Accepted 'candidate-accepted-after-build-at-current-commit' (Invoke-Isolated @('-File', $package, '-Candidate')) 'Package validation passed'
    $published8 = @(Get-ChildItem -LiteralPath $laterCandidateDir -File | ForEach-Object { $_.Name } | Sort-Object)
    $expected8 = @('build-provenance.json', 'KingmakerDiceRoller-0.2.0.zip', 'KingmakerDiceRoller.dll', 'native-ui-contracts.json', 'package-manifest.json', 'package-validation.json', 'respec-contracts.json', 'runtime-contracts.json', 'source-qualification.json') | Sort-Object
    Add-Case 'candidate-directory-holds-the-complete-set' (($published8 -join ',') -eq ($expected8 -join ',')) ($published8 -join ', ')
    # R6-F2: a Managed directory inside the checkout, under ignored artifacts: a copy of the disposable Managed copy above,
    # so its Assembly-CSharp.dll is the game's bytes while its UnityEngine.UI.dll keeps the one-bit change made for
    # package-reuse-after-resolved-managed-reference-change-rejected. It tests the repository-local Assembly-CSharp path:
    # the receipt names the game assembly repository-relatively, and the clean build and its matching local candidate
    # are accepted (the conflicting-report refusal is candidate-with-another-builds-report-refused above).
    $localManaged = Join-Path $base 'artifacts\managed copy'
    Copy-Item -LiteralPath $managedCopy -Destination $localManaged -Recurse
    $gamePathBefore = [IO.File]::ReadAllText($gamePath)
    [IO.File]::WriteAllText($gamePath, $gamePathBefore.Replace('</Project>', "  <PropertyGroup><KingmakerManagedDir>$([Security.SecurityElement]::Escape($localManaged))</KingmakerManagedDir></PropertyGroup>`r`n</Project>"))
    $localBuild = Invoke-Isolated @('-File', (Join-Path $base 'scripts\Build-Local.ps1'))
    $gameNames = @((Get-Content -LiteralPath $receiptPath -Raw | ConvertFrom-Json).compiler_references | Where-Object { $_.kind -eq 'reference' -and $_.name -like '*Assembly-CSharp.dll' } | ForEach-Object name)
    Remove-Item -LiteralPath $laterCandidateDir -Recurse -Force
    $localCandidate = Invoke-Isolated @('-File', $package, '-Candidate')
    Add-Case 'candidate-with-repository-local-managed-directory-accepted' ($localBuild.ExitCode -eq 0 -and ($gameNames -join ',') -ceq 'artifacts/managed copy/Assembly-CSharp.dll' -and $localCandidate.ExitCode -eq 0 -and $localCandidate.Output -match 'Package validation passed' -and (Test-Path -LiteralPath (Join-Path $laterCandidateDir 'package-manifest.json'))) ("build exit $($localBuild.ExitCode); receipt game reference '$($gameNames -join ',')'; candidate exit $($localCandidate.ExitCode); " + (Get-Reason $localCandidate.Output))
    [IO.File]::WriteAllText($gamePath, $gamePathBefore)
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
