# Characteristic Modification Kind

Full requirements: `openspec/changes/introduce-characteristic-modification-kind/` (original,
int-only resolver/clamp), `openspec/changes/classify-weapon-characteristic-effects/` (Damage's
dice-aware resolution path, described below). The bottom layer of the not-yet-built "Modification
Engine" `.claude/vnext-ideas.md`'s "Characteristic-modification domain hardening" entry sketches —
resolves an `EffectVerb` (`rule-effect-classification`'s `Improve`/`Worsen`/`Set`) plus a stated
amount into the correct signed mutation for a characteristic's own rulebook arithmetic family, and
enforces that characteristic's legal value bound afterward. `rule-effect-classification`
deliberately stops short of this resolution; the two hand-authored `statline-flag-rules` (Shield
Dome, Vexilla) each embed their own one-off arithmetic inline today, with no shared, tested
sign/clamp logic behind either.

```
CharacteristicModificationKind        // Domain/Catalogue/CharacteristicModificationKind.cs - see
                                       // the enum's own doc comment (plain closed enum, not a
                                       // CharacteristicValue-style abstract/sealed hierarchy)
  RollThreshold                       // see member's own doc comment (WS, BS, Sv, Ld)
  ArmourPenetration                   // see member's own doc comment (AP, root CLAUDE.md's sign
                                       // convention)
  Plain                               // see member's own doc comment (M, T, W, Oc, S, Range, and -
                                       // since classify-weapon-characteristic-effects - Damage
                                       // ("D"), the first dice-shaped characteristic in this family)

CharacteristicModificationKinds.Of(characteristic) -> CharacteristicModificationKind
                                       // see class's and method's own doc comments

CharacteristicModificationClamp.Apply(characteristic, value) -> int
                                       // see class's and method's own doc comments
CharacteristicModificationClamp.ApplyToDice(characteristic, DiceExpression value) -> DiceExpression
                                       // Damage's own clamp path, see method's own doc comment -
                                       // clamps the value's GUARANTEED MINIMUM (Count + Modifier),
                                       // not its literal Modifier, since Apply's own int-keyed
                                       // Bounds table assumes its value IS the resolved value
                                       // itself, not a derived quantity like a dice roll's floor

CharacteristicModificationResolver.ResolveDelta(kind, verb, amount) -> int
                                       // see method's own doc comment
CharacteristicModificationResolver.Resolve(characteristic, current, verb, amount) -> CharacteristicValue
                                       // see method's own doc comment - branches on
                                       // DiceCharacteristicValue (Damage) vs. NumericCharacteristicValue
                                       // (every other in-scope characteristic) before falling through
                                       // to a symbolic value's own untouched pass-through
CharacteristicModificationResolver.ResolveVerbFromRawDelta(kind, delta) -> (EffectVerb Verb, int Amount)
                                       // see method's own doc comment (the inverse of ResolveDelta -
                                       // an already-signed raw delta back to a rulebook Verb+amount);
                                       // feeds the offline report tool's structural-derivation path
                                       // only (see CharacteristicModifierCandidate's own doc
                                       // comment) - not consumed at Build time.
```

**Deliberately excludes InSv and `WeaponProfile`'s Attacks** — a scope correction made before any
code was written, once the original draft (which folded InSv into `RollThreshold`) was found not to
type-check against `InvulnerableSaveCharacteristicView`'s actual shape; see
`introduce-characteristic-modification-kind/design.md`'s Context and Decisions for the full trail,
and characteristic-modifier-caveats.md for the matching precedent. Attacks stays excluded even now
that Damage is covered (below) - `classify-weapon-characteristic-effects`'s own Non-Goals treats
classification and resolution as separately-sequenced work per characteristic, the same way a
Statline Effect was provably extracted well before its own resolver existed; Attacks is real,
classifiable `WeaponCharacteristicEffect` output today with no resolution path to consume it yet.

**Damage ("D") is covered, despite being dice-shaped too** (`classify-weapon-characteristic-effects`)
— the first characteristic this component resolves whose `CharacteristicValue` is a
`DiceCharacteristicValue` rather than a `NumericCharacteristicValue`. Classifies `Plain` (a
numerically higher value is better, same as Strength) - dice-shape affects how the resolved value is
*represented*, not which arithmetic family governs its sign.
`CharacteristicModificationResolver.Resolve` branches on the current value's own runtime type:
`Improve`/`Worsen` apply the resolved signed delta to the dice value's flat `Modifier` via
`DiceExpression`'s existing `+` operator (preserving `Count`/`Sides` unchanged); `Set` replaces the
value outright with a fixed `DiceExpression`, discarding any prior dice component. Clamping goes
through the new `ApplyToDice` (above) rather than `Apply`'s own `int`-keyed table directly, since a
dice value's legal floor of 1 is enforced against its *guaranteed minimum possible outcome*
(`Count + Modifier`), not its literal `Modifier` - e.g. worsening "D6" (guaranteed minimum 1) past
its floor raises the modifier just enough to bring the guaranteed minimum back to 1, never lets it
go negative. A fixed (`Count == 0`) Damage value clamps through the exact same `["D"]` floor-of-1
entry in `Apply`'s own `Bounds` table every other `Plain`-family scalar uses.

**Proven against Vexilla (Oc) and, since `classify-weapon-characteristic-effects`, real
`WeaponCharacteristicEffect` data too** — `VexillaStatlineFlagRule.Apply`'s own `current + 1` is
reproduced exactly via `CharacteristicModificationResolver.Resolve`; see
`introduce-characteristic-modification-kind/design.md`'s Risks/Trade-offs for why Shield Dome
couldn't serve as a second Statline proving example and how that risk is mitigated. `S`/`AP` (the
`Plain`/`ArmourPenetration` kinds a real weapon characteristic can carry) get their first real
proving example against genuine weapon data here — a real corpus run extracted 19 distinct
`WeaponCharacteristicEffect` results (`rule-effect-classification.md`'s own "Weapon-characteristic
Effects" section), several targeting `S`/`AP` directly (e.g. Chance for Glory, Conversion
Eradicator). `WS`/`BS` (already in `CharacteristicModificationKinds`' lookup table, added by an
earlier change) stay unproven by a real *weapon* Effect - `classify-weapon-characteristic-effects`'s
own weapon-characteristic vocabulary deliberately scopes to exactly Strength/Attacks/Armour
Penetration/Damage (spec.md's own Requirement text), even though real corpus text naming Weapon
Skill/Ballistic Skill as a weapon characteristic does exist (see `.claude/vnext-ideas.md`'s
recorded finding) - a future phase widening that vocabulary is what would give `WS`/`BS` their own
first real weapon-Effect proving example.

**Consumed today only by the corpus report tool's own structural-derivation path (Statline) and by
unit tests directly (Damage)** — still no `AttachedUnitAggregator`/`/LivePlay` call site resolves a
`WeaponCharacteristicEffect` against a real `WeaponProfile` (Phase 3 of the
`WeaponProfile`-targeting rule effects plan, `.claude/vnext-ideas.md`); Damage's own resolver/clamp
path is proven in isolation against hand-built fixtures first, the same sequencing this component's
Statline coverage already used. See `classify-weapon-characteristic-effects/design.md`'s Non-Goals
for the full out-of-scope list for this specific change (no `WeaponProfile` mutation, no
weapon-selector-to-contribution resolution, no rendering).
