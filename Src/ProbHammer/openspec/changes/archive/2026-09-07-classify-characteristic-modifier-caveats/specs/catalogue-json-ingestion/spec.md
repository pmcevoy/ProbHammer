## ADDED Requirements

### Requirement: Closed-World Characteristic-Modifier Classification (Tiers 1-2 Only)
The system SHALL classify a resolved entry's own `BsModifier` data as a characteristic-modifier
candidate only when it meets one of two conditions: (tier 1) the modifier carries no
`Conditions`/`ConditionGroups` at all, or (tier 2) every condition it carries refers only to the
modifier's own granting entry's state (e.g. a quantity threshold within that same selection). A
modifier whose condition references a different selection, a different unit, live attachment state,
or any condition shape the classifier does not specifically recognize SHALL be left unclassified —
the system SHALL NOT default to treating an unrecognized condition as satisfied or as absent.

#### Scenario: A modifier with no condition is classified
- **WHEN** a selection entry carries a `BsModifier` with no `Conditions` or `ConditionGroups`,
  targeting a known characteristic field
- **THEN** the system classifies it as a candidate targeting that characteristic

#### Scenario: A modifier gated on a sibling selection is left unclassified
- **WHEN** a `BsModifier`'s own condition references a different selection entry's id, not its own
  granting entry
- **THEN** the system does not classify that modifier as a candidate

#### Scenario: A modifier gated on an unrecognized condition shape is left unclassified
- **WHEN** a `BsModifier`'s own condition tree uses a field or structure the classifier does not
  recognize
- **THEN** the system does not classify that modifier as a candidate, and does not raise an error

#### Scenario: An unrecognized characteristic field is left unclassified
- **WHEN** a `BsModifier`'s own `Field` does not correspond to a known Statline or WeaponProfile
  characteristic
- **THEN** the system does not classify that modifier as a candidate

## MODIFIED Requirements

### Requirement: No Composition, Validation, or Points Data
The system SHALL NOT interpret BSData's composition/validation apparatus (`constraints`,
`conditionGroups`, or `associations`/eligibility links) for legality-checking purposes, and SHALL
NOT extract points costs, consistent with `datasheet-catalogue`'s existing "No Wargear Constraint or
Points Modeling" requirement — this system resolves names to rules data; it does not validate that a
chosen roster is legal. A profile/entry's own `modifiers` (`BsModifier` data) ARE read, but only for
two narrow, closed purposes: hidden-gating (excluding an entry/group/rule outside a specific game
mode or chapter/sub-faction) and classifying whether a modifier deterministically targets a
resolvable characteristic (see `characteristic-modifier-caveats`). Neither purpose writes a
modifier's own value onto the produced `Datasheet`, `Statline`, or `WeaponProfile` — classification
only ever produces an on-demand candidate description (see `datasheet-catalogue`'s own requirement),
never a mutated catalogue value.

#### Scenario: Composition apparatus is ignored
- **WHEN** resolving a unit entry that carries `constraints` or an `associations` block describing
  which units it may lead
- **THEN** none of that data appears on the produced `Datasheet`, `Statline`, `WeaponProfile`, or
  `Ability` objects, and no legality check is performed against it

#### Scenario: Points cost is ignored
- **WHEN** resolving a unit entry whose `costs` array includes an entry named `"pts"`
- **THEN** the produced `Datasheet` exposes no points value

#### Scenario: A modifier's own value is never written onto catalogue data
- **WHEN** resolving an entry whose `modifiers` array includes a `BsModifier` with a `Value`
- **THEN** neither hidden-gating nor characteristic-modifier classification writes that `Value` onto
  the produced `Datasheet`'s own `Statline` or `WeaponProfile` fields
