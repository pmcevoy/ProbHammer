# statline-flag-rules Specification

## Purpose

Recognizes a small, closed set of previously-catalogued ability texts that grant or change a
resolved unit's Statline characteristic (an invulnerable save, Objective Control, or similar), and
derives a flagged value for display wherever that specific ability is currently present on that
specific resolved unit — without ever removing the source ability from its normal place in the
unit's Abilities or Enhancements.
## Requirements
### Requirement: Closed-Vocabulary Ability-to-Characteristic Rules
The system SHALL recognize a rule/ability by looking up its text in the ability-classification
catalogue (per `ability-classification-catalogue`'s Catalogue Lookup By Normalized-Text Content
Hash), independent of the ability's Name, since multiple differently-named abilities can share
identical rules text. The system SHALL NOT derive a flagged value from an ability with no catalogue
record, nor from any of a record's conditional effects (per `ability-classification-catalogue`'s
Unconditional Effect Rule). When a record's matched ability is present on a specific resolved unit
(per Mutation Liveness Follows Ability Presence below) and that record's own classified target scope
is one this capability applies to (per Target-Scoped Application below), the system SHALL derive a
flagged value for the Statline characteristic(s) named by that record's unconditional Statline scalar
and invulnerable-save effects, replacing or adjusting the Datasheet's own base value for display on
that unit, and SHALL record a reference back to the matched ability for each flagged value. An
invulnerable-save effect SHALL be combined with the existing value per side (melee, ranged), keeping
the better save, so that a side the effect does not name, or names with a worse save, keeps its
existing value; an effect improving neither side SHALL leave the value unflagged. Effects of any other
kind SHALL NOT affect the Statline.

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

#### Scenario: An unmatched ability produces no flagged value
- **WHEN** a resolved unit carries an ability whose text has no catalogue record
- **THEN** no Statline characteristic is flagged on that unit's behalf by this capability

#### Scenario: A conditional effect produces no flagged value
- **WHEN** a resolved unit carries an ability whose catalogue record classifies an Objective Control
  improvement whose residual-condition bucket is not none, or whose record carries a usage limit or
  turn ownership
- **THEN** that unit's Objective Control is not flagged by that effect, and the ability still renders
  in the unit's normal ability listing

### Requirement: Target-Scoped Application
A matched catalogue record's own classified target scope SHALL determine which of a resolved unit's
Statline entries the flagged value applies to: a target scoped to the ability's own bearer SHALL
apply only to that bearer's own row(s) — a specific model-line when the matched ability is
model-line-sourced, the whole owning component when it is component-wide (the existing Bearer
scope); a target scoped to the bearer's whole attached unit SHALL apply to every row of the whole
resolved unit regardless of which component granted the matched ability (the existing WholeUnit
scope). A matched record whose own classified target is a set of keywords, or is unconditionally
roster-wide with no bearer/unit qualifier at all, SHALL NOT produce a flagged value — the same
outcome as an unmatched ability — since no roster-wide keyword-predicate evaluation exists in this
capability, with one exception: an ability whose Origin is Detachment Rule (per
`army-roster-enrichment`'s Detachment Rule Keyword Target Resolution) has already had its own
keyword target evaluated against the resolved roster before it was ever attached as a present
ability on this unit, so it SHALL be treated as WholeUnit-scoped for the purposes of this
requirement regardless of its own record's classified target.

#### Scenario: A keyword-scoped match produces no flagged value
- **WHEN** a resolved unit carries an ability whose catalogue record's own classified target is a set
  of keywords rather than the bearer or its unit, and that ability's Origin is not Detachment Rule
- **THEN** no Statline characteristic is flagged on that unit's behalf by this capability, the same
  outcome as if no record had matched at all

#### Scenario: An unconditionally roster-wide match produces no flagged value
- **WHEN** a resolved unit carries an ability whose catalogue record's own classified target names no
  bearer, unit, or keyword qualifier at all
- **THEN** no Statline characteristic is flagged on that unit's behalf by this capability

#### Scenario: A Detachment-Rule-origin ability applies as WholeUnit-scoped despite its own keyword-classified target
- **WHEN** a resolved unit carries a present ability whose Origin is Detachment Rule and whose
  catalogue record's own classified target is a set of keywords
- **THEN** the flagged value applies to every row of the whole resolved unit, the same treatment as
  an ordinary WholeUnit-scoped match, not the "no flagged value" outcome that keyword target would
  otherwise produce

### Requirement: Source Ability Always Remains Visible
A rule's match SHALL NOT remove, hide, or otherwise suppress the source ability from the unit's
normal Abilities or Enhancements listing. The source ability SHALL continue to render exactly as it
would if no rule matched it, alongside the newly-derived flagged value.

#### Scenario: A matched ability still appears in the normal ability listing
- **WHEN** a resolved unit carries an ability that matches a known rule and produces a flagged
  Statline value
- **THEN** that ability still appears, unchanged, in the unit's normal Abilities or Enhancements
  listing

### Requirement: Mutation Liveness Follows Ability Presence
A flagged value SHALL be re-derived on every render from the unit's currently-present abilities, and
SHALL apply only while the specific bearer that grants the matched ability is itself still present —
following the same liveness rule that already governs whether that ability itself renders: a
component-wide source (a Datasheet-level ability or a resolved Enhancement) remains live only while
its owning component is present, and a model-line-sourced source remains live only while that
specific model-line's remaining count is above zero. The system SHALL NOT cache a flagged value
independent of this recomputation, and marking the bearer of a matched ability as a casualty SHALL
cause the flagged value to no longer apply on the unit's next render, reverting the affected
characteristic to the Datasheet's own base value.

#### Scenario: Marking the bearer a casualty removes the flagged value
- **WHEN** a resolved unit's Objective Control is flagged via a matched ability granted by one
  specific model-line, and the player marks every model in that model-line as a casualty
- **THEN** the unit's Objective Control reverts to the Datasheet's own base value on the next render,
  no longer flagged

#### Scenario: A surviving bearer keeps the flagged value applied
- **WHEN** a resolved unit's Objective Control is flagged via a matched ability granted by one
  specific model-line, and a different, unrelated model-line in the same unit is marked as a
  casualty
- **THEN** the flagged value remains applied, unchanged, since the ability's actual bearer is still
  present

### Requirement: Conditional Statline Effects Are Recorded Without Being Applied
When a present ability's catalogue record contains a conditional Statline scalar or invulnerable-save
effect, the system SHALL record it on every Statline entry an unconditional effect of that record
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
- **WHEN** a present ability whose Origin is not Detachment Rule carries a conditional Statline
  effect and its catalogue record's classified target is a set of keywords
- **THEN** no not-applied effect is recorded for it, the same outcome as for an unconditional effect
  with that target

#### Scenario: An activated Statline effect is applied, not recorded
- **WHEN** the player activates Martial Honour on Helbrecht's Crusader Squad
- **THEN** the bearer's OC is improved by 5 and no not-applied effect for Martial Honour is recorded

