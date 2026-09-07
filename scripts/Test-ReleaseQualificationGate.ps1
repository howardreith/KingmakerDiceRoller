[CmdletBinding()]
param()

. (Join-Path $PSScriptRoot 'ReleaseQualificationGate.ps1')

function Assert-True {
    param(
        [Parameter(Mandatory = $true)][bool] $Condition,
        [Parameter(Mandatory = $true)][string] $Message
    )

    if (-not $Condition) { throw $Message }
}

function Assert-Throws {
    param(
        [Parameter(Mandatory = $true)][scriptblock] $Action,
        [Parameter(Mandatory = $true)][string] $MessagePattern
    )

    try {
        & $Action
    }
    catch {
        if ($_.Exception.Message -notmatch $MessagePattern) {
            throw "Expected error matching '$MessagePattern', got '$($_.Exception.Message)'."
        }
        return
    }

    throw "Expected error matching '$MessagePattern', but no error was raised."
}

$testingState = @'
## Qualification truth
- Runtime-qualified: **No**.
- Source-qualified: **Yes**.
- Contract-qualified: **Yes**.
- Build-qualified: **Yes**.
- Package-qualified: **Yes**.
- Release-authorized: **Yes**.
- Testing-prerelease-authorized: **Yes**.
'@

$tests = [ordered]@{
    AuthorizedTestingPrereleaseDoesNotClaimRuntimeQualification = {
        Assert-PublicationQualification -ProjectStateText $testingState -TestingPrerelease
        Assert-True -Condition ($testingState -match 'Runtime-qualified: \*\*No\*\*') `
            -Message 'Testing publication changed runtime evidence.'
    }
    TestingAuthorizationCannotPublishStableRelease = {
        Assert-Throws -Action {
            Assert-PublicationQualification -ProjectStateText $testingState -ConfirmRuntimeQualified
        } -MessagePattern 'does not mark.*Runtime-qualified: Yes'
    }
    TestingPrereleaseRequiresEveryCurrentQualification = {
        foreach ($label in @('Source-qualified', 'Contract-qualified', 'Build-qualified',
                              'Package-qualified', 'Release-authorized', 'Testing-prerelease-authorized')) {
            $state = $testingState.Replace("$label`: **Yes**", "$label`: **No**")
            Assert-Throws -Action {
                Assert-PublicationQualification -ProjectStateText $state -TestingPrerelease
            } -MessagePattern ([Regex]::Escape("requires current $label`: Yes"))
        }
    }
    HistoricalTestingAuthorizationCannotPublish = {
        $state = "## Qualification truth`n- Runtime-qualified: **No**.`n## Historical record`n" + $testingState
        Assert-Throws -Action {
            Assert-PublicationQualification -ProjectStateText $state -TestingPrerelease
        } -MessagePattern 'requires current Source-qualified: Yes'
    }
    TestingSwitchRemainsExplicit = {
        Assert-Throws -Action {
            Assert-PublicationQualification -ProjectStateText $testingState
        } -MessagePattern 'requires -ConfirmRuntimeQualified'
    }

    FollowingHeadingRenameDoesNotBreakGate = {
        $state = @'
# Project state

## Qualification truth

- Runtime-qualified: **Yes**.

## Immediate human gate

Runtime follow-up.
'@
        $section = Get-QualificationTruthSection -ProjectStateText $state
        Assert-True -Condition ($section -match 'Runtime-qualified:\s+\*\*Yes\*\*') `
            -Message 'Qualification section omitted its runtime status.'
        Assert-True -Condition ($section -notmatch 'Immediate human gate') `
            -Message 'Qualification section included the following level-2 heading.'
        Assert-PublicationQualification -ProjectStateText $state `
            -ConfirmRuntimeQualified
    }
    ArbitraryFollowingHeadingDoesNotBreakGate = {
        $state = @'
## Qualification truth
- Runtime-qualified: **Yes**.
## Publication queue
Publish later.
'@
        Assert-PublicationQualification -ProjectStateText $state `
            -ConfirmRuntimeQualified
    }
    EndOfFileTerminatesQualificationSection = {
        $state = @'
## Qualification truth
- Runtime-qualified: **Yes**.
'@
        Assert-PublicationQualification -ProjectStateText $state `
            -ConfirmRuntimeQualified
    }
    LevelThreeHeadingDoesNotTerminateSection = {
        $state = @'
## Qualification truth
### Runtime evidence
- Runtime-qualified: **Yes**.
## Next gate
Publish later.
'@
        Assert-PublicationQualification -ProjectStateText $state `
            -ConfirmRuntimeQualified
    }
    LaterSectionCannotAuthorizePublication = {
        $state = @'
## Qualification truth
- Runtime-qualified: **No**.
## Next gate
- Runtime-qualified: **Yes**.
'@
        Assert-Throws -Action {
            Assert-PublicationQualification -ProjectStateText $state `
                -ConfirmRuntimeQualified
        } -MessagePattern 'does not mark.*Runtime-qualified: Yes'
    }
    MissingQualificationSectionFailsClosed = {
        $state = @'
## Status
- Runtime-qualified: **Yes**.
'@
        Assert-Throws -Action {
            Assert-PublicationQualification -ProjectStateText $state `
                -ConfirmRuntimeQualified
        } -MessagePattern 'does not contain the current qualification section'
    }
    ConfirmationSwitchRemainsRequired = {
        $state = @'
## Qualification truth
- Runtime-qualified: **Yes**.
'@
        Assert-Throws -Action {
            Assert-PublicationQualification -ProjectStateText $state
        } -MessagePattern 'requires -ConfirmRuntimeQualified'
    }
    RuntimeNoRemainsRejected = {
        $state = @'
## Qualification truth
- Runtime-qualified: **No**.
'@
        Assert-Throws -Action {
            Assert-PublicationQualification -ProjectStateText $state `
                -ConfirmRuntimeQualified
        } -MessagePattern 'does not mark.*Runtime-qualified: Yes'
    }
}

$passed = 0
foreach ($test in $tests.GetEnumerator()) {
    & $test.Value
    $passed++
    Write-Host "PASS $($test.Key)"
}

Write-Host "RESULT $passed/$($tests.Count) passed"
