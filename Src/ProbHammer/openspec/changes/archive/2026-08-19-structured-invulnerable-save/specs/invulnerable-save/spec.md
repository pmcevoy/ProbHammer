## Purpose

Represents a Statline's invulnerable save as a structured melee/ranged value, distinguishing a
fully-understood value from one whose real-data source text needs interpretation this layer
deliberately does not perform.

## ADDED Requirements

### Requirement: Invulnerable Save Value Shape
A Statline's invulnerable save SHALL be represented as a value carrying a melee-attack value, a
ranged-attack value, a caveated flag, and — set only when caveated — a reference to the specific
`Ability` its source text is linked to.

#### Scenario: No invulnerable save
- **WHEN** a Statline has no invulnerable save
- **THEN** its melee and ranged values are both 0, caveated is false, and no ability reference is
  set

#### Scenario: Uniform invulnerable save
- **WHEN** a Statline has a single, unconditional invulnerable save value
- **THEN** its melee and ranged values are both equal to that value, caveated is false, and no
  ability reference is set

### Requirement: Caveated Values Always Carry Their Source Ability
Whenever a Statline's invulnerable save is caveated, it SHALL carry a reference to the specific
`Ability` whose text a future interpretation step would need to read to fully resolve the value; a
caveated value with no ability reference SHALL NOT occur.

#### Scenario: Caveated value has an ability reference
- **WHEN** a Statline's invulnerable save is caveated
- **THEN** it carries a non-null reference to the `Ability` associated with it
