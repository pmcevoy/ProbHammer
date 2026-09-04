## MODIFIED Requirements

### Requirement: Invulnerable Save Value Shape
A Statline's invulnerable save SHALL be represented as a characteristic view wrapping an original
melee/ranged value: the underlying value itself SHALL carry only a melee-attack value and a
ranged-attack value, with no caveat concept of its own; whether the save is caveated, and which
ability (if any) is responsible for the displayed value not simply being that original value,
SHALL be properties of the wrapping view, not the value.

#### Scenario: No invulnerable save
- **WHEN** a Statline has no invulnerable save
- **THEN** the view's original melee and ranged values are both 0, the view has no contributing
  abilities, and the view is not caveated

#### Scenario: Uniform invulnerable save
- **WHEN** a Statline has a single, unconditional invulnerable save value
- **THEN** the view's original melee and ranged values are both equal to that value, the view has
  no contributing abilities, and the view is not caveated

### Requirement: Caveated Values Always Carry Their Source Ability
Whenever a Statline's invulnerable save view is caveated, its contributing abilities SHALL include
the specific `Ability` whose text a future interpretation step would need to read to fully resolve
the value; a caveated view with no contributing ability SHALL NOT occur.

#### Scenario: Caveated value has an ability reference
- **WHEN** a Statline's invulnerable save view is caveated
- **THEN** its contributing abilities include a non-null reference to the `Ability` associated with
  the caveat
