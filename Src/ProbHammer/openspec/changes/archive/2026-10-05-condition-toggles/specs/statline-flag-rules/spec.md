## MODIFIED Requirements

### Requirement: Conditional Statline Effects Are Recorded Without Being Applied
When a present ability's catalogue record contains a conditional Statline scalar or invulnerable-save
effect, the system SHALL record it on every Statline entry an unconditional effect of that record
would reach (per Target-Scoped Application), naming the source ability, the characteristic, the
effect's value and its condition, and SHALL NOT change that entry's value. An effect the player has
activated, or one in an unselected option of a choice group with a selection, SHALL NOT be recorded
(per `condition-activation`).

#### Scenario: A conditional Objective Control effect is recorded, not applied
- **WHEN** Helbrecht's Crusader Squad carries Martial Honour ("the first time a model in this
  model's unit makes a melee attack that destroys one or more enemy units ... add 5 to this model's
  Objective Control"), classified as a conditional OC improvement of 5
- **THEN** the bearer's Statline entry records a not-applied OC +5 effect naming Martial Honour and
  its condition, and its OC value is unchanged

#### Scenario: A conditional invulnerable save is recorded on a unit without one
- **WHEN** a unit with no invulnerable save carries Waaagh!, classified as a 5+ invulnerable save
  while the unit is riled up
- **THEN** each of that unit's Statline entries records a not-applied 5+ invulnerable-save effect
  naming Waaagh! and that condition, and its invulnerable save stays absent

#### Scenario: A conditional effect follows bearer liveness
- **WHEN** the bearer of an ability with a recorded conditional Statline effect is marked a casualty
- **THEN** the effect is no longer recorded on the next render, the same liveness rule an applied
  effect follows

#### Scenario: A keyword-targeted conditional effect is not recorded
- **WHEN** a present ability whose Origin is not Detachment Rule carries a conditional Statline
  effect and its catalogue record's classified target is a set of keywords
- **THEN** no not-applied effect is recorded for it, the same outcome as for an unconditional effect
  with that target

#### Scenario: An activated Statline effect is applied, not recorded
- **WHEN** the player activates Martial Honour on Helbrecht's Crusader Squad
- **THEN** the bearer's OC is improved by 5 and no not-applied effect for Martial Honour is recorded
