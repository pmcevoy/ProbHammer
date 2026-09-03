## MODIFIED Requirements

### Requirement: Phase/Turn Selector Control
The `/LivePlay` page SHALL render exactly one phase/turn selector control for the whole page — never
one per unit block, unlike the per-unit status toolbar (Half Strength/Battle-shock/Reset
Casualties) — positioned between the Army Header and the first unit block, outside the Army Header
entirely, so the control remains visible regardless of whether the Army Header is currently
collapsed (see `live-play-view`'s "Army Header Full Collapse"). The control is structured as
a grid with two rows (My Turn, Their Turn) and five phase columns (Command, Movement, Shooting,
Charge, Fight). Each row's own label SHALL also be independently selectable, as
a twelfth and thirteenth cell alongside the ten Turn/Phase cells. Exactly one of these twelve cells
SHALL be the current selection at all times. A row-label cell's selection carries that row's Turn
with no specific Phase. A browser/session that has never recorded a selection SHALL default to the
My Turn / Command Phase cell. Selecting a different cell SHALL replace the current selection with
the newly selected cell; the previously selected cell SHALL no longer be shown as selected.

#### Scenario: The control renders as a 2x5 grid with two selectable row labels
- **WHEN** a player navigates to `/LivePlay`
- **THEN** the page shows one selector control with two rows (My Turn, Their Turn), each with five
  phase cells (Command, Movement, Shooting, Charge, Fight), and each row's own label is itself a
  selectable cell

#### Scenario: Exactly one control renders for the whole page, not one per unit
- **WHEN** a player navigates to `/LivePlay` with a roster containing more than one unit block
- **THEN** exactly one phase/turn selector control renders on the page, between the Army Header and
  the first unit block, regardless of how many unit blocks the roster has — unlike the per-unit
  status toolbar, it is not repeated per unit block

#### Scenario: A session with no prior selection defaults to My Turn / Command Phase
- **WHEN** `/LivePlay` is opened in a session that has never recorded a phase/turn selection
- **THEN** the My Turn / Command cell renders as the current selection

#### Scenario: Selecting a cell replaces the current selection
- **WHEN** a player selects a cell other than the currently selected one
- **THEN** the newly selected cell becomes the current selection and the previously selected cell no
  longer renders as selected

#### Scenario: Selecting a row label selects that turn with no specific phase
- **WHEN** a player selects the "My Turn" (or "Their Turn") row label itself, rather than one of its
  five phase cells
- **THEN** that row label renders as the current selection, distinct from any of its own row's five
  phase cells

#### Scenario: Exactly one cell is ever selected
- **WHEN** the page is in any state
- **THEN** exactly one of the twelve selectable cells (ten Turn/Phase cells, two row labels) renders
  as the current selection, never zero and never more than one

#### Scenario: The control stays visible when the Army Header is collapsed
- **WHEN** the Army Header is collapsed
- **THEN** the phase/turn selector control remains visible and fully functional, since it renders
  outside the Army Header entirely

## ADDED Requirements

### Requirement: Phase/Turn Selection Also Drives Unit Block Collapse
Every phase/turn selection SHALL, in addition to the existing inner-section Forced set described in
"Section Relevance By Turn And Phase", also fully set every unit block's own collapsed/expanded
state (per `live-play-view`'s "Unit Block Full Collapse"), determined by the kind of cell selected:
a row label (either "My Turn" or "Their Turn", no Phase) SHALL collapse every unit block to its own
name bar, giving a compact, low-scroll list of every unit's name; a specific phase-column cell (one
of the five phases under either Turn) SHALL instead expand every unit block, so that phase's own
Forced/Expanded inner sections (per "Section Relevance By Turn And Phase") are actually visible
rather than forced open server-side yet hidden inside a still-collapsed block. This is a full
override applied on every phase/turn selection, not a one-time transition or a carry-forward: a
unit block the player manually collapsed or expanded themselves is still overridden the next time
any phase/turn cell is selected, including reselecting the cell already active. This bulk
collapse/expand SHALL NOT alter any unit's own casualty or status state, matching the
presentation-only guarantee `live-play-view`'s Unit Block Full Collapse requirement already makes
for a single block.

#### Scenario: Selecting a row label collapses every unit block
- **WHEN** a player selects the "My Turn" (or "Their Turn") row-label cell
- **THEN** every unit block on the page collapses to its own name bar, regardless of its prior
  collapsed/expanded state

#### Scenario: Selecting a phase column expands every unit block
- **WHEN** a player selects a specific phase-column cell (e.g. My Turn / Shooting)
- **THEN** every unit block on the page expands, regardless of its prior collapsed/expanded state,
  so that phase's own Forced/Expanded inner sections are visible rather than hidden

#### Scenario: A unit block expanded by a phase-column selection can still be manually collapsed
- **WHEN** a unit block has been expanded by a phase-column selection
- **THEN** the player can still collapse that individual block by activating its own name bar,
  independent of the current phase/turn selection - until the next phase/turn selection overrides
  it again

#### Scenario: Moving from a row label into a specific phase re-expands every unit block
- **WHEN** a player selects a row-label cell, collapsing every unit block, and then selects one of
  that row's own phase columns
- **THEN** every unit block expands, and that phase's own Forced/Expanded inner sections (per
  "Section Relevance By Turn And Phase") render correctly rather than staying hidden inside a
  collapsed block

#### Scenario: Moving from one phase to another leaves unit blocks expanded
- **WHEN** a player has a specific phase column selected, with every unit block expanded, and
  selects a different phase column
- **THEN** every unit block remains expanded; only the inner Forced/Expanded sections change, per
  "Section Relevance By Turn And Phase"

#### Scenario: Reselecting the row label already active re-collapses every unit block
- **WHEN** a player has a row label selected, with every unit block collapsed, and a unit block has
  since been manually expanded, and the player reselects that same row label
- **THEN** every unit block - including the manually-expanded one - collapses again

#### Scenario: The bulk collapse/expand does not alter casualty or status state
- **WHEN** a phase/turn selection changes every unit block's collapsed/expanded state
- **THEN** no unit's casualty counts, Half Strength status, or Battle-shocked status changes as a
  result
