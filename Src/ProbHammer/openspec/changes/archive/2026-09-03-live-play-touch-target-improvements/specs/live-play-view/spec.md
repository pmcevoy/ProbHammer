## MODIFIED Requirements

### Requirement: Unit Status Toolbar
Each unit block SHALL render one status toolbar, in its own row, between the unit-name header and
the Statline section — never inside the unit-name header row itself, and never contingent on that
header's own text wrapping to more than one line. The toolbar SHALL always render three controls,
in this order: Half Strength, Battle-shock, Reset Casualties, visually separated from one another
(e.g. by a dividing rule) and centered within the toolbar row. All three controls SHALL always be
present, regardless of whether their current state is "nothing to show" — unlike the header glyphs
this toolbar replaces, no control disappears based on state.

Each control SHALL independently communicate two states:
- **Actionable**, when activating it currently has an effect, rendered with a distinct "actionable"
  background; or **inert**, when activating it currently has no effect, rendered with a distinct,
  visually muted "inert" background. An inert control SHALL NOT respond to activation.
- **Active**, when the fact it represents is currently true, rendered with an amber glyph; or
  **inactive**, when that fact is false, rendered with the control's ordinary (non-amber) glyph
  color. This applies even to an inert control — its glyph color still reflects whether the
  represented fact is true.

The three controls' actionable/active semantics:
- **Half Strength**, for a combat unit with a combined starting strength of 1: always actionable;
  activating it toggles that unit's player-set half-strength status (per `attached-unit-tracker`'s
  Single-Model Half-Strength Is Player-Set requirement); active exactly when that status is true.
- **Half Strength**, for a combat unit with a combined starting strength of 2 or more: always
  inert (the computed determination is read-only); active exactly when the unit is at or below
  half-strength per that computed determination.
- **Battle-shock**: always actionable, for every unit regardless of starting strength; activating
  it toggles that unit's Battle-shocked status (per `attached-unit-tracker`'s Battle-Shocked Status
  Is Player-Set requirement); active exactly when that status is true.
- **Reset Casualties**: actionable exactly when the unit currently has at least one casualty
  recorded; inert otherwise. Active (amber glyph) exactly when actionable — there is no separate
  notion of this control's represented fact being true independent of whether it can currently be
  activated.

The toolbar and its controls SHALL remain visible and, for actionable controls, functional
regardless of whether that unit block's Statline, Ranged Weapons, or Melee Weapons sections are
currently expanded or collapsed, and regardless of how many lines the unit-name header itself wraps
to.

Each control's activatable (tap/click) region SHALL be its entire toolbar item — the icon glyph
together with its caption label — not only the icon glyph by itself. The caption label SHALL NOT
render as a separate, non-interactive element sitting outside the control's own hit area; tapping
either the glyph or its adjoining label SHALL activate the same control, with the same
actionable/inert gating.

#### Scenario: The toolbar renders as its own row, independent of the header
- **WHEN** a unit block's name is long enough to wrap to multiple lines in its header
- **THEN** the status toolbar still renders as a single row immediately below that header, with all
  three controls fully visible

#### Scenario: All three controls always render
- **WHEN** a unit has no recorded casualties and is not at or below half-strength
- **THEN** its toolbar still renders all three controls, with Reset Casualties shown inert rather
  than omitted

#### Scenario: A single-model unit's Half Strength control is always actionable
- **WHEN** a unit block's combat unit has a combined starting strength of 1
- **THEN** its Half Strength control renders with an actionable background regardless of that
  unit's current half-strength status, and activating it toggles that status

#### Scenario: Marking a single-model unit at half-strength changes only the glyph color
- **WHEN** a player activates the Half Strength control on a unit with a combined starting strength
  of 1, setting its player-set half-strength status to true
- **THEN** the control's glyph renders amber while its background stays actionable; activating it
  again sets the status back to false and the glyph returns to its ordinary color

#### Scenario: A multi-model unit's Half Strength control is always inert
- **WHEN** a unit block's combat unit has a combined starting strength of 2 or more
- **THEN** its Half Strength control renders with an inert background and does not respond to
  activation, regardless of that unit's current half-strength status

#### Scenario: A multi-model unit's Half Strength glyph still reflects the computed status
- **WHEN** a unit block's combat unit has a combined starting strength of 2 or more and casualties
  bring it to at or below half-strength
- **THEN** its (inert) Half Strength control's glyph renders amber; when the unit is above
  half-strength, the same control's glyph renders in its ordinary color

#### Scenario: The Battle-shock control defaults to its ordinary glyph color
- **WHEN** a unit block's combat unit has a Battle-shocked status of false
- **THEN** its Battle-shock control renders with an actionable background and its ordinary
  (non-amber) glyph color

#### Scenario: Activating the Battle-shock control marks the unit Battle-shocked
- **WHEN** a player activates a unit's Battle-shock control
- **THEN** that unit's Battle-shocked status becomes true and the control's glyph renders amber;
  activating it again sets the status back to false and the glyph returns to its ordinary color

#### Scenario: Reset Casualties is inert until there is something to reset
- **WHEN** a unit currently has no recorded casualties
- **THEN** its Reset Casualties control renders with an inert background and an ordinary
  (non-amber) glyph, and does not respond to activation

#### Scenario: Reset Casualties becomes actionable once a casualty is recorded
- **WHEN** a unit has at least one recorded casualty
- **THEN** its Reset Casualties control renders with an actionable background and an amber glyph

#### Scenario: The toolbar stays visible regardless of section disclosure state
- **WHEN** a unit block's Statline, Ranged Weapons, and Melee Weapons sections are all collapsed
- **THEN** its toolbar and all three controls remain visible and, for actionable controls,
  functional

#### Scenario: Tapping a control's caption label activates it, not only its icon
- **WHEN** a player taps or clicks an actionable control's caption label text rather than its icon
  glyph
- **THEN** the control activates exactly as if its icon had been tapped

#### Scenario: An inert control's caption label does not activate it
- **WHEN** a player taps or clicks an inert control's caption label text
- **THEN** the control does not respond, matching the icon's own inert behavior

### Requirement: Keyword Filter Expands And Highlights Matching Units
While a keyword's filter is active, every unit block whose own Keywords section contains that
keyword SHALL have both its Keywords section AND its own containing unit block expanded (if not
already), without collapsing any section or unit block the player had independently expanded or
collapsed. Within each such matching unit's Keywords section, the specific pill(s) matching an
active keyword SHALL render with a distinct flagged/highlighted style, separate from the All
Keywords section's own active-control pill style, and not itself an interactive control.
Deactivating a keyword's filter SHALL remove that keyword's flagged style from every unit's pill,
but SHALL NOT collapse any Keywords section or unit block it had expanded.

#### Scenario: Activating a filter expands every matching unit's Keywords section
- **WHEN** a player activates a keyword's filter, and one or more units have that keyword in their
  own Keywords section
- **THEN** each such unit's Keywords section expands (if collapsed), showing its keyword pills

#### Scenario: Activating a filter also expands the matching unit's own block
- **WHEN** a player activates a keyword's filter, and a matching unit's own block is currently
  collapsed (e.g. via a prior row-label phase/turn selection, or a manual collapse)
- **THEN** that unit's block expands as well as its Keywords section, so the match is actually
  visible rather than forced open inside a still-collapsed block

#### Scenario: A non-matching unit's Keywords section is left alone
- **WHEN** a player activates a keyword's filter
- **THEN** any unit whose Keywords section does not contain that keyword has neither its Keywords
  section nor its own unit block forced open or closed

#### Scenario: The matching keyword's own pill is flagged within a matching unit
- **WHEN** a unit's Keywords section is expanded due to an active keyword filter
- **THEN** that specific keyword's pill within the unit's own section renders with the flagged
  style, while the unit's other keyword pills render in their ordinary, unflagged style

#### Scenario: Deactivating a filter removes the flag but does not collapse the section
- **WHEN** a player deactivates a keyword filter that had expanded one or more units' Keywords
  sections and unit blocks
- **THEN** the flagged style is removed from every matching pill, but each previously-expanded
  Keywords section and unit block remains expanded

#### Scenario: A manually-expanded section is unaffected by an unrelated filter change
- **WHEN** a player has manually expanded a unit's Keywords section, and then activates or
  deactivates a keyword filter that unit does not carry
- **THEN** that unit's Keywords section's expanded/collapsed state is unchanged by the filter change

## ADDED Requirements

### Requirement: Keyword Filter Scrolls To First Match
Activating a keyword's filter SHALL scroll the first unit block (in page order) whose Keywords
section contains that keyword into view, positioned at the top of the viewport. This SHALL happen
only on activation of that specific keyword, never on deactivation, and never as a side effect of
an unrelated page update (a casualty adjustment, a phase/turn selection, or activating a different
keyword) re-evaluating which units currently match any active filter.

#### Scenario: Activating a filter scrolls to the first matching unit
- **WHEN** a player activates a keyword's filter, and one or more units have that keyword in their
  own Keywords section
- **THEN** the page scrolls so that the first such unit, in page order, is positioned at the top of
  the viewport, with that unit's block and Keywords section both expanded

#### Scenario: Deactivating a filter does not scroll the page
- **WHEN** a player deactivates an active keyword's filter
- **THEN** the page does not scroll as a result

#### Scenario: An unrelated page update does not re-trigger the scroll
- **WHEN** a casualty adjustment, status toggle, or phase/turn selection causes the page to
  re-render while a keyword filter is already active
- **THEN** the page does not scroll to the filter's matching unit as a result of that unrelated
  update

### Requirement: Army Header Full Collapse
The Army Header SHALL support collapsing to show only its own name bar, independent of and in
addition to its existing Rules/All Keywords section disclosures — collapsing it hides the metadata
line and every section together, in one action, mirroring "Unit Block Full Collapse" exactly (the
same whole-block collapse mechanism, applied to the one page-wide header instead of a per-unit
block). The phase/turn tracker (`live-play-phase-tracker`) renders as its own element outside the
Army Header entirely, so it is never affected by this collapse. On initial page render, the Army
Header SHALL render expanded (not collapsed) by default. Collapsing it SHALL NOT alter any casualty
or status state — it is a presentation-only change, fully reversible by expanding it again. Each of
its own nested sections (Rules, All Keywords) SHALL keep its own independent collapsed/expanded
state across the header being collapsed and re-expanded.

#### Scenario: Collapsing the Army Header hides its metadata and sections together
- **WHEN** a player collapses the Army Header
- **THEN** its metadata line, Rules section, and All Keywords section are all hidden, leaving only
  its name bar visible

#### Scenario: The phase/turn tracker is unaffected by the Army Header's collapse
- **WHEN** the Army Header is collapsed
- **THEN** the phase/turn tracker, rendered separately below it, remains visible and fully
  functional

#### Scenario: The Army Header loads expanded by default
- **WHEN** a player navigates to `/LivePlay`
- **THEN** the Army Header renders expanded, not collapsed

#### Scenario: Expanding the header restores each nested section's own prior state
- **WHEN** a player had left the Rules section expanded, then collapsed the Army Header, then
  expands the header again
- **THEN** the Rules section renders expanded, matching the state it held before the header was
  collapsed

#### Scenario: Collapsing the Army Header does not affect unit-block collapse state
- **WHEN** a player collapses or expands the Army Header
- **THEN** no unit block's own collapsed/expanded state changes as a result
