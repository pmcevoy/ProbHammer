## ADDED Requirements

### Requirement: Datasheet Keyword Extraction
The system SHALL extract a Datasheet's top-level Keywords from the resolved entry's own
`categoryLinks`, treating each link's `name` as one Keyword with a literal `"Faction: "` prefix
stripped when present, and no other transformation or exclusion. A `categoryLinks` entry whose
name matches the entry's own name SHALL be kept as a Keyword like any other, not excluded.

#### Scenario: A bare category becomes a Keyword unchanged
- **WHEN** a resolved entry's `categoryLinks` includes an entry named `"Character"`
- **THEN** the Datasheet's Keywords includes `"Character"`

#### Scenario: A Faction-prefixed category has its prefix stripped
- **WHEN** a resolved entry's `categoryLinks` includes an entry named `"Faction: Black Templars"`
- **THEN** the Datasheet's Keywords includes `"Black Templars"`, not `"Faction: Black Templars"`

#### Scenario: A datasheet's own name is included as a Keyword
- **WHEN** a resolved entry's `categoryLinks` includes an entry whose name equals the entry's own
  name (e.g. "Emperor's Champion" appearing among its own datasheet's categories)
- **THEN** the Datasheet's Keywords includes that name

### Requirement: Per-Model Keyword Extraction
For a squad unit whose statlines are extracted from distinct child model entries (per Statline
Extraction), the system SHALL additionally extract each such child entry's own `categoryLinks`
into that model's own Keywords, exposed via `datasheet-catalogue`'s Per-Model Keyword Lookup,
using the same extraction rule as Datasheet Keyword Extraction. A per-model Keyword SHALL NOT be
added to the Datasheet's own top-level Keywords, and a Datasheet's top-level Keywords SHALL NOT be
added to any per-model Keyword set.

#### Scenario: A named individual's own keyword is captured
- **WHEN** one child model entry within a squad (e.g. "Garlon Souleater") has its own
  `categoryLinks` including an entry named `"Psyker"`, while the unit's own top-level
  `categoryLinks` does not
- **THEN** that model's own Keywords includes `"Psyker"`, and the Datasheet's top-level Keywords
  does not

#### Scenario: A model with no distinguishing categoryLinks has no extra keywords
- **WHEN** a child model entry within the same squad has no `categoryLinks` of its own beyond
  those inherited structurally from the unit
- **THEN** that model's own Keywords set is empty
