## Context

See proposal.md - Why for motivation. This is Stage 2 of a two-stage effort; Stage 1
(`widen-baseline-generation-coverage`) is a hard prerequisite and must be complete before this starts
— it closes the one real coverage gap (plural "have" InSv phrasing) that makes deleting
`InvulnerableSaveCaveatClassifier` safe. Current state this design has to work with:

- `AttachedUnitAggregator.Build` runs three characteristic-resolution steps today, in order:
  `ApplyStatlineFlagRules` (matches every present ability against the baseline), then
  `ApplyCharacteristicModifierCandidates` (always caveats a present structural candidate, guarded by
  "skip if the field already carries a contributing ability" so it doesn't regress an
  already-resolved value). A fourth, earlier step happens outside `Build` entirely, at mapper time:
  `BsdataDatasheetMapper`/`BattleScribeRosterMapper`'s own `ResolveInvulnerableSave` calls
  `InvulnerableSaveCaveatClassifier` against a footnote's linked ability text, before that ability is
  ever seen by anything in `Domain.Roster`.
- The bug that started this investigation: `ResolveInvulnerableSave`'s own linked-ability lookup
  (`ResolveCaveatAbility`) is a *separate* code path from the general ability-collecting walk
  (`WalkEntry`/`WalkInfoLink`/`ProcessProfile`/`ProcessAbilityProfile`) that independently discovers
  the exact same ability profile and adds it to `Datasheet.Abilities` at component-wide scope
  (`StatlineName: null`) — even when the ability's true relevance is one specific named Statline.
  `AttachedUnitAggregator.BuildAbilities` has no way to know these are the same ability serving two
  different purposes.
- `AggregateAbilityEntry(ComponentName, StatlineName, Ability, ...)` already distinguishes
  component-wide (`StatlineName: null`) from model-line/Statline-specific (`StatlineName` set)
  attachment — this is the existing vocabulary any new "characteristic-pinned, deferred" shape should
  most likely reuse rather than duplicate.
- `RuleTarget` (`Self`/`AttachedUnit`/`Keyword`/`Unconditional`) already models "how far does a
  matched entry's effect reach" independently of "where was this ability found" — `Keyword`/
  `Unconditional` are the two cases with no roster-wide evaluator built yet, and stay that way after
  this change.
- A second, independently-confirmed real collision exists beyond InSv: Auric Mantle (Adeptus
  Custodes Enhancement) is reachable both as an ordinary present ability (resolves via the baseline,
  `Improve W 2`) and as a `CharacteristicModifierCandidate` (always caveats) — today it lands on the
  *correct* outcome only because `ApplyStatlineFlagRules` happens to run first and the existing guard
  happens to see a non-empty `ContributingAbilities` by the time `ApplyCharacteristicModifierCandidates`
  runs. This change removes the accident, not just the bug.

## Goals / Non-Goals

**Goals:**
- One resolution pass in `AttachedUnitAggregator.Build`, one trust boundary (the baseline), for every
  characteristic-affecting ability or association, regardless of source.
- No parser (`BsdataDatasheetMapper`, `BattleScribeRosterMapper`) makes a caveated-vs-resolved
  judgment about ability text ever again — a parser either fully resolves from raw catalogue text
  alone, or records a plain fact (an ability exists, associated with a specific characteristic at a
  specific, correct attachment scope) with no interpretation attached.
- Fix the root-cause scoping bug: a characteristic-pinned association is recorded once, at its true
  scope, never independently re-discovered at a broader scope by the general ability walk.
- Delete both narrower, now-redundant live mechanisms (`InvulnerableSaveCaveatClassifier`,
  `ApplyCharacteristicModifierCandidates`) rather than leave them running alongside the unified path.

**Non-Goals:**
- Building the `Keyword`/`Unconditional` (cross-unit, "incoming") evaluator. This design reserves
  conceptual room for a future fourth attachment-scope case sourced from outside a unit's own
  components (a Detachment rule, an Army rule, another unit's own keyword-targeted ability) but does
  not build it — those `RuleTarget` cases stay unevaluated, exactly as today.
- Redesigning `CharacteristicModifierCandidate`'s own classification logic
  (`ClassifyCharacteristicModifierCandidates`, `IsTier1OrTier2`) beyond adding InSv to its Field
  allowlist.
- Any change to how `/LivePlay` renders a flagged/caveated tile — the marker/legend mechanism already
  reads `ContributingAbilities` generically.

## Decisions

### 1. A parser hands off a fact, never a judgment
When raw catalogue characteristic text alone doesn't fully determine InSv, the parser stops short of
`InvulnerableSaveCaveatClassifier`'s own text-interpretation step entirely — it records the linked
ability plus the characteristic and scope it's associated with, and nothing more. Whether that
resolves is decided exactly once, later, by `AttachedUnitAggregator.Build`. This directly answers the
user's own stated objection during exploration to an earlier draft of this idea (having the parser
still emit a `Caveated(...)` *view* pre-labels the outcome, which is a judgment by another name) —
see this change's own conversation history for the exact correction. **Alternative considered**: keep
emitting `Caveated(...)` from the parser, just stop calling the classifier. Rejected per that
correction — a `Caveated` view already commits to "this is unresolved, and here is its one recorded
source," which is exactly the premature judgment being removed; the fact handed off must be able to
resolve into either a caveated *or* a resolved outcome with no pre-commitment either way.

### 2. Reuse the existing attachment-scope vocabulary; no new hierarchy for v1
A characteristic-pinned association records its scope using the same `(ComponentName, StatlineName)`
pair `AggregateAbilityEntry` already carries for a ModelLine-sourced ability — not a new
`AbilityAttachmentScope` type. This is deliberately the minimum needed to fix the actual bug (the
association's true scope is always exactly the one named Statline it was resolving InSv for, never
broader) without inventing scope levels no real case needs yet. See Open Questions below for why this
isn't fully settled.

### 3. `CharacteristicModifierCandidate` is retired as a live mechanism, not merely reordered
`ApplyCharacteristicModifierCandidates` is deleted outright rather than kept as a fallback for a
candidate the baseline doesn't resolve. **Alternative considered**: keep it as a last-resort caveat
for a present candidate with no baseline entry (closer to today's behavior). Rejected — this is
exactly the redundant-mechanism shape this whole change removes; a candidate with no baseline entry
should produce no flagged value at all (the same outcome an ordinary unmatched ability already
produces), not a second, differently-behaved fallback path. Its own `RawValue` stays uninterpreted at
runtime (see Open Question (c)) — it remains purely an offline signal feeding Stage 1's report tool.

### 4. InSv rejoins the candidate Field allowlist now that there's one safe consumer
The exclusion's own documented reason (no safe consumer existed) is the only reason recorded for it;
once decision 3 makes `CharacteristicModifierCandidate` purely an offline-feed and decision 1 makes
InSv's own parse-time path defer rather than pre-judge, both paths converge on the same single
Build-time resolution with no collision risk left to guard against.

## Risks / Trade-offs

- **[Risk]** Deleting two live mechanisms and replacing them with one is a real behavior change for
  every roster carrying an ability either one used to touch — not purely additive like Stage 1.
  → **Mitigation**: every real scenario either mechanism covered today has a corresponding scenario in
  this change's own spec deltas; Stage 1 exists specifically to make sure the baseline's coverage is a
  superset, not a subset, before this ships. Real-corpus verification (per this project's own
  established practice) against known InSv-footnote and Auric-Mantle-shaped units is a hard
  requirement of the task breakdown, not optional polish.
- **[Risk]** `CharacteristicModifierCandidate`'s own `RawValue` never gets a resolution path in this
  change — a real candidate whose granting ability has no matchable prose text (Open Question (c))
  would silently stop producing any flagged value at all, a regression from "always caveated" to
  "nothing shown."
  → **Mitigation**: needs confirming against the live corpus before implementation — if such a case
  exists, it changes the task breakdown (see Open Questions); if it doesn't, no mitigation is needed
  beyond confirming that during the corpus-verification task.
- **[Risk]** Two import pipelines (`BsdataDatasheetMapper`, `BattleScribeRosterMapper`) both need the
  identical simplification to `ResolveInvulnerableSave`, and drifting between them would reintroduce
  the exact "identical behavior regardless of pipeline" gap `invulnerable-save`'s own spec already
  guards against.
  → **Mitigation**: both mappers' relevant tests already assert byte-for-byte pipeline parity; keep
  that assertion, don't relax it.

## Migration Plan

Single-PR, internal-only breaking change (no external contract, per proposal.md's Impact):
1. Widen `CharacteristicModifierCandidate`'s consumption is already done by this point (Stage 1
   shipped); confirm the checked-in baseline actually covers every real corpus case this change's own
   verification will exercise.
2. Add InSv to `BsdataDatasheetMapper`'s `CharacteristicFieldIds` allowlist.
3. Simplify both mappers' `ResolveInvulnerableSave` to stop calling
   `InvulnerableSaveCaveatClassifier`; resolve Open Question (b)'s carrier shape first, since every
   later task depends on it.
4. Rework `AttachedUnitAggregator.Build`'s resolution pipeline: extend (or replace)
   `ApplyStatlineFlagRules` to also resolve a deferred association at its own recorded scope; delete
   `ApplyCharacteristicModifierCandidates`.
5. Delete `InvulnerableSaveCaveatClassifier` and its own tests; update/replace any test asserting the
   old three-mechanism behavior.
6. Real-corpus verification: run a real captured export through `/LivePlay` covering at least one
   InSv-footnote unit and Auric Mantle (or its real equivalent), confirming both now resolve through
   the single unified path with the expected values.

**Rollback**: revert the commit — no persisted-data migration; the only checked-in data this change
touches is the baseline JSON, which Stage 1 already grew and this change only reads from.

## Open Questions

- **(a) Does attachment scope need a new formal type, or does `AggregateAbilityEntry`'s existing
  `(ComponentName, StatlineName)` pair suffice for v1?** Decision 2 above tentatively reuses the
  existing pair; this stays open because the exact mechanics of threading a deferred association
  (which currently has no equivalent to `AggregateAbilityEntry` at all — it needs to travel from the
  mapper, through `Datasheet`/`Statline`, into `Build`) may or may not fit cleanly onto that existing
  shape once actually attempted. Resolve during task 3/4 of the migration plan; changes the exact task
  breakdown for that step but not this change's own specs or overall approach.
- **(b) What type carries "an ability is associated with a specific characteristic at a specific
  scope, deferred, no judgment yet"?** Not `Caveated(...)` (Decision 1's whole point). Candidate
  shapes not yet compared: a new field on `Statline` itself; a new field on `ModelLine`; a
  `Datasheet`-level list mirroring `CharacteristicModifierCandidates`' own existing shape but carrying
  a real `Ability` instead of an `EntryName` string. Must be resolved before task 3 can start — it
  changes what that task actually writes.
- **(c) Does `CharacteristicModifierCandidate`'s own `RawValue` ever need a non-baseline resolution
  path?** Every real example found during exploration (Auric Mantle, Vexilla) has matchable ability
  text and can go through the baseline uniformly. Unconfirmed whether a real corpus candidate exists
  with a structural signal but no usable prose at all. Resolve via the corpus-verification task before
  finalizing whether Risk 2 above needs its own mitigation task added.
