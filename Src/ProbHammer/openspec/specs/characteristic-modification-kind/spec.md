# characteristic-modification-kind Specification

## Purpose

Resolves an `Improve`/`Worsen`/`Set` effect verb and amount into the correct signed mutation for a
specific characteristic's own rulebook arithmetic family, and enforces that characteristic's legal
value bounds afterward — the arithmetic step `rule-effect-classification` deliberately stops short
of, and the step the existing hand-authored `statline-flag-rules` currently duplicate ad hoc.

Scoped to the characteristics this codebase actually represents as a plain scalar value today — the
six `Statline` scalars (M, T, Sv, W, Ld, Oc) and the four `WeaponProfile` scalars (Bs, Ws, S, Ap),
plus the bare-integer Range characteristic — and, in addition, `WeaponProfile`'s Damage
characteristic, which is dice-shaped (a fixed integer, a pure dice roll, or a dice roll plus a flat
modifier) rather than a plain scalar and so resolves via its own dice-aware arithmetic (see the
dice-shaped requirements below), while still classifying into the same closed arithmetic family a
plain scalar would. Invulnerable save (InSv) is deliberately excluded: it is already a compound
melee/ranged value with its own dedicated resolution path
(`invulnerable-save`/`characteristic-value`), not a plain scalar — the same reason
`characteristic-modifier-caveats` already excludes it from its own Field allowlist. A weapon's
Attacks characteristic is also excluded: this codebase represents it as a bare `DiceExpression` with
no resolution path yet — classifiable (`rule-effect-classification`'s own weapon-characteristic
Effect extraction covers it) but not yet resolvable, a deliberately separate, later-sequenced piece
of work from Damage's own resolution path.

## Requirements

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
- **THEN** it is classified into the `Plain` arithmetic family, the same family a
  numerically-valued characteristic like Strength uses — Damage's dice shape affects how its
  resolved value is represented, not which arithmetic family governs its sign

### Requirement: Sign Resolution By Arithmetic Family
The system SHALL resolve an `Improve` or `Worsen` verb plus a stated amount into a signed delta
according to the characteristic's own arithmetic family: for `RollThreshold`, `Improve` SHALL
subtract the amount from the threshold number and `Worsen` SHALL add it; for `ArmourPenetration`,
`Improve` SHALL subtract the amount (moving further from zero) and `Worsen` SHALL add it (moving
toward zero); for `Plain`, `Improve` SHALL add the amount and `Worsen` SHALL subtract it. A `Set`
verb SHALL assign its stated value directly, with no sign resolution applied regardless of arithmetic
family.

#### Scenario: Improving a RollThreshold characteristic subtracts
- **WHEN** a WS of 3+ is improved by 1
- **THEN** the resolved value is 2+

#### Scenario: Worsening a RollThreshold characteristic adds
- **WHEN** a WS of 3+ is worsened by 1
- **THEN** the resolved value is 4+

#### Scenario: Improving ArmourPenetration subtracts
- **WHEN** an AP of -1 is improved by 1
- **THEN** the resolved value is -2

#### Scenario: Worsening ArmourPenetration adds
- **WHEN** an AP of -1 is worsened by 1
- **THEN** the resolved value is 0

#### Scenario: Improving or worsening a Plain characteristic adds or subtracts directly
- **WHEN** a Strength of 4 is improved by 1
- **THEN** the resolved value is 5

#### Scenario: A Set verb assigns its value directly
- **WHEN** an Objective Control is set to 3, regardless of its prior value
- **THEN** the resolved value is 3, with no sign resolution applied

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

### Requirement: Per-Characteristic Clamp Bounds Enforced After Resolution
After a delta or `Set` value is resolved, the system SHALL clamp the result to that characteristic's
own legal bound before it is used as a resolved value, per the rulebook's characteristic-modification
limits: Sv SHALL NOT resolve better than 2+; Ld SHALL NOT resolve better than 5+ or worse than 8+; WS
and BS SHALL NOT resolve better than 2+ or worse than 6+; Oc SHALL NOT resolve below 0; AP SHALL NOT
resolve worse than 0 (i.e. SHALL NOT become positive); M, T, S, and the length-valued Range
characteristic SHALL NOT resolve below 1 (1" for the length-valued one). Clamping SHALL silently cap
the value at its bound rather than raise an error.

#### Scenario: Worsening AP past its cap clamps to 0
- **WHEN** an AP of 0 is worsened by 1
- **THEN** the resolved value is 0, not 1

#### Scenario: Improving Sv past its floor clamps to 2+
- **WHEN** a Sv of 3+ is improved by 2
- **THEN** the resolved value is 2+, not 1+

#### Scenario: Ld resolution is clamped to its 5+–8+ range
- **WHEN** a Ld of 6+ is improved by 3
- **THEN** the resolved value is 5+, not 3+

#### Scenario: WS/BS resolution is clamped to its 2+–6+ range
- **WHEN** a BS of 5+ is worsened by 3
- **THEN** the resolved value is 6+, not 8+

#### Scenario: Oc resolution is clamped to a floor of 0
- **WHEN** an Oc of 1 is worsened by 3
- **THEN** the resolved value is 0, not -2

#### Scenario: A Plain characteristic with a floor of 1 is clamped
- **WHEN** a Movement of 2" is worsened by 5"
- **THEN** the resolved value is 1", not -3"

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

### Requirement: Symbolic Characteristic Values Are Never Modified
The system SHALL NOT modify a characteristic whose value is symbolic (e.g. "-", "*", "N/A"),
regardless of the effect's stated verb, characteristic, or amount. Resolving an effect against a
symbolic value SHALL leave it unchanged.

#### Scenario: An effect targeting a symbolic value produces no change
- **WHEN** an effect states an `Improve` or `Set` verb against a characteristic whose current value
  is symbolic ("-", "*", or "N/A")
- **THEN** the characteristic's value remains the unmodified symbolic value

### Requirement: Reproduces An Existing Hand-Authored Rule's Result
Resolving the same effect a currently-shipped `statline-flag-rules` rule already derives by hand,
for a characteristic this capability covers, SHALL produce an identical resolved value — proving the
arithmetic-family resolution is a correct, independently-verified replacement for that rule's own ad
hoc arithmetic before either rule is changed to use it. Shield Dome's own rule is not a valid proving
example here, since it mutates InSv, which this capability deliberately does not cover (see Purpose).

#### Scenario: Reproduces Vexilla's resolved Objective Control
- **WHEN** an `Improve` effect of Oc by 1 is resolved against a model's base Objective Control value,
  mirroring Vexilla's own ability text ("Add 1 to the Objective Control characteristic of models in
  the bearer's unit.")
- **THEN** the resolved value is the base value plus 1 — identical to what `VexillaStatlineFlagRule`
  already derives for a unit carrying that ability
