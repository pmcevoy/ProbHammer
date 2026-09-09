## Context

See proposal.md - Why for motivation. This is Stage 2 of a two-stage effort; Stage 1
(`widen-baseline-generation-coverage`) is a hard prerequisite and must be complete before this starts
— it closes the one real coverage gap (plural "have" InSv phrasing) that makes deleting
`InvulnerableSaveCaveatClassifier` safe. Current state this design has to work with:

- `AttachedUnitAggregator.Build` runs two characteristic-resolution steps today, in order:
  `ApplyStatlineFlagRules` (matches every present ability against the baseline), then
  `ApplyCharacteristicModifierCandidates` (always caveats a present structural candidate, guarded by
  "skip if the field already carries a contributing ability" so it doesn't regress an
  already-resolved value). A third step happens outside `Build` entirely, at mapper time:
  `BsdataDatasheetMapper`/`BattleScribeRosterMapper`'s own `ResolveInvulnerableSave` calls
  `InvulnerableSaveCaveatClassifier` against a footnote's linked ability text, before that ability is
  ever seen by anything in `Domain.Roster`.
- The bug that started this investigation: `ResolveInvulnerableSave`'s own linked-ability lookup
  (`ResolveCaveatAbility`) is a *separate* code path from the general ability-collecting walk
  (`WalkEntry`/`WalkInfoLink`/`ProcessProfile`/`ProcessAbilityProfile`) that independently discovers
  the exact same ability profile and adds it to `Datasheet.Abilities` at component-wide scope
  (`StatlineName: null`) — even when the ability's true relevance is one specific named Statline.
- `ResolveCaveatAbility` only ever resolves an ability under one of exactly two naming conventions:
  the digit-parameterized `"Invulnerable Save ({digit}+*)"`, or the generic `"*Invulnerable Save"`.
  Every real corpus occurrence of either literal name pattern is confirmed to be this internal
  mechanism — the method's own doc comment records a real base-catalogue collision (two different
  profiles both named `"Invulnerable Save (4+*)"` with opposite melee/ranged meanings, resolved by id,
  never by name alone) that would be nonsensical for a genuinely player-facing, independently-readable
  ability to have.
- `Datasheet`'s own constructor already filters certain ability names out of `Datasheet.Abilities`
  centrally, for the identical class of reason: `ExcludedAttachmentAbilityNames` removes `"Leader"`/
  `"Support"`/`"Attached Unit"` because those names only ever restate a fact the roster's own
  attachment relationships already represent directly. Both BSData and BattleScribe pipelines already
  funnel through this one constructor.
- A second, independently-confirmed real collision exists beyond InSv: Auric Mantle (Adeptus
  Custodes Enhancement) is reachable both as an ordinary present ability (resolves via the baseline,
  `Improve W 2`) and as a `CharacteristicModifierCandidate` (always caveats) — today it lands on the
  *correct* outcome only because `ApplyStatlineFlagRules` happens to run first and the existing guard
  happens to see a non-empty `ContributingAbilities` by the time `ApplyCharacteristicModifierCandidates`
  runs.
- Corpus-verified during this design's own exploration (not left as an assumption): every real,
  tier-1, recognized-field `CharacteristicModifierCandidate` in the live BSData clone (16 total) has
  real ability text reachable through the existing general ability-resolution walk — either a local
  `Abilities` profile or a `"type": "profile"` infoLink, the same two shapes `Datasheet.Abilities`/
  `OptionalAbilityNames` already resolve. Two candidates initially looked like exceptions
  (`"Sentinel Blade & Praesidium Shield"`, `"Pyrithite Spear & Praesidium shield"`) but both resolve
  their shared `"Praesidium Shield"` ability text through an infoLink once followed.

## Goals / Non-Goals

**Goals:**
- One resolution pass in `AttachedUnitAggregator.Build` for every characteristic-affecting present
  ability, reading only the checked-in baseline — unchanged from `apply-rule-effect-baseline`.
- Fix the root-cause scoping bug directly: the InSv-caveat-internal ability is recorded once, at its
  true scope (the one named Statline it belongs to), and is never independently rediscovered by the
  general ability walk at a broader scope.
- A caveated invulnerable save gets exactly one chance to resolve against the baseline during roster
  aggregation, using the same proven `InvulnerableSaveEffectResolver` `apply-rule-effect-baseline`
  already built.
- Delete both narrower, now-redundant live mechanisms (`InvulnerableSaveCaveatClassifier`,
  `ApplyCharacteristicModifierCandidates`) rather than leave them running alongside the unified path.
- Do this with the smallest mechanism that actually closes the gap — reuse `Datasheet`'s existing
  name-exclusion pattern and `Statline.InSv`'s own existing per-Statline attachment, rather than
  introduce a new scope/association type.

**Non-Goals:**
- Building the `Keyword`/`Unconditional` (cross-unit, "incoming") evaluator. `RuleTarget`'s existing
  cases stay unevaluated, exactly as today — nothing in this design forecloses building that later, but
  nothing in it is built now either.
- Any general-purpose "ability association deferred to Build" abstraction. Explored during this
  design's own conversation and rejected in favor of two much smaller, independent fixes (see
  Decisions 1-3) — see the Open Questions this design originally carried, now resolved below rather
  than left open.
- Redesigning `CharacteristicModifierCandidate`'s own classification logic
  (`ClassifyCharacteristicModifierCandidates`, `IsTier1OrTier2`) beyond adding InSv to its Field
  allowlist.
- Any change to how `/LivePlay` renders a flagged/caveated tile — the marker/legend mechanism already
  reads `ContributingAbilities` generically.

## Decisions

### 1. The mapper's own output shape is unchanged; only what it does before producing it changes
`ResolveInvulnerableSave` keeps returning `InvulnerableSaveCharacteristicView.Caveated(fallbackValue,
ability)` for a footnoted/split value it can't fully determine — exactly the shape it returns today.
The only change is that it no longer calls `InvulnerableSaveCaveatClassifier` first to try
interpreting that ability's Text. `Caveated(...)` was never a final, judged state — it's an honest
"not yet resolved, here is the one ability responsible" fact, and nothing about producing it forecloses
a later step from replacing it (see Decision 3). **Alternative considered, and rejected during this
design's own exploration**: a new, dedicated "deferred, no judgment yet" carrier type distinct from
`Caveated(...)`. Rejected once it became clear the actual objection was to the mapper *interpreting the
ability's text*, not to the *shape* it hands back — removing the interpretation step already satisfies
that concern with the existing shape; a new type would have solved a problem that didn't exist.

### 2. Fix the scope bug by excluding the internal ability names, not by inventing scope machinery
Add the two InSv-caveat-internal name conventions (`"Invulnerable Save ({N}+*)"` as a pattern,
`"*Invulnerable Save"` literally) to `Datasheet`'s existing exclusion mechanism alongside
`"Leader"`/`"Support"`/`"Attached Unit"`. Once excluded from the general walk, the ability is *only*
ever reachable through `Statline.InSv.ContributingAbilities` — its one correct home, already scoped
exactly right by virtue of living on that specific Statline. No new `AbilityAttachmentScope` type, no
generalization of `AggregateAbilityEntry`'s `(ComponentName, StatlineName)` pair, is needed — the bug
was never "the correct scope doesn't exist," it was "a second, wrong scope also gets populated."
**Alternative considered**: a new formal scope type recording where a characteristic association
"really" belongs, checked against `AggregateAbilityEntry`'s own attachment at resolution time.
Rejected — this would solve the bug by cross-checking two scopes against each other every time,
instead of the simpler fix of ensuring only one scope is ever populated in the first place.

### 3. A new, small, InSv-specific Build step — not a generalization of `ApplyStatlineFlagRules`
`AttachedUnitAggregator` gains one new step: for every `AggregateStatlineEntry` whose `Statline.InSv`
is still caveated, resolve its single contributing ability's normalized Text against the baseline via
`InvulnerableSaveEffectResolver`, exactly the same resolver `ApplyStatlineFlagRules` already calls for
a present ability's own InSv-granting match. This step is deliberately narrow — scoped to InSv only,
reading `Statline.InSv` directly rather than joining through `BuildAbilities`' present-ability list —
because Decision 2 already ensures there's no ability-presence collision left to coordinate against.
Ordering relative to `ApplyStatlineFlagRules` doesn't matter for correctness (there's nothing left for
the two to race over), though placing it right after `BuildStatlines` reads most naturally: resolve
what's already known to need resolving, then apply ability-presence-driven flags.
**Alternative considered**: fold this into `ApplyStatlineFlagRules` itself as a generalized "resolve a
deferred association" mode. Rejected — `ApplyStatlineFlagRules` is fundamentally "walk present
abilities, check the baseline"; this new step is "walk statlines, check one already-known ability" —
different iteration shape, different input, no shared logic beyond "call the same resolver." Keeping
them separate keeps each one legible on its own terms.

### 4. `CharacteristicModifierCandidate` is retired as a live mechanism outright, not merely reordered
`ApplyCharacteristicModifierCandidates` is deleted rather than kept as a last-resort caveat for a
candidate the baseline doesn't resolve. **Alternative considered**: keep it as a fallback, closer to
today's behavior. Rejected — this is exactly the redundant-mechanism shape this whole change removes;
confirmed via the corpus scan above that every real candidate has matchable ability text reachable
through the ordinary present-ability path already, so there is no real case that would lose coverage.
A present candidate with no baseline entry now produces no flagged value at all — the same outcome an
ordinary unmatched ability already produces — rather than a permanently-caveated one.

### 5. InSv rejoins the candidate Field allowlist now that there's one safe consumer
The exclusion's own documented reason (no safe consumer existed) is the only reason recorded for it;
Decisions 3 and 4 together mean a structurally-derived InSv candidate now resolves through the exact
same present-ability path as a scalar one, with no collision risk left to guard against.

## Risks / Trade-offs

- **[Risk]** Deleting two live mechanisms and replacing them is a real behavior change for every
  roster carrying an ability either one used to touch — not purely additive like Stage 1.
  → **Mitigation**: every real scenario either mechanism covered today has a corresponding scenario in
  this change's own spec deltas; Stage 1 exists specifically to make the baseline's coverage a
  superset, not a subset, before this ships. Real-corpus verification against known InSv-footnote and
  Auric-Mantle-shaped units is a hard task, not optional polish.
- **[Risk]** Excluding two ability-name conventions from the general walk is itself a targeted,
  name-based rule, the same shape `ExcludedAttachmentAbilityNames` already uses successfully — but
  that existing exclusion is backed by its own corpus-scan regression test
  (`LeaderSupportAttachedUnitNameScanTests`) confirming every real occurrence of those three names
  really is the attachment-eligibility shape. This change's own exclusion needs the equivalent
  confirmation, not just the two-instance spot-check already done in Context above.
  → **Mitigation**: add the mirrored corpus-scan test as part of this change's own task list, not
  deferred.
- **[Risk]** Two import pipelines (`BsdataDatasheetMapper`, `BattleScribeRosterMapper`) both need the
  identical simplification to `ResolveInvulnerableSave`.
  → **Mitigation**: the exclusion itself lives centrally in `Datasheet`'s own constructor (shared by
  both pipelines already); only the "stop calling the classifier" simplification needs to land in both
  mappers' own code, and both mappers' existing tests already assert pipeline parity — keep that
  assertion, don't relax it.

## Migration Plan

Single-PR, internal-only breaking change (no external contract, per proposal.md's Impact):
1. Add InSv to `BsdataDatasheetMapper`'s `CharacteristicFieldIds` allowlist.
2. Add the two InSv-caveat-internal ability-name conventions to `Datasheet`'s exclusion mechanism,
   alongside `ExcludedAttachmentAbilityNames`; add the mirrored corpus-scan regression test.
3. Simplify both mappers' `ResolveInvulnerableSave` to stop calling
   `InvulnerableSaveCaveatClassifier` — the produced `Caveated(...)` shape is otherwise unchanged.
4. Add the new, small Build-time InSv-resolution step to `AttachedUnitAggregator`; delete
   `ApplyCharacteristicModifierCandidates` and its own call site.
5. Delete `InvulnerableSaveCaveatClassifier` and its own tests; update/replace any test asserting the
   old mechanism split.
6. Real-corpus verification: run a real captured export through `/LivePlay` covering at least one
   InSv-footnote unit and Auric Mantle (or its real equivalent), confirming both now resolve through
   the unified path with the expected values.

**Rollback**: revert the commit — no persisted-data migration; the only checked-in data this change
touches is the baseline JSON, which Stage 1 already grew and this change only reads from.
