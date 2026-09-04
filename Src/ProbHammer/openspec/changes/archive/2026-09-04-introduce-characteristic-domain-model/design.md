## Context

See `proposal.md` for motivation. Current state, precisely:

- `Statline(M, T, Sv, W, Ld, Oc)` — six plain `int` properties; `InSv` is bolted on separately as
  `InvulnerableSave` (melee/ranged split, `Caveated`/`CaveatAbility` for a single unresolved
  ability — see `InvulnerableSave.cs`).
- `WeaponProfile(Range, A, S, Ap, D)` — `A`/`D` are `DiceExpression`, `S`/`Ap` are plain `int`, an
  inconsistency confirmed real by the corpus scan (`CharacteristicResolutionAllowlist.cs` skips
  dice-notation `S` — Ork Battlewagon `D6+6` — rather than parsing it).
- `StatlineFlagRule.Apply(Statline) -> Statline` does raw, hand-written arithmetic per rule
  (`ShieldDomeStatlineFlagRule`, `VexillaStatlineFlagRule` — the only two that exist). Nothing
  retains the pre-mutation value or a per-characteristic list of contributing abilities; `Flags`
  (`AttachedUnitAggregateView.cs`) records exactly one `SourceAbility` per characteristic.
- Everything in `Roster` that derives a live view over casualty state does so as a pure function,
  recomputed on every read, never mutating `Datasheet`/`Unit` (`KeywordResolution`,
  `HalfStrengthResolution`, `ToughnessResolution`, and `StatlineFlagRule`'s own doc comment all
  state this explicitly). This design follows the same convention.
- Checked during this exploration: C# does not yet have native discriminated unions. They land as
  the `union` keyword in **C# 15 / .NET 11**, first preview ~April 2026, GA targeted **November
  2026** — still preview-quality as of this writing, and this project targets `net8.0`/C# 12. This
  design does not wait for or depend on that feature; it uses the abstract-record-with-sealed-
  subtypes pattern already established by `WeaponProfile`/`RangedWeapon`/`MeleeWeapon`, which gives
  the same "impossible states unrepresentable" guarantee today.

## Goals / Non-Goals

**Goals:**
- Give every characteristic property (on `Statline` and `WeaponProfile` alike) one shared shape
  for: its raw value (numeric, dice, or symbolic), its original catalogue value, the abilities
  classified as touching it, and a derived value when fully resolvable.
- Make the "all contributors must be understood, or the value is caveated" rule (the generalization
  of today's `InvulnerableSave.Caveated`) a property of this shape, not something each future rule
  reimplements.
- Allow a characteristic's value to be a compound shape (not a bare scalar) where the real rules
  require it — proven via `InvulnerableSave`'s existing melee/ranged split, formalized as the first
  instance of the pattern.

**Non-Goals (this change):**
- No modification/legality engine. The rulebook's set→multiply→add→divide→subtract→round-up
  precedence, and the per-kind post-modifier clamp table (M ≥ 1", Ld ∈ [4,9], WS/BS ∈ [2,7], etc.),
  are real and were discussed at length, but computing a derived value is out of scope here — this
  change only defines the shape that *would hold* a derived value once something computes one.
- No Movement/Fly compound value shape — its exact fields aren't designed yet; deferred to a
  follow-up change (see "Resolved During Review" below).
- No wiring into `Statline`, `WeaponProfile`, `StatlineFlagRule`, `AttachedUnitAggregator`, or any
  `/LivePlay` rendering. All new types are added in isolation, unreferenced by existing code, so
  the shape can be reviewed before any refactor is attempted.
- No answer to *where* a `ContributingAbilities` classification comes from — that's the paused
  prose-classification schema and the structural `BsModifier` extraction (both tracked separately
  in `.claude/vnext-ideas.md`); this change only defines what the classification's *output* would
  be attached to.
- No handling of the WS/BS redirect rule ("modifying a model's WS/BS modifies its weapons' Skill,
  not a Statline field") — noted as real and relevant, but a targeting/ownership question for the
  eventual modification engine, not the value shape itself.
- No decision on how a modified `WeaponProfile` characteristic renders in `/LivePlay` (a `*`-marker
  stat cell mirroring the Statline tile treatment, vs. a `.weapon-tag`-style pill) — deliberately
  left open per the proposal's own "cross that bridge later."

## Decisions

**1. `CharacteristicValue`: a closed hierarchy, not a single `object`/type-tag field.**

```csharp
public abstract record CharacteristicValue;
public sealed record NumericCharacteristicValue(int Value) : CharacteristicValue;
public sealed record DiceCharacteristicValue(DiceExpression Value) : CharacteristicValue;
public sealed record SymbolicCharacteristicValue(string Symbol) : CharacteristicValue; // "-", "*", "N/A"
```

Mirrors `WeaponProfile`'s existing abstract-plus-sealed-subtypes pattern. The rulebook's "a `-`/
`*`/`N/A` characteristic can never be modified" falls out structurally: there is no `Apply`/modify
member anywhere on `CharacteristicValue` in this change (no modification engine exists yet), and
when one is designed later, it would only ever be defined for the numeric/dice branches — a
`SymbolicCharacteristicValue` simply has nothing to call. Alternative considered: a single record
with a `Kind` enum and nullable `int?`/`DiceExpression?`/`string?` fields — rejected because it
allows an invalid state (more than one populated, or none) that the sealed-subtype approach makes
unrepresentable, the same reasoning `WeaponProfile` itself already documents for why it isn't one
record with a `Type` field.

`NumericCharacteristicValue` gains an implicit `int` conversion, mirroring the existing
`DiceExpression`/`InvulnerableSave` convention (`Domain.Catalogue`) that already lets a plain int
literal be written at a call site expecting either type:

```csharp
public static implicit operator CharacteristicValue(int value) => new NumericCharacteristicValue(value);
```

Declared on the abstract `CharacteristicValue` base (not just `NumericCharacteristicValue`) so a
call site can write a bare `4` wherever a `CharacteristicValue` is expected, exactly as today's
`Statline`/`WeaponProfile` constructors accept a bare `4`/`"D6"` for an `InvulnerableSave`/
`DiceExpression`-typed parameter — no behavior change to those existing types, just the same
established pattern extended to this new one.

**2. `CharacteristicView`: a shared abstract base plus concrete per-kind sealed subtypes, not a generic.**

Considered as a single generic `CharacteristicView<T>`; decided against in favor of the same
abstract-base/sealed-subtype shape `CharacteristicValue` (above) and `WeaponProfile` already use in
this codebase, since nothing in this domain is actually generic *over* the wrapped type today (no
code operates uniformly across kinds) and a concrete-per-kind shape reads at call sites without
generic-parameter noise:

```csharp
public abstract record CharacteristicView(IReadOnlyList<Ability> ContributingAbilities)
{
    public abstract bool IsCaveated { get; }
}

public sealed record ScalarCharacteristicView(
    CharacteristicValue OriginalValue,
    CharacteristicValue? DerivedValue,
    IReadOnlyList<Ability> ContributingAbilities) : CharacteristicView(ContributingAbilities)
{
    public override bool IsCaveated => DerivedValue is null;
}

public sealed record InvulnerableSaveCharacteristicView(
    InvulnerableSave OriginalValue,
    InvulnerableSave? DerivedValue,
    IReadOnlyList<Ability> ContributingAbilities) : CharacteristicView(ContributingAbilities)
{
    public override bool IsCaveated => DerivedValue is null;
}
```

Only `ContributingAbilities` is pulled onto the shared base as a constructor field — `OriginalValue`/
`DerivedValue` can't be, since their type genuinely differs per kind (that's exactly what a generic
parameter would otherwise buy), and `IsCaveated` isn't a constructor field at all: it's fully
redundant with `DerivedValue`'s own nullability (caveated exactly when `DerivedValue is null`), so
it's declared `abstract` on the base and computed per subtype instead of passed in — a type-level
guarantee that the two can never disagree, rather than a construction-time convention.
`ContributingAbilities` is many-to-many by construction — nothing here restricts one ability to one
characteristic (an ability can appear in more than one property's `ContributingAbilities`, and one
property can list several abilities). This generalizes `InvulnerableSave.Caveated`/`CaveatAbility`
(a single hardcoded ability reference) to N abilities without changing its all-or-nothing semantics
(`DerivedValue` present vs. absent, never partial credit).

**`CharacteristicView` is pure data — it computes nothing.** `DerivedValue` is always supplied at
construction, by whatever external code builds the view; this type has no method that classifies
abilities or computes a mutated value. A first draft added exactly that: a shared
`ComputeDerivedValue<T>(originalValue, contributingAbilities, Func<Ability, bool> isDeterministic)`
helper plus a per-subtype `Create` factory, so a subtype's "construction path" would run through
one shared place. Removed after review, for two reasons:

1. **It couldn't honestly do its job.** With no modification/legality engine (Non-Goal below),
   there's no mutation to apply — the helper could only ever return `originalValue` unchanged or
   `null`. A method named "compute the derived value" that cannot compute a *different* value is
   misleading about what it does, and its own doc comment had to explicitly disclaim this.
2. **A `Func<Ability, bool>` parameter on the value type pre-empts the real future design**,
   discussed directly with the user: the actual modification engine should look like
   `StatlineFlagRule`/`StatlineFlagRuleCatalogue` — a closed, catalogued set of rule objects
   external to the value type, applied by an aggregator step (mirroring
   `AttachedUnitAggregator.ApplyStatlineFlagRules`), not an arbitrary delegate threaded through the
   domain type's own constructor. Embedding a `Func` on `CharacteristicView` would also break its
   value-type cleanliness — record equality is structural, but delegate equality is reference-based,
   so two otherwise-identical views closing over different delegates would stop comparing equal.

Computed fresh at render/aggregate time from a `Datasheet`'s base value plus whichever of the unit's
currently-present abilities are classified against that characteristic — never stored, consistent
with every other pure-recompute view in `Roster` — but by a future external engine, not by this
type itself. Alternative considered: a three-state "partially resolved" model (apply the understood
contributors, flag the rest) — raised during exploration and explicitly rejected in favor of
all-or-nothing, which is simpler and matches what `InvulnerableSave` already does.

**Future direction (not this change):** the natural shape for the eventual modification engine is a
`StatlineFlagRule`-style catalogue generalized to operate on `CharacteristicValue`/kind instead of
raw `Statline` — `Matches(Ability)` plus apply logic, held in a closed list, applied by an
aggregator step that produces a `CharacteristicView` from a `Datasheet`'s base value plus a unit's
present abilities. That engine would also be the natural place to finally address the original
`vnext-ideas.md` complaint that started this whole thread — `StatlineFlagRule`'s missing
ordering/stacking/cap logic — since it would likely replace `StatlineFlagRule` outright rather than
live alongside it. Tracked as a follow-up, not scoped here.

**3. Per-kind compound value shapes are separate concrete subtypes, not a one-size-fits-all `T`.**

Confirmed during exploration: `InSv` is already a melee/ranged pair (`InvulnerableSave`), and a
`Fly` model's Movement characteristic needs a minimum-distance component alongside its normal
value. Following from Decision 2, a new compound kind is simply a new `CharacteristicView` sealed
subtype:

```csharp
ScalarCharacteristicView               // most kinds: M, T, W, Ld, Oc, Ap, D, Range, and A/S once
                                        // they use CharacteristicValue's Dice branch
InvulnerableSaveCharacteristicView     // InSv - InvulnerableSave already has exactly this
                                        // melee/ranged shape; this change formalizes it as one
                                        // instance of the general pattern rather than a one-off
```

**Out of scope for this change**: a Movement/Fly compound subtype. Its exact fields (a value plus a
minimum-move component — is the minimum always present, or only for `Fly` models; is it itself ever
a `DiceExpression`?) are not yet designed, and inventing them now risks guessing wrong. Deferred to
a follow-up change once that shape is actually worked out — this change proves the pattern with the
one already-confirmed compound shape (`InvulnerableSave`) and leaves Movement as a natural,
low-risk extension point (just another `CharacteristicView` subtype) rather than something this
change needs to unblock.

## Risks / Trade-offs

- **`OriginalValue`/`DerivedValue` field declarations, and the `IsCaveated` override, are duplicated
  per `CharacteristicView` subtype.** Decision 2's abstract-base approach only pulls
  `ContributingAbilities` onto the shared base as a constructor field, since `OriginalValue`/
  `DerivedValue` are genuinely differently-typed per subtype — a new compound kind means writing a
  new sealed record with its own copy of those two field declarations plus its one-line
  `IsCaveated => DerivedValue is null` override (mitigated for the one piece of real logic — deciding
  what `DerivedValue` actually is — which is centralized in one shared static helper both subtypes'
  construction paths call; only the field/property declarations themselves repeat, not the logic).
- **Unconsumed types can drift from what a real refactor needs.** Because this change deliberately
  adds nothing that consumes these types, there's a real risk the shape looks right in isolation
  but doesn't fit once `StatlineFlagRule`/`AttachedUnitAggregator` actually try to use it. Mitigated
  by treating this as a reviewable sketch, not a commitment — the follow-on change that wires this
  in is expected to reshape it if needed.

## Open Questions

- Should `ContributingAbilities` retain any ordering/precedence hint now (even though this change
  doesn't compute a derived value from it), or is that purely the future modification engine's
  concern? Leaning toward no — nothing in this change reads such a hint, and adding one
  speculatively risks guessing the wrong shape before the modification engine that would actually
  consume it exists.

## Resolved During Review

- **Movement/Fly compound shape**: excluded from this change (confirmed with the user) — its exact
  fields aren't designed yet; deferred to a follow-up change once that shape is worked out. This
  change proves the compound-shape pattern with `InvulnerableSave` alone.
- **Generic vs. concrete `CharacteristicView`**: resolved to the abstract-base/concrete-sealed-
  subtype shape (Decision 2) — consistent with `WeaponProfile`'s and `CharacteristicValue`'s own
  precedent in this codebase, and avoids generic-parameter noise at call sites for a domain where
  nothing is actually generic *over* the wrapped type today.
- **Implicit `int` conversion**: added to `CharacteristicValue` (Decision 1), mirroring the existing
  `DiceExpression`/`InvulnerableSave` implicit-int-conversion convention already established in
  `Domain.Catalogue`.
- **`IsCaveated` as a stored constructor field**: dropped (raised by the user) — it's fully
  redundant with `DerivedValue`'s own nullability (`IsCaveated` is true exactly when `DerivedValue`
  is `null`), so it's now an `abstract` property on `CharacteristicView`, overridden per subtype as
  `DerivedValue is null`, rather than a base-constructor parameter every subtype must also thread
  through and keep in sync by convention.
- **`ComputeDerivedValue<T>`/per-subtype `Create` factories**: added during initial implementation,
  then removed after a post-implementation critique and discussion with the user — see Decision 2's
  "`CharacteristicView` is pure data" note above for the full reasoning. `CharacteristicView` now
  computes nothing; `DerivedValue` is always supplied by whatever external code constructs the view.
