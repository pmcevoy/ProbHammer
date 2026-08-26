## ADDED Requirements

### Requirement: Full-Corpus Army-Rule Name Resolution Scan
The system SHALL provide a manually-triggered scan that, when the local BSData clone is present,
resolves every entry in `army-rule-name-lookup`'s curated inclusion table against its own faction's
real catalogue closure in the clone, and records any entry whose name does not resolve as a real
rule.

#### Scenario: A curated entry that still resolves produces no result
- **WHEN** the scan resolves a curated table entry's name against its own faction's real closure in
  the clone, and the name resolves as a real rule
- **THEN** that entry produces no result

#### Scenario: A curated entry that no longer resolves is recorded
- **WHEN** the scan resolves a curated table entry's name against its own faction's real closure in
  the clone, and the name does not resolve (e.g. a mid-edition BSData/codex rename)
- **THEN** that entry is recorded as an unresolved result, subject to the existing Bidirectional
  Allowlist Enforcement requirement

### Requirement: Full-Corpus Primary-Catalogue-Gated Rule Triage Scan
The system SHALL provide a manually-triggered scan that, when the local BSData clone is present,
collects every rule definition anywhere in the clone whose own gating carries a condition scoped to
`primary-catalogue`, and records any such rule whose Name is accounted for by neither
`army-rule-name-lookup`'s curated inclusion table nor its curated exclusion list.

#### Scenario: A gated rule already in the inclusion table produces no result
- **WHEN** the scan finds a `primary-catalogue`-gated rule definition whose Name matches an entry in
  the curated inclusion table
- **THEN** that rule produces no result

#### Scenario: A gated rule already in the exclusion list produces no result
- **WHEN** the scan finds a `primary-catalogue`-gated rule definition whose Name matches an entry in
  the curated exclusion list
- **THEN** that rule produces no result

#### Scenario: A newly-discovered gated rule is recorded as untriaged
- **WHEN** the scan finds a `primary-catalogue`-gated rule definition whose Name matches neither the
  curated inclusion table nor the curated exclusion list (e.g. a rule newly added by a BSData/codex
  update since the lists were last reviewed)
- **THEN** that rule is recorded as an untriaged result, subject to the existing Bidirectional
  Allowlist Enforcement requirement — forcing an explicit human decision (add to the inclusion
  table, or add to the exclusion list with its reason) before the scan passes again

#### Scenario: A rule defined in its own dedicated, ungated file is not discoverable by this scan
- **WHEN** a rule definition (e.g. a faction's own army-wide rule, defined only in that faction's
  own dedicated catalogue file with no `primary-catalogue`-scoped gating of its own, since it has no
  sibling rule to distinguish itself from)
- **THEN** that rule is never found by this scan, regardless of whether it is or is not yet present
  in the curated inclusion table — this scan's coverage is limited to rules using the shared-library
  gating shape, by design

### Requirement: Full-Corpus Faction Coverage Scan
The system SHALL provide a manually-triggered scan that, when the local BSData clone is present,
enumerates every real playable-faction catalogue file in the clone and records any faction that has
no entry at all — populated or deliberately empty — in `army-rule-name-lookup`'s curated inclusion
table.

#### Scenario: A faction with any table entry produces no result
- **WHEN** the scan derives a real catalogue file's own most-specific faction name, and that faction
  has an entry in the curated inclusion table (a populated name list, or a deliberately empty one)
- **THEN** that faction produces no result

#### Scenario: A faction with no table entry at all is recorded
- **WHEN** the scan derives a real catalogue file's own most-specific faction name, and that faction
  has no entry in the curated inclusion table
- **THEN** that faction is recorded as an uncovered result, failing the scan until an explicit
  decision (a verified name, or a deliberately empty entry) is added
