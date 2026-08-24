## MODIFIED Requirements

### Requirement: Ability Shape
An Ability SHALL carry a Name, free-text Text, an optional list of structured Choices, a Scope of
either Model or Unit, and an Origin classifying it as Intrinsic (a fact about the datasheet), an
Enhancement (an optional ability found within a selection group named "Enhancements"), an Optional
Grant (any other optional ability nested inside its own selection entry, e.g. a wargear choice that
grants an ability rather than a weapon profile), a Core Rule (a datasheet-wide ability whose text is
a reference to a separately-defined Core rule with no chapter/sub-faction exclusivity of its own,
e.g. Deadly Demise or Firing Deck, rather than text carried directly on the datasheet's own ability
profile), or an Army Rule (the same reference shape as a Core Rule, but whose referenced rule
carries its own chapter/sub-faction exclusivity, e.g. Oath of Moment or a Chapter's own Vows — the
distinguishing signal is a structural property of the referenced rule's own gating, not a guess or
a count of how many datasheets happen to reference it).

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

#### Scenario: A datasheet-wide rule reference with no chapter exclusivity is classified as a Core Rule
- **WHEN** an Ability is resolved from a datasheet-wide reference to a separately-defined rule
  whose own gating carries no chapter/sub-faction-scoped condition (e.g. Impulsor's "Deadly Demise
  D3" or "Firing Deck 6" reference)
- **THEN** its Origin is Core Rule, distinct from Intrinsic, Enhancement, Optional Grant, and Army
  Rule, and it carries a Scope of Unit

#### Scenario: A datasheet-wide rule reference with chapter exclusivity is classified as an Army Rule
- **WHEN** an Ability is resolved from a datasheet-wide reference to a separately-defined rule
  whose own gating carries a chapter/sub-faction-scoped condition (e.g. Oath of Moment or Templar
  Vows), regardless of whether that condition currently excludes the reference for the resolved
  army
- **THEN** its Origin is Army Rule, distinct from Core Rule, and it carries a Scope of Unit
