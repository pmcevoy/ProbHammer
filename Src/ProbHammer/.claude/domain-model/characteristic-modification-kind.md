# Characteristic Modification Kind

Full requirements: `openspec/changes/introduce-characteristic-modification-kind/`. The bottom
layer of the not-yet-built "Modification Engine" `.claude/vnext-ideas.md`'s "Characteristic-
modification domain hardening" entry sketches — resolves an `EffectVerb`
(`rule-effect-classification`'s `Improve`/`Worsen`/`Set`) plus a stated amount into the correct
signed mutation for a characteristic's own rulebook arithmetic family, and enforces that
characteristic's legal value bound afterward. `rule-effect-classification` deliberately stops short
of this resolution; the two hand-authored `statline-flag-rules` (Shield Dome, Vexilla) each embed
their own one-off arithmetic inline today, with no shared, tested sign/clamp logic behind either.

```
CharacteristicModificationKind        // Domain/Catalogue/CharacteristicModificationKind.cs - plain
                                       // closed enum (RollThreshold/ArmourPenetration/Plain), not a
                                       // CharacteristicValue-style abstract/sealed hierarchy - a Kind
                                       // selects behavior (which sign rule applies) with the same
                                       // int-in/int-out shape in every case, not a value that varies
                                       // in shape per case.
  RollThreshold                       // WS, BS, Sv, Ld - an "N+" die-roll bar, lower is better,
                                       // Improve subtracts/Worsen adds
  ArmourPenetration                   // AP - stored as a negative integer (root CLAUDE.md), Improve
                                       // subtracts (further from zero)/Worsen adds (toward zero,
                                       // capped at 0)
  Plain                               // M, T, W, Oc, S, and the length-valued Range characteristic -
                                       // higher is better, no inversion

CharacteristicModificationKinds.Of(characteristic) -> CharacteristicModificationKind
                                       // closed characteristic-name lookup, keyed by the same plain
                                       // strings CharacteristicEffect.Characteristic already uses.
                                       // Throws for a characteristic this component doesn't cover
                                       // rather than guessing.

CharacteristicModificationClamp.Apply(characteristic, value) -> int
                                       // separate lookup table (nullable floor/ceiling per
                                       // characteristic) - a characteristic's arithmetic family and
                                       // its clamp bound are independent facts (WS and Ld share
                                       // RollThreshold but have different bounds), so kept as two
                                       // tables, not merged. Silently caps rather than throwing; a
                                       // characteristic with no registered bound (W) passes through
                                       // unchanged.

CharacteristicModificationResolver.ResolveDelta(kind, verb, amount) -> int
                                       // pure sign arithmetic, ignorant of any current value or
                                       // bound. Set is deliberately not handled here (throws) - a
                                       // Set assigns its stated value directly with no sign
                                       // resolution, so callers skip this function entirely for that
                                       // verb.
CharacteristicModificationResolver.Resolve(characteristic, current, verb, amount) -> CharacteristicValue
                                       // the top-level entry point: returns current unchanged when
                                       // it's a symbolic value ("-"/"*"/"N/A" - see this component's
                                       // own "Symbolic Characteristic Values Are Never Modified"
                                       // requirement), otherwise resolves via ResolveDelta (skipped
                                       // for Set) and clamps, rewrapping as a
                                       // NumericCharacteristicValue.
```

**Deliberately excludes InSv and `WeaponProfile`'s Attacks/Damage.** InSv is a compound melee/ranged
`InvulnerableSave`, wrapped in its own `InvulnerableSaveCharacteristicView` - not a plain
`CharacteristicValue`/`ScalarCharacteristicView` like `Statline`'s other five scalars and
`WeaponProfile`'s four - the same reason `characteristic-modifier-caveats` already excludes InSv
from its own Field allowlist (see characteristic-modifier-caveats.md). `WeaponProfile.A`/`D` are `DiceExpression`,
never a plain scalar, in this codebase today. This was a scope correction made to this change's own
proposal/spec/design before any code was written, once starting implementation surfaced that the
original draft (which folded InSv into `RollThreshold` and used Shield Dome as a second proving
example) doesn't type-check against `InvulnerableSaveCharacteristicView`'s actual shape - see that
change's own design.md Context/Decisions for the full trail. Covering InSv through a general
mechanism remains real, tracked future work (`.claude/vnext-ideas.md`).

**Proven only against Vexilla, not Shield Dome** - `VexillaStatlineFlagRule.Apply`'s own
`current + 1` (a `Plain`-family Improve on Oc) is reproduced exactly via
`CharacteristicModificationResolver.Resolve`; Shield Dome's InSv mutation is out of scope per the
exclusion above, so `RollThreshold`/`ArmourPenetration`'s sign resolution is validated only against
the rulebook's own worked examples (`.claude/vnext-ideas.md`, quoted verbatim from the user), not
against a second real, independently-authored consumer.

**Not yet consumed by anything** - no `AttachedUnitAggregator`, `RuleEffectClassifier`, or
`/LivePlay` call site uses this component. It exists to be provably correct in isolation before a
later change asks anything to trust it, the same sequencing `rule-effect-classification` itself
used. Explicitly out of scope: the full Modification Engine (multi-rule stacking grouped by
step-type, `Set`-vs-`Set` "best wins" resolution, `Multiply`/`Divide` steps, a
`CharacteristicModifier`/"Mutator rule" abstraction); wiring this into `AttachedUnitAggregator` or
any rendering; tier 3+ (conditional) effects; a `DerivedValue` for `characteristic-modifier-caveats`'
own candidates.
