# Guarded native-theme runtime verification

The current repair owner authorized the installed Kingmaker and Steam on this
development machine. Historical offline-only restrictions in earlier handoffs
apply to those missions. Use a fresh, owned new-character creation session;
never Continue, load, respec or overwrite a valued campaign. Mercenary/respec
smoke tests require a separate disposable campaign fixture.

Before installation, ensure no Kingmaker process or foreign runtime lock is
active. Record the clean candidate commit and package/DLL hashes. Back up the
entire installed Dice Roller directory, its settings and UMM Params.xml;
record hashes, reject redirected paths, and verify the installed DLL still
matches the recorded baseline. Preserve existing mod enablement. Inventory
save hashes read-only to verify they remain unchanged. Do not alter Cloud.
Install only the validated candidate and the separate temporary probe below.
Own the launched PID/start time; restrict all input to that process/window.
Capture evidence outside Git. After exiting that owned process, archive test
output and restore the exact original mod/settings/configuration. Verify hashes
and remove only the probe directory created by this test.

## Temporary probe

`Build-NativeThemeRuntimeProbe.ps1` builds `NativeThemeRuntimeProbe.cs` against
the installed Unity/UMM contracts. This is a lab-only module and never part of
the six-file candidate ZIP or a dependency of Dice Roller. Its separate UMM ID
is `KingmakerDiceRollerNativeThemeProbe`. Use only an unoccupied temporary mod
directory for its output DLL and Info.json; remove it after testing.

An owned game launch must inherit `KDR_THEME_PROBE_DIRECTORY` (an existing
local request/evidence directory) and `KDR_THEME_PROBE_SHA256` (candidate DLL
hash). Without these it declines to load. Requests in `request.txt` contain
`<owned-game-PID>|<report-id>|inspect` or `...|fixtures`. IDs contain only
letters/digits/hyphens. Every accepted request rechecks the actual loaded mod
assembly's file hash. It uses the mod host's exact active allocator; enter
new-character Skills first. Reports record candidate hash/MVID, the result of
actual `NativeBookTheme.Resolve`, each donor identity, attached capability
state, owned control geometry/fonts/sprites/listener counts and session state.

`fixtures` first requires fully validated live donors. It creates an inactive
probe-owned Unity hierarchy and exercises the shipped resolver/Unity adapter,
literal-vs-path, ambiguous/missing/wrong components, independent optional
fallback, delayed readiness, bounded retries, retained control identity/draft,
single listener and destroyed-donor teardown. It destroys only those fixture
objects. It never renames, removes or otherwise damages native UI donors and
never invokes a gameplay/navigation command. Deterministic domain/coordinator
tests cover RNG, saved-array and skill-navigation invariants separately.

## Evidence and acceptance

Run the final clean candidate after the last production change. Open from the
native access tab without generating a roll. Inspect the real paper, button
normal/hover/held/disabled states, role typography, input/scroll clipping,
readability, access geometry and ordinary click audio. Capture normal and an
additional supported resolution when available. Exercise roll modes, draft,
Store/Previous/Next/Recall/Delete, close/reopen and Point Buy restoration.
Allocate skills, change INT in both directions, close and immediately check
the remaining-points badge and forward veto; restore valid allocation and
inspect populated traits/feats. Keep evidence attached to the exact DLL hash.

Report separately: source repair implemented, native themed path verified,
rendered appearance/interactions inspected, audio, compatibility and remaining
entry points. Probe/IL success alone cannot pass rendered or audible behavior.
