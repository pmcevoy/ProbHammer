## MODIFIED Requirements

### Requirement: Attached Unit and Unit Graph Assembly
For each parsed attachment group, the system SHALL build an `AttachedUnit` whose `Bodyguard` is
the resolved `Unit` for the group's Bodyguard-role member and whose `Attached` collection contains
the resolved `Unit`s for the group's Leader/Support-role members, in the order captured during
parsing. Each parsed standalone unit SHALL become a plain `Unit`. Each resolved model sub-group
SHALL become one `ModelLine`, referencing its resolved `Statline`'s name, its resolved weapon
names, its own model count, and its own Keywords resolved from the Datasheet's per-model Keyword
lookup for that Statline's name.

#### Scenario: An attachment group assembles into one AttachedUnit
- **WHEN** a parsed attachment group has one Bodyguard-role member and two Leader/Support-role
  members
- **THEN** the resulting `AttachedUnit`'s `Bodyguard` is the Bodyguard-role member's resolved
  `Unit`, and its `Attached` collection contains the other two, in their captured order

#### Scenario: A standalone unit assembles into a plain Unit
- **WHEN** a parsed unit was recognized as standalone (per Standalone Unit Section Recognition)
- **THEN** it resolves to a plain `Unit`, not wrapped in any `AttachedUnit`

#### Scenario: A split model group produces multiple ModelLines sharing one statline name
- **WHEN** a parsed unit's model group was partitioned into two sub-groups during parsing (e.g.
  Crusader Squad's Initiate 3/2 split)
- **THEN** the resolved `Unit` has two `ModelLine`s, both referencing the same resolved `Statline`
  name, each with its own count and weapon list

#### Scenario: A model-line's Keywords come from its own statline name, not a sibling's
- **WHEN** a resolved `Unit`'s Datasheet has per-model Keywords data for one named statline (e.g.
  "Garlon Souleater" carrying Psyker) but not for another statline name sharing the same squad
- **THEN** only the `ModelLine` referencing "Garlon Souleater" has Psyker in its own Keywords; a
  `ModelLine` referencing a different statline name does not
