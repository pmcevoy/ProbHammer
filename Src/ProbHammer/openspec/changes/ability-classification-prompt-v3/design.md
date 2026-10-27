# Design

## Context

How the classifier works today (`tools/AbilityPipeline/Classifier`):
- `submit` selects hashes with no record or a different prompt label (`IncrementalSelector`).
- `collect` writes every result into `data/classifications.json`, overwriting a record of the same
  hash, including the 20 hand-corrected records that carry a `ReviewerNote`.
- The model is a constant (`Model.ClaudeSonnet5`). The pinned SDK, Anthropic 12.50.0, has no Sonnet
  5.5 constant.
- The output schema is generated from the C# models (`schema` command), so the models are the schema.
- `export` writes every record it holds, live or not.

On the app side, Core's `AbilityClassificationCatalogue` fails the load on an unknown effect kind, so
adding `RollModifier` needs a Core type, even though nothing in Core consumes it yet.

Judiciar's footnote is now only "Against melee attacks only". The InSv caveat resolution
(`InvulnerableSaveEffectResolver.ResolveCaveat`) needs a classified InvulnerableSave effect, and a
text with no value gives the classifier nothing to extract.

## Goals / Non-Goals

**Goals:**
- One full v3 run whose output is trustworthy enough to export.
- Small, cheap draft runs first, to compare models and to check the hand-corrected records.

**Non-Goals:**
- Any LivePlay rendering of roll modifiers.
- Netting or capping roll modifiers. Each one is stored as stated, and the 11e stacking rule is a
  question for the simulation work.
- Changing how hand-corrected records survive a re-run. A correction v3 doesn't reproduce is
  re-applied by hand from the backup.

## Decisions

**`RollModifier` shape.** Fields: `roll`, `rolledBy`, `modifier`, `amount`, and an optional
`selector`.
- **The verbs are the text's own.** The corpus words roll changes as "add N to" (215 texts) and
  "subtract N from" (178), and never as "improve" or "worsen" a roll. That differs from
  characteristics on purpose, matching the Scalar convention of recording the text's own verb.
- **`rolledBy: friendly | enemy`** says whose roll it is. It was chosen over `made`/`targeting`, which
  aren't opposites, and over attacker/defender, which doesn't fit Advance, Charge or Battle-shock.
- **Re-roll variants are values of `modifier`** (`reroll`, `rerollOnes`, `rerollSingle`), not a
  separate scope field. A flat enum is easier for the model to fill and is still enough for the
  simulation.
- **The classifier records weapons as flat fields.** The first draft batch failed with "The
  compiled grammar is too large": v2's grammar sat at the ceiling, and `RollModifier` tipped it over
  (as `Choice` did in v2). Flattening only `RollModifier`'s selector was not enough, so every weapon
  effect (`WeaponCharacteristic`, `WeaponKeywordGrant`, `RollModifier`) now has optional
  `weaponClass` (`Melee`/`Ranged`) and `weaponName` fields instead of the `WeaponSelector` union,
  which is deleted. The v3 schema ends up smaller than v2's. `export` turns the fields into Core's
  `selector` (every weapon when neither is set; for a roll modifier, Hit, Wound or Damage only), so
  Core and the catalogue file keep the selector shape. The stored `classifications.json`,
  `classifications-v1-sample.json` and few-shot file were migrated once by script, verified lossless
  effect by effect, and a test export matched the checked-in catalogue on all 3,299 shared records
  (user decisions, 2026-10-26).
- **Damage-characteristic reduction is `Damage`/`enemy`/`subtract`.** It is technically a
  characteristic, not a roll, but it fits the shape without adding a kind.

**The residue split is a second field.** `unclassifiedResidue` keeps its meaning (in scope, not
expressible), so the provenance popover and existing records keep working unchanged. The new
`outOfScopeResidue` is optional. Core loads it, and the popover shows it after the in-scope note, so
what a player sees doesn't shrink. A per-item tag inside a list was rejected: it is a more complex
schema for the model to fill, and nothing consumes item-level structure.

**Model option on `submit`, `--out` on `collect`.**
- `--model sonnet-5 | sonnet-5-5` is stored in `pending-batch.json`, which already records the
  model, so `collect` knows which model produced the results.
- `collect --out <file>` writes the results to that file under `data/` and leaves
  `classifications.json` alone. That lets the two draft runs share the same hashes and keeps the
  hand-corrected records intact until the full run.
- For Sonnet 5.5, the first choice is upgrading the Anthropic SDK to a version with the constant. If
  no such version exists, the model is built from its id string `claude-sonnet-5-5`.
- Neither model is given a thinking setting, and the structured-output request is unchanged. None of
  Sonnet 5.5's breaking changes (disabled thinking, forced tool choice) apply to this request.

**The draft test set.**
- It contains:
  - the live subset of the 20 `ReviewerNote` records, minus Doctrina Imperatives, which is a
    few-shot example;
  - Combat Doctrines, Templar Vows, Aegis of the Emperor and Daughters of the Abyss;
  - a fixed-seed sample of about 10 roll-modifier texts, to check the new kind before paying for the
    full run.
- Both models run under the label `v3-draft`.
- The new rules' live hashes:
  - Combat Doctrines (Army rule): `02666a37a7f1b72ed2d8c2b3c9d47395b18e7346d98f82fe68e01d9cc21fa4e4`
  - Templar Vows (Detachment rule):
    `f9d04adfd98d93e40b2c0e0c2d5f0d372ec713af91e76290371c3cfab15ee3b1`
  - Aegis of the Emperor: `2b3f74675d9ab2e176732058feb275a3a1d9b443a7e236b9fe519a182fd480dd`
  - Daughters of the Abyss: `9e611a48a1d6186671ba7fba7e49b92f5e783981ad3e5e44af01466926f3741a`
  - Judiciar's footnote ("Against melee attacks only"):
    `b00ea448ebc49f4e4c96b6d5d7b57d78fb5cd7660704be07b26e159c92d6f21f`. It is resolved
    structurally, so it is not submitted.
- 15 of the 20 `ReviewerNote` records are live. Two are few-shot examples and are left out: Doctrina
  Imperatives (`b8d86632…`) and the Army-rule Templar Vows text (`e4c3b6f9…`), which is still live.
  The 13 submitted:
  - Attuned Onslaught (Psychic) `0e0bf1c3db6f9988a0c921a8c7886830dedbfcd3bf49850e484b725af13f50c6`
  - Knight Diabolus `1731614ab8b5a6ebf447496de0ee4f199587abb0e5ec6f6bad60bc245a99fb77`
  - Extremis Trigger Word `3b97f44ba0be93dbec2ec3671eecb703f22a127d471e5a4d076f94f8230402a0`
  - Spotter `425f5569a22b56c8248d860478f2e039c553c0cbc4294ad5f6c14e06bb52f125`
  - Whirling Death `499d2fd591bd65b78ae09bf5f43d62ae453ba90bf1080dd20b0164f150c2de98`
  - Speed of Vaul `791af13b6ca7dc0dfe721ff02562ea9cee498ff0e441e05661f83bd14752e536`
  - Debt to the Soulforge `8c972e49d6725b0df96db1c32b141f2114f0f1fdeeb5976a8c9c74e2d61a3703`
  - Mindlock `8ce7078258a2333287b199358d41c0261b03e81543ccc4f1ff07f651cfd79782`
  - Swift as the wind `a623412db14367afd5a79e921b9975bc6d708738dd451496f13e723201f04cb5`
  - Brutal Raider `a721d0965ed0bd4ee7830813205fa0ddd4f74c078c5b9628cfb45811661c0c55`
  - Psychic Guidance `a7b5a2feb92f0b0f759d3b1dd55709b9413bd9fc916dc4a483be785a10ed4290`
  - Assisted Targeting (Aura) `bc68ff707ee31b10aaad0b1ed5e3eefc198d60d3adf4e1d2b01c7636b5750334`
  - Impetuous Glory `d2dda13fbb514749995d6a1f75983a728a43a22fa14f9b57e820deb8c44f08db`
- The comparison covers outputs side by side, reviewed in the user's spot-check format, and output
  tokens per record.

**A live hash is one the latest extraction saw.** The Extractor keeps a hash absent from its run,
with its old `LastSeenAt`, so "in the corpus" is not "in BSData". A hash is live when its
`LastSeenAt` equals the newest `LastSeenAt` in the corpus. Both `export` and `submit`'s selector use
this, so the full run skips the stale texts. Changing the Extractor to drop them was rejected: it
would undo its presence tracking (user decision, 2026-10-26).

**Pruning happens in `export`, never in `classifications.json`.** The classification results are
the pipeline's memory, "the model's output is never rewritten". Pruning only at export means a text
BSData later reverts is still classified. The export reports its count, so running it against an
out-of-date corpus shows up as a large number dropped.

**The control-character check fails the export instead of cleaning the text.** Stripping characters
would hide damage like v2's junk-for-apostrophe. Failing points at the record so it can be fixed by
hand or re-run.

**The attack-type-only footnote is resolved by an exact whole-text match, before the catalogue
lookup.** This follows the precision-over-recall preference: a structural anchor, not a growing
phrase list.
- The value comes from the footnoted digit the caveat already carries. The other side follows the
  existing bare and split rules.
- Matching the whole text means "Against melee attacks only, while leading" falls through to the
  catalogue.
- Asking the classifier was rejected, because the text has no value to extract.

**Few-shot examples.** Add two `RollModifier` examples: one `friendly` re-roll with a ranged
selector, and one `enemy` defensive Hit modifier. Both must be live corpus hashes
(`check-fewshot`). Because `fewshot/examples.json` is shared across prompt versions, the new field
must be optional so v2 examples still load. Five existing examples taught v2 semantics that v3
reverses (Harlequins charge-through, the Dark Pacts removal, Emperor's Champion, Templar Vows,
Doctrina Imperatives). They are rewritten to v3 in the shared file rather than split per version:
v2 is retired, so v2 only needs its examples to load (user decision, 2026-10-26).

## Draft runs and model choice (2026-10-26)

Both drafts ran the 27-hash test set: Sonnet 5 at 561 output tokens per record, Sonnet 5.5 at 402,
both with 27 of 27 succeeding, about $0.13 each. Reviewed record by record with the user (verdicts in
the session scratchpad):
- Sonnet 5 was better on 5, 12, 13 and 18. It also wrote six damaged strings (`+`, `"` for an
  apostrophe, empty weapon names), left six names unresolved (a fused `DAEMON VEHICLE`, "... profile"
  weapon names), put an enemy unit's strength under `evaluable-now` (22), and set `mine` without cause
  (20).
- Sonnet 5.5 was better on 1, 2, 11, 14, 17, 20, 22 and 23, with clean strings and one unresolved
  name. Its misses were rule readings: an optional activation left `none` (7, 13), a keyword choice
  dropping its re-rolls (18), and a model subset written into condition text (5).
- Both reproduced the five BS/WS hand corrections. For the eight event-trigger hand corrections, Sonnet
  5 reproduced 7 and Sonnet 5.5 reproduced 6; both missed Extremis Trigger Word.

**Model: Sonnet 5.5** (user decision).

**Prompt fixes from the review** (user decisions; the pipeline spec delta carries each as a scenario):
- An optional activation that switches an effect on for a limited time is `never`, with the
  activation as its condition text (Extremis Trigger Word, Swift as the wind).
- A Normal, Advance or Fall Back move only happens in your own Movement phase, so an ability it
  triggers is `mine`. A Charge move and the attack sequence imply nothing (Whirling Death, Speed of
  Vaul).
- A keyword or parameter choice keeps the effects it gates, as `never` naming the choice; only the
  selection is residue (Fated Hero).
- A unit's own ability naming its own models is `Self`; a model subset never goes into condition
  text (Attuned Onslaught).
- A "■ Trigger: / ■ Effect:" manoeuvre is chosen like a Stratagem, so its effects are `never`. This
  was added after the six-record re-run: that run fixed 5 of 6, but Swift as the wind (an Agile
  Manoeuvre, one of 4 live texts in that layout) stayed `none`.

## Full run (2026-10-27)

**Run:** batch `msgbatch_01QQq5DPXvcch4fXE8pbzDh7`, Sonnet 5.5, 3,785 requests: 3,785 succeeded, 0
failed. Tokens: input 411,734, output 1,038,328 (274 per record, against v2's ~510), cache read
82,717,390, cache write 0, about $13.90 at Sonnet 5.5 batch pricing (estimate was ~$16). Backup of
the pre-collect `classifications.json` (sha256 `eea221c0…`) is in the session scratchpad.

**Coverage of the 3,785 v3 records** (model's own status / effective after name resolution):
complete 2,641 / 2,617, partial 319 / 343, unclassifiable 825 / 825. v2's run was 752 / 455 / 2,616 of
3,823. 1,473 records carry out-of-scope residue, 1,280 of them `complete`. 765 records carry at
least one roll modifier (1,023 `RollModifier` effects). The 529 stale records keep their v2
classifications.

**Hand-corrected records (7.3).** The 15 live `ReviewerNote` records were reclassified; the 5 stale
ones keep their v2 records and notes. v3 reproduced every fix unaided, so none was re-applied:
- event trigger `never` (8 of 8, including Extremis Trigger Word and Swift as the wind, which both
  draft models missed);
- BS/WS effects (5 of 5);
- apostrophe junk (2 of 2 clean; Medical scalpels went from unclassifiable to a WS effect on the named
  weapon).

Other differences follow the v3 rules: Paladin Squad and the Agile Manoeuvre target `Self` and
`AttachedUnit`, Advance triggers set `mine`, Daemon Vehicle's keywords are split, and Doctrina
Imperatives and Templar Vows gain their roll modifiers and go `complete`.

**Spot-check (7.4)**, 24 records drawn with seed `v3-spotcheck-7.4` (sha256 of seed plus hash): the
four new rules, 5 `complete` records, 5 `complete` with out-of-scope residue, 10 with roll modifiers.
Verdicts (user-agreed):
- 1-7, 9-11 correct. Nits: Combat Doctrines and Templar Vows have `MinSelect: 1` for an optional
  "you can select" (no visible effect, "None" is always offered); Daughters of the Abyss grants
  Psychic Null, which is defined inline and so unresolved.
- 8 (Grey Knights fight-phase rule, `cc747e07…`) wrong: `complete`, but its grants apply only to
  [Psychic] melee weapons and the selector covers every melee weapon. Hand-corrected to `partial`
  with a `ReviewerNote`; a weapon-keyword-filtered selector is parked in vnext-ideas.
- 12-23 correct. Every roll-modifier record had the right roll, `rolledBy`, modifier and selector.
  Synaptic Imperatives has `MinSelect: 0` for the same "you can select" wording, so the nit above is
  inconsistency, not a rule the model misses. Record 20 (`47c806a8…`) is an honest `partial` (an
  any-of keyword target), and its "Aspect Warriors" didn't resolve because BSData spells it "Aspect
  Warrior"; plural keyword resolution is parked in vnext-ideas.
- 24 (`f3bdecfd…`) was `partial` for shoot-after-Fall-Back eligibility, which Combat Doctrines puts in
  out-of-scope residue. Hand-corrected to `complete` with a `ReviewerNote`.

Totals: 22 correct, 1 wrong, 1 inconsistent, both hand-corrected.

**Export (8.1).** `resolve` then `export`: no control characters, 529 stale records left out. The
effective split over all 4,314 records is 2,738 / 416 / 1,160. `resolve` re-resolves the stale v2
records against the refreshed vocabulary too, demoting four of them (renamed Custodes weapons among
them); they are stale, so none is exported.

**"Units with this ability" targets (8.2).** v3 gave Aegis of the Emperor ("Friendly Adeptus
Custodes units with this ability have…") a Keyword target. The aggregator applies a Keyword target
only for Detachment and Army rules, so the datasheet ability never matched and the "Feel No Pain 5+*"
footnote stayed caveated. "With this ability" already names the bearer's unit, so seven such
records were hand-corrected to `AttachedUnit` with a `ReviewerNote` (user decision): Aegis of the
Emperor, Daughters of the Abyss, Martial Ka'tah, Mission Tactics (two texts), Questing Tendrils,
Unstable Energies. Combat Doctrines keeps its Keyword target, since an Army rule ignores it. The new
codex reworded Living Fortress to "-1AP" (no FNP), so the Wardens test now asserts Aegis's qualified
amber "–" tile instead of a conditional one. 8.3's "Living Fortress is switchable" check is dropped
for the same reason: it has nothing left to switch (user decision).

**The text key trims surrounding whitespace (8.4).** Combat Doctrines had no classification on a
NewRecruit import: BSData's text ends in a non-breaking space and two newlines, NewRecruit's export
of it is trimmed, so the hashes differed. 58 corpus texts (52 live) had surrounding whitespace.
`AbilityTextKey.Normalize` now trims, alongside its apostrophe and NBSP folding (user decision over a
lookup-only fallback). `ability-corpus.json` and `classifications.json` were re-keyed once by a
script using the pipeline's own load/save: 4,314 to 4,312 records. In two cases the trimmed text
was already a record of its own and the two were merged, keeping the already-trimmed key's v3
classification: Sorrowscent Vulture (identical) and Narthecium (residue wording only). Every
untouched record is unchanged, and every classification's text matches its corpus record.

**Live checks (8.3, 8.4).**
- Custodes: every FNP tile is amber "–", with Aegis of the Emperor (5+ mortal wounds) or Daughters
  of the Abyss (3+ psychic attacks) in its popover, and the nested ability popover opens.
- Templars:
  - Combat Doctrines offers None plus three doctrines per unit.
  - With Vow-Sworn Crusaders, each unit carries one Templar Vows with its Vow choice.
  - The Judiciar's InSv is melee 4+.
  - To get the Judiciar and Vow-Sworn Crusaders, the user updated `nr-templars-for-unit-tests.json`
    (dropping Tactical Brethren and its Enhancements to stay within the Detachment limit); no test
    depended on them.
- Open, not in this change:
  - Judiciar's popover still shows the footnote record's "nothing can be classified" residue (left
    as is, in vnext-ideas).
  - The NewRecruit export lists the Judiciar's Fights First twice, and the BattleScribe mapper renders
    both.

**`classifications-v1-sample.json` deleted.** Nothing reads it and v1 is retired. Loading it through
the current types also loses its v1-only singular `Keyword` target field (user decision).

## Corpus refresh (2026-10-26)

The Extractor saw 3,785 distinct texts: 486 new, 3,299 known, and 529 stale (kept, `LastSeenAt`
2026-10-03). The corpus now holds 4,314 records. Against the backup, no hash was removed, and 64
known records changed besides `LastSeenAt`: 61 in `Names` (names moving to new texts in the codex)
and 4 in `SourceKind`.

## Risks / Trade-offs

- [Unreviewed `complete` records now include roll modifiers] → Nothing consumes them yet, so a wrong
  one changes no value on screen. The spot-check samples roll-modifier records specifically before
  the follow-up change builds a consumer.
- [The residue split pushes more records into `complete` on the model's own judgement] → An
  out-of-scope item never changes a value LivePlay shows. The spot-check includes records that went
  `complete` only because of out-of-scope residue.
- [The full run overwrites the 20 hand-corrected records] → Back up before `collect`, compare
  against the backup, and re-apply any correction v3 doesn't reproduce.
- [Two draft runs are too small to cache] → The cost is cents, so this is accepted.
- [Sonnet 5.5 can stop with a `refusal`] → `collect` already counts a response without a text block
  as failed. Failures are reported and can be re-submitted.
- [Re-running the Extractor restamps every `LastSeenAt`] → Diff the corpus against a backup to
  confirm only the expected hashes were added or removed.

## Migration Plan

The order of work is in tasks.md. Rolling back is a git revert of the exported catalogue and
`classifications.json`; no data outside the repo changes.
