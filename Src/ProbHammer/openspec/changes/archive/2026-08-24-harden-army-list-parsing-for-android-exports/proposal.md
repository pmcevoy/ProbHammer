## Why

`ArmyListParser` was built and verified only against iOS-captured GW-app exports
(`data/gw-app-export*.txt`). Two real Android exports (`data/gw-android-export-custodes.txt`,
`data/gw-android-export-deathguard.txt` — different app versions, different players, both relayed
over WhatsApp) fail to parse at all: a case-sensitivity mismatch in the army-metadata/section
matchers, and a bullet-rendering convention the parser has no way to interpret, cause either an
outright `ArmyListParseException` or a silently wrong model-group weapon list. Investigated in
detail (see this change's own exploration) against both files: the failures are consistent across
both independent samples, not one-off corruption, and are mechanically fixable without touching the
parser's existing partition logic.

## What Changes

- Case-insensitive matching for the `points`/`Points` cost suffix (army header and battle-size
  lines) and for the `Attached Units`/`ATTACHED UNITS` section marker and `Attached Unit N`/
  `Attached unit N` group marker. The four top-level section headers (`CHARACTERS`, `BATTLELINE`,
  `DEDICATED TRANSPORTS`, `OTHER DATASHEETS`) are unaffected — both Android samples already render
  these identically to iOS.
- A new bullet-continuation rule, applied uniformly at every nesting depth: within one weapon list
  (whether the unit's own direct weapons, or a specific model-group's nested weapons), only the
  first item carries a `•` bullet; every subsequent item in that same list is a plain line with no
  bullet, indented to align with the first item's own text. The parser must recognize such a line as
  a further item of the *current* list rather than either a nested sub-list (today's `◦`-only
  nesting assumption) or, worse, a new unit header (today's actual failure mode: `CollectBulletBlocks`
  gives up on an unbulleted line and the outer loop tries to parse it as `"<Name> (<N> Points)"`,
  throwing a confusing diagnostic that names the wrong thing).
- `ForceDisposition` is now optional: `gw-android-export-deathguard.txt`'s preamble omits that line
  entirely (goes straight from the Detachment line to the BattleSize line) - a third, independent
  finding surfaced only while verifying tasks 1-2 against this file end-to-end, confirmed with the
  user before implementing since it falls outside the two causes this change originally scoped to.
  See design.md's "ForceDisposition is optional".
- **Explicitly out of scope**: one confirmed real ambiguity (Custodian Guard's `1x Guardian spear` /
  `3x Praesidium Shield` / `3x Sentinel blade`, once the bullet-continuation rule flattens them into
  one list) cannot be resolved from text structure alone — the existing "remaining weapon counts
  must sum to the model group's total" partition rule already fails loudly on it (same diagnostic
  path as today's Company-Veteran-shaped case), and this change does not attempt to guess a fix.
  See design.md for the full investigation and why a heuristic was rejected for now.

## Capabilities

### New Capabilities

(none)

### Modified Capabilities

- `army-list-parsing`: `ArmyListParser`'s header/metadata matching becomes case-insensitive for the
  points-suffix and attached-unit markers; bullet-block collection gains the continuation-line rule
  described above. No change to the existing partition algorithm's own behavior or its failure mode.

## Impact

- `src/ProbHammer.Core/Domain/Import/ArmyListParser.cs`: `NamePointsRegex`
  (points-suffix case-insensitivity), the `"ATTACHED UNITS"`/`AttachedUnitGroupRegex` matchers
  (case-insensitivity), `CollectBulletBlocks` (continuation-line recognition, plus each tokenized
  line now carries its own leading-indent count alongside its trimmed content, since the
  continuation rule needs it), and `Parse` (`ForceDisposition` now optional).
- `tests/ProbHammer.Tests/Domain/Import/ArmyListParserTests.cs`: new cases from real captured
  Android export excerpts, plus hand-built fixtures for the case-insensitivity and
  bullet-continuation rules.
- No change to `ArmyRosterEnricher`, `Domain.Catalogue.Bsdata`, or `/LivePlay` — this is confined to
  the text-parsing stage, upstream of BSData resolution.
