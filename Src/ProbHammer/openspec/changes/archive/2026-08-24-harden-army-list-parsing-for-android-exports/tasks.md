## 1. Case-insensitive metadata/section matching

- [x] 1.1 Make `NamePointsRegex` match the `Points`/`points` suffix case-insensitively.
- [x] 1.2 Make the `"ATTACHED UNITS"` section check and `AttachedUnitGroupRegex` match
      case-insensitively (accepting `"Attached Units"` / `"Attached Unit N"` too).
- [x] 1.3 Unit tests: army header and battle-size lines with a lower-case `points` suffix; an
      `"Attached Units"` / `"Attached Unit 1"` (title-case) export parses identically to the
      `"ATTACHED UNITS"` / `"Attached unit 1"` form.

## 2. Bullet-continuation rule

- [x] 2.1 In `CollectBulletBlocks`, recognize an unbulleted line — once at least one bulleted line
      has already been collected for the current block — as a further weapon-count entry of the
      current (innermost open) list, rather than falling through to `else { break; }`.
- [x] 2.2 Confirm the rule applies uniformly at both existing call sites that reach
      `CollectBulletBlocks`/its nested-weapon parsing: a unit's own direct weapon list and a model
      group's nested weapon list — no depth-specific branching.
- [x] 2.3 Confirm an unbulleted line at the *start* of a unit's bullet block (before any bulleted
      line has been seen) still falls through to unit-header parsing unchanged, preserving today's
      diagnostic for a genuine malformed export.
- [x] 2.4 Unit tests: a single-model unit's direct weapon list with one continuation line (Daemon
      Prince shape); a single-model unit's direct weapon list with four consecutive continuation
      lines, all at the same indent (Defiler shape); a model group's nested weapon list with a
      continuation line two tiers deep (Custodian Wardens' `Guardian spear`/`Vexilla` shape,
      asserting the resulting counts — this one is expected to still fail via the existing
      unpartitionable-counts diagnostic, per design.md, so assert the failure and its message
      rather than a successful parse).

## 3. Real-export regression coverage

- [x] 3.1 Add `data/gw-android-export-custodes.txt` and `data/gw-android-export-deathguard.txt` as
      fixtures (same convention as the existing `data/gw-app-export*.txt` files already used by
      `ArmyListParserTests`).
- [x] 3.2 Add a test parsing `gw-android-export-deathguard.txt` end-to-end and asserting the full
      resulting `ParsedArmyList` shape (this file has no attached units and no partition
      ambiguity, so it should fully succeed after tasks 1-2). Required a third fix beyond tasks
      1-2 (`ForceDisposition` treated as optional — see design.md); confirmed with the user before
      implementing.
- [x] 3.3 Add a test parsing `gw-android-export-custodes.txt` and asserting: `Blade Champion` and
      `Allarus Custodians` parse successfully; `Custodian Wardens` and `Custodian Guard` fail with
      `ArmyListParseException` naming the unit and raw wargear text (per design.md's deferred
      Custodian Guard ambiguity). A whole-file parse only ever reaches the first failure
      (`Custodian Wardens`, inside `Attached unit 1`), so the per-unit success/failure assertions
      use fragments extracted verbatim from the real file and parsed in isolation.

## 4. Docs

- [x] 4.1 Update `.claude/domain-model-11e.md`'s "Army List Parsing" section to describe the
      case-insensitive matching and the bullet-continuation rule.
- [x] 4.2 Update `PROGRESS.md` with a summary of this change, its real-export findings, and the
      deferred Custodian Guard ambiguity (pointing at this change's design.md for the full
      investigation).
