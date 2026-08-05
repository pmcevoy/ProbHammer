## ADDED Requirements

### Requirement: Live Play Page Lists Example Army Units
The `/LivePlay` page SHALL render one block per `AttachedUnitAggregateView` returned by `Examples.View.MyArmy()`, in the order returned.

#### Scenario: Page loads with the example army
- **WHEN** a user navigates to `/LivePlay`
- **THEN** the page displays one rendered block for each unit in `Examples.View.MyArmy()`, with no unit omitted or duplicated

### Requirement: Statline Section Rendering
Each unit block SHALL render one row per distinct M/T/Sv/W/Ld/Oc value combination present in the view's `Statlines`. When two or more `Statlines` entries share identical M/T/Sv/W/Ld/Oc values, they SHALL collapse into that single row, with all contributing statline names listed together in the row's name column. Statlines with a value combination unique among the view's `Statlines` SHALL render alone in their own row under their own name.

This is a display-only grouping in `live-play-view` — it does not change `roster-model`'s underlying name-based dedupe rule (`AttachedUnitAggregator` still lists "Assault Intercessor" and "Assault Intercessor Sergeant" as two distinct `Statlines` entries); it is what makes two identically-valued entries collapse into one row here.

#### Scenario: Squad with a distinct sergeant statline sharing the same values
- **WHEN** a unit's aggregate view contains both an "Assault Intercessor" and an "Assault Intercessor Sergeant" statline entry with identical M/T/Sv/W/Ld/Oc values
- **THEN** the rendered block shows a single collapsed statline row, listing both names together, with the shared M/T/Sv/W/Ld/Oc values shown once

#### Scenario: Statlines with differing values stay on separate rows
- **WHEN** a unit's aggregate view contains two statline entries whose M/T/Sv/W/Ld/Oc values differ from each other
- **THEN** the rendered block shows two separate statline rows, each under its own name

### Requirement: Weapon Section Rendering
Each unit block SHALL list every entry in the view's `Weapons`, showing the weapon's name, type (Ranged/Melee), Range, Attacks, Skill, Strength, AP, Damage, and the aggregated live count.

#### Scenario: Weapon count reflects aggregation across components
- **WHEN** a unit's aggregate view has a weapon entry with a combined count greater than any single component's contribution (per `attached-unit-tracker`'s aggregation rule)
- **THEN** the rendered row shows that combined count, not any individual component's count

### Requirement: Ability Section Rendering
Each unit block SHALL render `UnitScopedAbilities` as one combined list, and SHALL render `ModelScopedAbilities` grouped under the name of their owning model-line, separately from the combined list.

#### Scenario: Unit-scoped ability appears once in the combined section
- **WHEN** a unit's aggregate view has a non-empty `UnitScopedAbilities` list
- **THEN** each ability appears once in the block's combined-abilities section

#### Scenario: Model-scoped ability appears under its own model-line
- **WHEN** a unit's aggregate view has a `ModelScopedAbilities` entry tied to a specific model-line
- **THEN** that ability is rendered under that model-line's own heading, not in the combined-abilities section

### Requirement: Keyword Section Rendering
Each unit block SHALL render the view's `Keywords` as a single list of the unit's effective keywords.

#### Scenario: Keywords render when present
- **WHEN** a unit's aggregate view has a non-empty `Keywords` set
- **THEN** every keyword in that set is rendered somewhere in the unit's block

### Requirement: Empty Sections Render Without Error
If any of `Statlines`, `Weapons`, `UnitScopedAbilities`, `ModelScopedAbilities`, or `Keywords` is empty for a given unit, the page SHALL render that unit's block without error, omitting or clearly marking the empty section.

#### Scenario: Unit with no unit-scoped abilities
- **WHEN** a unit's aggregate view has an empty `UnitScopedAbilities` list
- **THEN** the page renders that unit's block successfully, with no exception and no blank/broken markup in the abilities section
