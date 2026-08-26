## MODIFIED Requirements

### Requirement: Core Rule Extraction
For each unit/model selection, the system SHALL extract every entry in that selection's own `rules`
list as an ability attached to the resulting Unit, resolving each rule's full text from a
roster-scoped index built by collecting every distinct rule entry found anywhere in the roster.
Because the roster contains only rules that actually apply to the exported army, the system SHALL
NOT apply any separate chapter, sub-faction, or game-mode gating when extracting these. The
extracted ability's Origin SHALL be Army Rule when its own Name matches one of the known army-wide
rule names resolved for the roster's own Faction (per `army-rule-name-lookup`'s Faction-Scoped
Resolution, using the Faction the mapper derives from the roster's own `catalogueName`), and Core
Rule otherwise.

#### Scenario: A non-army-wide rule resolves with its full text as Core Rule
- **WHEN** a unit selection's `rules` list includes an entry named "Templar Vows" with its own
  description text, and its Name is not one of the known army-wide rule names resolved for the
  roster's own Faction
- **THEN** the resulting Unit carries a Core-Rule-origin ability named "Templar Vows" with that
  text, with no additional gating check performed

#### Scenario: A rule matching the curated lookup resolves with Army Rule Origin
- **WHEN** a unit selection's `rules` list includes an entry whose Name matches one of the known
  army-wide rule names resolved for the roster's own Faction (e.g. "Nurgle's Gift" for a Death
  Guard roster)
- **THEN** the resulting Unit carries an Army-Rule-origin ability with that Name and its own text
