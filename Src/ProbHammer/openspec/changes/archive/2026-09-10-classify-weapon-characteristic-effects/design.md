## Context

See proposal.md - Why/What Changes for motivation and scope; `.claude/domain-model/rule-effect-
classification.md` and `.claude/domain-model/characteristic-modification-kind.md` for the two
existing components this change extends, and `.claude/vnext-ideas.md`'s "WeaponProfile-targeting
rule effects — phased plan" for Phase 1's corpus findings this design builds directly on.

Three existing conventions this design follows without re-deriving them:
- `RuleTarget` (`RuleTarget.cs`) — abstract base + sealed subtypes + `[JsonPolymorphic]`/
  `[JsonDerivedType]`, `kind` discriminator.
- `CharacteristicEffect` (`CharacteristicEffect.cs`) — same polymorphic shape, one sealed subtype
  per Effect kind, `RuleClassification.Effects` staying a single uniformly-typed list.
- `RuleEffectClassifier` — first-sentence-anchored-match-wins regex extraction, `SentenceStart`
  anchor, case-insensitive word-literal patterns except where case is the signal, a checked-in
  human-verified baseline (`RuleClassificationBaseline`/`RuleEffectClassifications.json`).

`CharacteristicModificationResolver.Resolve` today special-cases on `is not
NumericCharacteristicValue numeric => return current unchanged` — so a `DiceCharacteristicValue`
(already a real `CharacteristicValue` subtype, see `CharacteristicValue.cs`) is silently left
unmodified by the existing resolver, not rejected or erroring. This is the concrete gap Phase 2's
dice-aware resolution path closes for Damage specifically.

## Goals / Non-Goals

**Goals:**
- Classify real weapon-characteristic rule text (both of Phase 1's confirmed shapes) into
  `WeaponCharacteristicEffect` records, provable against hand-built fixtures first and then a real
  BSData corpus review pass, the same sequencing every prior classifier-layer change in this area
  used.
- Prove `CharacteristicModificationResolver` against a dice-shaped value for the first time
  (Damage), and prove the previously-unconsumed `WS`/`BS`/`AP`/`S` kinds against real weapon data.
- Leave `WeaponProfile.D` ready to carry per-ability provenance (Phase 3's actual need), without
  wiring any resolution into it yet.

**Non-Goals:**
- No mutation of any real `WeaponProfile` instance, no `WeaponProfileEqualityKey` group
  split/merge, no weapon-selector-to-contribution resolution (named-weapon matching, class
  matching) — all Phase 3.
- No rendering (Phase 4) and no runtime wiring into `AttachedUnitAggregator` (Phase 5).
- No resolver/kind extension for Attacks (`A`) — per the phased plan, classification treats
  Attacks and Damage uniformly (both are just a `Characteristic` string on
  `WeaponCharacteristicEffect`), but only Damage's resolver/clamp path and only `WeaponProfile.D`'s
  view retype are in scope here. Attacks stays a bare `DiceExpression` field and
  `CharacteristicModificationKinds` stays silent on `"A"` — extracting an `Improve A by 3` Effect
  from real text is fully in scope and will be baselined, it just has no resolution path to feed
  yet, matching how a Statline Effect was provably extracted (`classify-rule-effects-from-text`)
  well before its own resolver existed (`introduce-characteristic-modification-kind`).
- No `Multiply`/`Divide` verb support (permanent boundary, see vnext-ideas.md).

## Decisions

### `WeaponCharacteristicEffect` is one shape for every weapon characteristic
A single record `WeaponCharacteristicEffect(WeaponSelector Selector, string Characteristic,
EffectVerb Verb, int Amount)`, not a per-characteristic subtype family. `Characteristic` is a plain
string (`"S"`, `"A"`, `"AP"`, `"D"`) — the same "plain string, not a new enum" convention
`ScalarCharacteristicEffect.Characteristic` already uses, extended with a second, disjoint
vocabulary (weapon codes, never colliding with the Statline codes since callers already know which
list applies to which Effect subtype). Rejected: a `WeaponScalarCharacteristicEffect` /
`WeaponDiceCharacteristicEffect` split mirroring `CharacteristicValue`'s numeric/dice split — Phase
1 found classification never needs to know or care which characteristic is dice-shaped (that's a
resolution-time fact, per the phased plan's own "diverge only at resolution time" framing), so
splitting at the classification layer would just push a resolution-layer concern up into
extraction for no benefit.

### `WeaponSelector` mirrors `RuleTarget`'s exact shape
`WeaponSelector` abstract base, `[JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")]`, three
sealed subtypes: `AllWeapons`, `WeaponClass(WeaponType Type)` (reusing the existing `WeaponType`
enum from `WeaponType.cs` rather than inventing a parallel Melee/Ranged distinction), and
`NamedWeapon(string Name)`. Every real Phase 1 example resolves to `WeaponClass(Melee)` — no
corpus evidence yet for `AllWeapons` or `NamedWeapon`, but both are kept (same "kept for
completeness, not required to be exercised by a real example" precedent `UnconditionalRuleTarget`
already sets) since the phrasing space ("all weapons", "this model's bolt rifle") is a predictable,
narrow extension of the same pattern family, not speculative.

### Two extraction shapes, not one general "coordinate clause" parser
Per Phase 1's own finding that the two shapes are structurally distinct (shared-verb/shared-amount
coordinate list vs. two independently-verbed anaphora-joined clauses), the classifier gets two
separate regex-driven extraction paths rather than one shared grammar attempting to parse both.
This mirrors the existing precedent of `InvulnerableSaveRangedRestricted`/
`InvulnerableSaveMeleeRestricted`/`InvulnerableSaveGrant` being three separate patterns for three
related-but-distinct shapes, rather than one parameterized pattern.

For the dominant shape, one regex captures the shared verb, shared amount, weapon-class subject,
and the full coordinate characteristic-list substring; a second pass splits that substring on
`,`/`and` and maps each token through a weapon-characteristic-name lookup (mirroring
`RuleEffectClassifier.CharacteristicNames`, a new dictionary scoped to `"Strength"`, `"Attacks"`,
`"Armour Penetration"`, `"Damage"` → `"S"`/`"A"`/`"AP"`/`"D"`) to produce N Effects.

For the rarer anaphora shape, two clause-scoped sub-patterns are tried against the full sentence;
when both match, the second clause's selector is inferred as identical to the first's (the anaphora
resolution) rather than re-extracted independently.

### Amendment (found during implementation, 2026-09-10): a widened anchor for the dominant shape
This design originally assumed the dominant/anaphora-clause-1 patterns anchor on the plain,
pre-existing `SentenceStart` (`(?<=^|\.\s*)`), unmodified. Re-verifying the five Phase 1 ground-
truth texts directly (tasks.md task 1.1) found this doesn't hold: every real example states its
mutation clause immediately after a ", until the end of the phase," / ", until the end of the
turn," temporal-scope clause, itself preceded by an activation preamble not at a true sentence
start (`SentenceStart`'s own two cases). Applied literally, `SentenceStart` would reject all five
ground-truth texts, contradicting spec.md's own Scenarios for those exact texts. `RuleEffectClassifier`
gained a second, narrower anchor - `WeaponEffectStart` - scoped only to the two dominant-shape
weapon patterns: `SentenceStart`'s own cases plus a third, structural (not phrase-list) case,
immediately after that exact temporal-scope clause. `SentenceStart` itself, and every existing
Statline/InSv pattern, are unchanged - zero regression risk (confirmed: the full pre-existing
`RuleEffectClassifierTests` suite still passes unmodified). See `RuleEffectClassifier.WeaponEffectStart`'s
own doc comment for the full reasoning.

### Damage resolves through the `Plain` family, with amount applied to the flat modifier only
Confirmed no real corpus example (Statline or, per Phase 1, weapon text) ever states a `Set` verb
or a non-integer amount against a weapon characteristic — every real example is `Improve`-verb,
integer amount. `CharacteristicModificationResolver.Resolve` gains a branch: when `current is
DiceCharacteristicValue dice`, resolve the signed delta exactly as `Plain` already does (reusing
`ResolveDelta`), then apply it via `dice.Value + delta` (the existing `DiceExpression operator +`
already does exactly this — add to `Modifier`, preserve `Count`/`Sides`). `Set` assigns
`DiceExpression.Fixed(amount)` directly, discarding any dice component — consistent with how `Set`
already discards a prior value's history for every other characteristic, even though no real
example exercises this path yet (kept for the same "complete, not speculative" reasoning
`WeaponSelector`'s unexercised cases follow).

### Damage's clamp floor is enforced on the flat modifier, not by rejecting negative rolls
`CharacteristicModificationClamp.Apply` gains a Damage-specific bound check operating on
`DiceCharacteristicValue.Value`'s guaranteed minimum (`Count + Modifier` when `Count > 0`, else
`Modifier`) rather than reusing the existing `int`-keyed `Bounds` dictionary directly — that table
assumes its value is the resolved value itself, not a derived quantity. If the guaranteed minimum
would fall below 1, the modifier is raised just enough to bring it back to 1 (e.g. "D6" worsened by
8 would unclamp to "D6-8" — guaranteed minimum `Count + Modifier` = `1 + -8` = `-7` — so the
modifier is instead clamped to `1 - Count` = `0`, i.e. plain "D6", guaranteed minimum 1; "2D6"
worsened by 5 similarly clamps its modifier to `1 - 2` = `-1`, i.e. "2D6-1", guaranteed minimum 1).
A fixed (`Count == 0`) Damage value clamps exactly like any other `Plain`-family scalar (floor of
1, mirroring M/T/S/Range).

**Amendment (found during implementation, 2026-09-10):** this section's original worked example
stated "D6 worsened by 8 clamps to a modifier of -5" — an arithmetic error (`-5` doesn't satisfy
`Count + Modifier = 1` for `Count = 1`; the correct clamped modifier is `0`). Corrected above; the
underlying design decision (clamp to `1 - Count`) was always correct and is what's implemented.

### `WeaponProfile.D` retypes to `ScalarCharacteristicView`
`CharacteristicValue` already has a dice-kind variant (`DiceCharacteristicValue`) and
`ScalarCharacteristicView` is already generic over any `CharacteristicValue` shape (per the
`characteristic-value` capability's existing "Characteristic Value Representation"/"Characteristic
Modification View" requirements — both already cover this case structurally, confirmed by reading
`CharacteristicValue.cs`/`CharacteristicView.cs` directly). So this retype needs no new domain
type, no spec delta (see proposal.md's Impact note), and no resolution wiring — `D` simply becomes
`ScalarCharacteristicView.Resolved(DiceCharacteristicValue, DiceCharacteristicValue, [])` at every
existing construction site (identical original/derived value, zero contributing abilities, exactly
mirroring how a not-yet-caveated `S`/`Ap`/`Bs`/`Ws` is already constructed today), with `D.Value`
reading back the same `DiceExpression` every current caller already expects via
`WeaponProfileEqualityKey`. Every current `WeaponProfile.D` read site needs a one-line adjustment
(`.Value` unwrap) rather than a behavior change.

### Weapon-characteristic baseline entries live in the existing `RuleClassificationBaseline` file
No parallel baseline file. `RuleClassificationBaselineEntry`/`RuleClassificationBaseline` are
already keyed by normalized Text and already carry a polymorphic `IReadOnlyList<CharacteristicEffect>`
— a `WeaponCharacteristicEffect` entry serializes through the exact same `[JsonDerivedType]`
mechanism `ScalarCharacteristicEffect`/`InvulnerableSaveCharacteristicEffect` already use, once its
own `[JsonDerivedType(typeof(WeaponCharacteristicEffect), "Weapon")]` line is added to
`CharacteristicEffect`'s polymorphic attribute list. `RuleClassificationDiff`'s existing
Drift/NewInformation logic needs no change — it already operates generically on
`RuleClassification`'s own fields.

## Risks / Trade-offs

- **[Risk]** The weapon-characteristic-name vocabulary ("Strength"/"Attacks"/"Armour Penetration"/
  "Damage") might collide in spirit with the existing Statline vocabulary ("Toughness"/"Wounds"/
  etc.) if a future rule text names both kinds of characteristic in one sentence.
  → **Mitigation**: the two lookups stay disjoint dictionaries feeding disjoint Effect subtypes;
  Phase 1's corpus survey found no real example mixing a Statline and a weapon-characteristic
  mutation in the same coordinate list, and this design's two extraction paths only ever read from
  their own dictionary, so a collision has no code path to occur through even if a genuinely
  ambiguous phrase somehow existed.
- **[Risk]** The corpus-wide live-review pass (mirroring `classify-rule-effects-from-text`'s own 6
  found bugs) may surface a third weapon-effect phrasing shape Phase 1's spike didn't sample.
  → **Mitigation**: tasks.md budgets an explicit review-and-fix pass, not just a single
  implement-and-ship pass; any newly-discovered shape gets documented in `.claude/vnext-ideas.md`
  rather than silently worked around if it would expand this change's own scope materially.
- **[Trade-off]** Attacks (`A`) gets a classified Effect with no resolution path to consume it yet
  (see Non-Goals) — a `WeaponCharacteristicEffect` targeting `"A"` is real, correct, baselined
  output that nothing downstream can act on until a later phase extends
  `CharacteristicModificationKind` to cover it. Accepted deliberately, matching the same
  extract-before-resolve sequencing the Statline classifier/resolver pair already used across two
  separate changes.

## Migration Plan

No runtime behavior changes for any existing caller — `WeaponCharacteristicEffect`/`WeaponSelector`
are additive types, and `WeaponProfile.D`'s retype is a compile-time signature change with an
identical effective value at every existing call site (a one-line `.Value` unwrap, no logic
change). No data migration, no feature flag — the new classification output isn't consumed by
`/LivePlay` or `AttachedUnitAggregator` yet (Phase 5), so there's no live behavior to roll back if a
bug surfaces; a fix lands as an ordinary follow-up commit.

## Open Questions

None — Phase 1 resolved the two design questions this phase depended on, and every remaining
ambiguity (dice clamp semantics, baseline file reuse, Attacks' resolver scope) is resolved above
rather than deferred.
