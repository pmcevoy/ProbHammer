## MODIFIED Requirements

### Requirement: Resolving A Weapon-Characteristic Effect Into A Mutated Profile
Given a classified weapon-characteristic Effect naming a Strength, Armour Penetration, Damage, Ballistic
Skill, or Weapon Skill characteristic, its source Ability, and a weapon profile, the system SHALL produce a mutated profile
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


#### Scenario: A Ballistic Skill Improve lowers a ranged weapon's Skill
- **WHEN** resolving a weapon-characteristic Effect stating an Improve of `1` against the Ballistic
  Skill characteristic of a ranged weapon profile whose current Skill is `4`
- **THEN** the mutated profile's Skill derived value is `3`, since a lower roll threshold is better

#### Scenario: A Weapon Skill Worsen raises a melee weapon's Skill
- **WHEN** resolving a weapon-characteristic Effect stating a Worsen of `1` against the Weapon Skill
  characteristic of a melee weapon profile whose current Skill is `3`
- **THEN** the mutated profile's Skill derived value is `4`

#### Scenario: A Skill result is clamped to 2 through 6
- **WHEN** resolving a Ballistic Skill Improve of `2` against a ranged weapon profile whose current
  Skill is `3`
- **THEN** the mutated profile's Skill derived value is `2`, not `1`

#### Scenario: A Set Skill effect replaces the value
- **WHEN** resolving a Ballistic Skill Set of `3` against a ranged weapon profile whose current Skill
  is `4`
- **THEN** the mutated profile's Skill derived value is `3`

### Requirement: Attacks Characteristic Is Not Resolved
The system SHALL NOT resolve a weapon-characteristic Effect naming the Attacks characteristic — this
capability's resolution is scoped to Strength, Armour Penetration, Damage, Ballistic Skill, and Weapon
Skill only, matching this project's existing, deliberate exclusion of Attacks from
characteristic-modification resolution.

#### Scenario: Attempting to resolve an Attacks-characteristic effect is rejected
- **WHEN** resolving a weapon-characteristic Effect naming the Attacks characteristic
- **THEN** the system rejects the attempt rather than silently producing a result

## ADDED Requirements

### Requirement: Skill Characteristic Matches Weapon Type
A weapon's Ballistic Skill and Weapon Skill are its one Skill characteristic, read by weapon type. A
Ballistic Skill effect SHALL apply only to a ranged weapon and a Weapon Skill effect only to a melee
weapon. Weapon-selector matching SHALL treat a Skill effect whose skill doesn't match the weapon's
type, or whose weapon has no Skill (shown "N/A"), as not matching that weapon, whatever its
selector. Resolving a Skill effect directly against a weapon of the other type SHALL be rejected.

#### Scenario: A Ballistic Skill effect does not match a melee weapon
- **WHEN** matching a Ballistic Skill effect whose selector names every weapon against a melee weapon
  profile
- **THEN** the effect does not match

#### Scenario: A Weapon Skill effect matches a melee weapon
- **WHEN** matching a Weapon Skill effect whose selector names every weapon against a melee weapon
  profile
- **THEN** the effect matches

#### Scenario: Resolving a Skill effect against the other weapon type is rejected
- **WHEN** resolving a Weapon Skill effect against a ranged weapon profile
- **THEN** the system rejects the attempt rather than silently producing a result

#### Scenario: A Skill effect does not match a weapon with no Skill
- **WHEN** matching a Ballistic Skill effect against a ranged weapon whose Ballistic Skill is "N/A"
  (e.g. a Torrent weapon)
- **THEN** the effect does not match, so the weapon gains no Ballistic Skill
