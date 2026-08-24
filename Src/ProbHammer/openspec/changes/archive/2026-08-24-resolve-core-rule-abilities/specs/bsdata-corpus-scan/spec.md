## ADDED Requirements

### Requirement: Full-Corpus Rule-Reference Type Scan
The system SHALL provide a manually-triggered scan that, when the local BSData clone is present,
collects the distinct type value of every datasheet-wide rule reference found anywhere in every
real catalogue file in the clone (excluding `Warhammer 40,000.json`), and records any type value
not already accounted for as a recognized, handled shape.

#### Scenario: A recognized reference type produces no unrecognized result
- **WHEN** the scan encounters a datasheet-wide rule reference whose type is one of the shapes this
  system already extracts an Ability from
- **THEN** that occurrence produces no unrecognized result

#### Scenario: An unrecognized reference type is recorded
- **WHEN** the scan encounters a datasheet-wide rule reference whose type is not one of the shapes
  this system extracts an Ability from
- **THEN** that type value is recorded as an unrecognized result, subject to the existing
  Bidirectional Allowlist Enforcement requirement

#### Scenario: The scan is exhaustive across the whole reachable descendant tree
- **WHEN** the scan walks a catalogue file
- **THEN** it inspects every such reference reachable at any depth within a datasheet's descendant
  tree, not only references found directly on a datasheet's own top-level entry
