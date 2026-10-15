function Get-QualificationTruthSection {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [AllowEmptyString()]
        [string] $ProjectStateText
    )

    $heading = [Regex]::Match(
        $ProjectStateText,
        '(?m)^##[ \t]+Qualification truth[ \t]*\r?$')
    if (-not $heading.Success) {
        throw 'PROJECT-STATE.md does not contain the current qualification section.'
    }

    $followingStart = $heading.Index + $heading.Length
    $followingText = $ProjectStateText.Substring($followingStart)
    $nextLevelTwoHeading = [Regex]::Match(
        $followingText,
        '(?m)^##[ \t]+[^\r\n]+\r?$')
    $sectionEnd = if ($nextLevelTwoHeading.Success) {
        $followingStart + $nextLevelTwoHeading.Index
    }
    else {
        $ProjectStateText.Length
    }

    return $ProjectStateText.Substring(
        $heading.Index,
        $sectionEnd - $heading.Index)
}

function Assert-PublicationQualification {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [AllowEmptyString()]
        [string] $ProjectStateText,

        [switch] $ConfirmRuntimeQualified,

        [switch] $TestingPrerelease,

        [switch] $ConfirmOwnerAuthorizedPreRuntimeRelease,

        [string] $Version
    )

    $dispositions = @(@([bool] $ConfirmRuntimeQualified, [bool] $TestingPrerelease,
        [bool] $ConfirmOwnerAuthorizedPreRuntimeRelease) | Where-Object { $_ })
    if ($dispositions.Count -gt 1) {
        throw 'Choose exactly one publication disposition: -ConfirmRuntimeQualified, -TestingPrerelease or -ConfirmOwnerAuthorizedPreRuntimeRelease.'
    }

    if ($ConfirmOwnerAuthorizedPreRuntimeRelease) {
        # Owner release approval is separate from runtime evidence: this path
        # publishes a stable release only while the current section still says
        # Runtime-qualified: No, and only for the exact authorized version.
        if ([string]::IsNullOrWhiteSpace($Version)) {
            throw 'The owner-authorized pre-runtime path requires the release version.'
        }
        $qualification = Get-QualificationTruthSection -ProjectStateText $ProjectStateText
        foreach ($label in @('Source-qualified', 'Contract-qualified', 'Build-qualified',
                              'Package-qualified', 'Release-authorized')) {
            $pattern = '(?m)^- ' + [Regex]::Escape($label) + ':\s+\*\*Yes\*\*'
            if ($qualification -notmatch $pattern) {
                throw "Owner-authorized pre-runtime release requires current $label`: Yes."
            }
        }
        if ($qualification -notmatch '(?m)^- Runtime-qualified:\s+\*\*No\*\*') {
            throw 'Owner-authorized pre-runtime release requires current Runtime-qualified: No; use -ConfirmRuntimeQualified for a runtime-qualified release.'
        }
        # The authorization bullet may wrap onto indented continuation lines,
        # but the version must appear inside that same bullet.
        $authorization = [Regex]::Match(
            $qualification,
            '(?m)^- Owner-authorized pre-runtime release:\s+\*\*Yes\*\*[^\r\n]*(?:\r?\n[ \t]+[^\r\n]*)*')
        $versionPattern = '(?<![0-9.])v?' + [Regex]::Escape($Version) + '(?![0-9]|\.[0-9])'
        if (-not $authorization.Success -or $authorization.Value -notmatch $versionPattern) {
            throw "PROJECT-STATE.md does not record current Owner-authorized pre-runtime release: Yes for $Version."
        }
        return
    }

    if ($TestingPrerelease) {
        $qualification = Get-QualificationTruthSection -ProjectStateText $ProjectStateText
        foreach ($label in @('Source-qualified', 'Contract-qualified', 'Build-qualified',
                              'Package-qualified', 'Release-authorized', 'Testing-prerelease-authorized')) {
            $pattern = '(?m)^- ' + [Regex]::Escape($label) + ':\s+\*\*Yes\*\*'
            if ($qualification -notmatch $pattern) {
                throw "Testing prerelease requires current $label`: Yes."
            }
        }
        return
    }

    if (-not $ConfirmRuntimeQualified) {
        throw 'Public publication requires -ConfirmRuntimeQualified or -ConfirmOwnerAuthorizedPreRuntimeRelease.'
    }

    $qualification = Get-QualificationTruthSection `
        -ProjectStateText $ProjectStateText
    if ($qualification -notmatch 'Runtime-qualified:\s+\*\*Yes\*\*') {
        throw 'PROJECT-STATE.md does not mark the current candidate Runtime-qualified: Yes.'
    }
}
