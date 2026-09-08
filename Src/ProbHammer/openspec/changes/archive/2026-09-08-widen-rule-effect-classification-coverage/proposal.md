## Why

`classify-rule-effects-from-text` shipped `RuleEffectClassifier` and proved the extraction mechanism
works, but a live corpus-report review already found real, confirmed gaps beyond what it fixed —
recorded in `.claude/vnext-ideas.md` and left queued specifically because they depended on
`baseline-rule-effect-classifications` (now implemented) to land safely without swamping manual
review. Three are narrow pattern gaps that silently drop real Effects the corpus actually contains
(a shorthand "+N Characteristic" phrasing, a possessive "the bearer's X characteristic" phrasing, and
a "Move" vs "Movement" naming gap), and one is a validated-but-unbuilt signal for when a classified
ability states more than its extracted Effects capture. Landing all four now increases real
extraction coverage and closes a known silent-gap class, while the baseline mechanism means each
newly-caught text surfaces once for review rather than reopening the whole corpus.

## What Changes

- Add a fourth `CharacteristicEffect` extraction pattern recognizing shorthand "+N Characteristic"
  phrasing (e.g. "+1 OC"), alongside the existing "Add N to the X characteristic" pattern — fixes
  Marshal's Household/Faith-Fuelled Resolve's actual text, the one confirmed real corpus occurrence
  of this phrasing today.
- Widen the existing `AddCharacteristic` pattern to accept an optional possessive noun between "the"
  and the characteristic name (e.g. "the bearer's Wounds characteristic", not just "the Wounds
  characteristic") — fixes three confirmed real, currently-dropped Wounds-boosting texts (Blasphemous
  Engine, Da Krushin' Armour, Master Artisan's first clause).
- Add "Move" as an additional recognized name for the `M` characteristic alongside the existing
  "Movement" — fixes a confirmed real corpus text using the abbreviated form.
- Add a caveated-classification signal to `RuleClassification`: when at least one Target/Effect match
  succeeded, and text remains (after trimming whitespace and a single trailing period) beyond the end
  of the last successful match, the classification is marked caveated — signaling "this ability
  states more than what was extracted" without attempting to classify what the remainder says. Fixes
  five confirmed real texts (Blastajet Force Field, Leader-beast, Lesk's Heroes, Redoubtable Machine
  Spirit, Scattershield) that today extract a correct Effect while silently dropping other real
  content (a keyword change, a granted ability, a recurring non-characteristic effect) with no signal
  of incompleteness.
- Explicitly excludes the riskier "and add..." mid-sentence continuation widening for `SentenceStart`
  — deliberately parked in `.claude/vnext-ideas.md` since a naive fix would reopen a confirmed false
  positive (Righteous Zeal's still-conditional second clause).
- Add a standing "Caveated baseline entries needing review" section to
  `RuleEffectClassificationReport`: every baselined text whose current `IsCaveated` is true and whose
  baseline entry carries no `note` yet, regardless of Unchanged/Drift/NewInformation status — without
  this, a caveated-and-unchanged entry collapses into the baseline's summary count and never prompts a
  return visit. Closes a real gap found immediately after this change's own `IsCaveated` field shipped:
  the field has no other mechanism prompting a human to actually decide "extend the classifier" vs.
  "accept this as-is" for a newly-caveated entry once it's baselined.
- Add `FullyHandled` to `RuleClassificationBaselineEntry`: a human verdict, distinct from `IsCaveated`
  and never computed, recording that a caveated entry's un-extracted trailing text names no further
  game effect (an eligibility restriction, flavor text) rather than real omitted content. `IsCaveated`
  stays a permanent, purely textual fact either way — a downstream consumer that eventually wires a
  classified Effect into `/LivePlay` needs this second signal to know whether the player should ever be
  prompted to re-read the ability, which `IsCaveated` alone can't express. Confirmed real and worth
  building now, not speculative: Marshal's Household's "Restrictions:" trailing paragraph is one of 12
  identically-shaped occurrences across Space Marines chapter Detachments, the same category of content
  an already-excluded LEADING restriction (`"X model only."`) already isn't caveated for.
- Add `Names` to `RuleClassificationBaselineEntry`: every ability/rule Name currently seen carrying an
  entry's Text, refreshed by `--write-baseline` alongside `Target`/`Effects`/`IsCaveated` (it's a
  corpus-derived fact, not a human judgment call). Purely a findability aid, never part of an entry's
  identity (Text remains the sole key) — without it, locating a specific entry in the checked-in JSON
  meant searching for a snippet of its Text, which is awkward when the text is long or shared verbatim
  by several differently-named abilities.

## Capabilities

### New Capabilities

(none)

### Modified Capabilities

- `rule-effect-classification`: widens the Effect-extraction requirement to recognize two additional
  real phrasings (shorthand "+N Characteristic", possessive "the bearer's X characteristic") and one
  additional characteristic name synonym ("Move"), and adds a new requirement that a classification
  signal when the source text states content beyond what Target/Effects extraction captured.
- `rule-effect-classification-baseline`: adds a requirement that the report surface a baselined,
  caveated entry with no recorded `note` on every run until one is added, independent of whether its
  classification otherwise changed.

## Impact

- `src/ProbHammer.Core/Domain/Catalogue/RuleEffectClassifier.cs` — new/widened regex patterns, new
  caveat-detection logic reading match end positions.
- `src/ProbHammer.Core/Domain/Catalogue/RuleClassification.cs` — new field for the caveated signal.
- `tools/RuleEffectClassificationReport/Program.cs` — report output should surface the new caveated
  signal per the same convention the baseline mechanism already established.
- `src/ProbHammer.Web/Data/RuleEffectClassifications.json` (baseline) — re-run
  `--write-baseline` after landing to backfill the new field on existing verified entries (the
  baseline capability's own "New Classification Fields Backfill" requirement governs this: a default
  `false` backfills silently, a computed `true` surfaces once for review).
- `.claude/vnext-ideas.md` — remove the four now-closed queued items once implemented and verified
  against the live BSData clone.
