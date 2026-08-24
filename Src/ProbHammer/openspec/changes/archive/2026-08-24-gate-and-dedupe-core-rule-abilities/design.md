## Context

See `proposal.md` - Why for the two bugs. Relevant current-state facts, confirmed directly (not
assumed):

- `BsdataDatasheetMapper.IsGameModeGated(modifiers, ctx)` + `IsProvablyAlwaysTrueInMatchedPlay`
  already read a `hidden` modifier's `conditions`/`conditionGroups` and treat a condition whose
  `ChildId` is in a known gate-id set as "provably true," combined via real AND/OR evaluation —
  but that gate-id set (`ctx.GameModeGateIds`) is built only from `ForceEntries` names
  ("Army Roster", "Crusade Force") and the check ignores each condition's own `Scope`/`Type`
  entirely (it only asks "is `ChildId` in the set"). It's called only on `BsSelectionEntry`/
  `BsSelectionEntryGroup` modifiers today, never on a `BsRule`'s own.
- `BsRule` (`Json/BsCatalogueFile.cs`) does not deserialize `modifiers` at all — explicitly
  documented as "silently dropped... like every other unmapped BSData field."
- `BsCondition` already carries `Type`/`ChildId`/`Scope` — no JSON-model gap there, only on
  `BsRule`.
- Confirmed by direct id comparison: `Templar Vows`' own `hidden` condition's `childId`
  (`36d3-36bc-68dd-40ac`) is literally Black Templars' own catalogue `id`. `Oath of Moment`'s
  `hidden` condition lists 11 catalogue ids the SAME way, all via `notInstanceOf` at
  `scope: "primary-catalogue"`. Confirmed by running the real pipeline against two different
  starting files (`Imperium - Black Templars.json`, `Imperium - Salamanders.json`) that both
  currently produce the identical, wrong-for-at-least-one-of-them ability list.
- `ResolvedBsdataCatalogue`/`BsdataClosure` already know the resolved army's own starting/primary
  catalogue — `Closure.Files[0].Catalogue.Id` — nothing new needs discovering there, just
  threading it through.
- `attached-unit-tracker`'s `Aggregate Ability View` requirement explicitly states "no
  cross-component combination or deduplication" and its own text says this *reverses* an earlier
  design (`add-live-play-ability-columns`, archived) that DID combine `Unit`-scoped abilities
  across every component. That reversal wasn't "cross-component combination is wrong" as a
  principle — it was fixing a different bug (Datasheet-level Model-scoped abilities like
  `Leader`/`Support` were silently dropped by the old two-field shape) and aligning with
  `BuildStatlines`' own per-component pattern. The old combination was also *unconditional* —
  every same-`Scope` ability across every component got merged, regardless of whether it was
  genuinely the same fact or two different abilities that happened to share a Scope. This change's
  exception is narrower and safer: it combines only when two entries are provably the *same*
  reference (confirmed same `targetId` at extraction time, not a coincidental name/scope match).

## Goals / Non-Goals

**Goals:**
- A `CoreRule` ability whose target rule is gated out for the resolved army's actual chapter (or
  game mode, reusing the identical mechanism) produces no `Ability`, for any closure.
- A `CoreRule` ability shared verbatim across multiple components of one `AttachedUnit` renders
  once, in its own place, not duplicated per component.
- Everything else about `resolve-core-rule-abilities`' shipped behavior (name construction, text
  resolution, `Origin`, universal per-datasheet Core rules like `Deadly Demise`/`Firing Deck`)
  stays exactly as-is.

**Non-Goals:**
- No `Ability.Scope` (Model/Unit) reclassification — stays hardcoded `Unit`, per explicit user
  direction this session. The dedup/span work in this change is orthogonal to that: it's about
  whether an entry belongs to *one component* or *no single component*, not about which column it
  renders in.
- No general cross-component combination for anything other than this one identity-matched case —
  `attached-unit-tracker`'s "no combination" default stays the rule for every other ability
  source.
- No `infoGroup` work (separate, already-tracked, blocked on an export).

## Decisions

### Gating: generalize the existing evaluator, don't build a parallel one
`IsProvablyAlwaysTrueInMatchedPlay`'s per-condition check becomes scope-aware instead of a single
gate-id membership test:
- `scope: "force"` / `"roster"` (existing): unchanged — `ChildId` in the known force-gate-id set
  is provably true, regardless of the condition's own `Type`, exactly as today (real corpus
  confirmed this ignores polarity safely for the two known shapes).
- `scope: "primary-catalogue"` (new): evaluated *with* the condition's own `Type`, against the
  closure's own starting catalogue id (`ctx.PrimaryCatalogueId`, a new `WalkContext` field,
  threaded in the same optional-trailing-parameter way as `forceEntries`/`glossary`) —
  `notInstanceOf` is provably true when `ChildId != PrimaryCatalogueId`, `instanceOf` when
  `ChildId == PrimaryCatalogueId`. This can't reuse the existing "type-agnostic membership" trick:
  unlike the force-gate shapes (where matching a gate id happens to mean "true" under both
  polarities observed so far), a primary-catalogue condition's truth value genuinely depends on
  its comparison type — `notInstanceOf(BT)` is true for every non-BT army and false for BT itself.
  Any other `Type` at this scope is unrecognized — not provably anything, matching the existing
  "every other condition as unknowable" default.
- Any other `scope`: unchanged, unknowable.

Both the existing `IsGameModeGated(modifiers, ctx)` entry/group call sites and the new call this
change adds from rule-level modifiers route through the same shared evaluator — same function
signature works for both, since `BsRule.Modifiers` is the same `List<BsModifier>` shape
`BsSelectionEntry.Modifiers` already is. No parallel gating mechanism, no new exception class.

**Alternative considered:** treat primary-catalogue gating as a wholly separate method, mirroring
`IsGameModeGated`'s name/shape but independent. Rejected — the two share the exact same "walk a
hidden modifier's condition tree, evaluate it against known context" shape; a second near-copy
would drift the same way this project has explicitly avoided elsewhere (`WeaponKeywordParser`,
`RuleGlossary` reuse existing mechanisms rather than duplicate them).

### `ctx.PrimaryCatalogueId` sourced from `Closure.Files[0].Catalogue.Id`, no new resolution
The starting file's own catalogue is already the closure's `Files[0]`; its `Id` is already read
today (just never threaded anywhere). `BuildDatasheet` gains this as another optional trailing
parameter (default `null` → no primary-catalogue gating applied, same fail-open convention as
`forceEntries`/`glossary`), and `ResolvedBsdataCatalogue.ResolveDatasheet` passes it.

### Rule gating happens where `ProcessRuleInfoLink` already resolves the rule
Once `RuleGlossary.TryResolve` succeeds, check the resolved `RuleDefinition`'s own gating before
building the `Ability`. This means `RuleDefinition` needs to carry its source `BsRule`'s
`Modifiers` through (today `RuleDefinition(Name, Aliases, Text)` — gains a fourth field, or the
gating check reads the modifiers before/during resolution rather than after; either is acceptable,
tasks.md should pick one based on what's least invasive to `RuleGlossary`'s existing shape — this
is an implementation-order choice, not a behavior choice). A gated-out reference produces no
`Ability`, silently — identical treatment to today's "unresolvable rule name" case, not a new
failure mode.

### Dedup identity: match by Name only, scoped to `CoreRule`-origin entries within one AttachedUnit
Two `CoreRule` entries from different components of the same `AttachedUnit` are treated as the
same fact when their `Name`s match (case-insensitive) — mirroring `WalkContext.SeenAbilityNames`'s
existing name-based dedup convention used throughout the mapper, rather than inventing a new
identity concept (a retained `targetId`) that no other `Ability` consumer needs. Since gating (the
first half of this change) already ensures only chapter/context-*applicable* Core rules survive
extraction at all, a name collision between two genuinely different rules that happen to share a
name is not a realistic risk here the way it might be for arbitrary free-text ability names.

**Alternative considered:** retain the resolved rule's own `targetId` on `Ability` (or an
extraction-time side channel) and match by that instead of `Name`. More precise in principle, but
widens `Ability`'s shape for every other consumer (weapon-tag popovers, Enhancement rendering)
that has no use for a BSData-internal id, for a precision gain this specific case doesn't need —
per resolve-core-rule-abilities' own confirmed evidence, every component's reference to the same
rule already produces byte-identical `Name`+`Text`.

**Scope note:** this dedup is `AttachedUnit`-local — it never looks across separate `AttachedUnit`s
or standalone `Unit`s in the same roster, matching the existing per-block rendering scope of
everything else on `/LivePlay`.

### The deduped entry's placement: `ComponentName: null` on `AggregateAbilityEntry`, rendered above the block
`AggregateAbilityEntry.ComponentName` (currently always a real string) becomes optional; a `null`
value means "belongs to no single component," rendered as its own row above every component's
statline rows — a third placement alongside the existing row-bound (`StatlineName` set) and
component-wide (`StatlineName: null`, `ComponentName` set) cases, not a variant of either. For a
standalone `Unit` (one component, no `AttachedUnit`), the same detection still applies — a
qualifying `CoreRule` ability still gets `ComponentName: null` for consistency (one code path,
no standalone-vs-attached special case), even though visually a "no component" row and a
"component-wide, one component" row are indistinguishable when there's only one component.

Collapse rule for this new placement: hidden only once **every** component's every model-line is
fully dead — not per this-component's-own-span like the existing component-wide collapse rule,
since the fact this entry represents (an army-wide rule) remains true as long as any part of the
attached formation still lives. Stated explicitly because it's a materially different rule from
the two collapse rules `Statline Ability Column Rendering` already documents.

### Verification: both a fixture-based test and a real-corpus regression check
- Fixture-based (default suite, deterministic): a `BsRule` with a `primary-catalogue`-scoped
  `hidden` modifier, resolved against two different synthetic starting-catalogue ids, asserting
  opposite inclusion — mirrors `CoreRuleAbilityTests`' own fixture style.
- A small, targeted, explicit-gated (needs the live clone) regression test resolving `Scout Squad`
  against both `Imperium - Black Templars.json` and `Imperium - Salamanders.json`, asserting the
  exact real-world result this session found by hand (`Templar Vows` only for the former,
  `Oath of Moment` only for the latter) — not a full corpus sweep, a two-closure targeted check,
  so the specific bug this change fixes can never silently regress.

## Risks / Trade-offs

- **[Risk]** `IsProvablyAlwaysTrueInMatchedPlay`'s AND/OR evaluation is still a narrow, 2-value
  boolean evaluator (per its own existing doc comment), not general condition-tree evaluation.
  Adding a second recognized `scope` widens what it claims to understand, increasing the surface
  a future unrecognized condition shape could silently mis-evaluate. → **Mitigation:** unchanged
  default behavior for anything not exactly matching a known scope+type combination (unknowable,
  never provably true) — a shape this doesn't recognize fails safe (content stays visible,
  matching this project's existing "extraction is eager, exclusion is conservative" posture)
  rather than failing gated.
- **[Risk]** Name-based dedup could, in principle, collapse two different rules that happen to
  share a display name within one `AttachedUnit`. → **Mitigation:** scoped narrowly to
  `CoreRule`-origin entries only (not applied to Intrinsic/Enhancement/OptionalGrant), and gating
  already filters to context-applicable rules first — no real corpus case found this session where
  this would misfire.
- **[Trade-off]** This reopens cross-component combination in one narrow case, right after a prior
  change deliberately moved away from it generally. Documented above why this instance doesn't
  reintroduce that change's original problem (unconditional same-Scope merging) — worth a second
  look at review time given the history.

## Post-Delivery Correction

Found via user feedback after the change above was applied and verified — two real gaps, both
fixed within this same change rather than as a separate proposal, since both are completions of
work this change had already started, not new scope.

### The "shared by 2+ present components" dedup trigger was the wrong signal
User-reported: `Templar Vows` got the "own box above" treatment on a multi-component
`AttachedUnit`, but rendered as an ordinary inline entry on a standalone `Unit` (Impulsor, Scout
Squad) - even though it's just as much an army-wide fact there. The "2+ contributors" trigger was
always an accidental proxy for "is this an army-wide fact," true for this Black Templars roster's
multi-component attached units but false for anything with only one component.

**Fix:** the real signal already existed from the gating work - whether the rule's own gating
carries a `primary-catalogue` condition at all (structurally, regardless of whether it evaluates
excluded for the current closure). `AbilityOrigin` gains `ArmyRule` (chapter/sub-faction-exclusive
Core rule, e.g. Oath of Moment/Templar Vows) alongside the existing `CoreRule` (no such
exclusivity, e.g. Deadly Demise/Firing Deck/Infiltrators) - a new
`BsdataDatasheetMapper.HasPrimaryCatalogueScope` presence check (deliberately simpler than the
full gating evaluator - "is this chapter-scoped at all", not "is it excluded here") decides which.
`AttachedUnitAggregator`'s promotion (renamed `PromoteArmyRuleAbilities`) now triggers on
`Origin == ArmyRule` unconditionally - even a single contributor gets `ComponentName: null` - not
on a contributor headcount. This also revisits (correctly, given new evidence) `resolve-core-rule-
abilities`' own earlier design.md argument against splitting `CoreRule` by chapter-exclusivity
("the source data... declines to make" that distinction) - it doesn't decline; `scope: "primary-
catalogue"` on the rule's own modifiers is exactly that distinction, just not read at the time.

### Shield Dome was still colliding - the original bug was never actually fixed
User-reported (second time - the first report during this same conversation got lost when the
discussion moved to the mockup exercise). Impulsor's row-bound `Shield Dome` cell and its
component-wide `Transport`/`Assault Vehicle`/etc. cell occupy identical grid coordinates whenever
a component has exactly one statline row and carries both kinds of ability - this change's own
`rowOffset` work only ever added a *new* row for the multi-component dedup case; it never touched
this pre-existing single-row collision, which is a different bug with a different cause.

**Fix:** `LivePlay.cshtml.cs`'s `BuildComponentAbilitySpans` now returns an adjusted
`statlineBlocks` alongside its spans - when a component-wide span covers exactly one run
(`FirstRunIndex == LastRunIndex`), that run's own row-bound abilities are absorbed into the span's
own `ModelAbilities`/`UnitAbilities` lists (row-bound first, then component-wide), and that run's
own ability lists are cleared in the returned adjusted blocks so the now-redundant row-bound cell
never renders. A multi-row span is left untouched - its grid-row range strictly contains but is
never identical to any one row-bound cell's range, so the two nest visually rather than colliding.

Both fixes verified: new `AttachedUnitAggregatorTests`/`PrimaryCatalogueGatingTests` coverage for
the `ArmyRule` split and unconditional promotion; a new `LivePlayAbilityRenderingTests` case for
the absorption merge; full suite green; `docker compose up` + a real browser session against the
real Templars export confirmed both fixes visually (Impulsor now shows `Templar Vows` in its own
box *and* `Shield Dome` alongside `Transport`/`Assault Vehicle`/etc., all visible together) with no
regression to the already-verified `AttachedUnit` rendering.
