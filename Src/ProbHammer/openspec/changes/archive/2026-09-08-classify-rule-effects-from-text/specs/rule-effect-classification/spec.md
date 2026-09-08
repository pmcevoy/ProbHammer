## Purpose

Extracts, from a rule or ability's own Name and free-text Text alone, who it affects (its Target)
and what unconditional characteristic mutations it states (its Effects) — independent of any
specific catalogue JSON format or resolved roster, so the same classification applies uniformly
regardless of which import pipeline produced the rule/ability text.

## ADDED Requirements

### Requirement: Text-Only Input, No Catalogue Format Dependency
The system SHALL classify a rule/ability using only its Name and Text as input, with no dependency
on BSData JSON structure, BattleScribe/NewRecruit JSON structure, or any other catalogue-format-
specific type. The same classification SHALL be produced for identical Name+Text regardless of
which import pipeline the rule/ability originated from.

#### Scenario: Identical text classifies identically regardless of source pipeline
- **WHEN** two abilities from different import pipelines share the exact same Name and Text
- **THEN** classifying either produces the same Target and the same Effects

### Requirement: Target Classification Is Always Present
The system SHALL classify every rule/ability's Target as exactly one of a closed set of values,
never absent and never ambiguous: the bearer alone (`Self`), the bearer's whole attached unit
(`AttachedUnit`), a roster-wide target filtered by a named keyword found in the text
(`KeywordRuleTarget`), or a roster-wide target with no keyword qualifier (`UnconditionalRuleTarget`).
`Self` SHALL be the result when no broader target language is recognized in the text.

#### Scenario: No broader target language classifies as Self
- **WHEN** a rule/ability's text states an effect on "the bearer" with no further qualification
  (e.g. Shield Dome: "The bearer has a 5+ invulnerable save.")
- **THEN** its Target is `Self`

#### Scenario: "the bearer's unit" language classifies as AttachedUnit
- **WHEN** a rule/ability's text states an effect on "models in the bearer's unit" (e.g. Vexilla:
  "Add 1 to the Objective Control characteristic of models in the bearer's unit.")
- **THEN** its Target is `AttachedUnit`

#### Scenario: A named keyword qualifying "units" classifies as a keyword target
- **WHEN** a rule/ability's text names a specific keyword qualifying which units it affects (e.g.
  Templar Vows: "...for ADEPTUS ASTARTES units from your army"; a Detachment rule: "Friendly SWORD
  BRETHREN SQUAD units have +1 OC")
- **THEN** its Target is a `KeywordRuleTarget` carrying that keyword (`"ADEPTUS ASTARTES"` /
  `"SWORD BRETHREN SQUAD"` respectively)

### Requirement: Unconditional Characteristic Effect Extraction
The system SHALL extract zero or more Effects from a rule/ability's Text, each stating an
unconditional mutation of exactly one named `Statline` scalar characteristic (M/T/Sv/W/Ld/Oc) by a
fixed amount, using one of three verbs matching the text's own rulebook vocabulary: `Improve`,
`Worsen`, or `Set`. An Effect SHALL target exactly one characteristic; a rule/ability whose text
states mutations to more than one characteristic SHALL produce one Effect per characteristic
mutated. The system SHALL NOT attempt to resolve an Effect's arithmetic sign or compute a final
value — it extracts only what the text states, in the text's own vocabulary.

#### Scenario: A Set-verb effect extracts with its stated value
- **WHEN** a rule/ability's text grants a specific value outright (e.g. Shield Dome: "The bearer has
  a 5+ invulnerable save.")
- **THEN** one Effect is extracted: `Set` the `InSv` characteristic to `5`

#### Scenario: An Improve-verb effect extracts with its stated amount
- **WHEN** a rule/ability's text adds a fixed amount to a named characteristic (e.g. Vexilla: "Add 1
  to the Objective Control characteristic...")
- **THEN** one Effect is extracted: `Improve` the `Oc` characteristic by `1`

#### Scenario: Text with no recognizable effect language extracts no Effects
- **WHEN** a rule/ability's text states no unconditional characteristic mutation the classifier
  recognizes (e.g. Templar Vows' own vow-selection text, which names a Target but grants no
  characteristic mutation directly)
- **THEN** zero Effects are extracted, and classification completes without error

### Requirement: Unrecognized Text Fails Closed
The system SHALL NOT raise an error and SHALL NOT guess when a rule/ability's text matches none of
its recognized Target or Effect patterns. Such text SHALL classify with Target `Self` and zero
Effects — the same result as text that explicitly states no broader effect — rather than a
distinguishable "unclassified" state.

#### Scenario: Arbitrary unrecognized text classifies safely
- **WHEN** a rule/ability's Text is arbitrary prose matching none of the classifier's recognized
  patterns
- **THEN** classification completes without error, producing Target `Self` and zero Effects

### Requirement: Corpus-Wide Classification Reporting
The system SHALL provide a way to run classification against a supplied set of real rule/ability
Name+Text pairs (such as the live BSData corpus) and report, for each, its classified Target and
Effects — distinguishing an entry that produced a non-default result (a recognized Target broader
than `Self`, or one or more Effects) from one that classified to the all-default result — so
real-corpus classification coverage can be inspected directly.

#### Scenario: Running against a real corpus reports classified and default-only entries separately
- **WHEN** classification is run against a supplied set of real rule/ability Name+Text pairs
- **THEN** the report distinguishes entries that produced a non-default Target or at least one
  Effect from entries that classified to the all-default result (`Self`, no Effects)
