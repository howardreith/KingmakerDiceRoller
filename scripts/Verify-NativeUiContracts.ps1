[CmdletBinding()]
param(
    [string] $DllPath = 'artifacts/bin/Release/KingmakerDiceRoller/KingmakerDiceRoller.dll',
    [string] $OutputPath = 'artifacts/contracts/native-ui-contracts.json'
)
. (Join-Path $PSScriptRoot 'Common.ps1')
$root = Get-RepositoryRoot
$config = Get-KingmakerConfiguration
$dll = if ([IO.Path]::IsPathRooted($DllPath)) { $DllPath } else { Join-Path $root $DllPath }
Assert-FileExists $dll 'Candidate DLL'
$resolver = [ResolveEventHandler]{
    param($sender, $eventArgs)
    $name = New-Object Reflection.AssemblyName($eventArgs.Name)
    foreach ($directory in @($config.ManagedDir, $config.UnityModManagerDir)) {
        $candidate = Join-Path $directory ($name.Name + '.dll')
        if (Test-Path -LiteralPath $candidate) { return [Reflection.Assembly]::LoadFrom($candidate) }
    }
    return $null
}
[AppDomain]::CurrentDomain.add_AssemblyResolve($resolver)
try {
    if (-not ('NativeUiContractProbe' -as [type])) { Add-Type -Path (Join-Path $root 'tools\NativeUiContractProbe.cs') }
    $checks = @([NativeUiContractProbe]::Run($config.ManagedDir, $dll))
    $report = [ordered]@{
        status = 'source-contract-qualified'
        checks_passed = $checks.Count
        checks = $checks
        dll_sha256 = Get-Sha256 $dll
        game_assembly_sha256 = Get-Sha256 (Join-Path $config.ManagedDir 'Assembly-CSharp.dll')
        unity_ui_sha256 = Get-Sha256 (Join-Path $config.ManagedDir 'UnityEngine.UI.dll')
        live_visual_audio_interaction = 'NOT RUN'
    }
    $output = Join-Path $root $OutputPath
    New-Item -ItemType Directory -Force -Path (Split-Path $output -Parent) | Out-Null
    $report | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $output -Encoding UTF8
    Write-Host "Native UI IL/contracts: $($checks.Count)/$($checks.Count) passed. Engine interaction/audio is NOT RUN."
}
finally { [AppDomain]::CurrentDomain.remove_AssemblyResolve($resolver) }
