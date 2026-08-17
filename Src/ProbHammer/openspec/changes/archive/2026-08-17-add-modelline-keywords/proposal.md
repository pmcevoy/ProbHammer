## Why

`ModelLine` has no `Keywords` field, so nothing below the whole `Datasheet` can carry a keyword scoped to one specific model. A real datasheet ("Masters of the Maelstrom") lists five named individuals, three sharing one statline and two sharing another, with the keyword `Psyker` scoped to only one of the three that share a statline (`GARLON SOULEATER: PSYKER` on the printed card) — the project's own `ChaosSpaceMarineSquad` fixture has the same five-distinct-model-types shape and the same gap. `KeywordResolution.EffectiveKeywords` also only reads `Datasheet.Keywords` today, so even a `ModelLine.Keywords` field would be invisible to unit-level rule checks without a matching change there.

A separate question surfaced alongside this — whether attachment role (Leader/Support/Bodyguard) needs its own synthesized keyword — turned out less settled than first assumed, and is **not** resolved by this change: BSData does carry `Leader`/`Support` as static keywords on many datasheets (confirmed present on Emperor's Champion even while it sits unattached in an export), but at least one real datasheet (Masters of the Maelstrom) has no `Support` keyword in its own static data despite always requiring attachment as Support in practice — both the GW app and NewRecruit reject it standing alone. Since parsed exports are trusted as already valid (no composition-rule validation, matching this project's existing stance), that specific unit will simply always arrive pre-attached and nothing needs to validate it — but whether our own model should ever synthesize a role keyword for a case like this remains genuinely open. The `Psyker` case is sufficient motivation for `ModelLine.Keywords` on its own, independent of how that question resolves later.

## What Changes

- `ModelLine` gains a `Keywords` property (`IReadOnlySet<string>`, matching `Datasheet.Keywords`'s shape and case-insensitive comparer), defaulting to empty when not supplied.
- `KeywordResolution.EffectiveKeywords` is extended to also union the `Keywords` of every currently-present `ModelLine` (`RemainingCount > 0`) across a combat unit's present components, alongside the existing per-component `Datasheet.Keywords` union.
- Model-level keyword checks (already documented as reading only "that specific model's own component Unit's Keywords") are extended to also read that specific model's own `ModelLine.Keywords` — never a sibling `ModelLine`'s, even within the same component `Unit`.
- `UnitFixtures.cs` gains a new fixture building a `Unit` from the existing `DatasheetFixtures.ChaosSpaceMarineSquad()` (already has the right five-distinct-statline shape): five model-lines, one per named model type, with `Psyker` in only one model-line's own `Keywords` — proving the scoping is per-named-individual, not per-statline-value (`Keywords` lives on `ModelLine`, a Roster-context type, not on `Datasheet`, so the worked example has to be a `Unit`, not a further `Datasheet` fixture).
- **Not in scope**: BSData/JSON catalogue wiring (so `ModelLine.Keywords` stays hand-authored in fixtures, same as every other domain-model piece today), and any decision about whether/how to synthesize a `Support`-style keyword for a datasheet like Masters of the Maelstrom that lacks one statically — that question is unresolved and deliberately left for a later change, once the export parser exists to make it concrete.

## Capabilities

### New Capabilities
(none)

### Modified Capabilities
- `roster-model`: `ModelLine` gains its own `Keywords`; `Attached Unit Keyword Resolution` is extended to include `ModelLine`-level keywords at both the unit-level union and the model-level check.

## Impact

- `src/ProbHammer.Core/Domain/Roster/ModelLine.cs`: new `Keywords` property and constructor parameter.
- `src/ProbHammer.Core/Domain/Roster/KeywordResolution.cs`: `EffectiveKeywords` also unions present `ModelLine.Keywords`.
- `tests/ProbHammer.Tests/Domain/Fixtures/UnitFixtures.cs`: new fixture exercising the per-named-individual keyword case.
- No `/LivePlay` or Web changes — `ModelLine.Keywords` is not rendered anywhere yet.
