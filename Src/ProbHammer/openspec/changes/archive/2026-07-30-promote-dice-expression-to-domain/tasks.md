## 1. Move DiceExpression into Domain

- [x] 1.1 Create `src/ProbHammer.Core/Domain/Catalogue/DiceExpression.cs` with namespace
      `ProbHammer.Core.Domain.Catalogue`, carrying over `Count`, `Sides`, `Modifier`, `Parse`,
      `Fixed`, `Scale`, `Add`, and `ToString` unchanged from the current
      `Simulation/DiceExpression.cs`.
- [x] 1.2 Delete `src/ProbHammer.Core/Simulation/DiceExpression.cs`.

## 2. Add ergonomic construction API

- [x] 2.1 Add `public static implicit operator DiceExpression(int value) => Fixed(value);`
- [x] 2.2 Add static readonly presets `DiceExpression.D3` and `DiceExpression.D6`.
- [x] 2.3 Add `public static DiceExpression operator +(DiceExpression d, int modifier)` returning
      a new expression with `modifier` folded into `Modifier` (non-mutating).

## 3. Repoint Simulation to the relocated type

- [x] 3.1 Update `using` statements in `Simulation/SimulationAdapter.cs`,
      `Simulation/CombatSimulator.cs`, `Simulation/SimWeaponProfile.cs`, and
      `Simulation/IDiceRoller.cs` to reference `ProbHammer.Core.Domain.Catalogue` instead of the
      deleted `Simulation.DiceExpression`. Used a `using DiceExpression = ...` alias in the two
      files that also import `ProbHammer.Core.Contracts`, since a blanket
      `using ProbHammer.Core.Domain.Catalogue;` collides with `Contracts.WeaponProfile`/
      `Contracts.WeaponType` (both namespaces define same-named types).
- [x] 3.2 Remove the now-unnecessary `using ProbHammer.Core.Simulation;` from
      `Domain/Catalogue/WeaponProfile.cs`.
- [x] 3.3 Build the solution and confirm no other file references the old namespace. Also fixed
      three test files with the same missing-using/namespace-collision issue
      (`tests/.../Simulation/SequenceRoller.cs`, `DiceRollerTests.cs`, `CombatSimulatorTests.cs`)
      and one pre-existing test bug surfaced by the build: `DatasheetTests.cs` asserted
      `profile.A.Should().Be(4)` against a property that was already `DiceExpression` (widened in
      the prior commit, before this change), which only started failing at runtime instead of
      silently not compiling once the new implicit `int → DiceExpression` conversion (task 2.1)
      made it compile. Fixed to `.Be(DiceExpression.Fixed(4))` — asserting through the implicit
      conversion inside a generic FluentAssertions call didn't resolve as expected, so the
      explicit form is used instead. Full suite: 214/214 passing.

## 4. Simplify existing call sites

- [x] 4.0 **Scope addition, agreed with user mid-implementation**: widened `WeaponProfile.D`
      (and `RangedWeapon.D` / `MeleeWeapon.D` / `WeaponProfileEqualityKey.D`) from `int` to
      `DiceExpression`, matching `A`. This was left half-done in the commit that preceded this
      change (a `//NOTE: D needs to be able to accept "D6"` comment on a `Fixed(6)` placeholder
      for the Multi-melta) — not something design.md originally scoped, since design.md only
      covered `DiceExpression` itself. Confirmed with the user before proceeding; proposal.md's
      "Why" section already (incorrectly, as it turned out — see 5.1's correction) described `D`
      as already widened, so this brings the code in line with what was actually proposed.
- [x] 4.1 Update `Domain/Examples/BlackCrusade.cs` to drop `DiceExpression.Fixed(...)` wrapping
      for fixed-value Attacks/Damage, relying on the implicit int conversion; use the `D3`/`D6`
      presets and `+` operator where notation like "D6" or "D6+2" is intended (e.g. the
      Multi-melta's Damage, currently a `//NOTE` TODO comment on a `Fixed(6)` placeholder). Also
      fixed the Pyre pistol's Attacks (previously `new DiceExpression { Sides = 6, Count = 1 }`)
      to use the `D6` preset.
- [x] 4.2 Update `tests/ProbHammer.Tests/Domain/Fixtures/WeaponFixtures.cs` the same way; dropped
      its now-unused `using ProbHammer.Core.Simulation;`.
- [x] 4.3 Confirm existing domain and simulation tests still pass unchanged (behavior-preserving).
      Also updated `DatasheetTests.cs`'s `profile.D.Should().Be(...)` assertion to the same
      explicit `DiceExpression.Fixed(...)` form used for `A` (see 3.3's note on why the implicit
      conversion doesn't resolve inside that generic FluentAssertions call). Full suite:
      214/214 passing; build 0 warnings/0 errors.

## 5. Test suite for DiceExpression

- [x] 5.1 Create `tests/ProbHammer.Tests/Domain/DiceExpressionTests.cs`. **Correction to
      proposal.md**: this type already had a dedicated suite at
      `tests/ProbHammer.Tests/Simulation/DiceExpressionTests.cs` — the proposal's "it currently
      has no dedicated tests of its own" was inaccurate. Relocated that file (namespace/using
      updated) rather than writing a duplicate from scratch, then extended it with the new
      scenarios below.
- [x] 5.2 Cover `Parse`: fixed integer, single die, multiple dice with modifier, invalid notation
      (per `specs/dice-expression/spec.md` "Fixed and Randomized Quantity Representation") —
      already present in the relocated suite, carried over unchanged.
- [x] 5.3 Cover the implicit int conversion (`specs/dice-expression/spec.md` "Ergonomic
      Fixed-Value Construction").
- [x] 5.4 Cover the `D3`/`D6` presets (`specs/dice-expression/spec.md` "Common Die Presets").
- [x] 5.5 Cover the `+` operator, including that the source preset/value is left unmutated
      (`specs/dice-expression/spec.md` "Modifier Composition").
- [x] 5.6 Cover `Scale` and `Add`, including the fixed+fixed, fixed+dice, dice+dice
      (matching sides), and dice+dice (mismatched sides throws) cases
      (`specs/dice-expression/spec.md` "Attack and Damage Aggregation Across Models") — already
      present in the relocated suite, carried over unchanged.

## 6. Documentation

- [x] 6.1 Update `.claude/domain-model-11e.md`: document `DiceExpression`'s new location under
      `ProbHammer.Core.Domain.Catalogue`, and update the `WeaponProfile` shape description to
      note `A`/`D` are `DiceExpression`, not `int`.
- [x] 6.2 Update `PROGRESS.md` noting this change and clarifying that the `Simulation/*` edits
      were namespace-only (`using` updates), not the start of the deferred rewiring effort.
