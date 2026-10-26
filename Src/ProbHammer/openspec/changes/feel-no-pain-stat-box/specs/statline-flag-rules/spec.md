## MODIFIED Requirements

### Requirement: Closed-Vocabulary Ability-to-Characteristic Rules
The system SHALL recognize a rule/ability by looking up its text in the ability-classification
catalogue (per `ability-classification-catalogue`'s Catalogue Lookup By Normalized-Text Content
Hash), independent of the ability's Name, since multiple differently-named abilities can share
identical rules text. The system SHALL NOT derive a flagged value from an ability with no catalogue
record, nor from any of a record's conditional effects (per `ability-classification-catalogue`'s
Unconditional Effect Rule). When a record's matched ability is present on a specific resolved unit
(per Mutation Liveness Follows Ability Presence below) and that record's own classified target scope
is one this capability applies to (per Target-Scoped Application below), the system SHALL derive a
flagged value for the Statline characteristic(s) named by that record's unconditional Statline scalar,
invulnerable-save and Feel No Pain effects, replacing or adjusting the Datasheet's own base value for
display on that unit, and SHALL record a reference back to the matched ability for each flagged value.
An invulnerable-save effect SHALL be combined with the existing value per side (melee, ranged), keeping
the better save, so that a side the effect does not name, or names with a worse save, keeps its
existing value; an effect improving neither side SHALL leave the value unflagged. A Feel No Pain effect
SHALL be combined per `feel-no-pain`'s A Feel No Pain Grant Keeps The Better Threshold. A Feel No Pain
effect whose record names a qualifier (a damage source or a subset of models it applies to) SHALL NOT
change the value. Effects of any other kind SHALL NOT affect the Statline.

#### Scenario: A matched ability grants a new invulnerable save value
- **WHEN** a resolved unit carries an ability whose text has a catalogue record classifying it ("The
  bearer has a 5+ invulnerable save.") as an unconditional invulnerable-save effect of 5+
- **THEN** that unit's displayed invulnerable save is flagged with a value of 5+, referencing that
  ability as its source

#### Scenario: A one-sided invulnerable-save grant keeps the other side's existing save
- **WHEN** a resolved unit with a 5+ invulnerable save carries an ability whose catalogue record
  classifies it as an unconditional 4+ invulnerable save against ranged attacks only
- **THEN** that unit's displayed invulnerable save is flagged as 5+ melee and 4+ ranged, referencing
  that ability as its source

#### Scenario: A worse invulnerable-save grant leaves the value unflagged
- **WHEN** a resolved unit with a 4+ invulnerable save carries an ability whose catalogue record
  classifies it as an unconditional 5+ invulnerable save
- **THEN** that unit's invulnerable save stays 4+ and is not flagged on that ability's behalf

#### Scenario: A matched ability adjusts an existing characteristic value
- **WHEN** a resolved unit carries an ability whose text has a catalogue record classifying it ("Add
  1 to the Objective Control characteristic of models in the bearer's unit.") as an unconditional
  Objective Control improvement of 1, targeting the bearer's unit
- **THEN** every model in that unit's Objective Control is flagged with the Datasheet's base value
  plus 1, referencing that ability as its source

#### Scenario: A matched ability grants Feel No Pain
- **WHEN** a unit with no Feel No Pain carries an ability whose catalogue record classifies it as an
  unconditional Feel No Pain 5+ with no qualifier, targeting the bearer's unit
- **THEN** every entry of that unit has Feel No Pain 5+, referencing that ability as its source

#### Scenario: A qualified Feel No Pain effect leaves the value unchanged
- **WHEN** a unit carries an ability whose catalogue record classifies it as an unconditional Feel No
  Pain 5+ against mortal wounds
- **THEN** that unit's Feel No Pain value is unchanged and not flagged by that effect

#### Scenario: An unmatched ability produces no flagged value
- **WHEN** a resolved unit carries an ability whose text has no catalogue record
- **THEN** no Statline characteristic is flagged on that unit's behalf by this capability

#### Scenario: A conditional effect produces no flagged value
- **WHEN** a resolved unit carries an ability whose catalogue record classifies an Objective Control
  improvement whose residual-condition bucket is not none, or whose record carries a usage limit or
  turn ownership
- **THEN** that unit's Objective Control is not flagged by that effect, and the ability still renders
  in the unit's normal ability listing

### Requirement: Conditional Statline Effects Are Recorded Without Being Applied
When a present ability's catalogue record contains a conditional Statline scalar, invulnerable-save or
Feel No Pain effect, the system SHALL record it on every Statline entry an unconditional effect of that record
would reach (per Target-Scoped Application), naming the source ability, the characteristic, the
effect's value and its condition, and SHALL NOT change that entry's value. An effect the player has
activated, or one in an unselected option of a choice group with a selection, SHALL NOT be recorded
(per `condition-activation`).

#### Scenario: A conditional Objective Control effect is recorded, not applied
- **WHEN** Helbrecht's Crusader Squad carries Martial Honour ("the first time a model in this
  model's unit makes a melee attack that destroys one or more enemy units ... add 5 to this model's
  Objective Control"), classified as a conditional OC improvement of 5
- **THEN** the bearer's Statline entry records a not-applied OC +5 effect naming Martial Honour and
  its condition, and its OC value is unchanged

#### Scenario: A conditional invulnerable save is recorded on a unit without one
- **WHEN** a unit with no invulnerable save carries Waaagh!, classified as a 5+ invulnerable save
  while the unit is riled up
- **THEN** each of that unit's Statline entries records a not-applied 5+ invulnerable-save effect
  naming Waaagh! and that condition, and its invulnerable save stays absent

#### Scenario: A conditional effect follows bearer liveness
- **WHEN** the bearer of an ability with a recorded conditional Statline effect is marked a casualty
- **THEN** the effect is no longer recorded on the next render, the same liveness rule an applied
  effect follows

#### Scenario: A keyword-targeted conditional effect is not recorded
- **WHEN** a present ability whose Origin is neither Detachment Rule nor Army Rule carries a
  conditional Statline effect and its catalogue record's classified target is a set of keywords
- **THEN** no not-applied effect is recorded for it, the same outcome as for an unconditional effect
  with that target

#### Scenario: An activated Statline effect is applied, not recorded
- **WHEN** the player activates Martial Honour on Helbrecht's Crusader Squad
- **THEN** the bearer's OC is improved by 5 and no not-applied effect for Martial Honour is recorded

#### Scenario: A once-per-battle Feel No Pain is recorded, not applied
- **WHEN** a Custodian Wardens unit carries Living Fortress ("Once per battle, at the start of any
  phase ... models in this unit have the Feel No Pain 4+ ability"), classified as Feel No Pain 4+
  with the usage limit once per battle and no qualifier
- **THEN** each of that unit's Statline entries records a not-applied Feel No Pain 4+ effect naming
  Living Fortress, and its Feel No Pain stays absent

#### Scenario: An activated Feel No Pain is applied
- **WHEN** the player activates Living Fortress on a Custodian Wardens unit
- **THEN** each of that unit's Statline entries has Feel No Pain 4+ referencing Living Fortress, and
  no not-applied effect for Living Fortress is recorded
