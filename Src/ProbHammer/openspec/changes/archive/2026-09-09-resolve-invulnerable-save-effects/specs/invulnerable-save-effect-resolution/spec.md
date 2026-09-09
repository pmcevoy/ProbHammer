## Purpose

Resolves a classified invulnerable-save Effect (from the rule-effect-classification capability) and
its source Ability into a real, displayable `InvulnerableSaveCharacteristicView` — the InSv-specific
counterpart to the characteristic-modification-kind capability's scalar resolution, proven correct in
isolation ahead of any roster-resolution-time wiring.

## ADDED Requirements

### Requirement: Resolving An Invulnerable-Save Effect Into A View
Given a classified invulnerable-save Effect and its source Ability, the system SHALL produce a
resolved (non-caveated) `InvulnerableSaveCharacteristicView` whose derived value is the Effect's own
melee/ranged pair, whose pre-mutation original value is preserved from the characteristic's existing
value before this mutation, and whose contributing abilities record the source Ability.

#### Scenario: A uniform grant resolves to an equal melee/ranged value
- **WHEN** resolving an invulnerable-save Effect stating a uniform grant (melee and ranged both `5`)
  against a source Ability
- **THEN** the resolved view's value is `5` against both melee and ranged attacks, and the source
  Ability is recorded as a contributing ability

#### Scenario: A restricted grant resolves with the absent side as no invulnerable save
- **WHEN** resolving an invulnerable-save Effect stating a value for only one attack type (e.g.
  Ranged `4`, no value for melee) against a source Ability
- **THEN** the resolved view's value is `4` against ranged attacks and no invulnerable save against
  melee attacks

#### Scenario: The pre-mutation original value is preserved
- **WHEN** resolving an invulnerable-save Effect against a characteristic whose existing value before
  this mutation differs from the Effect's own stated value
- **THEN** the resolved view's original value is the characteristic's own pre-mutation value, not the
  Effect's stated value

### Requirement: Reproduces An Existing Hand-Authored Rule's Result
The system's resolution SHALL reproduce, for a real hand-authored rule already shipped elsewhere in
this project, the exact result that rule's own hand-written logic already produces — proving the
general resolver is at least as correct as the specific rule it is meant to eventually replace.

#### Scenario: Reproduces Shield Dome's resolved invulnerable save
- **WHEN** resolving the invulnerable-save Effect classified from Shield Dome's own text ("The bearer
  has a 5+ invulnerable save.") against Shield Dome's own Ability
- **THEN** the resolved view matches the result `ShieldDomeStatlineFlagRule.Apply` already produces
  for the same input
