## Why

`/LivePlay`'s Statline section already breaks out a statline entry's `Loadouts` as separate `<li>`
lines when more than one `ModelLine` shares a statline name (e.g. Crusader Squad's "Initiate" splits
into a Power-fist-armed line and an Astartes-chainsword-armed line). Each line shows its full
weapon list. In practice most of that list is identical across siblings — both Initiate loadouts
also carry Bolt pistol and Heavy Bolt pistol — so the shared weapons add noise instead of signal;
the only thing a player actually needs to read at a glance is what's *different* about this loadout.
This is also a deliberate prerequisite for a follow-on change (selection-scoped weapon filtering on
`/LivePlay`) that will reuse per-loadout labels inside weapon-contribution breakdown rows, where the
same compression is even more valuable — full repeated weapon lists would be worse there than here.

## What Changes

- Each loadout `<li>` in the Statline section's breakdown now shows only the weapons that
  distinguish it from its sibling loadouts under the same statline entry, instead of its full
  weapon list. For Crusader Squad's "Initiate" entry this changes the two lines from
  `"Bolt pistol, Heavy Bolt pistol, Power fist"` / `"Bolt pistol, Heavy Bolt pistol, Astartes
  chainsword"` to `"Power fist"` / `"Astartes chainsword"`.
- Compression is computed per statline entry, across that entry's own `Loadouts` only (never across
  entries, components, or units): the shared weapons are the **multiset (bag) intersection** of
  every sibling loadout's weapon list, and each loadout's displayed label is its own list with that
  shared multiset subtracted. Multiset, not set — a loadout carrying two of a weapon its siblings
  only carry one of keeps one copy visible after subtraction (the genuinely-extra copy), rather than
  losing it to a naive set-difference. The intersection is a true multiset intersection (weapons
  that don't all appear in the same list position still count as shared), not a common-prefix strip.
- A statline entry with exactly one loadout is unaffected — there is nothing to compress against, and
  today's existing `Loadouts.Count > 1` guard already means no breakdown renders at all in that case.
- Two loadouts under the same statline entry with 100% identical weapon lists (the compressed result
  would be empty) is treated as an input-data anomaly, not a case this change defends against — real
  army data has no reason to split identical loadouts into separate `ModelLine`s.

## Capabilities

### New Capabilities
- None.

### Modified Capabilities
- `live-play-view`: "Statline Section Rendering" requirement's loadout breakdown scenario changes
  from showing each loadout's full weapon list to showing only the weapons that distinguish it from
  its sibling loadouts under the same statline entry.

## Impact

- **Changed**: `src/ProbHammer.Web/Pages/LivePlay.cshtml.cs` (loadout-label compression, computed at
  the page-view-model layer alongside existing grouping logic like `GroupStatlines`), possibly
  `src/ProbHammer.Core/Domain/Roster/AttachedUnitAggregator.cs` or its `ModelLineLoadout` type if the
  raw per-loadout weapon list needs to be exposed alongside (or instead of) the existing pre-joined
  `WeaponsLabel` string — exact shape decided in design.md.
- **Unchanged**: `AggregateStatlineEntry`'s counts, `AttachedUnitAggregateView`, `WeaponContribution`,
  aggregation math, all non-Statline sections of `/LivePlay`.
- **Tests**: `tests/ProbHammer.Tests` gains unit tests for the compression function directly (real
  Crusader Squad case, non-contiguous shared weapons, multiset subtraction with a duplicated weapon,
  single-loadout no-op case). Visual verification via `firefox-devtools-mcp` against the real example
  army, per established project practice for `/LivePlay` changes.
