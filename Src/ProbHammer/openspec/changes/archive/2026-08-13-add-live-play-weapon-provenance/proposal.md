## Why

`/LivePlay` merges same-profile weapons across an Attached Unit's components into one row showing
only the aggregated total Attacks — by design, today's spec explicitly forbids showing which
component/model-line contributed how much. Mid-game this is a real gap: for **Ranged** weapons, a
single weapon's attacks must all be directed at one enemy unit, so knowing the per-source breakdown
is sometimes necessary (not just informative) when deciding how to split fire, e.g. across two
simultaneous combats an Attached Unit is engaged in. The domain already retains this provenance
(`WeaponContribution(ComponentName, StatlineName, Count, PerModelAttacks)` on
`AggregateWeaponEntry.Contributions`, added in `rework-attached-unit-aggregate-view` specifically so
a future UI could use it) — nothing surfaces it today.

## What Changes

- Clicking a weapon's name expands pre-rendered sub-rows beneath it showing the breakdown; clicking
  again collapses them. The trigger is per-**unit**, not per-weapon: available on every weapon row
  as long as the unit has more than one `ModelLine` in total (across all of its components), even
  when that specific weapon has only one contribution — knowing "this came from the Sword Brother"
  is useful on its own, not just when there's aggregation to explain. Refined during implementation
  from an initial per-weapon `Contributions.Count > 1` trigger once hands-on use showed a
  single-contribution weapon (e.g. a Pyre pistol carried only by a unit's Sword Brother) was exactly
  the case worth naming. A unit with only one `ModelLine` in total (e.g. a single-model vehicle like
  Impulsor) shows no click affordance anywhere — there is no second source to distinguish from. Note
  an `AttachedUnit` can never hit that exclusion: Bodyguard + at least one Attached component, each
  contributing at least one `ModelLine`, is always two or more by construction.
- Sub-rows group contributions by `(ComponentName, StatlineName)`. When every grouped contribution
  shares the same `PerModelAttacks`, they collapse into one sub-row: `Name (Count×PerModelAttacks)`
  with the computed subtotal in the Attacks column and the other stat columns blank. When grouped
  contributions disagree on `PerModelAttacks` (possible since `WeaponProfileEqualityKey` excludes
  Attacks by design, though not expected in practice — see design.md), each contributing
  `ModelLine` renders as its own sub-row instead, so no total is ever shown that would misstate what
  it's built from.
- Sub-rows render in a visually distinct (dimmer/smaller/indented) style from primary weapon rows,
  so the two are never confused at a glance, and share their parent row's alternating-stripe
  background rather than striping independently — discovered mid-implementation that `:nth-child`
  counts a row's always-present-but-`[hidden]` sub-rows too, so striping must be driven by the
  weapon's own list index rather than raw DOM position.
- This is the first client-side interaction `/LivePlay` has ever had — every existing disclosure on
  the page uses native `<details>`, which cannot span additional `<tr>` elements below a table row.
  A small toggle script is added for this one interaction; no other page behavior changes.
- No domain-model change. `WeaponContribution` already carries every field needed
  (`ComponentName`, `StatlineName`, `Count`, `PerModelAttacks`); the grouping/formatting above is
  page-layer view-model work, matching this page's existing precedent (e.g. `StatlineBlockViewModel`
  for the by-value statline merge).
- **BREAKING**: relaxes `live-play-view`'s "Weapon Section Rendering" requirement. Its existing
  "Weapon count reflects aggregation across components" scenario currently states a merged entry's
  rendering "does not show any individual contribution's per-model Attacks value or a separate model
  count" — an absolute prohibition. This change revises that to "not shown by default, but available
  on demand via the new expand interaction"; the default (collapsed) rendering itself is unchanged.

## Capabilities

### New Capabilities
- None.

### Modified Capabilities
- `live-play-view`: "Weapon Section Rendering" requirement gains the expand/collapse
  contribution-breakdown interaction for merged weapon entries, and its
  "Weapon count reflects aggregation across components" scenario is revised from an absolute
  prohibition on showing per-contribution values to "not by default; available on demand."

## Impact

- **Changed**: `src/ProbHammer.Web/Pages/LivePlay.cshtml` (weapon table rows gain conditional
  sub-rows), `src/ProbHammer.Web/Pages/LivePlay.cshtml.cs` (view-model grouping logic for the
  collapse-when-uniform / expand-when-not rule), `src/ProbHammer.Web/wwwroot/css/site.css`
  (sub-row styling, scoped under `.live-play-page`).
- **New**: `src/ProbHammer.Web/wwwroot/js/live-play.js` (click-to-toggle script for sub-row
  visibility — `/LivePlay`'s first JavaScript).
- **Unchanged**: `AttachedUnitAggregateView`, `AttachedUnitAggregator`, `WeaponContribution`,
  `Datasheet`, `Unit`, `AttachedUnit`, `Simulation/*`.
- **Tests**: `tests/ProbHammer.Tests/Web/LivePlayModelTests.cs` gains cases for the grouping rule
  (uniform-collapse vs. disagreeing-expand) against the real `CrusaderSquadUnitWithChainSwords`
  fixture (`Initiate` split 2×Power-fist / 3×Astartes-chainsword, both loadouts sharing the same
  `Bolt pistol` entry). Manual/visual verification via `firefox-devtools-mcp`, per established
  project practice for `/LivePlay` changes.
