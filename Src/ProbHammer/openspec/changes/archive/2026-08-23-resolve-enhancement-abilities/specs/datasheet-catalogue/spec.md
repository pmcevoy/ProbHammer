## MODIFIED Requirements

### Requirement: Datasheet Identity
A Datasheet SHALL represent the reference/rules data for a single named unit type, independent of
any specific army list, including its name, faction keywords, general keywords, and its intrinsic,
unconditional unit-wide abilities — abilities that are simply true about the datasheet, not one of
several optional choices a player may or may not take.

#### Scenario: Basic datasheet fields are present
- **WHEN** a Datasheet is constructed from catalogue data
- **THEN** it exposes a Name, a set of Faction Keywords, a set of Keywords, and a list of its
  intrinsic unit-wide Abilities

#### Scenario: An optional ability grant is excluded from the intrinsic list
- **WHEN** catalogue data for a unit includes an ability nested inside its own optional selection
  entry (e.g. an Enhancement, or a wargear choice that grants an ability rather than a weapon
  profile)
- **THEN** that ability does NOT appear in the Datasheet's intrinsic unit-wide Abilities list

### Requirement: Ability Shape
An Ability SHALL carry a Name, free-text Text, an optional list of structured Choices, a Scope of
either Model or Unit, and an Origin classifying it as Intrinsic (a fact about the datasheet), an
Enhancement (an optional ability found within a selection group named "Enhancements"), or an
Optional Grant (any other optional ability nested inside its own selection entry, e.g. a wargear
choice that grants an ability rather than a weapon profile).

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

## ADDED Requirements

### Requirement: On-Demand Ability Resolution
A Datasheet SHALL resolve an optional ability (an Enhancement, or any other ability nested inside
its own optional selection entry) by name on request, without exposing or requiring enumeration of
every optional ability the Datasheet defines — mirroring the existing On-Demand Weapon Profile
Resolution requirement.

#### Scenario: Resolving a named optional ability
- **WHEN** a caller requests the optional ability named "Shield Dome" from a Datasheet
- **THEN** the Datasheet returns that ability's Name, Text, and Origin, without exposing or
  requiring enumeration of every other optional ability the Datasheet defines

#### Scenario: An unresolvable optional ability name fails without a result
- **WHEN** a caller requests an optional ability name the Datasheet does not define
- **THEN** resolution fails without a result, mirroring the existing weapon-profile resolution
  failure behavior
