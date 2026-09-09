## MODIFIED Requirements

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

## ADDED Requirements

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
exists in this capability.

#### Scenario: A keyword-scoped match produces no flagged value
- **WHEN** a resolved unit carries an ability whose normalized Text matches a baseline entry whose
  own classified target is scoped to a named keyword rather than to the bearer or its unit
- **THEN** no Statline characteristic is flagged on that unit's behalf by this capability, the same
  outcome as if no baseline entry had matched at all

#### Scenario: An unconditionally roster-wide match produces no flagged value
- **WHEN** a resolved unit carries an ability whose normalized Text matches a baseline entry whose
  own classified target names no bearer, unit, or keyword qualifier at all
- **THEN** no Statline characteristic is flagged on that unit's behalf by this capability
