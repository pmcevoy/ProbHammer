## ADDED Requirements

### Requirement: Unit Header Status Glyphs
Each unit block's name header SHALL render a group of status glyphs right-aligned within that same
header row, alongside the unit's name (which stays left-aligned) — never in a separate row or
section. The group SHALL always render the Battle-shock glyph rightmost.

The half-strength glyph's rendering depends on that combat unit's combined starting strength (per
`attached-unit-tracker`'s Half-Strength Determination requirement):
- When starting strength is exactly 1, the glyph SHALL always render as an interactive control.
  Activating it SHALL toggle that unit's player-set half-strength status (per `attached-unit-
  tracker`'s Single-Model Half-Strength Is Player-Set requirement). It SHALL render in the header's
  ordinary text color while that status is false, and in an amber color while it is true.
- When starting strength is 2 or more, the glyph SHALL render only while the unit is at or below
  half-strength per the computed determination, and SHALL NOT be interactive — activating it SHALL
  have no effect. Whenever shown, it SHALL render in an amber color.

The Battle-shock glyph SHALL always render, for every unit regardless of starting strength, as an
interactive control. Activating it SHALL toggle that unit's Battle-shocked status (per `attached-
unit-tracker`'s Battle-Shocked Status Is Player-Set requirement). It SHALL render as an
outline-only shape, in the header's ordinary text color, while that status is false, and as a
solid shape in an amber color while it is true.

Both glyphs SHALL remain visible, and their interactive cases SHALL remain functional, regardless
of whether that unit block's Statline, Ranged Weapons, or Melee Weapons sections are currently
expanded or collapsed.

#### Scenario: A single-model unit's half-strength glyph is always present and interactive
- **WHEN** a unit block's combat unit has a combined starting strength of 1
- **THEN** its header renders the half-strength glyph regardless of that unit's current
  half-strength status, and activating it toggles that status

#### Scenario: Marking a single-model unit at half-strength changes the glyph's color
- **WHEN** a player activates the half-strength glyph on a unit with a combined starting strength
  of 1, setting its player-set half-strength status to true
- **THEN** the glyph renders in amber; activating it again sets the status back to false and the
  glyph returns to the header's ordinary text color

#### Scenario: A multi-model unit's half-strength glyph is hidden above half-strength
- **WHEN** a unit block's combat unit has a combined starting strength of 2 or more and is not at
  or below half-strength
- **THEN** its header does not render the half-strength glyph

#### Scenario: A multi-model unit's half-strength glyph appears once at or below half-strength
- **WHEN** a unit block's combat unit has a combined starting strength of 2 or more and casualties
  bring it to at or below half-strength
- **THEN** its header renders the half-strength glyph in amber, and activating it has no effect on
  that unit's (computed) half-strength status

#### Scenario: The Battle-shock glyph defaults to an outline
- **WHEN** a unit block's combat unit has a Battle-shocked status of false
- **THEN** its header renders the Battle-shock glyph as an outline-only shape in the header's
  ordinary text color

#### Scenario: Activating the Battle-shock glyph marks the unit Battle-shocked
- **WHEN** a player activates a unit's Battle-shock glyph
- **THEN** that unit's Battle-shocked status becomes true and the glyph renders as a solid amber
  shape; activating it again sets the status back to false and the glyph returns to its outline
  rendering

#### Scenario: The Battle-shock glyph is always rightmost
- **WHEN** a unit block's header renders both the half-strength glyph and the Battle-shock glyph
- **THEN** the Battle-shock glyph renders to the right of the half-strength glyph

#### Scenario: Status glyphs remain visible while every section is collapsed
- **WHEN** a unit block's Statline, Ranged Weapons, and Melee Weapons sections are all collapsed
- **THEN** its header's status glyphs (whichever are currently rendered) remain visible and, for
  their interactive cases, functional

### Requirement: Half-Strength And Battle-Shocked State Persist Across Reloads and Restarts
The page SHALL record every player-set half-strength toggle (for a combined-starting-strength-1
unit) and every Battle-shocked toggle so that reloading the page, or restarting the browser, on the
same browser/device reproduces the same statuses shown before the reload — without the player
needing to re-set anything. This persistence SHALL survive the server process itself restarting
between requests, the same guarantee `live-play-view`'s existing Casualty State Persists Across
Reloads and Restarts requirement makes for casualty state. State recorded in one browser/device is
not required to appear when `/LivePlay` is opened from a different browser or device.

#### Scenario: Reloading the page preserves a marked half-strength status
- **WHEN** a player sets a single-model unit's half-strength status to true and then reloads the
  page
- **THEN** the reloaded page still shows that unit as at or below half-strength

#### Scenario: Reloading the page preserves a marked Battle-shocked status
- **WHEN** a player sets a unit's Battle-shocked status to true and then reloads the page
- **THEN** the reloaded page still shows that unit as Battle-shocked

#### Scenario: A server restart between requests does not lose either status
- **WHEN** the server process restarts after a player has set a half-strength or Battle-shocked
  status, and the player then reloads or revisits the page
- **THEN** the page still shows the previously set status

#### Scenario: A browser with no recorded toggles shows the default statuses
- **WHEN** `/LivePlay` is opened in a browser that has never recorded a half-strength or
  Battle-shocked toggle
- **THEN** every unit shows a player-set half-strength status of false and a Battle-shocked status
  of false, alongside whatever the computed half-strength determination reports for a
  multi-model unit

### Requirement: Battle-Shocked Objective Control Rendering
Every Objective Control stat-tile belonging to any component of a Battle-shocked combat unit SHALL
render using the same visual treatment as a caveated Invulnerable Save tile (a background that
visually distinguishes it from an ordinary stat-tile), with its displayed value replaced by the
same glyph used for the header's Battle-shock control, rendered in the ordinary stat-value text
color — never the tile's numeric Objective Control value, and never in an amber or otherwise
distinct color. The tile's label SHALL remain "OC", unchanged, and no explanatory text SHALL render
beneath it — unlike a caveated Invulnerable Save tile, there is only one fixed reason (the unit is
Battle-shocked), already conveyed by the header glyph, so no per-instance footnote is needed. This
rendering applies to every component's Objective Control tile within a Battle-shocked combat unit,
not only whichever component's header the Battle-shock glyph itself renders against.

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
  the run belonging to whichever component the header's own glyph visually sits above

#### Scenario: Clearing Battle-shocked status reverts the tile
- **WHEN** a player clears a unit's Battle-shocked status
- **THEN** every Objective Control tile in that unit's block reverts to its ordinary numeric
  rendering
