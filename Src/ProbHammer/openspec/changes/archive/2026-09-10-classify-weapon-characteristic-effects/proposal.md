## Why

`RuleEffectClassifier` extracts unconditional Statline/invulnerable-save characteristic effects
from a rule/ability's own text, but has no vocabulary for the other large family of real 11e rules
text: an effect that mutates a *weapon's* characteristics (Strength, Attacks, AP, Damage) rather
than a model's. Phase 1's corpus spike (`.claude/vnext-ideas.md`) confirmed this phrasing is real
and common — ~30 examples of a dominant "improve the Strength and Attacks characteristics of melee
weapons equipped by this model by 3" shape, plus a rarer 3-example two-verb anaphora shape — and
settled the two open design questions this work depends on (a coordinate characteristic list always
shares exactly one weapon selector/verb/amount; `RuleClassification.Target` never needs to vary
within one ability's own Effects). This is the next classification-layer step before any of it can
be aggregated into a weapon's rendered profile (Phase 3+, deliberately out of scope here).

## What Changes

- New `WeaponCharacteristicEffect(WeaponSelector Selector, string Characteristic, EffectVerb Verb, int Amount)`
  as a third sealed subtype of the existing abstract `CharacteristicEffect`, alongside
  `ScalarCharacteristicEffect`/`InvulnerableSaveCharacteristicEffect`.
- New `WeaponSelector` abstract base with sealed `NamedWeapon`/`WeaponClass`/`AllWeapons` subtypes,
  mirroring `RuleTarget`'s own abstract-base/sealed-subtype/`[JsonPolymorphic]` convention — one
  selector shape shared by every `WeaponCharacteristicEffect` an ability's text produces.
- Widen `RuleEffectClassifier` to recognize weapon-characteristic phrasing per Phase 1's two
  confirmed shapes:
  - The dominant shared-amount/coordinate-characteristic-list shape splits into N atomic
    `WeaponCharacteristicEffect`s (one per named characteristic), all sharing an identical
    `WeaponSelector`/`Verb`/`Amount`.
  - The rarer two-independent-verb "and"-joined anaphora shape (Brutal Raider/Euphoric Strikes)
    extracts two effects with independent verbs, still resolving to one shared selector.
- Extend `CharacteristicModificationKind`/`CharacteristicModificationResolver`/
  `CharacteristicModificationClamp` to cover Damage — the first in-scope characteristic whose
  `CharacteristicValue` is dice-shaped rather than a plain int, needing dice-aware resolution via
  `DiceExpression`'s existing `+`/`Add` support rather than the resolver's current plain-int path.
  `WS`/`BS`/`AP`/`S` (already present in `CharacteristicModificationKinds`' lookup table but
  unconsumed by any real weapon data) get their first real proving ground here too.
- A checked-in verified-classification baseline entry set for the new weapon-characteristic Effect
  results, extending the existing `RuleClassificationBaseline` mechanism (same file, same
  `RuleClassificationBaselineEntry` shape) rather than a parallel baseline file — plus a real
  live-corpus review pass against the BSData clone before calling this done, the same rigor
  `classify-rule-effects-from-text` required (that work found 6 real bugs beyond its own passing
  unit tests).

**Not breaking**: `CharacteristicEffect` gains a new sealed subtype; existing
`ScalarCharacteristicEffect`/`InvulnerableSaveCharacteristicEffect` consumers are unaffected.
`WeaponProfile.D`'s eventual retype to a caveat/provenance-capable view (sketched in design.md as
groundwork for Phase 3, since `CharacteristicValue` already has a dice-kind variant) is **not**
part of this change's shipped behavior — no call site here reads or mutates it — so it is not
listed as a spec-level change; see design.md for why it's flagged as a likely Phase 3 precursor
instead.

## Capabilities

### New Capabilities
(none — this widens two existing capabilities' own requirements; no new capability boundary is
introduced)

### Modified Capabilities
- `rule-effect-classification`: the "Unconditional Characteristic Effect Extraction" requirement
  gains a third recognized Effect shape (a weapon-characteristic mutation via a `WeaponSelector`),
  alongside the existing Statline-scalar and invulnerable-save shapes — including the
  coordinate-characteristic-list splitting rule and the two-verb anaphora shape.
- `characteristic-modification-kind`: the closed arithmetic-family classification, sign resolution,
  and clamp-bound enforcement requirements extend to cover Damage (dice-shaped) in addition to the
  existing int-shaped characteristics.

## Impact

- `src/ProbHammer.Core/Domain/Catalogue/CharacteristicEffect.cs` — new `WeaponCharacteristicEffect`
  sealed subtype.
- `src/ProbHammer.Core/Domain/Catalogue/WeaponSelector.cs` (new file) — `WeaponSelector` abstract
  base + `NamedWeapon`/`WeaponClass`/`AllWeapons` sealed subtypes.
- `src/ProbHammer.Core/Domain/Catalogue/RuleEffectClassifier.cs` — new regex patterns + extraction
  logic for weapon-characteristic phrasing.
- `src/ProbHammer.Core/Domain/Catalogue/CharacteristicModificationKind.cs`,
  `CharacteristicModificationResolver.cs`, `CharacteristicModificationClamp.cs` — Damage coverage.
- `src/ProbHammer.Web/Data/RuleEffectClassifications.json` — new baseline entries for the
  weapon-characteristic Effect results found in the live corpus review pass.
- `tools/RuleEffectClassificationReport/` — no structural change expected (already walks every
  classified text generically), but its output will surface the new Effect shape.
- No `AttachedUnitAggregator`/`/LivePlay` call site changes — nothing wires a
  `WeaponCharacteristicEffect` into a rendered weapon profile yet (Phase 3+).
