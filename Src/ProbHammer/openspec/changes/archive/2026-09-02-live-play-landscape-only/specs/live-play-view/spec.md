## MODIFIED Requirements

### Requirement: Army Header Rendering
The `/LivePlay` page SHALL render a header, above every unit block, showing the ArmyRoster's Name,
Faction (in declared order), BattleSize together with PointsSpent and PointsLimit, and
ForceDisposition. Below this, the page SHALL render every distinctly-named `ArmyRule`-origin
ability present on any unit in the roster together with every selected Detachment's own resolved
rule(s), grouped into two columns — one for army-wide rules, one for Detachment rules — under a
section heading of its own. This Rules section SHALL follow the same collapsible `lp-section`
disclosure convention used elsewhere on the page (matching the existing All Keywords section),
collapsed by default. Each individual rule SHALL render as its own named, popover-capable trigger
(per the existing Ability And Rule Text Popover mechanism), showing that rule's full text when
tapped, rather than rendering its text inline by default. A Detachment SHALL render its own name
once, as a plain (non-interactive) label, with one popover trigger beneath it per rule it
resolved — including zero triggers when it resolved no rule at all. Neither a Detachment's own DP
cost nor any unit's own points cost SHALL be shown anywhere on the page, even though the roster's
own total PointsSpent/PointsLimit renders in the header.

#### Scenario: Army metadata renders above the unit blocks
- **WHEN** `/LivePlay` renders a roster
- **THEN** the page shows the roster's Name, Faction, BattleSize, PointsSpent/PointsLimit, and
  ForceDisposition above every unit block

#### Scenario: Rules section collapses by default
- **WHEN** a player navigates to `/LivePlay`
- **THEN** the Rules section renders in a collapsed state, showing no Army/Detachment rule
  triggers until expanded

#### Scenario: A Detachment with one rule shows one popover trigger under its name
- **WHEN** a selected Detachment resolved exactly one rule
- **THEN** the header shows that Detachment's own name as a plain label, with one popover trigger
  beneath it for its rule

#### Scenario: A Detachment with more than one rule shows one trigger per rule
- **WHEN** a selected Detachment resolved more than one rule (e.g. "Black Spear Task Force",
  resolving both "Kill Teams" and "Mission Tactics")
- **THEN** the header shows that Detachment's own name once, as a plain label, with one popover
  trigger per resolved rule listed beneath it

#### Scenario: A Detachment with no resolved rule shows its name with no popover trigger
- **WHEN** a selected Detachment's resolved rule list is empty
- **THEN** the header shows that Detachment's own name as a plain label with no popover trigger
  beneath it

#### Scenario: More than one ArmyRule-origin ability renders in the army-wide column
- **WHEN** the roster's units carry more than one distinctly-named `ArmyRule`-origin ability (e.g.
  a Drukhari army's "Power from Pain" and "Corsairs and Travelling Players")
- **THEN** the header's army-wide column shows one popover trigger per distinctly-named ability

#### Scenario: No ArmyRule ability omits the army-wide column
- **WHEN** no unit in the roster carries an `ArmyRule`-origin ability
- **THEN** the header's rules section omits the army-wide column entirely, rather than rendering it
  empty

#### Scenario: An ArmyRule ability continues to render on its own unit too
- **WHEN** a unit carries an `ArmyRule`-origin ability that is also shown in the header's army-wide
  column
- **THEN** that unit's own existing per-unit rendering of the ability (per the existing
  dedupe-and-span behavior) is unchanged by the header also showing it

#### Scenario: No per-Detachment or per-unit points cost is shown
- **WHEN** the header renders its Detachment rules, or any unit block renders
- **THEN** no individual Detachment's own DP cost and no individual unit's own points cost is
  rendered anywhere on the page

### Requirement: Statline Ability Column Rendering
Each unit block's Statline area SHALL render one additional Abilities column alongside the
statline column, holding both Model-scoped and Unit-scoped ability entries stacked together in one
list — never as two separate side-by-side columns. Within this column, an ability entry bound to a
specific statline row SHALL render beside that row only; an ability entry with no statline-row
binding but belonging to one component (a component-wide ability) SHALL render beside the first
rendered statline row belonging to its component, and SHALL visually span every row belonging to
that component; an ability entry belonging to no single component (an Army Rule-origin ability,
always promoted regardless of contributor count) SHALL render as its own row, above every
component's statline rows, aligned to none of them. Within a single cell holding both scopes,
Model-scoped entries SHALL render before Unit-scoped entries. Each ability SHALL render by Name
only; ability descriptive Text is not rendered inline by this requirement, but is available on
demand via the "Ability And Rule Text Popover" requirement below.

A row-bound ability cell SHALL collapse (hide its ability names) whenever its own row's run is
collapsed, per "Statline Section Rendering" — i.e. whenever every entry in that run is either
fully deselected or fully dead. A component-wide, row-spanning ability cell SHALL collapse whenever
every run within its visual span is collapsed by that same rule. It SHALL remain fully expanded as
long as at least one entry in at least one row within its span is neither fully deselected nor
fully dead. This applies regardless of the scope(s) of ability held in the cell. An ability entry
belonging to no single component SHALL collapse only once every component that contributes to it
has every one of its model-lines reach a remaining count of 0 — not per this-entry's-own-span like
the component-wide case, since the fact it represents remains true as long as any part of the
attached formation still lives.

#### Scenario: A row-bound ability renders beside its own statline row
- **WHEN** an ability entry carries the name of a specific statline
- **THEN** it renders in the Abilities column beside that specific statline row, not beside any
  other row of the same component

#### Scenario: A component-wide ability spans every row of its component
- **WHEN** an ability entry carries no statline name (a Datasheet-sourced, component-wide ability)
  and its owning component renders three statline rows (e.g. two merged into one shared tile and
  one separate)
- **THEN** it renders once, beside the first of those three rows, visually spanning all three

#### Scenario: A component-less ability renders above every component, aligned to none
- **WHEN** an ability entry belongs to no single component (an Army Rule-origin ability, promoted
  whether contributed by one component of a standalone Unit or shared by several of an
  AttachedUnit)
- **THEN** it renders once, in its own row above every component's statline rows, not aligned to
  or spanning any one component's rows specifically

#### Scenario: Column placement is determined by Ability Scope
- **WHEN** a component contributes both a Model-scoped and a Unit-scoped ability at the same
  position (row-bound, component-wide, or component-less)
- **THEN** both render together as one stacked list in the same single Abilities column, with the
  Model-scoped entry appearing before the Unit-scoped entry — Ability Scope determines each entry's
  position within the shared list, not which of two columns it renders in

#### Scenario: Only the ability name renders
- **WHEN** an ability entry renders in the Abilities column
- **THEN** only its Name is shown inline; its descriptive Text is not rendered inline (see
  "Ability And Rule Text Popover" for on-demand access to it)

#### Scenario: A row-bound ability cell collapses with its own row
- **WHEN** a row-bound ability entry's own row is collapsed because every entry in that row's run
  is fully deselected or fully dead
- **THEN** that ability's name is hidden along with the rest of the row's collapsed content

#### Scenario: A component-wide ability cell stays expanded while any spanned row is still selected
- **WHEN** a component-wide ability visually spans three statline rows and only two of those rows
  are collapsed (fully deselected or fully dead), with the third still having a selected or partial
  entry with a nonzero remaining count
- **THEN** the ability cell remains fully expanded

#### Scenario: A component-wide ability cell collapses once every spanned row is fully deselected
- **WHEN** a component-wide ability visually spans three statline rows and every entry in all three
  rows becomes fully deselected
- **THEN** the ability cell collapses (its ability names hide)

#### Scenario: A component-wide ability cell collapses once every spanned row is fully dead
- **WHEN** a component-wide ability visually spans three statline rows and every entry in all three
  rows reaches a remaining count of 0
- **THEN** the ability cell collapses (its ability names hide), matching the fully-deselected case

#### Scenario: A component-less ability cell stays expanded while any contributing component lives
- **WHEN** a component-less ability entry is contributed by three components, and only two of them
  have every model-line reach a remaining count of 0
- **THEN** the ability cell remains fully expanded, since the third contributing component still
  has a present model-line

#### Scenario: A component-less ability cell collapses once every contributing component is dead
- **WHEN** a component-less ability entry is contributed by three components, and every one of them
  has every model-line reach a remaining count of 0
- **THEN** the ability cell collapses (its ability name hides)

#### Scenario: A row-bound ability sharing a single-row component's span renders together, not hidden
- **WHEN** a component renders exactly one statline row, and that row carries both a row-bound
  ability and a component-wide ability
- **THEN** both abilities render together in one cell at that row's position — the row-bound
  ability is never rendered as a separate cell that would occupy the identical grid position as
  the component-wide cell and be visually hidden behind it

#### Scenario: A row-bound ability on a multi-row component still renders in its own cell
- **WHEN** a component renders more than one statline row, and one of those rows carries a
  row-bound ability while a component-wide ability spans all of that component's rows
- **THEN** the row-bound ability still renders in its own cell at its specific row, distinct from
  the component-wide cell spanning the full range — only the single-row case merges them

## ADDED Requirements

### Requirement: Narrow-Viewport Orientation Gate
When `/LivePlay` is viewed on a narrow viewport (phone-class, at or below a width threshold at
which the page's layout is known to lose or garble information — max-width ~600px) in portrait
orientation, the system SHALL render a rotate-device prompt in place of the roster, rather than
the roster itself. A viewport wider than that threshold (tablet-class) SHALL render the roster
normally regardless of orientation. The system SHALL detect the viewport's current width and
orientation and update which of the two (prompt or roster) is shown automatically as the device is
rotated or resized, without requiring a page reload or losing any in-progress casualty/status
state.

#### Scenario: A narrow viewport in portrait shows the rotate prompt
- **WHEN** a session with a phone-class viewport (at or below the width threshold) navigates to
  `/LivePlay` while held in portrait orientation
- **THEN** the page shows a rotate-device prompt and does not render the roster

#### Scenario: A narrow viewport in landscape shows the roster
- **WHEN** a session with a phone-class viewport is in landscape orientation
- **THEN** the page renders the roster normally, with no rotate prompt

#### Scenario: A wide viewport in portrait is exempt
- **WHEN** a session with a tablet-class viewport (above the width threshold) is in portrait
  orientation
- **THEN** the page renders the roster normally, with no rotate prompt

#### Scenario: Rotating the device reveals the roster without a reload
- **WHEN** a phone-class session is showing the rotate prompt and the player physically rotates
  the device to landscape
- **THEN** the roster becomes visible in place of the prompt without a page reload, and any
  casualty/status state already recorded for the session is unaffected

### Requirement: Unit Block Full Collapse
Each unit block SHALL support collapsing to show only its own name bar, independent of and in
addition to its existing per-section (Statline/Ranged/Melee/Keywords) disclosures — collapsing a
unit block hides its status toolbar and every section together, in one action. Each unit block's
collapsed/expanded state SHALL be independent of every other unit block's. On initial page render,
every unit block SHALL render expanded (not collapsed) by default. Collapsing a unit block SHALL
NOT alter its casualty or status state — it is a presentation-only change, fully reversible by
expanding the block again. A subsequent re-render of a unit block not itself acted upon (a
casualty/status/phase-turn sync affecting other units) SHALL preserve that block's own prior
collapsed/expanded state.

#### Scenario: Collapsing a unit block hides everything but its name bar
- **WHEN** a player collapses a unit block
- **THEN** that block's status toolbar and every section (Statline, Ranged Weapons, Melee Weapons,
  Keywords) are hidden, leaving only its name bar visible

#### Scenario: Collapsing one unit block does not affect others
- **WHEN** a player collapses one unit block
- **THEN** every other unit block's own collapsed/expanded state is unchanged

#### Scenario: Unit blocks load expanded by default
- **WHEN** a player navigates to `/LivePlay`
- **THEN** every unit block renders expanded (not collapsed)

#### Scenario: An unrelated sync does not reset a collapsed unit block
- **WHEN** a unit block is collapsed and the player then adjusts a casualty or phase/turn selection
  affecting a different unit block
- **THEN** the collapsed unit block remains collapsed after the resulting re-render
