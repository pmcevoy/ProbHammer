## ADDED Requirements

### Requirement: Unit Composition
A Unit SHALL reference a single Datasheet and carry a list of model-lines, each specifying which named statline it uses, which weapons it is equipped with, and how many models share that combination, plus any Enhancements applied to the Unit.

#### Scenario: Unit with uniform loadout
- **WHEN** a Unit's export data describes 5 models all equipped identically
- **THEN** the Unit has one model-line with Count 5

#### Scenario: Unit with mixed loadout within one statline
- **WHEN** a Unit contains 2 models equipped with a Power Fist and 3 models equipped with a Master-crafted Power Weapon, all sharing the same statline
- **THEN** the Unit has two separate model-lines, both referencing the same named statline, each with its own weapon selection and count

#### Scenario: Statline reference is always explicit
- **WHEN** a model-line is constructed
- **THEN** its statline name is taken directly from the source data and is never inferred from the weapons or wargear selected

### Requirement: Attached Unit Composite
An AttachedUnit SHALL consist of exactly one Bodyguard Unit and an open collection of zero or more attached Units, and SHALL be treated as a single unit for the purposes of aggregate views and keyword resolution.

#### Scenario: Default one Leader, one Support
- **WHEN** a Bodyguard unit has one Leader unit and one Support unit attached
- **THEN** the AttachedUnit's Attached collection contains exactly those two Units

#### Scenario: Exception with more than one Leader
- **WHEN** a Bodyguard unit has two Leader units and one Support unit attached (a documented exception to the usual one-Leader/one-Support default)
- **THEN** the AttachedUnit's Attached collection contains all three Units without the model rejecting or restructuring the extra Leader

#### Scenario: Attachment source is not tracked
- **WHEN** a Unit is added to an AttachedUnit's Attached collection
- **THEN** the model does not record whether it was attached via the Leader ability or the Support ability

### Requirement: Attached Unit Keyword Resolution
An AttachedUnit's effective keyword set for unit-level rule checks SHALL be computed as the union of the Keywords of whichever component Units are currently present, and SHALL NOT be persisted as stored data. Model-level rule checks SHALL use only that specific model's own component Unit's Keywords, never the union.

#### Scenario: Union reflects all present components
- **WHEN** an AttachedUnit's Bodyguard has Keywords {INFANTRY, BATTLELINE} and its attached Leader has Keywords {CHARACTER, INFANTRY}
- **THEN** a unit-level check for keyword CHARACTER against the AttachedUnit succeeds

#### Scenario: Union changes when a component is removed
- **WHEN** the Support Unit in an AttachedUnit is destroyed before the Leader and Bodyguard
- **THEN** the AttachedUnit's effective keyword union no longer includes any keyword that only the Support Unit contributed, without any change to the Leader's or Bodyguard's own Keywords

#### Scenario: Model-level checks do not see other components' keywords
- **WHEN** a specific model in the Bodyguard Unit does not have the CHARACTER keyword itself
- **THEN** a model-level check for keyword CHARACTER against that specific model fails, even though the AttachedUnit as a whole has the CHARACTER keyword via its attached Leader

### Requirement: Toughness Resolution Against an Attached Unit
When an attack targets an AttachedUnit, the Toughness characteristic used to resolve that attack SHALL be the highest Toughness among the Bodyguard's currently-present models if any remain, otherwise the highest Toughness among the currently-present Leader/Support models.

#### Scenario: Bodyguard models remain
- **WHEN** an AttachedUnit still has one or more Bodyguard models present, regardless of the Toughness of any attached Leader/Support model
- **THEN** the Toughness used to resolve an attack against that AttachedUnit is the highest Toughness among the Bodyguard's present models

#### Scenario: Bodyguard has been destroyed
- **WHEN** all Bodyguard models in an AttachedUnit have been destroyed but one or more Leader/Support models remain
- **THEN** the Toughness used to resolve an attack against that AttachedUnit is the highest Toughness among the remaining Leader/Support models
