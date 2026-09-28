## Purpose

Classifies every distinct ability/rule text from the extraction output into a structured
representation (target, effects, and condition triage) via an offline, batch LLM process, producing
a checked-in result file whose coverage status marks which records are safe to use without review -
never a live/runtime call.

## ADDED Requirements

### Requirement: Offline, batch-only classification
The classification pipeline SHALL run as an offline batch process against a pre-extracted corpus
file, and SHALL NOT be invoked from any live/runtime request path.

#### Scenario: Classification is triggered
- **WHEN** the classification tool is run
- **THEN** it reads from the checked-in extraction file and writes to a checked-in classification
  file, with no network dependency introduced into any deployed runtime service

### Requirement: Incremental re-classification
The classification pipeline SHALL classify a given hash only when that hash is missing from the
current classification file, the classification file's own recorded text for that hash no longer
matches the corpus's current text for it, or the prompt version differs from the one that produced
its existing classification.

Because a hash is a content digest of its own text (see `ability-corpus-extraction`'s
"Content-hash keying" requirement), a hash can never legitimately recur with genuinely different
text from ordinary pipeline operation - a real text change always produces a different hash
entirely, which is already covered by "that hash is missing." The middle condition above instead
guards against the two files (`ability-corpus.json`, hash-keyed by construction, and
`classifications.json`, edited independently) drifting apart - e.g. a person hand-correcting a
classification record's own stored text without updating its hash. This is a consistency check
between two independently-editable files, not a "the underlying text evolved" trigger.

#### Scenario: Unchanged text, unchanged prompt version
- **WHEN** the classification tool is re-run and a hash's extracted text and the current prompt
  version both match its existing classification record
- **THEN** that hash is not resubmitted for classification

#### Scenario: Prompt version changed
- **WHEN** the current prompt version differs from the version recorded on a hash's existing
  classification, even though its extracted text is unchanged
- **THEN** that hash is resubmitted for classification

#### Scenario: Classification file's stored text drifts from the corpus
- **WHEN** a hash's existing classification record's own stored text no longer matches what the
  corpus currently records for that same hash (e.g. after a hand edit to the classification record)
- **THEN** that hash is resubmitted for classification, so the corpus's own current text - not the
  stale/hand-edited copy - is what gets classified

### Requirement: Versioned prompts and schema
Each classification run SHALL use a specific, checked-in prompt version, and every resulting
classification record SHALL record which prompt version produced it.

#### Scenario: Inspecting a classification's provenance
- **WHEN** a person inspects a classification record
- **THEN** they can determine the exact prompt version, target schema, and model that produced it

### Requirement: Few-shot examples independent of prompt wording
Few-shot examples used to guide classification SHALL be stored separately from prompt wording, so
editing one does not require editing the other.

#### Scenario: Prompt wording is revised
- **WHEN** a prompt version's wording is edited
- **THEN** the few-shot examples file is unaffected and unchanged

### Requirement: Coverage-driven review
Every classification record SHALL carry a review status, defaulting to an unreviewed state when
first produced, and a coverage status of `complete`, `partial`, or `unclassifiable`. A `complete`
record states that everything the text says is captured, and is intended to be usable without human
review. A `partial` or `unclassifiable` record SHALL carry unclassified-residue text describing what
was left out and why.

#### Scenario: A freshly produced classification
- **WHEN** the classification tool produces a new classification for a hash that had none before
- **THEN** that record's review status is set to a pending/unreviewed state, not automatically
  approved

#### Scenario: A partially captured text
- **WHEN** a text states at least one representable effect and also something the schema cannot
  represent
- **THEN** the record's coverage status is `partial` and its residue text names what was left out

### Requirement: Feel No Pain, weapon keyword, and named-ability grant recognition
The classification schema SHALL be able to represent a Feel No Pain grant (with an optional
qualifier), a weapon-scoped keyword grant, and a closed-vocabulary named-ability grant, as distinct
result shapes.

#### Scenario: A Feel No Pain grant with a qualifier
- **WHEN** an ability's text grants a Feel No Pain value restricted to a specific damage-type
  qualifier (e.g. "against mortal wounds")
- **THEN** the classification records both the Feel No Pain value and its qualifier

#### Scenario: A weapon keyword grant
- **WHEN** an ability's text grants a named keyword to a selected set of the bearer's weapons
- **THEN** the classification records the weapon selector and the verbatim keyword text

#### Scenario: A named-ability grant
- **WHEN** an ability's text grants a whole separate named ability to its target
- **THEN** the classification records the verbatim granted ability name

#### Scenario: A weapon keyword upgrade
- **WHEN** an ability's text grants a weapon keyword instead of one the weapon already has (e.g.
  "[SUSTAINED HITS 2] instead of [SUSTAINED HITS 1]")
- **THEN** the classification records the granted keyword and the keyword it replaces

#### Scenario: A named-ability removal
- **WHEN** an ability's text removes a named ability (e.g. "lose the Dark Pacts ability")
- **THEN** the classification records the verbatim removed ability name, with no allowlist
  restriction

### Requirement: Choice groups
The classification schema SHALL represent a "select N of the following" choice as a group with a
minimum and maximum pick count and one label per option, with each option's own effects recorded as
ordinary effects tagged with the option they belong to.

#### Scenario: A one-of-N choice
- **WHEN** an ability's text lets the player select one of several listed abilities
- **THEN** the classification records one choice group with a pick count of exactly one, a label for
  every option, and each representable option effect tagged with its option

#### Scenario: A conditionally larger pick count
- **WHEN** the text allows more picks under a further condition (e.g. "select both if this unit
  made a Charge move this turn")
- **THEN** the group's maximum is the larger count and the group carries condition text saying when
  it applies

### Requirement: Condition triage per effect
Each classified effect SHALL carry a condition bucket of `none`, `evaluable-now`, or `never`, and the
classification as a whole SHALL separately record every phase in which the ability is activated, its
turn ownership, and its usage limit (e.g. "Once per battle").

#### Scenario: A phase-restricted effect
- **WHEN** an ability's text restricts an effect to a specific game phase and turn ownership
- **THEN** the classification records that phase and turn ownership as their own fields, distinct
  from the effect's condition bucket

#### Scenario: An ability activated in more than one phase
- **WHEN** an ability's text is activated in more than one phase (e.g. "each time this unit is
  selected to shoot or fight")
- **THEN** the classification records every one of those phases

#### Scenario: A roster-derivable condition
- **WHEN** an ability's effect is gated by a condition determinable from the roster's own tracked
  state (e.g. current wounds versus starting wounds)
- **THEN** that effect's condition bucket is `evaluable-now`, not `never`

#### Scenario: A genuinely unknowable condition
- **WHEN** an ability's effect is gated by a condition with no domain representation (e.g. board
  position)
- **THEN** that effect's condition bucket is `never`

#### Scenario: A usage-limited, player-activated effect
- **WHEN** an ability's text limits its own activation to a fixed frequency (e.g. "Once per battle")
- **THEN** the classification records that usage limit as its own field, independent of the effect's
  condition bucket, so an ability can carry a usage limit and a separate roster-derivable or
  unknowable condition at the same time

### Requirement: Human-readable condition text for conditions the structured fields don't capture
Whenever an effect's condition bucket is `evaluable-now` or `never`, the classification SHALL include
a human-readable condition text describing that condition. Condition text SHALL NOT merely restate
the phases, turn ownership, or usage limit, which are already structured fields the app can present
itself.

#### Scenario: An evaluable-now condition still carries readable text
- **WHEN** an effect's condition bucket is `evaluable-now`
- **THEN** the classification still includes the human-readable text describing that condition, not
  just the bucket value alone

#### Scenario: A phase-only gate
- **WHEN** an effect's only gate is the phase the ability is activated in
- **THEN** its condition bucket is `none` and it carries no condition text
