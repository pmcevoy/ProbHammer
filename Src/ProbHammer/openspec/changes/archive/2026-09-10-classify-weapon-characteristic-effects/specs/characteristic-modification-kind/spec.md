## MODIFIED Requirements

### Requirement: Closed Arithmetic-Family Classification
The system SHALL classify every characteristic it resolves effects for into exactly one of three
closed arithmetic families: `RollThreshold` (WS, BS, Sv, Ld — an "N+" die-roll bar, where a
numerically lower value is better), `ArmourPenetration` (AP — stored as a negative integer, where a
numerically lower value is a stronger penetration), or `Plain` (every other in-scope characteristic:
M, T, W, Oc, S, Damage, and the length-valued Range characteristic — where a numerically higher
value is better and the stated amount applies with no inversion).

#### Scenario: WS, BS, Sv, and Ld classify as RollThreshold
- **WHEN** a characteristic to be modified is WS, BS, Sv, or Ld
- **THEN** it is classified into the `RollThreshold` arithmetic family

#### Scenario: AP classifies as ArmourPenetration
- **WHEN** a characteristic to be modified is AP
- **THEN** it is classified into the `ArmourPenetration` arithmetic family

#### Scenario: Every other in-scope characteristic classifies as Plain
- **WHEN** a characteristic to be modified is M, T, W, Oc, S, Damage, or the length-valued Range
  characteristic
- **THEN** it is classified into the `Plain` arithmetic family

#### Scenario: Damage classifies as Plain despite being dice-shaped
- **WHEN** a weapon's Damage characteristic (a dice-shaped value, e.g. "D6", not a plain integer)
  is to be modified
- **THEN** it is classified into the `Plain` arithmetic family, the same family a numerically-valued
  characteristic like Strength uses — Damage's dice shape affects how its resolved value is
  represented, not which arithmetic family governs its sign

## ADDED Requirements

### Requirement: Dice-Shaped Characteristics Resolve Via Dice-Aware Arithmetic
For a characteristic whose current value is dice-shaped (a fixed integer, a pure dice roll, or a
dice roll plus a flat modifier — Damage, the first such characteristic this capability resolves
effects for) rather than a plain integer, the system SHALL resolve an `Improve`/`Worsen`/`Set`
effect against that value using the same `Plain`-family sign rule the arithmetic-family
classification assigns it (`Improve` adds, `Worsen` subtracts), applying the signed amount to the
value's own flat modifier component while preserving its dice component (count and sides)
unchanged. A `Set` verb SHALL replace the value outright with a fixed value equal to the stated
amount, discarding any prior dice component — mirroring how `Set` already assigns any other
characteristic's stated value directly with no sign resolution.

#### Scenario: Improving a dice-shaped Damage value adds to its flat modifier
- **WHEN** a Damage of "D6" is improved by 1
- **THEN** the resolved value is "D6+1"

#### Scenario: Worsening a dice-shaped Damage value subtracts from its flat modifier
- **WHEN** a Damage of "D6+2" is worsened by 1
- **THEN** the resolved value is "D6+1"

#### Scenario: Improving a fixed Damage value behaves identically to a plain scalar
- **WHEN** a Damage of a fixed "2" is improved by 1
- **THEN** the resolved value is a fixed "3"

#### Scenario: Setting a dice-shaped Damage value replaces it with a fixed value
- **WHEN** a Damage of "D6" is set to 3
- **THEN** the resolved value is a fixed "3", with no dice component remaining

### Requirement: Dice-Shaped Characteristic Clamp Bound Enforced After Resolution
After a delta or `Set` value is resolved against a dice-shaped characteristic, the system SHALL
clamp the result so the value's own guaranteed minimum possible outcome (its flat modifier, plus
one for each die it rolls) never falls below Damage's rulebook floor of 1, the same floor
`M`/`T`/`S`/`Range` already enforce for a plain-scalar `Plain`-family characteristic. Clamping
SHALL silently cap the flat modifier at whatever value keeps the guaranteed minimum at 1, rather
than raise an error.

#### Scenario: Worsening a dice-shaped Damage value past its floor clamps its modifier
- **WHEN** a Damage of "D6" (guaranteed minimum 1) is worsened by 3
- **THEN** the resolved value's flat modifier is clamped so its guaranteed minimum stays 1, rather
  than resolving to "D6-3" (a guaranteed minimum of -2)

#### Scenario: Worsening a fixed Damage value past its floor clamps to 1
- **WHEN** a Damage of a fixed "2" is worsened by 5
- **THEN** the resolved value is a fixed "1", not "-3"
