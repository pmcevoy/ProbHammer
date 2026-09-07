## Context

See proposal.md - Why for the motivation and the reverted attempt's two real bugs. Relevant
existing shape this design builds on rather than reinvents:

- `AttachedUnitAggregator.Build(ICombatUnit)` already walks every present component's own `Unit` —
  and therefore its `Datasheet` — while building `Statlines`/`Abilities`/`Weapons`. It also already
  computes `AggregateAbilityEntry` list scoped correctly per component/model-line (component-wide vs
  bearer-specific), and `ApplyStatlineFlagRules` already presence-checks a hand-authored rule's
  matched ability against that same list before mutating a `Statline`.
- `ArmyRosterEnricher.ResolveWargearItem` already guarantees every wargear name a real export lists
  resolves to either a `WeaponProfile` (recorded on `ModelLine.Weapons` by name) or an `Ability`
  (recorded on `ModelLine.Abilities`) — it throws otherwise. So every wargear choice a resolved
  roster actually contains today is already name-findable in one of those two places.

## Goals / Non-Goals

**Goals:**
- Classify a `BsModifier` as a characteristic-modifier candidate only for tier 1 (no condition) and
  tier 2 (condition scoped to the modifier's own granting entry), closed-world.
- Presence-gate every candidate's application per resolved unit using the *existing* name-based
  presence tracking (`ModelLine.Weapons`, resolved `Ability` names) — no new match-key concept.
- Reuse `CharacteristicView.IsCaveated`/`ContributingAbilities` exactly as already established; no
  new domain shape for the view itself.

**Non-Goals:**
- Computing a `DerivedValue` for any caveat (a later, separate change).
- Tier 3+ conditions (sibling selection, attachment state, cross-unit) — explicitly deferred per
  proposal.md.
- Any `/LivePlay` rendering change — the flagged-tile-and-legend mechanism already generalizes to
  all six scalar characteristics; this change only needs to confirm it renders a caveat-with-no-
  value correctly, not modify it.

## Decisions

**No new "is this candidate present" concept — reuse existing Weapon/Ability name presence.** A
candidate's granting `BsSelectionEntry` has the same `Name` that, when that entry also carries a
weapon or ability profile, already surfaces through `TryResolveWeaponProfile`/`TryResolveAbility`
and ends up on `ModelLine.Weapons`/`ModelLine.Abilities` once actually selected. Presence-checking a
candidate is therefore: "does this component's present model-lines' `Weapons` contain the
candidate's `EntryName`, or does this component's present `AggregateAbilityEntry` list contain an
ability by that name?" — the same lookup `StatlineFlagRule.Matches` already performs, generalized
from exact-text matching to exact-name matching against a wider (data-derived) rule set. *Alternative
considered*: thread BSData entry ids all the way through `ArmyListParser`/`ArmyRosterEnricher`/
`ModelLine` for exact-id presence-checking. Rejected — large blast radius across the whole
army-list-import pipeline for marginal precision gain, when every real wargear choice already
resolves to a findable name today.

**Classification lives in `BsdataDatasheetMapper` (catalogue-json-ingestion), keyed by a closed
`BsModifier.Field` → characteristic allowlist.** Built from the real `Field` string values a
full-corpus scan finds (task 1), not guessed — mirrors `WeaponKeywordParser`/`ParseThreshold`'s own
"closed vocabulary, fail closed on the unrecognized case" discipline elsewhere in this mapper. An
unrecognized `Field` is left unclassified, same as an unrecognized condition shape.

**`Datasheet` exposes candidates via a new on-demand type**, e.g.
`CharacteristicModifierCandidate(EntryName, Characteristic, Value)`, mirroring the existing
`OptionalAbilityNames`/`TryResolveWeaponProfile` on-demand pattern — never enumerated as part of the
always-built `Statlines`/`Abilities`, never applied to `Datasheet`'s own fields.

**Application step sits alongside `ApplyStatlineFlagRules`, not inside it.** A new step in
`AttachedUnitAggregator.Build` walks each present component's `Datasheet` candidates, presence-checks
each per the rule above, and — when present — mutates the targeted `Statline`/`WeaponProfile` field's
`CharacteristicView` to `IsCaveated: true` with no `DerivedValue`. Kept as a separate step from
`ApplyStatlineFlagRules` (rather than folded into the same loop) because the two operate on
different candidate sources (hand-authored `StatlineFlagRuleCatalogue` vs. data-derived `Datasheet`
candidates) with different match keys (ability Name+Text vs. entry Name) — sharing a loop body would
force one shape to accommodate the other for no real benefit.

**Both real prior bugs are structurally excluded by this scope, not just avoided by care.** Bug 1
(misjudging an unrecognized condition as satisfied) can't recur for tier 1 (no condition exists to
misjudge) or tier 2 (the only condition shape recognized looks solely at the modifier's own entry,
so there is no cross-entity state to misjudge). Bug 2 (`entry.Type == "upgrade"` as an unreliable
"is this optional" signal) can't recur because there is no "unconditional" path at all — every
candidate, regardless of its own entry's structural type, goes through the same presence check.

## Risks / Trade-offs

- **[Risk]** A real tier-1/2 candidate's granting entry might not resolve to a `Weapon`/`Ability`
  name found anywhere in a real corpus roster (a bare stat-only wargear item with no accompanying
  descriptive text) → **Mitigation**: fails closed — such a candidate simply never finds a present
  match and never applies, the same outcome as if it weren't classified at all. Task 1's corpus scan
  should specifically check whether this shape exists for real; if it does, it's a follow-up, not a
  correctness bug in this change.
- **[Risk]** `BsModifier.Field` values may not map cleanly onto existing `Statline`/`WeaponProfile`
  characteristic names → **Mitigation**: closed allowlist from real corpus values; unrecognized
  `Field` left unclassified.
- **[Risk]** Automated tests and full-corpus scans alone already missed both of the reverted
  attempt's real bugs once → **Mitigation**: per `feedback_verify_bsdata_claims_precisely`, a manual
  NewRecruit cross-check against real captured rosters is a required verification step for this
  change, not optional polish.

## Open Questions

- Does `ContributingAbilities` (`IReadOnlyList<Ability>`) need a way to carry a data-derived
  candidate reference that has no backing `Ability` of its own, or does every real tier-1/2 candidate
  in practice co-locate with a resolvable `Ability` (as every example found during the reverted
  attempt's investigation — Technodermis, Armour of the Soulless Sentry — suggests, both being
  relic/enhancement text)? Resolve via the task 1 corpus scan before finalizing
  `CharacteristicModifierCandidate`'s own shape — this does not change the spec or the overall
  approach, only an implementation detail of how a caveat's source is recorded.
- Whether a genuine tier-2 (same-entry-condition) example exists in the real corpus at all, or
  whether v1 effectively only ever classifies tier 1 in practice. An empty tier-2 bucket is an
  acceptable, honest outcome, not a design failure — the requirement's contract is "conservative,"
  not "populated."
