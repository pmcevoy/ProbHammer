# weapon-characteristic-effect-resolution

## Purpose

Resolves a classified weapon-characteristic Effect (from the rule-effect-classification capability)
and its source Ability against a specific weapon's profile into a mutated profile — the
weapon-specific counterpart to the characteristic-modification-kind capability's scalar resolution
and the invulnerable-save-effect-resolution capability's own compound-value resolution.

## Requirements

### Requirement: Resolving A Weapon-Characteristic Effect Into A Mutated Profile
Given a classified weapon-characteristic Effect naming a Strength, Armour Penetration, or Damage
characteristic, its source Ability, and a weapon profile, the system SHALL produce a mutated profile
whose named characteristic's derived value reflects the Effect's stated Verb and Amount applied to
the characteristic's own pre-mutation value, whose pre-mutation original value is preserved, and
whose contributing abilities record the source Ability. Every other field of the profile (Name,
Type, Range, Attacks, and every keyword/ability flag) SHALL be unchanged by this resolution.

#### Scenario: An Improve effect raises the targeted characteristic
- **WHEN** resolving a weapon-characteristic Effect stating an Improve of `1` against the Strength
  characteristic of a weapon profile whose current Strength is `4`
- **THEN** the mutated profile's Strength derived value is `5`, and the source Ability is recorded
  as a contributing ability on that field

#### Scenario: An Armour Penetration effect follows the negative-integer sign convention
- **WHEN** resolving a weapon-characteristic Effect stating an Improve of `1` against the Armour
  Penetration characteristic of a weapon profile whose current Armour Penetration is `-1`
- **THEN** the mutated profile's Armour Penetration derived value is `-2`, matching this project's
  stored-as-negative convention for a stronger penetration

#### Scenario: A Damage effect resolves dice-aware
- **WHEN** resolving a weapon-characteristic Effect stating an Improve of `1` against the Damage
  characteristic of a weapon profile whose current Damage is a dice expression
- **THEN** the mutated profile's Damage derived value adds `1` to that expression's flat modifier,
  preserving its dice component unchanged

#### Scenario: The pre-mutation original value is preserved
- **WHEN** resolving a weapon-characteristic Effect against a characteristic whose existing value
  before this mutation differs from the Effect's own resulting value
- **THEN** the mutated profile's original value for that characteristic is the profile's own
  pre-mutation value, not the Effect's resulting value

#### Scenario: Every other field of the profile is unchanged
- **WHEN** resolving a weapon-characteristic Effect naming one characteristic against a weapon
  profile
- **THEN** the mutated profile's Name, Type, Range, Attacks, and every keyword/ability flag are
  identical to the profile before resolution

### Requirement: Resolving An Attacks-Characteristic Effect Into A Per-Model Amount
Given a classified weapon-characteristic Effect naming the Attacks characteristic and its source
Ability, the system SHALL produce a signed per-model amount reflecting the Effect's stated Verb and
Amount, using this project's existing Improve/Worsen sign convention for a Plain-family
characteristic. This is distinct from "Resolving A Weapon-Characteristic Effect Into A Mutated
Profile", which never covers Attacks: this resolution produces a value meant to be added to a
contributor's own base Attacks value, not a mutated weapon profile.

#### Scenario: An Improve effect resolves to a positive per-model amount
- **WHEN** resolving a weapon-characteristic Effect stating an Improve of `3` against the Attacks
  characteristic
- **THEN** the resolved per-model amount is `+3`

#### Scenario: A Worsen effect resolves to a negative per-model amount
- **WHEN** resolving a weapon-characteristic Effect stating a Worsen of `1` against the Attacks
  characteristic
- **THEN** the resolved per-model amount is `-1`

#### Scenario: A Set effect is rejected rather than silently producing an amount
- **WHEN** resolving a weapon-characteristic Effect stating a Set verb against the Attacks
  characteristic
- **THEN** the system rejects the attempt rather than producing a per-model amount, matching this
  project's existing convention that a Set verb has no delta/sign concept

### Requirement: Attacks Characteristic Is Not Resolved
The system SHALL NOT resolve a weapon-characteristic Effect naming the Attacks characteristic — this
capability's resolution is scoped to Strength, Armour Penetration, and Damage only, matching this
project's existing, deliberate exclusion of Attacks from characteristic-modification resolution.

#### Scenario: Attempting to resolve an Attacks-characteristic effect is rejected
- **WHEN** resolving a weapon-characteristic Effect naming the Attacks characteristic
- **THEN** the system rejects the attempt rather than silently producing a result

### Requirement: Weapon-Selector Matching
Given a weapon selector (naming every weapon, every weapon of one class, or one specifically named
weapon) and a candidate weapon profile, the system SHALL determine whether that selector applies to
that profile: an unqualified selector matches every profile; a class-qualified selector matches only
a profile of the same weapon type; a name-qualified selector matches only a profile with that exact
name.

#### Scenario: An unqualified selector matches any weapon
- **WHEN** matching an unqualified weapon selector against a weapon profile of either type
- **THEN** the selector matches

#### Scenario: A class-qualified selector matches only its own weapon type
- **WHEN** matching a melee-class weapon selector against a ranged weapon profile
- **THEN** the selector does not match

#### Scenario: A class-qualified selector matches its own weapon type
- **WHEN** matching a melee-class weapon selector against a melee weapon profile
- **THEN** the selector matches

#### Scenario: A named selector matches only the exact name
- **WHEN** matching a selector naming one specific weapon against a profile with a different name
- **THEN** the selector does not match
