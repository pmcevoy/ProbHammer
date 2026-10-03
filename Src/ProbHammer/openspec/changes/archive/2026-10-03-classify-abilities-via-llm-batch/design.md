## Context

See proposal.md for motivation. Current state, for reference:

- `RuleEffectClassifier.Classify` (regex-based, `src/ProbHammer.Core/Domain/Catalogue/`) is called
  only from `tools/RuleEffectClassificationReport/` and tests - never from live runtime. Runtime
  (`AttachedUnitAggregator`, `DetachmentRuleInboundAbilityResolver`) only ever does a
  `RuleClassificationBaseline.TryGet(normalizedText, out entry)` dictionary lookup against the
  checked-in `src/ProbHammer.Web/Data/RuleEffectClassifications.json`.
- `Domain.Catalogue.Bsdata` (`BsdataClosureResolver`, `BsdataDatasheetMapper`, `RuleGlossary`, etc.)
  already solves the hard corpus-walking problem: `catalogueLinks` resolution, `infoLink`/`infoGroup`
  handling, force-entry names, game-mode gating. `tools/RuleEffectClassificationReport/Program.cs`
  already demonstrates the correct walk (local + shared rules, always-enumerated Datasheet
  abilities, on-demand Enhancement abilities, Detachment rule text).
- Real corpus scan this session: 46 bundled BSData files, ~7,516 raw ability/rule Name+Text
  occurrences, ~3,830 distinct texts after normalization (matches the existing
  `classify-rule-effects-from-text` corpus run's own figure - a useful cross-check for Stage 1's
  own distinct-hash count once built).
- A concrete, real failure this session: an ability ("Dark Pact") was cited from an old memory-file
  paraphrase as an example of "two independently phase-gated stat modifications" - the real corpus
  text (`Dark Pacts`, Chaos Space Marines) is an unrelated Leadership-test risk/reward mechanic
  granting a choice of `[LETHAL HITS]`/`[SUSTAINED HITS 1]`. The wrong claim had gone unchallenged
  for weeks because there was no easy way to check it directly against source text.

## Goals / Non-Goals

**Goals:**
- Build a standalone, file-based, two-stage pipeline (extract, then classify) that can be iterated
  on independently of `ProbHammer.Core`/`ProbHammer.Web`.
- Produce a single, human-readable, hash-keyed extraction file covering the entire corpus - valuable
  as a standalone artifact for restoring/maintaining a correct mental model of corpus content, not
  just as input to classification.
- Make ongoing classification cost near-zero via an incremental re-classification design: reclassify
  a hash when it's new, when the prompt/schema version changes, or when the classification file's
  own stored text has drifted from the corpus's current text for that hash (a hand-editing
  consistency check, not a "text evolved under a stable hash" case - a hash is a content digest of
  its own text, so that specific case can't happen from ordinary pipeline operation; see
  `IncrementalSelector`'s own doc comment).
- Capture the real corpus-confirmed gaps (Feel No Pain grant, weapon keyword grant, named-ability
  grant) plus richer Condition/Phase/TurnOwnership triage, as classification *data* - without
  building the roster-resolution-time engine that would act on it.
- Preserve full reproducibility: every classification record traceable to an exact prompt version,
  model, and timestamp.

**Non-Goals:**
- No change to any live/runtime consumption path, and no change to `ProbHammer.Core`/`ProbHammer.Web`
  at all in this change - wiring the new pipeline's output into LivePlay is a separate, later change.
- No database - files only, chosen deliberately for hand-editability and grep/vim searchability;
  revisit only if files genuinely stop scaling.
- No change to `tools/RuleEffectClassificationReport/` or `RuleEffectClassifier` - they keep running,
  unmodified, as the current source of the shipped baseline until this pipeline is proven.
- Does not build the Condition-evaluation *engine* (the roster-resolution-time code that would act on
  an `evaluable-now` condition) - only the classification/extraction of Condition triage.
- Does not attempt the per-attack conditional grant family, the wargear-option profile-swap family,
  or attachment-eligibility changes (see proposal history for why these are permanently out of scope).

## Decisions

**Principles carried forward from the existing `RuleClassification`/`CharacteristicEffect` schema -
explicit here so a fresh session building this doesn't need to rediscover them:**
1. Discriminated union per distinct shape (a `kind` JSON discriminator), never one record with unused
   fields - applies to the new Effect kinds below the same way `RuleTarget`/`CharacteristicEffect`/
   `WeaponSelector` already work.
2. Plain string, not enum, for anything sourced from BSData's own vocabulary (weapon keywords,
   named-ability names) - proven the hard way by `retire-weapon-keyword-flags`. `UsageLimit` is the
   deliberate exception (see below) because it's a rulebook-level concept, not BSData content.
3. **Verb/Amount (and by extension any new Effect's own magnitude fields) stay unresolved and
   unsigned in the classification itself** - sign resolution and clamping are a separate concern
   (`CharacteristicModificationKind`/`CharacteristicModificationResolver`), deliberately kept out of
   what a classifier states. The new schema describes what the text *says*, never a computed final
   value.
4. **Target and Effect stay independent axes** (who it applies to vs. what it does) - don't let a
   schema iteration collapse them back into one field for convenience.
5. **Fail-closed on ambiguity** - an unrecognized token/qualifier/name is left unclassified, never
   guessed. For names taken from BSData's own vocabulary (weapon keywords, granted ability names,
   `NamedWeapon` names, target keywords) this is enforced deterministically at `collect`, by
   resolving each name against the corpus - not by a closed list in the prompt (revised
   2026-09-30, see "Sample review").
6. Never silently claim full understanding of partially-understood text - the entire
   `ResidualConditionBucket`/`ConditionText`/`UsageLimit` design above is this principle's evolution,
   not a replacement of it.
7. Never trust silently: every record carries a review status and a coverage status; `complete`
   records are used without review (revised 2026-09-28 - reviewing ~3,800 records isn't feasible),
   and anything not captured is surfaced as residue. Drift on re-runs stays detectable via the
   per-record review-status field and the acceptance-gate open question below.

**Two standalone tools, not one, mirroring the two genuinely different problems.** Extraction
(walking a large, structurally complex but *stable* corpus correctly) and classification (an
evolving, LLM-driven schema under active iteration) have very different rates of change and very
different failure modes - bundling them would make iterating on the classification schema
accidentally risk the extraction logic, and vice versa.

**Stage 1 (extractor) references `ProbHammer.Core`'s `Domain.Catalogue.Bsdata` directly** - this is
the one place reuse matters more than decoupling. Re-deriving `catalogueLinks` resolution,
`infoLink`/`infoGroup` handling, and game-mode gating from scratch would duplicate genuinely hard,
already-debugged logic and reintroduce correctness risk for no benefit. Extraction's own *output
schema* is deliberately simple and stable (`{hash, names, text, sourceKind, firstSeenAt,
lastSeenAt}`), so this reference doesn't couple Stage 1 to anything that's actually still moving.

**Stage 2 (classifier) does NOT reference `ProbHammer.Core`** - the opposite tradeoff, deliberately.
Its own POCOs for the classification schema are free to change shape between every prompt-version
iteration without touching or recompiling the main solution. Only once the schema stabilizes does a
later change map it onto real `ProbHammer.Core` types (`CharacteristicEffect` and siblings).

**File layout:**
```
tools/AbilityPipeline/
  Extractor/                    - Stage 1 console app (references ProbHammer.Core)
  Classifier/                   - Stage 2 console app (standalone POCOs, Anthropic SDK)
  prompts/
    v1/
      system-prompt.md          - instructions/wording for this version
      schema.json               - structured-output schema for this version
  fewshot/
    examples.json               - {hash, text, expectedClassification}[], shared across versions
  data/
    ability-corpus.json         - Stage 1 output
    classifications.json        - Stage 2 output (hash -> classification + metadata)
```
Prompt wording and schema are kept as **separate files**, not merged into one - no reason to mix
prose and structured-schema formats in a single file. Few-shot examples live in **one shared file**,
not duplicated per prompt version, since they're ground-truth data independent of prompt wording;
a version's `system-prompt.md` references "the current `fewshot/examples.json`" rather than freezing
a copy - full historical reproducibility is available via git history if ever needed.

**Hash key = normalized Text only, not `(Name, Text)`** - matches `RuleEffectClassifier.Normalize`'s
existing dedup convention and the report tool's own existing grouping behavior, since BSData reuses
identical rule text under many different wargear/ability names. Every distinct `Name` a hash is known
under is still recorded as data on the extraction record, just not part of the key.

**Extraction tracks `firstSeenAt`/`lastSeenAt` per hash**, not just presence - so a BSData update that
*removes* an ability (not just adds/changes one) is visible on the next extraction run, rather than
silently dropping out with no signal.

**Source-kind tagging preserved from the existing report tool's own taxonomy** (Datasheet ability /
Enhancement / Detachment rule / Core rule / Army rule / raw shared rule) - free to carry forward
since the extraction walk already knows this, and directly useful context for Stage 2's prompt.

**Incremental re-classification**: a hash gets (re-)classified when it's new to `ability-corpus.json`,
when its `promptVersion` in `classifications.json` doesn't match the current prompt version being
run, or when `classifications.json`'s own stored text for that hash no longer matches
`ability-corpus.json`'s current text for it - whichever fires, not just the first. That third case
is a hand-editing consistency check between the two independently-editable files, not a "the
underlying text changed" case: a hash is a content digest of its own text
(`ability-corpus-extraction`'s "Content-hash keying" requirement), so the same hash recurring with
genuinely different text can't happen from ordinary pipeline operation - a real text change always
produces a different hash, already covered by "it's new." See `IncrementalSelector`'s own doc
comment for the full reasoning.

**Condition/Phase/TurnOwnership - CONFIRMED, Path B.** `RuleClassification`'s per-effect shape for
this pipeline becomes `ClassifiedEffect(Effect, ResidualConditionBucket, ConditionText?)`, plus
top-level `Phase`/`TurnOwnership` fields, per the schema already worked out in
`project_ability_classification_prose_schema` (paused 2026-09-07, resumed here):
`ResidualConditionBucket` is `none` / `evaluable-now` / `never`, with Phase/TurnOwnership pulled out
as their own fields rather than folded into it, since the phase/turn tracker already makes them
auto-evaluable.

**`UsageLimit` - a fourth top-level field, alongside `Phase`/`TurnOwnership`.** Real corpus evidence
(not speculative): every weapon-characteristic Effect baselined so far (Finest Hour, Instrument of
the Emperor's Wrath, Possessed Lord, Euphoric Strikes, the Zealot/Moment-of-Glory family) opens with
"Once per battle, at the start of the Fight phase, this model can use this ability. If it does,
until the end of the phase, ..." - a usage-limited, player-activated trigger, not a roster-derivable
or genuinely-unknowable one. This is a fourth condition *shape*, distinct from all three
`ResidualConditionBucket` values: not `evaluable-now` (roster state can't tell you *when* a player
chooses to activate it), not `never` in the forever-caveated sense (the player knows exactly when
they're using it - directly player-assertable, same as the existing `IsBattleShocked`/
`IsHalfStrengthOverride` toggle convention). Deliberately top-level, not per-`ClassifiedEffect` -
a usage limit gates the ability's own activation as a whole (same placement reasoning as Phase/
TurnOwnership), and it must compose independently of `ResidualConditionBucket` (an ability could be
both once-per-battle AND separately carry a roster-derivable sub-condition, as two orthogonal
facts). Closed, small vocabulary (`"Once per battle"` / `"Once per battle round"` / `"Twice per
battle"` / `"Once per turn"` / `"Once per phase"` / `null` - the two middle values added
2026-09-30) - a rulebook-level mechanical concept, not BSData content that grows with new codexes, so
this is closer to `EffectVerb`'s closed-enum convention than to the open-vocabulary lesson governing
weapon keywords and named-ability grants.

Extracting `UsageLimit` now does not commit this change to building the toggle UI it anticipates -
that's still deferred to whichever later change wires this pipeline's output into LivePlay, same
"extract now, apply later" principle governing the rest of Path B.

**Generalization (still Consume-stage, not this change's scope, but worth recording now since it
reshapes what the fields above are *for*): the eventual player-toggle idea should cover ANY
conditional effect, not just usage-limited ones** - `evaluable-now`, `never`, and any `UsageLimit`
or Phase/TurnOwnership restriction all get the same uniform, player-assertable toggle, labelled from
`ConditionText`. This simplifies the earlier, narrower "which condition shapes deserve a checkbox"
framing from the prior exploration session (which reserved a checkbox for the `never` bucket only,
specifically to avoid a manual toggle becoming a permanent substitute for building real
`evaluable-now` automation) into: every condition is manually assertable today; `evaluable-now` is
additionally a marked candidate for a later engine to compute automatically instead, while `never`/
`UsageLimit`/phase-restricted conditions will always rely on player assertion, since no engine could
ever derive "did the player choose to activate this." Recorded here as a conscious trade-off, not
silently dropped: shipping one uniform mechanism now is simpler and ships sooner, at the cost of no
longer having a forcing function that pushes toward eventually automating the `evaluable-now` cases.
The one concrete effect on THIS change's schema: `ConditionText` must be populated whenever any
condition exists at all (any non-`none` bucket, any `UsageLimit`, any phase/turn restriction) so a
uniform toggle always has a label to show - not reserved for the `never` bucket alone.

**New Effect kinds** (data shape only - not yet added to `ProbHammer.Core`, this lives in the
classifier's own standalone schema for now):
```
FeelNoPainCharacteristicEffect(int Value, string? Qualifier)
    // Qualifier: null (unqualified), "Mortal Wounds", "Psychic Attacks" - plain string from a
    // known-but-growable vocabulary, not an enum (same lesson as #2 below)

WeaponKeywordGrantEffect(WeaponSelector Selector, string Keyword)
    // Keyword: verbatim string - directly required by retire-weapon-keyword-flags's own lesson
    // (a closed enum silently drops a renamed/new BSData keyword); canonicalized to BSData's own
    // spelling at collect, verbatim kept alongside (2026-09-30)

NamedAbilityGrantEffect(string AbilityName)
    // AbilityName: verbatim string, resolved against RuleGlossary/BSData ability names at collect
    // (2026-09-30 - replaced the original prompt-side allowlist)
```

**Old tool left alone.** `RuleEffectClassifier`/`tools/RuleEffectClassificationReport/` keep running
exactly as today. This new pipeline earns the right to replace them only once it clears an
acceptance gate (see Open Questions) - until then they coexist, and the shipped baseline is
unaffected by anything in this change.

## Risks / Trade-offs

- **[Risk] Batch size ceiling** - a batch is capped at 100,000 requests or 256 MB, and every request
  repeats the ~53 KB prompt prefix (caching cuts its cost, not its size). The full v2 run was 3,773
  requests, ~210 MB. → Mitigation: none in code yet; a larger corpus or prompt needs `submit` to
  split across batches.
- **[Risk] Review bottleneck at initial rollout** - ~3,830 first-run candidates is a much larger
  review surface than today's ~20 manually-vetted results. → Mitigation: not fully solved here (see
  Open Questions), but the per-record review-status field (pending/approved/rejected/note) at least
  makes triage a structured file-editing task rather than an ad hoc one.
- **[Risk] LLM output isn't provably correct.** → Mitigation: unchanged from before - nothing enters
  a "trusted" state without human review; this pipeline doesn't relax that discipline, it just moves
  where the review happens (a file field, not a console-tool queue).
- **[Risk] Schema drift between the classifier's own POCOs and `ProbHammer.Core`'s eventual types.**
  → Mitigation: deliberately deferred, not solved now - the whole point of decoupling Stage 2 from
  `ProbHammer.Core` is to allow this drift *during* iteration; a later change is responsible for
  reconciling them once the schema is stable enough to commit to.
- **[Trade-off] Files over a DB** - explicit, deliberate choice for hand-editability and
  vim-searchability; accepted cost is no relational querying, no transactional updates, and any
  cross-record consistency (e.g. one hash appearing in `classifications.json` but not
  `ability-corpus.json`) has to be checked by tooling rather than enforced structurally.

## Open Questions

- **Review workflow at scale.** **Direction set 2026-09-28 (user):** reviewing all ~3,800 records
  isn't feasible, so `coverageStatus` is the signal - `complete` records are intended to be used
  without review, backed by safe defaults. The prompt now tells the model this, so "`complete`"
  means "safe to use unreviewed". What happens to `partial`/`unclassifiable` records is deliberately
  undecided: many will cover mechanics outside LivePlay's scope and may never need classifying -
  decide after the full run shows how many there are and what their residue says. Also still open:
  whether to spot-check a sample of `complete` records, and the exact consumption rule (a later
  LivePlay-wiring change).
- ~~**Out-of-scope clauses.** Clauses that can never change what LivePlay shows or reminds during a
  battle (e.g. Slaves to None's "when mustering your army..." restriction) currently count against
  `complete`. Candidate rule if that proves noisy: a closed list of categories judged by *effect*,
  not timing (army construction/list legality; unsupported game modes), recorded in a separate
  optional note field so nothing is silently dropped. Decide after the full run, by clustering the
  real `unclassifiedResidue` text - no re-classification needed to do that analysis.~~ **RESOLVED
  2026-10-03** - broader than the candidate rule: residue splits into in-scope-not-expressible vs.
  outside LivePlay's scope, and only the former blocks `complete`. See "Full-run review, 2026-10-03".
- **Acceptance gate before this pipeline is trusted over the existing regex classifier.** Whether to
  require reproducing all 41 already-verified `RuleClassificationBaseline` entries with an equivalent
  (not necessarily identical-shaped, given the schema is richer now) correct classification, as an
  explicit pass/fail check.
- **When/how the later "Consume" change gets scoped** - deliberately not this change's problem; noted
  here only so it isn't forgotten.
- ~~**Parameterized ("X") template rules resolve to empty Effects, by design - is that acceptable
  for v1?**~~ **RESOLVED 2026-09-26, no Stage 1 change needed.** Investigated in full: of the 10
  corpus families using the literal "This ability always takes the form..." template phrase, only 5
  (Damaged, Deadly Demise, Feel No Pain, Firing Deck, Scouts) actually lack their value in Stage 1
  output - the other 5 (Anti, Cleave, Melta, Rapid Fire, Sustained Hits) state their concrete value
  directly in every granting ability's own bespoke prose (e.g. "...have the [SUSTAINED HITS 1]
  ability"), confirmed across 35+ real corpus occurrences, so they classify correctly already. For
  the remaining 5: no known LivePlay display field consumes a resolved value for any of them today
  (confirmed - `FeelNoPain`/`FNP` doesn't appear anywhere in `src/`), so there is nothing for Stage 1
  to unblock yet; building the `{Name, Value}` extraction split now would serve no consumer. Added
  instead: a prompt guardrail (`system-prompt.md`'s "Fail closed" section) making explicit that a
  required numeric effect field must never be filled with an invented value when the text states
  only a placeholder - this was already the *intended* behavior via the general fail-closed
  principle, but is now explicit given `FeelNoPainEffect.Value` is a non-nullable `int` and a
  hallucinated "Feel No Pain 3+" would be indistinguishable from a genuine one in
  `classifications.json`. Also added a 10th few-shot example (Custodian Wardens' "Living Fortress")
  covering the adjacent case that *does* classify correctly today: a usage-limited grant stating its
  value directly, with an unrestricted "any phase" trigger (`phase: null`, not a specific phase).
  A related-but-separate finding, tracked in `.claude/vnext-ideas.md` instead of here since it's a
  LivePlay/consume-time concern, not a Stage 1/2 one: some datasheets link the bare, valueless
  template rule directly as their own redundant ability-list entry (confirmed real via Custodian
  Wardens' BSData) - suppressing it is a plain corpus-level check, independent of this pipeline
  succeeding or failing, not a classification-schema change. Revisit only if/when a Feel No Pain
  display field is actually proposed (see the vnext idea) - that would be the first real consumer
  and the point at which the `{Name, Value}` Stage 1 split becomes worth building.
- ~~**"Select one of the following" choice-grouped effects have no schema representation at
  all.**~~ **RESOLVED 2026-09-26, implemented - reversing an intermediate decision to defer, made
  and then un-made within the same session.** Confirmed real and non-trivial: 41 distinct corpus
  records (~1.1% of 3,823, a floor not a ceiling) grant a player a choice among effects. A wider
  survey (~30 of the 41, prompted by the user - "do you not think we should try and model the OR'd
  effects?") found the family more heterogeneous than first characterized:
  - Many options individually map onto no existing Effect kind at all regardless of grouping -
    reroll-a-roll-of-1 effects, flat to-hit/to-wound modifiers, healing, model revival, Command
    Point grants, "eligible to shoot/charge after Advancing/Falling Back" - a separate, larger gap
    than the choice-grouping question itself.
  - Some records choose a *bundle* of 2+ effects per branch, not one effect per option.
  - At least one record allows taking *both* of two options under a further condition ("or both, if
    this unit Charged") - not "exactly one of N" at all.
  - At least one record isn't a choice among granted effects at all - a shooting/targeting
    restriction rule that happens to also say "select one of the following."
  - Some records choose a *keyword/parameter* that gates a separately-stated later effect (e.g.
    "Slayer's Oath: CHARACTER, VEHICLE or MONSTER"), not a choice among effects.
  - Confirmed directly from a user recollection ("a Black Templar detachment rule that asks the
    player to pick m from n where m < n and m > 1"): **Guiding Omens** (the Emperor's Champion
    Enhancement) is "select up to **three** of the following **six**" - a bounded combination
    choice, not "exactly one of N" - and of those 6 sub-abilities, only 2 map onto any existing
    Effect kind, each carrying its own independent phase/usage-limit condition nested inside that
    one sub-option, distinct from the ability's own top-level fields.

  This range initially argued for deferring entirely (a scoped `ChoiceEffect` risked guessing the
  wrong shape off 2-3 examples). The user pushed back directly - "I think we should try to tackle
  choice effects given we know they exist for 'Templar Vows' and for 'Guiding Omens'. There's no
  point waiting to see them appear in the Unclassifiable list" - and that reasoning held: three real,
  fully-characterized anchor examples (Dark Pacts, Guiding Omens, and Templar Vows, the latter
  investigated live at the user's request and confirmed to be a fourth "select one of the following"
  shape with three of four branches individually unrepresentable) were already enough to design
  against, without needing the batch run to surface more. **Superseded 2026-09-28 by a flat
  representation** (see "Choice resolved" below): the first implementation - a 7th
  `ClassificationEffect` kind, `ChoiceEffect`, nesting a per-branch effect union - could not survive
  the Batch API's strict structured-output compiler. Now a top-level
  `ChoiceGroups: IReadOnlyList<ChoiceGroup(MinSelect, MaxSelect, IReadOnlyList<string> Options)>`,
  where each option is a label (required even when a branch has zero representable effects - e.g.
  Guiding Omens' "Foreseen Paths of the Unholy: -3\" detection range"), and each branch's own
  effects are ordinary `ClassifiedEffect`s tagged with a nullable `ChoiceBranch(Group, Option)`
  index pair - reusing the existing per-effect condition-triage shape, since Guiding Omens showed a
  single branch can carry its own nested usage-limit/phase condition. Deliberately does *not* attempt: a
  conditional expansion of the count ("or select both, if Charged" stays top-level residue), a
  parameter-selection shape (Slayer's Oath-style), or the false-positive shooting-restriction case -
  all three stay unclassified/residue-only, explicitly called out in `system-prompt.md`'s new
  "Choice-grouped effects" section as things NOT to force into Choice. `schema.json` regenerated;
  all three anchor examples now carry a real structured `ChoiceEffect` in `fewshot/examples.json`
  (11th-13th entries) with `coverageStatus: "partial"` (not `"unclassifiable"` - a real, if partial,
  structure is now captured for all three, rather than nothing).
- ~~**Coverage status per classification, not a numeric confidence score.**~~ **RESOLVED
  2026-09-26, implemented.** Raised directly by the user, worried about a real tension: don't want a
  "half-classified" ability silently presented as complete, but also don't want fail-closed
  strictness so aggressive it reproduces `RuleEffectClassifier`'s own "anaemic" narrowness. A raw
  numeric confidence score (0-100/0.0-1.0) was considered and rejected: LLM self-reported confidence
  numbers are well known to be poorly calibrated, and a single number conflates two different
  questions - "did I capture everything the text states" vs. "am I sure what I DID capture is
  right" - into one uninterpretable digit, with no indication of WHAT was missed for a reviewer to
  act on. Implemented instead: `ClassificationResult.CoverageStatus` (`Complete`/`Partial`/
  `Unclassifiable`, required) plus `UnclassifiedResidue: string?` (populated whenever status isn't
  `Complete` - a short plain-English note of what the text states that isn't represented anywhere in
  Target/Effects). Recovers the role `RuleClassification.IsCaveated` played for the old regex
  classifier (Principle #6: "Never silently claim full understanding of partially-understood
  text"), generalized since the existing per-effect `ResidualConditionBucket`/`ConditionText` can
  only caveat an effect that WAS extracted - it has no way to flag a whole clause that wasn't
  extracted as any Effect at all. `Unclassifiable` records point at real schema gaps worth
  extending; `Partial` records are the highest-value human-review queue; `Complete` records are
  lowest priority. `system-prompt.md` got a new "Coverage status" section (with the OR'd-choice and
  "no Effect kind exists" shapes called out explicitly), `schema.json` was regenerated from the
  updated POCOs, and all 10 existing few-shot examples were backfilled with `coverageStatus`/
  `unclassifiedResidue` - one of them (the Harlequins "move through enemy models" example) turned
  out to be mis-labeled as a negative control under the old implicit assumption and is now correctly
  `unclassifiable` (it states a real movement/terrain-interaction effect this schema still has no
  kind for) - a genuine finding surfaced directly by implementing this field properly rather than
  retrofitting it loosely.

## Session paused 2026-09-26 - resume point for task 4.1

Real end-to-end pipeline testing began today (API key wired up, `Classifier submit` extended with an
optional `[N|hash1,hash2,...]` arg for small test batches instead of the full corpus). This surfaced
a real, still-unresolved blocker specific to `Choice`, plus one confirmed, fixed-in-code bug and one
confirmed, not-yet-fixed bug. Read this section first before touching `BatchRequestBuilder.cs` again.

**Confirmed working:** the base schema (Target/Effect [6 non-Choice kinds]/WeaponSelector), auth,
Batch API submit/poll/collect, and `ClassificationResult` deserialization all work correctly against
the real API - a real test classification (`'Ard As Nails`, an Ork Warboss "don't die" mechanic)
round-tripped cleanly and produced a sensible `coverageStatus: "unclassifiable"` verdict with a good
residue note. This is strong evidence the schema, prompt, and few-shot design are sound *without*
`Choice` in the mix.

**The blocker: `Choice` cannot pass real-world use with `output_config.format`'s strict/structured
enforcement, even after 8 real API round-trips of fixes.** In order, what was found and fixed:
1. `anyOf`-wrapping node also carrying sibling `type`/`required` - fixed (`FixSchemaNode`, still in
   `BatchRequestBuilder.cs`).
2. Each `anyOf` branch missing its own `type` - fixed (same method).
3. `ChoiceEffect` originally reused the full effect union recursively (a Choice branch could contain
   another Choice) - rejected outright regardless of `$defs` placement ("Circular reference
   detected... self-referencing... not supported"). Fixed at the **model** level, not the schema-
   patching level: `ChoiceOption` (see its own doc comment) is now a `Plain`/`Weapon` two-kind union,
   narrowed to `WeaponKeywordGrantEffect` only - no recursion possible. This was also independently
   the right scope call per the user's own steer ("Dark Pacts/Martial Ka'tah adding a keyword tag is
   already a win" - full per-branch richness deferred, not lost forever).
4. Even narrowed, `Choice` alone still tipped the schema past a separate ceiling ("The compiled
   grammar is too large") - confirmed via a real test that the identical schema WITHOUT `Choice`
   succeeds, isolating `Choice` (not prompt length, not the base schema) as the cost driver.
5. Attempted fix: deduplicate `WeaponSelector`'s 3-way union (inlined 3 times - in
   `WeaponCharacteristicEffect`, `WeaponKeywordGrantEffect`, and now `WeaponChoiceOption`) into one
   shared, **non-recursive** `$defs` entry (`DeduplicateUnions` in `BatchRequestBuilder.cs`, still
   present in code). This got the request PAST schema validation for the first time with `Choice`
   enabled - but produced a new, worse symptom instead: on a trivial one-line ability text
   (`'Ardboyz`: "BOYZ unit only. This unit has 4+ Sv.", which should produce at most one `Scalar`
   effect), the model generated 48+ effects and ran out of its 2048-token budget mid-array, producing
   truncated, unparseable JSON. This looks like degenerate/runaway constrained-decoding behavior
   triggered by the `$ref`/`$defs` restructuring, not a fluke of that one input.

**Choice resolved 2026-09-28 - option C, a fourth option not in the list below:** stop nesting
entirely. Choice's grammar cost came from being a *container* (a union inside the effect union), so
it became a *tag* instead: top-level `choiceGroups` holds only `minSelect`/`maxSelect`/option
labels, and a branch's effects are ordinary top-level effects carrying a `choiceBranch` index. No
recursion, no nested union, only a few scalar fields added to the grammar - and it restores full
per-branch richness (any effect kind, not just WeaponKeywordGrant; Guiding Omens' "+2 Attacks"
branch is now a real `WeaponCharacteristic` effect). `ChoiceEffect`/`ChoiceOption` deleted;
`DeduplicateUnions` removed (it existed only to make room for the nested Choice, and was the prime
suspect for the runaway generation). Trade-off: the model must keep the indices consistent - not
schema-enforced, so review should check them. Fallback if strict output still fails: option A below,
keeping the flat shape.

Also found and fixed while doing this: few-shot assistant turns were serialized with explicit
`null`s (`"conditionText": null`, `"phase": null`, ...), but `StructuredOutput` renders a nullable
member as an *optional key*, never a `null` - so the grammar could never produce what every example
showed. `ClassificationJson.Options` now omits nulls when writing (it only writes few-shot turns),
and `system-prompt.md` says a `null` field is expressed by omission. Plausibly a contributor to the
runaway generation too; the next real test run will tell.

**Model and prompt review, 2026-09-28** (all still prompt v1 - v1 has only ever produced disposable
test output, so editing it in place loses nothing):
- **Model: Sonnet 5, not Haiku 4.5.** Same 7 unseen records: Haiku invented usage limits and
  `turnOwnership: mine` on "each time selected to fight" texts, normalized keywords, and under-scoped
  targets; Sonnet fixed all but the targets. `MaxTokens` 2048 -> 8192 (adaptive thinking tokens count
  against it).
- **`phase` -> `phases` (array).** It's the *activation* phase - LivePlay's phase tracker uses it to
  remind the player to invoke the ability. [Redefined 2026-09-30 - see "Sample review".] ~26 corpus records activate in two phases ("selected to
  shoot or fight", "Shooting phase or the Fight phase"), including Dark Pacts.
- **`conditionText` only for gates the structured fields don't capture** (required for
  `evaluable-now`/`never`, null for `none`) - no longer repeats `phases`/`turnOwnership`/
  `usageLimit`, and never records duration. Phase gates stay `none` + `phases`.
- **Target follows the 11e attached-unit rule** (quoted verbatim in the prompt): "the bearer"/"this
  model" is `Self`, anything addressing a unit is `AttachedUnit`, regardless of which component
  carries it. "X unit only" and "If your Army Faction is X" are not targets. The old "`target: Self`
  as the safe default" wording was removed - it was steering both models to `Self`. Dark Pacts'
  few-shot target corrected to `AttachedUnit` (was `Self`, correct-by-accident only because every CSM
  unit carries the rule).
- **Prompt now states its purpose, the source kind is passed in each user turn** (as this design
  originally intended), markup-stripping rules are explicit, effects on enemy units are residue, "each
  time X" is not a usage limit, and `evaluable-now` lists what the app actually tracks.
- **`WeaponKeywordGrant.replacesKeyword`** (optional) for "X instead of Y" upgrades - only 2 corpus
  records state it explicitly (Fidelity's Whirling Stance, Payback Time); a field, not a new kind, to
  avoid another `WeaponSelector` copy in the grammar. Implicit upgrades (granting a higher value of a
  keyword the profile already has, without saying "instead") are invisible to the classifier and
  belong to LivePlay's keyword merging.
- **`NamedAbilityRemoval`** effect kind, no allowlist (a removal only acts on an ability the unit
  already displays). ~25 corpus records remove a named ability, e.g. Slaves to None removing Dark
  Pacts - LivePlay would otherwise show an ability the unit no longer has. No weapon-keyword removal
  exists anywhere in the corpus; unit-keyword loss (Smoke, Markerlight) stays residue.
- **Conditional choice counts fail open** ("select one... or both, if this unit made a Charge
  move"): `maxSelect` is the larger count, and a new optional `ChoiceGroup.conditionText` says when it
  applies. LivePlay treats option selection as a player-set toggle and never enforces the count, so
  this allows every legal game state and loses nothing - such records are now `complete` instead of
  `partial`. Replaces the earlier "model the base choice, conditional count goes to residue" rule.
- **Prompt caching:** a 1h-TTL cache breakpoint on the last few-shot turn makes system prompt + all
  examples (~97% of each request's input) one shared cached prefix. Measured uncached cost before
  this: ~11.4k input / 228 output tokens per record on Haiku 4.5 (~$24 full run at batch pricing),
  ~15.1k / 488 on Sonnet 5 (~$67). `collect` now prints token usage, including cache reads/writes,
  so the next run shows whether hits actually happen (batch cache hits are best-effort).
- Few-shot set grew 13 -> 19 with phase/turn examples (phase-only, phase + untrackable gate,
  `mine` + usage limit, `theirs` + dice roll).

**Separately confirmed, real - since fixed (per-result try/catch in `Program.cs`):** `CollectAsync` (`Program.cs`) lets a single
unparseable response crash the entire process (an unhandled `JsonException`) instead of logging that
one hash as failed and continuing to the next result. Harmless with a 1-record test batch; would be
serious on the real ~3,823-hash run, where one bad response would take down collection of everything
else that succeeded alongside it. Fix this regardless of how the `Choice` question resolves - wrap
the per-result deserialize in try/catch, log + skip on failure.

**Three-way decision still open, not yet made** (user was about to answer when the session was
paused to wrap up for the day):
- **Drop `output_config.format` entirely, keep the schema exactly as designed.** Submit a plain
  prompted request (system prompt already describes the schema in detail) and validate/parse the
  response ourselves. No compiled grammar, so no size ceiling and nothing to trigger degenerate
  array-generation loops. The one real cost: lose the API's guarantee that every response parses -
  mitigated by (a) fixing the `CollectAsync` crash regardless, so a bad response just leaves that
  hash pending for retry, and (b) [superseded 2026-09-28 - `complete` records are now used without
  review] every classification already requires mandatory human review before
  being trusted, so an occasional malformed response is a small, already-absorbed risk, not a new
  category of one. Leaning direction going into the pause, given the degenerate-generation finding.
- **Keep debugging structured output.** Investigate why `$ref`/`$defs` provoked runaway generation,
  try further schema-size mitigations, possibly raise `MaxTokens`. Uncertain payoff - 8 round-trips in
  and still finding new failure classes, not just closing out known ones.
- **Fix only the `CollectAsync` crash for now and leave `Choice`'s fate for next session** - lowest-
  commitment option, defers the real decision without blocking on it.

**Stray test artifacts cleaned up before pausing:** `tools/AbilityPipeline/data/classifications.json`
and `pending-batch.json` were deleted - both only ever held disposable test output from this
debugging session (one stale classification produced while `Choice` was temporarily disabled for a
diagnostic, and one leftover pending-batch record), never real corpus output. Nothing of value was
lost; regenerate as normal once `submit` actually runs against real records.

**Nothing in this change has been committed to git yet** - the entire `tools/AbilityPipeline/` tree,
this change's `openspec/` folder, and the two `.claude/` doc edits from this session are all
uncommitted working-tree changes (confirmed via `git status`, 2026-09-26).

## Sample review, 2026-09-30 (task 4.0)

The 50-record Sonnet 5 sample (batch `msgbatch_018jwawUyXEV4nB8Sv23Dd6p`, seed 20260928) came back
8 `complete` / 4 `partial` / 38 `unclassifiable`, 0 failures. Prompt caching works (cacheRead 769k
vs. 5k uncached input; ~447 output tokens per record). The user reviewed every record. Residues were
honest schema gaps (re-rolls, hit/wound-roll modifiers, effects on enemy units, healing, CP), and no
`complete` record stated a wrong effect - the findings below are about phases, vocabulary, and
names. They ship as **prompt v2**, not an in-place v1 edit: the sample's records are stamped v1, and
the incremental selector only reclassifies on a version change, so v2 is what lets the same 50
hashes be re-run and compared.

**`phases` = every phase in which the player needs to see the ability** - both when it's used and
when its effects apply. Replaces the activation-only definition: the phase tracker is meant to draw
attention to abilities relevant to the current phase (the intended UI is the ability's button
changing colour), and an ability declared in the Command phase that buffs attacks is no use if it's
only highlighted in the Command phase. Worked examples for the prompt:
- Declared in Command, +1 Attacks to melee and ranged weapons -> `["command","shooting","fight"]`.
- An event only possible in one phase implies it: "each time this model ends a Charge move" ->
  `["charge"]`.
- Passive roll modifiers: "add 1 to Advance and Charge rolls" -> `["movement","charge"]`.
- Anything in the attack sequence (an attack targeting this unit, a Hit/Wound roll, saving throw, or
  Damage) -> `["shooting","fight"]`; "ranged attack" narrows to `["shooting"]`, "melee attack" to
  `["fight"]`. No `movement` for Fire Overwatch shots - it would light up every ranged buff in the
  opponent's Movement phase.
- "Start of the battle round" -> `command`, "end of the battle round" -> `fight`, both with
  `turnOwnership` null: the tracker doesn't know who goes first, and over-reminding in both turns
  beats missing it (round awareness parked in `.claude/vnext-ideas.md`).
- "Start of your turn" -> `command`/`mine`; "at the end of your (opponent's) turn" -> `fight`/
  `mine` (`theirs`). "Until the end of your turn" is a duration and sets nothing.
- Triggers that aren't phase-bound ("when this model is destroyed") -> `[]`.

Considered and rejected as over-modelling: separate activation vs. effect phases, per-effect
`activePhases`, and a `trigger { phases, turnOwnership, usageLimit, text }` object (the rules'
own term, and the most faithful shape). Nothing consumes the distinction yet, every extra field is
one more thing an unreviewed `complete` record can get wrong, and hash-keyed re-classification is
cheap if a richer shape is ever needed.

**Core-Stratagem timing table in the prompt.** ~177 of 3,823 texts mention a Stratagem, ~78 by
name. The ability text never states the Stratagem's own timing, so the model can't infer phases.
Timings supplied by the user from the 11e core rules (not in BSData; not taken from memory, which
is edition-stale). Each row lists both names where 11e renamed a Stratagem, since ability texts lag
the rename:

| Stratagem | `phases` | `turnOwnership` |
|---|---|---|
| Fire Overwatch (32 texts) | movement | theirs |
| Heroic Intervention (14) | charge | theirs |
| Rapid Ingress (12) | movement | theirs |
| Grenade / Explosives (7 / 1) | shooting | mine |
| Command Re-roll (4) | `[]` | null |
| Tank Shock / Crushing Impact (2 / 2) | charge | mine |

Generic mentions (~36 CP-cost reductions, ~20 CP gains, ~15 "targeted with a Stratagem") need no
row.

**`usageLimit` gains "Once per battle round" (49 texts) and "Twice per battle" (7)** - both
surfaced as residue.

**Target keywords are an all-of list.** `KeywordTarget { keyword }` -> `KeywordTarget { keywords }`,
a unit needing every listed keyword. "a friendly Leagues of Votann Infantry unit" means LEAGUES OF
VOTANN *and* INFANTRY; the sample recorded it as one fused keyword. ~63 targets are a faction plus a
unit type, against ~429 single keywords (rough pattern count). A slash-separated list ("BULLGRYN
SQUAD/OGRYN SQUAD/RATLINGS") is OR, not AND, and stays residue - it belongs with the unit-keyword-
grant idea in `.claude/vnext-ideas.md`.

**Names from BSData's vocabulary are resolved at `collect`, failing closed.** The prompt keeps
recording names verbatim (Haiku normalizing keywords was one of its failures), and one shared
deterministic step resolves them against the corpus, storing the canonical form alongside the
verbatim one. An unresolvable name demotes the record to `partial` with the name in its residue:
- **Weapon keywords** (`keyword`, `replacesKeyword`): strip `^^`/`**`/brackets, match case-
  insensitively against the corpus's weapon `KeywordsText` tokens, keep the value ("Sustained Hits
  1" - so `RuleGlossary.NormalizeToken`, which drops values, can't be used alone). Sample issues:
  `"[LETHAL HITS]"` vs. BSData's `"Lethal Hits"`, verbatim lowercase `"[sustained hits 1]"`, and a
  valued `"[BLAST 1]"` for a keyword BSData normally writes bare.
- **Granted ability names**: resolved against `RuleGlossary` and BSData ability names. **This
  retires the named-ability allowlist** (`prompts/v1/named-ability-allowlist.json`). The allowlist
  mixed up "did the text grant X" with "does the app understand X", and dropped real grants - Scouts
  alone has 36 (6" x30, 9" x8, 7" x5, 5"/8" x1), more than any allowlisted name. The value stays in
  the name (`"Scouts 9\""`), as weapon keywords do; the consuming app decides which grants it acts on.
- **`NamedWeapon` names**: resolved against the corpus's weapon names, tolerant of case and
  singular/plural (sample: `"heavy bolters"` -> `Heavy bolter`). Checked against the whole corpus,
  not one unit's weapons - classification is deduplicated by text and doesn't know which datasheet
  it came from, so a per-unit match is the consuming app's job.
- **Target keywords**: each must be a real BSData unit keyword, which also catches a fused keyword.

**Parked in `.claude/vnext-ideas.md`, not in scope:** unit keyword grants and removals (ABHUMAN,
Grenades, Smoke, Markerlight) with an any-of target; battle-round awareness in the phase tracker.

**As implemented (2026-09-30):**
- **Vocabulary comes from the extractor.** The classifier still doesn't reference
  `ProbHammer.Core`, so the extractor's corpus walk also writes `data/vocabulary.json`: every
  spelling of each weapon keyword, weapon name, unit keyword, and ability/rule name (incl. glossary
  aliases), with occurrence counts. The classifier's `NameResolver` matches ignoring case, markup and
  punctuation; the canonical form is the most common spelling. Weapon names also try dropping a
  plural `s`/`es`; an ability name with a value not listed verbatim falls back to its base name and
  keeps the value (`Scouts 10"` -> `Scouts` + `10"`).
- **The model's output is never rewritten.** Each record gets a separate `resolution` (resolved
  names with canonical forms, plus unresolved names), and `effectiveCoverageStatus` demotes
  `complete` to `partial` when anything is unresolved. `report` splits by the effective status. A new
  `resolve` command re-applies resolution to existing records, idempotently, after a vocabulary
  refresh.
- **Sample findings from the real vocabulary:** `BLAST 1` is a real BSData keyword (33 spellings as
  `BLAST 1`), and `Wolf Guard Weapon` a real weapon name - both resolve. Every name in the v1 sample
  resolves; the fused Votann keyword can only be caught on v2 output, which records a list.
- **`RuleGlossary.Normalize` doesn't strip an inch value**: `Scouts 9"` normalizes to `scouts9`, not
  `scouts`, so a granted `Scouts 9"` won't resolve to the Scouts popover. That's `ProbHammer.Core`,
  out of this change's scope - the change that consumes these grants in LivePlay needs to extend its
  trailing-value pattern to `\d+"`.
- **Few-shot phases under the new definition:** Feel No Pain / invulnerable-save grants and the
  weapon grants take their attack phases; Feel No Pain against mortal wounds, Objective Control and
  Infiltrators stay `[]`; the Command-phase +2 Damage example is `["command", "shooting", "fight"]`;
  the two first-battle-round choice abilities take `command` plus the union of their options'
  phases.

## v2 sample re-run, 2026-10-01 (task 4.0.6)

Same 50 hashes: 9/4/37 complete/partial/unclassifiable (v1 8/4/38), no unresolved names, effects
unchanged except `Scouts 9"` now resolving. Phases, `usageLimit` and keyword lists behaved as
intended. Decisions from the user's review:
- **Battle-shock tests can happen in any phase in 11e** (the user's correction, not just Command), so
  an ability about them is `phases: []` unless its text names a phase. The v2 prompt claimed
  Command-only and is edited in place; the sample isn't re-run.
- **"The first time the bearer is destroyed"** on a `Self` target as `Once per battle` is accepted -
  the same outcome either way.
- **A selection condition on an ability with no representable effect stays in residue** - the
  condition bucket belongs to an effect, so there's nothing to attach it to.

## Full-run review, 2026-10-03 (tasks 4.2-5.2)

**Run:** 3,773 requests, 0 failed; with the 50 sample records, 3,823 distinct hashes - 752 complete /
455 partial / 2,616 unclassifiable. Tokens: input 413,451, output 1,931,056, cache read 63,677,255,
cache write 67,580 - ~$16.50 at Sonnet 5 batch pricing (estimate was $15-25 cached).

**5.1 spot-check** - 30 seeded-random full-run `complete` records, every one reviewed by the user: 27
correct, 2 wrong, 1 design gap. Neither error is visible today.
- **Wrong - event trigger absorbed into the phase fields** (Scuttling Gait/Turbo-boost: "each time
  this unit Advances... add 6" to the Move characteristic" as bucket `none`). Should be `never`; only
  safe today because `turnOwnership: mine` trips the unconditional rule.
- **Wrong - `AttachedUnit` where the bearer's own models are meant** (Bound Daemon, on the
  Daemonhost [Legends] retinue: "the OC of DAEMONHOST models in that unit is 1"). Should be `Self`;
  `AttachedUnit` would also set the leading Inquisitor's OC.
- **Design gap - `evaluable-now` has no structured form** (Ardent Protectors: "while a CHARACTER
  model is leading this unit"). The bodyguard outlives its Leader, so this can't ride on the target
  the way a Leader's own "while leading" does. Decided: a structured condition beside
  `conditionText` (led-by-keyword, has/lacks keyword, contains model), not a composite target -
  `Target` is who benefits. `KeywordResolution.EffectiveKeywords` is already casualty-aware.
- Minor: `turnOwnership` set where the text doesn't say whose turn; "contains model X" bucketed
  `never` where a sibling text got `evaluable-now` (safe direction).

**FNP qualifier:** of 104 FNP effects, none is a JSON `null` - 66 use a string for "unqualified"
(`"null"` 37, `""`, `none`, `None`, `all`, `__NONE__`), ~8 put a condition or model restriction in
the qualifier. `export` maps the sentinels to `null` (5.5) - the stored records stay verbatim, per
"the model's output is never rewritten". The misplaced ones need the prompt fix. A fixed qualifier
vocabulary is left to the work that surfaces FNP like InSv.

**Consumer bug found:** `InvulnerableSaveEffectResolver` replaces the InSv wholesale, so a
ranged-only grant (Kustom Force Field) wipes an existing melee save -
`adopt-llm-ability-classifications` task 7.2.1.

**5.2 - residue scope, resolving the "Out-of-scope clauses" open question.** "Unclassifiable" means
"no stat effect captured", not "unknown": 73% of unclassifiable records still carry phases, and every
record a target. Residue splits two ways:
1. **In scope, not expressible** - LivePlay or the simulation would use it if the schema could hold
   it. This is the schema backlog, and only this should keep a record from `complete`.
2. **Outside LivePlay's scope** - tactical actions, resource/token economies, CP, movement/reserves/
   deployment, enemy-only effects (forced Battle-shock tests), objective control, list legality.
   Phase/target/timing is the whole useful extraction.

The per-record split is left to the next prompt version (the model sorts its own residue) rather
than clustering ~3,000 residues by hand. Army and Detachment rules are the worst-covered (1/25 and
24/332 complete); a rough Detachment residue tally is led by Hit/Wound roll modifiers (~69) and
re-rolls (~51). Sampled partials were honest and their captured effects safe.

**Next-iteration backlog (schema + prompt v3):**
- **Roll-modifier effect kind** - the largest in-scope family and a core simulation input (Oath of
  Moment fails on this alone). Draft fields: roll (Hit/Wound/Damage/Save/Advance/Charge/
  Battle-shock), modifier (re-roll / re-roll 1s / +N / -N / critical on X+), direction (made by vs.
  targeting this unit), existing weapon selector. LivePlay idea: an info line in the matching weapon
  panel for the current phase/turn; "targeting" lines beside Sv/InSv in the opponent's turn.
- **BS/WS** as weapon characteristics - deferred by the weapon-effects plan, not excluded;
  `RollThreshold` already handles the sign. 35 texts.
- **Per-effect timing** - `phases`/`turnOwnership`/`usageLimit` are record-level, so "has Deep Strike;
  once per battle, redeploy" (Warp-borne Stalker) gates the permanent grant too.
- **Structured `evaluable-now` conditions** (above).
- **"Select N units"** selections - covered by the planned condition toggle.
- **Prompt fixes:** event triggers ("each time this unit Advances/makes a Charge move") are `never`;
  "models with the bearer's own keyword in that unit" is `Self`; an FNP qualifier is only a damage
  source; `turnOwnership` only when the text says whose turn; a multi-word weapon name ("Tyrnak and
  Fenrir") isn't split on "and"; faction + type keywords are split ("LEGIONES DAEMONICA TZEENTCH").

**When:** no re-run now (user decision). The current output stays in use; the next run is timed with
the new Space Marine codex once BSData has absorbed it, since the rewording will stale these
classifications anyway.
