## MODIFIED Requirements

### Requirement: Full-Corpus Characteristic-Resolution Scan
The system SHALL provide a manually-triggered scan that, when the local BSData clone is present,
attempts to resolve every named entry in every real catalogue file in the clone (excluding
`Warhammer 40,000.json`) and records any exception raised during resolution, keyed by a
recognizable pattern rather than by the specific datasheet or entry that raised it.

#### Scenario: A known ambiguous-InSv pattern is recorded
- **WHEN** the scan resolves a datasheet whose `InSv` characteristic is one of the forms that
  previously required a dedicated allowlist entry (e.g. `"4+* / 5+"` on Wyches/Howling Banshees,
  `"5+ (Ranged)"` on a Titan or the Stormbird, or a bare footnoted value like `"5+*"`)
- **THEN** resolution now succeeds for all of them, so no exception is raised and nothing is
  recorded against an InSv-specific allowlist pattern for these forms

#### Scenario: A known dice-notation Strength pattern is recorded
- **WHEN** the scan resolves a weapon profile whose `S` characteristic uses dice notation (e.g.
  `"D6+6"` on the Ork Battlewagon)
- **THEN** the resulting exception is recorded against the corresponding allowlist pattern, not
  treated as an unexpected failure

#### Scenario: `Warhammer 40,000.json` is excluded from the scan
- **WHEN** the scan walks the local clone's catalogue files
- **THEN** `Warhammer 40,000.json` is not attempted, and its known deserialization failure never
  appears in the scan's results
