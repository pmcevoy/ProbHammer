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

### Requirement: Caveated Entries Without A Recorded Decision Are Surfaced For Review
The system SHALL list, on every report run, every baselined text whose freshly-computed
classification is caveated, whose baseline entry carries no recorded note, and whose baseline entry is
not marked fully handled — independent of whether that text's classification is otherwise Unchanged,
Drift, or NewInformation. A caveated entry gaining a recorded note or being marked fully handled SHALL
no longer appear in this listing on a subsequent run.

#### Scenario: A caveated, unchanged entry with no recorded decision is listed for review
- **WHEN** a baselined text's freshly-computed classification is caveated, matches the baseline in
  every other respect, and the baseline entry carries no note and is not marked fully handled
- **THEN** the text appears in the report's review listing, not only in the collapsed unchanged
  summary count

#### Scenario: A caveated entry with a recorded note is not listed
- **WHEN** a baselined text's freshly-computed classification is caveated and its baseline entry
  carries a recorded note
- **THEN** the text does not appear in the review listing

#### Scenario: A caveated entry marked fully handled is not listed
- **WHEN** a baselined text's freshly-computed classification is caveated and its baseline entry is
  marked fully handled
- **THEN** the text does not appear in the review listing, even if it carries no note

#### Scenario: A non-caveated entry is never listed
- **WHEN** a baselined text's freshly-computed classification is not caveated
- **THEN** the text does not appear in the review listing, regardless of its note or fully-handled state

### Requirement: A Human Verdict Distinguishes Fully-Handled Caveats From Genuinely Incomplete Ones
The system SHALL let a baseline entry record a human's confirmation that a caveated classification's
un-extracted trailing text names no further game effect (e.g. an army-composition eligibility
restriction, flavor text) as distinct from the classification's own computed caveated state, which
remains a permanent, purely textual fact the system SHALL NOT alter based on this verdict. This
recorded verdict SHALL NOT be computed, refreshed, or altered by any report run — it is exclusively
human-authored, the same as a recorded note.

#### Scenario: A fully-handled entry's own caveated fact never changes
- **WHEN** a baseline entry is marked fully handled
- **THEN** its classification's own caveated value, freshly computed from its text, is unaffected and
  remains true

#### Scenario: Marking an entry fully handled is never done by a report run
- **WHEN** a baseline snapshot is written
- **THEN** no baseline entry's fully-handled marking is added, removed, or altered by that operation

### Requirement: Baseline Entries Record Findable Names
The system SHALL record, for each baseline entry, every distinct rule/ability Name currently seen
carrying that entry's Text, refreshed whenever a baseline snapshot is written. This recorded Name
information SHALL NOT be treated as part of an entry's identity — two entries SHALL NOT be considered
distinct solely because they record different Names, and lookup SHALL continue to be by Text alone.

#### Scenario: A baseline snapshot backfills current Names onto a tracked entry
- **WHEN** a baseline snapshot is written for a tracked entry whose Text is seen under one or more
  Names in the current corpus run
- **THEN** the entry records every one of those Names

#### Scenario: Several differently-named abilities sharing identical Text all appear on one entry
- **WHEN** more than one distinct rule/ability Name is seen carrying the exact same Text
- **THEN** the single tracked entry for that Text records every one of those Names, not just one

#### Scenario: Recorded Names never affect entry lookup
- **WHEN** a corpus text is looked up against the baseline
- **THEN** the lookup is performed by Text alone, regardless of what Names are recorded on any entry
