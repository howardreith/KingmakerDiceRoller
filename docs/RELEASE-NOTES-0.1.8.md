# Roll panel input initialization repair

**Official release at the owner's direction.** The owner will test in play;
rendered interaction, audio and full runtime/compatibility acceptance remain
**NOT RUN** and are disclosed below. Publication does not claim visual
verification in a running character-creation session.

## The 0.1.7 regression

After installing 0.1.7, the Skills page showed the normal 25-point ability
allocator but no Roll Stats access button or dice-rolling interface. The log
recorded two deduplicated warnings — a panel attachment failure and a panel
lifecycle failure, both `NullReferenceException`.

The confirmed cause: `NativeRollPanelHost.CreateInput` created a
`TMP_InputField` and immediately captured fallback styling, including
`input.caretColor`, before creating and binding the input's viewport, text
component and placeholder. The installed game's TextMesh Pro assembly
implements that getter as `customCaretColor ? m_CaretColor :
textComponent.color`, and a freshly added component has
`customCaretColor == false` with a null `textComponent`, so the early read
dereferenced null. The exception escaped construction before the access tab
was created; the handlers detached the partially constructed view and
repeated the failed reconstruction every frame, hidden by log deduplication.
Version 0.1.6 never read the fresh input's caret color, so the defect was
introduced with the 0.1.7 native-theme fallback capture.

## The repair

- `CreateInput` now fully creates and binds the viewport, text component and
  placeholder before reading any caret/selection fallback styling and before
  registering the input theme/fallback binding. Caret color, custom caret
  flag, selection color, blink rate, placeholder styling, custom-expression
  editing and theme recovery behavior are unchanged.
- Attachment and lifecycle failures are reported per operation phase —
  construction versus rendering — with the complete exception, stack trace
  and inner exceptions, under the existing bounded deduplication.
- A deterministic owned-view construction failure stops rebuilding the panel
  after three attempts for one allocator/controller identity. Any allocator
  or session-controller identity change, or a later successful construction,
  reopens construction; readiness transients that fail before view
  construction are not counted against the budget.
- Theme-recovery failures also include the complete exception while retaining
  the owned fallback controls.

## Qualification and limitations

- 375/375 C# behavior cases (five new construction-budget cases) and 30/30
  Python oracle cases passed; 13/13 release-gate cases and the repository
  source validation passed.
- Existing Kingmaker contracts, 11 respec groups and the native UI IL checks
  passed, including three new checks: the installed TMP `caretColor` getter
  dependency, the candidate `CreateInput` binding order (verified to fail
  against the official 0.1.7 DLL as a negative control), and the
  construction-budget wiring.
- A guarded disposable runtime attempt installed the exact release DLL
  through the repository installer (after a `-WhatIf` dry run) and launched
  an owned game process with the lab probe pinned to its hash. The UMM log
  recorded 0.1.8 loading, enabling and `Active` with no Dice Roller startup
  exceptions. The Windows session was disconnected, so screen capture failed
  and no visible input could be sent without bypassing the guard; the owned
  process exited gracefully and the original installation, settings and all
  85 inventoried saves were restored and hash-verified.
- Actual in-game Skills-page appearance, drawer opening, custom-expression
  rolling, reopen/navigation behavior and adjacent roll/assign/Point Buy/INT
  checks are **NOT RUN**; the owner's upcoming in-play test is the acceptance
  lane for them. A passing build or startup does not claim them.
- Mercenary/respec live fixtures, third-party compatibility matrix, audio and
  additional resolutions remain separately **NOT RUN**.
