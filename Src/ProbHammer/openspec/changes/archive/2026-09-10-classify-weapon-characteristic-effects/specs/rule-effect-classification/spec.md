## ADDED Requirements

### Requirement: Weapon-Characteristic Effect Extraction
The system SHALL extract, alongside the existing Statline-scalar and invulnerable-save Effect
shapes, a weapon-characteristic Effect from text stating an unconditional mutation of exactly one
named weapon characteristic (Strength, Attacks, Armour Penetration, or Damage) scoped to a weapon
selector — which weapons owned by the ability's bearer the mutation applies to: every weapon
("all weapons"), every weapon of one class ("melee weapons"/"ranged weapons"), or one specifically
named weapon. A weapon-characteristic Effect SHALL carry that selector, the mutated characteristic,
the stated verb (`Improve`, `Worsen`, or `Set`), and the stated amount — the same closed vocabulary
`ScalarCharacteristicEffect` already uses for a Statline scalar, applied to a weapon characteristic
instead. This extraction is subject to the same sentence-start anchoring the existing Statline/
invulnerable-save Effect patterns already require (see "Unconditional Characteristic Effect
Extraction").

A rule/ability's text stating one shared verb and one shared amount against a comma/and-joined list
of two or more weapon characteristics (e.g. "improve the Strength and Attacks characteristics of
melee weapons equipped by this model by 3") SHALL extract one weapon-characteristic Effect per
named characteristic in that list, every extracted Effect sharing an identical weapon selector,
verb, and amount — never a single Effect record holding more than one characteristic. A rule/
ability's text stating two independent clauses, each with its own verb, joined by "and" and
referring back to the same weapons via anaphora (e.g. "...add 1 to the Strength characteristic of
melee weapons equipped by this model and improve the Armour Penetration characteristic of those
weapons by 1") SHALL extract one weapon-characteristic Effect per clause, each carrying its own
clause's verb and amount, both sharing the one weapon selector the first clause names.

#### Scenario: A single weapon-characteristic mutation extracts
- **WHEN** a rule/ability's text states a mutation to exactly one named weapon characteristic for a
  weapon class (e.g. "Add 1 to the Strength characteristic of melee weapons equipped by this
  model.")
- **THEN** one weapon-characteristic Effect is extracted: `Improve` the `S` characteristic by `1`,
  scoped to a weapon-class selector for melee weapons

#### Scenario: A shared-amount coordinate characteristic list splits into atomic Effects
- **WHEN** a rule/ability's text states one verb and one shared amount against a comma/and-joined
  list of weapon characteristics (e.g. Zealot: "improve the Strength and Attacks characteristics of
  melee weapons equipped by this model by 3")
- **THEN** two weapon-characteristic Effects are extracted — `Improve` the `S` characteristic by
  `3` and `Improve` the `A` characteristic by `3` — both sharing the same melee weapon-class
  selector

#### Scenario: A four-way coordinate characteristic list splits into four atomic Effects
- **WHEN** a rule/ability's text states one verb and one shared amount against a four-item
  characteristic list (e.g. Chance for Glory: "improve the Strength, Attacks, Armour Penetration
  and Damage characteristics of melee weapons equipped by this model by 1")
- **THEN** four weapon-characteristic Effects are extracted — `Improve` `S`, `A`, `AP`, and `D`
  each by `1` — all sharing the same melee weapon-class selector

#### Scenario: A two-verb anaphora-joined pair extracts as two independently-verbed Effects
- **WHEN** a rule/ability's text states two full clauses with independent verbs, joined by "and",
  the second referring back to the first clause's weapons via anaphora (e.g. Brutal Raider: "add 1
  to the Strength characteristic of melee weapons equipped by this model and improve the Armour
  Penetration characteristic of those weapons by 1")
- **THEN** two weapon-characteristic Effects are extracted — `Improve` the `S` characteristic by
  `1` and `Improve` the `AP` characteristic by `1` — both sharing the same melee weapon-class
  selector the first clause names

#### Scenario: No differing amounts across one coordinate characteristic list
- **WHEN** a rule/ability's text states a coordinate characteristic list under one shared verb and
  amount
- **THEN** every Effect extracted from that list carries the identical amount — the system SHALL
  NOT extract differing amounts per characteristic from a single shared-amount clause
