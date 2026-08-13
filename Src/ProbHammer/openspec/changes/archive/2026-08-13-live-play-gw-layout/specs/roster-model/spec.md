## ADDED Requirements

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
