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
as unrecognized text does under this requirement. This restricted-grant recognition SHALL apply
whether the subject of the grant is singular ("This model has a {N}+ invulnerable save against
{ranged|melee} attacks.") or plural ("Models in this unit have a {N}+ invulnerable save against
{ranged|melee} attacks.") — the same claim stated from either a single bearer's or a whole unit's
own perspective.

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

#### Scenario: A plural-subject restricted invulnerable save extracts the same as its singular form
- **WHEN** a rule/ability's text states an attack-type-restricted invulnerable save from a whole
  unit's own perspective (e.g. "Models in this unit have a 4+ invulnerable save against melee
  attacks.") rather than a single bearer's ("This model has...")
- **THEN** one Effect is extracted: `Set` the `InSv` characteristic with Melee `4` and no invulnerable
  save against ranged attacks — the same extraction the singular form of the identical claim would
  produce
