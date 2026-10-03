## MODIFIED Requirements

### Requirement: A Caveated Value Is Given One Resolution Attempt During Roster Aggregation
For every resolved unit's Statline whose invulnerable save is caveated, the system SHALL attempt to
resolve it exactly once per aggregate rebuild by looking up its own single contributing ability's
text in the ability-classification catalogue (per `ability-classification-catalogue`'s Catalogue
Lookup By Normalized-Text Content Hash). A match SHALL resolve the value only when that ability's
record contains an unconditional invulnerable-save effect (per `ability-classification-catalogue`'s
Unconditional Effect Rule); a successful resolution SHALL replace the caveated result with a resolved
value carrying that ability as its source, preserving the true pre-caveat original value. The
effect defines the footnoted save: a side it names takes its value; a side it leaves without a save
SHALL keep the characteristic's plain value when the characteristic states one for that side (a split
form such as "4+* / 5+"), and SHALL have no invulnerable save when the whole characteristic is the
footnoted value (a bare form such as "4+*"). No record, or a
record with no unconditional invulnerable-save effect, SHALL leave the result caveated, unchanged
from today's existing fallback behavior.

#### Scenario: A caveated value resolves via a baseline match
- **WHEN** a resolved unit's Statline carries a caveated invulnerable save, and its single
  contributing ability's catalogue record contains an unconditional invulnerable-save effect
- **THEN** the invulnerable save is displayed as resolved, using that effect's value, still
  referencing that ability as its source

#### Scenario: A bare footnote resolves to the named side only
- **WHEN** a Statline's invulnerable save is "4+*" and its contributing ability's catalogue record
  classifies a 4+ invulnerable save against melee attacks only
- **THEN** the invulnerable save is displayed as resolved: 4+ melee and no ranged invulnerable save

#### Scenario: A split footnote keeps the plain value on the other side
- **WHEN** a Statline's invulnerable save is "4+* / 5+" and its contributing ability's catalogue
  record classifies a 4+ invulnerable save against melee attacks only
- **THEN** the invulnerable save is displayed as resolved: 4+ melee and 5+ ranged

#### Scenario: A caveated value with no baseline match stays caveated
- **WHEN** a resolved unit's Statline carries a caveated invulnerable save, and its contributing
  ability's text has no catalogue record
- **THEN** the invulnerable save remains caveated, showing the Datasheet's own base value, exactly
  as before this resolution attempt was made

#### Scenario: A conditional invulnerable-save effect leaves the value caveated
- **WHEN** a resolved unit's Statline carries a caveated invulnerable save, and its contributing
  ability's catalogue record's only invulnerable-save effect is conditional
- **THEN** the invulnerable save remains caveated, showing the Datasheet's own base value

#### Scenario: A resolved value is never re-caveated by a later pass
- **WHEN** a resolved unit's Statline invulnerable save has already been resolved by this
  requirement or by any other characteristic-resolution mechanism
- **THEN** no later step in the same aggregate rebuild replaces or downgrades that resolved value
