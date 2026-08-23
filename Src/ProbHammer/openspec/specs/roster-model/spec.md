# roster-model

## Purpose

Represents army-list composition and live game state (the Roster context) for 40K units: Unit
composition from model-lines, and the Attached Unit composite (Bodyguard + open collection of
attached Units) with its keyword-resolution and toughness-resolution rules. TBD: expand as this
capability grows beyond its initial domain-model scope.

## Requirements

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

### Requirement: Toughness Resolution Against an Attached Unit
When an attack targets an AttachedUnit, the Toughness characteristic used to resolve that attack SHALL be the highest Toughness among the Bodyguard's currently-present models if any remain, otherwise the highest Toughness among the currently-present Leader/Support models.

#### Scenario: Bodyguard models remain
- **WHEN** an AttachedUnit still has one or more Bodyguard models present, regardless of the Toughness of any attached Leader/Support model
- **THEN** the Toughness used to resolve an attack against that AttachedUnit is the highest Toughness among the Bodyguard's present models

#### Scenario: Bodyguard has been destroyed
- **WHEN** all Bodyguard models in an AttachedUnit have been destroyed but one or more Leader/Support models remain
- **THEN** the Toughness used to resolve an attack against that AttachedUnit is the highest Toughness among the remaining Leader/Support models

### Requirement: ICombatUnit Name Resolution
Every `ICombatUnit` SHALL expose a computed `Name`, derived from the Datasheet(s) of its
currently-configured components and never persisted as stored state. A `Unit`'s `Name` SHALL equal
its Datasheet's name. An `AttachedUnit`'s `Name` SHALL be composed from its Bodyguard's Datasheet
name and the Datasheet names of its Attached units, joined as a humanized list: no attached units
yields the Bodyguard's name alone; one attached unit joins with `"with {A}"`; two attached units
join with `"with {A} and {B}"` (no comma); three or more join with an Oxford comma
(`"with {A}, {B}, and {C}"`).

#### Scenario: Unit name comes from its Datasheet
- **WHEN** a `Unit`'s Datasheet has name "Crusader Squad"
- **THEN** the Unit's `Name` is "Crusader Squad"

#### Scenario: AttachedUnit with no attached units
- **WHEN** an `AttachedUnit`'s Bodyguard Datasheet is named "Crusader Squad" and its Attached
  collection is empty
- **THEN** the AttachedUnit's `Name` is "Crusader Squad", with no "with" clause

#### Scenario: AttachedUnit with one attached unit
- **WHEN** an `AttachedUnit`'s Bodyguard Datasheet is named "Crusader Squad" and it has one
  Attached unit whose Datasheet is named "Marshal"
- **THEN** the AttachedUnit's `Name` is "Crusader Squad with Marshal"

#### Scenario: AttachedUnit with two attached units
- **WHEN** an `AttachedUnit`'s Bodyguard Datasheet is named "Crusader Squad" and it has two
  Attached units whose Datasheets are named "Marshal" and "Lieutenant"
- **THEN** the AttachedUnit's `Name` is "Crusader Squad with Marshal and Lieutenant", with no comma
  before "and"

#### Scenario: AttachedUnit with three or more attached units
- **WHEN** an `AttachedUnit`'s Bodyguard Datasheet is named "Crusader Squad" and it has three
  Attached units whose Datasheets are named "Marshal", "Lieutenant", and "Ancient"
- **THEN** the AttachedUnit's `Name` is "Crusader Squad with Marshal, Lieutenant, and Ancient",
  using an Oxford comma before the final "and"

#### Scenario: Name is recomputed, not cached, across calls
- **WHEN** `Name` is read from the same `ICombatUnit` instance more than once without any change to
  its components' Datasheets
- **THEN** every read returns an identical value, derived fresh each time rather than from a stored
  field

#### Scenario: Duplicate attached-leader names are not deduplicated
- **WHEN** an `AttachedUnit` has two Attached units whose Datasheets share the exact same name
  (e.g. two units both named "Marshal")
- **THEN** the composed `Name` lists that name twice (e.g. "...with Marshal and Marshal") rather
  than collapsing it to a single qualified count — this is a known, accepted limitation

### Requirement: Army Roster Container
An ArmyRoster SHALL wrap an army list's per-unit roster together with army-level metadata that no individual Unit or AttachedUnit carries: the list's Name, PointsSpent, Faction (one or more ordered names), Detachments (one or more ordered names), ForceDisposition, BattleSize, and PointsLimit.

#### Scenario: Units collection is unchanged from the existing roster
- **WHEN** an ArmyRoster is built from an existing per-unit roster
- **THEN** its Units collection contains exactly those ICombatUnit instances, unmodified

#### Scenario: Single-faction army
- **WHEN** an ArmyRoster is built for an army with no sub-faction (e.g. a Chaos Space Marines list)
- **THEN** its Faction contains exactly one name

#### Scenario: Sub-faction army
- **WHEN** an ArmyRoster is built for an army whose faction is a sub-faction of a parent codex (e.g. Black Templars under Space Marines)
- **THEN** its Faction contains the parent codex name followed by the sub-faction name, in that order

#### Scenario: Multiple detachments
- **WHEN** an ArmyRoster is built for an army that selected more than one detachment
- **THEN** its Detachments contains one entry per selected detachment, in selection order

#### Scenario: Points spent is independent of the unit roster
- **WHEN** an ArmyRoster's Units collection is read
- **THEN** PointsSpent is not derived from or validated against the points of any Unit or AttachedUnit in Units, since no points value is tracked anywhere below ArmyRoster
