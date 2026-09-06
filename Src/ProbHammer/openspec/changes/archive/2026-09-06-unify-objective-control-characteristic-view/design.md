## Context

`ScalarCharacteristicView` (`Domain.Catalogue.CharacteristicView.cs`) already exists — added
unconsumed by `introduce-characteristic-domain-model` — but today only has its bare positional
constructor (`OriginalValue`, `DerivedValue`, `ContributingAbilities`), no `Resolved`/`Caveated`
factories or implicit `int` conversion. `InvulnerableSaveCharacteristicView` already has that full
shape and is the proven template: its `Resolved(value)`/`Resolved(value, abilities)`/
`Caveated(value, ability)` factories and implicit `int` operator are what
`unify-invulnerable-save-characteristic-view` added when InSv became the first real consumer. This
change repeats that exact recipe for `ScalarCharacteristicView`/`Statline.Oc`, the second and (per
today's catalogue) last plain-value characteristic with a live mutation source
(`VexillaStatlineFlagRule`).

`Statline.Oc` is currently `int`, constructed positionally at every real call site (both BSData and
BattleScribe mappers' `MapStatline`, every `Examples/Datasheets.cs` fixture) — none of these
construct `Oc` from a variable typed anything other than `int`, so the retype's only required
change at those call sites is that the value now flows through an implicit conversion instead of
assigning directly. `VexillaStatlineFlagRule.Apply` is the one place that reads and reassigns `Oc`
today (`baseStatline.Oc + 1`), and `LivePlayModel.GroupStatlines`/`AttachedUnitAggregator
.ApplyStatlineFlagRules` are the two places that currently route around `Oc`'s missing
`ContributingAbilities` via the separate `StatlineFlag` side-channel.

See proposal.md for the full motivation and file-level impact list.

## Goals / Non-Goals

**Goals:**
- Give `ScalarCharacteristicView` its first real consumer via `Statline.Oc`, exactly mirroring
  `InvulnerableSaveCharacteristicView`'s proven factory/conversion shape.
- Retire `StatlineFlag`/`StatlineFlagCharacteristic`/`AggregateStatlineEntry.Flags` outright, since
  Oc migrating removes their only remaining non-null case.
- Keep `VexillaStatlineFlagRule`'s and `_UnitBlock.cshtml`'s change surface to exactly what the
  retype requires — no new abstraction beyond the `ScalarCharacteristicView` factories themselves.

**Non-Goals** (see proposal.md's own "Explicitly out of scope" for the full list and why none of
these have a real caller yet):
- The general modification/legality engine (Set→Multiply→Add→Divide→Subtract→round-up ordering,
  per-characteristic clamp bounds).
- `CharacteristicModifier`/mutator-rule generalization, or widening `StatlineFlagRule.Apply`'s
  signature to accept `WeaponProfile`s.
- `WeaponProfile.S`/`Ap` dice-notation support.
- Set-vs-Set conflict resolution (keeping the better of two `Set`-type mutations) — today's
  catalogue never has two rules target the same characteristic.

## Decisions

### D1: `ScalarCharacteristicView` factories mirror `InvulnerableSaveCharacteristicView` exactly
Add `Resolved(CharacteristicValue value)` (forwards to `Resolved(value, [])`), `Resolved
(CharacteristicValue value, IReadOnlyList<Ability> contributingAbilities)`, `Caveated
(CharacteristicValue value, Ability caveatAbility)`, and `public static implicit operator
ScalarCharacteristicView(int uniformValue) => Resolved(uniformValue)` — the int converts to
`CharacteristicValue` first via that type's own existing implicit operator, then to
`ScalarCharacteristicView` via this new one, so a plain-int construction call site (every mapper's
`MapStatline`, every `Examples/Datasheets.cs` fixture) needs no change beyond the type migrating.
No `None` preset is added: unlike `InvulnerableSave.None`, there is no real call site constructing
an "absent" Oc — a Datasheet's own Oc value is always a real catalogue int today.

**Alternative considered**: a generic `ScalarCharacteristicView.Resolved<T>` templated on
`CharacteristicValue`'s three subtypes. Rejected — `InvulnerableSaveCharacteristicView` doesn't do
this either (its `Resolved`/`Caveated` take the concrete `InvulnerableSave` type directly), and
`CharacteristicValue`'s own implicit conversions already handle every real construction shape
(`int`, `DiceExpression`) at the call site; a generic factory would just re-solve a problem the
value type's own conversions already solve.

### D2: `VexillaStatlineFlagRule` downcasts `Oc.Value` to `NumericCharacteristicValue` to do arithmetic
`ScalarCharacteristicView.Value` returns the abstract `CharacteristicValue` base type, which exposes
no arithmetic of its own (deliberately — see `introduce-characteristic-domain-model`'s Decision 1
and this project's repeated rejection of premature behavior on these value types, most recently
`unify-invulnerable-save-characteristic-view`'s reverted `ComputeDerivedValue`). `Vexilla`'s own
`Apply` needs a plain int to add 1 to, so it downcasts explicitly:

```csharp
public override Statline Apply(Statline baseStatline, Ability matchedAbility)
{
    var current = ((NumericCharacteristicValue)baseStatline.Oc.Value).Value;
    return baseStatline with
    {
        Oc = ScalarCharacteristicView.Resolved(current + 1, [matchedAbility])
    };
}
```

This is safe under today's real data: Objective Control has only ever been parsed as a plain int by
both mapper pipelines (`ParsePlainInt`, never `DiceExpression`/symbolic) — `Statline.Oc` was itself
a bare `int` before this change, so no dice/symbolic Oc has ever existed in this codebase. An
invalid cast here would mean OC data genuinely changed shape, which is a loud, fail-fast signal
exactly matching this project's convention against defensive validation for scenarios that can't
happen (root `CLAUDE.md`).

**Alternative considered**: add a `Kind`-driven `Improve`/`Worsen` operation on `CharacteristicValue`
or `ScalarCharacteristicView` itself, per the parked general-engine sketch in `.claude/vnext-ideas
.md`. Rejected for this change specifically — that sketch is designed for a modification engine
with zero real callers today (see proposal.md's "Explicitly out of scope"); building it now for
Vexilla's single `+1` would be exactly the premature-behavior mistake this project has already
caught and reverted twice on these same types.

### D3: `StatlineFlag`/`StatlineFlagCharacteristic`/`AggregateStatlineEntry.Flags` are deleted, not deprecated
`StatlineFlagRule.Characteristic` (`StatlineFlagCharacteristic?`) exists specifically as the "this
rule still needs a `StatlineFlag`" escape hatch — `ShieldDomeStatlineFlagRule.Characteristic` is
already `null` because InSv self-describes via its own `CharacteristicView`. Once Vexilla's
`Characteristic` also becomes unreachable (Oc self-describes the same way), there is no remaining
non-null case anywhere in `StatlineFlagRuleCatalogue.All`, so the property, the enum, the record,
and `AttachedUnitAggregator.ApplyStatlineFlagRules`'s flag-building branch are removed outright
rather than left as dead code — mirrors how `InvulnerableSave.Caveated`/`.CaveatAbility` were
removed outright (not deprecated) by the InSv unification.

`StatlineFlagRule.Apply`'s own signature (`Statline baseStatline, Ability matchedAbility) ->
Statline`) is unchanged — only `.Characteristic` goes away.

### D4: `LivePlayModel.GroupStatlines` reads `statline.Oc.ContributingAbilities` directly, same as InSv
Replaces:
```csharp
var ocSource = g.SelectMany(e => e.Flags)
    .FirstOrDefault(f => f.Characteristic == StatlineFlagCharacteristic.ObjectiveControl)
    ?.SourceAbility;
```
with:
```csharp
var ocSource = statline.Oc.ContributingAbilities.FirstOrDefault();
```
— byte-for-byte the same shape as the existing `insvSource` line immediately below it. OC and InSv
now read through one uniform mechanism with no per-characteristic branch, the exact outcome
`unify-invulnerable-save-characteristic-view`'s own proposal called out as the reason to do this
for Oc too.

### D5: `_UnitBlock.cshtml` renders `.Oc.Value` and lets `CharacteristicValue.ToString()` do the work
Both existing render sites (`@(block.Statline.Oc)`) become `@(block.Statline.Oc.Value)`. Razor's
`@()` interpolation calls `.ToString()` on the result; `NumericCharacteristicValue.ToString()`
already returns `Value.ToString()`, so the rendered markup is byte-identical to today's — no visual
change, confirmed by the existing `LivePlayFlaggedStatlineRenderingTests`/battle-shock rendering
tests continuing to assert on the same literal HTML strings (`">OC*<"`, etc.).

## Risks / Trade-offs

- **[Risk] A future non-numeric Oc value would throw an unhandled `InvalidCastException` inside
  `VexillaStatlineFlagRule.Apply`** → Accepted per D2: no such data exists today, and BSData's own
  Objective Control field has never been anything but a plain int across the full corpus scans this
  project already runs. If a future corpus scan ever surfaces a dice/symbolic OC, that's new
  information requiring its own follow-up, not something to defensively guard against now with no
  concrete failure case.
- **[Risk] Deleting `StatlineFlagCharacteristic`/`StatlineFlag` is a breaking change for any test or
  external caller still constructing them** → Mitigated: this is a small, closed codebase (this
  project) with exactly two known construction sites (`AttachedUnitAggregator
  .ApplyStatlineFlagRules` and the three `LivePlayFlaggedStatlineRenderingTests` cases), both
  covered by tasks.md.

## Post-Implementation Refinements

All three raised by direct user review after the tasks above were already marked done, not self-caught —
same pattern `unify-invulnerable-save-characteristic-view`'s own "Post-Review Fixes" section
documents, and the same lesson [[feedback_avoid_premature_behavior_on_unconsumed_domain_types]]
already names: a clever-looking factory call isn't trustworthy until challenged.

1. **Real regression, fixed**: D2's `Resolved(current + 1, [matchedAbility])` (and the equivalent in
   `ShieldDomeStatlineFlagRule.Apply`) passed only the newly-computed value to the 2-arg `Resolved`,
   which sets `OriginalValue` and `DerivedValue` to the *same* value — silently discarding the true
   pre-mutation catalogue value (Oc's real base of 2, or InSv's real base of `None`) the moment a
   rule mutated it. This directly blocks a real, named future need: a planned "tap the stat-tile to
   see the original catalogue value" affordance requires `OriginalValue` to survive every mutation
   applied on top of it, not just report whatever the most recent rule computed. Fixed by adding a
   3-arg `Resolved(originalValue, derivedValue, contributingAbilities)` overload to both
   `ScalarCharacteristicView` and `InvulnerableSaveCharacteristicView`, and having both
   `StatlineFlagRule`s read `baseStatline.Oc.OriginalValue`/`baseStatline.InSv.OriginalValue` (never
   the effective `.Value`) to seed it. Reading `OriginalValue` rather than `.Value` also makes this
   correct through a future chain of stacked rules on one characteristic: each rule computes its own
   delta against the current *effective* value, but must thread the *original* forward unchanged, so
   the true catalogue value never gets re-anchored to an intermediate rule's own output.
2. **The 2-arg `Resolved(value, contributingAbilities)` overload was removed outright, on both
   types** — once fixed, no production call site was left calling it with a real (non-empty) ability
   list: every real mutation now goes through the 3-arg form, and every no-mutation construction
   already goes through the 1-arg `Resolved(value)` convenience form. Its only remaining callers were
   test fixtures constructing a pre-flagged view directly, using it as a shortcut in exactly the same
   collapsed-original-equals-derived shape that caused the bug above — keeping it available would
   have kept inviting the same mistake for a future rule author with no forcing function to think
   about original vs. derived. `Resolved(value)` now forwards straight to the 3-arg form
   (`Resolved(value, value, [])`). Every affected test (`LivePlayFlaggedStatlineRenderingTests.cs`'s
   four fixtures, `CharacteristicViewTests.cs`'s
   `InvulnerableSave_ResolvedWithContributingAbilities_IsNotCaveated`) was rewritten to state an
   explicit, distinct original value via the 3-arg form rather than relying on the removed shortcut.
3. **`ScalarCharacteristicView`'s own 1-arg `Resolved(value)` was also removed**, on a closer look at
   its actual callers: unlike `InvulnerableSaveCharacteristicView.Resolved(value)` (which has real
   direct callers - `Examples/Datasheets.cs`, a `CharacteristicViewTests.cs` test - independent of any
   mutation concern), grepping every `ScalarCharacteristicView.Resolved(...)` call site showed exactly
   one caller of the 1-arg form: the implicit `int` operator itself. Nothing else in the codebase
   constructs a `ScalarCharacteristicView` from a single already-known value, so the overload existed
   solely to serve its own operator - removed, with the operator now calling the 3-arg form directly
   (`Resolved(uniformValue, uniformValue, [])`). `InvulnerableSaveCharacteristicView.Resolved(value)`
   was deliberately left alone since it has real, independent callers unrelated to this change.

## Migration Plan

Single-PR change, no runtime data migration (no persisted `Statline`/`Oc` state exists — every
`/LivePlay` request rebuilds the `ArmyRoster` fresh from session-stored `StoredArmyImport`, per
`domain-model-11e.md`'s "Session-Backed Import"). Build order: `ScalarCharacteristicView` factories
first (D1) → `Statline.Oc` retype → `VexillaStatlineFlagRule` (D2) → delete the `StatlineFlag` side-
channel (D3) → `LivePlayModel` (D4) → `_UnitBlock.cshtml` (D5) → fix tests. No feature flag; the
compiler enforces every call site is updated before the build succeeds.
