[CmdletBinding()]
param()
. (Join-Path $PSScriptRoot 'Common.ps1')
$root = Get-RepositoryRoot
$config = Get-KingmakerConfiguration
$probe = Join-Path $root 'artifacts\theme-activation\probe'
New-Item -ItemType Directory -Force -Path $probe | Out-Null
$compiler = Join-Path (Split-Path (Resolve-MSBuild) -Parent) 'Roslyn\csc.exe'
Assert-FileExists $compiler 'C# compiler'
$references = @('Assembly-CSharp.dll', 'Assembly-CSharp-firstpass.dll', 'UnityEngine.dll',
    'UnityEngine.CoreModule.dll', 'UnityEngine.UI.dll', 'UnityEngine.UIModule.dll', 'UnityEngine.TextRenderingModule.dll')
$arguments = @('/nologo', '/target:library', '/langversion:7.3', '/deterministic+', '/warnaserror+',
    ('/out:' + (Join-Path $probe 'KingmakerDiceRoller.NativeThemeProbe.dll')))
foreach ($reference in $references) { $arguments += '/reference:' + (Join-Path $config.ManagedDir $reference) }
$arguments += '/reference:' + (Join-Path $config.UnityModManagerDir 'UnityModManager.dll')
$arguments += Join-Path $root 'tools\NativeThemeRuntimeProbe.cs'
& $compiler @arguments
if ($LASTEXITCODE -ne 0) { throw 'Native runtime probe compilation failed.' }
$info = [ordered]@{ Id = 'KingmakerDiceRollerNativeThemeProbe'; DisplayName = 'Dice Roller Native Theme Probe (lab only)'; Author = 'Kingmaker Dice Roller'; Version = '0.0.1'; ManagerVersion = '0.33.0'; AssemblyName = 'KingmakerDiceRoller.NativeThemeProbe.dll'; EntryMethod = 'NativeThemeRuntimeProbe.Load' }
$info | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $probe 'Info.json') -Encoding UTF8
Write-Host "Lab probe compiled: $probe. Not installed or included in the candidate package."
