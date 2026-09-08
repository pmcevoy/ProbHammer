## Context

See `proposal.md` — `Why`. Relevant existing shapes this builds alongside, not on top of:

- `EffectVerb` (`Improve`/`Worsen`/`Set`) and `CharacteristicEffect` already exist in
  `rule-effect-classification`, deliberately unresolved to a signed value.
- `CharacteristicValue`/`CharacteristicView` (`characteristic-value`) are pure data — no method on
  either type computes a mutation; a prior attempt to add one (`ComputeDerivedValue`) was reverted
  for exactly this reason (see `feedback_avoid_premature_behavior_on_unconsumed_domain_types`
  memory).
- `ShieldDomeStatlineFlagRule`/`VexillaStatlineFlagRule` (`statline-flag-rules`) each embed the
  specific arithmetic their one ability needs inline, with no shared sign/clamp logic behind either.
- `.claude/vnext-ideas.md`'s "Characteristic-modification domain hardening" section already
  transcribes the rulebook's own Improve/Worsen arithmetic table and the full per-characteristic
  clamp-bound table verbatim, and lays out a larger, not-yet-built "Modification Engine" this
  component is the bottom layer of. This change builds only the `Kind` layer from that sketch.
- `InSv` is a compound melee/ranged `InvulnerableSave`, wrapped in its own
  `InvulnerableSaveCharacteristicView` — not a plain `CharacteristicValue`/`ScalarCharacteristicView`
  like `Statline`'s other five scalars and `WeaponProfile`'s four. `characteristic-modifier-caveats`
  already excludes InSv from its own Field allowlist for this exact reason. `WeaponProfile.A`/`D` are
  likewise `DiceExpression`, never a plain scalar. Confirmed by reading `Statline.cs`/
  `WeaponProfile.cs`/`CharacteristicView.cs` directly while starting implementation — the original
  draft of this change (and its spec) assumed InSv could be folded into the same plain-scalar
  resolver as Sv/Ld/etc. and used Shield Dome as a second proving example; both were corrected before
  any code was written once this was caught.

## Goals / Non-Goals

**Goals:**
- A pure, dependency-free arithmetic component: given a characteristic name, an `EffectVerb`, and an
  amount, resolve the correct signed delta and enforce that characteristic's clamp bound — scoped to
  the characteristics this codebase actually represents as a plain scalar (`Statline`'s M/T/Sv/W/Ld/Oc,
  `WeaponProfile`'s Bs/Ws/S/Ap, and the bare-integer Range).
- Prove it reproduces `VexillaStatlineFlagRule`'s existing result exactly, so a later change can point
  at this as a tested foundation instead of re-deriving the arithmetic from the rulebook text a second
  time.

**Non-Goals:**
- Wiring this into `AttachedUnitAggregator`, `RuleEffectClassifier`, or any `/LivePlay` rendering.
- The full "Modification Engine" from `vnext-ideas.md` — grouping multiple simultaneous modifiers by
  step-type, `Set`-vs-`Set` "best wins" resolution, `Multiply`/`Divide` steps, or a
  `CharacteristicModifier`/"Mutator rule" abstraction. This change is only the `ResolveDelta`/`Clamp`
  primitives that engine would eventually call.
- InSv and `WeaponProfile.A`/`D` — see Context above. Covering InSv through this general mechanism is
  real, tracked future work (`.claude/vnext-ideas.md`), not attempted here.
- `WeaponProfile`'s scalar fields (Bs/Ws/S/Ap) are included in the classification/clamp tables since
  they're genuinely plain-scalar-typed today, but no caller of this change targets a `WeaponProfile`
  field — they're covered for completeness against the rulebook table, not because anything consumes
  them yet.
- Dice-valued or symbolic base values participating in arithmetic beyond the explicit
  no-op-on-symbolic requirement — every real BSData `Statline` scalar mapping call site already
  constructs a plain numeric value (no Statline scalar is ever dice-valued in the live corpus), so
  dice support is not attempted.

## Decisions

**`CharacteristicModificationKind` is a plain closed enum (`RollThreshold`/`ArmourPenetration`/
`Plain`) with a static lookup table and static resolver functions, not a `CharacteristicValue`-style
abstract/sealed hierarchy.** Considered mirroring `CharacteristicValue`/`WeaponProfile`/`RuleTarget`'s
own abstract-base-plus-sealed-subtypes convention, since that's this codebase's default shape for a
closed domain concept. Rejected: those hierarchies represent a *value* that genuinely varies in
shape per case (a dice expression vs. a plain int vs. a symbol). A `Kind` here selects *behavior*
(which sign rule applies), with identical shape (an `int` amount in, an `int` delta out) in every
case — the same shape `EffectVerb` itself already uses as a plain enum. A lookup table plus a
`switch` on the enum is simpler and avoids inventing a hierarchy with no per-case data to justify it.

**Two separate small functions, not one combined "resolve and apply" call**: `ResolveDelta(Kind,
EffectVerb, amount) -> int` (pure sign arithmetic, ignorant of any current value or bound) and
`Clamp(characteristicName, value) -> int` (bound enforcement, ignorant of verbs). A `Set` verb skips
`ResolveDelta` entirely (assigns its value directly) but still passes through `Clamp` — keeping the
two functions separate makes that skip trivial to express and keeps clamping (which every path needs)
from being duplicated per verb.

**Characteristic-to-Kind and characteristic-to-clamp-bound are two separate lookup tables, keyed by
the same plain characteristic-name strings `rule-effect-classification`'s `CharacteristicEffect`
already uses** (not merged into one table, and not attached to `Statline`/`WeaponProfile` as
properties) — a characteristic's arithmetic family and its clamp bound are independent facts (WS and
Ld share `RollThreshold` but have different bounds), so collapsing them into one entry per
characteristic would misleadingly suggest they always vary together.

**InSv is excluded from this component's classification/clamp tables entirely**, matching
`characteristic-modifier-caveats`'s own precedent — see Context above. This is a scope correction
made before any code was written, not a deferred risk: the original draft tried to fold InSv into
`RollThreshold` alongside Sv/WS/BS/Ld and use Shield Dome as a second proving example, which doesn't
type-check against `InvulnerableSaveCharacteristicView`'s actual (melee/ranged, non-`CharacteristicValue`)
shape. Covering InSv through a general mechanism remains real, tracked future work
(`.claude/vnext-ideas.md`), for whoever eventually builds a compound-aware layer on top of this one.

**Symbolic-value no-op is the caller-facing entry point's job, not `ResolveDelta`/`Clamp`'s** —
`ResolveDelta`/`Clamp` operate on plain integers only, matching their "pure arithmetic" scope. A
third, small orchestrating function takes the characteristic's actual `CharacteristicValue`, returns
it unchanged when it's the symbolic variant, and otherwise unwraps/reapplies through the two
arithmetic functions — keeping the arithmetic itself simple while still satisfying the
"symbolic values are never modified" requirement end-to-end.

## Risks / Trade-offs

- **[Risk]** Only one hand-authored rule (Vexilla) is a valid proving example, since the other
  (Shield Dome) targets InSv, which is out of scope — so `RollThreshold`/`ArmourPenetration`'s sign
  resolution is validated only against the rulebook's own worked examples, not against a second real,
  independently-authored consumer. → **Mitigation**: the rulebook's worked examples
  (`.claude/vnext-ideas.md`) are quoted verbatim from the user, not derived/guessed, so they're as
  trustworthy a ground truth as a second hand-authored rule would be; a real second consumer can still
  validate this component further whenever one is wired in.
- **[Risk]** Shipping an unconsumed component reads as premature scope. → **Mitigation**: this is a
  deliberate, precedented sequencing in this codebase — `rule-effect-classification` itself shipped
  fully unwired and proven only against ground-truth/corpus examples before any consumer touched it.
  This change's whole purpose is to be provably correct in isolation (via the one hand-authored rule
  it can validly reach, plus the rulebook's own worked examples) *before* anything is asked to trust
  it.
- **[Risk]** A future Modification Engine (multi-rule stacking, `Set`-vs-`Set` best-wins,
  `Multiply`/`Divide`) will need to build on these primitives without rework. → **Mitigation**: keep
  `ResolveDelta`/`Clamp` pure and free of any dependency on `Ability`, `AttachedUnitAggregator`, or
  roster state, so a later grouping engine can call them per-modifier without needing to change their
  signatures.

## Migration Plan

Pure addition, no existing code path touched — nothing to migrate or roll back. Landing this change
is just adding new, fully-tested, currently-unconsumed types under `ProbHammer.Core.Domain.Catalogue`.
