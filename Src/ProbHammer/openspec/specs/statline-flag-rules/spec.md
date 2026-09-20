# statline-flag-rules Specification

## Purpose

Recognizes a small, closed set of previously-catalogued ability texts that grant or change a
resolved unit's Statline characteristic (an invulnerable save, Objective Control, or similar), and
derives a flagged value for display wherever that specific ability is currently present on that
specific resolved unit — without ever removing the source ability from its normal place in the
unit's Abilities or Enhancements.

## Requirements

### Requirement: Closed-Vocabulary Ability-to-Characteristic Rules
The system SHALL recognize a closed, human-verified vocabulary of rule/ability texts recorded in a
checked-in baseline of classified characteristic Effects, matching an ability by its own normalized
Text alone — independent of the ability's Name, since multiple differently-named abilities can share
identical rules text. The system SHALL NOT derive a flagged value from an ability whose normalized
Text has no corresponding baseline entry. When a baseline entry's matched ability is present on a
specific resolved unit (per Mutation Liveness Follows Ability Presence below) and that entry's own
classified target scope is one this capability applies to (per Target-Scoped Application below), the
system SHALL derive a flagged value for the Statline characteristic(s) that entry's classified
Effects name, replacing or adjusting the Datasheet's own base value for display on that unit, and
SHALL record a reference back to the matched ability for each flagged value.

#### Scenario: A matched ability grants a new invulnerable save value
- **WHEN** a resolved unit carries an ability whose normalized Text matches the baseline entry
  recorded for "Shield Dome" ("The bearer has a 5+ invulnerable save.")
- **THEN** that unit's displayed invulnerable save is flagged with a value of 5+, referencing that
  ability as its source

#### Scenario: A matched ability adjusts an existing characteristic value
- **WHEN** a resolved unit carries an ability whose normalized Text matches the baseline entry
  recorded for "Vexilla" ("Add 1 to the Objective Control characteristic of models in the bearer's
  unit.")
- **THEN** every model in that unit's Objective Control is flagged with the Datasheet's base value
  plus 1, referencing that ability as its source

#### Scenario: An unmatched ability produces no flagged value
- **WHEN** a resolved unit carries an ability whose normalized Text does not match any baseline
  entry
- **THEN** no Statline characteristic is flagged on that unit's behalf by this capability

### Requirement: Target-Scoped Application
A matched baseline entry's own classified target scope SHALL determine which of a resolved unit's
Statline entries the flagged value applies to: a target scoped to the ability's own bearer SHALL
apply only to that bearer's own row(s) — a specific model-line when the matched ability is
model-line-sourced, the whole owning component when it is component-wide (the existing Bearer
scope); a target scoped to the bearer's whole attached unit SHALL apply to every row of the whole
resolved unit regardless of which component granted the matched ability (the existing WholeUnit
scope). A matched entry whose own classified target is scoped to a named keyword, or is
unconditionally roster-wide with no bearer/unit qualifier at all, SHALL NOT produce a flagged value
— the same outcome as an unmatched ability — since no roster-wide keyword-predicate evaluation
exists in this capability, with one exception: an ability whose Origin is Detachment Rule (per
`army-roster-enrichment`'s Detachment Rule Keyword Target Resolution) has already had its own
keyword-scoped target evaluated against the resolved roster before it was ever attached as a
present ability on this unit, so it SHALL be treated as WholeUnit-scoped for the purposes of this
requirement regardless of its own baseline entry's classified target.

#### Scenario: A keyword-scoped match produces no flagged value
- **WHEN** a resolved unit carries an ability whose normalized Text matches a baseline entry whose
  own classified target is scoped to a named keyword rather than to the bearer or its unit, and
  that ability's Origin is not Detachment Rule
- **THEN** no Statline characteristic is flagged on that unit's behalf by this capability, the same
  outcome as if no baseline entry had matched at all

#### Scenario: An unconditionally roster-wide match produces no flagged value
- **WHEN** a resolved unit carries an ability whose normalized Text matches a baseline entry whose
  own classified target names no bearer, unit, or keyword qualifier at all
- **THEN** no Statline characteristic is flagged on that unit's behalf by this capability

#### Scenario: A Detachment-Rule-origin ability applies as WholeUnit-scoped despite its own keyword-classified target
- **WHEN** a resolved unit carries a present ability whose Origin is Detachment Rule and whose
  matched baseline entry's own classified target is a named keyword
- **THEN** the flagged value applies to every row of the whole resolved unit, the same treatment as
  an ordinary WholeUnit-scoped match, not the "no flagged value" outcome that keyword-scoped target
  would otherwise produce

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
