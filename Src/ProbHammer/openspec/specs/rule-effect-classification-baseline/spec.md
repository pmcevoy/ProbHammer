# rule-effect-classification-baseline Specification

## Purpose

Records which specific rule/ability text classifications a human has actually verified as correct,
so a reviewer of the corpus-wide classification report never has to re-read an already-verified,
unchanged result, while any genuine change to a previously-verified answer is always surfaced.

## Requirements

### Requirement: Baseline Records Human-Verified Classifications
The system SHALL maintain a durable, checked-in record of rule/ability classifications a human has
explicitly verified as correct, keyed by the rule/ability's own normalized Text. The record SHALL
be independent of any specific rule/ability Name, so identical Text verified under one Name is
recognized as the same verified entry when later seen under a different Name.

A baseline entry SHALL support recording that the current classification, while verified correct as
far as it goes, is known to be incomplete against the source text — e.g. text that states real
content (a keyword grant, an additional ability, a recurring effect) the classification's current
vocabulary has no way to extract. This is distinct from an entry with no such note, which represents
the classification as verified complete against its text.

#### Scenario: A verified text is recognized regardless of which name carries it
- **WHEN** a rule/ability's normalized Text matches a baseline entry, even though the Name attached
  to it in this run differs from the Name recorded when the entry was verified
- **THEN** the text is treated as already verified

#### Scenario: A verified-but-incomplete entry keeps its known gap documented
- **WHEN** a baseline entry represents a classification verified correct but known to omit real
  content the text states (e.g. Blastajet Force Field's InSv grant is correctly extracted, but its
  text also states losing a keyword, which nothing in the classification captures)
- **THEN** that gap is recorded on the entry itself, not left implicit or discoverable only outside
  the baseline

### Requirement: Per-Field Value Drift Is Always Surfaced
The system SHALL compare each field of a baselined text's freshly-computed classification against
the value recorded in the baseline. When a field present in the baseline computes a different value
than what was recorded, the system SHALL always surface that text as changed, never silently
accept or discard the difference.

#### Scenario: An unchanged verified entry is not reprinted
- **WHEN** every field of a baselined text's freshly-computed classification matches the value
  recorded in the baseline
- **THEN** the text is excluded from the report's normal listings and counted only in a collapsed
  summary total

#### Scenario: A changed field on a verified entry is always surfaced
- **WHEN** a field recorded in the baseline for a text (e.g. its Target, or one of its Effects)
  computes a different value on a fresh run
- **THEN** the text is surfaced in the report as changed, regardless of whether the new value looks
  more or less correct than the recorded one

### Requirement: New Classification Fields Backfill Without Forcing Re-Verification
When the classification schema gains a field that a baseline entry's recorded classification does
not carry (because the entry was verified before that field existed), the system SHALL NOT treat
the field's absence as drift. It SHALL compute the new field's value and, when that value equals
the field's own defined default, silently record it into the baseline with no further review
required. When the computed value is not the field's default, the system SHALL surface the text
once, labeled as newly discovered information on an already-verified entry — never as a request to
re-verify the fields that did not change.

#### Scenario: A newly added field computing to its default backfills silently
- **WHEN** a classification schema field did not exist at the time a baseline entry was verified,
  and freshly computing it for that entry's text produces that field's defined default value
- **THEN** the baseline entry is updated to record the default value with no report output for that
  text

#### Scenario: A newly added field computing to a non-default value is surfaced once
- **WHEN** a classification schema field did not exist at the time a baseline entry was verified,
  and freshly computing it for that entry's text produces a value other than that field's defined
  default
- **THEN** the text is surfaced in the report, labeled as new information found on an
  already-verified entry, without re-presenting the fields that were already verified and remain
  unchanged

### Requirement: Baseline Snapshot Writing
The system SHALL provide a way to record the current full classification of every corpus text
already tracked in the baseline, plus any newly-reviewed text, into the baseline in one operation,
so that accepting a completed review pass does not require hand-editing the baseline's stored
records. This operation SHALL update only computed classification fields; it SHALL NOT remove or
alter a human-authored known-incomplete note already recorded on an entry, since that note is not
something a fresh classification run can derive on its own.

#### Scenario: Writing a snapshot updates the baseline to match current output
- **WHEN** a snapshot-write is run after a report run
- **THEN** the baseline's recorded classification for every text it tracks, plus any text newly
  added to it, matches that run's freshly-computed classification

#### Scenario: Writing a snapshot preserves an existing known-incomplete note
- **WHEN** a snapshot-write is run for an entry that already carries a known-incomplete note
- **THEN** the entry's classification fields are refreshed but its note is left unchanged

### Requirement: Baseline Storage Is Isolated From BSData Catalogue Scanning
The baseline's stored data SHALL NOT reside in any directory that the BSData catalogue-loading
pipeline scans as a source of catalogue files, so that adding to or changing the baseline can never
be misinterpreted as a new or modified BSData catalogue.

#### Scenario: The baseline file is never enumerated as a catalogue file
- **WHEN** the BSData catalogue-loading pipeline enumerates its configured catalogue directory
- **THEN** the baseline's stored data is not among the files it discovers
