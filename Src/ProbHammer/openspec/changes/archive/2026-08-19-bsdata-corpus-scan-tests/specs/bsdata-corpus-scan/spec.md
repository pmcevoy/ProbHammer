## Purpose

Provides a manually-triggered, permanent regression scan across the full local BSData 11th
Edition corpus, catching new characteristic-resolution failures and unrecognized weapon-keyword
tokens against a maintained allowlist, so a real-data gap is never silently reintroduced or
silently left unnoticed once fixed.

## ADDED Requirements

### Requirement: Full-Corpus Characteristic-Resolution Scan
The system SHALL provide a manually-triggered scan that, when the local BSData clone is present,
attempts to resolve every named entry in every real catalogue file in the clone (excluding
`Warhammer 40,000.json`) and records any exception raised during resolution, keyed by a
recognizable pattern rather than by the specific datasheet or entry that raised it.

#### Scenario: A known ambiguous-InSv pattern is recorded
- **WHEN** the scan resolves a datasheet whose `InSv` characteristic is one of the known
  multi-value forms (e.g. `"4+* / 5+"` on Wyches/Howling Banshees, or `"5+ (Ranged)"` on a Titan
  or the Stormbird)
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
