## Context

See proposal.md - Why. Grounding facts established this session, not yet written down anywhere
else:

- `RuleTarget` (`Domain/Catalogue/RuleTarget.cs`) already has a `KeywordRuleTarget(string Keyword)`
  case, and its own doc comment already names the exact gap this change closes: "Evaluating this
  predicate against an actual resolved roster is out of scope for this type." `RuleEffectClassifier`
  already extracts Faith-Fuelled Resolve's text ("Friendly SWORD BRETHREN SQUAD units have +1
  OC...") as `KeywordRuleTarget("SWORD BRETHREN SQUAD")` plus a resolvable `Improve Oc 1` Effect,
  confirmed in the checked-in baseline and in `.claude/domain-model/rule-effect-classification.md`'s
  own ground-truth section. Nothing runtime-side consumes this today.
- `ResolvedDetachment(Name, Rules: IReadOnlyList<DetachmentRule>)` lives on `ArmyRoster.Detachments`
  directly. `DetachmentRule(Name, Text)` is a bare record — not an `Ability`, no `Scope`/`Origin` —
  and is resolved once per pipeline (`ArmyRosterEnricher`/BSData via
  `Domain.Catalogue.Bsdata.DetachmentRuleTextExtractor`; `BattleScribeRosterMapper.MapDetachment`
  from the roster's own inline `selections[].rules[]`). Today it is consumed only by
  `_ArmyHeader.cshtml`'s Detachment column — never looked up against the baseline, never connected
  to any specific unit.
- `KeywordResolution.EffectiveKeywords(ICombatUnit)` already unions `Datasheet.Keywords` across
  every present component of a combined `ICombatUnit`, plus each present model-line's own Keywords.
  This session confirmed (via real 40k Core Rules text, corroborated against this codebase's own
  `roster-model` spec's existing "Attached Unit Keyword Resolution" requirement) that this union is
  the *correct* implementation of the real "a rule targeting units with keyword X reaches the whole
  combined unit once any present component carries X" mechanic — not an approximation this change
  needs to work around. A rule phrased at the model level instead ("models with keyword X") would
  need the narrower, already-existing model-level check the same requirement also describes — not
  exercised by this change, since no real Detachment-rule text found so far is phrased that way.
- `AttachedUnitAggregator.Build(ICombatUnit, RuleClassificationBaseline)` (`AttachedUnitAggregator.cs`)
  operates on exactly one unit at a time, with no visibility into the rest of `ArmyRoster.Units`.
  `TryGetApplicableEntry` (line 126) already performs a baseline lookup by normalized ability Text
  for every present ability, but explicitly discards any match whose `Target` is `KeywordRuleTarget`
  or `UnconditionalRuleTarget` — the same gap named above, from the resolution side.
- `ArmyRosterEnricher.Enrich` builds `units` (every `Unit`'s `Datasheet`/`Keywords` fully resolved)
  and `detachments` (every `DetachmentRule.Text` known) independently, only combining them at
  `new ArmyRoster(...)` — an open seam between "everything needed is known" and "the roster is
  sealed."

## Goals / Non-Goals

**Goals:**
- Match a Detachment rule's already-classified `KeywordRuleTarget` against every `ICombatUnit` in a
  resolved roster, using the already-correct `EffectiveKeywords` union.
- Render a match as an orphaned ability on the matched unit, reusing the existing Army Rule
  promotion's visual slot with no new `/LivePlay` markup.
- Apply a matched rule's already-classified Effect (when one exists and isn't caveated) to the
  matched unit's Statline, reusing the existing `statline-flag-rules` resolution mechanism.
- Keep the matching step BSData-independent so both import pipelines share one implementation.

**Non-Goals:** see proposal.md's own Non-Goals section — not restated here.

## Decisions

**D1. Matching happens once, at roster-enrichment time, not per-render.** `KeywordRuleTarget`
membership depends only on `Datasheet.Keywords`/`FactionKeywords` (static catalogue data) and
`ResolvedDetachment.Rules` (static once parsed) — neither depends on casualty/RemainingCount/live
game state. Computing it once during `Enrich` (mirroring how `Enhancements` are resolved once, not
re-resolved every render) keeps `AttachedUnitAggregator.Build`'s per-request signature untouched
and avoids threading the whole `ArmyRoster` into a function deliberately built to operate on one
unit in isolation. Rejected alternative: recompute per render inside `AttachedUnitAggregator.Build`,
given the whole roster as a new parameter — rejected because it would change a function every other
capability already depends on having a narrow, single-unit signature, for a computation that never
needs to be live.

**D2. `InboundAbilities` lives on `ICombatUnit`, not `Unit`.** This session initially assumed
matching needed to happen per-component (to avoid an attached Character wrongly inheriting a
Sword-Brethren-scoped rule) — that assumption was wrong. The real 40k Core Rules keyword-propagation
mechanic (confirmed against this codebase's own pre-existing `roster-model` spec) means a
"units with keyword X" rule genuinely reaches the *whole* combined `AttachedUnit` once any present
component contributes that keyword — the union `EffectiveKeywords` already computes. So the match
result is a fact about the combined unit as a whole, not about any one component, and belongs where
`IsHalfStrengthOverride`/`IsBattleShocked` already live: directly on the interface, mutable, set
once post-construction. Rejected alternative: a property on `Unit` (the leaf/component class),
which would have required a second, separate cross-component aggregation step to reconstruct
"does the combined unit qualify" — unnecessary, since `EffectiveKeywords` already does that
aggregation.

**D3. Matching is scoped to `KeywordRuleTarget` only.** `RuleTarget` has four cases; only
`KeywordRuleTarget` is meaningfully evaluable against a roster today. `UnconditionalRuleTarget`
(roster-wide, no qualifier) is a real, valid shape per `RuleTarget`'s own doc comment but has no
confirmed real Detachment-rule example yet — extending the matching rule to it would be cheap but
unverified, deliberately deferred rather than guessed at (see proposal.md Non-Goals). `SelfRuleTarget`
on a Detachment rule (the classifier's own explicit default/fallback) has no meaningful "self" to
bind to for an abstract army-level rule — deliberately produces no match, the same "fail closed on
the unrecognized/inapplicable case" convention `TryGetApplicableEntry` already uses today for
`KeywordRuleTarget`/`UnconditionalRuleTarget`.

**D4. A `DetachmentRule`-origin ability is treated as WholeUnit-scoped for Effect application, not
re-derived from its own baseline `Target`.** Once the roster-wide keyword match has already
confirmed a rule belongs to this specific `ICombatUnit`, that decision should not be re-litigated by
a second, narrower check downstream in `statline-flag-rules`. Its existing WholeUnit-scope branch
already expresses exactly the right semantic ("applies to every row of this resolved unit") —
reusing it needs only a narrow, `Origin`-keyed exception to `Target-Scoped Application`'s existing
"keyword-scoped target produces no flagged value" rule, not a new code path.

**D5. The shared matching step lives in `Domain.Roster`, independent of `Domain.Catalogue.Bsdata` —
and is called once, after `ArmyRoster` construction, from `ArmyRosterProvider.Build`.** Same
BSData-independence precedent as `InvulnerableSaveCaveatClassifier`/`ArmyRuleNameLookup`/
`RuleEffectClassifier` — this matching logic (a baseline lookup plus keyword-set membership) needs
nothing BSData-specific; it operates purely on already-resolved `Unit`/`ResolvedDetachment` domain
objects. Revised from this design's original plan (calling it separately from inside both
`ArmyRosterEnricher.Enrich` and `BattleScribeRosterMapper.Map`) after finding `ArmyRosterProvider.
Build` (`src/ProbHammer.Web/Services/ArmyRosterProvider.cs`) already dispatches to both pipelines
and is the one place both call sites (`/Import` and `/LivePlay`) already converge — calling the
matching step once there, over the already-built `ArmyRoster.Units`/`Detachments`, needs zero
changes to either `Enrich`'s or `Map`'s own signature, and fits `InboundAbilities`'s own "settable
post-construction" shape (D2) exactly: mutate each `ICombatUnit.InboundAbilities` in place after
`new ArmyRoster(...)` has already run, the same way `IsHalfStrengthOverride`/`IsBattleShocked` are
set well after construction too. `ArmyRosterProvider` gains a `RuleClassificationBaseline`
constructor parameter (already a registered singleton in `Program.cs`).

**D6. The synthesized `Ability`'s `Scope` is `Unit`.** Faith-Fuelled Resolve's own text is
unit-scoped, and every Detachment-rule shape confirmed so far is phrased at the unit level, not the
model level — consistent with `AbilityScope.Unit` being the only value this change's matching step
ever produces. A future model-level qualifier (per Lysander's "non-Character models," explored this
session but out of scope here) would need its own, separate handling.

## Risks / Trade-offs

- [Risk] A Detachment rule's baseline entry could in principle carry a `WeaponCharacteristicEffect`
  rather than a `ScalarCharacteristicEffect` — `AttachedUnitAggregator.BuildWeapons` has its own,
  separate bearer-scope check that this change does not touch. → [Mitigation] No real Detachment-rule
  baseline entry classified so far carries one; named as a Non-Goal in proposal.md rather than
  guessed at. A future change should re-check the corpus before assuming this gap is safe to leave
  indefinitely.
- [Risk] Two different selected Detachments could each carry a keyword-matched rule sharing the
  exact same Name. `AggregateAbilityEntry` today has no cross-source dedup for non-Army-Rule-origin
  entries. → [Mitigation] No dedup attempted in this change; two identically-named
  `DetachmentRule`-origin entries would render as two separate orphaned rows — an accepted, narrow
  limitation until a real corpus example is found.
- [Risk] ~~The `FactionKeywords`/`Keywords` split could mean some real Detachment-rule keyword
  targets never match anything, silently.~~ **Resolved, not a risk**: confirmed `FactionKeywords` is
  always empty in the real mapper and every real keyword (including "Sword Brethren Squad" itself)
  lands in `Keywords` — see Open Questions.

## Migration Plan

1. Add `AbilityOrigin.DetachmentRule` and `ICombatUnit.InboundAbilities`.
2. Build the shared matching helper in `Domain.Roster`; call it once from `ArmyRosterProvider.Build`
   (per D5), after either pipeline's `ArmyRoster` is built.
3. Extend `AttachedUnitAggregator.BuildAbilities` to read `InboundAbilities`; extend
   `TryGetApplicableEntry`/`IsBearerOf` (or the equivalent `statline-flag-rules` call site) for the
   `DetachmentRule`-origin exception.
4. Verify end-to-end against a real captured Black Templars export selecting "Marshal's Household"
   with a Sword Brethren Squad in the list (this project's standing practice for changes touching
   `AttachedUnitAggregator`) — expected to show a visible result: the Sword Brethren Squad's unit
   block gains an orphaned "Faith-Fuelled Resolve" ability and its OC tile reads one higher than the
   Datasheet's base value.

No baseline JSON changes — this change consumes Faith-Fuelled Resolve's existing, already-verified
baseline entry as-is.

## Open Questions

(none remaining)

**Resolved** — does the matching step need to check `Datasheet.FactionKeywords` in addition to
`Datasheet.Keywords`? No: `BsdataDatasheetMapper.BuildDatasheet` (`Domain/Catalogue/Bsdata/
BsdataDatasheetMapper.cs`, line ~90) unconditionally passes `factionKeywords: []` when constructing
every real `Datasheet` — `FactionKeywords` is never populated by the real mapper at all.
`MapCategoryLinks` (line ~103) routes every `categoryLinks` entry into the single `Keywords` set,
stripping a literal `"Faction: "` prefix when present but otherwise keeping the name verbatim,
"including an entry matching the owning entry's own name" (its own doc comment). Confirmed directly
against the real corpus (`Imperium - Black Templars.json`, the Sword Brethren Squad datasheet's own
entry, line ~1558): its `categoryLinks` carries `"Sword Brethren Squad"` (self-referencing),
`"Faction: Adeptus Astartes"`, and `"Faction: Black Templars"` — all three land in `Keywords` once
mapped. `KeywordResolution.EffectiveKeywords`, already built from `Keywords` alone, therefore
already covers every real keyword shape this change's matching step needs. D3/D4/D5/D6 are
unaffected; no design change results from this resolution.
