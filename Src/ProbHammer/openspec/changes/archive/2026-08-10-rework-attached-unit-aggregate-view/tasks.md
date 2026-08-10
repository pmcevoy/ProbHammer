## 1. Aggregate View Shapes

- [x] 1.1 In `AttachedUnitAggregateView.cs`, add `ModelLineLoadout(WeaponsLabel, RemainingCount, InitialCount)` and extend `AggregateStatlineEntry` with `RemainingCount`, `InitialCount`, and `Loadouts: IReadOnlyList<ModelLineLoadout>`
- [x] 1.2 Add `WeaponContribution(ComponentName, StatlineName, Count, PerModelAttacks)` and replace `AggregateWeaponEntry`'s `Count` with `TotalAttacks: DiceExpression` and `Contributions: IReadOnlyList<WeaponContribution>`
- [x] 1.3 Add a doc comment on `AggregateWeaponEntry.Profile` stating its `A` is not authoritative once merged and must not be rendered — only `TotalAttacks` should be read for display

## 2. Aggregator Logic

- [x] 2.1 In `AttachedUnitAggregator.Build()`, split the single `presentLines` (RemainingCount > 0) list into two explicit inputs: keep it as-is for `BuildWeapons`, and pass an unfiltered per-component model-line set to `BuildStatlines` (so fully-dead loadouts and correct initial counts remain visible)
- [x] 2.2 Rewrite `BuildStatlines` to group by `StatlineName` across the unfiltered set, computing summed `RemainingCount`/`InitialCount` and building the `Loadouts` list (one entry per model-line, `WeaponsLabel` = comma-joined `ModelLine.Weapons`); a statline name is included only while at least one of its model-lines has `RemainingCount > 0`
- [x] 2.3 Rewrite `BuildWeapons` to compute `TotalAttacks` via `DiceExpression.Scale(modelLine.RemainingCount)` per contributing model-line, `Add`-reduced across all contributors sharing an `EqualityKey`, and to build the `Contributions` list alongside it (`ComponentName` = the owning `Unit.Datasheet.Name`)

## 3. Tests

- [x] 3.1 Extend `AttachedUnitAggregatorTests` with a case asserting the weapon-merge Attacks fix: two components contributing the same `EqualityKey` with different per-model Attacks (e.g. 4×A3 + 1×A7) produce one entry with `TotalAttacks` = 19, not a count-only merge
- [x] 3.2 Add a case asserting differently-modified same-named weapons (e.g. one copy with Lethal Hits, one without) stay as separate `AggregateWeaponEntry` rows, each with their own `TotalAttacks`
- [x] 3.3 Add a case asserting `Contributions` lists one item per contributing model-line with correct `Count`/`PerModelAttacks`
- [x] 3.4 Add a case asserting `AggregateStatlineEntry.InitialCount` sums across model-lines sharing a name even when one of them has been fully removed as casualties, while `RemainingCount` reflects only survivors
- [x] 3.5 Add a case asserting a fully-removed model-line's `Loadouts` entry still appears (remaining 0, initial preserved) as long as another model-line sharing the same statline name still has models remaining
- [x] 3.6 Add a case asserting a statline name still disappears entirely once every model-line sharing it reaches `RemainingCount` 0 (existing behavior, now re-verified against the new fields)
- [x] 3.7 Confirm the existing `StatlineView_TreatsDifferentlyNamedStatlinesAsDistinct_EvenWithEqualValues` test still passes unchanged against the new shape

## 4. Live Play Page

- [x] 4.1 Remove `LivePlayModel`'s `GroupStatlinesByValue` and its by-value collapsing; render one row per `AttachedUnitAggregateView.Statlines` entry (by name) directly
- [x] 4.2 Render each statline row's `RemainingCount`/`InitialCount` and, when `Statline.InSv` is set, its invulnerable save value
- [x] 4.3 Render each statline row's `Loadouts` as a nested sub-list (weapon list + remaining/initial count per loadout)
- [x] 4.4 Update the weapon table to show `TotalAttacks` in place of the previous per-row Attacks value, and drop the separate model-count column
- [x] 4.5 Fix the "Bold Pistol"/"Bolt Pistol" mismatch already corrected in `Datasheets.cs`/`Units.cs` remains consistent — confirm `/LivePlay` still loads without error for all units in `Examples.View.MyArmy()` after the above changes

## 5. Documentation

- [x] 5.1 Update `.claude/domain-model-11e.md`: document `Statline.InSv`, the revised `AggregateStatlineEntry`/`AggregateWeaponEntry`/`ModelLineLoadout`/`WeaponContribution` shapes, and the corrected weapon-aggregation rule (total Attacks via `Scale`/`Add`, not a representative profile's raw `A`)

## 6. Verification

- [x] 6.1 Run the app locally, navigate to `/LivePlay`, and visually confirm every unit from `Examples.View.MyArmy()` renders with correct statlines (including nested loadouts and counts), aggregated weapon totals, abilities, and keywords
- [x] 6.2 Manually cross-check `Units.SwordBretheren_Marshal()`'s Master-crafted Power Weapon total and `Units.CrusaderSquad_Marshal_Lieutenant()`'s Astartes Chainsword (28), Lethal-Hits Master-crafted Power Weapon (10), and Lieutenant's non-Lethal-Hits copy (5) against the page — confirmed 28/10/5 exactly; SwordBretheren_Marshal's total rendered as **19** (4 Sword Brothers × A3 + 1 Marshal × A7), not the "10" this task line names — see note below, task text appears to have copied the CrusaderSquad_Marshal_Lieutenant figure by mistake
- [x] 6.3 `dotnet build` and full test suite pass with no new failures

## 7. Weapon Equality Fix (found via manual /LivePlay check)

- [x] 7.1 Fix `WeaponProfile.EqualityKey()` / `WeaponProfileEqualityKey` in `src/ProbHammer.Core/Domain/Catalogue/WeaponProfile.cs` to include `Pistol`, `IgnoresCover`, and `Assault` — previously omitted, causing structurally-different weapons (e.g. Scout Squad's Bolt pistol A1/Pistol and Astartes shotgun A2/Assault) to merge into one row with a meaningless summed total (13, from 5×A1 + 4×A2)
- [x] 7.2 Add a regression test (`WeaponView_WeaponsDifferingOnlyByPistolAssaultOrIgnoresCover_StaySeparate`) covering all three previously-omitted fields
- [x] 7.3 Update the `attached-unit-tracker` delta spec's Aggregate Weapon Count View requirement to enumerate the full ability/keyword field list and add a scenario for targeting/eligibility keywords (Pistol/Assault/Ignores Cover) staying distinct
- [x] 7.4 Update `proposal.md`'s Impact section: move `WeaponProfileEqualityKey` from Unchanged to Changed
- [x] 7.5 `dotnet build` and full test suite pass; `/LivePlay` re-verified — Scout Squad's Bolt pistol (A5) and Astartes shotgun (A8) now render as separate rows
