## ADDED Requirements

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
