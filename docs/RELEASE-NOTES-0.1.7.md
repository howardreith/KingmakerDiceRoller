# Native theme activation repair

**Official release at the owner's direction.** Native themed-path, rendered
appearance/interactions, audio and full runtime/compatibility acceptance remain
**NOT RUN**. The source repair is implemented and qualified; publication does
not claim that the reskin has been visually verified in a running character
creation session.

The 0.1.6 theme failed because its action label is a direct child literally
named `Next/Complete text`. The old lookup interpreted `/` as a hierarchy
separator, then discarded every valid theme resource. The existing attachment
could not recover. Version 0.1.7 repairs that activation path:

- Resolve exact literal child names separately from hierarchy paths, including
  inactive donors, with clear missing/ambiguous/component diagnostics.
- Validate paper, button artwork, typography, input, scrollbar and ornament
  independently, retaining available styling when another capability is missing.
- Apply recovered styling to existing owned controls at the verified allocator
  FillData boundary, capped at the initial attempt plus two retries. Preserve
  the session, unfinished custom input, saved selection and command listeners.

The existing paper/button/font assets, layout intent and ordinary click route
are preserved. No extracted game assets, native callbacks, new UI framework or
additional mod dependency are distributed. Rolling, assignments, saved arrays,
Point Buy restoration, close-time skill-counter synchronization and invalid
forward-navigation guards retain their existing implementation and coverage.
The useless player-facing “Array applied” message remains removed.

## Qualification and limitations

- 370/370 C# behavior cases and 30/30 Python oracle cases passed.
- 13/13 release-gate cases and 18 source-validation groups passed.
- Existing Kingmaker contracts, 11 respec groups and 22/22 native UI IL checks
  passed. Release build has zero warnings/errors; package has six allowed files.
- Deterministic tests execute the production lookup, capability, recovery and
  binding logic. They do not host Unity or prove the live resolver succeeds.
- An authorized guarded startup loaded the reviewed DLL and temporary probe.
  The Windows desktop was disconnected, so the foreground guard refused input
  before genuine character creation. No native resolver/Unity fixtures,
  screenshots, button/audio checks or new INT/skills navigation smoke occurred.
- The owned game exited, the original mod/settings/display preferences were
  restored, and all 85 inventoried saved-game files were unchanged.
- Actual native themed-path resolution, visual/input/audio behavior at supported
  resolutions, live skill-counter/forward guards and populated traits/feats,
  mercenary/respec and finalization/save/reload remain **NOT RUN**.

The merged release DLL must match the reviewed implementation's SHA-256:
`9bd269d9f54f937323ef48285781e4ad67896c06117a4449f8b099fc04ad852d`.
The installable ZIP is rebuilt with this official-release README; its checksum
and exact release commit are recorded below. See `CODEX-NATIVE-THEME-ACTIVATION-STATE.md`
for the full acceptance matrix and evidence paths.
