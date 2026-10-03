# ability-corpus-extraction Specification

## Purpose

Produces a single, human-readable, hash-keyed file covering every distinct rule/ability/Enhancement
text in the BSData corpus, so corpus content can be inspected, searched, and fed to downstream
classification without hand-parsing the multi-file catalogue structure each time.

## Requirements

### Requirement: Deduplicated extraction by normalized text
The extraction tool SHALL produce exactly one record per distinct normalized ability/rule text,
regardless of how many differently-named datasheets or wargear items reference that same text.

#### Scenario: Two differently-named grants share identical text
- **WHEN** two different wargear items in the corpus grant the same rule text verbatim (e.g. "The
  bearer has a 4+ invulnerable save.")
- **THEN** the extraction output contains exactly one record for that text, with both source names
  recorded on the same record

### Requirement: Content-hash keying
Each extraction record SHALL be keyed by a stable hash computed over the normalized text alone, not
over the text's associated name(s).

#### Scenario: Re-running extraction against unchanged BSData
- **WHEN** the extraction tool is run twice against an unchanged BSData corpus
- **THEN** every record's hash key is identical across both runs

### Requirement: Source-kind tagging
Each extraction record SHALL be tagged with the kind of source it came from (at minimum: Datasheet
ability, Enhancement, Detachment rule, Core rule, Army rule, raw shared rule).

#### Scenario: A Detachment rule is distinguishable from a Datasheet ability
- **WHEN** the corpus contains both a Detachment rule and a Datasheet ability
- **THEN** their extraction records carry different source-kind tags, even if unrelated in content

### Requirement: Reuse of existing corpus-resolution logic
The extraction tool SHALL resolve corpus content using the project's existing BSData
closure-resolution and rule-walking logic, and SHALL NOT implement independent parsing of
`catalogueLinks`, `infoLink`, or `infoGroup` structures.

#### Scenario: A rule reachable only through catalogueLink resolution
- **WHEN** a rule's text is only reachable by following a `catalogueLink` from one file into another
- **THEN** the extraction tool's output still includes that rule's text, correctly resolved

### Requirement: Presence tracking across runs
Each extraction record SHALL track when it was first observed and when it was most recently observed,
so content that disappears from a later corpus version is detectable rather than silently dropped.

#### Scenario: An ability is removed in a later BSData update
- **WHEN** an ability present in one extraction run is absent from a subsequent run against updated
  BSData
- **THEN** that ability's record is not deleted, and its last-observed timestamp does not advance on
  the later run, making the removal visible

### Requirement: Human-searchable output format
The extraction tool's output SHALL be a single file formatted so a person can locate a specific
ability's record by searching for its name in a text editor.

#### Scenario: Looking up a known ability by name
- **WHEN** a person opens the extraction output file in a text editor and searches for an ability
  name they already know
- **THEN** the search locates that ability's full record, including its actual current text

### Requirement: Aggregate summary reporting
The extraction tool SHALL report aggregate counts of what it found - at minimum, a count of distinct
records per source kind and the raw-to-distinct occurrence ratio - so a person can assess the shape
of the corpus without inspecting individual records.

#### Scenario: Reviewing corpus composition after a run
- **WHEN** the extraction tool completes a run
- **THEN** it reports how many distinct records exist per source kind, and how many raw occurrences
  collapsed into that distinct count, without requiring the person to write an ad hoc query first
