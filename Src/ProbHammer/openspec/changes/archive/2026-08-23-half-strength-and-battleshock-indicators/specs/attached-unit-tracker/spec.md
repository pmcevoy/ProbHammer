## ADDED Requirements

### Requirement: Half-Strength Determination
A combat unit's starting strength SHALL be the sum of every one of its components' model-lines'
original Count, computed across every component together — for an AttachedUnit this combines the
Bodyguard and every Attached unit into one total, matching the rule that an attached unit's
starting strength is the number of models it contains at the start of the first battle round. A
combat unit whose starting strength is 2 or more SHALL be considered at or below half-strength
when its current strength — the sum of every component's model-lines' remaining count — is less
than or equal to half its starting strength, rounded down. This determination SHALL NOT apply to a
combat unit whose starting strength is exactly 1 (see "Single-Model Half-Strength Is Player-Set").

#### Scenario: A squad becomes at-or-below half-strength after casualties
- **WHEN** a plain Unit has a combined starting strength of 10 and its current strength is reduced
  to 5
- **THEN** it is considered at or below half-strength (5 <= floor(10/2) = 5)

#### Scenario: An odd starting strength rounds the half-strength threshold down
- **WHEN** a plain Unit has a combined starting strength of 5
- **THEN** its half-strength threshold is 2 (floor(5/2)), so a current strength of 2 is at or below
  half-strength while a current strength of 3 is not — it takes 3 casualties, not 2, to reach it

#### Scenario: A 3-model unit does not reach half-strength after only a single casualty
- **WHEN** a plain Unit has a combined starting strength of 3 and one model is removed as a
  casualty, leaving a current strength of 2
- **THEN** it is not considered at or below half-strength (2 > floor(3/2) = 1); a second casualty,
  leaving a current strength of 1, is what reaches it (1 <= 1)

#### Scenario: An AttachedUnit's starting strength combines every component
- **WHEN** an AttachedUnit has a Bodyguard with a starting strength of 5 and one Attached unit with
  a starting strength of 1
- **THEN** its combined starting strength is 6 and its half-strength threshold is 3, regardless of
  how casualties happen to be distributed between the Bodyguard and the Attached unit

#### Scenario: A unit above the threshold is not at half-strength
- **WHEN** a plain Unit has a combined starting strength of 10 and a current strength of 6
- **THEN** it is not considered at or below half-strength (6 > floor(10/2) = 5)

### Requirement: Single-Model Half-Strength Is Player-Set
For a combat unit whose combined starting strength (per "Half-Strength Determination") is exactly
1, at-or-below-half-strength status SHALL be an independent, player-set boolean rather than a
computed value, since the real determination for a single-model unit is based on lost Wounds and
this app does not track partial wounds on a model-line. This status SHALL default to false (not at
half-strength) and SHALL change only when the player explicitly sets it.

#### Scenario: A single-model unit defaults to not at half-strength
- **WHEN** a combat unit with a combined starting strength of 1 has never had its half-strength
  status set by the player
- **THEN** it is not considered at or below half-strength

#### Scenario: A player marks a single-model unit as at half-strength
- **WHEN** a player sets a combat unit's player-set half-strength status to true
- **THEN** that unit is considered at or below half-strength until the player sets it back to false

#### Scenario: The computed determination never overrides the player-set status
- **WHEN** a combat unit has a combined starting strength of 1
- **THEN** its at-or-below-half-strength status is always whatever the player last set it to,
  never a value derived from model or wound count

### Requirement: Battle-Shocked Status Is Player-Set
Every combat unit SHALL carry an independent, player-set Battle-shocked boolean, defaulting to
false. This status SHALL be entirely player-controlled — the app does not simulate or validate the
2D6-versus-Leadership test a Battle-shock check requires — and SHALL NOT be cleared automatically
by any rule this capability enforces elsewhere (e.g. a casualty adjustment, a change in
half-strength status, or the passage of a game turn the app has no concept of). It changes only
when the player explicitly sets or clears it.

#### Scenario: A unit defaults to not Battle-shocked
- **WHEN** a combat unit's Battle-shocked status has never been set by the player
- **THEN** it is not considered Battle-shocked

#### Scenario: A player marks a unit as Battle-shocked
- **WHEN** a player sets a combat unit's Battle-shocked status to true
- **THEN** that unit is considered Battle-shocked until the player explicitly clears it

#### Scenario: Battle-shocked status is independent of half-strength status
- **WHEN** a combat unit's half-strength status changes (whether computed or player-set)
- **THEN** its Battle-shocked status is unaffected

#### Scenario: A casualty adjustment does not clear Battle-shocked status
- **WHEN** a player marks or restores a casualty on a Battle-shocked combat unit
- **THEN** that unit's Battle-shocked status remains unchanged
