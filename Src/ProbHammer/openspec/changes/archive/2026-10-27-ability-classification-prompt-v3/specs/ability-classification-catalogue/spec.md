# Spec Delta

## MODIFIED Requirements

### Requirement: Catalogue Lookup By Normalized-Text Content Hash
The system SHALL load the ability-classification catalogue once from a checked-in file bundled with
the web application, and SHALL look up an ability's classification by the content hash of its
normalized text - never by its Name. The normalization and hash SHALL be byte-identical to those the
pipeline's extractor uses to key its corpus records, so every extracted text's record is reachable
from the same text at runtime. Normalization SHALL fold a typographic apostrophe, a non-breaking
space in place of a plain one, and whitespace surrounding the text. An ability whose hash has no
record SHALL have no classification, and SHALL be treated by every consumer exactly as an ability
that classifies to no effects. A missing catalogue file SHALL load as an empty catalogue rather than
fail startup.

#### Scenario: Two differently-named abilities sharing one text share one classification
- **WHEN** two abilities with different Names carry identical rules text
- **THEN** both resolve to the same catalogue record

#### Scenario: Authoring variance that normalization folds does not change the lookup
- **WHEN** an ability's text differs from the extracted corpus text only by a typographic apostrophe
  or a non-breaking space in place of a plain one
- **THEN** it resolves to the same catalogue record

#### Scenario: Surrounding whitespace does not change the lookup
- **WHEN** BSData's text for an ability ends in whitespace and a NewRecruit export carries the same
  text trimmed
- **THEN** the imported ability resolves to that text's catalogue record

#### Scenario: Every record's key is reproducible from its own text
- **WHEN** the checked-in catalogue file is loaded
- **THEN** recomputing the content hash of every record's own text yields that record's key

#### Scenario: An unclassified ability produces no effect
- **WHEN** an ability's normalized text has no catalogue record
- **THEN** no consumer derives any value, reference or attachment from it

#### Scenario: A missing catalogue file loads empty
- **WHEN** the catalogue file does not exist at its configured path
- **THEN** the app starts with an empty catalogue, and every ability behaves as unclassified

### Requirement: Catalogue Records Carry The Full Classification Shape
Each catalogue record SHALL expose: a target (the bearer, the bearer's whole attached unit, a set of
unit keywords that a unit must all carry, or unconditional); an ordered list of classified effects,
each with its effect, a residual-condition bucket (none, evaluable now, or never evaluable), optional
condition text, and an optional choice branch; choice groups; the phases in which the ability is
relevant; an optional turn ownership; an optional usage limit; a coverage status; optional
unclassified (in-scope) residue; and optional out-of-scope residue. Effects SHALL be one of eight
kinds - Statline scalar, invulnerable save, weapon characteristic, Feel No Pain, weapon keyword
grant, named ability grant, named ability removal, roll modifier - and every kind SHALL load, whether
or not any consumer acts on it yet. A record of an unrecognized effect or target kind SHALL fail the
load rather than be skipped.

#### Scenario: A record using a not-yet-consumed effect kind loads
- **WHEN** the catalogue contains a record whose only effect is a weapon keyword grant
- **THEN** the catalogue loads, and the record exposes that effect with its weapon selector and
  keyword

#### Scenario: A roll-modifier record loads
- **WHEN** the catalogue contains a record whose only effect is a roll modifier re-rolling Hit rolls
  of 1 for ranged attacks
- **THEN** the catalogue loads, and the record exposes the roll, whose roll it is, the modifier, the
  amount and the weapon selector

#### Scenario: A multi-keyword target keeps every keyword
- **WHEN** a record's target names the keywords LEAGUES OF VOTANN and INFANTRY
- **THEN** the record's target exposes both keywords

#### Scenario: An unrecognized effect kind fails the load
- **WHEN** the catalogue file contains an effect whose kind is none of the eight known kinds
- **THEN** loading the catalogue fails with an error naming the problem

### Requirement: Catalogue File Is Exported From The Pipeline With Canonical Names
The pipeline's classifier tool SHALL provide an export that writes the catalogue file from its
classification results, one record per classified hash that is live in the extracted corpus (its
last-seen date equal to the newest last-seen date in the corpus), carrying that record's source text
and every Name the extractor saw it under. A classified hash that is stale or absent from the corpus
SHALL be left out of the export but kept in the classification results, and the export SHALL report
how many it left out. Wherever the classification's name resolution
resolved a name - a weapon keyword, a named weapon, a target keyword, a granted or removed ability
name - the exported record SHALL carry the canonical BSData spelling in place of the verbatim one; an
unresolved name SHALL be exported verbatim. Review metadata, the prompt version and model SHALL NOT
be exported.

#### Scenario: A named-weapon selector is exported with its canonical name
- **WHEN** a classification's named-weapon selector was recorded verbatim as "heavy bolters" and
  resolved to the canonical name "Heavy bolter"
- **THEN** the exported record's selector names "Heavy bolter"

#### Scenario: An unresolved name is exported verbatim
- **WHEN** a classification records a granted ability name that name resolution could not resolve
- **THEN** the exported record carries that name exactly as the classification recorded it

#### Scenario: Re-exporting unchanged results is deterministic
- **WHEN** the export runs twice against unchanged classification results
- **THEN** both runs write byte-identical files

#### Scenario: A classification of text no longer in BSData is left out
- **WHEN** the classification results hold a record whose hash the corpus still keeps but the
  latest extraction did not see
- **THEN** the exported catalogue has no record for that hash, the classification results still
  hold it, and the export reports one record left out

## ADDED Requirements

### Requirement: Export Refuses Control Characters
The export SHALL check every string in the classifications it is about to write and, when any
contains a control character other than a line feed, SHALL write nothing and report each offending
record's hash and field.

#### Scenario: A classification string carries a carriage-return escape
- **WHEN** a classification's condition text contains a carriage return in place of an apostrophe
- **THEN** the export writes no catalogue file and reports that record's hash and the condition-text
  field
