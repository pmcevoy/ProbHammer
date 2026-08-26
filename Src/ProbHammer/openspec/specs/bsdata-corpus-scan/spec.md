# bsdata-corpus-scan

## Purpose

Provides a manually-triggered, permanent regression scan across the full local BSData 11th
Edition corpus, catching new characteristic-resolution failures and unrecognized weapon-keyword
tokens against a maintained allowlist, so a real-data gap is never silently reintroduced or
silently left unnoticed once fixed.

## Requirements

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

### Requirement: Full-Corpus Weapon-Keyword Token Scan
The system SHALL provide a manually-triggered scan that, when the local BSData clone is present,
tokenizes the `Keywords` characteristic of every weapon profile in every real catalogue file in
the clone (excluding `Warhammer 40,000.json`) and records every token that does not exactly match
an existing `WeaponProfile` ability flag.

#### Scenario: An unrecognized token is recorded
- **WHEN** the scan encounters a weapon whose `Keywords` characteristic contains a token with no
  corresponding `WeaponProfile` flag
- **THEN** that token is recorded against the corresponding allowlist pattern, not treated as an
  unexpected failure

### Requirement: Full-Corpus Rule-Reference Type Scan
The system SHALL provide a manually-triggered scan that, when the local BSData clone is present,
collects the distinct type value of every datasheet-wide rule reference found anywhere in every
real catalogue file in the clone (excluding `Warhammer 40,000.json`), and records any type value
not already accounted for as a recognized, handled shape.

#### Scenario: A recognized reference type produces no unrecognized result
- **WHEN** the scan encounters a datasheet-wide rule reference whose type is one of the shapes this
  system already extracts an Ability from
- **THEN** that occurrence produces no unrecognized result

#### Scenario: An unrecognized reference type is recorded
- **WHEN** the scan encounters a datasheet-wide rule reference whose type is not one of the shapes
  this system extracts an Ability from
- **THEN** that type value is recorded as an unrecognized result, subject to the existing
  Bidirectional Allowlist Enforcement requirement

#### Scenario: The scan is exhaustive across the whole reachable descendant tree
- **WHEN** the scan walks a catalogue file
- **THEN** it inspects every such reference reachable at any depth within a datasheet's descendant
  tree, not only references found directly on a datasheet's own top-level entry

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

### Requirement: Bidirectional Allowlist Enforcement
Each scan SHALL check its recorded results against a maintained, per-pattern "known limitation"
allowlist in both directions: every result the scan produces SHALL match an allowlist entry, and
every allowlist entry SHALL have matched at least one result produced during that run. The scan
SHALL report failure if either direction does not hold, and SHALL identify, for each failure,
whether it is an unrecognized result (not on the allowlist) or a stale allowlist entry (on the
allowlist but not produced by the run).

#### Scenario: An unrecognized result fails the scan
- **WHEN** the scan produces a result that does not match any allowlist entry
- **THEN** the scan reports failure, identifying the unmatched result

#### Scenario: A stale allowlist entry fails the scan
- **WHEN** an allowlist entry matches none of the results the scan produced during a run
- **THEN** the scan reports failure, identifying the unmatched allowlist entry

#### Scenario: Fully matched results and allowlist pass
- **WHEN** every result the scan produces matches an allowlist entry, and every allowlist entry
  matches at least one produced result
- **THEN** the scan reports success

### Requirement: No-Op Without Local Clone
Each scan SHALL detect whether the local BSData clone is present before attempting any file
access. When the clone is absent, the scan SHALL skip its checks explicitly — visibly reported as
skipped, not as passed — and SHALL NOT report failure.

#### Scenario: Local clone absent
- **WHEN** a scan runs on a machine without the local BSData clone present
- **THEN** the scan performs no file access, is reported as skipped, and does not fail

### Requirement: Manually-Triggered Only
Each scan SHALL be excluded from the project's default test run and from continuous integration,
and SHALL only execute when explicitly and individually requested.

#### Scenario: Default test run
- **WHEN** the project's test suite is run without explicitly requesting a scan
- **THEN** neither scan executes
