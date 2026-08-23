## 1. Domain: wire Enhancements into the aggregate ability view

- [x] 1.1 In `AttachedUnitAggregator.BuildAbilities` (`src/ProbHammer.Core/Domain/Roster/AttachedUnitAggregator.cs`),
      add a loop over each present component's `Enhancements`, adding one `AggregateAbilityEntry`
      per resolved Enhancement with `StatlineName: null` (component-wide), the same treatment as
      the existing `Datasheet.Abilities` loop, guarded by the same `component.IsPresent` check.
- [x] 1.2 Update the method's doc comment to describe the new source.

## 2. Rendering: the ✦ indicator

- [x] 2.1 In `_UnitBlock.cshtml`, add an `AbilityDisplayName(Ability ability)` helper (in the
      `@functions` block) returning `$"✦ {ability.Name}"` when `ability.Origin ==
      AbilityOrigin.Enhancement`, else `ability.Name`.
- [x] 2.2 Replace `WebUtility.HtmlEncode(ability.Name)` with
      `WebUtility.HtmlEncode(AbilityDisplayName(ability))` at all four ability-name render call
      sites (per-run Model column, per-run Unit column, per-component-span Model column,
      per-component-span Unit column).

## 3. Tests

- [x] 3.1 `AttachedUnitAggregatorTests`: a present component with a non-empty `Enhancements` list
      produces a matching `AggregateAbilityEntry` with `StatlineName: null`.
- [x] 3.2 `AttachedUnitAggregatorTests`: an Enhancement disappears from the aggregate view once its
      owning component's model-lines all reach a remaining count of 0.
- [x] 3.3 `AttachedUnitAggregatorTests`: a component with an empty `Enhancements` list produces no
      Enhancement-sourced entry.
- [x] 3.4 A rendering-level test (or manual verification, per task 4.1) confirming an
      Enhancement-classified ability's rendered name is prefixed with `✦ ` and a non-Enhancement
      ability's is not.

## 4. Verification and docs

- [x] 4.1 Manually verify via `dotnet run` + a real import (`data/gw-app-export-templars.txt`,
      which already carries a Marshal with "Oathbound Exemplar" applied) that `/LivePlay` now shows
      "✦ Oathbound Exemplar" in that Marshal's Unit Abilities column.
- [x] 4.2 Run the full test suite; confirm no regression.
- [x] 4.3 Update `.claude/domain-model-11e.md`'s Aggregate Ability View description and
      `PROGRESS.md` with a summary of this change.
