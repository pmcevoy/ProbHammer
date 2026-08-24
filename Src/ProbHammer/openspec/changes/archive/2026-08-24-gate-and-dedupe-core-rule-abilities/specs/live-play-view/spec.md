## MODIFIED Requirements

### Requirement: Statline Ability Column Rendering
Each unit block's Statline area SHALL render two additional columns alongside the statline column:
a Model Abilities column for entries whose Ability has Scope Model, and a Unit Abilities column for
entries whose Ability has Scope Unit. Within each column, an ability entry bound to a specific
statline row SHALL render beside that row only; an ability entry with no statline-row binding but
belonging to one component (a component-wide ability) SHALL render beside the first rendered
statline row belonging to its component, and SHALL visually span every row belonging to that
component; an ability entry belonging to no single component (an Army Rule-origin ability, always
promoted regardless of contributor count) SHALL render as its own row, above every component's
statline rows, aligned to none of them. Each ability SHALL render by Name only; ability descriptive
Text is not rendered inline by this requirement, but is available on demand via the "Ability And
Rule Text Popover" requirement below.

A row-bound ability cell SHALL collapse (hide its ability names) whenever its own row's run is
collapsed, per "Statline Section Rendering" — i.e. whenever every entry in that run is either
fully deselected or fully dead. A component-wide, row-spanning ability cell SHALL collapse whenever
every run within its visual span is collapsed by that same rule. It SHALL remain fully expanded as
long as at least one entry in at least one row within its span is neither fully deselected nor
fully dead. This applies identically to the Model Abilities and Unit Abilities columns. An ability
entry belonging to no single component SHALL collapse only once every component that contributes
to it has every one of its model-lines reach a remaining count of 0 — not per this-entry's-own-span
like the component-wide case, since the fact it represents remains true as long as any part of the
attached formation still lives.

#### Scenario: A row-bound ability renders beside its own statline row
- **WHEN** an ability entry carries the name of a specific statline
- **THEN** it renders in its Scope's column beside that specific statline row, not beside any
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
- **WHEN** a component contributes both a Model-scoped and a Unit-scoped ability
- **THEN** the Model-scoped ability renders in the Model Abilities column and the Unit-scoped
  ability renders in the Unit Abilities column, regardless of whether either is row-bound,
  component-wide, or belongs to no single component

#### Scenario: Only the ability name renders
- **WHEN** an ability entry renders in either column
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
  ability and a component-wide ability in the same column
- **THEN** both abilities render together in one cell at that row's position — the row-bound
  ability is never rendered as a separate cell that would occupy the identical grid position as
  the component-wide cell and be visually hidden behind it

#### Scenario: A row-bound ability on a multi-row component still renders in its own cell
- **WHEN** a component renders more than one statline row, and one of those rows carries a
  row-bound ability while a component-wide ability spans all of that component's rows
- **THEN** the row-bound ability still renders in its own cell at its specific row, distinct from
  the component-wide cell spanning the full range — only the single-row case merges them
