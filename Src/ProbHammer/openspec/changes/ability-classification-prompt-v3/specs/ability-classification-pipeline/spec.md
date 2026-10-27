# Spec Delta

## MODIFIED Requirements

### Requirement: Coverage-driven review
Every classification record SHALL carry a review status, defaulting to an unreviewed state when
first produced, and a coverage status of `complete`, `partial`, or `unclassifiable`. A `complete`
record states that everything the text says within LivePlay's scope is captured, and is intended to
be usable without human review. Residue SHALL be split into in-scope residue (something LivePlay or
the simulation would use but the schema cannot represent) and out-of-scope residue (tactical
actions, resource or token economies, Command points, movement, reserves and deployment, effects on
enemy units only, objective control, list legality). Only in-scope residue SHALL keep a record from
`complete`. A `partial` or `unclassifiable` record SHALL carry in-scope residue text describing what
was left out and why.

#### Scenario: A freshly produced classification
- **WHEN** the classification tool produces a new classification for a hash that had none before
- **THEN** that record's review status is set to a pending/unreviewed state, not automatically
  approved

#### Scenario: A partially captured text
- **WHEN** a text states at least one representable effect and also something in scope that the
  schema cannot represent
- **THEN** the record's coverage status is `partial` and its in-scope residue names what was left out

#### Scenario: Only out-of-scope residue remains
- **WHEN** a text's every in-scope statement is captured, and it also lets the unit perform a
  tactical action
- **THEN** the record's coverage status is `complete`, and the tactical action is recorded as
  out-of-scope residue

### Requirement: Keyword targets require every listed keyword
A keyword-filtered target SHALL record a list of keywords, every one of which a unit must have. A
multi-word faction keyword followed by a unit-type keyword SHALL be split into separate keywords.

#### Scenario: A faction plus unit-type target
- **WHEN** an ability's text addresses "a friendly Leagues of Votann Infantry unit"
- **THEN** the target records the keywords `LEAGUES OF VOTANN` and `INFANTRY` separately, not one
  fused keyword

#### Scenario: A multi-word faction keyword plus a god keyword
- **WHEN** an ability's text addresses "LEGIONES DAEMONICA TZEENTCH units"
- **THEN** the target records the keywords `LEGIONES DAEMONICA` and `TZEENTCH` separately

### Requirement: Incremental re-classification
The classification pipeline SHALL classify a given hash only when that hash is live (seen by the
latest extraction, its last-seen date equal to the newest last-seen date in the corpus) and is
missing from the current classification file, the classification file's own recorded text for that
hash no longer matches the corpus's current text for it, or the prompt version differs from the one
that produced its existing classification. A stale hash, kept in the corpus but not seen by the
latest extraction, SHALL NOT be submitted.

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

#### Scenario: A stale hash is not submitted
- **WHEN** a corpus hash's last-seen date is older than the newest last-seen date in the corpus
- **THEN** that hash is not submitted, whatever its classification state

## ADDED Requirements

### Requirement: Roll modifier recognition
The classification schema SHALL represent a modifier to a dice roll as a roll-modifier effect,
recording the roll (Hit, Wound, Damage, Save, Advance, Charge or Battle-shock), whose roll it is
(`friendly` for the target's own rolls, `enemy` for an opponent's rolls against the target), the
modifier (add, subtract, re-roll, re-roll ones, re-roll a single roll, critical on X+, or ignore
modifiers), and a non-negative amount. Text modifying two rolls SHALL produce one effect per roll.

#### Scenario: A re-roll of ones
- **WHEN** classifying "each time a model in this unit makes an attack, you can re-roll a Hit roll
  of 1"
- **THEN** the result has one roll-modifier effect: Hit, `friendly`, re-roll ones

#### Scenario: Two rolls in one sentence
- **WHEN** classifying "add 1 to the Hit roll and add 1 to the Wound roll"
- **THEN** the result has two roll-modifier effects, one adding 1 to Hit and one adding 1 to Wound

#### Scenario: A critical-hit threshold
- **WHEN** classifying "an unmodified Hit roll of 5+ scores a Critical Hit"
- **THEN** the result has one roll-modifier effect: Hit, `friendly`, critical on X+, amount 5

#### Scenario: A single re-roll
- **WHEN** classifying "once per turn, you can re-roll one Hit roll made for the bearer"
- **THEN** the result has one roll-modifier effect: Hit, `friendly`, re-roll a single roll, with the
  usage limit `Once per turn`

#### Scenario: Ignoring modifiers
- **WHEN** classifying "you can ignore any or all modifiers to the Hit roll"
- **THEN** the result has one roll-modifier effect: Hit, `friendly`, ignore modifiers

### Requirement: Rolls made against the target are enemy rolls
A modifier to a Hit, Wound or Damage roll made for an attack that targets the ability's target SHALL
be recorded with `rolledBy` `enemy`. A reduction to the Damage characteristic of an attack allocated
to the target SHALL be a Damage roll-modifier with `rolledBy` `enemy` and modifier subtract. A
modifier to a roll made by enemy units that does not depend on attacking the target SHALL be
residue, as an effect on enemy units.

#### Scenario: A defensive Hit modifier
- **WHEN** classifying "each time a melee attack targets this unit, subtract 1 from the Hit roll"
- **THEN** the result has one roll-modifier effect: Hit, `enemy`, subtract, amount 1, selecting melee
  weapons

#### Scenario: A Damage reduction
- **WHEN** classifying "each time an attack is allocated to this model, subtract 1 from the Damage
  characteristic of that attack"
- **THEN** the result has one roll-modifier effect: Damage, `enemy`, subtract, amount 1

#### Scenario: An enemy-wide debuff stays residue
- **WHEN** classifying "while an enemy unit is within 3\" of this unit, each time a model in that
  unit makes an attack, subtract 1 from the Hit roll"
- **THEN** the result has no roll-modifier effect, and the text is recorded as out-of-scope residue

### Requirement: Attack-roll modifiers carry a weapon selector
A roll-modifier effect on a Hit, Wound or Damage roll SHALL carry a weapon selector naming which
attacks it applies to: every weapon when the text names no class or weapon, a weapon class for
"melee attacks" or "ranged attacks", or a named weapon. A roll-modifier effect on any other roll
SHALL carry no selector.

#### Scenario: A ranged-only Hit bonus
- **WHEN** classifying "each time a model in this unit makes a ranged attack, add 1 to the Hit roll"
- **THEN** the effect's selector is the ranged weapon class

#### Scenario: A Charge roll has no selector
- **WHEN** classifying "you can re-roll Charge rolls made for this unit"
- **THEN** the effect is Charge, `friendly`, re-roll, with no selector

### Requirement: Event-triggered effects are never evaluable
An effect that applies only when an event happens ("each time this unit Advances", "each time this
unit makes a Charge move") SHALL have condition bucket `never`, with the event as its condition
text, and SHALL NOT be recorded as an unconditional effect limited to that event's phase. An
optional activation that switches an effect on for a limited time ("you can choose to invoke... if
you do, until the end of the phase") SHALL likewise be `never`, with the activation as its condition
text.

#### Scenario: A trigger-only text
- **WHEN** classifying "each time this unit Advances, add 6\" to the Move characteristic of models in
  this unit"
- **THEN** the Move effect's condition bucket is `never`, with condition text naming the Advance

#### Scenario: An optional limited-time activation
- **WHEN** classifying "each time this unit is selected to fight, you can choose to invoke its
  extremis trigger word. If you do, then until the end of the phase, arco-flails equipped by models
  in this unit have an Attacks characteristic of 6"
- **THEN** the Attacks effect's condition bucket is `never`, with condition text naming the
  invocation

#### Scenario: A Trigger/Effect manoeuvre
- **WHEN** classifying "■ Trigger: When an eligible unit from your army is selected to make a Normal,
  Advance or Fall Back move. ■ Effect: Until the end of the phase, add 2\" to the Move characteristic
  of models in that unit"
- **THEN** the Move effect's condition bucket is `never`, with condition text naming the manoeuvre

### Requirement: The bearer's own models are Self
Text addressing models in the bearer's unit that share the bearer's own keyword ("DAEMONHOST models
in that unit", when the bearer is a Daemonhost) SHALL target `Self`, not the whole attached unit.

#### Scenario: A unit's own datasheet ability naming its own models
- **WHEN** classifying a Paladin Squad ability that says "add 1 to the Damage characteristic of melee
  weapons equipped by Paladin Squad models in this unit"
- **THEN** the target is `Self`, and no condition text names the model subset

#### Scenario: A retinue ability naming its own models
- **WHEN** classifying a Daemonhost ability that says "the Objective Control characteristic of
  DAEMONHOST models in that unit is 1"
- **THEN** the target is `Self`

### Requirement: A Feel No Pain qualifier names only a damage source
A Feel No Pain qualifier SHALL name only the source of the damage it applies against (for example
mortal wounds or Psychic Attacks). A model subset or a situation SHALL NOT be recorded as the
qualifier; it is a condition or a target restriction instead.

#### Scenario: A model-subset restriction
- **WHEN** classifying "CHARACTER models in this unit have the Feel No Pain 5+ ability"
- **THEN** the Feel No Pain effect has no qualifier

### Requirement: Turn ownership only when the text says whose turn
The classification SHALL record a turn ownership only when the text names whose turn it is ("your
Shooting phase", "your opponent's turn"), or when the ability is triggered by a Normal, Advance or
Fall Back move, which only happens in its owner's Movement phase (`mine`). A phase named without an
owner SHALL leave turn ownership unset, and a Charge move or the attack sequence SHALL NOT imply one.

#### Scenario: A phase without an owner
- **WHEN** classifying "in the Fight phase, add 1 to the Attacks characteristic of melee weapons
  equipped by models in this unit"
- **THEN** the classification records the Fight phase and no turn ownership

#### Scenario: An Advance trigger
- **WHEN** classifying "each time this model Advances, do not make an Advance roll for it. Instead,
  until the end of the phase, add 9\" to the Move characteristic of this model"
- **THEN** the classification records the Movement phase and turn ownership `mine`

### Requirement: Weapon names containing "and" stay whole
A named-weapon selector SHALL record a weapon name containing "and" as one name, not one name per
side of the "and".

#### Scenario: A paired weapon name
- **WHEN** classifying text that names the weapon "Tyrnak and Fenrir"
- **THEN** the selector records the single name "Tyrnak and Fenrir"

### Requirement: Absent values are JSON null
An optional classification field with no value SHALL be JSON `null` or omitted, never a string
standing in for no value (`"null"`, `"none"`, an empty string).

#### Scenario: An unqualified Feel No Pain
- **WHEN** classifying "this model has the Feel No Pain 5+ ability"
- **THEN** the Feel No Pain qualifier is JSON `null`, not the string `"null"`

### Requirement: Model chosen per run
Each classification run SHALL use a model chosen when it is submitted, defaulting to the model of the
last full run, and every record SHALL record the model that produced it.

#### Scenario: A run on a non-default model
- **WHEN** a person submits a run naming Sonnet 5.5
- **THEN** every record that run produces records Sonnet 5.5 as its model

### Requirement: A test run can be collected without touching the classification file
Collecting a run SHALL be able to write its results to a separate file named by the person, leaving
the classification file unchanged, so two test runs on the same hashes can be compared and
hand-corrected records are not overwritten.

#### Scenario: Collecting a comparison run
- **WHEN** a person collects a test run into a named comparison file
- **THEN** the results are written to that file with the run's token usage reported, and
  `classifications.json` is byte-identical to before

### Requirement: A keyword choice keeps the effects it gates
When a text has the player select a keyword or parameter that a stated effect then depends on, the
classification SHALL still record that effect, with condition bucket `never` and condition text
naming the selection, and SHALL record only the selection itself as in-scope residue.

#### Scenario: Re-rolls against a chosen keyword
- **WHEN** classifying "at the start of the battle, select one of the following keywords: Infantry,
  Monster, Mounted, Vehicle. Each time this model makes an attack that targets a unit with the
  selected keyword, re-roll a Hit roll of 1 and re-roll a Wound roll of 1"
- **THEN** the result has a Hit and a Wound re-roll-ones effect, each `never` naming the selected
  keyword, and in-scope residue describing the keyword selection
