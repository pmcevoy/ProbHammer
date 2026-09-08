## Context

`RuleEffectClassifier` (`src/ProbHammer.Core/Domain/Catalogue/RuleEffectClassifier.cs`) is a
standalone, text-only classifier — no BSData dependency — that extracts a `RuleTarget` and zero or
more `CharacteristicEffect`s from a rule/ability's Name+Text via anchored regex matching. It's
exercised by `tools/RuleEffectClassificationReport` against the live BSData clone, and its results
can now be checked against a checked-in, human-verified baseline
(`src/ProbHammer.Web/Data/RuleEffectClassifications.json`, `RuleClassificationBaseline`/
`RuleClassificationDiff`) that collapses unchanged, already-verified results to a summary count while
always surfacing drift or newly-computed field values. See proposal.md for the four specific
confirmed-real gaps this change closes.

## Goals / Non-Goals

**Goals:**
- Recognize three additional real phrasings the current patterns miss (shorthand "+N Characteristic",
  possessive "the X's Y characteristic", "Move" as a Movement synonym).
- Add a caveated signal so a classification that extracts a correct-but-partial Effect no longer
  looks indistinguishable from one that captured everything the text states.
- Validate the widened patterns and the new signal against the live BSData corpus before considering
  this done — not just against the known confirmed examples.

**Non-Goals:**
- The "and add..." mid-sentence continuation widening (deliberately parked — see proposal.md).
- Classifying *what* the caveated remainder says (a keyword grant vs. an ability grant vs. a
  recurring effect) — the signal states only that something remains, per the validated mechanism.
- Extending the caveat signal to the Target-only or default-only buckets — the hand-validated
  5-caveated/15-clean split only covers the 20 texts with at least one extracted Effect; applying the
  same mechanism to the other ~3,800 texts is unvalidated and out of scope here.

## Decisions

**Shorthand "+N Characteristic" pattern, anchored the same way as the other two Effect patterns.**
Adds a new `[GeneratedRegex]` requiring `SentenceStart`, matching `+N` followed by one of the six
Statline scalar codes (`M`, `T`, `Sv`, `W`, `Ld`, `Oc`) as a whole word, with an optional intervening
"to" (covers both "+1 OC" and a possible "+1 to OC" variant). Case-insensitive, matching this
classifier's existing convention for word-literal patterns (real corpus text capitalizes these
inconsistently — see `AttachedUnitPhrase`'s own precedent). Anchored by `SentenceStart` like the
other two Effect patterns, for the same reason: an unanchored version could match a shorthand
increment embedded in a still-conditional clause.

*Alternative considered*: match any short (1-3 char) alphabetic token after `+N`, then look it up in
a name table. Rejected — a closed 6-code whole-word match is simpler, keeps the same "known
vocabulary, not free-form" property every other pattern in this classifier already has, and needs no
new lookup structure.

*Risk, addressed via the corpus report rather than guessed away*: single-letter codes (`M`, `T`, `W`)
could in principle match a `+N` immediately followed by an unrelated capitalized word starting with
that letter. Only one real occurrence (Marshal's Household) is confirmed today. Task list requires
re-running the corpus report against the live clone after implementing this pattern and inspecting
every newly-produced Effect result (not just the previously-known one) before merging — the same
verification discipline the original `classify-rule-effects-from-text` live-review pass established.
If the corpus scan finds a real false positive, narrow the pattern (e.g. require a following
"characteristic"-adjacent word, or drop the single-letter codes) rather than accept it.

**Possessive widening as a generic optional group, not a word denylist.** `AddCharacteristic`'s regex
gains an optional `(?:[A-Za-z]+'s\s+)?` immediately before the characteristic-name capture group,
matching any possessive noun ("bearer's", "model's", a named character's own possessive) rather than
hardcoding "bearer's" specifically. Matches this codebase's established preference for structural
widenings over phrase denylists (`SentenceStart`, `AttachedUnitPhrase`'s "models in this unit"
widening) — confirmed real examples all use "bearer's", but nothing else about the pattern depends on
that specific word.

**"Move" added as a second `CharacteristicNames` key mapping to `"M"`, not a text-normalization
step.** `CharacteristicNames` is already a plain lookup dictionary; adding a second key for the same
value is the smallest possible change and keeps `Normalize()` scoped to its existing job (harmless
authoring-variance cleanup: apostrophes, NBSP), not characteristic-name synonym resolution.

**Caveat signal computed from raw match end positions, not from the already-built
`RuleClassification`.** `ClassifyTarget`/`ClassifyEffects` currently return only their resulting
values; each gains an overload (or the existing method is restructured) to also expose the `Match`
objects that actually contributed to the result — the fallback `SelfRuleTarget` contributes no match,
and only a `CharacteristicEffect`-producing match counts, mirroring exactly which matches the
validated hand-check used. `Classify` takes the maximum `Match.Index + Match.Length` across every
contributing match, trims the remainder (whitespace, then one trailing period), and sets caveated
`true` when non-empty. This lives entirely inside `RuleEffectClassifier` — no new public surface
beyond the new field on `RuleClassification` itself.

*Alternative considered*: re-scan the text for the caveat check independently (e.g. re-run every
pattern again and take the max end position without threading `Match` objects through). Rejected —
would silently drift from "the matches that actually decided this classification" if a future pattern
change altered match behavor without a parallel update to the caveat scan; threading the actual
contributing matches through ties the two together structurally.

**`RuleClassification.IsCaveated` defaults to `false`.** Every existing baseline entry that doesn't
carry a caveat determination backfills through the baseline capability's own "New Classification
Fields Backfill" requirement: a text whose freshly-computed `IsCaveated` is `false` (the default)
backfills silently; the five known-real caveated texts (Blastajet Force Field, Leader-beast, Lesk's
Heroes, Redoubtable Machine Spirit, Scattershield) compute `true` and surface once as new information
on their already-verified entries — exactly the outcome the baseline mechanism was built to support.

**Review-needed report section keyed on "caveated AND no note," not on Drift/NewInformation status.**
Discovered necessary immediately after landing: once a caveated entry is baselined and stops changing,
it collapses into the "unchanged" summary count and nothing ever prompts a human to actually decide
"extend the classifier" vs. "accept this." The fix reuses the existing `note` field as the sole
decision-recorded signal (no new schema) — a baselined text with `IsCaveated: true` and `note: null`
appears in a new "Caveated baseline entries needing review" section on every run, regardless of
whether anything else about it changed; adding a `note` is what removes it. Deliberately independent
of the Unchanged/Drift/NewInformation split (a caveated-but-unchanged entry is exactly the case that
split would otherwise hide forever).

**`FullyHandled` added to `RuleClassificationBaselineEntry`, deliberately NOT to `RuleClassification`
itself.** Discovered necessary reviewing Marshal's Household's own entry: `IsCaveated` is a purely
mechanical, text-only fact ("is there text left over") with no way to distinguish real omitted game
content (the original 5 caveated entries' shape) from content the classifier's vocabulary was never
meant to cover at all (an army-composition eligibility restriction). Confirmed this isn't a one-off:
the identical "Restrictions:" trailing shape recurs 12 times across Space Marines chapter Detachments,
though only this one currently produces an extracted Effect. `FullyHandled` is the human verdict that
answers the *different* question a future `/LivePlay` consumer will actually need answered: "does the
player need to be prompted to re-read this ability's full text?" — `IsCaveated` says "textually, yes,
there's more"; `FullyHandled` says "a human already read it, and there's nothing there worth reading
again." Kept off `RuleClassification` on purpose: that type has nothing to freshly compute for this
field, so putting it there would mean it enters `RuleClassificationDiff`'s comparison and inherits
exactly the schema-growth DRIFT-vs-NEW-INFO mislabeling problem `IsCaveated` itself just exposed (see
the "Verified baseline" section above) — for zero benefit, since nothing could ever "drift" on a field
nothing computes. Never touched by `--write-baseline`'s refresh loop, mirroring `Note` exactly.

*Alternative considered*: fix the general case — teach the caveat signal itself to recognize a trailing
"Restrictions:" section structurally, mirroring the already-existing leading-restriction exclusion.
Rejected for now, not ruled out: only one entry is actually affected today (the other 11 "Restrictions:"
occurrences don't yet produce an extracted Effect at all), so building general classifier logic against
a currently-N=1-active pattern is exactly the premature-complexity risk this codebase consistently
avoids elsewhere. Documented in `.claude/vnext-ideas.md` as a confirmed, real, deferred fix to revisit
if more Detachment-rule Effects surface this shape.

**`Names` added to `RuleClassificationBaselineEntry`, refreshed by `--write-baseline` (unlike
`Note`/`FullyHandled`).** Discovered necessary from direct user feedback: the baseline is deliberately
keyed by Text, not Name, so locating a specific entry in the checked-in JSON meant searching for a
snippet of (sometimes long, sometimes near-duplicate) Text — a real ability Name is what a person
actually remembers. `Names` records every distinct Name the corpus currently attaches to that Text,
sorted; it's corpus-derived, not a human judgment call, so it's refreshed by `--write-baseline` exactly
like `Target`/`Effects`/`IsCaveated` rather than preserved untouched like `Note`/`FullyHandled`. Purely
cosmetic — Text remains the sole identity key, and `Names` is never compared by anything (it doesn't
appear on `RuleClassification`, so it can never be part of `RuleClassificationDiff`'s comparison
either, for the identical reason `FullyHandled` isn't).

## Risks / Trade-offs

- **Regex widening always risks new false positives in a ~3,800-text corpus**, even when anchored the
  same way as proven patterns. Mitigated by re-running the corpus report after implementation and
  manually reviewing every newly-produced Effect result before merging — the same discipline that
  caught all six real bugs in the original classifier build.
- **The caveat signal's validated split (5/15) only covers today's 20 Effect results.** Widening the
  Effect patterns in this same change could add new Effect results whose caveat determination hasn't
  been hand-verified. Task list requires re-checking the caveat signal against every Effect result
  (not just the original 20) after the pattern widenings land, in the same pass.

## Migration Plan

No runtime migration — this only affects the offline classifier and its corpus-report tool. After
implementation: re-run `RuleEffectClassificationReport` against the live clone, review new/changed
output, then run it with `--write-baseline` to backfill `IsCaveated` onto existing verified entries
and record the newly-caught texts. No rollback concerns beyond reverting the commit.
