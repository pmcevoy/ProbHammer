# Rule Effect Classification (Text-Only)

Full requirements: `openspec/changes/classify-rule-effects-from-text/` (original extraction),
`openspec/changes/widen-rule-effect-classification-coverage/` (pattern widenings, the `IsCaveated`
signal), `openspec/changes/baseline-rule-effect-classifications/` (the checked-in baseline),
`openspec/changes/resolve-invulnerable-save-effects/` (the widened InSv extraction described below,
plus the standalone resolver documented in its own "Invulnerable-Save Effect Resolution" section).
A standalone, text-only
counterpart to the structural `CharacteristicModifierCandidate` classifier (characteristic-modifier-caveats.md) — instead of
reading BSData's structured `BsModifier` JSON, `RuleEffectClassifier.Classify(name, text)` extracts
a rule/ability's Target and unconditional characteristic Effects from its own Name+Text alone, with
**no dependency on `Domain.Catalogue.Bsdata` or any BSData JSON type** — same classification for
identical Name+Text regardless of which import pipeline (BSData or BattleScribe/NewRecruit)
produced it. Lives in `Domain.Catalogue` (not `Domain.Catalogue.Bsdata`) for exactly that reason —
the user's own stated discomfort with adding more classification responsibility to
`BsdataDatasheetMapper` (already the source of several real data-misunderstanding bugs found only by
manual NewRecruit cross-checks) is why this is an independent component rather than an extension of
that mapper's existing ability-extraction walk. **Not wired to anything yet** — no
`BsdataDatasheetMapper`/`AttachedUnitAggregator`/`/LivePlay` call site consumes a
`RuleClassification` today; this proves the extraction mechanism works, nothing more. See
`.claude/vnext-ideas.md`'s "Characteristic-modification domain hardening" entry for the still-open
follow-on work (conditional effects, `WeaponProfile`-targeting effects, `Multiply`/`Divide` verbs,
roster-wide predicate evaluation, applying a classified Effect anywhere, LLM-assisted discovery).

```
RuleTarget                            // Domain/Catalogue/RuleTarget.cs - see class's own doc
                                       // comment. Named SelfRuleTarget/AttachedUnitRuleTarget (not
                                       // bare Self/AttachedUnit) to avoid colliding with the real
                                       // Domain.Roster.AttachedUnit class.

EffectVerb = Improve | Worsen | Set   // see enum's own doc comment

CharacteristicEffect                  // Domain/Catalogue/CharacteristicEffect.cs - see class's own
                                       // doc comment
ScalarCharacteristicEffect(string Characteristic, EffectVerb Verb, int Amount) : CharacteristicEffect
                                       // see record's own doc comment
InvulnerableSaveCharacteristicEffect(InvulnerableSave Value) : CharacteristicEffect
                                       // see record's own doc comment

RuleClassification(RuleTarget Target, IReadOnlyList<CharacteristicEffect> Effects,
                    bool IsCaveated = false)
                                       // see record's own doc comment

RuleEffectClassifier.Classify(string name, string text) -> RuleClassification
                                       // Domain/Catalogue/RuleEffectClassifier.cs - see the class's
                                       // own doc comment; hardened by a live-review pass (below)
                                       // into "first sentence-anchored match wins" for the two
                                       // Effect patterns, not literally "first match anywhere."
RuleEffectClassifier.Normalize(string text) -> string
                                       // see method's own doc comment

// Case-insensitive on every word-literal pattern except AllCapsKeywordPhrase (case IS the signal
// there) - see each pattern method's own doc comment for the real-corpus examples motivating each
// rule.
//
// Target recognizes: "the bearer's unit" or "models in this unit" -> AttachedUnitRuleTarget; a
// markup-wrapped small-caps keyword or a literal ALL-CAPS run followed by "units" -> KeywordRuleTarget;
// nothing recognized -> SelfRuleTarget (the explicit default, never absent). See AttachedUnitPhrase/
// MarkupKeywordPhrase/AllCapsKeywordPhrase's own doc comments.
//
// Effects recognizes: "has a {N}+ invulnerable save" (excluding a match followed by "against") -> a
// uniform Set InSv; a melee/ranged-restricted grant, one- or two-sided -> Set InSv with the stated
// value(s), untargeted side 0 (InvulnerableSaveRangedRestricted/InvulnerableSaveMeleeRestricted,
// tried first); "Add {N} to the {Characteristic} characteristic" -> Improve {code} N
// (AddCharacteristic); a shorthand "+{N} {Code}" grant -> Improve {code} N
// (ShorthandCharacteristicPlus). See each pattern's own doc comment for the real-corpus examples and
// the MayStateInvulnerableSave pre-filter gate. Every Effect pattern is anchored by SentenceStart -
// see its own doc comment for why (a structural "is this clause at its sentence's true start" signal,
// not a growing denylist of trigger phrases) and the real false-positives it fixed.
//
// Never throws on unrecognized text - defaults to SelfRuleTarget + empty Effects (see spec's
// "Unrecognized Text Fails Closed").
```

**Caveated signal** (`widen-rule-effect-classification-coverage`): `RuleClassification.IsCaveated`
signals "this text states more than Target/Effects captured," without attempting to classify what the
extra content is. Computed only when at least one Effect was extracted
(`RuleEffectClassifier.IsCaveated`, private): `ClassifyTarget`/`ClassifyEffects` were widened to
return their contributing `Match` object(s) alongside their result, and `Classify` takes the maximum
`Match.Index + Match.Length` across every Target- and Effect-contributing match, trims the text
remaining after that position (whitespace, then a single trailing period), and marks the
classification caveated when non-empty. A leading eligibility restriction before the matched clause
(e.g. "Imperial Knights model only. The bearer has a 5+ invulnerable save.") never trips this - the
signal only ever looks *after* the last match. Validated by hand against all 20 pre-existing Effect
results (reproducing the known 5-caveated/15-clean split) and confirmed by a full corpus re-run:
found one previously-unnoticed sixth real caveat (a Crusade-mode-only scope qualifier the classifier's
`KeywordRuleTarget` doesn't capture), one confirmed false negative (Master Artisan - the
`AttachedUnitPhrase` match that classifies its Target happens to consume the tail of the very same
unextracted "and add..." second clause the parked `SentenceStart` gap already describes, below,
leaving nothing trailing to detect), and one confirmed false-POSITIVE-in-spirit (Marshal's
Household/Faith-Fuelled Resolve - its trailing "Restrictions:" paragraph is army-composition
eligibility text, the same *kind* of content an already-excluded LEADING restriction isn't caveated
for, just trailing instead; confirmed recurring 12 times across Space Marines chapter Detachments, not
a one-off) - all three documented on their own baseline entries rather than fixed; the third one
specifically via the new `FullyHandled` verdict (see "Verified-Classification Baseline" above), which
keeps `IsCaveated` itself truthful (a permanent textual fact) while still recording that a human
confirmed there's nothing left to read. See `.claude/vnext-ideas.md`'s "Characteristic-modification
domain hardening" for the full detail on all three, plus a real, confirmed (but out-of-scope-here) gap
in how the baseline capability's own schema-growth detection handles a brand-new top-level
`RuleClassification` field.

**Ground-truth verification** (`tasks.md`'s task 1, before any template was written): Shield Dome's
"The bearer has a 5+ invulnerable save." and Vexilla's "Add 1 to the Objective Control
characteristic of models in the bearer's unit." both confirmed byte-for-byte against the live
clone. Templar Vows' text (resolved via `Library - Astartes Heresy Legends.json`, not a file its own
name would suggest) contains "...active for **^^Adeptus Astartes^^** units from your army." — the
per-vow effect text further down names no single direct characteristic mutation, so zero Effects is
the correct, spec-confirmed extraction. The rule referred to as "Marshal's Household" is actually a
nested rule named "Faith-Fuelled Resolve" *inside* the "Marshal's Household" Detachment entry
(`Imperium - Space Marines.json`): "Friendly SWORD BRETHREN SQUAD units have +1 OC.\n\n\nRestrictions:
...ADEPTUS ASTARTES units..." — the Restrictions paragraph itself contains two further ALL-CAPS +
"units" occurrences, confirming the classifier must (and does, via first-match-wins) pick the
earlier, actual effect statement rather than either of those. Since
`widen-rule-effect-classification-coverage`, the "+1 OC" shorthand itself is also recognized
(`ShorthandCharacteristicPlus`), extracting `Improve Oc 1` alongside the `KeywordRuleTarget` — the
trailing Restrictions paragraph is real content beyond that Effect, correctly marked `IsCaveated`.

**Corpus-Wide Classification Reporting**: `tools/RuleEffectClassificationReport/` (its own console
app project referencing only `ProbHammer.Core`, not the test project) walks the live BSData clone
the same way `BracketTokenResolutionScanTests` does — local + shared rules, every locally-built
`Datasheet.Abilities` entry, plus `Datasheet.OptionalAbilityNames`/`TryResolveAbility` (needed since
Shield Dome/Vexilla are OptionalGrant-origin and never in the always-enumerated `Abilities` list),
plus every resolved Detachment's own rule text via `BsdataNameResolver.ResolveDetachmentEntries`/
`DetachmentRuleTextExtractor` (needed for Marshal's Household/Faith-Fuelled Resolve, which is
Detachment-nested text, not a Datasheet ability) — classifies every distinct rule/ability text found.
Forces `Console.OutputEncoding = Encoding.UTF8` up front - the process's default encoding can't
represent every character real BSData text carries (the same NBSP quirk `Normalize` handles) and
silently substitutes a garbage byte instead of erroring, confirmed real via a stray `0xFF` in report
output that turned out to be a mis-encoded NBSP.

**Grouped by `RuleEffectClassifier.Normalize`d Text alone** — neither by `(Name, Text)` nor by raw
Text. Not-by-Name: `Classify` never reads its own `name` parameter, so keying on Name too would
artificially split one real classification result across several report rows purely because different
wargear/abilities share the exact same sentence (confirmed real: 12 differently-named invulnerable-
save items — Astartes shield, Blizzard shield, Brute Shield, Dispersion Shield, Endurant shield,
Forceshield, Mistshield, Scattershield, Shield Generator, Shimmershield, Storm Shield, Weavefield
crest — all grant "The bearer has a 4+ invulnerable save." verbatim). Not-by-raw-Text: a typographic-
apostrophe or NBSP-vs-plain-space variant of one real sentence (confirmed: Ancient's Banner/Vexilla's
own text exists both ways) is something `Classify` already treats as identical input, so the report
groups it the same way rather than showing it twice. Every distinct Name seen for a given (normalized)
Text is still reported as data on the row, alongside a truncated preview of the text itself.

**Three report sections, not the spec's original two-way non-default/default-only split**: Effect
results (1+ Effects — the ones worth eyeballing to confirm an extracted Effect matches the text),
Target-only results (a Target broader than Self but no Effect — still "non-default" per the spec, just
not an Effect-review candidate), and Default-only results (spot-checked, first 25). Added after an
earlier two-way version (a user change narrowing the "non-default" section to Effects-only, for
focused Effect review) was found to silently drop the Target-only bucket from both printed sections
entirely — still counted in the header total, never shown anywhere.

**A fourth listing splits the Default-only bucket further** (`resolve-invulnerable-save-effects`,
D5/D6): the D5 gate (`RuleEffectClassifier.MayStateInvulnerableSave`) partitions Default-only into a
gate-rejected majority (no "invulnerable save" substring at all - carries zero review value, since no
InSv pattern could ever have matched it, and is excluded from any reviewable listing entirely) and a
small, distinct "Default-only results with unmatched invulnerable-save language" section, printed in
full rather than sampled. A live run against the full corpus found 3,174 total default-only texts,
3,127 gate-rejected, leaving 47 fully reviewable - all 47 manually reviewed: 46 correctly, deliberately
unmatched (a conditional preamble before the grant, a restriction axis other than melee/ranged, or a
pre-existing markup-prefixed-subject/mid-sentence-comma-list limitation `InvulnerableSaveGrant` already
had), and one real, confirmed gap (T'au's "Skirmish Fighters") recorded in `.claude/vnext-ideas.md`
rather than fixed - see that entry for the full detail.

**Final numbers after a sustained live-review pass found and fixed six real classification bugs** (see
`.claude/vnext-ideas.md`'s "Characteristic-modification domain hardening" entry and
`classify-rule-effects-from-text/tasks.md`'s task 4.3 for the full list — case-insensitivity, the
grouping fixes above, real NBSP normalization, the attack-type-restricted-save exclusion, the
`SentenceStart` anchor, and the "models in this unit" Target widening): 45 files, 3830 distinct texts,
**20 Effect results — every one manually reviewed and confirmed fully correct**, 618 Target-only, 3192
default-only (25-entry spot-check found nothing that looked like an obvious, cheap-to-recognize miss).
All four ground-truth examples classify as expected throughout.

**Updated numbers after `widen-rule-effect-classification-coverage`** (pattern widenings plus the new
`IsCaveated` signal, both described above): a fresh corpus run against the same 45 files found 16
additional real Effect results beyond the original 20 (615 Target-only, 3179 default-only — the drop
from 618/3192 reflects texts that now classify with an Effect instead), every one manually reviewed
and confirmed correct with no false positives; the same review found one real caveat-signal false
negative (Master Artisan) and one real, previously-unnoticed sixth caveat on an already-tracked entry
(Army: Shivversplint) — see the "Caveated signal" entry above and `.claude/vnext-ideas.md` for both.
The checked-in baseline now tracks all 36 Effect results, so a future corpus run reports these as
unchanged rather than reprinting them.

**Updated numbers after `resolve-invulnerable-save-effects`** (the widened melee/ranged-restricted
InSv extraction, described above): a fresh corpus run found 5 new real Effect results - Chaos
Knights' Ensorcelled Shield (ranged-only, caveated by its own trailing Feel No Pain grant) and Veil
of Medrengard (two-sided, not caveated), plus three more real ranged-/melee-only grants (War Dog
units' footnoted "*Invulnerable Save"/"Invulnerable Save (N+*)" profiles; Space Marines' Judiciar,
melee-only) - all five manually reviewed and added to the baseline, which now tracks 41 entries, all
unchanged on the next run.

**Weapon-characteristic Effects** (`classify-weapon-characteristic-effects`): alongside the Statline-
scalar and InSv shapes above, the classifier now also recognizes an unconditional mutation of a
*weapon's* own characteristic (Strength/Attacks/Armour Penetration/Damage — `"S"`/`"A"`/`"AP"`/`"D"`,
disjoint from `CharacteristicNames`' Statline vocabulary), scoped to a `WeaponSelector`
(`AllWeapons`/`WeaponClass(WeaponType)`/`NamedWeapon(string)`, mirroring `RuleTarget`'s own
abstract-base/sealed-subtype/`[JsonPolymorphic]` shape) naming which of the bearer's weapons it
applies to:

```
WeaponSelector                          // Domain/Catalogue/WeaponSelector.cs - see class's own doc
                                         // comment
AllWeapons : WeaponSelector             // every weapon, unqualified
WeaponClass(WeaponType Type) : WeaponSelector   // every weapon of one class (melee/ranged)
NamedWeapon(string Name) : WeaponSelector       // one specifically named weapon - no real corpus
                                         // example yet, kept for completeness

WeaponCharacteristicEffect(WeaponSelector Selector, string Characteristic, EffectVerb Verb,
                            int Amount) : CharacteristicEffect
                                         // Domain/Catalogue/CharacteristicEffect.cs - the third
                                         // sealed CharacteristicEffect subtype
```

Two extraction shapes, mirroring the InSv family's own multi-pattern precedent rather than one
general grammar: a **dominant coordinate-list shape** ("Add N to the X[, Y and Z] characteristic(s)
of {selector} weapons equipped by this model[ by N]" — two amount-position variants,
`WeaponCharacteristicAdd`/`WeaponCharacteristicImproveWorsen`, since real corpus text uses both "Add
N to..." and "...by N" phrasing) splits a comma/and-joined characteristic list into N atomic
`WeaponCharacteristicEffect`s sharing one selector/verb/amount, never a single Effect holding more
than one characteristic; and a rarer **two-verb anaphora shape** ("...add 1 to the Strength
characteristic of melee weapons equipped by this model and improve the Armour Penetration
characteristic of those weapons by 1") where `WeaponCharacteristicAnaphoraContinuation` recognizes
the second, independently-verbed clause and infers its selector as identical to the first clause's
own, rather than re-extracting it.

The weapon-selector qualifier (the text between "of" and "weapons equipped by this model") is
resolved structurally, not via a phrase list: empty → `AllWeapons`, "melee"/"ranged" →
`WeaponClass`, anything else (a real corpus example: "Psychic weapons", "Lethal Hits weapons" —
ability-flag-qualified selectors out of scope for this classifier) → no selector at all, which fails
the whole match closed (zero Effects), per this classifier's existing "fail closed on the
unrecognized case" convention.

**`WeaponEffectStart`, a widened anchor scoped only to these two patterns**: every real corpus
weapon-characteristic mutation (re-verified directly against five named ground-truth examples -
Zealot, Chance for Glory, Brutal Raider, Euphoric Strikes ×2 - see
`openspec/changes/classify-weapon-characteristic-effects/tasks.md`'s task 1.1) states its actual
mutation clause immediately after a ", until the end of the phase," / ", until the end of the
turn," temporal-scope clause, itself preceded by an activation preamble not at a true sentence
start - so the plain `SentenceStart` anchor, applied unmodified, would reject every one of them.
`WeaponEffectStart` widens `SentenceStart`'s own `^`/`\.\s*` cases with a third, equally structural
case: immediately after that exact temporal-scope clause. `SentenceStart` itself, and every
existing Statline/InSv pattern, are untouched.

**Corpus review** (`classify-weapon-characteristic-effects` tasks.md task 6): a live run found 19
distinct weapon-characteristic Effect results (0 false negatives found via a targeted grep of the
Target-only bucket for any further "weapons equipped by this model" occurrence) — **all 19 manually
reviewed and confirmed correct**, zero bugs found. All 19 added to the checked-in baseline (below),
6 carrying a `note`: two document a real, confirmed extraction gap (a real "...and those weapons
have the [KEYWORD] ability" continuation this classifier has no `KeywordEffect` vocabulary for yet —
see `.claude/vnext-ideas.md`'s "`KeywordEffect`/`AbilityEffect`" idea), four document that their
trailing content is a Crusade-campaign resource grant, not a characteristic mutation at all and so
out of this classifier's vocabulary entirely, not a gap. The wider corpus grep (beyond the exact
"weapons equipped by this model" anchor phrase) surfaced real phrasing this change deliberately
does not implement — a real `Set`-verb-shaped weapon effect, a dice-valued amount ("D3"), two further
real weapon-selector shapes (ability-flag-qualified, whole-unit-scoped), and two more real weapon
characteristics (Weapon Skill/Ballistic Skill) — all recorded in `.claude/vnext-ideas.md` rather
than folded into this change's own scope; see tasks.md task 1.2's own finding for the full detail.
The checked-in baseline now tracks 62 entries (19 new weapon-characteristic entries added on top of
whatever it tracked immediately before this change), all unchanged on the next run.

**A related, real, NOT-yet-built idea surfaced by this same review**: several Effect results correctly
extract their `CharacteristicEffect` but the source text also states content
`RuleClassification` has no vocabulary for at all (a keyword grant/removal, another ability grant, a
recurring non-characteristic effect) — silently dropped, with no signal the classification is
incomplete. See `.claude/vnext-ideas.md`'s "Caveated rule-effect classifications" note.

**Verified-Classification Baseline** (`baseline-rule-effect-classifications`): a checked-in JSON
baseline (`src/ProbHammer.Web/Data/RuleEffectClassifications.json`, a sibling of `BsData/` — same
`<Content Remove>`/`COPY --link` Docker-bundling treatment, never nested inside `BsData/` since
`LocalDiskBsdataCatalogueSource.ListFileNames()` would otherwise try to parse it as a catalogue file)
records, per distinct normalized rule/ability Text, the `RuleClassification` a human has explicitly
verified as correct — so the report tool never has to reprint an already-verified, unchanged result
on every future run, while any genuine change to a verified result is always resurfaced. Seeded with
all 20 Effect results above; 5 (Blastajet Force Field, Leader-beast, Lesk's Heroes, Redoubtable
Machine Spirit, Scattershield) carry a `note` recording that their classification is verified correct
but known-incomplete (the "related idea" paragraph above). `widen-rule-effect-classification-coverage`
grew this to 36 entries: `IsCaveated` backfilled onto all 20 original entries (one, "Army:
Shivversplint," genuinely flipped to `true` — a real, previously-unnoticed caveat), plus 16 new
entries for the real Effect results its own pattern widenings newly caught (one, Master Artisan,
carries a `note` documenting a confirmed caveat-signal false negative rather than an incomplete-but-
correct Effect — see that change's own tasks.md and `.claude/vnext-ideas.md`).

```
RuleClassificationBaselineEntry(Text, Target, Effects, Names = null, IsCaveated = false,
                                 FullyHandled = false, Note = null)
                                       // Domain/Catalogue/RuleClassificationBaseline.cs - see the
                                       // record's own doc comment for Note/FullyHandled/Names/
                                       // IsCaveated/Classification (the [JsonIgnore] repackaging for
                                       // diffing) in full - it covers the same real examples and
                                       // rationale this block used to restate (12 invulnerable-save
                                       // items sharing one Text; Marshal's Household/Faith-Fuelled
                                       // Resolve's Restrictions paragraph; Army: Shivversplint's
                                       // caveat flip). See .claude/vnext-ideas.md for the related,
                                       // NOT-fixed-here schema-growth-detection gap.

RuleClassificationBaselineFile(Entries)   // see record's own doc comment

RuleClassificationBaseline            // see class's own doc comment
  Load(path) -> RuleClassificationBaseline   // see method's own doc comment
  TryGet(text, out entry) -> bool
  Upsert(entry)                       // see method's own doc comment
  Save(path)                          // see method's own doc comment
  Options                             // see field's own doc comment

RuleTarget's own [JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")]/[JsonDerivedType] pair
                                       // see RuleTarget's own doc comment; EffectVerb's matching
                                       // [JsonConverter(typeof(JsonStringEnumConverter))] - see its
                                       // own doc comment.

RuleClassificationDiff.Compare(baseline, current) -> RuleClassificationDiffResult
                                       // Domain/Catalogue/RuleClassificationDiff.cs - see the
                                       // class's own doc comment. A property present in both but
                                       // unequal -> Drift (always surfaced). A property absent from
                                       // the baseline (schema growth) -> compared against ITS OWN
                                       // default instead: equal -> silently treated as unchanged,
                                       // unequal -> NewInformation (surfaced once, not a request to
                                       // re-verify unrelated fields).
```

**Report tool integration**: a corpus text with a baseline entry (matched by
`RuleEffectClassifier.Normalize`d Text, same key both sides) is diverted entirely out of the
Effect/Target-only/default-only sections above into its own "Verified baseline" line — an unchanged
entry only contributes to a summary count, never reprinted; a `Drift`/`NewInformation` entry prints
under a "changed since verified" listing naming exactly which field(s) moved (`baseline=... ->
current=...`, each side a raw JSON fragment via `JsonNode.ToJsonString()`) plus the entry's own `Note`
if present. A text with no baseline entry is completely unaffected — the three original sections
still print exactly as before this change existed. Since
`widen-rule-effect-classification-coverage`'s own follow-on fix, a further "Caveated baseline entries
needing review" section lists every baselined text whose current `IsCaveated` is true and whose
baseline entry's `Note` is null AND `FullyHandled` is false — independent of
Unchanged/Drift/NewInformation, since a caveated-and-unchanged entry would otherwise collapse into the
summary count with nothing ever prompting a human to revisit it. A human reviewing this list has two
genuinely different verdicts available, not one: extend the classifier to capture real omitted content
(a keyword grant, a recurring effect — the original 5 caveated entries' shape, where the player
genuinely needs to read the ability), or mark the entry `FullyHandled: true` with a `note` explaining
that the leftover text names no game effect at all (an eligibility restriction, flavor text — Marshal's
Household's shape). `IsCaveated` never changes either way; `FullyHandled` is the separate, permanent
human verdict a future `/LivePlay` wiring will need to decide whether the player should ever be
prompted to re-read the ability. `Note`/`FullyHandled` are the only two fields this tool never
computes; setting either is what removes an entry from this review list.

`Describe` also appends `; CAVEATED` when `IsCaveated` is true
(`widen-rule-effect-classification-coverage`), the one addition needed to make that field manually
reviewable at all; the "Verified baseline" summary line and any printed "changed since verified" entry
also report a `[FULLY HANDLED]` marker/count from the baseline entry directly (never from
`RuleClassification`, which carries no such field). `--write-baseline` snapshots the CURRENT
freshly-computed classification onto every baseline entry whose Text still resolves in this run's
corpus (`Target`/`Effects`/`IsCaveated`/`Names` overwritten via `entry with { ... }` — `Names` refreshed
alongside the rest since it's corpus-derived, not a human judgment call; `Note`/`FullyHandled`
left untouched, since neither is something a fresh classification run could ever derive) and writes
the result back — refreshing an entry's tracked values needs no separate backfill path even for schema
growth, since `--write-baseline` always writes whatever `RuleClassification` computes right now,
defaulted field included. A genuinely new baseline entry must first be added to the checked-in JSON by
hand (at minimum its `text`) — `--write-baseline` only ever refreshes Texts already present as a key in
the loaded baseline, it never invents a new tracked entry on its own.

**A real gap found while seeding the baseline, not by any test**: BSData source text can carry a
literal embedded newline mid-sentence (confirmed: Redoubtable Machine Spirit's own JSON has `"...at
the end of your\nCommand phase..."`, no comma, a bare `\n`) that `Truncate`'s `ReplaceLineEndings(" ")`
collapses to a single space for **display only** — so a hand-typed baseline `text` value copied from
the report's own printed output, with an ordinary space where that newline was, silently fails to
match the real (un-displayed) dictionary key. Caught immediately by task 6.1's own verification (the
seeded entry stayed in the unbaselined "Effect results" section instead of collapsing to "unchanged"),
not a latent bug — but a real trap for hand-authoring any future baseline entry directly from console
output rather than from the corpus text itself.
