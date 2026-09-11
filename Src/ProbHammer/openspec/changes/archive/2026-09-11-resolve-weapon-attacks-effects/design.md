## Context

`WeaponProfile.EqualityKey()` deliberately excludes Attacks from the structural equality that
determines weapon grouping — it's the quantity `AggregateWeaponEntry.TotalAttacks` sums across
contributions, not part of a weapon's identity. Two contributions in one merged entry can therefore
already legitimately carry different per-model Attacks with no ability involved at all (see
`attached-unit-tracker`'s own "Same weapon profile from different components is combined" scenario: a
Bodyguard's 3-Attacks weapon and an attached Leader's 7-Attacks weapon of otherwise-identical
structural profile merge into one entry today). This is the reason S/AP/D's already-shipped
resolved-value/footnote-marker convention (`render-weapon-characteristic-effects`) cannot extend
cleanly to Attacks: a marker on a resolved value only reads unambiguously when every unmutated
contributor in the same group is known to share that value, which S/AP/D guarantee (they're part of
grouping identity) and Attacks does not.

A round of worked-example review (see proposal.md - Why) settled on a different convention instead:
every contributor row keeps showing its base value; a matched ability's own contribution renders as a
separate, additive, nested line using the same `(Count×Amount)` notation the base rows already use.
This mirrors `AggregateAbilityEntry`'s existing three-tier placement for Statline abilities (row-
bound / component-wide / shared-across-components, the last from the Core Rule dedup work) rather
than building a new mechanism.

## Goals / Non-Goals

**Goals:**
- Resolve a matched, non-caveated Attacks effect into a signed per-model delta, reusing
  `CharacteristicModificationResolver`'s existing `Plain`-family sign arithmetic — no new resolution
  code needed beyond a `CharacteristicModificationKinds` table entry.
- Render that delta as a real, additive, nested line under whichever contributor row(s) it reaches —
  never folded into a contributor's own displayed value — so every number in an expanded breakdown is
  a genuine addend a player can read aloud to reconstruct the total.
- Place a matched effect at the correct tier: nested under one or some contributor rows (row-bound /
  partial — the same mechanism, just reaching more than one row), or rendered once above the whole
  breakdown when it provably reaches every current contributor identically (group-wide) — reusing
  `AggregateAbilityEntry`'s existing tiering concept rather than inventing a parallel one.
- Keep every existing Attacks-summation/grouping mechanic unchanged: Attacks still never splits or
  merges a group (unlike S/AP/D), and the entry's collapsed (unexpanded) rendering still shows only
  the aggregated total, per the existing "Weapon Section Rendering" requirement.
- Keep the existing caveated-Attacks-match behavior (`render-weapon-characteristic-effects`'s
  unresolved-ability-reference name marker) entirely unchanged — this change only adds a resolution
  path for the *non-caveated* case.

**Non-Goals:**
- Retyping `WeaponProfile.A` — unlike S/AP/D, nothing in this design ever needs a single "resolved
  A" value on the profile itself; the base value and the ability deltas stay separate all the way to
  the render layer. `WeaponCharacteristicEffectResolver` stays untouched and still never resolves
  Attacks.
- Resolving a `Set`-verb Attacks effect — no real corpus example exists for Attacks
  (all 13 baselined entries use `Improve`); `CharacteristicModificationResolver.ResolveDelta` already
  throws for `Set` by design, and this change doesn't change that.
- Resolving WS/BS weapon-characteristic effects — still unclassified, no real baseline entries exist.
- Evaluating a caveated entry's own activation condition — unchanged permanent boundary (12 of the
  13 real Attacks-effect baseline entries are caveated and stay that way).

## Decisions

### D1: A new, separate per-contribution Attacks-contribution list — not a profile mutation
`WeaponContribution` gains `AttacksContributions: IReadOnlyList<AttacksContribution> = []`, where
`AttacksContribution(Ability SourceAbility, int Amount)` records one matched, non-caveated effect's
own signed per-model delta (`CharacteristicModificationResolver.ResolveDelta(CharacteristicModificationKinds.Of("A"),
effect.Verb, effect.Amount)`). Computed by a new `AttachedUnitAggregator` method, a sibling to
`ResolveContributionProfile`, reusing its exact `IsBearerOf`/`WeaponSelectorMatches`/
`TryGetWeaponEffectEntry(isCaveated: false)` matching machinery — only the target field differs
(collect a list of deltas rather than mutate a `WeaponProfile` field). `WeaponContribution.
PerModelAttacks` stays exactly as it is today (the base, unmutated `DiceExpression` — it already
never gets touched by Attacks resolution, since `ResolveContributionProfile`'s own S/AP/D-only filter
already excludes `"A"`).

`AggregateWeaponEntry.TotalAttacks`'s computation in `BuildWeapons` changes to fold each
contribution's own delta sum into its scale-and-sum: `(contribution.PerModelAttacks +
contribution.AttacksContributions.Sum(c => c.Amount)).Scale(modelLine.RemainingCount)`, reusing
`DiceExpression`'s existing `+`/`Scale` operators — the same arithmetic
`CharacteristicModificationResolver.Resolve`'s own dice-aware branch already performs, just applied
directly here instead of through that method (since there's no `WeaponProfile.A` field to write the
result back onto).

**Alternative considered**: mirror S/AP/D exactly — retype `WeaponProfile.A` to
`ScalarCharacteristicView`, mutate it via `WeaponCharacteristicEffectResolver.Resolve`, and derive the
render-layer breakdown by re-deriving "what changed" from `ContributingAbilities` after the fact
(this was this change's own original design, before the worked-example review). Rejected — a single
mutated value can't carry "which of possibly several abilities contributed how much," and re-deriving
per-ability amounts from a merged `ContributingAbilities` list after resolution loses the very
information (`(count×amount)` per source) the render layer needs. Keeping the list at aggregation time
is simpler than reconstructing it downstream.

### D2: The existing S/AP/D allowlist filter stays S/AP/D-only; Attacks never reaches it
`ResolveContributionProfile`'s characteristic filter (guarding `ApplyWeaponCharacteristicEffect`/
`GetWeaponScalarField` against an unrecognized characteristic name — see D3 of an earlier draft of
this design) becomes `.Where(e => e.Characteristic is "S" or "AP" or "D")` — Attacks is excluded here
because it's handled entirely by D1's new, separate path, never by `WeaponCharacteristicEffectResolver`.
`FindUnresolvedAbilities` (the caveated-branch lookup powering the existing unresolved-ability-
reference name marker) instead becomes `.Where(e => e.Characteristic is "S" or "AP" or "D" or "A")` —
Attacks *is* included here, since a caveated Attacks match should still surface via the existing,
unchanged name-marker mechanism `render-weapon-characteristic-effects` already ships.

Both filters guard the same real robustness gap a future WS/BS classification would otherwise hit
(an unrecognized characteristic reaching either a resolver that throws, or being silently skipped
where it shouldn't be) — this is a direct carry-forward of the earlier draft's own D3, just correctly
split between "S/AP/D only" (mutation path) and "S/AP/D/A" (caveated-reference path) now that Attacks
has its own separate resolution mechanism.

### D3: Rendering tiers — reusing `AggregateAbilityEntry`'s pattern, not inventing one
The render layer (a widened `LivePlayModel.BuildContributionBreakdown`) determines, per distinct
`(Ability, Amount)` pair appearing across an `AggregateWeaponEntry`'s contributions, which of two
placements applies:
- **Group-wide**: the pair appears, with the same `Amount`, on *every* current contribution in the
  entry — rendered once, as its own line, above the breakdown (not nested under any one row).
- **Row-bound / partial**: the pair appears on some but not all contributions — nested directly under
  each contributor row it actually reaches. A contributor-row group only collapses to one merged row
  (the existing uniform-`PerModelAttacks` collapse rule) when every contribution in it *also* shares
  the same `AttacksContributions` — extending the existing equality check, not replacing it, so a
  merged group's own nested ability line is unambiguous (it applies to the whole merged row, at the
  row's own summed `Count`).

Row-bound and partial reach are the same mechanism — "nested under the row(s) it reaches" is
identical code whether that's one row or several; "group-wide" is the only case needing a distinct
check (does this pair's reach set equal the entry's full contributor set).

**Alternative considered**: always nest per-contribution, never promote a universally-reaching effect
to a single above-the-breakdown line. Rejected — for a squad where every model shares an aura-type
buff, repeating the identical `ⓘ Source (N×Amount)` line under every single contributor row is exactly
the redundant repetition `AggregateAbilityEntry`'s own group-wide promotion already exists to avoid
for Statline abilities; reusing that precedent here keeps the two ability-rendering mechanisms
consistent instead of diverging for no reason.

### D4: No footnote marker on the collapsed entry's total
Unlike the original (superseded) design, the entry's collapsed Total Attacks value carries no marker
at all — discoverability comes from the existing breakdown-trigger mechanism instead: for a multi-
`ModelLine` unit, every weapon entry's name is already an interactive breakdown trigger regardless
(existing "Weapon Section Rendering" behavior, unchanged), so the ability detail is always one tap
away. The existing single-`ModelLine`-unit exception (`ShowsBreakdownTrigger` only fires when there's
something to show) widens to also fire when a contribution carries a non-empty
`AttacksContributions` list, alongside the existing S/AP/D-marker and unresolved-reference checks.

**Trade-off accepted**: a player scanning only collapsed rows on a multi-model unit gets no visual cue
that a specific entry's total includes an ability contribution (unlike S/AP/D's asterisk). This is a
deliberate simplification, open to revisiting if it proves to hurt discoverability in practice — see
Risks below.

## Risks / Trade-offs

- **[Risk]** No footnote marker on the collapsed total (D4) means a player has to expand every weapon
  entry to know whether an ability affected it, rather than being drawn to the flagged ones directly.
  → Mitigation: this is the same trade-off the pre-existing "no raw per-model Attacks in the collapsed
  row" design already makes for ordinary per-contributor variation (e.g. a Leader's 7 vs. a
  Bodyguard's 3) — a player already has no summary signal for that today. Revisit if real usage shows
  this is a problem worth a lighter-weight indicator.
- **[Risk]** Extending the breakdown-row collapse rule to also require matching `AttacksContributions`
  (D3) could, in principle, cause a previously-merged group to split for a reason a player doesn't
  immediately understand (e.g. two loadouts of the same statline, one carrying a wargear-granted
  ability). → Mitigation: this is exactly the same, already-accepted trade-off `PerModelAttacks`
  divergence already causes for any other reason — the split always has a nested ability line right
  there explaining why, so it's self-documenting the same way an ordinary base-value split already is.
- **[Risk]** `TotalAttacks`'s computation now depends on two fields (`PerModelAttacks` and
  `AttacksContributions`) staying in sync per contribution, rather than one already-resolved value. →
  Mitigation: both are set at the same point in `BuildWeapons`, from the same unmutated `baseProfile`
  and the same present-ability list; a test asserts the sum of a contribution's own base + deltas,
  scaled by count, always equals its share of the entry's `TotalAttacks`.

## Migration Plan

Purely additive: `WeaponContribution.AttacksContributions` defaults to empty, `AggregateWeaponEntry.
TotalAttacks`'s new computation is behaviorally identical to today's when no contribution has any
Attacks contributions (the sum term is zero). No `WeaponProfile` retype, so no risk to any existing
`RangedWeapon`/`MeleeWeapon` construction site. No `RuleClassificationBaseline` JSON change — this
change consumes the same 13 already-baselined Attacks entries exactly as classified.
