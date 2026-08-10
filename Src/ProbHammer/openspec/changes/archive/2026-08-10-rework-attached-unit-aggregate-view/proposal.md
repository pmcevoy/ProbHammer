## Why

`render-aggregate-view` built `/LivePlay` against the existing `AttachedUnitAggregateView`, but using it against a real hand-built army (`Examples/Units.cs`, `Examples/Datasheets.cs`) exposed that the aggregate view itself is both buggy and too thin for what it needs to be: a live, table-side reference a player consults mid-game to know current unit composition, remaining models, and total weapon output — replacing the mental aggregation currently forced across two paper datasheets (Bodyguard + attached Leader/Support) every time a model dies. `AttachedUnitAggregator.BuildWeapons` silently drops data when merging weapons contributed by different components (see Bug below), and the view has no way to represent per-model-line casualty counts at all, which is the other half of "live" — a card that can't tell you how many Initiates are left, or which loadout they're carrying, isn't actually usable at the table.

## Bug Being Fixed

`BuildWeapons` groups weapon instances by `WeaponProfileEqualityKey` (Type, Skill, S, Ap, D, and all ability flags — excludes Name, Range, and Attacks), summing `Count` across matches but keeping only the first-inserted profile's `A`:

```csharp
groups[key] = groups.TryGetValue(key, out var existing)
    ? (existing.Profile, existing.Count + modelLine.RemainingCount)   // A silently comes from whichever profile inserted first
    : (profile, modelLine.RemainingCount);
```

Confirmed against the real fixture `Units.SwordBretheren_Marshal()`: the Bodyguard's Sword Brother "Master-crafted power weapon" (A=3, 4 models) and the attached Marshal's "Master-crafted power weapon" (A=7, 1 model) share an `EqualityKey` (same Skill/S/Ap/D/Lethal Hits) and merge into one row reporting Count=5, A=3 — the Marshal's real A=7 is discarded without a trace. This is `render-aggregate-view` task 2.5's exact verification scenario, and it shipped without catching it.

## What Changes

- **Fix weapon Attacks aggregation.** When multiple model-lines' weapons share an `EqualityKey`, compute a true aggregated `TotalAttacks` (a `DiceExpression`, built via `Scale`/`Add` per contributing model-line — `3 models × A4` → `12`, mixed sources sum correctly, e.g. `4×3 + 1×7 = 10`) instead of keeping one contributor's raw `A` and only summing `Count`. The `EqualityKey` itself is unchanged — it already correctly keeps differently-modified copies of a same-named weapon apart (e.g. a Lieutenant's Master-crafted Power Weapon without Lethal Hits already doesn't collide with a Marshal's that has it), verified by hand against `Units.CrusaderSquad_Marshal_Lieutenant()` (Astartes Chainsword → A28 across 7 models; Lethal-Hits Master-crafted Power Weapon → A10 across Marshal+Sword Brother; the Lieutenant's non-Lethal-Hits copy stays separate at A5).
- **Retain per-contribution provenance on weapon entries.** The aggregate view exposes only the total today. Reaching a total is necessary for the headline render (the whole reason to look at a merged row mid-game is "how many dice do I reach for"), but the aggregator must not discard which model-line/component contributed how much — a future UI needs to explode a merged row back into its sources (this matters most for Ranged weapons, where — unlike Melee — a single weapon's attacks must all target one enemy unit, so knowing the per-source breakdown is sometimes necessary, not just informative). This change adds that provenance to the view; it does not build the UI to expose it (see Out of Scope).
- **Add current/initial model counts to statlines.** `AggregateStatlineEntry` gains a remaining/initial count, summed across every model-line sharing that statline name.
- **Add per-model-line loadout breakdown under each statline.** A statline entry currently collapses every model-line sharing its name into one M/T/Sv/W/Ld/Oc row with no memory of which model-lines contributed. This change exposes each contributing model-line (its weapon loadout, and its own remaining/initial count) nested under the statline, e.g. distinguishing the two differently-equipped "Initiate" model-lines in `CrusaderSquadUnitWithChainSwords` instead of flattening them into an undifferentiated "Initiate x5". This is the granularity a future casualty-removal control needs to target (confirmed against a worked example: removing "2 Neophytes and 3 Initiates, where 2 of the Initiates carry a Power Fist" from a squad requires knowing exactly this breakdown to correctly decrement weapon counts — today's view can't answer "which Initiates have the Power Fist" because that distinction is discarded before the view is built).
- **Render `Statline.InSv`.** The invulnerable save field already exists on `Statline` (set on Marshal and High Marshal Helbrecht in `Datasheets.cs`) but is unrendered anywhere and undocumented in `.claude/domain-model-11e.md`. This change surfaces it on `/LivePlay` and corrects the docs.
- **Remove the by-value statline collapsing added in `render-aggregate-view`.** `LivePlay.cshtml.cs`'s `GroupStatlinesByValue` merges display rows whose M/T/Sv/W/Ld/Oc values coincide (e.g. "Assault Intercessor"/"Assault Intercessor Sergeant"). With current/initial counts and per-model-line breakdown now in play, this collapsing becomes actively harmful — e.g. it would merge "Initiate" and "Sword Brother" in `CrusaderSquad_Marshal_Lieutenant()` (both M6/T4/Sv3+/W2/Ld6+/Oc2) into one row despite them being different roles that must be tracked separately for casualty removal. **BREAKING** (behavior change to the `live-play-view` capability's Statline Section Rendering requirement).
- **Update `/LivePlay`'s render minimally** to show the new statline (loadout breakdown, InSv, counts) and weapon (total attacks, no more separate raw-A + Count columns) shapes. No visual redesign — plain tables, matching the existing page's current style.
- **Fix `WeaponProfile.EqualityKey()` to include every ability/keyword flag.** Discovered by manually checking `/LivePlay` against this change's own Scout Squad fixture: `EqualityKey()` omitted `Pistol`, `Assault`, and `IgnoresCover`, so a Bolt pistol (A1, Pistol) and an Astartes shotgun (A2, Assault) — identical on Type/Skill/S/AP/D — merged into one row (5 × A1 + 4 × A2 = 13), a structurally-wrong-but-plausible-looking total. This was a pre-existing gap (present since the profile-equality concept was introduced) that the more-correct `TotalAttacks` math in this change made visible instead of obviously broken. `EqualityKey()` now includes all three fields, matching the requirement text's existing "and Abilities" wording, which was already correct — the implementation just didn't fully satisfy it. **BREAKING** (behavior change to the `attached-unit-tracker` capability's Aggregate Weapon Count View requirement: weapons that previously merged under the incomplete key may now render as separate rows).

## Out of Scope (explicitly deferred)

- Any GW-datasheet-style visual redesign of `/LivePlay` — parked entirely; this change proves the new data with plain markup only.
- Building the Ranged/Melee provenance-breakdown UI (a toggle or drill-down showing which components contributed to a merged total) — the view carries the data this needs; the interaction is future work.
- Session/state persistence for casualty tracking across page loads — `LivePlay.OnGet()` continues to rebuild a fresh army every request via `Examples.View.MyArmy()`.
- Ability-driven attack modifiers (e.g. a unit-wide "+1 Attack to melee weapons" ability changing a total from A28 to A35).
- Splitting `Keywords` into unit-wide-union vs. per-component, or adding `FactionKeywords` — left as the existing unioned `IReadOnlySet<string>`.
- Any `Simulation`/`CombatSimulator` integration — `/LivePlay` remains a static-per-request reference view, not a dice-rolling tool.
- A UI/API for actually removing casualties (calling `ModelLine.RemoveCasualties`) — this change makes the view capable of showing the right granularity for that; wiring an interactive control is separate follow-on work.

## Capabilities

### Modified Capabilities
- `attached-unit-tracker`: "Aggregate Weapon Count View" requirement changes from summing counts (with an unspecified/incorrect representative Attacks value) to aggregating a true total Attacks and retaining per-contribution provenance. "Aggregate Statline View" requirement gains current/initial model counts and a per-model-line loadout breakdown nested under each statline entry.
- `live-play-view`: "Statline Section Rendering" requirement changes from by-value collapsing to one row per statline name with a nested per-model-line breakdown; "Weapon Section Rendering" requirement changes from raw-A-plus-Count to a single aggregated total-Attacks column; both requirements pick up the new current/initial count and InSv fields.

### New Capabilities
- None.

## Impact

- **Changed**: `src/ProbHammer.Core/Domain/Roster/AttachedUnitAggregateView.cs` (`AggregateStatlineEntry`, `AggregateWeaponEntry` shapes), `src/ProbHammer.Core/Domain/Roster/AttachedUnitAggregator.cs` (`BuildStatlines`, `BuildWeapons`), `src/ProbHammer.Web/Pages/LivePlay.cshtml` + `.cshtml.cs`, `src/ProbHammer.Core/Domain/Catalogue/WeaponProfile.cs` (`EqualityKey()`/`WeaponProfileEqualityKey` gain `Pistol`/`IgnoresCover`/`Assault`).
- **Unchanged**: `Datasheet`, `Unit`, `AttachedUnit`, `KeywordResolution`, `ToughnessResolution`, `Examples/View.cs`, `Index`/`ArmyView`, `Enricher`, `Simulation/*` (`SimulationAdapter.WeaponGroupKey` is a conceptually-parallel but separate type in the 10e model; not touched by this change).
- **Tests**: `tests/ProbHammer.Tests/Domain/Roster/AttachedUnitAggregatorTests.cs` gains cases for the Attacks-aggregation fix and the new statline/loadout fields; existing tests for ability/keyword behavior are unaffected.
- **Docs**: `.claude/domain-model-11e.md` updated for `Statline.InSv`, the revised `AggregateStatlineEntry`/`AggregateWeaponEntry` shapes, and the corrected weapon-aggregation rule.
- **Housekeeping**: `render-aggregate-view` is fully implemented and committed but not yet archived; archiving it (so `live-play-view` becomes canonical in `openspec/specs/`) is a natural companion step to this change, not a blocker for drafting it.
