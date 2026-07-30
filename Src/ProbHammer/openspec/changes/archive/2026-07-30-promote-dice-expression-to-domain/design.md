## Context

`DiceExpression` (`src/ProbHammer.Core/Simulation/DiceExpression.cs`) is a value type parsing
40K dice notation ("D6", "2D3+1", fixed integers) with `Scale`/`Add` helpers for aggregating
attacks across models. It was written for the (now paused) Monte Carlo simulation engine.

The most recent domain-model commit widened `WeaponProfile.A` and `WeaponProfile.D` from `int` to
`DiceExpression`, because real weapon data (e.g. Multi-melta: D6 damage) can't be represented as a
fixed integer. That commit imported `ProbHammer.Core.Simulation` into
`Domain/Catalogue/WeaponProfile.cs` to get the type — the only place in `Domain/*` that currently
references `Simulation/*`. `.claude/domain-model-11e.md` documents `Simulation/*` as "untouched but
paused," which this import already quietly contradicts.

Hand-authoring `BlackCrusade.cs` against the current shape also showed that every *fixed* weapon
stat now requires explicit `DiceExpression.Fixed(n)` wrapping, which is the common case (most
weapons have fixed Attacks and Damage) — this is real, measured friction across both the new
example file and the existing `WeaponFixtures.cs` test fixtures.

## Goals / Non-Goals

**Goals:**
- Give `DiceExpression` a Domain-owned home that matches what it represents (a game-rules
  quantity), so `Domain/Catalogue/WeaponProfile.cs` no longer needs to reach into `Simulation/*`.
- Make the common case (a fixed weapon stat) as easy to write as a plain integer at construction
  sites, without an explosion of constructor overloads on `RangedWeapon`/`MeleeWeapon`.
- Give `DiceExpression` its own direct unit test coverage — today it is only exercised indirectly
  through `Simulation` and `Domain` tests that happen to construct one.

**Non-Goals:**
- Rewiring `Simulation/*` (`CombatSimulator`, `SimulationAdapter`, `WoundPool`) onto the new
  11e domain model. That is a separate, larger change; this one only repoints a namespace.
- Changing `DiceExpression`'s parsing grammar, rolling semantics, or `Scale`/`Add` aggregation
  behavior. Those are unchanged — this change is additive (new operators/presets) plus a move.
- Touching `SimWeaponProfile`, `CombatSimulator`, or any other `Simulation/*` *behavior* — those
  files only need a `using` update.

## Decisions

### 1. New namespace: `ProbHammer.Core.Domain.Catalogue`, not a new `Domain.Values`/`Domain.Common`

`DiceExpression.cs` moves into `src/ProbHammer.Core/Domain/Catalogue/`, alongside
`WeaponProfile.cs` — its only current consumer in `Domain/*`.

**Alternative considered:** a new shared namespace (e.g. `ProbHammer.Core.Domain.Values`) so
neither `Catalogue` nor `Roster` is seen as "owning" a cross-cutting primitive. Rejected for now:
there is no second consumer today. `Ability.Text` in the Roster context is deliberately
free-text and never auto-parsed (`.claude/domain-model-11e.md`, "Deliberate Omissions") — so
introducing a namespace to serve a hypothetical future Roster consumer would be speculative. If a
second real consumer appears, promoting `DiceExpression` to a shared namespace at that point is a
one-file move, not a redesign.

### 2. Implicit `int -> DiceExpression` conversion, not constructor overloads on the weapon records

```csharp
public static implicit operator DiceExpression(int value) => Fixed(value);
```

**Alternative considered:** secondary constructors on `RangedWeapon`/`MeleeWeapon` chained via
`: this(...)` (e.g. an all-int overload alongside the all-`DiceExpression` one). Rejected: `A` and
`D` vary independently between fixed and dice-based (the Multi-melta needs fixed `A` *and* dice
`D` in the same weapon), so covering every combination needs a combinatorial number of overloads
per weapon type, and the count grows again if a third field ever needs the same treatment. An
implicit conversion on `DiceExpression` itself applies per-argument, so mixed cases fall out for
free with zero changes to `WeaponProfile`, `RangedWeapon`, or `MeleeWeapon`.

The conversion is narrow (one primitive type, one direction, lossless — every `int` maps to
exactly one `DiceExpression.Fixed` value) which is the standard case where an implicit operator is
appropriate rather than surprising.

### 3. Ergonomic sugar: `DiceExpression.D3`/`D6` presets and `operator +(DiceExpression, int)`

Static readonly presets and a `+` operator let the two remaining awkward constructions —
"a plain die" and "a die plus a flat modifier" — read close to game notation:
`DiceExpression.D6`, `DiceExpression.D6 + 2`. This is additive and doesn't touch `Parse`, which
remains the entry point for runtime-parsed strings (e.g. a future BSData/army-list parser reading
raw characteristic text).

### 4. Test suite location

New tests go in `tests/ProbHammer.Tests/Domain/` (mirroring the type's new location), covering
`Parse`, `Fixed`, `Scale`, `Add` (existing behavior, currently untested in isolation) plus the
three new additions from this change. Existing `Simulation`-side tests that exercise
`DiceExpression` indirectly are left as-is; they continue to pass unchanged since behavior is
unchanged.

## Risks / Trade-offs

- **[Risk]** Moving a type out of `Simulation/*` touches files in an area `PROGRESS.md` describes
  as paused/untouched, which could be misread as the start of the deferred rewiring effort.
  → **Mitigation**: scope strictly to `using` statement updates in `Simulation/*`; no behavior,
  method signature, or test change in that folder. Call this out explicitly in `PROGRESS.md` when
  the change lands.
- **[Risk]** Implicit conversions can occasionally cause surprising overload resolution.
  → **Mitigation**: the conversion is one primitive → one value type with no competing overloads
  in this codebase; risk is low and contained to `DiceExpression` call sites.
- **[Trade-off]** `BlackCrusade.cs` and `WeaponFixtures.cs` edits are cosmetic (behavior-preserving
  simplification), adding review surface to a change that's conceptually about the type's home.
  → Accepted: bundling them lets the new ergonomics be verified against real call sites rather
  than only unit tests.

## Migration Plan

1. Add `DiceExpression.cs` under `Domain/Catalogue/` with the new operators/presets; delete it
   from `Simulation/`.
2. Update `using` statements in `Simulation/{SimulationAdapter,CombatSimulator,SimWeaponProfile,
   IDiceRoller}.cs` and remove the now-unneeded `using ProbHammer.Core.Simulation;` from
   `Domain/Catalogue/WeaponProfile.cs`.
3. Simplify `BlackCrusade.cs` and `WeaponFixtures.cs` call sites.
4. Add the new `DiceExpression` test suite.
5. Update `.claude/domain-model-11e.md`.
6. Build + full test suite must pass with zero behavior change outside the new tests.

No rollback complexity beyond a standard revert — this is a compile-time-checked, non-persisted,
internal type move (nothing serialized to session JSON references `DiceExpression` by namespace).

## Open Questions

- None blocking. If a second real consumer of `DiceExpression` emerges outside `Catalogue` (e.g.
  a future structured-ability-effects model in `Roster`), revisit Decision 1.
