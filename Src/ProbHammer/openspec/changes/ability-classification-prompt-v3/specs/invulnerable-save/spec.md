# Spec Delta

## ADDED Requirements

### Requirement: An Attack-Type-Only Footnote Resolves Without A Classification
When a caveated invulnerable save's single contributing ability's whole text, ignoring case,
surrounding whitespace and a trailing full stop, is exactly "Against melee attacks only" or "Against
ranged attacks only", the system SHALL resolve it from that text alone, before any catalogue lookup:
the named side takes the footnoted value, and the other side follows the same bare and split rules
as a catalogue resolution. Any other text SHALL fall through to the catalogue lookup.

#### Scenario: A bare melee-only footnote
- **WHEN** a Statline's invulnerable save is "4+*" and its contributing ability's text is "Against
  melee attacks only"
- **THEN** the invulnerable save is displayed as resolved: 4+ melee and no ranged invulnerable save,
  still referencing that ability as its source, with no catalogue record needed

#### Scenario: A ranged-only footnote with a trailing full stop
- **WHEN** a Statline's invulnerable save is "5+*" and its contributing ability's text is "Against
  ranged attacks only."
- **THEN** the invulnerable save is displayed as resolved: 5+ ranged and no melee invulnerable save

#### Scenario: Footnote text with more words falls through
- **WHEN** a caveated invulnerable save's contributing ability's text is "Against melee attacks only,
  while this model is leading a unit"
- **THEN** the structural rule does not apply, and the value resolves only through the catalogue
  lookup
