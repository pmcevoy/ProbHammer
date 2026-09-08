## 1. Pattern widening

- [x] 1.1 Add "Move" as a second key in `CharacteristicNames` mapping to `"M"`, alongside the
      existing "Movement" key.
- [x] 1.2 Widen `AddCharacteristic`'s regex to accept an optional possessive noun
      (`(?:[A-Za-z]+'s\s+)?`) between "the" and the characteristic name.
- [x] 1.3 Add a new `SentenceStart`-anchored `[GeneratedRegex]` (e.g. `ShorthandCharacteristicPlus`)
      recognizing "+N" followed by one of the six Statline scalar codes (`M`/`T`/`Sv`/`W`/`Ld`/`Oc`,
      case-insensitive, whole-word) with an optional intervening "to", and wire it into
      `ClassifyEffects` as an `Improve` Effect.
- [x] 1.4 Update `RuleEffectClassifier`'s class-level and method-level doc comments to describe the
      three widened/added patterns, following this file's existing convention of citing the specific
      confirmed real corpus example each one fixes.

## 2. Caveat signal

- [x] 2.1 Add `IsCaveated` (bool, defaulting `false`) to `RuleClassification`.
- [x] 2.2 Restructure `ClassifyTarget`/`ClassifyEffects` (or add sibling overloads) so the `Match`
      object(s) that actually contributed to the result are available to `Classify`, not just the
      resulting `RuleTarget`/`Effects` values.
- [x] 2.3 In `Classify`, when at least one Effect was extracted, compute the caveat determination:
      take the maximum `Match.Index + Match.Length` across every contributing Target/Effect match,
      trim the remaining text (whitespace, then a single trailing period), and set `IsCaveated` true
      when non-empty.
- [x] 2.4 Confirm a zero-Effect classification is never marked caveated regardless of Target.

## 3. Unit tests

- [x] 3.1 Add positive/negative test cases to `RuleEffectClassifierTests.cs` for: the shorthand "+N
      Characteristic" pattern, the possessive characteristic-name widening, and the "Move" synonym.
- [x] 3.2 Add test cases reproducing the five known-real caveated texts' shape (a matched Effect
      clause followed by real additional content) and confirm `IsCaveated` is `true`.
- [x] 3.3 Add test cases for the two known-real non-caveat shapes that could be mistaken for
      caveats: a leading eligibility restriction before the matched clause with nothing trailing
      (Sanctuary/Brute Shield's shape), and an ordinary clean single-Effect text with nothing
      trailing at all.

## 4. Corpus validation

- [x] 4.0 Surface `IsCaveated` in `RuleEffectClassificationReport`'s `Describe` output - discovered
      missing while starting 4.1/4.2 (the report currently prints only Target/Effects, so the new
      field can't be manually reviewed without this). Also discovered and fixed:
      `RuleClassificationBaselineEntry` had no `IsCaveated` property at all, so `--write-baseline`
      could never actually persist it - every run would have re-surfaced the same "changed since
      verified" entries forever. Fixed by adding the property and threading it through
      `Classification`/`--write-baseline`'s upsert.
- [x] 4.1 Re-run `tools/RuleEffectClassificationReport` against the live BSData clone
      (`C:\Users\Pete\wh40k-11e`) and manually review every Effect result the widened patterns newly
      produce (not just the previously-known Marshal's Household/possessive/Move examples) for false
      positives, per design.md's stated risk. Found and fixed one real bug in the new
      `ShorthandCharacteristicPlus` pattern itself (a comma in the subject-run character class defeated
      `SentenceStart`, caught by its own negative test before the corpus run). 16 new real Effect
      results found; all manually reviewed and confirmed correct extractions - no false positives.
- [x] 4.2 Manually review the `IsCaveated` value on every Effect result in this run (not only the
      original 20), confirming each caveated/non-caveated determination matches a human read of its
      source text. All 16 new results checked by hand; 6 correctly caveated, 9 correctly clean, and one
      confirmed false NEGATIVE (Master Artisan - see its baseline `note`): the caveat signal misses a
      real unextracted second clause because the Target-classifying match's own span happens to consume
      the tail of that same unextracted text, leaving nothing trailing to detect. This is the same
      underlying gap as the already-parked "and add..." SentenceStart widening (see design.md's
      Non-Goals) - documented, not fixed here.
- [x] 4.3 If a false positive or wrong caveat determination is found, narrow the offending pattern or
      the caveat trim logic and re-run 4.1/4.2 until the output is clean. (The `ShorthandCharacteristicPlus`
      comma bug above was the one real false positive found and fixed; Master Artisan's false negative
      is a documented, deliberately-not-fixed limitation tied to the parked "and add" gap.)
- [x] 4.4 Run `tools/RuleEffectClassificationReport --write-baseline` to backfill `IsCaveated` onto
      existing verified baseline entries and add any newly-caught real Effect texts as new baseline
      entries. Backfilled all 20 existing entries (one, "Army: Shivversplint", genuinely flipped to
      `IsCaveated: true` - a real, previously-unnoticed caveat: its text names a Crusade-only scope
      qualifier the classifier's vocabulary doesn't capture). Added all 16 new real Effect results as
      baseline entries by hand (fetching exact raw text via `jq` scoped to each ability's actual source
      file, per vnext-ideas.md's documented embedded-newline trap - one text, Faith-Fuelled Resolve,
      does carry a literal embedded newline). Final state: 36 tracked, 36 unchanged, 0 changed, 0
      unbaselined Effect results.

## 5. Documentation

- [x] 5.1 Update `.claude/vnext-ideas.md`: remove the four now-closed queued items (shorthand
      phrasing, possessive phrasing, Move synonym, caveated signal) from the
      "Characteristic-modification domain hardening" section, keeping the still-open items (the
      parked "and add" widening, tier 3+ conditions, `DerivedValue` computation, etc.) intact. Also
      recorded three new real findings from this change's own implementation: the Shivversplint
      sixth-caveat discovery, the Master Artisan caveat-signal false negative, and the baseline
      schema-growth DRIFT-vs-NEW-INFO labeling gap (the last one flagged as a
      `rule-effect-classification-baseline` concern, not fixed here).
- [x] 5.2 Update `.claude/domain-model-11e.md`'s "Rule Effect Classification (Text-Only)" section to
      describe the widened patterns and the new `IsCaveated` field.

## 6. Review-needed report section (added post-landing, user-requested)

- [x] 6.1 Add a "Caveated baseline entries needing review" section to
      `RuleEffectClassificationReport`: every baselined text whose current `IsCaveated` is true and
      whose baseline entry's `note` is null, independent of Unchanged/Drift/NewInformation status.
- [x] 6.2 Verify against the live BSData clone: confirms exactly the one real, currently-undecided
      caveated entry ("Army: Shivversplint") and none of the other 35 tracked entries (30 non-caveated,
      5 caveated-with-a-note already recorded).
- [x] 6.3 Add a `rule-effect-classification-baseline` delta spec (`ADDED Requirements`) for this new
      behavior, and update proposal.md's Capabilities/design.md accordingly.
- [x] 6.4 Re-run the full test suite and `openspec validate --strict` to confirm nothing regressed.

## 7. `FullyHandled` human verdict (user-requested, distinguishes two kinds of caveat)

- [x] 7.1 Add `FullyHandled` (bool, default `false`) to `RuleClassificationBaselineEntry` — a
      human-only verdict, deliberately kept off `RuleClassification` itself so it never enters
      `RuleClassificationDiff`'s comparison (nothing computes it, so nothing could ever "drift" on it).
      Never touched by `--write-baseline`'s refresh loop, mirroring `Note`.
- [x] 7.2 Confirmed the "Restrictions:" trailing-eligibility shape is real and recurring (12
      occurrences across Space Marines chapter Detachments in the live clone, via `jq`), not a one-off —
      informed the decision to add a proper field rather than a one-off note-only workaround, while
      still deferring the general classifier fix itself (see design.md's "Alternative considered").
- [x] 7.3 Mark the Marshal's Household / Faith-Fuelled Resolve baseline entry `fullyHandled: true` with
      an updated `note` explaining the eligibility-restriction reasoning and citing the 12-occurrence
      corpus finding.
- [x] 7.4 Update `RuleEffectClassificationReport`: exclude `FullyHandled` entries from the "needing
      review" section (alongside the existing `note`-present exclusion), and surface a `[FULLY
      HANDLED]` marker plus a summary count in the "Verified baseline" line.
- [x] 7.5 Add `rule-effect-classification-baseline` delta spec requirements for the `FullyHandled`
      verdict and its interaction with the review listing; update proposal.md/design.md.
- [x] 7.6 Re-run against the live BSData clone to confirm Marshal's Household drops out of "needing
      review" while "Army: Shivversplint" remains (the one real undecided item); re-run the full test
      suite and `openspec validate --strict`.

## 8. `Names` findability field (user-requested)

- [x] 8.1 Add `Names` (`IReadOnlyList<string>`, defaults to empty) to `RuleClassificationBaselineEntry`
      — purely a human-findability aid, never part of an entry's identity (Text remains the sole key).
- [x] 8.2 Wire `--write-baseline`'s refresh loop to also set `Names` from the current corpus run —
      unlike `Note`/`FullyHandled`, it's a corpus-derived fact, not a human judgment call, so it
      refreshes alongside `Target`/`Effects`/`IsCaveated` rather than being preserved untouched.
      Deliberately excluded from `RuleClassification`/`RuleClassificationDiff` for the same reason
      `FullyHandled` is - nothing there could ever "drift" on a field nothing on that side computes.
- [x] 8.3 Run `--write-baseline` against the live BSData clone to backfill `Names` onto all tracked
      entries; verify a multi-name-collision entry (the 12 invulnerable-save items sharing one sentence)
      records every distinct Name, and single-name entries record exactly one.
- [x] 8.4 Add `rule-effect-classification-baseline` delta spec requirements for `Names`; update
      proposal.md/design.md.
- [x] 8.5 Re-run the full test suite and `openspec validate --strict`.
