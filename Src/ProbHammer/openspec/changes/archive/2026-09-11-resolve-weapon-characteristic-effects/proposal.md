## Why

`classify-weapon-characteristic-effects` taught `RuleEffectClassifier` to recognize a
`WeaponCharacteristicEffect` (a bearer's weapon-Strength/AP/Damage/Attacks mutation, e.g. Zealot's
"improve the Strength and Attacks characteristics of melee weapons equipped by this model by 3"),
and the checked-in baseline now tracks 19 real, human-verified examples — but nothing consumes one.
`WeaponProfile.S`/`Ap`/`D` are already `ScalarCharacteristicView` (carrying an OriginalValue/
DerivedValue/ContributingAbilities chain, same as Statline's own scalars), and
`AttachedUnitAggregator.BuildWeapons` already groups weapons by `WeaponProfileEqualityKey`
structural equality — the two pieces this work needs already exist, unconnected. This is Phase 3 of
the `WeaponProfile`-targeting rule-effects plan (`.claude/vnext-ideas.md`), and — per direct
instruction for this change — wires the mutation into the real, live `AttachedUnitAggregator` now
rather than staying an unconsumed, standalone resolver first (the sequencing the Statline family
used across `resolve-invulnerable-save-effects` then `apply-rule-effect-baseline`).

## What Changes

- New `WeaponCharacteristicEffectResolver.Resolve(effect, sourceAbility, current) -> WeaponProfile`,
  mirroring `InvulnerableSaveEffectResolver`'s own shape: mutates exactly the targeted `S`/`Ap`/`D`
  field via the existing `CharacteristicModificationResolver`, preserving every other field and the
  pre-mutation `OriginalValue` chain. Throws for an Attacks (`"A"`) effect — deliberately unsupported
  in this change (see Non-Goals); callers must filter it out first.
- A `WeaponSelector`-to-`WeaponProfile` matching predicate (`AllWeapons`/`WeaponClass`/`NamedWeapon`).
- `AttachedUnitAggregator.BuildWeapons` mutates each weapon contribution's resolved `WeaponProfile`
  with every applicable, checked-in-baseline `WeaponCharacteristicEffect` **before** computing its
  `WeaponProfileEqualityKey`, so existing structural-equality grouping does the split/merge work for
  free — a mutation reaching every current contributor of a group re-merges into one entry; one
  reaching only some contributors splits them into their own entry. Bearer scoping (`Self` vs.
  `AttachedUnit`) reuses the exact matching rule `apply-rule-effect-baseline` already established
  for Statline effects.
- **A deliberate divergence from the Statline precedent**: a baseline entry whose `IsCaveated` is
  `true` is NOT applied here. Unlike most Statline caveats (often trailing eligibility/flavor text,
  harmless to still apply around), Phase 2's own corpus review found the weapon-characteristic
  family's caveats are disproportionately real, unmodeled *activation conditions* ("Once per
  battle... If it does," / "Each time this model's unit ends a Charge move,") — this app has no
  turn-trigger mechanism to evaluate those, so auto-applying them would misrepresent a conditional
  buff as a permanent one. **Verified directly against the checked-in baseline (task 1.1 of
  tasks.md), correcting this proposal's original 9/10 estimate**: only 4 of the 19 baselined entries
  are uncaveated (Iron-hard Talons/Touch of Rot — AP; Conversion Eradicator — AP+S; Whipcord Sinews
  — S; Scorpion Tail/Writhing Tentacles — Attacks only), and of those 4, only 3 touch a resolvable
  characteristic — Scorpion Tail/Writhing Tentacles names Attacks alone, which this change doesn't
  resolve either (see Non-Goals), so it produces no visible change even though uncaveated. The other
  15 (including the well-known examples like Chance for Glory/Brutal Raider/Zealot) render unchanged
  until a future change gives this app a way to represent a conditional trigger.
- No `/LivePlay` template changes: `_UnitBlock.cshtml` already reads `@weapon.S.Value`/`.Ap.Value`/
  `.D.Value` (from `classify-weapon-characteristic-effects`'s own `WeaponProfile.D` retype), so a
  resolved mutation's numeric result already renders correctly with zero markup changes — but with
  no visual marker yet explaining *why* a value changed (no asterisk, no caveat legend). That marker
  UI is Phase 4, deliberately out of scope here — the same "wire real resolution first, add the
  explanatory marker later" sequencing this project already used for Statline
  (`apply-rule-effect-baseline` → `resolve-known-ability-effects`).

### Non-Goals (named for a later change, not built here)

- Resolving an Attacks-characteristic (`"A"`) effect — `WeaponProfile.A` stays a plain
  `DiceExpression`, unretyped; `CharacteristicModificationKinds` already, deliberately, excludes it.
  A baseline entry whose Effects touch both a resolvable characteristic and Attacks (e.g. Zealot:
  Strength + Attacks) still resolves its S/AP/D-shaped Effects; its Attacks-shaped Effect is
  silently skipped.
- Any Phase 4 rendering (marker, caveat legend, ability-attribution popover on a mutated weapon
  stat) — the raw numeric value alone is this change's entire visible surface.
- Evaluating a `KeywordRuleTarget`/`UnconditionalRuleTarget`-scoped baseline entry — same
  already-established "no roster-wide predicate evaluation" boundary Statline's own
  `apply-rule-effect-baseline` has.
- Any representation of a conditional/temporal trigger (per-turn "this fired" state) — the reason
  caveated entries stay unapplied above, not something this change attempts to build around.

## Capabilities

### New Capabilities

- `weapon-characteristic-effect-resolution`: resolves a classified `WeaponCharacteristicEffect` plus
  its source `Ability` against a `WeaponProfile`'s targeted `S`/`Ap`/`D` field into a mutated
  profile, including `WeaponSelector`-to-profile matching — the weapon-specific counterpart to
  `invulnerable-save-effect-resolution`.

### Modified Capabilities

- `attached-unit-tracker`: the "Aggregate Weapon Count View" requirement gains real
  weapon-characteristic effect application — a matched, non-caveated, bearer-scoped baseline entry
  mutates the applicable weapon's `S`/`Ap`/`D` field before grouping, and grouping continues to
  split/merge purely by the existing structural `WeaponProfileEqualityKey`, now over mutated values.

## Real-world impact today (verified during implementation, task 7.1 of tasks.md)

Checked all 4 uncaveated baseline entries directly against the real bundled BSData corpus: the other
3 (Conversion Eradicator, Whipcord Sinews, Iron-hard Talons/Touch of Rot) are all Crusade-only
Battle Honour wargear, already excluded before ever becoming a present `Ability` by this project's
own `IsGameModeGated` mechanism. Combined with the 15 caveated entries, **no currently-importable
ordinary roster produces a visible weapon-characteristic mutation yet** — the mechanism this change
builds is real and proven (fixture tests, task 6), but today's real corpus has no live example that
exercises it end-to-end with a visible result. This is a fact about the current corpus, not a defect
in this change; it will start producing visible results the moment a future change either uncaveats
a real matched-play ability or adds Crusade-mode import support.

## Impact

- **A real, pre-existing production bug fixed as part of this work, discovered via task 1's own
  regression run**: `AttachedUnitAggregator.ApplyEffect` (the Statline resolution pipeline, already
  live) crashed with `ArgumentOutOfRangeException` on any present ability whose checked-in baseline
  entry carries a `WeaponCharacteristicEffect` and a Self/AttachedUnit target — 17 of the 19 real
  weapon-characteristic baseline entries already meet that shape (e.g. Zealot, Whipcord Sinews), so
  any roster carrying one of those abilities already crashed `/LivePlay` before this change existed.
  Fixed with a one-line no-op case in `ApplyEffect`'s switch — see design.md D8.
- `src/ProbHammer.Core/Domain/Catalogue/WeaponCharacteristicEffectResolver.cs` (new file)
- `src/ProbHammer.Core/Domain/Catalogue/WeaponSelector.cs` — a `Matches(WeaponProfile)`-shaped
  predicate (exact placement decided in design.md)
- `src/ProbHammer.Core/Domain/Roster/AttachedUnitAggregator.cs` — `BuildWeapons` threads `abilities`
  + the checked-in `RuleClassificationBaseline` through to resolve and apply matched effects before
  grouping; the existing `IsBearer` bearer-scope check is generalized for reuse by both the Statline
  and weapon call sites.
- Tests: `WeaponCharacteristicEffectResolverTests` (new), `AttachedUnitAggregatorTests` (new weapon
  mutation/split/merge/caveat-gating cases), a real-captured-export verification pass per this
  project's standing practice for changes touching `AttachedUnitAggregator`.
- No `openspec/specs/rule-effect-classification`/baseline JSON changes — this change consumes the
  existing 19 baselined entries as-is.
