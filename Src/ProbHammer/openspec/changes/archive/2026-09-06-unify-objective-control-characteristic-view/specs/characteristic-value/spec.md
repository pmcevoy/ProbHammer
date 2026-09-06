## MODIFIED Requirements

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
