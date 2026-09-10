## Why

An aggregated weapon entry groups contributions by structural profile equality
(`WeaponProfile.EqualityKey()`), which deliberately excludes Name — real weapons that share an
identical structural profile but come from different catalogue entries are meant to roll together
for efficiency, and the rulebook itself encourages this. But the entry's own displayed Name today is
just "whichever contributing `WeaponProfile` happened to be inserted into the group's dictionary
first" — an accident of enumeration order, not a considered value. A group that in fact merged two
differently-named weapons (e.g. a Bolt rifle and a Combat rifle sharing an identical profile) already
silently displays only one of the two names, with no indication the other weapon is folded in. This
change makes that merge visible and correct: a merged group shows every distinct name it merged,
not an arbitrary single one.

This is also Phase 0 of a larger, still-parked plan for WeaponProfile-targeting rule effects (see
`.claude/vnext-ideas.md`) — a later phase needs to resolve a named-weapon-targeting ability against
the specific contribution(s) it names, which requires a contribution to know its own weapon's Name.
That need is out of scope here; this change only adds the Name tracking and the display fix it
directly enables.

## What Changes

- `WeaponContribution` gains a `Name` field, populated from the same `WeaponProfile` each
  contributing model-line's weapon is already resolved to during aggregation.
- An aggregated weapon entry's displayed name changes from "the first-inserted contributor's own
  Name" to a composite of every distinct Name among its contributions, in first-encountered order:
  the single name unchanged when every contribution shares one name; two distinct names joined as
  "X and Y"; three or more joined with a trailing Oxford comma ("X, Y, and Z"), mirroring the
  joining convention `AttachedUnit.Name` already uses for a comparable composite-name case.
- No change to grouping/equality behavior itself — which contributions merge into one entry is
  unchanged; only what that entry displays as its name changes.

## Capabilities

### New Capabilities

(none)

### Modified Capabilities

- `attached-unit-tracker`: the Aggregate Weapon Count View requirement's retained-contribution shape
  gains Name per contribution, and an aggregated entry's own display name changes from an arbitrary
  single contributor's Name to a composite of every distinct Name among its contributions.
- `live-play-view`: the Weapon Section Rendering requirement's "the weapon's name" wording is
  clarified to require this composite name, not a single (arbitrary) contributor's Name.

## Impact

- `src/ProbHammer.Core/Domain/Roster/AttachedUnitAggregateView.cs` — `WeaponContribution` record
  gains a `Name` field.
- `src/ProbHammer.Core/Domain/Roster/AttachedUnitAggregator.cs` — `BuildWeapons` populates the new
  field from the same `WeaponProfile.Name` it already resolves per model-line.
- The composite-name computation itself (new, small) and where it's introduced in the
  domain-vs-page-rendering-layer split — see `design.md`.
- `src/ProbHammer.Web/Pages/Shared/_UnitBlock.cshtml` — renders the composite name instead of
  `AggregateWeaponEntry.Profile.Name` directly.
- Existing tests asserting a merged group's rendered name (if any hand-built fixture currently
  happens to combine two differently-named, structurally-identical weapons) will need updating to
  the new composite-name expectation.
