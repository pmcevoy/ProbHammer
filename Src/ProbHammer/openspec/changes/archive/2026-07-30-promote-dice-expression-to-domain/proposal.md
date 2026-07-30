## Why

`DiceExpression` currently lives in `ProbHammer.Core.Simulation`, but the 11th-edition domain
model already depends on it: `WeaponProfile.A` and `WeaponProfile.D` were widened from `int` to
`DiceExpression` to represent weapon stats like "D6" or "D6+2" (e.g. Multi-melta damage), and
`Domain/Catalogue/WeaponProfile.cs` now imports `ProbHammer.Core.Simulation` to get it. This
contradicts the documented invariant that `Simulation/*` is untouched while paused, and it puts a
Simulation-owned type at the center of a Domain value object — backwards for a concept that is
fundamentally a game-rules quantity (a D6 means the same thing whether or not a simulation ever
rolls it).

Hand-authoring a real army list (`BlackCrusade.cs`) against the current shape also surfaced an
ergonomics problem: every fixed weapon stat needs explicit `DiceExpression.Fixed(n)` wrapping
(`new MeleeWeapon("Armoured hull", 3, 4, 6, 0, 1)` is not legal; `DiceExpression.Fixed(3)` must be
written for the `A`), and there is no dedicated test suite for `DiceExpression` itself.

## What Changes

- Move `DiceExpression` from `ProbHammer.Core.Simulation` to `ProbHammer.Core.Domain` — it becomes
  a Domain-owned value type. `Simulation/*` repoints its `using` statements to the relocated type;
  this is a mechanical namespace move with no behavior change and no wiring of Simulation onto the
  new domain model (that remains deferred).
- Add an implicit `int -> DiceExpression` conversion operator so fixed-value weapon stats (the
  common case) can be written as plain integers at call sites, while variable stats still use
  `DiceExpression` explicitly.
- Add ergonomic construction sugar: static readonly `DiceExpression.D3` / `DiceExpression.D6`
  presets, and `operator +(DiceExpression, int)` so "D6+2" can be written as
  `DiceExpression.D6 + 2` instead of a runtime-parsed string or a verbose object initializer.
- Add a dedicated unit test suite for `DiceExpression` (`Parse`, `Fixed`, `Scale`, `Add`, the new
  implicit conversion, the new presets, and the new `+` operator) — it currently has none of its
  own.
- Update `Domain/Examples/BlackCrusade.cs` and `tests/.../Fixtures/WeaponFixtures.cs` to drop
  now-unnecessary `DiceExpression.Fixed(...)` wrapping now that the implicit conversion exists.
- Update `.claude/domain-model-11e.md` to document `DiceExpression`'s new home and the widened
  `A`/`D` shape (currently undocumented drift from the last change).
- **BREAKING** (internal only): `ProbHammer.Core.Simulation.DiceExpression` no longer exists at
  that namespace. Any code referencing it must update its `using` statement. No public API of the
  Web app is affected; no serialized session data references this type by namespace.

## Capabilities

### New Capabilities
- `dice-expression`: a Domain value type representing a fixed or randomized 40K quantity (attacks,
  damage), with parsing from game notation, scaling/aggregation for multi-model attack pooling,
  and ergonomic construction (implicit int conversion, common-die presets, modifier addition).

### Modified Capabilities
- None. `datasheet-catalogue`'s existing "On-Demand Weapon Profile Resolution" requirement already
  describes Attacks/Damage as opaque characteristics without constraining their representation, so
  no requirement text there changes.

## Impact

- `src/ProbHammer.Core/Simulation/DiceExpression.cs` removed; re-created under
  `src/ProbHammer.Core/Domain/` (exact sub-namespace decided in design.md).
- `src/ProbHammer.Core/Simulation/{SimulationAdapter,CombatSimulator,SimWeaponProfile,
  IDiceRoller}.cs` — `using` statement updated only, no logic changes.
- `src/ProbHammer.Core/Domain/Catalogue/WeaponProfile.cs` — drops its
  `using ProbHammer.Core.Simulation;` import, now that `DiceExpression` is a Domain sibling.
- `src/ProbHammer.Core/Domain/Examples/BlackCrusade.cs`,
  `tests/ProbHammer.Tests/Domain/Fixtures/WeaponFixtures.cs` — simplified call sites.
- New test file under `tests/ProbHammer.Tests/Domain/` covering `DiceExpression` directly.
- `.claude/domain-model-11e.md` updated.
