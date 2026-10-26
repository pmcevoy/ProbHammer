# feel-no-pain Specification

## Purpose
Treats Feel No Pain as a Statline characteristic: its value shape, the value a resolved unit's own
Feel No Pain abilities give it, and how a granted value combines with an existing one.

## Requirements

### Requirement: Feel No Pain Value Shape
A Statline's Feel No Pain SHALL be a characteristic view over a single roll threshold (2+ to 6+) or
absent, with the same original value, shown value, caveat and contributing-ability shape as the
other Statline characteristics. Absent SHALL be the default.

#### Scenario: A Statline with no Feel No Pain
- **WHEN** a Statline is built with no Feel No Pain information
- **THEN** its Feel No Pain is absent, not caveated, and has no contributing abilities

#### Scenario: Two Statlines differing only in Feel No Pain are not equal
- **WHEN** two Statlines have identical M/T/Sv/W/Ld/OC and invulnerable save but one has Feel No
  Pain 5+ and the other has none
- **THEN** they compare as different values

### Requirement: Feel No Pain From The Bearer's Own Abilities
For each Statline entry of a resolved unit, the system SHALL derive Feel No Pain from the present
abilities named "Feel No Pain X+" that the entry's own bearer carries (a model-line's ability, from
the datasheet or wargear, for that row; a component-wide ability for every row of that component),
keeping the best threshold, with absent as the original value and the giving ability as its
contributing ability. A name with a footnote marker or more than one value SHALL give a caveated
value, except that a "Feel No Pain X+*" name SHALL give no value when a qualified Feel No Pain X+
grant reaches the same entry. A valueless "Feel No Pain" name SHALL give no value.

#### Scenario: A datasheet Feel No Pain ability gives every row its value
- **WHEN** a Poxwalkers unit's datasheet carries "Feel No Pain 5+"
- **THEN** each of its Statline entries has Feel No Pain 5+, not caveated, original value absent,
  with "Feel No Pain 5+" as its contributing ability

#### Scenario: A wargear Feel No Pain applies to its own row only
- **WHEN** one model-line of a unit carries "Feel No Pain 4+" through its wargear and the unit's
  other model-lines do not
- **THEN** only that model-line's Statline entry has Feel No Pain 4+, with "Feel No Pain 4+" as its
  contributing ability

#### Scenario: A footnoted Feel No Pain name is caveated
- **WHEN** a unit's datasheet carries "Feel No Pain 3+*"
- **THEN** its Statline entries have a caveated Feel No Pain of 3+ whose contributing ability is
  "Feel No Pain 3+*"

#### Scenario: A footnote explained by a qualified grant gives no value
- **WHEN** a Vigilators unit carries "Feel No Pain 3+*" and Daughters of the Abyss (Feel No Pain 3+
  against Psychic Attacks and mortal wounds)
- **THEN** its Statline entries have no Feel No Pain, and Daughters of the Abyss is recorded in force

#### Scenario: An explained footnote leaves the general value
- **WHEN** an entry carries "Feel No Pain 5+", "Feel No Pain 3+*" and a qualified Feel No Pain 3+ grant
- **THEN** its Feel No Pain is 5+, not caveated, with "Feel No Pain 5+" as its contributing ability

#### Scenario: A valueless Feel No Pain name gives no value
- **WHEN** a Custodian Wardens unit's datasheet carries a "Feel No Pain" ability with no value
- **THEN** its Statline entries have no Feel No Pain

#### Scenario: The value follows its bearer's liveness
- **WHEN** the only model-line carrying "Feel No Pain 4+" is marked fully destroyed
- **THEN** that entry's Feel No Pain is absent on the next render

### Requirement: A Feel No Pain Grant Keeps The Better Threshold
Applying a granted Feel No Pain value to a Statline entry SHALL keep the lower (better) of the
granted and existing thresholds, comparing thresholds even when the existing value already has a
contributing ability; an absent value SHALL take the granted one. An improving grant SHALL become the
value's contributing ability in place of the one it beat. A grant that does not improve the value
SHALL leave it and its contributing ability unchanged.

#### Scenario: A grant to a unit without Feel No Pain
- **WHEN** a Feel No Pain 4+ grant applies to an entry with no Feel No Pain
- **THEN** the entry's Feel No Pain is 4+, with the granting ability as its contributing ability and
  absent as its original value

#### Scenario: A worse grant leaves the value alone
- **WHEN** a Feel No Pain 6+ grant applies to an entry whose own "Feel No Pain 5+" ability gives 5+
- **THEN** the entry's Feel No Pain stays 5+ with "Feel No Pain 5+" as its contributing ability

#### Scenario: A better grant replaces the bearer's own value
- **WHEN** a Feel No Pain 4+ grant applies to an entry whose own "Feel No Pain 5+" ability gives 5+
- **THEN** the entry's Feel No Pain is 4+ with the granting ability as its contributing ability
