## MODIFIED Requirements

### Requirement: Full-Corpus Weapon-Keyword Token Scan
The system SHALL provide a manually-triggered scan that, when the local BSData clone is present,
tokenizes the `Keywords` characteristic of every weapon profile in every real catalogue file in
the clone (excluding `Warhammer 40,000.json`) and records every token that has no matching entry
in that clone's own `RuleGlossary` (per `rules-glossary`'s "Glossary Lookup By Normalized Name Or
Alias" requirement) — the check that reflects what a player actually sees on `/LivePlay`, where an
unresolved token still renders but as a non-interactive chip, rather than a check tied to any
fixed, hand-maintained recognized vocabulary.

#### Scenario: An unrecognized token is recorded
- **WHEN** the scan encounters a weapon whose `Keywords` characteristic contains a token with no
  matching `RuleGlossary` entry
- **THEN** that token is recorded against the corresponding allowlist pattern, not treated as an
  unexpected failure

#### Scenario: A token resolved only through a glossary alias produces no unresolved result
- **WHEN** the scan encounters a weapon keyword token that does not match any `RuleGlossary`
  entry's `Name` directly, but does match one of that entry's `Alias` values once normalized
- **THEN** the token is treated as resolved, and is not recorded as an unresolved result
