## 1. Domain: carry ability names onto each loadout

- [x] 1.1 Add an `Abilities: IReadOnlyList<string>` field to `ModelLineLoadout`
  (`src/ProbHammer.Core/Domain/Roster/AttachedUnitAggregator.cs` or wherever the record is
  declared) - ability *names*, mirroring `WeaponsLabel`'s already-joined-per-loadout shape (see
  design.md - Decisions).
- [x] 1.2 In `AttachedUnitAggregator.BuildStatlines`, populate the new field from that loadout's
  own `ml.Abilities.Select(a => a.Name).ToList()` when constructing each `ModelLineLoadout`.
- [x] 1.3 Update every existing `ModelLineLoadout` construction call site (test fixtures included)
  for the new constructor arity - confirmed call sites in
  `tests/ProbHammer.Tests/Web/LivePlayModelTests.cs` (several `new ModelLineLoadout(...)` calls)
  plus any production call site beyond `AttachedUnitAggregator` itself.

## 2. Web: fold ability names into the loadout-label computation

- [x] 2.1 In `LivePlayModel.CompressLoadoutLabels` (`src/ProbHammer.Web/Pages/LivePlay.cshtml.cs`),
  run the existing weapons multiset intersection/subtraction unchanged, and run the identical
  algorithm a second time over each loadout's `Abilities` list.
- [x] 2.2 Join each loadout's distinguishing weapons and distinguishing ability names into one
  comma-separated label, weapons first then ability names, per design.md - Decisions.
- [x] 2.3 Add a `statlineName` parameter to `CompressLoadoutLabels` and return it verbatim (instead
  of `""`) for any loadout whose combined weapons+abilities label is empty after subtraction; update
  its one call site in `GroupStatlines` to pass `entry.StatlineName`.
- [x] 2.4 In `LivePlayModel.BuildLoadoutLabelLookup`, special-case the fallback so a loadout whose
  compressed label equals `entry.StatlineName` gets `entry.StatlineName` alone as its lookup value
  (no `" w/ {compressed}"` suffix), matching the existing single-loadout-entry lookup shape -
  otherwise it would render the redundant `"Custodian Warden w/ Custodian Warden"` - see design.md.

## 3. Tests

- [x] 3.1 Add a `CompressLoadoutLabels` test reproducing the reported bug: two loadouts with
  identical weapon lists under a `"Custodian Warden"` entry, one carrying an extra ability name
  (e.g. `"Vexilla"`) - assert the ability-carrying loadout's label is `"Vexilla"` and the other's is
  `"Custodian Warden"` (the fallback, not empty).
- [x] 3.2 Add a `CompressLoadoutLabels` test for the "nothing distinguishes either pool" case: two
  loadouts with identical weapons AND identical ability names (or both empty) under a named entry -
  assert both labels equal that entry's `StatlineName`.
- [x] 3.3 Add a `BuildLoadoutLabelLookup` test covering the fallback case: assert the lookup value
  for a no-distinguishing-feature loadout is the bare `StatlineName` (e.g. `"Custodian Warden"`),
  not `"Custodian Warden w/ Custodian Warden"`.
- [x] 3.4 Add/extend an `AttachedUnitAggregatorTests` case confirming `BuildStatlines` populates
  `ModelLineLoadout.Abilities` correctly for a `ModelLine` carrying a wargear-granted ability,
  keyed off an existing Custodian Warden-shaped fixture if one exists, otherwise a minimal
  synthetic fixture mirroring it.
- [x] 3.5 Update every existing `CompressLoadoutLabels` test's expected output for the loadouts that
  previously asserted an empty string where nothing distinguished the loadout (e.g.
  `CompressLoadoutLabels_LeavesASingleLoadoutStatlineUnaffected` - confirm single-loadout entries are
  unaffected by this change, since they never call into the compression path at all) and run the
  full existing `CompressLoadoutLabels`/loadout-breakdown test suite
  (`tests/ProbHammer.Tests/Web/LivePlayModelTests.cs`) to confirm every other pre-existing scenario's
  output is unchanged.

## 4. Manual verification

- [x] 4.1 Run the app locally (`dotnet run` or `docker compose up`), import
  `data/gw-android-export-custodes.json` (via the browser paste, or the curl round-trip used to
  diagnose this bug: `POST /Import` with a cookie jar + antiforgery token, mirroring
  `ImportFlowTests`), and open `/LivePlay`.
- [x] 4.2 Confirm the "Custodian Wardens with Blade Champion" unit's Statline section now shows a
  loadout-breakdown label of "Vexilla" for the `(1/1)` loadout, and "Custodian Warden" (the
  fallback, no longer blank) for the `(4/4)` loadout - matching NewRecruit's own "w/ Vexilla"
  distinction for the same list while leaving no loadout line blank.
- [x] 4.3 Confirm the Melee Weapons section's Guardian Spear contribution-breakdown rows for the
  same unit now read "Custodian Warden w/ Vexilla" and "Custodian Warden" (not
  "Custodian Warden w/ Custodian Warden") instead of the previously blank
  `"Custodian Warden w/  (1×5)"`/`"Custodian Warden w/  (4×5)"` rows.

  Verified live (2026-09-10) via the curl round-trip against `data/gw-android-export-custodes.json`:
  rendered `<span class="loadout-label">` reads "Custodian Warden" / "Vexilla" for the (4/4)/(1/1)
  loadouts, and the Melee Weapons breakdown rows read "Custodian Warden (4×5)" /
  "Custodian Warden w/ Vexilla (1×5)" - matches design.md exactly.

## 5. Follow-up: real import (Death Guard) revealed the fallback still ties in a case with no
   distinguishing weapon/ability at all - the StatlineName-only fallback (section 2 above) isn't
   enough; both import pipelines already parse a distinguishing per-loadout name and discard it.

- [x] 5.1 Add `DisplayName` to `ModelLine` (`src/ProbHammer.Core/Domain/Roster/ModelLine.cs`) -
  a new optional constructor parameter defaulting to `statlineName` when omitted, so every existing
  call site is unaffected.
- [x] 5.2 In `ArmyRosterEnricher.BuildModelLine`, pass `group.ModelName` (the parsed export's own raw
  per-group name, pre-resolution) through as `displayName`.
- [x] 5.3 In `BattleScribeRosterMapper.BuildModelLines`, pass each loadout `node`'s own raw `Name`
  through as `displayName`.
- [x] 5.4 Add `DisplayName` to `ModelLineLoadout`
  (`src/ProbHammer.Core/Domain/Roster/AttachedUnitAggregateView.cs`), populated in
  `AttachedUnitAggregator.BuildStatlines` from `ml.DisplayName`.
- [x] 5.5 Factor `LivePlayModel.CompressLoadoutLabels`' weapons+abilities-only computation out into a
  shared private `DistinguishingLabels` helper (no fallback applied yet), so `BuildLoadoutLabelLookup`
  can tell "genuinely distinguishing attribute" apart from "fallback identity name" - see design.md.
  `CompressLoadoutLabels` itself drops its `statlineName` parameter (no longer needed: falls back to
  `loadout.DisplayName`, which already defaults to the bare statline name).
- [x] 5.6 Update `BuildLoadoutLabelLookup` to call `DistinguishingLabels` directly and render a tied
  loadout's bare `DisplayName` (no `"{StatlineName} w/ "` wrapping) rather than special-casing
  equality against `entry.StatlineName`.
- [x] 5.7 Tests: `LivePlayModelTests` - update every existing `ModelLineLoadout`/`CompressLoadoutLabels`
  call site for the new signature/constructor arity; add a case reproducing the Death Guard tie
  (`CompressLoadoutLabels_FallsBackToEachLoadoutsOwnDisplayName_WhenWeaponsAndAbilitiesAreIdentical`)
  and a `BuildLoadoutLabelLookup` case confirming the bare-DisplayName (no "w/") rendering for a tied
  pair. `AttachedUnitAggregatorTests` - a case confirming `BuildStatlines` populates
  `ModelLineLoadout.DisplayName` from `ModelLine.DisplayName`, and a case confirming it defaults to
  the statline name when a fixture gives none. `ArmyRosterEnricherTests` - a case confirming
  `BuildModelLine` carries `ParsedModelGroup.ModelName` through as `DisplayName`, distinct from the
  resolved `StatlineName`. `BattleScribeRosterMapperTests` - a case confirming `BuildModelLines`
  carries each loadout node's raw `name` through the same way, against the real Templars excerpt
  fixture's own "Initiate w/Power Fist & Heavy Bolt Pistol"-shaped node names.
- [x] 5.8 Run the full test suite and confirm every pre-existing scenario (Custodes/Vexilla,
  Crusader Squad Initiate, single-loadout) still passes unchanged.
- [x] 5.9 Manual verification: re-import `data/gw-android-export-deathguard.json` and confirm the
  Plague Marines unit's two loadouts now read "Plague Champion" and "Plague Marine w/ boltgun"
  (never two identical "Plague Marine" rows) in both the Statline section and the weapon-breakdown
  rows.

  Verified live (2026-09-10): Statline section loadout labels read "Plague Champion" / "Plague
  Marine w/ boltgun"; weapon-breakdown rows read "Plague Champion (1×2/3)" / "Plague Marine w/
  boltgun (4×2/3)" - bare DisplayName, no redundant "Plague Marine w/ Plague Champion" wrapping.
- [x] 5.10 Cross-check every import investigated today side by side (Custodes, Death Guard, and
  Black Templars - the last one covering the ordinary weapon-distinguishing case, to confirm no
  regression) via chrome-devtools MCP tabs against the running dev server.

  Verified live (2026-09-10) using three chrome-devtools MCP tabs, each in its own isolated browser
  context with its own imported session (`data/gw-android-export-custodes.json`,
  `data/gw-android-export-deathguard.json`, `data/gw-app-export-templars.txt`), screenshotted and
  inspected via `evaluate_script`, no console errors/warnings on any tab:
  - Custodes: Custodian Warden's `(4/4)` loadout now reads "Custodian Warden (Guardian Spear)" (its
    own raw import name, an even more informative fallback than the bare statline name from section
    2's fix) and `(1/1)` still reads "Vexilla" (ability-distinguished, unaffected).
  - Death Guard: Plague Marine's `(1/1)`/`(4/4)` loadouts read "Plague Champion"/"Plague Marine w/
    boltgun" - the reported bug is fixed.
  - Black Templars: Crusader Squad's Initiate loadouts still read "Astartes chainsword"/"Power fist"
    - the ordinary weapon-distinguishing case, byte-identical to before this change - no regression.
