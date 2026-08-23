# Kingmaker Dice Roller 0.1.1

`0.1.1` is a corrective stable release of the `0.1.0` feature set.

This release does not intentionally change dice mechanics, character-creation support, point-buy restoration, UI behavior, persistence, or compatibility integrations.

## Why this release exists

Unity Mod Manager does not apply Semantic Versioning precedence to prerelease
suffixes. Its parser splits on periods, removes non-digit characters from each
segment, and compares the resulting .NET `Version` values. It therefore reads:

- `0.1.0-alpha.2` as `0.1.0.2`;
- `0.1.0` as `0.1.0`;
- `0.1.1` as `0.1.1`.

That makes the old `0.1.0-alpha.2` archive appear newer than stable `0.1.0`.
Advancing the patch version to `0.1.1` makes the stable package sort correctly
above both earlier archives.

## Engineering changes

- Advanced UMM, runtime product, assembly, package, validation, and release-note
  metadata to `0.1.1`.
- Added a source-qualification regression that models Unity Mod Manager's
  parser and requires `0.1.1` to sort above `0.1.0-alpha.2`.
- Changed package validation to derive its expected version from the repository
  `Info.json` rather than duplicating a hard-coded version.
- Preserved the stable UMM ID `KingmakerDiceRoller` and all gameplay behavior.

## Installation

Download `KingmakerDiceRoller-0.1.1.zip` from the release **Assets** section and
drag that ZIP into Unity Mod Manager. Do not download GitHub's automatic source
archives.

After installation, Unity Mod Manager should display version `0.1.1` and should
not offer `0.1.0-alpha.2` as an update.
