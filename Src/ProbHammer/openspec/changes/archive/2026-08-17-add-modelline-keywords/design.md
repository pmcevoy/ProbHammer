## Context

See proposal.md - Why. `Datasheet.Keywords` (`src/ProbHammer.Core/Domain/Catalogue/Datasheet.cs`) is already `IReadOnlySet<string>` built with `StringComparer.OrdinalIgnoreCase`. `ModelLine` (`src/ProbHammer.Core/Domain/Roster/ModelLine.cs`) has `StatlineName`, `Weapons`, `Count`, `Abilities`, `RemainingCount` — no `Keywords`. `KeywordResolution.EffectiveKeywords` (`src/ProbHammer.Core/Domain/Roster/KeywordResolution.cs`) is a one-line pure function: `combatUnit.Components.Where(u => u.IsPresent).SelectMany(u => u.Datasheet.Keywords).ToHashSet(...)`. `domain-model-11e.md` already documents a model-level-check convention ("Model-level keyword checks must read a specific `Unit.Datasheet.Keywords` directly, never this union") as prose guidance — there is no dedicated method for it in code today, callers just read `Unit.Datasheet.Keywords` themselves.

The user confirmed BSData's JSON does expose per-model keyword overrides in a structured way (matching what the printed "Masters of the Maelstrom" card shows as `GARLON SOULEATER: PSYKER`), but no BSData/JSON wiring exists anywhere in this project yet — see Non-Goals.

## Goals / Non-Goals

**Goals:**
- Give `ModelLine` its own `Keywords`, same shape and comparer convention as `Datasheet.Keywords`.
- Extend `EffectiveKeywords` so unit-level checks see present `ModelLine.Keywords` too, alongside the existing per-component `Datasheet.Keywords` union.
- Update the model-level-check convention (currently only "read `Unit.Datasheet.Keywords` directly") to also read that specific model's own `ModelLine.Keywords` — never a sibling `ModelLine`'s.
- Prove it against a real per-model-scoped keyword case, reusing `DatasheetFixtures.ChaosSpaceMarineSquad()`'s existing five-distinct-statline shape rather than inventing a new Datasheet fixture.

**Non-Goals:**
- No BSData/JSON catalogue wiring. `ModelLine.Keywords` stays hand-authored in fixtures, same as every other field in this domain model today — enrichment is a separate, not-yet-started area of work.
- No build-time logic that synthesizes a role keyword (e.g. `Support`) when constructing an `AttachedUnit`. BSData already carries `Leader`/`Support` statically on most attachable datasheets (confirmed on Emperor's Champion), so most cases need no synthesis at all — whether the exceptional case (a datasheet like Masters of the Maelstrom with no static `Support` keyword) ever warrants synthesizing one is undecided and left for a later change, once the export parser exists to make the question concrete. This change only gives `ModelLine` somewhere such a keyword could go, if that's ever decided.
- No change to `Datasheet.Keywords` or the `datasheet-catalogue` capability. Datasheet-level keywords are untouched; `ModelLine.Keywords` is a new, independent set.
- No new public API for "the model-level check." The existing convention is callers reading `Unit.Datasheet.Keywords` directly (documented prose, not a method) — this change extends what that convention says to read, not its shape.

## Decisions

- **`ModelLine.Keywords` is `IReadOnlySet<string>` with `StringComparer.OrdinalIgnoreCase`**, mirroring `Datasheet.Keywords` exactly — same concept (keyword membership, not order), same case-insensitivity real 40k keyword text needs.
- **Constructor parameter is optional, defaulting to empty** (`IEnumerable<string>? keywords = null`), so every existing `ModelLine(...)` call site across `Examples/Datasheets.cs`, `Examples/Units.cs`, and existing test fixtures keeps compiling unchanged. Alternative considered: making it required — rejected, that would force touching every existing call site for a feature only one new fixture needs right now.
- **`EffectiveKeywords` folds `ModelLine.Keywords` into the same union**, not a second parallel method — `SelectMany` each present component's `ModelLines.Where(ml => ml.RemainingCount > 0)` alongside the existing `Datasheet.Keywords` selection, into one `ToHashSet`. Alternative considered: a separate `EffectiveModelKeywords`-style method — rejected, the spec already treats unit-level keyword resolution as one union concept, and the requirement text folds `ModelLine`-sourced keywords into it rather than introducing a second query surface.
- **Model-level check semantics stay convention (prose), not a new method** — the existing pattern ("read `Unit.Datasheet.Keywords` directly") is extended to "read `Unit.Datasheet.Keywords` union that specific `ModelLine.Keywords`," still just direct field reads at the call site, not a new `KeywordResolution` API. Matches how the current model-level convention is already just documentation, not code.
- **Worked example lives in `UnitFixtures.cs`, not `DatasheetFixtures.cs`** — `Keywords` is a `ModelLine` (Roster-context) field, and `DatasheetFixtures.ChaosSpaceMarineSquad()` only defines statlines (Catalogue-context). A `Unit` built from that Datasheet, with one model-line per named statline entry and `Psyker` in only one of them, is what actually exercises the new field.

## Risks / Trade-offs

- [Risk] A future caller could put a `ModelLine`'s "own" keyword on the wrong line by accident (e.g. tagging the wrong Chaos Space Marine model type as Psyker) with nothing to catch it structurally, since keyword assignment is just constructor input → Mitigation: the new fixture test asserts the keyword is visible for the tagged model-line's own check and absent for a sibling's, which is the actual failure mode worth guarding, not the general authoring mistake.
- [Risk] `EffectiveKeywords`' unit-level union now silently changes behavior for any existing consumer that assumed it was `Datasheet.Keywords`-only — since no `ModelLine` in the codebase carries a keyword yet (the new fixture is the first), this is a no-op today, but it's worth being explicit that any future keyword check against an `AttachedUnit`/`Unit` will start seeing `ModelLine`-sourced keywords too. → Mitigation: this is exactly the documented intent (see proposal.md - Why), not an accidental side effect.

## Migration Plan

Purely additive — `ModelLine`'s new parameter is optional, `EffectiveKeywords`' signature is unchanged (same return type, same inputs), and no existing fixture or call site needs to change unless it wants to opt into carrying a keyword. No `/LivePlay` or Web impact.
