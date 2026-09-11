## Why

`resolve-weapon-characteristic-effects` (Phase 3, archived 2026-09-11) mutates a weapon's `S`/`Ap`/`D`
`ScalarCharacteristicView` when a matched, non-caveated, bearer-scoped baseline
`WeaponCharacteristicEffect` applies, and `BuildWeapons` groups by the resulting `WeaponProfileEqualityKey`
so a reached/unreached split falls out for free — but `/LivePlay`'s weapon table renders
`@weapon.S.Value`/`.Ap.Value`/`.D.Value` as bare numbers with no indication *why* a value is what it
is, mirroring exactly the gap the Statline family closed via "Flagged Statline Characteristic
Rendering" (`resolve-known-ability-effects`). Worse: a **caveated** weapon-characteristic baseline
entry is currently invisible end-to-end — `AttachedUnitAggregator.TryGetWeaponEffectEntry` filters
`IsCaveated: false` at the lookup itself, so a caveated match (15 of the 19 real baselined entries,
including well-known abilities like Chance for Glory/Brutal Raider/Zealot) leaves no trace anywhere
a renderer could find it, unlike the Statline family where a caveated match still populates
`ContributingAbilities` for the legend mechanism to pick up. This change closes both gaps: it renders
a resolved weapon-characteristic value's source, and it surfaces a caveated match so a player at
least sees "this weapon has an unresolved ability affecting it" instead of nothing at all.

## What Changes

- `AttachedUnitAggregator.BuildWeapons`/`ResolveContributionProfile` widen to also identify a
  caveated-but-otherwise-matching `WeaponCharacteristicEffect` per contribution (bearer-scoped,
  selector-matched, non-`A` characteristic) without applying it — a new, additive signal alongside
  the existing apply-if-non-caveated behavior, not a replacement for it.
- `AggregateWeaponEntry`/`WeaponContribution` (exact shape decided in design.md) carry this
  unresolved-contribution signal through to the rendering layer, the weapon-side counterpart to
  `ScalarCharacteristicView.ContributingAbilities` already carrying a Statline field's resolved
  source.
- `/LivePlay`'s weapon table renders:
  - An asterisk-style marker appended to a weapon entry's own name when any contribution in its
    group carries an unresolved (caveated) ability contribution — mirrors the Statline `.stat-label`
    marker convention, scoped to the group name since the weapon table has no per-characteristic
    label the way a Statline tile does.
  - An inline marker + `--amber-tint` styling on the specific `S`/`AP`/`D` value cell whose
    `ScalarCharacteristicView` carries a resolved `ContributingAbilities` entry (e.g. "5*").
  - A legend mechanism naming the marker's source ability as an interactive popover trigger — reuses
    the existing popover mechanism (`BuildRulePopover`) every ability name on the page already uses.
    Marker assignment reuses `LivePlayModel.AssignFlagMarkers`'s existing per-unit-block algorithm
    (first-distinct-source-gets-`*`, reused wherever the same source recurs), extended to also walk
    weapon entries alongside the existing Statline scalar/InSv sources — one shared marker registry
    per unit block, not a separate one for weapons (see design.md for why).
- `ShowsBreakdownTrigger` (currently `> 1 ModelLine` only) widens so a weapon entry with an
  unresolved/resolved ability contribution also shows its contribution breakdown trigger, even on a
  single-`ModelLine` unit, so the affected contribution is reachable.

### Non-Goals (named for a later change, not built here)

- Resolving an Attacks (`"A"`) effect — unchanged from Phase 3's own boundary.
- Any turn-trigger/activation-condition mechanism that would let a caveated entry's condition ever
  be evaluated rather than just surfaced as unresolved.
- Wiring resolved weapon effects into any behavior beyond rendering (Phase 5, per `.claude/vnext-
  ideas.md`).

## Capabilities

### New Capabilities

(none — this change extends two existing capabilities' own requirements)

### Modified Capabilities

- `attached-unit-tracker`: the "Aggregate Weapon Count View" requirement gains a caveated-match
  surfacing signal (not applied, but visible) alongside the existing apply-when-uncaveated behavior.
- `live-play-view`: a new "Flagged Weapon Characteristic Rendering" requirement (mirroring "Flagged
  Statline Characteristic Rendering"); "Weapon Section Rendering"'s own breakdown-trigger condition
  widens to include a flagged contribution.

## Impact

- `src/ProbHammer.Core/Domain/Roster/AttachedUnitAggregator.cs` — `BuildWeapons`/
  `ResolveContributionProfile`/`TryGetWeaponEffectEntry` widen to surface a caveated match.
- `src/ProbHammer.Core/Domain/Roster/AttachedUnitAggregateView.cs` (or wherever `AggregateWeaponEntry`/
  `WeaponContribution` live — confirm exact file in design.md) — new field(s) carrying the
  unresolved-contribution signal.
- `src/ProbHammer.Web/Pages/LivePlay.cshtml.cs` — extend `AssignFlagMarkers`/`WeaponRowViewModel`/
  `WeaponContributionRow` to carry weapon-side markers; widen `ShowsBreakdownTrigger`'s condition.
- `src/ProbHammer.Web/Pages/Shared/_UnitBlock.cshtml` — render the name marker, value-cell marker,
  and legend line(s) in the Ranged/Melee weapon tables.
- Tests: `AttachedUnitAggregatorTests` (new caveated-surfacing cases), `LivePlayModelTests` (marker
  assignment across Statline + weapon sources sharing one registry), a real-captured-export
  verification pass per this project's standing practice for `AttachedUnitAggregator`-touching
  changes.
