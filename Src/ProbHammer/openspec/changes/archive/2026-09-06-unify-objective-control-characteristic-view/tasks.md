## 1. `ScalarCharacteristicView` factories (design D1)

- [x] 1.1 Add `Resolved(CharacteristicValue value)` (forwards to `Resolved(value, [])`) to
      `ScalarCharacteristicView` in `src/ProbHammer.Core/Domain/Catalogue/CharacteristicView.cs`.
- [x] 1.2 Add `Resolved(CharacteristicValue value, IReadOnlyList<Ability> contributingAbilities)`
      returning `new(value, value, contributingAbilities)`.
- [x] 1.3 Add `Caveated(CharacteristicValue value, Ability caveatAbility)` returning
      `new(value, null, [caveatAbility])`.
- [x] 1.4 Add `public static implicit operator ScalarCharacteristicView(int uniformValue) =>
      Resolved(uniformValue)`.

## 2. Retype `Statline.Oc` (design Context)

- [x] 2.1 In `src/ProbHammer.Core/Domain/Catalogue/Statline.cs`, change the `Oc` positional
      parameter's type from `int` to `ScalarCharacteristicView`.
- [x] 2.2 Build `ProbHammer.Core` and confirm every existing plain-int `Oc:`/positional
      construction site (both mappers' `MapStatline`, every `Examples/Datasheets.cs` fixture, every
      test fixture in `tests/ProbHammer.Tests/Domain/Fixtures/`) still compiles unchanged via the
      new implicit conversion — no source edits expected at these sites.

## 3. `VexillaStatlineFlagRule` (design D2)

- [x] 3.1 In `src/ProbHammer.Core/Domain/Roster/StatlineFlagRule.cs`, rewrite
      `VexillaStatlineFlagRule.Apply` to downcast `baseStatline.Oc.Value` to
      `NumericCharacteristicValue`, add 1 to its `.Value`, and rebuild `Oc` via
      `ScalarCharacteristicView.Resolved(current + 1, [matchedAbility])`.

## 4. Retire the `StatlineFlag` side-channel (design D3)

- [x] 4.1 In `src/ProbHammer.Core/Domain/Roster/AttachedUnitAggregateView.cs`, delete the
      `StatlineFlagCharacteristic` enum and the `StatlineFlag` record.
- [x] 4.2 In the same file, remove the `Flags` parameter (and its default-`[]`-forwarding
      constructor overload) from `AggregateStatlineEntry`.
- [x] 4.3 In `src/ProbHammer.Core/Domain/Roster/StatlineFlagRule.cs`, remove
      `StatlineFlagRule.Characteristic` from the abstract base and both
      `ShieldDomeStatlineFlagRule`/`VexillaStatlineFlagRule` overrides.
- [x] 4.4 In `src/ProbHammer.Core/Domain/Roster/AttachedUnitAggregator.cs`'s
      `ApplyStatlineFlagRules`, remove the `flags`-building branch (the `Characteristic is {}
      characteristic` check and the `entry with { ..., Flags = flags }` reassignment) — keep only
      the `Statline = mutated` reassignment.

## 5. `LivePlayModel` (design D4)

- [x] 5.1 In `src/ProbHammer.Web/Pages/LivePlay.cshtml.cs`'s `GroupStatlines`, replace the
      `ocSource` lookup (today: `g.SelectMany(e => e.Flags).FirstOrDefault(f => f.Characteristic ==
      StatlineFlagCharacteristic.ObjectiveControl)?.SourceAbility`) with
      `statline.Oc.ContributingAbilities.FirstOrDefault()`.

## 6. `_UnitBlock.cshtml` rendering (design D5)

- [x] 6.1 In `src/ProbHammer.Web/Pages/Shared/_UnitBlock.cshtml`, change both
      `@(block.Statline.Oc)` render sites to `@(block.Statline.Oc.Value)`.

## 7. Tests

- [x] 7.1 In `tests/ProbHammer.Tests/Domain/Roster/StatlineFlagRuleTests.cs`, change the four
      `s.Statline.Oc == <n>` predicate comparisons to `s.Statline.Oc.Value == <n>`.
- [x] 7.2 In the same file, delete the `entry.Flags.Should().BeEmpty();` assertion in
      `UnmatchedAbility_NameMatchesButTextDoesNot_ProducesNoFlag` (the `Flags` field no longer
      exists).
- [x] 7.3 In `tests/ProbHammer.Tests/Web/LivePlayFlaggedStatlineRenderingTests.cs`, rewrite every
      `AggregateStatlineEntry` construction that currently passes `Flags:
      [new StatlineFlag(StatlineFlagCharacteristic.ObjectiveControl, Vexilla)]` to instead
      construct its `Statline`'s `Oc` directly as
      `ScalarCharacteristicView.Resolved(<value>, [Vexilla])`, passing no `Flags` argument —
      mirroring how `DistinctSources_GetDistinctMarkers` already builds its `InSv` via
      `InvulnerableSaveCharacteristicView.Resolved(shieldDomeValue, [shieldDome])`. Affects
      `ObjectiveControlFlag_RendersMarkedLabelFlaggedTileAndLegendTrigger`,
      `UnitWideSource_KeepsTheSameMarker_AndItsLegendRepeatsInEveryAffectedRun` (both entries),
      `DistinctSources_GetDistinctMarkers`, and `FullyDeadRun_RendersItsFlaggedTileAndLegendInside
      TheSameCollapsedCell`.
- [x] 7.4 Run the full `ProbHammer.Tests` suite and confirm it passes with no other compile errors
      referencing `StatlineFlag`, `StatlineFlagCharacteristic`, or `AggregateStatlineEntry.Flags`.

## 8. Manual verification

- [x] 8.1 Run the app (`docker compose up` or `dotnet run`), import a real export containing a
      Custodian Guard unit with Vexilla, and confirm at `/LivePlay` that the OC tile still shows
      the flagged marker/legend exactly as before this change (no visual regression). Verified via
      `data/verification-roster-ability-effects-custodes.json` (BattleScribe JSON import) —
      Custodian Guard and Custodian Guard (Vexilla) both render `OC*` = 3 (base 2 + Vexilla's 1)
      with a `* Vexilla` legend line.
- [x] 8.2 Confirm a unit with no OC-mutating ability still renders its plain OC value with no
      marker, unchanged. Verified: Warhound Titan renders plain `OC` = 16, `[TEST] Split InSv
      (Unresolved)` renders plain `OC` = 2 — neither flagged.
