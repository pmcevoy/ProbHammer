## Why

`StatlineFlagRule` is a hand-authored, closed vocabulary of exactly 2 rules (Shield Dome, Vexilla)
for deriving a flagged Statline/InSv value from a matched ability — extending it means writing a
new C# subclass per ability. The `rule-effect-classification`/`rule-effect-classification-baseline`/
`characteristic-modification-kind`/`invulnerable-save-effect-resolution` work already built and
proven a general, text-driven equivalent: a checked-in, human-verified baseline of 41 rule/ability
texts (19x the current vocabulary) that already reproduces both hand-authored rules' exact results
in isolation — but nothing consumes it at runtime today. Growing the baseline only means running the
report tool, reviewing new output, and checking in a JSON diff, so retiring `StatlineFlagRule` in
its favor turns a code change into a data review.

## What Changes

- Replace `StatlineFlagRuleCatalogue`'s ability-match step in
  `AttachedUnitAggregator.ApplyStatlineFlagRules` with a lookup against `RuleClassificationBaseline`,
  keyed by each present ability's own normalized Text (not Name+Text — the baseline is deliberately
  Text-only, since many differently-named abilities share identical rules text).
- Load `RuleClassificationBaseline` once at Web startup from the already-shipped
  `ProbHammer.Web/Data/RuleEffectClassifications.json` (mirrors `BsdataCatalogueCache`'s singleton
  pattern) and thread it into `AttachedUnitAggregator.Build`, which today is a pure static method
  with no infrastructure dependency at all.
- Map a matched baseline entry's classified `RuleTarget` onto the existing Bearer/WholeUnit scope
  concept (`SelfRuleTarget` → Bearer, `AttachedUnitRuleTarget` → WholeUnit); an entry whose Target is
  `KeywordRuleTarget`/`UnconditionalRuleTarget` (2 known real baseline entries today) produces no
  flagged value — the same "no rule matched" outcome as today, not a crash.
- Resolve each matched entry's `Effects` via the existing, already-proven
  `CharacteristicModificationResolver` (scalars) and `InvulnerableSaveEffectResolver` (InSv)
  resolvers, adding the one missing piece of glue: wrapping a resolved `CharacteristicValue` back
  into a `ScalarCharacteristicView.Resolved(originalValue, resolvedValue, [ability])`, mirroring what
  `InvulnerableSaveEffectResolver` already does for InSv.
- Reuse `ApplyCharacteristicModifierCandidates`'s existing "skip a field that already carries a
  contributing ability" guard for same-field stacking between two baseline-matched entries — no new
  merge/accumulate logic, since no real corpus example needs it today (never more than one Effect
  applies to the same characteristic of the same unit in the 41-entry baseline).
- **BREAKING (internal only)**: delete `StatlineFlagRule`, `ShieldDomeStatlineFlagRule`,
  `VexillaStatlineFlagRule`, `StatlineFlagRuleCatalogue`, and `StatlineFlagRuleScope` — no external
  contract, but any other code directly referencing these types would break.

Explicitly out of scope for this change (see the exploration that preceded it):
- Extending resolution to `KeywordRuleTarget`/`UnconditionalRuleTarget` entries — no roster-wide
  keyword-predicate evaluation exists yet.
- A per-Effect "known-affected but unresolved" caveat signal (e.g. a default caveated InSv when text
  mentions "invulnerable save" but matches no specific pattern) — a real, evidenced future idea, not
  needed for this swap since every baseline entry with an Effect is already independently verified
  correct for that Effect regardless of other unmodeled content in its text.
- `KeywordEffect`/`AbilityEffect` sibling types — already tracked as deferred in
  `.claude/vnext-ideas.md`.

## Capabilities

### New Capabilities
(none)

### Modified Capabilities
- `statline-flag-rules`: the vocabulary source changes from a fixed set of hand-authored classes
  matching an ability's exact Name and Text, to the checked-in, human-verified
  `RuleClassificationBaseline`, matched by normalized Text alone (independent of Name); scope
  (Bearer vs. whole-unit) is derived from the matched entry's classified `RuleTarget` instead of a
  hand-set value per rule; an ability matching a baseline entry whose Target this capability cannot
  yet apply produces no flagged value, the same outcome as an ability matching no entry at all.

## Impact

- `src/ProbHammer.Core/Domain/Roster/StatlineFlagRule.cs` — deleted.
- `src/ProbHammer.Core/Domain/Roster/AttachedUnitAggregator.cs` — `ApplyStatlineFlagRules` rewritten
  to consult `RuleClassificationBaseline` instead of `StatlineFlagRuleCatalogue`; `Build` gains a new
  baseline dependency.
- `src/ProbHammer.Web/Program.cs` — registers a `RuleClassificationBaseline` singleton, loaded once
  from `Data/RuleEffectClassifications.json` (already shipped in the Docker image).
- `src/ProbHammer.Web/Pages/LivePlay.cshtml.cs` — the sole call site of `AttachedUnitAggregator
  .Build`, threads the new dependency through.
- `tests/ProbHammer.Tests/Domain/Roster/StatlineFlagRuleTests.cs` — deleted, replaced by a
  baseline-driven equivalent built against a small fixture `RuleClassificationBaseline`, not the
  real 41-entry corpus file.
- No `/LivePlay` rendering change — `_UnitBlock.cshtml`'s marker/legend mechanism already reads
  `ContributingAbilities` generically, independent of what populated it.
- No change to `ApplyCharacteristicModifierCandidates`'s own logic — its skip-guard is structural,
  not type-checked against `StatlineFlagRule`, so it keeps working unmodified regardless of what
  populates a field's `ContributingAbilities` first.
