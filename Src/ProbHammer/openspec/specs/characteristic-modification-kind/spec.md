# characteristic-modification-kind Specification

## Purpose

Resolves an `Improve`/`Worsen`/`Set` effect verb and amount into the correct signed mutation for a
specific characteristic's own rulebook arithmetic family, and enforces that characteristic's legal
value bounds afterward — the arithmetic step `rule-effect-classification` deliberately stops short
of, and the step the existing hand-authored `statline-flag-rules` currently duplicate ad hoc.

Scoped to the characteristics this codebase actually represents as a plain scalar value today — the
six `Statline` scalars (M, T, Sv, W, Ld, Oc) and the four `WeaponProfile` scalars (Bs, Ws, S, Ap),
plus the bare-integer Range characteristic. Invulnerable save (InSv) is deliberately excluded: it is
already a compound melee/ranged value with its own dedicated resolution path
(`invulnerable-save`/`characteristic-value`), not a plain scalar — the same reason
`characteristic-modifier-caveats` already excludes it from its own Field allowlist. A weapon's
Attacks/Damage characteristics are also excluded: this codebase represents both as a `DiceExpression`
(fixed or dice-valued), never a plain scalar, so a signed-integer delta does not apply to them as
currently modeled.

## Requirements

### Requirement: Closed Arithmetic-Family Classification
The system SHALL classify every characteristic it resolves effects for into exactly one of three
closed arithmetic families: `RollThreshold` (WS, BS, Sv, Ld — an "N+" die-roll bar, where a
numerically lower value is better), `ArmourPenetration` (AP — stored as a negative integer, where a
numerically lower value is a stronger penetration), or `Plain` (every other in-scope characteristic:
M, T, W, Oc, S, and the length-valued Range characteristic — where a numerically higher value is
better and the stated amount applies with no inversion).

#### Scenario: WS, BS, Sv, and Ld classify as RollThreshold
- **WHEN** a characteristic to be modified is WS, BS, Sv, or Ld
- **THEN** it is classified into the `RollThreshold` arithmetic family

#### Scenario: AP classifies as ArmourPenetration
- **WHEN** a characteristic to be modified is AP
- **THEN** it is classified into the `ArmourPenetration` arithmetic family

#### Scenario: Every other in-scope characteristic classifies as Plain
- **WHEN** a characteristic to be modified is M, T, W, Oc, S, or the length-valued Range
  characteristic
- **THEN** it is classified into the `Plain` arithmetic family

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
