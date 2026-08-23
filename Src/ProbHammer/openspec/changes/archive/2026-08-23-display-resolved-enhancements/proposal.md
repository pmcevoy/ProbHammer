## Why

`resolve-enhancement-abilities` (archived) made `Unit.Enhancements` a correctly-scoped list of
resolved `Ability` objects — but no rendering code reads that list. A player who applies an
Enhancement to a character has no way to see it on `/LivePlay` today; the data resolves correctly
and then goes nowhere.

## What Changes

- Wire `Unit.Enhancements` into the Attached Unit aggregate view's ability reporting, alongside the
  existing Datasheet-sourced abilities — same component-wide treatment (no statline binding), so an
  Enhancement shows up in whichever component applied it.
- Render an Enhancement-classified ability (`Ability.Origin == Enhancement`) with a leading `✦`
  symbol prefixed to its name, wherever an ability name is displayed on the page, distinguishing it
  from a plain intrinsic ability at a glance.
- An Enhancement renders in the Unit Abilities column unconditionally for now (BSData gives no
  per-ability Model/Unit scope signal, so every ability — Enhancement or not — already defaults to
  `Scope: Unit` at resolution time; this change does not alter that). A future ability-text
  classification pass is expected to determine genuine per-Enhancement scope instead of this
  default — noted in `.claude/vnext-ideas.md`, not part of this change's scope.

## Capabilities

### New Capabilities
(none)

### Modified Capabilities
- `attached-unit-tracker`: Aggregate Ability View requirement gains a third ability source — a
  present component's own resolved `Enhancements` — reported the same way as Datasheet-sourced
  abilities (component-wide, no statline name).
- `live-play-view`: new requirement governing the `✦` visual indicator on an Enhancement-classified
  ability name, wherever an ability name renders (ability columns and, since the popover title bar
  reuses the same rendered name — see `restyle-rule-popover-titlebar` — its popover too).

## Impact

- `src/ProbHammer.Core/Domain/Roster/AttachedUnitAggregator.cs` (`BuildAbilities`)
- `src/ProbHammer.Web/Pages/Shared/_UnitBlock.cshtml` (ability name rendering, all four call sites)
- `tests/ProbHammer.Tests/Domain/Roster/AttachedUnitAggregatorTests.cs`
- `.claude/domain-model-11e.md`, `PROGRESS.md`
