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
CharacteristicModificationKind        // Domain/Catalogue/CharacteristicModificationKind.cs - see
                                       // the enum's own doc comment (plain closed enum, not a
                                       // CharacteristicValue-style abstract/sealed hierarchy)
  RollThreshold                       // see member's own doc comment (WS, BS, Sv, Ld)
  ArmourPenetration                   // see member's own doc comment (AP, root CLAUDE.md's sign
                                       // convention)
  Plain                               // see member's own doc comment (M, T, W, Oc, S, Range)

CharacteristicModificationKinds.Of(characteristic) -> CharacteristicModificationKind
                                       // see class's and method's own doc comments

CharacteristicModificationClamp.Apply(characteristic, value) -> int
                                       // see class's and method's own doc comments

CharacteristicModificationResolver.ResolveDelta(kind, verb, amount) -> int
                                       // see method's own doc comment
CharacteristicModificationResolver.Resolve(characteristic, current, verb, amount) -> CharacteristicValue
                                       // see method's own doc comment
CharacteristicModificationResolver.ResolveVerbFromRawDelta(kind, delta) -> (EffectVerb Verb, int Amount)
                                       // see method's own doc comment (the inverse of ResolveDelta -
                                       // an already-signed raw delta back to a rulebook Verb+amount);
                                       // feeds the offline report tool's structural-derivation path
                                       // only (see CharacteristicModifierCandidate's own doc
                                       // comment) - not consumed at Build time.
```

**Deliberately excludes InSv and `WeaponProfile`'s Attacks/Damage** — a scope correction made before
any code was written, once the original draft (which folded InSv into `RollThreshold`) was found not
to type-check against `InvulnerableSaveCharacteristicView`'s actual shape; see
`introduce-characteristic-modification-kind/design.md`'s Context and Decisions for the full trail,
and characteristic-modifier-caveats.md for the matching precedent.

**Proven only against Vexilla, not Shield Dome** — `VexillaStatlineFlagRule.Apply`'s own
`current + 1` is reproduced exactly via `CharacteristicModificationResolver.Resolve`; see the design
doc's Risks/Trade-offs for why Shield Dome couldn't serve as a second proving example and how that
risk is mitigated.

**Not yet consumed by anything** — no `AttachedUnitAggregator`, `RuleEffectClassifier`, or
`/LivePlay` call site uses this component; it exists to be provably correct in isolation first, the
same sequencing `rule-effect-classification` itself used. See the design doc's Non-Goals for the
full out-of-scope list (the Modification Engine, wiring, tier 3+ effects, etc.).
