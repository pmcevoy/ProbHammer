## MODIFIED Requirements

### Requirement: Live Play Page Lists Example Army Units
The `/LivePlay` page SHALL render one block per `AttachedUnitAggregateView` returned by
`Examples.View.MyArmy()`, with no unit omitted or duplicated, ordered as follows: views whose
source `ICombatUnit` is an `AttachedUnit` SHALL render before views sourced from a plain `Unit`;
within each of those two groups, views SHALL be ordered by descending total model count (the sum
of `InitialCount` across the view's `Statlines`). Views with equal total model count within a
group SHALL be ordered by ascending `Name` (ordinal, case-insensitive).

An `AttachedUnit` with zero attached units still counts as attached-sourced for this ordering —
it sorts ahead of plain `Unit`-sourced views even though its rendered content (statlines,
weapons, abilities) is identical to what a plain `Unit` with the same Bodyguard would produce.

#### Scenario: Page loads with the example army
- **WHEN** a user navigates to `/LivePlay`
- **THEN** the page displays one rendered block for each unit in `Examples.View.MyArmy()`, with no
  unit omitted or duplicated

#### Scenario: Attached-sourced units render before plain units
- **WHEN** the example army contains both `AttachedUnit`-sourced and plain `Unit`-sourced views
- **THEN** every attached-sourced block renders above every plain-unit block, regardless of either
  group's model counts

#### Scenario: Larger units render first within a group
- **WHEN** two views in the same group (both attached-sourced, or both plain-unit-sourced) have
  different total model counts
- **THEN** the view with the larger total model count renders first

#### Scenario: Equal-sized units within a group break ties by name
- **WHEN** two views in the same group have equal total model counts (e.g. two "Crusader Squad
  with ..." blocks with the same total model count)
- **THEN** they render adjacent to each other, ordered by ascending `Name`

#### Scenario: An attached unit with no attachments still sorts as attached
- **WHEN** a view is sourced from an `AttachedUnit` with zero attached units
- **THEN** it is grouped with the attached-sourced units for ordering purposes, ahead of
  plain-`Unit`-sourced views, even though its rendered block looks identical to a plain unit's
