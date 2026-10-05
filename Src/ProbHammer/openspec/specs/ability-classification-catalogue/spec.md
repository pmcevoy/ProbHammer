# ability-classification-catalogue Specification

## Purpose
Gives the running app one checked-in, hash-keyed catalogue of LLM-produced ability classifications,
and the single rule every consumer uses to decide whether a classified effect applies on its own.
## Requirements
### Requirement: Catalogue Lookup By Normalized-Text Content Hash
The system SHALL load the ability-classification catalogue once from a checked-in file bundled with
the web application, and SHALL look up an ability's classification by the content hash of its
normalized text - never by its Name. The normalization and hash SHALL be byte-identical to those the
pipeline's extractor uses to key its corpus records, so every extracted text's record is reachable
from the same text at runtime. An ability whose hash has no record SHALL have no classification, and
SHALL be treated by every consumer exactly as an ability that classifies to no effects. A missing
catalogue file SHALL load as an empty catalogue rather than fail startup.

#### Scenario: Two differently-named abilities sharing one text share one classification
- **WHEN** two abilities with different Names carry identical rules text
- **THEN** both resolve to the same catalogue record

#### Scenario: Authoring variance that normalization folds does not change the lookup
- **WHEN** an ability's text differs from the extracted corpus text only by a typographic apostrophe
  or a non-breaking space in place of a plain one
- **THEN** it resolves to the same catalogue record

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
relevant; an optional turn ownership; an optional usage limit; a coverage status; and optional
unclassified residue. Effects SHALL be one of seven kinds - Statline scalar, invulnerable save,
weapon characteristic, Feel No Pain, weapon keyword grant, named ability grant, named ability
removal - and every kind SHALL load, whether or not any consumer acts on it yet. A record of an
unrecognized effect or target kind SHALL fail the load rather than be skipped.

#### Scenario: A record using a not-yet-consumed effect kind loads
- **WHEN** the catalogue contains a record whose only effect is a weapon keyword grant
- **THEN** the catalogue loads, and the record exposes that effect with its weapon selector and
  keyword

#### Scenario: A multi-keyword target keeps every keyword
- **WHEN** a record's target names the keywords LEAGUES OF VOTANN and INFANTRY
- **THEN** the record's target exposes both keywords

#### Scenario: An unrecognized effect kind fails the load
- **WHEN** the catalogue file contains an effect whose kind is none of the seven known kinds
- **THEN** loading the catalogue fails with an error naming the problem

### Requirement: Coverage Status Never Gates Use
The system SHALL use every catalogue record regardless of its coverage status: a `partial` or
`unclassifiable` record's captured effects, phases, turn ownership and usage limit SHALL be used
exactly as a `complete` record's would be.

#### Scenario: A partial record's unconditional effect applies
- **WHEN** a present ability's record has coverage status `partial` and includes an unconditional
  Statline scalar effect
- **THEN** that effect applies exactly as it would on a `complete` record

### Requirement: Unconditional Effect Rule
A classified effect SHALL be unconditional only when its residual-condition bucket is none, it
carries no choice branch, and its record has no usage limit and no turn ownership. Every other
classified effect SHALL be conditional. Every consumer that applies an effect automatically SHALL
apply only unconditional effects and conditional effects the player has activated (per
`condition-activation`); what a consumer does with any other conditional effect is defined by that
consumer.

#### Scenario: A usage-limited effect is conditional even with no residual condition
- **WHEN** a record's effect has residual-condition bucket none, and the record's usage limit is
  "Once per battle"
- **THEN** the effect is conditional

#### Scenario: A turn-restricted effect is conditional
- **WHEN** a record's effect has residual-condition bucket none and no choice branch, and the
  record's turn ownership is "mine"
- **THEN** the effect is conditional

#### Scenario: A choice-branch effect is conditional
- **WHEN** a record's effect belongs to a choice branch
- **THEN** the effect is conditional

#### Scenario: Phases alone do not make an effect conditional
- **WHEN** a record lists one or more phases, its effect has residual-condition bucket none and no
  choice branch, and the record has no usage limit and no turn ownership
- **THEN** the effect is unconditional

#### Scenario: An activated conditional effect is applied
- **WHEN** a record's effect is conditional and the player has activated it on a unit
- **THEN** a consumer that applies effects automatically applies it on that unit

### Requirement: Catalogue File Is Exported From The Pipeline With Canonical Names
The pipeline's classifier tool SHALL provide an export that writes the catalogue file from its
classification results, one record per classified hash, carrying that record's source text and every Name the extractor saw it under. Wherever the classification's name resolution
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

