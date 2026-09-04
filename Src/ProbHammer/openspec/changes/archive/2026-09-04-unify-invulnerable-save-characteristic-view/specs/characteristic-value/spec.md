## MODIFIED Requirements

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
