## Context

See `proposal.md` - Why for motivation. Current state this design has to work with:

- `AttachedUnitAggregator` is a pure `static class` with zero infrastructure dependencies —
  `ApplyStatlineFlagRules` currently reads only `StatlineFlagRuleCatalogue.All` (an in-memory
  constant) and the already-built `abilities`/`statlines` lists passed into it.
- `RuleClassificationBaseline`/`RuleClassificationBaselineEntry` (`Domain.Catalogue`) and the
  checked-in `src/ProbHammer.Web/Data/RuleEffectClassifications.json` (41 human-verified entries,
  already shipped in the Docker image) exist today but have exactly one consumer: the standalone
  offline `RuleEffectClassificationReport` console tool. Nothing in `ProbHammer.Web` loads it.
- `CharacteristicModificationResolver` (scalars) and `InvulnerableSaveEffectResolver` (InSv) are
  both built and proven in isolation (unit-tested to reproduce `VexillaStatlineFlagRule`/
  `ShieldDomeStatlineFlagRule`'s exact results) but have zero real call sites — this change is their
  first.
- `AttachedUnitAggregator.ApplyCharacteristicModifierCandidates` already runs immediately after
  `ApplyStatlineFlagRules` and depends on it for one thing only: a "skip if the field already carries
  a contributing ability" guard, needed because a real corpus overlap (Adeptus Custodes' Vexilla)
  is reachable through both mechanisms on the same field.

## Goals / Non-Goals

**Goals:**
- Replace `StatlineFlagRuleCatalogue`'s 2-rule closed vocabulary with the 41-entry (and growing)
  `RuleClassificationBaseline` as the sole runtime source of flagged Statline/InSv values, with no
  behavioral regression for the two abilities the old rules covered.
- Keep everything downstream of `AttachedUnitAggregator.Build` — `ApplyCharacteristicModifierCandidates`,
  `_UnitBlock.cshtml`'s marker/legend rendering — working unmodified, since both already key off
  `ContributingAbilities` structurally rather than off `StatlineFlagRule` as a type.
- Fail closed for target scopes this capability doesn't yet resolve (a baseline entry classified with
  a keyword or unconditional roster-wide target) — same observable outcome as no match at all, never
  a thrown exception or a wrong value.

**Non-Goals:**
- Evaluating a keyword-scoped or unconditional roster-wide target against an actual resolved roster.
- Any per-Effect "known-affected but unresolved" caveat signal (e.g. a default caveated InSv for
  unmatched "invulnerable save" text) — real, evidenced, deliberately deferred (see the exploration
  that preceded this change).
- `KeywordEffect`/`AbilityEffect` sibling types — already tracked in `.claude/vnext-ideas.md`.
- Changing how the baseline file itself is produced or reviewed — the offline report-tool workflow
  is untouched.
- Accumulating more than one contributing ability onto a single flagged field — see Decision 6.

## Decisions

### 1. The baseline is the runtime trust boundary — never call `RuleEffectClassifier.Classify` live
`AttachedUnitAggregator` looks up `RuleClassificationBaseline`, never `RuleEffectClassifier.Classify`
on an ability's raw text at request time. `StatlineFlagRule.Matches` today is exact-text, closed-
vocabulary — it can never fire on text a human hasn't read. Calling `Classify` live would fire on
*anything* matching a regex shape, verified or not, silently widening that guarantee into an open
one. **Alternative considered**: classify live and skip the baseline entirely. Rejected — conflicts
with this project's established precision-over-recall stance on text classifiers (see the review
this change's proposal grew out of); the whole reason the baseline/report-tool review loop exists is
to keep a human in the loop before any classification result is trusted.

### 2. Lookup key is normalized Text alone, never (Name, Text)
Mirrors how the baseline is already grouped (multiple differently-named abilities routinely share
identical rules text — e.g. 12 real invulnerable-save wargear items all granting the byte-identical
"The bearer has a 4+ invulnerable save."). `ContributingAbilities` still records the real, resolved
`Ability` instance carrying its own correct Name, so per-unit display attribution (which ability's
name shows in the legend) is unaffected by the Text-only key — only the *lookup*, not the *rendering*,
is Name-independent.

### 3. Load the baseline once as a singleton, mirroring `BsdataCatalogueCache`
`Program.cs` registers a `RuleClassificationBaseline` singleton, loaded once from
`Data/RuleEffectClassifications.json` (path resolved against `IWebHostEnvironment.ContentRootPath`,
the same convention `LocalDiskBsdataCatalogueSource`'s root already uses), threaded into
`AttachedUnitAggregator.Build` as a new parameter. **Alternatives considered**: (a) re-load/re-parse
the JSON on every call — rejected, wasteful on every render and every casualty-sync POST, with no
precedent elsewhere in this codebase for reloading static reference data per request; (b) make
`AttachedUnitAggregator` a non-static, injected service — rejected as broader than needed; every
other piece of `Domain.Roster` stays static/pure, and one new parameter is the minimal change that
achieves the goal.

### 4. `RuleTarget` → Bearer/WholeUnit mapping; keyword/unconditional targets are filtered before matching
`SelfRuleTarget` maps to the existing Bearer scope, `AttachedUnitRuleTarget` maps to the existing
WholeUnit scope — `IsBearer`'s own logic is reused unchanged, just fed from the mapped scope instead
of a hand-set `StatlineFlagRuleScope`. A baseline entry classified with a keyword-scoped or
unconditional roster-wide target is excluded before matching, producing no flagged value.
**Alternative considered**: throw on an unsupported target. Rejected — a real roster carrying an
ability that resolves to one of the two known keyword-scoped baseline entries (Army: Shivversplint,
Faith-Fuelled Resolve) is an expected, already-catalogued case, not a bug; throwing would break
`/LivePlay` for that roster instead of just omitting a flagged value it was never going to compute
anyway.

### 5. Trust every matched baseline Effect regardless of `IsCaveated`/`FullyHandled`
A baseline entry's Effects are applied whenever its target resolves to Bearer/WholeUnit, with no
additional gate on `IsCaveated` or `FullyHandled`. Verified by hand during this change's exploration:
every one of the baseline's 13 caveated, Effect-bearing entries has leftover text that is
categorically non-characteristic content (a keyword grant, an ability grant, a condition, an
eligibility restriction) — the single captured Effect on each is independently confirmed correct by
its own reviewer note regardless of what else the text states. **Alternative considered**: gate on
`IsCaveated == false`, or on `FullyHandled == true`. Rejected — either would silently withhold
already-verified-correct entries (e.g. Scattershield, Living Carapace — both `fullyHandled: false`
but with an independently-correct extracted Effect) for no real safety benefit, since a caveat here
has never meant "the captured Effect itself might be wrong."

### 6. Reuse the existing skip-if-already-touched guard for same-field stacking; no accumulate logic
If two baseline-matched entries would both touch the same characteristic of the same statline entry,
the first applied wins and the second is skipped — the exact shape
`ApplyCharacteristicModifierCandidates` already uses for its own overlap with this mechanism, now
generalized to also cover overlaps between two baseline matches. **Alternative considered**: extend
`ContributingAbilities` to accumulate multiple sources. Rejected for this change — no real corpus
example needs it (checked: zero baseline entries carry 2+ Effects, and no baseline-vs-baseline
same-field overlap exists in the reviewed 41 entries); this exact gap is already tracked as a real,
confirmed, deferred limitation of the sibling `CharacteristicModifierCandidate` mechanism in
`.claude/vnext-ideas.md` ("A caveated characteristic can only ever attribute ONE contributing
ability") — building accumulate logic here without also fixing it there would be inconsistent, and
neither has a real example driving it yet.

### 7. New glue: wrap a resolved `CharacteristicValue` back into a `ScalarCharacteristicView`
`CharacteristicModificationResolver.Resolve` takes and returns a raw `CharacteristicValue`, not a
view. A new, small, private step (living alongside the rewritten `ApplyStatlineFlagRules`, not a new
public type — no second caller exists today) reads the field's current effective `Value`, resolves
the delta, and wraps the result via
`ScalarCharacteristicView.Resolved(current.OriginalValue, resolvedValue, [ability])` — preserving the
true pre-mutation `OriginalValue` through a chain of mutations exactly the way
`VexillaStatlineFlagRule.Apply` already does today, required for behavioral parity with the rule
being replaced. The InSv case needs no equivalent glue — `InvulnerableSaveEffectResolver.Resolve`
already returns a view directly.

## Risks / Trade-offs

- **[Risk]** Going from 2 hand-authored rules to 39 Self/AttachedUnit-targeted baseline entries is a
  ~19x jump in what can now visibly mutate a live roster's display — a wrong Effect in the baseline
  is now user-visible on any matching real unit, not just Shield Dome/Vexilla.
  → **Mitigation**: every entry was already human-reviewed before entering the baseline (the existing
  report-tool workflow is unchanged by this proposal), and `RuleClassificationDiff` already
  re-surfaces any future drift on a corpus re-run — this change doesn't lower the existing review bar,
  it just makes what's already been verified actually render.
- **[Risk]** `AttachedUnitAggregator.Build` gaining a required new parameter is a breaking signature
  change for its one real call site and any test fixture constructing a roster view directly.
  → **Mitigation**: small, mechanical, compiler-enforced fixup — no runtime ambiguity possible.
- **[Risk]** A future baseline entry with a keyword/unconditional target and a real Effect silently
  produces no flagged value, with nothing telling the player "this ability probably affects something
  the app can't show yet."
  → **Mitigation**: identical to today's existing "ability matches no rule" outcome — not a
  regression this change introduces, and the two known real instances are already documented,
  confirmed gaps rather than surprises.
- **[Risk]** The baseline is data, not code — a malformed hand-edit to the checked-in JSON could load
  silently wrong at Web startup with no compiler check.
  → **Mitigation**: `RuleClassificationBaseline.Load` already treats a missing file as empty rather
  than throwing; the file is reviewed like any other checked-in change; `RuleTarget`/
  `CharacteristicEffect`'s existing `[JsonPolymorphic]` discriminators already fail loudly on a
  malformed "kind" value rather than silently misreading it.

## Migration Plan

Single-PR swap — an internal domain-mechanism change with no external API or persisted-data
migration surface:
1. Add the `RuleClassificationBaseline` singleton registration and the scalar-resolution glue
   (Decision 7), without yet removing `StatlineFlagRuleCatalogue`.
2. Rewrite `ApplyStatlineFlagRules` to consult the baseline; verify it reproduces the existing
   `StatlineFlagRuleTests` behavior for Shield Dome and Vexilla exactly before touching the old types.
3. Delete `StatlineFlagRule.cs` and `StatlineFlagRuleTests.cs` once the new path is proven equivalent;
   replace the tests with a baseline-driven equivalent built against a small fixture
   `RuleClassificationBaseline`, not the real 41-entry corpus file (mirrors the existing BSData
   fixture-testing convention).
4. Run a real captured export through `/LivePlay` (per this project's established "verify a mapper
   change against a real export" practice) to confirm Shield Dome/Vexilla-bearing units render
   identically, and spot-check any other newly-enabled baseline entries reachable in that export.

**Rollback**: revert the commit — no data migration, no schema change, nothing to unwind.

## Open Questions

- Should a missing or malformed baseline file at Web startup log a warning, or fail silently the same
  way `RuleClassificationBaseline.Load` already does for the offline report tool? Doesn't change the
  spec, the approach, or the task breakdown — safe to decide during implementation.
- Exact C# shape for threading the baseline + scope-mapping + resolution glue into
  `AttachedUnitAggregator.Build` (a raw `RuleClassificationBaseline` parameter vs. a small wrapper
  type bundling it with the resolution helpers) — an implementation-level choice, not a behavioral
  one; left for `tasks.md`/implementation time.
