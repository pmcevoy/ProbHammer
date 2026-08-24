## ADDED Requirements

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

## REMOVED Requirements

### Requirement: Unit Header Status Glyphs
**Reason**: The Half-Strength and Battle-shock glyphs, previously right-aligned within the
unit-name header row, are superseded by the "Unit Status Toolbar" requirement above — a dedicated
row between the header and the Statline section, holding those two controls plus Reset Casualties
(previously placed in the Statline section's own summary bar, which carried no requirement of its
own). This was needed because a long unit name wrapping the header row could push the glyphs out
of view.
**Migration**: See the "Unit Status Toolbar" requirement for the replacement placement and
rendering rules.

## MODIFIED Requirements

### Requirement: Battle-Shocked Objective Control Rendering
Every Objective Control stat-tile belonging to any component of a Battle-shocked combat unit SHALL
render using the same visual treatment as a caveated Invulnerable Save tile (a background that
visually distinguishes it from an ordinary stat-tile), with its displayed value replaced by the
same glyph used for the toolbar's Battle-shock control, rendered in the ordinary stat-value text
color — never the tile's numeric Objective Control value, and never in an amber or otherwise
distinct color. The tile's label SHALL remain "OC", unchanged, and no explanatory text SHALL render
beneath it — unlike a caveated Invulnerable Save tile, there is only one fixed reason (the unit is
Battle-shocked), already conveyed by the toolbar's own Battle-shock control, so no per-instance
footnote is needed. This rendering applies to every component's Objective Control tile within a
Battle-shocked combat unit, not only whichever statline run happens to sit nearest the unit block's
own toolbar.

#### Scenario: An ordinary unit's Objective Control tile shows its numeric value
- **WHEN** a unit block's combat unit is not Battle-shocked
- **THEN** every Objective Control tile in that block renders its ordinary numeric value with no
  special background

#### Scenario: A Battle-shocked unit's Objective Control tile shows the glyph instead of a number
- **WHEN** a unit block's combat unit is Battle-shocked
- **THEN** every Objective Control tile in that block renders the flagged-tile background and shows
  the Battle-shock glyph, in the ordinary stat-value text color, in place of the numeric value

#### Scenario: Every component's Objective Control tile is affected, not just one
- **WHEN** an AttachedUnit is Battle-shocked and has more than one statline run across its
  Bodyguard and Attached components
- **THEN** every one of those runs' Objective Control tiles renders the flagged treatment, not only
  the run nearest the unit block's own toolbar

#### Scenario: Clearing Battle-shocked status reverts the tile
- **WHEN** a player clears a unit's Battle-shocked status
- **THEN** every Objective Control tile in that unit's block reverts to its ordinary numeric
  rendering
