[CmdletBinding()]
param(
    [ValidateSet('Release')]
    [string] $Configuration = 'Release',

    [string] $ReleaseNotesPath,

    [switch] $Publish,

    [switch] $ConfirmRuntimeQualified,

    [switch] $AllowPrivateRepositoryRelease
)

. (Join-Path $PSScriptRoot 'Common.ps1')
. (Join-Path $PSScriptRoot 'ReleaseQualificationGate.ps1')

function Assert-CommandAvailable {
    param([Parameter(Mandatory = $true)][string] $Name)

    if (-not (Get-Command $Name -ErrorAction SilentlyContinue)) {
        throw "$Name is required but was not found on PATH."
    }
}

function Invoke-NativeCommand {
    param(
        [Parameter(Mandatory = $true)][string] $FilePath,
        [Parameter()][string[]] $Arguments = @()
    )

    & $FilePath @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "$FilePath failed with exit code $LASTEXITCODE."
    }
}

function Get-NativeCommandOutput {
    param(
        [Parameter(Mandatory = $true)][string] $FilePath,
        [Parameter()][string[]] $Arguments = @()
    )

    $output = & $FilePath @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "$FilePath failed with exit code $LASTEXITCODE."
    }

    return (($output | ForEach-Object { [string] $_ }) -join "`n").Trim()
}

function Test-NativeCommand {
    param(
        [Parameter(Mandatory = $true)][string] $FilePath,
        [Parameter()][string[]] $Arguments = @()
    )

    $priorErrorActionPreference = $ErrorActionPreference
    try {
        $ErrorActionPreference = 'SilentlyContinue'
        & $FilePath @Arguments *> $null
        return ($LASTEXITCODE -eq 0)
    }
    finally {
        $ErrorActionPreference = $priorErrorActionPreference
    }
}

$root = Get-RepositoryRoot
Assert-CommandAvailable -Name 'git'
Assert-CommandAvailable -Name 'gh'

Push-Location $root
try {
    $status = Get-NativeCommandOutput -FilePath 'git' -Arguments @(
        'status', '--porcelain'
    )
    if (-not [string]::IsNullOrWhiteSpace($status)) {
        throw 'Release publishing requires a clean working tree.'
    }

    Invoke-NativeCommand -FilePath 'gh' -Arguments @(
        'auth', 'status', '--hostname', 'github.com'
    )

    $repositoryJson = Get-NativeCommandOutput -FilePath 'gh' -Arguments @(
        'repo', 'view',
        '--json', 'nameWithOwner,defaultBranchRef,isPrivate'
    )
    $repositoryInfo = $repositoryJson | ConvertFrom-Json
    $repository = [string] $repositoryInfo.nameWithOwner
    $defaultBranch = [string] $repositoryInfo.defaultBranchRef.name
    $isPrivate = [bool] $repositoryInfo.isPrivate
    if ([string]::IsNullOrWhiteSpace($repository) -or
        [string]::IsNullOrWhiteSpace($defaultBranch)) {
        throw 'GitHub CLI did not return the repository or its default branch.'
    }

    $currentBranch = Get-NativeCommandOutput -FilePath 'git' -Arguments @(
        'rev-parse', '--abbrev-ref', 'HEAD'
    )
    if ($currentBranch -ne $defaultBranch) {
        throw "Release publishing must run from the default branch '$defaultBranch'; current branch is '$currentBranch'."
    }

    Invoke-NativeCommand -FilePath 'git' -Arguments @(
        'fetch', '--prune', '--tags', 'origin', $defaultBranch
    )

    $head = Get-NativeCommandOutput -FilePath 'git' -Arguments @(
        'rev-parse', 'HEAD'
    )
    $remoteHead = Get-NativeCommandOutput -FilePath 'git' -Arguments @(
        'rev-parse', "origin/$defaultBranch"
    )
    if ($head -ne $remoteHead) {
        throw "HEAD ($head) must exactly match origin/$defaultBranch ($remoteHead) before publishing."
    }

    $origin = Get-NativeCommandOutput -FilePath 'git' -Arguments @(
        'remote', 'get-url', 'origin'
    )
    if ($origin -notmatch [Regex]::Escape($repository)) {
        throw "Origin '$origin' does not match GitHub repository '$repository'."
    }

    $infoPath = Join-Path $root 'Info.json'
    Assert-FileExists $infoPath 'Info.json'
    $info = Get-Content -LiteralPath $infoPath -Raw | ConvertFrom-Json
    $id = [string] $info.Id
    $version = [string] $info.Version
    $displayName = [string] $info.DisplayName

    if ($id -ne 'KingmakerDiceRoller') {
        throw "Unexpected UMM ID: $id"
    }
    if ([string]::IsNullOrWhiteSpace($version)) {
        throw 'Info.json does not contain Version.'
    }
    if ($version -notmatch '^\d+\.\d+\.\d+(?:-[0-9A-Za-z.-]+)?(?:\+[0-9A-Za-z.-]+)?$') {
        throw "Info.json Version is not valid semantic version text: $version"
    }
    if ([string]::IsNullOrWhiteSpace($displayName)) {
        $displayName = $id
    }

    if ($Publish) {
        if ($isPrivate -and -not $AllowPrivateRepositoryRelease) {
            throw 'The repository is private. Use -AllowPrivateRepositoryRelease only when a private release is intentional.'
        }

        $projectStatePath = Join-Path $root 'PROJECT-STATE.md'
        Assert-FileExists $projectStatePath 'PROJECT-STATE.md'
        $projectState = Get-Content -LiteralPath $projectStatePath -Raw
        Assert-PublicationQualification `
            -ProjectStateText $projectState `
            -ConfirmRuntimeQualified:$ConfirmRuntimeQualified
    }

    $tag = "v$version"
    $title = "$displayName $tag"
    $existingRelease = $null

    if (Test-NativeCommand -FilePath 'gh' -Arguments @(
        'release', 'view', $tag, '--repo', $repository
    )) {
        $existingReleaseJson = Get-NativeCommandOutput -FilePath 'gh' -Arguments @(
            'release', 'view', $tag,
            '--repo', $repository,
            '--json', 'isDraft,isImmutable,url'
        )
        $existingRelease = $existingReleaseJson | ConvertFrom-Json
        if (-not [bool] $existingRelease.isDraft) {
            throw "Published GitHub release '$tag' already exists. Advance the version instead of replacing it."
        }
        if ([bool] $existingRelease.isImmutable) {
            throw "GitHub release '$tag' is immutable and cannot be refreshed."
        }
    }

    & (Join-Path $PSScriptRoot 'Qualify.ps1') `
        -Build `
        -Package `
        -Configuration $Configuration

    $statusAfterBuild = Get-NativeCommandOutput -FilePath 'git' -Arguments @(
        'status', '--porcelain'
    )
    if (-not [string]::IsNullOrWhiteSpace($statusAfterBuild)) {
        throw "Qualification modified tracked or unignored files:$([Environment]::NewLine)$statusAfterBuild"
    }

    $packageDirectory = Join-Path $root 'artifacts\packages'
    $assetName = "$id-$version.zip"
    $packagePath = Join-Path $packageDirectory $assetName
    Assert-FileExists $packagePath 'Qualified UMM package'

    & (Join-Path $PSScriptRoot 'Validate-Package.ps1') `
        -PackagePath $packagePath `
        -ReportPath 'artifacts\packages\package-validation.json'

    $packageHash = Get-Sha256 $packagePath
    $checksumsPath = Join-Path $packageDirectory 'SHA256SUMS.txt'
    "$packageHash  $assetName" |
        Set-Content -LiteralPath $checksumsPath -Encoding ASCII

    if (Test-NativeCommand -FilePath 'git' -Arguments @(
        'show-ref', '--verify', '--quiet', "refs/tags/$tag"
    )) {
        $tagCommit = Get-NativeCommandOutput -FilePath 'git' -Arguments @(
            'rev-list', '-n', '1', $tag
        )
        if ($tagCommit -ne $head) {
            throw "Existing tag '$tag' resolves to $tagCommit, not release commit $head."
        }
    }
    else {
        Invoke-NativeCommand -FilePath 'git' -Arguments @(
            'tag', '-a', $tag, '-m', $title, $head
        )
    }

    if (-not (Test-NativeCommand -FilePath 'git' -Arguments @(
        'ls-remote', '--exit-code', '--tags', 'origin', "refs/tags/$tag"
    ))) {
        Invoke-NativeCommand -FilePath 'git' -Arguments @(
            'push', 'origin', "refs/tags/$tag"
        )
    }

    $notesLines = @(
        '## Installation',
        '',
        "1. Download **$assetName** from **Assets** below.",
        '2. In Unity Mod Manager, select Pathfinder: Kingmaker and drag the ZIP into the **Mods** tab.',
        "3. Launch the game and confirm **$displayName** is enabled.",
        '',
        "Do not download GitHub's automatically generated **Source code** archives; they are not the installable Unity Mod Manager package.",
        '',
        '## Compatibility',
        '',
        "- Pathfinder: Kingmaker $($info.GameVersion)",
        "- Unity Mod Manager $($info.ManagerVersion)",
        '',
        '## Verification',
        '',
        "SHA-256: $packageHash",
        '',
        "Release commit: $head",
        '',
        'The asset was rebuilt, source-tested, contract-checked, deterministically packaged, and package-validated by the repository qualification pipeline.'
    )

    $notes = $notesLines -join [Environment]::NewLine
    if (-not [string]::IsNullOrWhiteSpace($ReleaseNotesPath)) {
        $resolvedNotesPath = if ([IO.Path]::IsPathRooted($ReleaseNotesPath)) {
            $ReleaseNotesPath
        }
        else {
            Join-Path $root $ReleaseNotesPath
        }
        Assert-FileExists $resolvedNotesPath 'Release notes file'
        $customNotes = (Get-Content -LiteralPath $resolvedNotesPath -Raw).Trim()
        if (-not [string]::IsNullOrWhiteSpace($customNotes)) {
            $notes = $customNotes + [Environment]::NewLine +
                [Environment]::NewLine + $notes
        }
    }

    $generatedNotesPath = Join-Path $packageDirectory "release-notes-$version.md"
    $notes | Set-Content -LiteralPath $generatedNotesPath -Encoding UTF8

    if ($null -eq $existingRelease) {
        $releaseArguments = @(
            'release', 'create', $tag,
            $packagePath,
            $checksumsPath,
            '--repo', $repository,
            '--title', $title,
            '--notes-file', $generatedNotesPath,
            '--verify-tag'
        )
        if ($version.Contains('-')) {
            $releaseArguments += '--prerelease'
            $releaseArguments += '--latest=false'
        }
        if (-not $Publish) {
            $releaseArguments += '--draft'
        }

        Invoke-NativeCommand -FilePath 'gh' -Arguments $releaseArguments
    }
    else {
        Invoke-NativeCommand -FilePath 'gh' -Arguments @(
            'release', 'upload', $tag,
            $packagePath,
            $checksumsPath,
            '--repo', $repository,
            '--clobber'
        )

        $editArguments = @(
            'release', 'edit', $tag,
            '--repo', $repository,
            '--title', $title,
            '--notes-file', $generatedNotesPath,
            '--verify-tag'
        )
        if ($Publish) {
            $editArguments += '--draft=false'
        }
        else {
            $editArguments += '--draft'
        }
        if ($version.Contains('-')) {
            $editArguments += '--prerelease'
            if ($Publish) {
                $editArguments += '--latest=false'
            }
        }
        else {
            $editArguments += '--prerelease=false'
            if ($Publish) {
                $editArguments += '--latest'
            }
        }

        Invoke-NativeCommand -FilePath 'gh' -Arguments $editArguments
    }

    $releaseUrl = Get-NativeCommandOutput -FilePath 'gh' -Arguments @(
        'release', 'view', $tag,
        '--repo', $repository,
        '--json', 'url',
        '--jq', '.url'
    )

    Write-Host "Release: $releaseUrl"
    Write-Host "State: $(if ($Publish) { 'published' } else { 'draft' })"
    Write-Host "Asset: $packagePath"
    Write-Host "SHA-256: $packageHash"
}
finally {
    Pop-Location
}
