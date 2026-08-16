# Source qualification

## What the validator proves

`tools/validate_repository.py` checks:

- required repository, script, and documentation inventory;
- UMM identity and target metadata;
- JSON/XML/Python parseability and PowerShell lexical/delimiter balance;
- absence of binaries, saves, archives, and extracted assets;
- balanced C# tokens after stripping comments and literals;
- rejection of selected post-C#-7.3 syntax;
- pure-domain dependency separation;
- exactly three narrow Harmony patch installations;
- fixed-array, point-budget restoration, context-guard, and stale-session
  lifecycle invariants;
- exact package allowlist and transactional-install rollback guards;
- minimum C# and Python behavior-test inventories;
- attribution/license preservation;
- explicit qualification disclosure.

## Executed oracle

The Python oracle mirrors the bounded expression grammar, immutable arrays,
position-based swaps, point-buy cost policy, and session-liveness grace rules. It exists so a Linux container
without .NET can execute the behavioral specification.

It is not compiled production code and cannot establish C# build qualification.
The independent C# runner must compile and pass on the Windows build host.

## C# runner

`tests/KingmakerDiceRoller.DomainTests` links the exact production domain source
and executes without a mocking framework or game assembly. It covers parser
bounds, rerolls, keep behavior, nested expressions, all presets, low-score
policies, no-clamp behavior, six-score validation, immutability, duplicate-safe
assignment, point-buy equivalents, saved-array validation, lifecycle rules, and confirmed/unconfirmed stale
session release behavior.

## Fresh handoff result

The source-qualified handoff passed with 54 production/test C# files, 48 C#
domain-runner cases discovered, and all 25 executable Python oracle cases
passing. These counts describe source qualification only.

## Limits

Lexical validation cannot replace Roslyn/MSBuild. Reflection contract source
cannot prove that Howie's exact game build matches. Harmony behavior, preview
refresh, mod ordering, UI behavior, completion, save/reload, and compatibility
remain live-game concerns.
