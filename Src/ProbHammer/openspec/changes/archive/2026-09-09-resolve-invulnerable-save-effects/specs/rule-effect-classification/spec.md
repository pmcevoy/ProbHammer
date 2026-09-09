## MODIFIED Requirements

### Requirement: Unconditional Characteristic Effect Extraction
The system SHALL extract zero or more Effects from a rule/ability's Text. Each Effect SHALL state an
unconditional mutation of exactly one named characteristic: either a `Statline` scalar (M/T/Sv/W/Ld/Oc)
by a fixed amount using one of three verbs matching the text's own rulebook vocabulary (`Improve`,
`Worsen`, or `Set`), or the invulnerable save (`InSv`) as a melee/ranged pair using the `Set` verb
only — the system SHALL NOT extract an `Improve`/`Worsen` Effect against `InSv`. A rule/ability whose
text states mutations to more than one characteristic SHALL produce one Effect per characteristic
mutated. The system SHALL NOT attempt to resolve an Effect's arithmetic sign or compute a final
value — it extracts only what the text states, in the text's own vocabulary.

An Effect SHALL only be extracted when its stating clause begins at the true start of its own
sentence (the start of the text, or immediately after a sentence-terminating period) — never when
embedded inside an earlier conditional preamble in the same sentence, or inside one item of a
"select one of the following" menu (whether bullet- or heading-separated).

An invulnerable-save grant restricted to one or both melee/ranged attack types (e.g. "...invulnerable
save against ranged attacks") SHALL be extracted as an `InSv` Effect stating the value for whichever
attack type(s) are named, with an attack type the text does not name for that grant treated as no
invulnerable save against that type. This extraction SHALL be scoped to the melee/ranged attack-type
axis only: a save restricted by any other qualifier (e.g. "...invulnerable save against Psychic
Attacks") SHALL NOT be extracted as an `InSv` Effect of any shape — such text SHALL classify exactly
as unrecognized text does under this requirement.

A characteristic-naming Effect (the "Add N to the X characteristic" phrasing) SHALL be recognized
whether the characteristic name is stated plainly ("the Wounds characteristic") or with an
intervening possessive noun ("the bearer's Wounds characteristic") — both name the same
characteristic. The system SHALL also recognize a shorthand "+N Characteristic" phrasing (e.g. "+1
OC") as an `Improve` Effect, in addition to the "Add N to the X characteristic" phrasing. The
recognized characteristic names SHALL include both a characteristic's full name ("Movement") and its
common abbreviation ("Move") where the corpus uses both.

#### Scenario: A Set-verb effect extracts with its stated value
- **WHEN** a rule/ability's text grants a specific value outright (e.g. Shield Dome: "The bearer has
  a 5+ invulnerable save.")
- **THEN** one Effect is extracted: `Set` the `InSv` characteristic to a uniform `5` (melee and ranged
  both `5`)

#### Scenario: An Improve-verb effect extracts with its stated amount
- **WHEN** a rule/ability's text adds a fixed amount to a named characteristic (e.g. Vexilla: "Add 1
  to the Objective Control characteristic...")
- **THEN** one Effect is extracted: `Improve` the `Oc` characteristic by `1`

#### Scenario: A shorthand "+N Characteristic" effect extracts with its stated amount
- **WHEN** a rule/ability's text states a shorthand increment (e.g. "+1 OC")
- **THEN** one Effect is extracted: `Improve` the `Oc` characteristic by `1`

#### Scenario: A possessive-phrased characteristic effect still extracts
- **WHEN** a rule/ability's text names the affected characteristic with an intervening possessive
  noun (e.g. "Add 2 to the bearer's Wounds characteristic.")
- **THEN** one Effect is extracted: `Improve` the `W` characteristic by `2`

#### Scenario: The abbreviated "Move" name is recognized as the Movement characteristic
- **WHEN** a rule/ability's text names the affected characteristic using "Move" rather than
  "Movement" (e.g. "Add 2 to the Move characteristic of models in the bearer's unit.")
- **THEN** one Effect is extracted targeting the `M` characteristic

#### Scenario: Text with no recognizable effect language extracts no Effects
- **WHEN** a rule/ability's text states no unconditional characteristic mutation the classifier
  recognizes (e.g. Templar Vows' own vow-selection text, which names a Target but grants no
  characteristic mutation directly)
- **THEN** zero Effects are extracted, and classification completes without error

#### Scenario: A save-granting clause embedded in a conditional preamble extracts no Effect
- **WHEN** a rule/ability's text states a would-be Effect clause preceded, within the same sentence,
  by a conditional preamble (e.g. "If it does, until the end of the phase, the bearer has a 2+
  invulnerable save.") or as one alternative in a "select one of the following" menu
- **THEN** zero Effects are extracted from that clause

#### Scenario: A one-sided attack-type-restricted invulnerable save extracts with the other side absent
- **WHEN** a rule/ability's text states an invulnerable save naming only one attack type (e.g.
  Ensorcelled Shield: "This model has a 4+ invulnerable save against ranged attacks.")
- **THEN** one Effect is extracted: `Set` the `InSv` characteristic with Ranged `4` and no invulnerable
  save against melee attacks

#### Scenario: A two-sided attack-type-restricted invulnerable save extracts both stated values
- **WHEN** a rule/ability's text states different invulnerable-save values for each attack type in the
  same clause (e.g. Veil of Medrengard: "The bearer has a 4+ invulnerable save against ranged attacks,
  and a 5+ invulnerable save against melee attacks.")
- **THEN** one Effect is extracted: `Set` the `InSv` characteristic with Ranged `4` and Melee `5`

#### Scenario: An attack-type-restricted invulnerable save extracts no Effect
- **WHEN** a rule/ability's text restricts an invulnerable save by a qualifier other than melee/ranged
  attack type (e.g. "...invulnerable save against Psychic Attacks") — outside the melee/ranged axis
  this requirement extracts
- **THEN** zero Effects are extracted from that clause

### Requirement: Caveated Signal For Text Stating More Than Extracted
When a classification has extracted at least one Effect, the system SHALL determine whether the
rule/ability's text states additional content beyond what its Target and Effects extraction
captured, and record this as a caveated signal on the classification. The system SHALL NOT attempt
to classify what any additional content is — only that it exists.

The caveated determination SHALL be made by locating the end position of the last regex match that
contributed to either the classified Target or the extracted Effects — including a match that
contributed an invulnerable-save Effect — and examining the text remaining after that position with
surrounding whitespace and a single trailing period trimmed: a non-empty remainder SHALL mark the
classification caveated; an empty remainder SHALL NOT.

This determination SHALL only be made for a classification with at least one extracted Effect. A
classification with zero Effects (whether its Target is `Self` or broader) SHALL NOT be marked
caveated by this requirement.

#### Scenario: Extracted content with real text remaining is marked caveated
- **WHEN** a rule/ability's text is classified with at least one Effect, and text beyond the last
  contributing match's end position remains non-empty after trimming whitespace and a trailing
  period (e.g. an invulnerable-save grant whose sentence continues with an unrelated keyword-removal
  clause)
- **THEN** the classification is marked caveated

#### Scenario: Extracted content with nothing remaining is not marked caveated
- **WHEN** a rule/ability's text is classified with at least one Effect, and no text remains after
  the last contributing match's end position once whitespace and a trailing period are trimmed
- **THEN** the classification is not marked caveated

#### Scenario: A leading restriction before the matched clause does not trigger the signal
- **WHEN** a rule/ability's text states an eligibility restriction before the clause that produced
  the extracted Effect (e.g. "Sanctuary model only. The bearer has a 4+ invulnerable save."), with
  nothing remaining after the matched clause
- **THEN** the classification is not marked caveated

#### Scenario: A classification with zero Effects is never marked caveated
- **WHEN** a rule/ability's text produces zero extracted Effects, regardless of its classified Target
- **THEN** the classification is not marked caveated

#### Scenario: A restricted invulnerable-save grant with remaining content is marked caveated
- **WHEN** a rule/ability's text states an attack-type-restricted invulnerable-save Effect and the
  same sentence continues with unrelated content (e.g. Ensorcelled Shield: "...against ranged
  attacks, and the Feel No Pain 6+ ability.")
- **THEN** the classification is marked caveated

### Requirement: Corpus-Wide Classification Reporting
The system SHALL provide a way to run classification against a supplied set of real rule/ability
Name+Text pairs (such as the live BSData corpus) and report, for each, its classified Target and
Effects — distinguishing an entry that produced a non-default result (a recognized Target broader
than `Self`, or one or more Effects) from one that classified to the all-default result — so
real-corpus classification coverage can be inspected directly.

For a text that already has a corresponding entry in the rule-effect-classification-baseline
capability's baseline, the report SHALL instead apply that capability's new/drift/unchanged
distinction in place of its normal non-default/default-only listing, so an already-verified,
unchanged result is never reprinted in full while a genuine change to it is always surfaced. A text
with no baseline entry is unaffected and continues to appear under the report's normal listing.

Within the default-only listing, the system SHALL distinguish a text that could not plausibly state
an invulnerable-save effect (no occurrence of invulnerable-save language at all) from a text that
could plausibly state one but matched no recognized invulnerable-save pattern — so review effort
concentrates on the latter, smaller set rather than the whole default-only listing.

#### Scenario: Running against a real corpus reports classified and default-only entries separately
- **WHEN** classification is run against a supplied set of real rule/ability Name+Text pairs
- **THEN** the report distinguishes entries that produced a non-default Target or at least one
  Effect from entries that classified to the all-default result (`Self`, no Effects)

#### Scenario: A baselined text is reported via the baseline's diff, not the normal listing
- **WHEN** a corpus text being classified has a corresponding entry in the verified-classification
  baseline
- **THEN** the report applies the baseline's new/drift/unchanged distinction for that text instead
  of listing it under the normal Effect/Target-only/default-only sections

#### Scenario: A text with no baseline entry is unaffected
- **WHEN** a corpus text being classified has no corresponding baseline entry
- **THEN** the report lists it under its normal Effect/Target-only/default-only section exactly as
  it did before the baseline existed

#### Scenario: A default-only text with no invulnerable-save language is excluded from review
- **WHEN** a rule/ability's text classifies to the all-default result and contains no occurrence of
  invulnerable-save language
- **THEN** the report excludes it from the reviewable default-only listing entirely

#### Scenario: A default-only text with unmatched invulnerable-save language is retained for review
- **WHEN** a rule/ability's text classifies to the all-default result but contains invulnerable-save
  language that matched no recognized pattern
- **THEN** the report retains it in a distinct, reviewable default-only listing
