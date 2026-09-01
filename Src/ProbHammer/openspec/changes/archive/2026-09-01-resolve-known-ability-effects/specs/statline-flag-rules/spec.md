## Purpose

Recognizes a small, closed set of previously-catalogued ability texts that grant or change a
resolved unit's Statline characteristic (an invulnerable save, Objective Control, or similar), and
derives a flagged value for display wherever that specific ability is currently present on that
specific resolved unit — without ever removing the source ability from its normal place in the
unit's Abilities or Enhancements.

## ADDED Requirements

### Requirement: Closed-Vocabulary Ability-to-Characteristic Rules
The system SHALL recognize a fixed, closed set of rules, each matching one specific ability by its
exact Name and exact whole-string Text. A rule SHALL NOT match on a partial or fuzzy comparison of
either field. When a rule's matched ability is present on a specific resolved unit (per Mutation
Liveness Follows Ability Presence below), the system SHALL derive a flagged value for the Statline
characteristic that rule names, replacing or adjusting the Datasheet's own base value for display on
that unit, and SHALL record a reference back to the matched ability for that flagged value.

#### Scenario: A matched ability grants a new invulnerable save value
- **WHEN** a resolved unit carries an ability whose Name and Text exactly match the rule for
  "Shield Dome" ("The bearer has a 5+ invulnerable save.")
- **THEN** that unit's displayed invulnerable save is flagged with a value of 5+, referencing Shield
  Dome as its source

#### Scenario: A matched ability adjusts an existing characteristic value
- **WHEN** a resolved unit carries an ability whose Name and Text exactly match the rule for
  "Vexilla" ("Add 1 to the Objective Control characteristic of models in the bearer's unit.")
- **THEN** every model in that unit's Objective Control is flagged with the Datasheet's base value
  plus 1, referencing Vexilla as its source

#### Scenario: An unmatched ability produces no flagged value
- **WHEN** a resolved unit carries an ability whose Name or Text does not exactly match any known
  rule
- **THEN** no Statline characteristic is flagged on that unit's behalf by this capability

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
