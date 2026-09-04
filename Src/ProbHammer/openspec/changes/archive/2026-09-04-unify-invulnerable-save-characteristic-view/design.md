## Context

See `proposal.md` for motivation. Current state, precisely:

- `InvulnerableSave` (`Domain/Catalogue/InvulnerableSave.cs`) — `MeleeInSv`, `RangedInSv`,
  `Caveated`, `CaveatAbility`; a 4-arg constructor throws if `Caveated` is true with no
  `CaveatAbility`. `Statline.InSv` is typed `InvulnerableSave` directly.
- `BsdataDatasheetMapper.ResolveInvulnerableSave`/`ResolveCaveatAbility` compute
  `(int melee, int ranged, bool caveated, Ability? caveatAbility)` internally (footnote parsing,
  `InvulnerableSaveCaveatClassifier` template matching) and construct the final `InvulnerableSave`
  at the end of that computation. `BattleScribeRosterMapper` does the same, independently, via a
  simpler resolution with no entryLink ancestry to walk.
- `StatlineFlagRule.Apply(Statline) -> Statline` does raw arithmetic/replacement with no access to
  the matched `Ability` itself — only `AttachedUnitAggregator.ApplyStatlineFlagRules` (the caller)
  has that, via `AggregateAbilityEntry.Ability`.
- `AggregateStatlineEntry.Flags: IReadOnlyList<StatlineFlag>` records `(Characteristic,
  SourceAbility)` pairs, populated by `ApplyStatlineFlagRules` for *every* rule match regardless of
  target characteristic (today: `InvulnerableSave` from Shield Dome, `ObjectiveControl` from
  Vexilla).
- `LivePlayModel.GroupStatlines` picks InSv's flag source with
  `statline.InSv.Caveated ? statline.InSv.CaveatAbility : flags.FirstOrDefault(...).SourceAbility`
  — the catalogue-caveat path always wins over a live rule match for InSv specifically (untested in
  practice: no known real Datasheet has both a caveated base InSv and a live InSv-granting ability
  simultaneously). OC has no catalogue-level caveat concept, so it only ever reads from `Flags`.
- `_UnitBlock.cshtml` reads `block.Statline.InSv` directly (`inv.Caveated`, `inv.MeleeInSv`,
  `inv.RangedInSv`) to render the InSv tile.
- `CharacteristicView`/`InvulnerableSaveCharacteristicView` (`introduce-characteristic-domain-model`,
  archived) are unconsumed today: abstract base carries `ContributingAbilities`
  (`IReadOnlyList<Ability>`) plus `abstract bool IsCaveated { get; }`;
  `InvulnerableSaveCharacteristicView(OriginalValue, DerivedValue, ContributingAbilities)` is a
  sealed subtype with `IsCaveated => DerivedValue is null`. Pure data — no method computes
  `DerivedValue`; it's always supplied at construction.

## Goals / Non-Goals

**Goals:**
- `Statline.InSv` becomes the single source of truth for "is this unit's invulnerable save the
  Datasheet's own base value, and if not, which ability(ies) are why" — whether that fact originates
  at catalogue-parse time or is computed live from currently-present abilities.
- `InvulnerableSave` itself carries no caveat concept — it's purely `(MeleeInSv, RangedInSv)`.
- No change to `InvulnerableSaveCaveatClassifier`'s own template-matching logic, or to
  `BsdataDatasheetMapper`'s footnote-parsing/ancestry-walking mechanics — only to what shape the
  final result is packaged into.
- No observable rendering-behavior change for a player — the marker/legend mechanism looks and
  behaves identically; only its internal data source changes.

**Non-Goals:**
- Objective Control (Vexilla) is NOT wrapped in a `CharacteristicView` by this change — `Statline.Oc`
  stays a plain `int`, `StatlineFlag`/`AggregateStatlineEntry.Flags` stay exactly as they are for OC.
  Only InSv moves to the new shape. Generalizing OC (or any other characteristic) is a future,
  separate change once this one proves the pattern.
- No modification/legality engine, no clamp table, no multi-modifier stacking/ordering — out of
  scope per `introduce-characteristic-domain-model`'s own Non-Goals, unaffected by this change. A
  characteristic ever having *more than one* simultaneous contributing ability (e.g. both a
  catalogue caveat AND a live rule match on the same run) is not handled specially — today's code
  doesn't handle that case either (the `Caveated ? ... : ...` branch picks one deterministically),
  and no known real data exercises it.
- No change to `WeaponProfile.S`/`Ap`, `Statline.M/T/W/Ld/Oc`, or any other characteristic's
  representation — this change is scoped to InSv only, per `introduce-characteristic-domain-model`'s
  own `vnext-ideas.md` follow-up note recommending a narrow, single-characteristic stress test
  before any broader rollout.

## Decisions

**1. `InvulnerableSave` strips to a plain value; the view wraps it.**

```csharp
public sealed record InvulnerableSave(int MeleeInSv, int RangedInSv)
{
    public static implicit operator InvulnerableSave(int uniformValue) => new(uniformValue, uniformValue);
}
```

No constructor guard needed — there's no invariant left to protect (the old guard existed
specifically to keep `Caveated`/`CaveatAbility` in sync, which no longer exist here). Every existing
caller passing a 2-arg or implicit-int `InvulnerableSave` is unaffected; every 4-arg caller (the
`Caveated`/`CaveatAbility` construction sites - the mapper, the fixtures, `ShieldDomeStatlineFlagRule`)
must be updated - full list in `proposal.md`'s Impact section.

**2. `Statline.InSv`'s type becomes `InvulnerableSaveCharacteristicView`, not `InvulnerableSave`.**

```csharp
public sealed record Statline(int M, int T, int Sv, int W, int Ld, int Oc)
{
    public InvulnerableSaveCharacteristicView InSv { get; init; } =
        new(new InvulnerableSave(0, 0), new InvulnerableSave(0, 0), []);
    // ...
}
```

Alternative considered: keep `Statline.InSv: InvulnerableSave` (plain) and have the *aggregate* layer
(`AttachedUnitAggregator`) wrap it into a view only at render time, alongside `AggregateStatlineEntry`.
Rejected: the catalogue-level footnote caveat is resolved once, at Datasheet-build time, from data
that's fixed for the life of the process (unlike a `StatlineFlagRule` match, which genuinely depends
on live roster state) - there's no reason to defer packaging a fact that's already fully known.
Storing the view directly on `Statline` also means `ShieldDomeStatlineFlagRule.Apply` has one
consistent target shape to replace, rather than two different InSv representations depending on
where in the pipeline you are.

A view's default (no invulnerable save at all) needs an explicit value - unlike the old
`InvulnerableSave`'s parameterless constructor, `InvulnerableSaveCharacteristicView` has no
zero-arg constructor (it's a `sealed record` with a positional primary constructor only), so
`Statline.InSv`'s default must be spelled out explicitly as shown above (now via `Resolved`,
below).

**Post-implementation refinement (2): named static factories for the two common shapes.**
Raised by the user during review: the "resolved" shape
(`new(value, value, [])`) turned out to already be duplicated verbatim as a private helper in
*both* `BsdataDatasheetMapper` and `BattleScribeRosterMapper`, plus hand-rolled a third time in an
`Examples/Datasheets.cs` fixture - genuine production duplication, not just test-file noise. Added
`InvulnerableSaveCharacteristicView.Resolved(value)` / `.Caveated(value, ability)` as named static
factories (not positional constructor overloads - a bare 2-arg `new(value, ability)` overload
wouldn't self-document whether it means "caveated" or something else, whereas the factory name
states its own semantics, matching `DiceExpression.Fixed`'s existing convention in this codebase).
Both mapper files' private helpers now delegate to these instead of duplicating the construction
logic; `Statline.InSv`'s default and every "resolved, no caveat" fixture/test call site were
updated to use `Resolved` too.

**Post-implementation refinement (2b): `Resolved` generalized to a 2-arg overload rather than
inventing a third factory.** `ShieldDomeStatlineFlagRule`'s own shape (`OriginalValue ==
DerivedValue`, one contributing ability) was first treated as a third, distinct combination not
worth its own factory for a single call site. Raised again by the user: "resolved" and "has
contributing abilities" are actually independent facts (`IsCaveated` is defined purely by whether
`DerivedValue` is set) - a matched, fully-understood `StatlineFlagRule` is resolved by definition
(never caveated), it just also has a contributing ability recorded. Correcting the earlier framing:
added `Resolved(InvulnerableSave value, IReadOnlyList<Ability> contributingAbilities)` as a second
overload of `Resolved` (the 1-arg form now forwards to it with `[]`) rather than a separate named
factory - `ShieldDomeStatlineFlagRule.Apply` and the one test constructing the same shape both use
it directly.

**Post-implementation refinement (3): a `None` static preset for the "absent" case.** Raised by
the user during review, same evidence-gathering approach as (2): `InvulnerableSave(0, 0)`/
`InvulnerableSaveCharacteristicView.Resolved(new InvulnerableSave(0, 0))` turned out to recur at
several real production call sites (both mappers' "no InSv text at all" and "explicitly N/A"
branches, `Statline.InSv`'s own default) - not an arbitrary value, but a specific, named domain
concept ("no invulnerable save") already described that way in this file's own prose. Added
`InvulnerableSave.None` and `InvulnerableSaveCharacteristicView.None` as static readonly presets,
mirroring `DiceExpression.D3`/`D6`'s own established convention for exactly this shape (a specific,
frequently-constructed value, not the general case `Resolved`/`Fixed` cover). Every real `(0, 0)`
call site updated to use `None` instead.

**3. `BsdataDatasheetMapper`/`BattleScribeRosterMapper` build the view at their existing return
point - no restructuring of the resolution logic itself.**

Both mappers already compute an internal `(melee, ranged, caveated, caveatAbility)` tuple via their
existing footnote/template logic; only the final construction changes:

```csharp
// was: return new InvulnerableSave(melee, ranged, caveated, caveatAbility);
var original = new InvulnerableSave(melee, ranged);
return new InvulnerableSaveCharacteristicView(
    OriginalValue: original,
    DerivedValue: caveated ? null : original,
    ContributingAbilities: caveatAbility is not null ? [caveatAbility] : []);
```

**4. `StatlineFlagRule.Apply` gains the matched `Ability` as a parameter - applied uniformly across
every rule, even ones that don't need it.**

```csharp
public abstract Statline Apply(Statline baseStatline, Ability matchedAbility);
```

`ShieldDomeStatlineFlagRule.Apply` uses it:

```csharp
public override Statline Apply(Statline baseStatline, Ability matchedAbility)
{
    var value = new InvulnerableSave(5, 5);
    return baseStatline with
    {
        InSv = new InvulnerableSaveCharacteristicView(value, value, [matchedAbility])
    };
}
```

`VexillaStatlineFlagRule.Apply` ignores it (OC stays a plain `int`, unaffected by this change):

```csharp
public override Statline Apply(Statline baseStatline, Ability matchedAbility) =>
    baseStatline with { Oc = baseStatline.Oc + 1 };
```

Alternative considered: special-case InSv inside `AttachedUnitAggregator.ApplyStatlineFlagRules`
instead (check `rule.Characteristic == StatlineFlagCharacteristic.InvulnerableSave` and build the
view there, leaving `Apply`'s signature untouched). Rejected - `StatlineFlagRule` is meant to fully
own its own mutation (each rule is a closed, self-contained unit matching one ability), and the
aggregator already treats every rule uniformly; special-casing one characteristic there would break
that uniformity and require the aggregator to know about `InvulnerableSaveCharacteristicView`
specifically. An unused parameter on `VexillaStatlineFlagRule.Apply` is a smaller cost than that.

**5. `StatlineFlagCharacteristic.InvulnerableSave` and `AggregateStatlineEntry.Flags`'s
InvulnerableSave-producing path are removed, not left dormant.**

`ApplyStatlineFlagRules` stops adding an InSv-targeting `StatlineFlag` (that information now lives
directly on the returned `Statline.InSv` view's own `ContributingAbilities`). Keeping the dead
enum value/code path around "just in case" would leave two competing ways to ask "why is this InSv
what it is" - exactly the duplication this change exists to remove. `StatlineFlagCharacteristic`
keeps its `ObjectiveControl` value (Vexilla's own path is untouched).

**6. Rendering (`LivePlayModel.GroupStatlines`, `_UnitBlock.cshtml`) reads the view directly - the
two-path branch collapses to one.**

```csharp
// was: statline.InSv.Caveated ? statline.InSv.CaveatAbility : flags.FirstOrDefault(...)?.SourceAbility
var insvSource = statline.InSv.ContributingAbilities.FirstOrDefault();
```

The marker/legend trigger condition is "has a contributing ability at all," not "`IsCaveated`" -
Shield Dome's match is never caveated (its `DerivedValue` is always set) but still needs a
marker/legend, exactly like today. `_UnitBlock.cshtml`'s tile body needs the melee/ranged numbers
actually displayed - `OriginalValue` while caveated (the one raw value known even when uncertain,
same as today's `Caveated` branch), `DerivedValue` once resolved.

**Post-implementation refinement** (raised by the user during review): that
`IsCaveated ? OriginalValue : DerivedValue!` selection was first written inline in
`_UnitBlock.cshtml`. Moved onto each `CharacteristicView` subtype as a `Value` property instead -
`ScalarCharacteristicView.Value`/`InvulnerableSaveCharacteristicView.Value`, each typed to its own
subtype's `OriginalValue`/`DerivedValue` (not declared on the abstract base, for the same reason
`OriginalValue`/`DerivedValue` themselves aren't - the type genuinely differs per subtype). This is
NOT a reprise of the rejected `ComputeDerivedValue` (Decision 2 above): `Value` needs no external
classification input and computes nothing new - both operands are already on the record, exactly
the same complexity class as `IsCaveated` itself. Leaving the selection inline at the one call site
would have left the "which value to actually use" rule - a fact about the view's own shape, not
about rendering - to be silently reimplemented by every future caller, the same duplication risk
this whole change exists to remove.

## Risks / Trade-offs

- **`VexillaStatlineFlagRule.Apply` carries an unused `matchedAbility` parameter.** Accepted per
  Decision 4 - keeps every rule's `Apply` fully self-contained and the aggregator's call site
  uniform, at the cost of one unused parameter on the one rule that doesn't need it.
- **A characteristic with two simultaneous contributing abilities isn't specially handled.** If a
  future Datasheet ever has both a caveated base InSv AND a live InSv-granting `StatlineFlagRule`
  match, `ShieldDomeStatlineFlagRule.Apply`'s `baseStatline with { InSv = ... }` fully replaces the
  incoming view (including its `ContributingAbilities`) rather than merging the two - matching
  today's `Caveated ? ... : ...` precedence (whichever runs last wins), not a defined stacking rule.
  No known real data exercises this; flagged rather than silently assumed away.
- **Breaking change surface is real** (`proposal.md`'s Impact section lists ~13 files) - mitigated by
  every call site being mechanical (construct the view instead of the flat value; read
  `.OriginalValue`/`.ContributingAbilities` instead of `.MeleeInSv`/`.Caveated`), not a logic
  rewrite, and by the full existing test suite (`InvulnerableSaveResolutionTests`,
  `StatlineFlagRuleTests`, `LivePlayInvulnerableSaveRenderingTests`,
  `LivePlayFlaggedStatlineRenderingTests`, the corpus-scan test) catching any behavioral drift.

## Post-Review Fixes

A `/code-review high` pass over the finished diff found two real, in-scope defects this
implementation introduced, plus smaller cleanup items - all fixed:

- **Record-equality regression (real bug, fixed).** `CharacteristicView` gained a custom
  `Equals`/`GetHashCode` override (comparing `ContributingAbilities` via `SequenceEqual`, not the
  compiler-generated per-field comparison). Without this, `LivePlayModel.GroupStatlines`'s
  run-merging check (`currentGroup[0].Statline == entry.Statline`) would silently stop merging two
  otherwise-identical rows whenever their InSv held the same ability in two independently-constructed
  list instances (e.g. two model-lines both caveated by the same footnote, or both flagged by
  `ShieldDomeStatlineFlagRule` - each builds its own `[ability]` array) - arrays/`List<T>` never
  override `Equals`, so the old auto-generated equality compared them by reference. The override
  lives once on the abstract base; both sealed subtypes' own auto-generated `Equals` already call
  `base.Equals(other)` for the inherited portion, so this fixes both (and any future subtype) for
  free. Covered by two new tests in `CharacteristicViewTests.cs` confirming content-equal-but-
  different-list-instance views compare equal, and differently-contributed ones don't.
- **Missing invariant enforcement (real gap, fixed).** The old `InvulnerableSave` 4-arg constructor
  threw if caveated with no ability; nothing carried that guard onto
  `InvulnerableSaveCharacteristicView`. Both `ScalarCharacteristicView`/`InvulnerableSaveCharacteristicView`
  now redeclare their own `DerivedValue` property with a validating initializer
  (`DerivedValue is null && ContributingAbilities.Count == 0 ? throw ... : DerivedValue`) - the
  standard idiom for adding a cross-field invariant to a positional record without losing positional
  construction syntax at existing call sites. This exposed that two of this session's own tests
  (`Scalar_NullDerivedValue_IsCaveated`, `InvulnerableSave_NullDerivedValue_IsCaveated`) had been
  constructing exactly the invalid state as "normal" test data - fixed to supply a contributing
  ability, plus new `..._Throws` tests confirming the guard.
- **Residual duplication (fixed).** `ResolvedInSv`/`CaveatedInSv` were still private per-mapper
  helpers after Decision 2's promotion - each forwarded to the shared factory instead of building the
  view directly, but the wrapper itself was still duplicated verbatim in both files. Added
  `Resolved(int, int)`/`Caveated(int, int, Ability)` overloads directly on
  `InvulnerableSaveCharacteristicView`; both mappers' private helpers deleted, call sites now call the
  view's own static factories.
- **Long narrative comments (fixed).** Several doc comments (`IsCaveated`, the old `Create`-removal
  note) recounted rejected earlier drafts across multiple paragraphs, against the user's own global
  CLAUDE.md comment rule. Trimmed to one line each; the design history stays in this file, not
  permanently embedded in source.
- **Duplicated `Value` ternary (fixed).** `ScalarCharacteristicView.Value`/
  `InvulnerableSaveCharacteristicView.Value` both implemented the identical
  `IsCaveated ? OriginalValue : DerivedValue!` expression. Factored into a shared
  `protected static T SelectValue<T>(...)` helper on the abstract base; each subtype's `Value` is now
  a one-line call.
- **Test coverage gap (fixed).** `InvulnerableSaveResolutionTests.cs`'s non-caveated cases asserted
  `.OriginalValue` but never `.DerivedValue`/`.Value` - the value actually rendered. Added `.Value`
  assertions to every non-caveated case.
- **Nullable `StatlineFlagRule.Characteristic` as a fragile fork point (accepted, not changed).**
  Flagged as a real future-maintainability risk (a third rule author could get `Characteristic`
  wrong with nothing catching it at compile time), but `statline-flag-rules`' own capability already
  states the mitigation for this class of risk explicitly: "no general ability-text parsing engine...
  a real third rule joins this list directly" - i.e. human review at the point a third rule is added,
  not a compile-time guarantee. Adding one now would be speculative complexity against a rule that
  doesn't exist yet.
- **`live-play.js` keyword-click double-scan (out of scope, left as-is).** A real, separate finding
  from an earlier, already-committed feature (`live-play-landscape-only`), not this change. Confirmed
  by direct inspection that today's code is correct, not buggy - it's two independent DOM scans
  checking two genuinely different predicates (`activeKeywordFilters.has(...)` vs. `=== key`), which
  only look like the same scan at a glance. The reviewer's own suggested fix (reuse `applyKeywordHighlighting`'s
  `anyFlagged`) would have been an actual regression under multiple simultaneous active filters,
  since the two predicates diverge exactly then. A correct fix exists (build a `firstMatchByKey` map
  during the one shared scan instead of a boolean), but the real-world cost of the current double
  scan is imperceptible at this app's real roster sizes - left as-is by explicit user decision rather
  than spending the complexity for a savings nobody would notice.
