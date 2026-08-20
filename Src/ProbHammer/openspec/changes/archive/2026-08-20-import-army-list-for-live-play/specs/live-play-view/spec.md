## MODIFIED Requirements

### Requirement: Live Play Page Lists Example Army Units
The `/LivePlay` page SHALL render one block per `AttachedUnitAggregateView` produced from the
current session's imported and enriched `ArmyRoster` (see `army-list-import`'s Per-Session Roster
Storage requirement), with no unit omitted or duplicated, ordered as follows: views whose source
`ICombatUnit` is an `AttachedUnit` SHALL render before views sourced from a plain `Unit`; within
each of those two groups, views SHALL be ordered by descending total model count (the sum of
`InitialCount` across the view's `Statlines`). Views with equal total model count within a group
SHALL be ordered by ascending `Name` (ordinal, case-insensitive). Each block SHALL be labeled with
that view's `Name` rather than a positional ordinal.

An `AttachedUnit` with zero attached units still counts as attached-sourced for this ordering —
it sorts ahead of plain `Unit`-sourced views even though its rendered content (statlines, weapons,
abilities) is identical to what a plain `Unit` with the same Bodyguard would produce.

#### Scenario: Page loads with the example army
- **WHEN** a user with an active session import navigates to `/LivePlay`
- **THEN** the page displays one rendered block for each unit in that session's enriched
  `ArmyRoster`, with no unit omitted or duplicated

#### Scenario: Unit block is labeled with its resolved Name
- **WHEN** a unit's aggregate view has `Name` "Sword Brethren with Marshal"
- **THEN** the rendered block's header displays "Sword Brethren with Marshal", not a positional
  label such as "Unit 3"

#### Scenario: Attached-sourced units render before plain units
- **WHEN** the session's imported army contains both `AttachedUnit`-sourced and plain
  `Unit`-sourced views
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

## ADDED Requirements

### Requirement: Live Play Redirects Without An Active Import
When `/LivePlay` is requested by a session with no successfully imported army list, the system
SHALL redirect the user to the import page (per `army-list-import`'s Import Submission
requirement) rather than rendering an empty or erroring page.

#### Scenario: A session with no import is redirected
- **WHEN** a user with no prior successful import navigates to `/LivePlay`
- **THEN** they are redirected to the import page instead of seeing a roster
