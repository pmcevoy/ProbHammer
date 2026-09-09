## MODIFIED Requirements

### Requirement: Closed-Vocabulary Ability-to-Characteristic Rules
The system SHALL recognize a closed, human-verified vocabulary of rule/ability texts recorded in a
checked-in baseline of classified characteristic Effects, matching an ability by its own normalized
Text alone — independent of the ability's Name, since multiple differently-named abilities can share
identical rules text. The system SHALL NOT derive a flagged value from an ability whose normalized
Text has no corresponding baseline entry. This matching SHALL apply uniformly to both of a resolved
unit's characteristic-affecting inputs: every ability present on the unit (per Mutation Liveness
Follows Ability Presence below), and every characteristic-specific ability association a parser
recorded because it could not determine that characteristic's value from raw catalogue text alone
(per Deferred Characteristic Associations Resolve At Their Recorded Scope below). When a baseline
entry's matched ability is present or associated, and that entry's own classified target scope is one
this capability applies to (per Target-Scoped Application below), the system SHALL derive a flagged
value for the Statline characteristic(s) that entry's classified Effects name, replacing or adjusting
the Datasheet's own base value for display on that unit, and SHALL record a reference back to the
matched ability for each flagged value.

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

### Requirement: Deferred Characteristic Associations Resolve At Their Recorded Scope
When a parser has recorded that a specific characteristic's value could not be determined from raw
catalogue text alone, and has associated that characteristic with a specific ability at a specific
attachment scope (a single named Statline, never more broadly than that), the system SHALL attempt to
resolve that association the same way it resolves a present ability: by matching the associated
ability's normalized Text against the baseline. A successful match SHALL flag the associated
characteristic at exactly the association's own recorded scope, using the matched entry's classified
Effects. An unsuccessful match SHALL leave the characteristic caveated, showing its Datasheet's own
base value with the associated ability recorded as the reason — never silently producing an
unexplained, unflagged value and never dropping the association without a trace. This resolution SHALL
apply the same Target-Scoped Application semantics as an ordinary present-ability match, treating the
association's own recorded scope as that ability's bearer.

#### Scenario: A deferred association resolves via a baseline match
- **WHEN** a parser has recorded that a specific Statline's invulnerable save could not be determined
  from its raw catalogue text alone, and has associated it with a linked ability whose normalized Text
  matches a baseline entry
- **THEN** that specific Statline's invulnerable save is flagged with the matched entry's resolved
  value, referencing the associated ability as its source

#### Scenario: A deferred association with no baseline match stays caveated
- **WHEN** a parser has recorded a characteristic-specific ability association, and that ability's
  normalized Text matches no baseline entry
- **THEN** the associated characteristic remains caveated, showing the Datasheet's own base value,
  with the associated ability recorded as its source

#### Scenario: A deferred association never widens beyond its recorded scope
- **WHEN** a parser has recorded a characteristic-specific ability association scoped to one specific
  named Statline within a component that declares more than one named Statline
- **THEN** resolving that association affects only that one named Statline's own characteristic —
  every other Statline the same component declares is unaffected, regardless of whether the
  associated ability is also independently present elsewhere on that component

#### Scenario: The same association is never resolved twice
- **WHEN** a characteristic-specific ability association has already been resolved (or left caveated)
  by this requirement
- **THEN** the same ability, if it is also separately present on the unit through the ordinary
  ability-presence path, SHALL NOT flag that already-settled characteristic a second time
