## MODIFIED Requirements

### Requirement: Multiple Named Statlines
A Datasheet SHALL support one or more named statlines, each independently specifying Movement,
Toughness, Save, Wounds, Leadership, and Objective Control. Statlines SHALL be exposed in the
order they were supplied to the Datasheet at construction — that declared order is a load-bearing
property of the Datasheet, not an incidental collection-enumeration detail, and downstream
consumers (e.g. the Attached Unit aggregate statline view) rely on it directly rather than
deriving a display order some other way.

#### Scenario: Datasheet with a single shared statline
- **WHEN** a Datasheet like "Assault Intercessor Squad" has one statline shared by all its models
- **THEN** the Datasheet exposes exactly one named statline entry, referenced by every model-line

#### Scenario: Datasheet with multiple distinct statlines
- **WHEN** a Datasheet like a Chaos Space Marine unit defines five distinct model types, each with its own statline
- **THEN** the Datasheet exposes five named statline entries, each independently queryable by name

#### Scenario: Statline and weapon eligibility vary independently
- **WHEN** a Datasheet's Sergeant-equivalent model shares its squad's statline but has different weapon options than the rest of the squad
- **THEN** the Datasheet allows the same named statline to be referenced by model-lines with different weapon eligibility, without requiring the weapon set to be identical

#### Scenario: Declared order is preserved on enumeration
- **WHEN** a Datasheet is constructed with statlines supplied in a specific order (e.g. the
  Sergeant-equivalent entry first, followed by the rank-and-file entry)
- **THEN** enumerating the Datasheet's statlines yields them in that same order, regardless of the
  names involved
