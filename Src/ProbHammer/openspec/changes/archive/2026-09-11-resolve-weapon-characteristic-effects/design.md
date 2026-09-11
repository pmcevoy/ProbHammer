## Context

See proposal.md - Why. Three pieces already exist, unconnected:
`WeaponCharacteristicEffect(Selector, Characteristic, Verb, Amount)` + `WeaponSelector`
(`AllWeapons`/`WeaponClass`/`NamedWeapon`) classified from real ability text
(`classify-weapon-characteristic-effects`); `WeaponProfile.S`/`Ap`/`D` already typed as
`ScalarCharacteristicView` (an OriginalValue/DerivedValue/ContributingAbilities chain, same shape
Statline's own scalars use); and `AttachedUnitAggregator.BuildWeapons` already groups weapon
contributions by `WeaponProfile.EqualityKey()` (excludes Name/Range/Attacks) into
`AggregateWeaponEntry`. `CharacteristicModificationResolver.Resolve` already handles the arithmetic
for every characteristic this change touches, including Damage's dice-aware branch. The Statline
family already solved the identical "match a baseline entry to its bearer, mutate the right field"
problem in `AttachedUnitAggregator.ApplyStatlineFlagRules`/`IsBearer` — this change reuses that
logic rather than re-deriving it.

Per direct instruction (see proposal.md), this change wires resolution into the real, live
`AttachedUnitAggregator.BuildWeapons` now, rather than staying an unconsumed standalone resolver
first — collapsing what the phased plan in `.claude/vnext-ideas.md` had sketched as separate Phase 3
(mechanism, unwired) and part of Phase 5 (real wiring) into one change. Phase 4 (rendering marker/
caveat legend) stays untouched and separate.

## Goals / Non-Goals

**Goals:**
- Resolve a matched, non-caveated `WeaponCharacteristicEffect` against its target weapon's `S`/`Ap`/
  `D` field, live, on `/LivePlay`.
- Let the existing `WeaponProfileEqualityKey` structural-equality grouping do split/merge with no
  new grouping logic — the core mechanism this change proves out.
- Share bearer-scope logic (`Self` vs. `AttachedUnit`) with the existing Statline call site rather
  than duplicating it.

**Non-Goals:**
- See proposal.md's own Non-Goals (Attacks resolution, Phase 4 rendering, roster-wide predicate
  targets, conditional-trigger representation).
- Redesigning `AttachedUnitAggregator`'s overall shape or introducing a general "apply any Effect
  kind" dispatcher beyond what's needed for this one new `CharacteristicEffect` subtype.

## Decisions

**D1. `WeaponCharacteristicEffectResolver.Resolve(WeaponCharacteristicEffect, Ability, WeaponProfile)
-> WeaponProfile`**, a new standalone class mirroring `InvulnerableSaveEffectResolver`'s own shape
exactly (pure function, no side effects, testable in isolation before any wiring). Internally: reads
the targeted field's current `ScalarCharacteristicView` off the profile (`S`/`Ap`/`D` only — see D3),
calls the existing `CharacteristicModificationResolver.Resolve(characteristic, current.Value, Verb,
Amount)`, wraps the result via `ScalarCharacteristicView.Resolved(current.OriginalValue, resolved,
[sourceAbility])` (preserving the pre-mutation original exactly as `ApplyScalarEffect` already does
for Statline), and returns `current with { <field> = resolvedView }`. Every other field (Name, Type,
Range, A, every keyword/ability flag) passes through unchanged via the record's own `with`
expression. Rejected alternative: a generic `Resolve<TCharacteristicHolder>` shared between Statline
and weapons — the two holders (`Statline`, `WeaponProfile`) have no common field-accessor shape to
generalize over without reflection or a much larger refactor; not worth it for one new caller,
mirroring this codebase's existing reluctance toward premature generalization
(`[[feedback_avoid_premature_behavior_on_unconsumed_domain_types]]`).

**D2. Bearer-scope matching (`IsBearer`) is generalized to a shared helper, not duplicated.**
`AttachedUnitAggregator.IsBearer(AggregateAbilityEntry, RuleTarget, AggregateStatlineEntry)` today
takes a full `AggregateStatlineEntry` just to read its `(ComponentName, StatlineName)`. Refactor its
core into `IsBearerOf(AggregateAbilityEntry abilityEntry, RuleTarget target, string componentName,
string? statlineName) -> bool`, with the existing Statline call site passing
`(entry.ComponentName, entry.StatlineName)` and the new weapon call site passing
`(contribution's owning Datasheet.Name, contribution's ModelLine.StatlineName)` — the same two
values a weapon contribution already carries. No behavior change for the existing Statline path;
covered by its own existing tests unchanged.

**D3. Attacks (`"A"`) is filtered out before calling the resolver, not handled inside it.**
`CharacteristicModificationKinds.Of` already throws `ArgumentOutOfRangeException` for `"A"`
(deliberately, per that file's own doc comment — `WeaponProfile.A` is unretyped `DiceExpression`
with no resolver/clamp path yet). Two ways to keep `BuildWeapons` from crashing on a real baseline
entry like Zealot (Strength **and** Attacks) or Chance for Glory (all four characteristics): filter
`effect.Characteristic == "A"` out of the applicable-effects list before calling `Resolve` at all
(chosen), or catch/swallow the exception inside the aggregator. Filtering upstream keeps
`WeaponCharacteristicEffectResolver.Resolve` itself simple and fail-loud (mirroring
`CharacteristicModificationResolver`'s own "throw for the genuinely unexpected case" convention) —
an "A" effect reaching `Resolve` at all would now be a real bug (the filter didn't run), not an
expected, silently-caught case. The filter is a one-line `Where` at the single call site, not a
try/catch masking a real programming error.

**D4. A caveated baseline entry's `WeaponCharacteristicEffect`s are not applied — diverges from the
Statline precedent, deliberately.** `ApplyStatlineFlagRules` applies a matched entry's Effects
regardless of `IsCaveated`; that's safe there because a Statline caveat (per
`rule-effect-classification.md`'s own review) is dominated by trailing eligibility/flavor text
("Restrictions: ..." — 12 confirmed instances), content that doesn't change whether the *captured*
Effect is currently true. The weapon-characteristic family is different in kind, not just degree:
`WeaponEffectStart` (the anchor `classify-weapon-characteristic-effects` added) exists specifically
because every real weapon-characteristic mutation sits immediately after a genuine activation
condition this app cannot evaluate ("Once per battle... If it does," / "Each time this model's unit
ends a Charge move,"). Applying these unconditionally would render a conditional, sometimes-off buff
as a permanent, always-on one — a real gameplay-accuracy bug, not a cosmetic omission. Gating on
`!baselineEntry.IsCaveated` is the conservative, correct choice until a future change gives this app
a real way to represent "this trigger fired this turn." Rejected alternative: apply caveated entries
anyway (matches existing Statline precedent exactly, simpler) — rejected because the failure mode is
asymmetric: a Statline caveat wrongly applied is usually harmless (the extra text was flavor); a
weapon caveat wrongly applied silently overstates a unit's real combat output at the table, which
this project's whole purpose (a live-game companion) makes a materially worse kind of wrong.

**D5. Grouping needs no new mechanism — `BuildWeapons` mutates a contribution's resolved
`WeaponProfile` before computing its `EqualityKey()`, in the same per-`(unit, modelLine, weaponName)`
loop that already exists.** `WeaponProfileEqualityKey` already carries `S`/`Ap`/`D` as full
`ScalarCharacteristicView` values, and `CharacteristicView.Equals` already compares
`ContributingAbilities` by content, not list reference
(`[[feedback_record_list_equality_gotcha]]`'s fix, already shipped) — so two contributions mutated
identically by the same ability(s) still hash/compare equal and merge into one group; a contribution
left unmutated (outside the effect's bearer scope, or the profile's `WeaponSelector` doesn't match
it) keeps its original key and lands in a different group. This is exactly the "split/merge for
free" mechanism the phased plan named as Phase 3's core deliverable — no `AggregateWeaponEntry` shape
change, no new field recording "was this group affected."

**D6. `WeaponSelector` matching lives as a private helper inside `AttachedUnitAggregator`, not as a
method on `WeaponSelector` itself.** Mirrors this codebase's own existing precedent: `RuleTarget`'s
bearer-matching logic (`IsBearer`) is private to the one consumer that needs it, not a method on
`RuleTarget`. Keeps `WeaponSelector` a plain data shape (as `RuleTarget` already is) rather than
acquiring behavior only one caller uses.

**D7. Multiple applicable abilities touching the same weapon's same field: first-applied wins, same
rule Statline already uses.** `ApplyScalarEffect`'s existing collision rule
(`if (current.ContributingAbilities.Count > 0) return statline;`) is reused verbatim for the weapon
field case — no real corpus example collides today (each of the 19 baselined entries names a
disjoint characteristic set per ability), so this is defensive consistency, not a scenario this
change needs to newly prove.

**D8 (found during implementation, task 5.4 of tasks.md). `ApplyEffect`'s Statline-resolution switch
gains a no-op case for `WeaponCharacteristicEffect`.** `ApplyStatlineFlagRules`'
`TryGetApplicableEntry` filters a baseline match by `Target` only (Self/AttachedUnit), never by
`CharacteristicEffect` subtype - so a baseline entry carrying only `WeaponCharacteristicEffect`s
(real data: 17 of the 19 checked-in weapon-characteristic entries are Self-targeted) already reached
`ApplyEffect`'s `switch`, whose default arm threw `ArgumentOutOfRangeException`. This predates this
change entirely - a real production bug shipped by `classify-weapon-characteristic-effects`, only
surfaced now because this change's own test fixtures are the first in this codebase to build a
roster carrying a Self/AttachedUnit-targeted, weapon-only baseline entry. Fixed with
`WeaponCharacteristicEffect => statline` (unchanged) alongside the existing Scalar/InvulnerableSave
cases - Statline resolution correctly ignores a weapon-shaped effect; `ResolveContributionProfile`
is what actually applies it.

## Risks / Trade-offs

- [Risk] Threading `abilities` + `RuleClassificationBaseline` through `BuildWeapons` (previously a
  pure `presentLines -> AggregateWeaponEntry` grouping function) increases that method's coupling
  and could make it harder to read. → [Mitigation] Extract the new per-contribution
  resolve-and-mutate step into its own private helper (e.g. `ResolveContributionProfile`), called
  once per `(unit, modelLine, weaponName)` before the existing grouping logic — `BuildWeapons`'s own
  grouping shape is otherwise untouched.
- [Risk] D4's caveat gating means 15 of the 19 real, human-verified baseline entries (e.g. Brutal
  Raider, Chance for Glory, Zealot — verified directly against the checked-in baseline, task 1.1 of
  tasks.md, correcting this design's original 9/10 estimate) produce no visible change at all in
  this change — could read as "nothing happened" / a bug to someone checking a specific datasheet by
  name. A 4th of the 19 (Scorpion Tail/Writhing Tentacles) is uncaveated but names only the Attacks
  characteristic, so it also produces no visible change (D3) — only 3 real abilities (Iron-hard
  Talons/Touch of Rot, Conversion Eradicator, Whipcord Sinews) actually resolve visibly. →
  [Mitigation] Documented explicitly in proposal.md's Non-Goals and this decision; a future change
  can decide whether to let a player manually assert a trigger fired, rather than this change
  guessing.
- [Risk] `NamedWeapon` selector matching has no real corpus example to validate against (per
  `classify-weapon-characteristic-effects`'s own 19-result review — every real example resolves to
  `WeaponClass`/`AllWeapons`). → [Mitigation] Covered by a hand-built fixture test regardless,
  mirroring `WeaponSelector.cs`'s own "kept for completeness" precedent for this branch.
- [Risk] `IsBearer`'s refactor (D2) touches a method the existing, passing Statline test suite
  already exercises heavily. → [Mitigation] Behavior-preserving by construction (same logic, just
  parameterized on raw `(componentName, statlineName)` instead of reading them off an
  `AggregateStatlineEntry`); the existing Statline tests are the regression guard, not new tests.

## Migration Plan

1. Refactor `IsBearer` into the shared `IsBearerOf` shape (D2); confirm the existing Statline test
   suite still passes unchanged (regression only, no new tests needed for this step).
2. Add `WeaponCharacteristicEffectResolver` (D1/D3) + its own unit tests: S/AP/D resolution each
   verified against the correct sign convention (reusing `CharacteristicModificationResolver`,
   already correct — this is a wiring/chain-preservation test, not a re-test of that resolver's own
   arithmetic), OriginalValue-chain preservation, and confirmation that an `"A"`-characteristic
   effect reaching `Resolve` throws (proving the fail-loud contract D3 depends on the caller
   respecting).
3. Add the `WeaponSelector`-to-`WeaponProfile` matching helper (D6) + unit tests for all three
   selector kinds.
4. Wire into `AttachedUnitAggregator.BuildWeapons` (D4/D5/D7): thread `abilities` + `baseline`
   through; per contribution, look up applicable present abilities' baseline entries, filter to
   `WeaponCharacteristicEffect` results whose bearer scope reaches this contribution (via
   `IsBearerOf`) and whose `Selector` matches this weapon's resolved profile, filter out `"A"`
   effects (D3) and caveated entries (D4), apply the rest via the resolver before computing
   `EqualityKey()`.
5. `AttachedUnitAggregator` fixture tests proving the actual deliverable: a bearer-scoped effect
   splits an existing merged group (only the affected contributor's key changes); a unit-scoped
   effect reaching every contributor keeps a group merged with its new mutated value; a caveated
   match leaves a profile unaffected; an effect naming both a resolvable characteristic and Attacks
   in the same ability (Zealot-shaped) applies the resolvable one and leaves Attacks untouched.
6. A real-captured-export verification pass: confirm at least one live, non-caveated
   weapon-characteristic datasheet (the corpus review's own 10 uncaveated examples are candidates)
   renders its mutated `S`/`Ap`/`D` value correctly on a running `/LivePlay`, per this project's
   standing practice for `AttachedUnitAggregator`-touching changes.

No production data migration — this change touches only `ProbHammer.Core` domain logic consumed
live by `ProbHammer.Web`; no schema, storage, or baseline JSON changes.

## Open Questions

None — the caveat-gating question (D4) and the Attacks-filtering boundary (D3) are both resolved
above with a stated rationale, not left open.
