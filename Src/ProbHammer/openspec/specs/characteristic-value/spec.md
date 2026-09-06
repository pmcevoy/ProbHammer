# Characteristic Value Specification

## Purpose

Defines the shared representation for a characteristic's underlying value (numeric, dice, or
symbolic) and the wrapper capturing a characteristic's original catalogue value, the abilities
classified as contributing to it, and its caveated/derived state — the shape a future
modification/legality engine will populate, not the engine itself.

## Requirements

### Requirement: Characteristic Value Representation
The system SHALL represent a characteristic's raw value as exactly one of three kinds — numeric,
dice, or symbolic — via a closed `CharacteristicValue` hierarchy, so a value can never carry more
than one kind at once or none at all.

#### Scenario: Numeric characteristic value
- **WHEN** a characteristic's catalogue value is a plain integer (e.g. a Toughness of 4)
- **THEN** it SHALL be represented as a numeric `CharacteristicValue`

#### Scenario: Dice-notation characteristic value
- **WHEN** a characteristic's catalogue value is dice notation (e.g. a Strength of "D6+6")
- **THEN** it SHALL be represented as a dice `CharacteristicValue` wrapping a `DiceExpression`

#### Scenario: Symbolic characteristic value
- **WHEN** a characteristic's catalogue value is non-numeric (e.g. "-", "*", "N/A")
- **THEN** it SHALL be represented as a symbolic `CharacteristicValue`, distinct from both the
  numeric and dice kinds

### Requirement: Characteristic Modification View
The system SHALL provide a view type that, for any characteristic property, captures its original
catalogue value, the abilities classified as contributing to its current value, a derived value
when computable, and whether it is caveated.

#### Scenario: Fully-understood contributors produce a derived value
- **WHEN** every ability contributing to a characteristic is classified as deterministic
- **THEN** the view SHALL expose a non-null derived value
- **AND** the view SHALL NOT be marked caveated

#### Scenario: Any unresolvable contributor caveats the whole value
- **WHEN** at least one ability contributing to a characteristic is not classified as
  deterministic
- **THEN** the view SHALL expose no derived value
- **AND** the view SHALL be marked caveated, with no partial credit given to whichever
  contributors were understood

#### Scenario: No contributing abilities
- **WHEN** a characteristic has no contributing abilities at all
- **THEN** the view SHALL expose its original catalogue value as the derived value
- **AND** the view SHALL NOT be marked caveated

#### Scenario: Objective Control formalized as a plain scalar view
- **WHEN** an Objective Control characteristic is represented via the characteristic modification
  view
- **THEN** it SHALL use a bare scalar characteristic value as its value type, with no compound
  per-kind shape — proving the view covers a plain scalar characteristic exactly as readily as the
  compound invulnerable-save case, with no dedicated case needed for either

### Requirement: Compound Characteristic Value Shapes
The system SHALL allow a characteristic's view to wrap a compound, per-kind shape rather than a
bare scalar, for a characteristic whose real value is not a single number — recognizing the
existing invulnerable-save melee/ranged split as the first confirmed instance of this pattern. A
compound view's contributing abilities SHALL accept an ability from either of two independent
sources — a catalogue-resolution-time fact about the source data itself, or a live, roster-state-
dependent rule match — without requiring a dedicated case for either.

#### Scenario: Invulnerable save formalized as a compound view
- **WHEN** an invulnerable-save characteristic is represented via the characteristic modification
  view
- **THEN** it SHALL use the existing melee/ranged invulnerable-save shape as its value type,
  rather than requiring a bare scalar

#### Scenario: A parse-time source and a live rule match use the same contributing-ability shape
- **WHEN** an invulnerable-save view's contributing ability comes from a catalogue-resolution-time
  footnote reference in one case, and from a live, currently-present ability match in another
- **THEN** both SHALL be represented as an entry in the view's contributing abilities, with no
  distinguishing shape between the two sources
