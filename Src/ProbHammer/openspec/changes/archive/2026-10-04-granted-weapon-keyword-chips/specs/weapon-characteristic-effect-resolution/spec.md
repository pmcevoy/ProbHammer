## ADDED Requirements

### Requirement: Weapon Keyword Identity
The system SHALL identify a weapon keyword by its name, ignoring case, surrounding brackets and
spacing, and, for an Anti keyword only, by its target type as well. A keyword's value (its trailing
number, dice expression, or `N+` threshold) SHALL NOT be part of its identity.

#### Scenario: Two values of the same keyword share one identity
- **WHEN** comparing "Sustained Hits 1" with "SUSTAINED HITS 2"
- **THEN** they have the same identity, with values 1 and 2

#### Scenario: Anti keywords with different targets are different keywords
- **WHEN** comparing "Anti-Infantry 4+" with "Anti-Vehicle 4+"
- **THEN** they have different identities

#### Scenario: Brackets and casing do not change identity
- **WHEN** comparing "[ANTI-TITANIC 4+]" with "Anti-Titanic 4+"
- **THEN** they have the same identity and the same value

#### Scenario: A keyword without a value
- **WHEN** comparing "Lethal Hits" with "LETHAL HITS"
- **THEN** they have the same identity and neither has a value

### Requirement: Resolving A Weapon Keyword Grant
Given a weapon's own keywords and a granted keyword, the system SHALL add the grant when no own
keyword shares its identity; SHALL leave the keywords unchanged when an own keyword shares its
identity with an equal or better value, or when neither has a value; and SHALL replace that own
keyword with the grant when the grant's value is strictly better, reporting the replaced keyword.

#### Scenario: A keyword the weapon lacks is added
- **WHEN** Lethal Hits is granted to a weapon whose keywords are Assault and Pistol
- **THEN** the weapon's keywords become Assault, Pistol and Lethal Hits

#### Scenario: A keyword the weapon already has changes nothing
- **WHEN** Lethal Hits is granted to a weapon that already has Lethal Hits
- **THEN** the weapon's keywords are unchanged and nothing is reported as granted

#### Scenario: A better value replaces the weapon's own keyword
- **WHEN** Sustained Hits 2 is granted to a weapon that has Sustained Hits 1
- **THEN** the weapon has Sustained Hits 2 instead of Sustained Hits 1, and Sustained Hits 1 is
  reported as replaced

#### Scenario: A worse or equal value changes nothing
- **WHEN** Sustained Hits 1 is granted to a weapon that has Sustained Hits 2
- **THEN** the weapon's keywords are unchanged

### Requirement: Comparing Weapon Keyword Values
For two values of the same keyword, the system SHALL treat a higher number as better for every
keyword except Anti, where a lower threshold SHALL be better. A dice value SHALL compare by its
expected value.

#### Scenario: A higher Sustained Hits value is better
- **WHEN** comparing Sustained Hits 2 with Sustained Hits 1
- **THEN** Sustained Hits 2 is better

#### Scenario: A lower Anti threshold is better
- **WHEN** comparing Anti-Infantry 2+ with Anti-Infantry 4+
- **THEN** Anti-Infantry 2+ is better

#### Scenario: A dice value compares by its expected value
- **WHEN** comparing Sustained Hits D3 with Sustained Hits 1
- **THEN** Sustained Hits D3 is better, since its expected value is 2
