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
