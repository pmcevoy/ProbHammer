## ADDED Requirements

### Requirement: Known Army Rule Name Resolution
The system SHALL resolve the known army-wide rule name set for a parsed army list once, from its
Faction (per `army-rule-name-lookup`'s Faction-Scoped Resolution), and SHALL supply that same name
set to every Datasheet resolution performed while enriching that army list, so the resulting
Abilities' Origin classification (per `catalogue-json-ingestion`'s Core Versus Army Rule Origin
Classification) reflects the roster's own Faction consistently across every resolved unit.

#### Scenario: The same name set is used for every unit in one roster
- **WHEN** an army list with two or more units is enriched
- **THEN** every unit's Datasheet resolution is supplied the identical known army-wide rule name
  set, resolved once from the army list's own Faction

#### Scenario: A faction with no curated entry enriches with an empty name set
- **WHEN** an army list's Faction has no entry in the curated lookup
- **THEN** every unit's Datasheet resolution is supplied an empty name set, and every Core Rule
  Ability Extraction reference for that roster classifies Core Rule Origin (per
  `catalogue-json-ingestion`'s Core Versus Army Rule Origin Classification, which relies solely on
  this name set — there is no other, structural fallback)
