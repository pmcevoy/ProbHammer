## Context

`AttachedUnitAggregator.ResolveContributionProfile`/`TryGetWeaponEffectEntry` (Phase 3,
`resolve-weapon-characteristic-effects`) already filter to `IsCaveated: false` at the baseline
lookup itself — a caveated match leaves no trace in `WeaponContribution`/`AggregateWeaponEntry`
today. A non-caveated match's applied result already carries its source ability via the mutated
field's own `ScalarCharacteristicView.ContributingAbilities` (identical shape to a Statline scalar's
own `ContributingAbilities`), so the resolved-value half of this change is pure wiring; the
caveated-surfacing half needs a small domain addition. See proposal.md - Why for the full gap
description.

The Statline family's marker mechanism (`LivePlayModel.AssignFlagMarkers`,
`.claude/domain-model/statline-flag-rules.md`) already assigns a per-unit-block, per-distinct-source
footnote marker across every `StatlineBlockViewModel` run. This change extends that exact mechanism
to also cover weapon entries, rather than building a parallel one — see D2.

## Goals / Non-Goals

**Goals:**
- Surface a caveated weapon-characteristic match as visible-but-unresolved, without applying it.
- Render a resolved weapon value's source and a caveated match's source using one shared,
  cross-section marker registry per unit block.
- Keep every existing weapon-table/aggregate-view behavior (grouping, selection filtering,
  breakdown rows) unchanged for a weapon with no flagged contribution.

**Non-Goals:**
- Evaluating a caveated entry's own activation condition (still permanently out of scope until this
  app has a turn-trigger mechanism — see proposal.md).
- Resolving an Attacks-characteristic effect (Phase 3's own boundary, unchanged).
- Any change to `WeaponSelector`/`WeaponCharacteristicEffect`'s own shape — this change only adds a
  second, non-mutating lookup path alongside the existing mutating one.

## Decisions

### D1: Where the unresolved-reference signal lives in the domain model
`WeaponContribution` gains a new field, `UnresolvedAbilities: IReadOnlyList<Ability> = []` —
every present, bearer-scoped, selector-matched, caveated ability found for that specific
contribution's weapon profile (computed by a new sibling to `ResolveContributionProfile`, reusing
its own `IsBearerOf`/`WeaponSelectorMatches` helpers unchanged, just reading the caveated branch of
`TryGetWeaponEffectEntry`'s lookup instead of the non-caveated one). `AggregateWeaponEntry` gains
`UnresolvedAbilities: IReadOnlyList<Ability>`, computed in `BuildWeapons` the same way `Name` already
is (order-preserving `Distinct()` by `(Name, Text)` over every contribution's own
`UnresolvedAbilities`, in first-encountered order) — a stored field set once at aggregation time,
not a method recomputed at render time, matching `Name`'s own precedent exactly.

**Alternative considered**: expose only a `bool HasUnresolvedContribution` at the entry level and
make the renderer walk `Contributions` itself to find the actual ability. Rejected — every other
"who caused this" signal on this page (`ScalarCharacteristicView.ContributingAbilities`,
`AggregateAbilityEntry`) exposes the real `Ability` list directly rather than a derived bool plus a
manual re-walk, and the render layer needs the actual `Ability` (Name + Text) to build a popover
trigger regardless.

### D2: One shared marker registry across Statline and Weapons
`LivePlayModel.AssignFlagMarkers` widens to accept both the unit block's `StatlineBlockViewModel`
list and its two weapon lists (Ranged, Melee, already sorted by descending `TotalAttacks`), and
assigns markers in one pass, in a fixed order: every Statline run top-to-bottom (existing order,
unchanged), then Ranged weapon entries in their rendered order, then Melee weapon entries in their
rendered order. Within a weapon entry, a resolved-value marker source is checked in S → AP → D
column order (mirroring `ScalarStatlineFieldOrder`'s own left-to-right convention), then the entry's
own `UnresolvedAbilities` (first entry only feeds the name marker — an entry with more than one
distinct unresolved ability still only shows one name marker, since the marker's job is "there is an
unresolved concern here, see the breakdown," not enumerating every one). The existing
`markerBySource` dictionary (keyed by `(Ability.Name, Ability.Text)`) is reused unchanged — a source
ability already assigned `*` for a Statline tile is not re-assigned a new marker when it also flags
a weapon value later in the same pass.

**Alternative considered**: a separate weapon-only marker registry, starting its own `*`/`**`
sequence independent of the Statline one. Rejected per direct user confirmation during scoping —
the same source ability affecting both a Statline value and a weapon value in one unit block (a
real, expected shape: nothing about `WeaponCharacteristicEffect`/`CharacteristicEffect` classification
prevents one ability from carrying both an Effect list entry of each kind) must read as one thing,
not two differently-numbered markers for what is, to the player, one ability doing one thing.

### D3: Resolved-value marker source needs no new domain field
A weapon value cell's own marker source is `weapon.S/Ap/D.ContributingAbilities.FirstOrDefault()` —
already populated by Phase 3's `WeaponCharacteristicEffectResolver.Resolve`, identical in shape to
`GetScalarField(statline, field).ContributingAbilities` already read for Statline tiles. No resolver
or aggregator change needed for this half.

### D4: Per-entry breakdown-trigger gating
`BuildUnitBlock`'s single unit-wide `showsBreakdownTrigger` bool (`totalModelLines > 1`) becomes a
per-entry computation: `WeaponRowViewModel.ShowsBreakdownTrigger` is `true` when either the existing
unit-wide condition holds, or the entry's own `Profile.S/Ap/D` has a non-empty
`ContributingAbilities`, or the entry's own `UnresolvedAbilities` is non-empty. This is a pure
view-layer change (`BuildUnitBlock`'s `orderedWeapons` projection); `AggregateWeaponEntry` itself
carries no "should this show a trigger" concept, matching the existing separation where the domain
model reports raw aggregate data and the page layer derives rendering gates from it (mirrors
`IsSingleModelUnit`/`isAtOrBelowHalfStrength` already being view-layer-computed from the domain
view, not stored on it).

### D5: Caveated-lookup reuses the existing matching helpers unchanged
The new caveated-branch lookup in `AttachedUnitAggregator` calls the exact same `IsBearerOf`/
`WeaponSelectorMatches` predicates `ResolveContributionProfile` already uses for the non-caveated
branch — only `TryGetWeaponEffectEntry`'s own `IsCaveated` filter direction flips for this second
call. No new selector-matching logic; the two branches differ only in which baseline entries they
admit and in what they do with a match (mutate vs. record-as-unresolved).

## Risks / Trade-offs

- **[Risk]** Marker-order coupling: interleaving Statline and Weapon sources into one assignment
  pass means a future Statline-only or Weapon-only rendering change could accidentally reorder
  marker assignment (e.g. reordering `ScalarStatlineFieldOrder`) and silently renumber markers
  elsewhere on the page. → Mitigation: `AssignFlagMarkers` keeps the exact fixed-order comment
  convention `ScalarStatlineFieldOrder`'s own doc comment already establishes; a test asserts marker
  identity across both a flagged Statline tile and a flagged weapon value in the same fixture unit.
- **[Risk]** A weapon entry's `UnresolvedAbilities` growing unboundedly distinct across many
  contributions could make the name-marker-only-shows-one-source behavior feel like it's hiding
  information. → Mitigation: no real corpus example today has more than one distinct caveated
  ability reaching the same weapon group (per Phase 2's 19-result corpus review); the breakdown
  trigger is always available to inspect further, and this is the same "one marker per source,
  legend enumerates" pattern the Statline family already ships with no reported confusion.

## Migration Plan

Purely additive - no existing field is removed or retyped. `WeaponContribution.UnresolvedAbilities`/
`AggregateWeaponEntry.UnresolvedAbilities` default to empty, so every existing caller/fixture
constructing these records positionally needs updating only if it uses positional (not
named-argument) construction; confirm during task 1 which existing tests need touching. No data
migration, no `RuleClassificationBaseline` JSON change - this change consumes the existing 19
baselined entries exactly as Phase 3 left them, just no longer discarding the caveated ones outright.
