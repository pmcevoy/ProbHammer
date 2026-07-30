## ADDED Requirements

### Requirement: Fixed and Randomized Quantity Representation
A DiceExpression SHALL represent either a fixed integer quantity or a randomized dice roll (a
count of same-sided dice plus a flat modifier), parseable from standard 40K notation ("3", "D6",
"2D3+1").

#### Scenario: Parsing a fixed integer
- **WHEN** `DiceExpression.Parse("3")` is called
- **THEN** the result represents a fixed value of 3 with no dice component

#### Scenario: Parsing a single die
- **WHEN** `DiceExpression.Parse("D6")` is called
- **THEN** the result represents one six-sided die with no modifier

#### Scenario: Parsing multiple dice with a modifier
- **WHEN** `DiceExpression.Parse("2D3+1")` is called
- **THEN** the result represents two three-sided dice with a modifier of 1

#### Scenario: Parsing invalid notation
- **WHEN** `DiceExpression.Parse` is called with text that matches neither a fixed integer nor
  dice notation
- **THEN** parsing fails rather than silently producing an incorrect value

### Requirement: Ergonomic Fixed-Value Construction
A DiceExpression SHALL be constructible from a plain integer without explicit wrapping, so callers
describing a fixed weapon statistic are not required to reference DiceExpression's factory methods
directly at every construction site.

#### Scenario: Implicit conversion from int
- **WHEN** a plain `int` value is supplied where a `DiceExpression` is expected (e.g. a weapon's
  Attacks or Damage characteristic)
- **THEN** it is implicitly converted to a DiceExpression representing that fixed value,
  equivalent to calling the existing fixed-value factory

### Requirement: Common Die Presets
DiceExpression SHALL expose ready-made values for the most common single dice (D3, D6) so callers
do not need to hand-construct them for the common case.

#### Scenario: Using the D6 preset
- **WHEN** code references the D6 preset
- **THEN** it yields a DiceExpression equivalent to one rolled six-sided die with no modifier

#### Scenario: Using the D3 preset
- **WHEN** code references the D3 preset
- **THEN** it yields a DiceExpression equivalent to one rolled three-sided die with no modifier

### Requirement: Modifier Composition
A DiceExpression SHALL support combining with a flat integer modifier to produce a new expression,
without mutating the original.

#### Scenario: Adding a modifier to a die
- **WHEN** the D6 preset is combined with a modifier of 2
- **THEN** the result is a DiceExpression equivalent to parsing "D6+2"
- **AND** the original D6 preset value is unchanged

### Requirement: Attack and Damage Aggregation Across Models
A DiceExpression SHALL support scaling by an integer count and adding to another DiceExpression,
so that identical weapon profiles carried by multiple models can be aggregated into a single
correctly-distributed expression rather than resolved by rolling once and multiplying.

#### Scenario: Scaling a fixed expression
- **WHEN** a fixed value of 2 is scaled by 3
- **THEN** the result is a fixed value of 6

#### Scenario: Scaling a dice expression preserves distribution
- **WHEN** a single D6 expression is scaled by 3 (representing 3 models each firing D6 attacks)
- **THEN** the result represents 3D6, not a single D6 roll multiplied by 3

#### Scenario: Adding two fixed expressions
- **WHEN** two fixed-value expressions are added
- **THEN** the result is a fixed expression whose value is their sum

#### Scenario: Adding a fixed expression to a dice expression
- **WHEN** a fixed-value expression is added to a dice expression
- **THEN** the result is a dice expression with the fixed value folded into its modifier

#### Scenario: Adding two dice expressions with matching sides
- **WHEN** two dice expressions with the same number of sides are added
- **THEN** the result combines their dice counts and modifiers into a single expression

#### Scenario: Adding two dice expressions with mismatched sides
- **WHEN** two dice expressions with different numbers of sides are added
- **THEN** the operation fails rather than silently producing an incorrect combined expression
