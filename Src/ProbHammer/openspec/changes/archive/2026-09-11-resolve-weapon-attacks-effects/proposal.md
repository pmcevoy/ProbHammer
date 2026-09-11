## Why

The `WeaponProfile`-targeting rule-effects work (`classify-weapon-characteristic-effects`,
`resolve-weapon-characteristic-effects`, `render-weapon-characteristic-effects`) deliberately
excluded the Attacks (`"A"`) characteristic at every phase, always naming it as a deferred boundary.
It's worth picking up now because real, corpus-verified evidence exists that this gap is visible
today, not hypothetical: the checked-in baseline (`src/ProbHammer.Web/Data/RuleEffectClassifications.json`)
already carries 13 classified weapon-characteristic effects naming Attacks, and one of them —
Scorpion Tail / Writhing Tentacles ("Add 1 to the Attacks characteristic of melee weapons equipped
by this model.") — is **uncaveated** and present in the live BSData corpus (Chaos Space Marines,
Death Guard).

A round of design review (worked ASCII examples, checked by hand) found that Attacks genuinely
cannot reuse the resolved-value/footnote-marker convention S/AP/D already ship with. S/AP/D are part
of `WeaponProfile.EqualityKey()`, so a merged group's own S/AP/D value is uniform by construction —
showing one resolved number with a marker is always unambiguous. Attacks is deliberately *excluded*
from that equality key (it's the summed quantity, not the weapon's identity), so two contributions in
one merged entry can already legitimately show different Attacks values for reasons unrelated to any
ability (e.g. a Marshal's Master-crafted Power Weapon at 7 Attacks vs. a Sword Brother's identically-
named weapon at 3). Baking a resolved value into a contributor's own row — as S/AP/D safely do —
makes it impossible to tell "this differs because of the model's own stats" apart from "this differs
because of an ability," which is exactly the ambiguity a player expanding a breakdown to double-check
(and quote) a roll-up total needs resolved, not hidden behind a footnote.

## What Changes

- Each weapon-table contributor row keeps showing its **base** (unmutated) Attacks value — never a
  value with an ability's bonus already folded in.
- A matched, non-caveated Attacks effect is recorded as a separate, **additive** line, nested
  directly under the contributor row(s) it reaches, using the same `(Count×Amount)` notation the
  base rows already use (e.g. `ⓘ Zealot (1×3)`) — so the aggregated total is always the literal sum
  of every visible number on the page, never a number requiring an already-applied bonus to be
  mentally subtracted or added again.
- An effect that provably reaches *every* current contributor of an entry renders once, above the
  breakdown, rather than repeated under each row — reusing (not duplicating) the same row-bound /
  component-wide / shared-across-components tiering `AggregateAbilityEntry` already established for
  Statline abilities. An effect reaching only some contributors nests under just those rows — the
  same mechanism as reaching one, just applied to more than one.
- `WeaponContribution` gains a list of per-ability Attacks contributions (source Ability + signed
  per-model amount), computed the same way S/AP/D's own applicable-effect matching already works
  (bearer/target scoping, weapon-selector matching, non-caveated only) — but this is a genuinely
  different mechanism from S/AP/D's "mutate the profile field" resolution: Attacks is never collapsed
  into one resolved `WeaponProfile.A` value. `WeaponProfile.A` itself is **not** retyped — it stays
  exactly what it is today, a bare `DiceExpression` representing the base, catalogue value.
  `WeaponCharacteristicEffectResolver` is unchanged and still never resolves Attacks — it isn't the
  mechanism this change uses.
- `CharacteristicModificationKinds` still gains an `"A"` entry (`Plain`, same family as `S`/`D`) —
  needed to resolve each matched effect's own signed per-model delta (`Improve`/`Worsen`), reusing
  the existing sign-resolution arithmetic with no new code.
- A caveated Attacks match keeps using the *existing*, unchanged mechanism from
  `render-weapon-characteristic-effects`: it surfaces as an unresolved-ability-reference name marker
  on the entry, exactly as a caveated S/AP/D match already does — no new caveated-side behavior.

## Capabilities

### New Capabilities

(none — this extends two existing capabilities)

### Modified Capabilities

- `weapon-characteristic-effect-resolution`: adds a new requirement describing how an Attacks
  effect resolves into a per-model signed amount (a genuinely different shape from the existing
  "mutated profile" requirement, which stays S/AP/D-only and unchanged in what it covers). The
  existing "Attacks Characteristic Is Not Resolved" requirement is narrowed, not removed — it still
  correctly states that `WeaponCharacteristicEffectResolver` never mutates a profile's Attacks field.
- `attached-unit-tracker`: "Aggregate Weapon Count View" gains the per-contribution Attacks-
  contribution-list mechanism (base value + a list of matched, non-caveated ability deltas), distinct
  from the existing S/AP/D mutate-and-regroup mechanism it already documents — an Attacks-only match
  never splits or merges a group, since Attacks was never part of the grouping identity.
- `live-play-view`: "Weapon Section Rendering" and the breakdown-row mechanism widen to render nested
  ability-contribution lines under the reaching contributor row(s), or once above the breakdown for a
  group-wide match, per the `(Count×Amount)` convention above — replacing the marker+legend approach
  this proposal originally sketched for Attacks specifically (S/AP/D's own marker+legend convention
  is untouched).

## Impact

- `src/ProbHammer.Core/Domain/Catalogue/CharacteristicModificationKind.cs` (new `"A"` entry only —
  no other file in `Domain/Catalogue` changes; `WeaponProfile.cs` and
  `WeaponCharacteristicEffectResolver.cs` are untouched).
- `src/ProbHammer.Core/Domain/Roster/AttachedUnitAggregator.cs` (a new sibling to
  `ResolveContributionProfile`/`FindUnresolvedAbilities` that collects each contribution's matched,
  non-caveated Attacks effects into signed per-model amounts) and `AttachedUnitAggregateView.cs`
  (`WeaponContribution` gains the new contribution-list field; `AggregateWeaponEntry.TotalAttacks`'s
  computation folds those amounts into the existing per-contribution scale-and-sum).
- `src/ProbHammer.Web/Pages/LivePlay.cshtml.cs` (`BuildContributionBreakdown`'s grouping/uniformity
  check, the new group-wide-vs-row-bound tiering logic, `WeaponContributionRow`/`WeaponRowViewModel`)
  and `_UnitBlock.cshtml` for rendering the nested ability lines.
- No BSData/BattleScribe import-mapper changes — `WeaponProfile.A`'s construction is untouched.
- `openspec/specs/weapon-characteristic-effect-resolution/spec.md`,
  `openspec/specs/attached-unit-tracker/spec.md`, `openspec/specs/live-play-view/spec.md`.
