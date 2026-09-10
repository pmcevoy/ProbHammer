## 1. Domain model

- [ ] 1.1 Add `Name` (string) to `WeaponContribution`
  (`src/ProbHammer.Core/Domain/Roster/AttachedUnitAggregateView.cs`).
- [ ] 1.2 Add `Name` (string) to `AggregateWeaponEntry`, alongside `Profile`/`TotalAttacks`/
  `Contributions`; update the record's own doc comment to flag `Profile.Name` as non-authoritative
  for display, the same way it already flags `Profile.A`.
- [ ] 1.3 In `AttachedUnitAggregator.BuildWeapons`, populate `WeaponContribution.Name` from the
  already-resolved `profile.Name` at the same point `PerModelAttacks` is populated.
- [ ] 1.4 In `BuildWeapons`, compute each group's composite `Name` from its `Contributions` (per
  design.md's Decision 4: order-preserving `Distinct()`, then 1/2/3+ join with a trailing Oxford
  comma at 3+) and set it on the constructed `AggregateWeaponEntry`.

## 2. Rendering

- [ ] 2.1 Update `_UnitBlock.cshtml`'s weapon-table rendering (both Ranged and Melee sections) to
  read the new `AggregateWeaponEntry.Name` instead of `row.Entry.Profile.Name` for the weapon
  name cell and its breakdown-toggle trigger.

## 3. Tests

- [ ] 3.1 Search existing tests (`tests/ProbHammer.Tests/Domain/Roster/
  AttachedUnitAggregatorTests.cs`, `tests/ProbHammer.Tests/Web/LivePlayModelTests.cs`, and any
  other fixture constructing `WeaponContribution`/`AggregateWeaponEntry` directly) for a case that
  already merges two differently-named, structurally-identical weapons under the old
  first-inserted-name behavior; update any such fixture's expected name deliberately.
  Update every other existing call site constructing `WeaponContribution` directly to supply the
  new required `Name` field.
- [ ] 3.2 Add a unit test on `AttachedUnitAggregator.BuildWeapons` (or its existing test class)
  covering: a group whose contributions all share one Name (composite Name unchanged); a group
  merged from two differently-named weapons ("X and Y"); a group merged from three or more
  ("X, Y, and Z").
- [ ] 3.3 Add/update a `_UnitBlock.cshtml` rendering test confirming the composite Name renders in
  the weapon name cell and as the breakdown-toggle trigger text.
- [ ] 3.4 Run the full test suite and confirm no regressions.

## 4. Verification

- [ ] 4.1 `dotnet run` + browser check against a real captured export — confirm ordinary
  (non-merged, single-name) weapon entries render unchanged, and, if a real merged-differently-named
  case can be found in a captured export, confirm it now renders its composite name.

## 5. Docs

- [ ] 5.1 Update `.claude/domain-model/roster-context.md` (or wherever `AggregateWeaponEntry`/
  `WeaponContribution` are documented) to describe the new `Name` fields and composite-naming
  behavior.
- [ ] 5.2 Trim the completed Phase 0 bullet out of `.claude/vnext-ideas.md`'s "WeaponProfile-
  targeting rule effects — phased plan" entry once this change is archived, per that file's own
  "delete an idea once it's turned into a change" convention.
