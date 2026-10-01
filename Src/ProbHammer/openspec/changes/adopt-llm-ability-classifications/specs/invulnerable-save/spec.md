## MODIFIED Requirements

### Requirement: A Caveated Value Is Given One Resolution Attempt During Roster Aggregation
For every resolved unit's Statline whose invulnerable save is caveated, the system SHALL attempt to
resolve it exactly once per aggregate rebuild by looking up its own single contributing ability's
text in the ability-classification catalogue (per `ability-classification-catalogue`'s Catalogue
Lookup By Normalized-Text Content Hash), using the same resolution this system already applies to an
ordinary present ability. A match SHALL resolve the value only when that ability's record contains
an unconditional invulnerable-save effect (per `ability-classification-catalogue`'s Unconditional
Effect Rule); a successful resolution SHALL replace the caveated result with a resolved value
carrying that ability as its source, preserving the true pre-caveat original value. No record, or a
record with no unconditional invulnerable-save effect, SHALL leave the result caveated, unchanged
from today's existing fallback behavior.

#### Scenario: A caveated value resolves via a baseline match
- **WHEN** a resolved unit's Statline carries a caveated invulnerable save, and its single
  contributing ability's catalogue record contains an unconditional invulnerable-save effect
- **THEN** the invulnerable save is displayed as resolved, using that effect's value, still
  referencing that ability as its source

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
