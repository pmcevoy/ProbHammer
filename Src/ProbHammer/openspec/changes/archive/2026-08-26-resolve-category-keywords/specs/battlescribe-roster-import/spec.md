## ADDED Requirements

### Requirement: Datasheet Keyword Extraction
For each unit/model selection, the system SHALL extract the synthesized Datasheet's top-level
Keywords from the selection's own `categories`, treating each category's `name` as one Keyword
with a literal `"Faction: "` prefix stripped when present, and no other transformation or
exclusion. A `categories` entry whose name matches the selection's own name SHALL be kept as a
Keyword like any other, not excluded.

#### Scenario: A bare category becomes a Keyword unchanged
- **WHEN** a unit selection's `categories` includes an entry named `"Battleline"`
- **THEN** the synthesized Datasheet's Keywords includes `"Battleline"`

#### Scenario: A Faction-prefixed category has its prefix stripped
- **WHEN** a unit selection's `categories` includes an entry named `"Faction: Black Templars"`
- **THEN** the synthesized Datasheet's Keywords includes `"Black Templars"`, not
  `"Faction: Black Templars"`

### Requirement: Per-Model Keyword Extraction
For a nested `model`-typed selection within a unit (per Statline, Weapon, and Ability Extraction),
the system SHALL extract that selection's own `categories` directly into the corresponding
`ModelLine`'s Keywords, using the same extraction rule as Datasheet Keyword Extraction. A
model-line's own Keywords SHALL NOT be added to the unit's synthesized Datasheet's top-level
Keywords, and the unit's top-level Keywords SHALL NOT be added to any model-line's Keywords.

#### Scenario: A named individual's own keyword is captured
- **WHEN** one nested `model` selection within a unit (e.g. "Garlon Souleater") has its own
  `categories` including an entry named `"Psyker"`, while the unit's own top-level `categories`
  does not
- **THEN** that model line's Keywords includes `"Psyker"`, and the unit's synthesized Datasheet's
  top-level Keywords does not

#### Scenario: A model selection with no categories has no keywords
- **WHEN** a nested `model` selection carries no `categories` of its own
- **THEN** that model line's Keywords set is empty
