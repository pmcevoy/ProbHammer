## MODIFIED Requirements

### Requirement: Ability Shape
An Ability SHALL carry a Name, free-text Text, an optional list of structured Choices, a Scope of
either Model or Unit, and an Origin classifying it as Intrinsic (a fact about the datasheet), an
Enhancement (an optional ability found within a selection group named "Enhancements"), an Optional
Grant (any other optional ability nested inside its own selection entry, e.g. a wargear choice that
grants an ability rather than a weapon profile), or a Core Rule (a datasheet-wide ability whose text
is a reference to a separately-defined Core or faction rule, e.g. Oath of Moment or a Chapter's own
Vows, rather than text carried directly on the datasheet's own ability profile).

#### Scenario: Simple ability with no choices
- **WHEN** an Ability has only descriptive Text and no sub-choices
- **THEN** its Choices collection is empty and its Text is preserved as-is

#### Scenario: Ability with structured choices and retained intro text
- **WHEN** an Ability's source data describes conditional instructions followed by a "choose one of the following" list (e.g. "While on an objective marker, choose one of the following:")
- **THEN** the Ability exposes that instructional prose as Text AND exposes each option as a structured entry in Choices, rather than flattening both into one string

#### Scenario: Ability scope is explicit
- **WHEN** an Ability is defined as affecting only its bearer model
- **THEN** its Scope is Model
- **WHEN** an Ability is defined as affecting every model in its unit (and, if attached, the whole attached unit)
- **THEN** its Scope is Unit

#### Scenario: An intrinsic ability carries the Intrinsic origin
- **WHEN** an Ability is resolved from a profile sitting directly on the entry that owns a
  Datasheet's resolved Statline
- **THEN** its Origin is Intrinsic

#### Scenario: An Enhancement is classified as such
- **WHEN** an Ability is resolved from a profile nested inside a selection group named
  "Enhancements"
- **THEN** its Origin is Enhancement

#### Scenario: A non-Enhancement optional ability grant is classified distinctly
- **WHEN** an Ability is resolved from a profile nested inside an optional selection entry that is
  not part of a group named "Enhancements" (e.g. Impulsor's "Shield Dome")
- **THEN** its Origin is Optional Grant, distinct from Enhancement

#### Scenario: A datasheet-wide rule reference is classified as a Core Rule
- **WHEN** an Ability is resolved from a datasheet-wide reference to a separately-defined Core or
  faction rule (e.g. Impulsor's "Oath of Moment" or "Deadly Demise D3" reference, or Black
  Templars' "Templar Vows" reference), rather than from a profile carrying its own text directly
- **THEN** its Origin is Core Rule, distinct from Intrinsic, Enhancement, and Optional Grant, and
  it carries a Scope of Unit
