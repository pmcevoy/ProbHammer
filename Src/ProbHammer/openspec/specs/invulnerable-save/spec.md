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

### Requirement: Footnoted Caveat Text Resolution
When a Statline's invulnerable save is caveated by a footnote on its raw source value (a single
value marked as footnoted, or a melee/ranged pair where exactly one side is footnoted), the system
SHALL attempt to resolve the caveat before falling back to a caveated result. Resolution SHALL
compare the linked `Ability`'s Text, as an exact whole-string match with no additional leading or
trailing content permitted, against a fixed, closed set of known templates, each naming an
attack-type restriction and a value:
- "This model has a {N}+ invulnerable save against ranged attacks."
- "This model has a {N}+ invulnerable save against melee attacks."
- "Models in this unit have a {N}+ invulnerable save against ranged attacks."
- "Models in this unit have a {N}+ invulnerable save against melee attacks."

A single footnoted value that matches one of these templates SHALL resolve to a non-caveated
result with the matched attack type set to the template's value and the other attack type set to 0.
A melee/ranged pair with exactly one footnoted side that matches one of these templates SHALL
resolve to a non-caveated result with the matched attack type set to the template's value and the
other attack type set to the pair's already-known, non-footnoted value. Any ability text that does
not match one of these templates exactly SHALL leave the result caveated, unchanged from today's
existing fallback behavior — this includes text describing a restriction unrelated to attack type
(e.g. a save that denies re-rolls rather than restricting by attack type).

When a melee/ranged pair's footnoted side is being resolved, the value named in the matched
template SHALL be checked for consistency against the digit already present in the pair's own raw
footnoted value. A mismatch between the two SHALL leave the result caveated rather than trusting
either value over the other.

This resolution behavior SHALL be identical regardless of which import pipeline produced the
Ability being matched.

#### Scenario: A single footnoted value resolves via an exact template match
- **WHEN** a Statline's invulnerable save is a single footnoted value, and its linked Ability's Text
  is exactly "This model has a 4+ invulnerable save against ranged attacks."
- **THEN** the result is non-caveated, with a ranged value of 4 and a melee value of 0

#### Scenario: A single footnoted value with unmatched text stays caveated
- **WHEN** a Statline's invulnerable save is a single footnoted value, and its linked Ability's Text
  does not exactly match any known template (e.g. it describes a restriction on re-rolling save
  rolls, not an attack-type split)
- **THEN** the result remains caveated with both melee and ranged set to the raw footnoted value,
  unchanged from today's existing fallback behavior

#### Scenario: A melee/ranged pair resolves the footnoted side via an exact template match
- **WHEN** a Statline's invulnerable save is a melee/ranged pair with one side footnoted (e.g. a raw
  value of "5+ / 4+*"), and the linked Ability's Text is exactly "Models in this unit have a 4+
  invulnerable save against melee attacks."
- **THEN** the result is non-caveated, with the melee value set to 4 (from the matched template) and
  the ranged value set to 5 (the pair's already-known non-footnoted value)

#### Scenario: A melee/ranged pair with unmatched text stays caveated
- **WHEN** a Statline's invulnerable save is a melee/ranged pair with one side footnoted, and the
  linked Ability's Text does not exactly match any known template
- **THEN** the result remains caveated with both melee and ranged set to the pair's non-footnoted
  value, unchanged from today's existing fallback behavior

#### Scenario: A digit mismatch between the raw text and the matched template stays caveated
- **WHEN** a Statline's invulnerable save is a melee/ranged pair with one side footnoted at one
  digit, and the linked Ability's Text exactly matches a known template but names a different digit
- **THEN** the result remains caveated rather than resolving with either digit

#### Scenario: Extra content in the ability text prevents resolution
- **WHEN** a Statline's invulnerable save is footnoted, and its linked Ability's Text contains a
  known template's wording plus any additional leading or trailing content
- **THEN** the result remains caveated — a match requires the Ability's entire Text to equal a
  known template exactly, not merely contain one as a substring
