## MODIFIED Requirements

### Requirement: Unit Composition
A Unit SHALL reference a single Datasheet and carry a list of model-lines, each specifying which named statline it uses, which weapons it is equipped with, how many models share that combination, and its own Keywords (distinct from the Datasheet's Keywords, for keyword variance scoped to specific named individuals or roles within a shared statline), plus any Enhancements applied to the Unit.

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

### Requirement: Attached Unit Keyword Resolution
An AttachedUnit's effective keyword set for unit-level rule checks SHALL be computed as the union of the Keywords of whichever component Units are currently present, together with the Keywords of whichever of those components' model-lines are currently present, and SHALL NOT be persisted as stored data. Model-level rule checks SHALL use only that specific model's own component Unit's Keywords together with that specific model's own model-line's Keywords, never another model-line's Keywords even within the same component Unit, and never the union.

#### Scenario: Union reflects all present components
- **WHEN** an AttachedUnit's Bodyguard has Keywords {INFANTRY, BATTLELINE} and its attached Leader has Keywords {CHARACTER, INFANTRY}
- **THEN** a unit-level check for keyword CHARACTER against the AttachedUnit succeeds

#### Scenario: Union changes when a component is removed
- **WHEN** the Support Unit in an AttachedUnit is destroyed before the Leader and Bodyguard
- **THEN** the AttachedUnit's effective keyword union no longer includes any keyword that only the Support Unit contributed, without any change to the Leader's or Bodyguard's own Keywords

#### Scenario: Model-level checks do not see other components' keywords
- **WHEN** a specific model in the Bodyguard Unit does not have the CHARACTER keyword itself
- **THEN** a model-level check for keyword CHARACTER against that specific model fails, even though the AttachedUnit as a whole has the CHARACTER keyword via its attached Leader

#### Scenario: Unit-level union includes a present model-line's own keyword
- **WHEN** a component's Datasheet Keywords do not include PSYKER, but one of that component's currently-present model-lines has PSYKER in its own Keywords
- **THEN** a unit-level check for keyword PSYKER against the AttachedUnit (or a plain Unit) succeeds

#### Scenario: Model-level checks do not see a sibling model-line's keyword
- **WHEN** two model-lines belong to the same component Unit and only one of them has PSYKER in its own Keywords
- **THEN** a model-level check for keyword PSYKER against a model in the other model-line fails, even though both model-lines belong to the same Unit

#### Scenario: A model-line's keyword drops out once it has no models remaining
- **WHEN** a model-line whose own Keywords include PSYKER has its RemainingCount reduced to 0, and no other present component, model-line, or Datasheet contributes PSYKER
- **THEN** the effective keyword union for that combat unit no longer includes PSYKER
