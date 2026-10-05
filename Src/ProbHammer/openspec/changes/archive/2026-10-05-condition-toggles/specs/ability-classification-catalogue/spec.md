## MODIFIED Requirements

### Requirement: Unconditional Effect Rule
A classified effect SHALL be unconditional only when its residual-condition bucket is none, it
carries no choice branch, and its record has no usage limit and no turn ownership. Every other
classified effect SHALL be conditional. Every consumer that applies an effect automatically SHALL
apply only unconditional effects and conditional effects the player has activated (per
`condition-activation`); what a consumer does with any other conditional effect is defined by that
consumer.

#### Scenario: A usage-limited effect is conditional even with no residual condition
- **WHEN** a record's effect has residual-condition bucket none, and the record's usage limit is
  "Once per battle"
- **THEN** the effect is conditional

#### Scenario: A turn-restricted effect is conditional
- **WHEN** a record's effect has residual-condition bucket none and no choice branch, and the
  record's turn ownership is "mine"
- **THEN** the effect is conditional

#### Scenario: A choice-branch effect is conditional
- **WHEN** a record's effect belongs to a choice branch
- **THEN** the effect is conditional

#### Scenario: Phases alone do not make an effect conditional
- **WHEN** a record lists one or more phases, its effect has residual-condition bucket none and no
  choice branch, and the record has no usage limit and no turn ownership
- **THEN** the effect is unconditional

#### Scenario: An activated conditional effect is applied
- **WHEN** a record's effect is conditional and the player has activated it on a unit
- **THEN** a consumer that applies effects automatically applies it on that unit
