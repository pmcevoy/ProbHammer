## MODIFIED Requirements

### Requirement: Versioned prompts and schema
Each classification run SHALL use a specific, checked-in prompt version, and every resulting
classification record SHALL record which prompt version produced it. A run MAY be given a version
label other than the checked-in default (for example `v3-draft`), naming which checked-in prompt it
reads. Records produced under that label SHALL record the label, so that a later run under the
final version treats them as stale and re-classifies them.

#### Scenario: Inspecting a classification's provenance
- **WHEN** a person inspects a classification record
- **THEN** they can determine the exact prompt version, target schema, and model that produced it

#### Scenario: A draft-labelled test run is re-classified by the final run
- **WHEN** a hash was classified under the label `v3-draft`, and a later run uses version `v3`
- **THEN** that later run selects the hash for classification again

#### Scenario: A draft label reads its checked-in prompt
- **WHEN** a run uses the label `v3-draft` for the `v3` prompt
- **THEN** it classifies with the `v3` prompt's wording and few-shot examples, and records
  `v3-draft` as each result's prompt version

## ADDED Requirements

### Requirement: Weapon skill changes are weapon-characteristic effects
The classification schema SHALL represent a change to a weapon's Ballistic Skill or Weapon Skill as
a weapon-characteristic effect naming `BS` or `WS`, as the text names it, with the text's own verb
and amount. Text naming both skills SHALL produce one effect per skill. Text naming only one skill
SHALL NOT be widened to the other.

#### Scenario: Both skills named on every weapon
- **WHEN** classifying "improve the Ballistic Skill and Weapon Skill characteristics of weapons
  equipped by this model by 1"
- **THEN** the result has two weapon-characteristic effects selecting every weapon, one improving
  `BS` by 1 and one improving `WS` by 1

#### Scenario: One skill named on unqualified weapons
- **WHEN** classifying "improve the Weapon Skill characteristic of weapons equipped by models in that
  unit by 1"
- **THEN** the result has one weapon-characteristic effect improving `WS` by 1, and none for `BS`

#### Scenario: A fixed skill value is a Set
- **WHEN** classifying "the bearer's ranged weapons have a Ballistic Skill characteristic of 3+"
- **THEN** the result has one weapon-characteristic effect setting `BS` to 3 for ranged weapons

#### Scenario: Ignoring modifiers is not a skill change
- **WHEN** classifying "you can ignore any or all modifiers to that attack's Ballistic Skill
  characteristic and to the Hit roll"
- **THEN** the result has no `BS` weapon-characteristic effect
