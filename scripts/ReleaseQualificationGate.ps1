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

        [switch] $ConfirmRuntimeQualified
    )

    if (-not $ConfirmRuntimeQualified) {
        throw 'Public publication requires -ConfirmRuntimeQualified.'
    }

    $qualification = Get-QualificationTruthSection `
        -ProjectStateText $ProjectStateText
    if ($qualification -notmatch 'Runtime-qualified:\s+\*\*Yes\*\*') {
        throw 'PROJECT-STATE.md does not mark the current candidate Runtime-qualified: Yes.'
    }
}
