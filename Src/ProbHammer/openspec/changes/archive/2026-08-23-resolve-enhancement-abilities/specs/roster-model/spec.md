## MODIFIED Requirements

### Requirement: Unit Composition
A Unit SHALL reference a single Datasheet and carry a list of model-lines, each specifying which named statline it uses, which weapons it is equipped with, how many models share that combination, and its own Keywords (distinct from the Datasheet's Keywords, for keyword variance scoped to specific named individuals or roles within a shared statline), plus the resolved Abilities of any Enhancements applied to the Unit.

#### Scenario: Unit with uniform loadout
- **WHEN** a Unit's export data describes 5 models all equipped identically
- **THEN** the Unit has one model-line with Count 5

#### Scenario: Unit with mixed loadout within one statline
- **WHEN** a Unit contains 2 models equipped with a Power Fist and 3 models equipped with a Master-crafted Power Weapon, all sharing the same statline
- **THEN** the Unit has two separate model-lines, both referencing the same named statline, each with its own weapon selection and count

#### Scenario: Statline reference is always explicit
- **WHEN** a model-line is constructed
- **THEN** its statline name is taken directly from the source data and is never inferred from the weapons or wargear selected

#### Scenario: Named individuals sharing a statline are modeled as separate model-lines when a keyword differs
- **WHEN** several named individuals within a Datasheet share an identical statline but only one of them carries a specific keyword (e.g. one of three same-statlined characters is a Psyker)
- **THEN** each named individual is represented as its own model-line, so that keyword belongs to only that model-line's Keywords, not to the shared statline's other model-lines

#### Scenario: An applied Enhancement is a resolved Ability, not a raw name
- **WHEN** a Unit has an Enhancement applied
- **THEN** the Unit's Enhancements collection contains the resolved Ability (Name, Text, Scope,
  and Origin), not merely the name string that selected it
