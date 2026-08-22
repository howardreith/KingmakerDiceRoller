# Source qualification

## Validator scope

`tools/validate_repository.py` checks:

- required source, test, script, and documentation inventory;
- exact `0.1.0-alpha.2` UMM/runtime/assembly version metadata;
- JSON, XML, Python, PowerShell, and C# lexical/parse invariants;
- C# 7.3 compatibility and pure-domain dependency separation;
- absence of binaries, archives, saves, logs, extracted assets, and game files;
- exactly four narrow delegated Harmony postfix surfaces;
- explicit PointBuy-first workflow and no production use of the historical
  diagnostic array;
- transition-time point-buy origin versus generation rollback separation;
- transactional rolled-assignment staging and current-live-preview validation;
- exact native panel contracts, presenter/command separation, and bounded
  attach/rebind/detach lifecycle;
- stable-owner context/liveness guards and all exclusion boundaries;
- saved schema, history, settings, package allowlist, and transactional install
  guards;
- minimum executable C# and Python behavior inventories;
- attribution and honest qualification disclosure.

Source-token checks supplement executable tests; they do not replace them.

## Executable oracles

The Python oracle independently exercises the bounded dice grammar, 1-120
score boundary, immutable arrays, position assignments, standard/extended
point-buy costs, and liveness policy.

The compiled C# runner links production domain/workflow/reflection services and
uses exact-shape fixtures. It covers explicit command/RNG semantics, presets,
policies, parser failures, transactional workflow modes, point-buy origins,
preview generations, assignment, history, saved-record XML/migration, native
presenter routing, panel lifecycle, control suppression/restoration, current
controller verification, disable recovery, and context exclusions.

The exact-contract script loads Howie's installed 2.1.7b assemblies and proves
every game/UI member used by production, including signatures, declaring
types, field types, writable state, and MVID/SHA-256.

## Limits

Source and exact-build qualification cannot prove real Unity layout, click
routing, display scaling, Harmony/mod-order behavior, save/reload, or optional
mod interoperability. Those remain the human gates in `docs/SMOKE-TEST.md`.
