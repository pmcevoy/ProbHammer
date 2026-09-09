# invulnerable-save Specification

## Purpose

Represents a Statline's invulnerable save as a structured melee/ranged value, distinguishing a
fully-understood value from one whose real-data source text needs interpretation this layer
deliberately does not perform.

## Requirements

### Requirement: Invulnerable Save Value Shape
A Statline's invulnerable save SHALL be represented as a characteristic view wrapping an original
melee/ranged value: the underlying value itself SHALL carry only a melee-attack value and a
ranged-attack value, with no caveat concept of its own; whether the save is caveated, and which
ability (if any) is responsible for the displayed value not simply being that original value,
SHALL be properties of the wrapping view, not the value.

#### Scenario: No invulnerable save
- **WHEN** a Statline has no invulnerable save
- **THEN** the view's original melee and ranged values are both 0, the view has no contributing
  abilities, and the view is not caveated

#### Scenario: Uniform invulnerable save
- **WHEN** a Statline has a single, unconditional invulnerable save value
- **THEN** the view's original melee and ranged values are both equal to that value, the view has
  no contributing abilities, and the view is not caveated

### Requirement: Caveated Values Always Carry Their Source Ability
Whenever a Statline's invulnerable save view is caveated, its contributing abilities SHALL include
the specific `Ability` whose text a future interpretation step would need to read to fully resolve
the value; a caveated view with no contributing ability SHALL NOT occur.

#### Scenario: Caveated value has an ability reference
- **WHEN** a Statline's invulnerable save view is caveated
- **THEN** its contributing abilities include a non-null reference to the `Ability` associated with
  the caveat

### Requirement: Parse-Time Resolution Never Classifies A Caveat's Linked Ability
When a Statline's invulnerable save cannot be fully determined from its own raw catalogue
characteristic text alone (a footnote, or a melee/ranged pair with exactly one footnoted side,
naming a linked ability), the parser SHALL produce a caveated result carrying that ability, exactly
as it does today. The parser SHALL NOT itself attempt to interpret the linked ability's Text,
compare it against any known phrasing, or otherwise judge whether the caveat should resolve — that
determination SHALL be made entirely by a later resolution step (see "A Caveated Value Is Given One
Resolution Attempt During Roster Aggregation" below), not by parsing.

#### Scenario: An unresolvable footnote defers rather than attempting its own resolution
- **WHEN** a Statline's invulnerable save text is footnoted, naming a linked ability
- **THEN** the parser produces a caveated result carrying that ability, without evaluating the
  ability's own Text against any template or pattern

#### Scenario: A fully-determinable value never involves an ability at all
- **WHEN** a Statline's invulnerable save text is fully determinable from the raw characteristic
  text alone (a plain value, or a parenthetical melee/ranged restriction)
- **THEN** the parser produces a non-caveated result with no ability involved, exactly as it does
  today — this requirement changes only the footnoted/split, ability-linked case

### Requirement: A Caveated Value Is Given One Resolution Attempt During Roster Aggregation
For every resolved unit's Statline whose invulnerable save is caveated, the system SHALL attempt to
resolve it exactly once per aggregate rebuild by matching its own single contributing ability's
normalized Text against the checked-in `RuleClassificationBaseline`, using the same resolution this
system already applies to an ordinary present ability. A successful match SHALL replace the
caveated result with a resolved value carrying that ability as its source, preserving the true
pre-caveat original value. An unsuccessful match SHALL leave the result caveated, unchanged from
today's existing fallback behavior.

#### Scenario: A caveated value resolves via a baseline match
- **WHEN** a resolved unit's Statline carries a caveated invulnerable save, and its single
  contributing ability's normalized Text matches a baseline entry
- **THEN** the invulnerable save is displayed as resolved, using the matched entry's value, still
  referencing that ability as its source

#### Scenario: A caveated value with no baseline match stays caveated
- **WHEN** a resolved unit's Statline carries a caveated invulnerable save, and its contributing
  ability's normalized Text matches no baseline entry
- **THEN** the invulnerable save remains caveated, showing the Datasheet's own base value, exactly
  as before this resolution attempt was made

#### Scenario: A resolved value is never re-caveated by a later pass
- **WHEN** a resolved unit's Statline invulnerable save has already been resolved by this
  requirement or by any other characteristic-resolution mechanism
- **THEN** no later step in the same aggregate rebuild replaces or downgrades that resolved value
