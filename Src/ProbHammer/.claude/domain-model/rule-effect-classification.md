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
RuleTarget                            // Domain/Catalogue/RuleTarget.cs - abstract, sealed
                                       // SelfRuleTarget/AttachedUnitRuleTarget/KeywordRuleTarget
                                       // (string Keyword)/UnconditionalRuleTarget subtypes, never
                                       // null - mirrors WeaponProfile's/CharacteristicValue's own
                                       // abstract-base/sealed-subtype convention. SelfRuleTarget is
                                       // the classifier's explicit default/fallback, not an absent
                                       // result. Named SelfRuleTarget/AttachedUnitRuleTarget (not
                                       // bare Self/AttachedUnit) to avoid colliding with the real
                                       // Domain.Roster.AttachedUnit class. Self and AttachedUnit are
                                       // deliberately separate cases, not merged into one - they map
                                       // onto Ability.Scope's Model/Unit distinction for an ordinary
                                       // ability, but DetachmentRule (a plain (Name, Text) record)
                                       // carries no Scope field to defer to, so this component must
                                       // classify both independently.

EffectVerb = Improve | Worsen | Set   // rulebook vocabulary, not pre-resolved signed arithmetic -
                                       // which sign each verb resolves to depends on the target
                                       // characteristic's own arithmetic family (a still-unbuilt
                                       // RollThreshold/ArmourPenetration/Plain "Kind" concept, see
                                       // vnext-ideas.md) - resolving that is out of scope here.

CharacteristicEffect                  // Domain/Catalogue/CharacteristicEffect.cs - abstract, sealed
                                       // ScalarCharacteristicEffect/InvulnerableSaveCharacteristicEffect
                                       // subtypes (resolve-invulnerable-save-effects; was a single
                                       // sealed record before that change) - mirrors RuleTarget's own
                                       // abstract-base/sealed-subtype convention, incl. an identical
                                       // [JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")]/
                                       // [JsonDerivedType] pair ("Scalar"/"InvulnerableSave"). Stays
                                       // inside RuleClassification's existing Effects list as the
                                       // element type varying per characteristic, not a new sibling
                                       // field - see that change's design.md D1.
ScalarCharacteristicEffect(string Characteristic, EffectVerb Verb, int Amount) : CharacteristicEffect
                                       // one atomic, unconditional mutation of exactly one named
                                       // Statline scalar characteristic by a fixed amount.
                                       // Characteristic is a plain string, the same convention
                                       // CharacteristicModifierCandidate already uses. Today's
                                       // original single-shape CharacteristicEffect record, renamed.
InvulnerableSaveCharacteristicEffect(InvulnerableSave Value) : CharacteristicEffect
                                       // an unconditional invulnerable-save grant, as a melee/ranged
                                       // pair - always Set-shaped (every real corpus grant on this
                                       // axis is a flat grant, never an Improve/Worsen - see
                                       // resolve-invulnerable-save-effects/design.md's D2), so no
                                       // separate Verb field. An attack type the text doesn't name
                                       // for this grant is 0 on that side (D3 - reuses
                                       // InvulnerableSave.None's own sentinel convention).

RuleClassification(RuleTarget Target, IReadOnlyList<CharacteristicEffect> Effects,
                    bool IsCaveated = false)
                                       // Effects defaults to empty, never null, via a
                                       // Target-only constructor overload. IsCaveated
                                       // (widen-rule-effect-classification-coverage) defaults false -
                                       // see RuleEffectClassifier.IsCaveated below for what it means
                                       // and how it's computed; this is the "boring default"
                                       // RuleClassificationDiff.DefaultClassification reads off this
                                       // type's own serialization for the baseline capability's
                                       // schema-growth backfill.

RuleEffectClassifier.Classify(string name, string text) -> RuleClassification
                                       // Domain/Catalogue/RuleEffectClassifier.cs - anchored/
                                       // template regex matching, same rigor as the now-retired
                                       // InvulnerableSaveCaveatClassifier (superseded by
                                       // unify-characteristic-effect-resolution's baseline-driven
                                       // Build-time resolution), but searches within
                                       // arbitrary-length prose rather than matching a whole string
                                       // (a Detachment/Core rule's own Text is rarely one sentence) -
                                       // an accepted brittleness trade-off (see design.md's Risks),
                                       // hardened by a live-review pass (below) into "first
                                       // sentence-anchored match wins" for the two Effect patterns,
                                       // not literally "first match anywhere."
RuleEffectClassifier.Normalize(string text) -> string
                                       // public (not private) so a caller comparing/grouping raw
                                       // corpus text - e.g. the corpus report tool, below - normalizes
                                       // the exact same way Classify itself does. Replaces a
                                       // typographic U+2019 apostrophe with plain ASCII (real corpus
                                       // data - Adeptus Custodes' Vexilla - carries both variants of
                                       // the identical "bearer's unit" sentence across different
                                       // entries) and a U+00A0 non-breaking space with a plain space
                                       // (confirmed widespread - 1,543 of ~7,660 raw ability texts
                                       // carry at least one). The NBSP half was originally claimed in
                                       // this method's own doc comment without actually being
                                       // implemented - caught live via a real console-encoding bug
                                       // (a raw 0xFF byte in report output) that led to checking the
                                       // claim against the actual code, not by any test.

// Every word-literal pattern (AttachedUnitPhrase, MarkupKeywordPhrase, InvulnerableSaveGrant,
// AddCharacteristic) matches case-insensitively - real corpus data confirmed the exact same "The
// bearer has a 4+ invulnerable/Invulnerable save." sentence with inconsistent capitalization across
// different wargear items (Storm Shield, Blizzard shield). AllCapsKeywordPhrase is the one
// deliberate exception - it stays case-SENSITIVE, since capitalization is the actual signal that
// distinguishes a real keyword from an ordinary capitalized word there, not noise to normalize past.
//
// Target recognizes: "the bearer's unit" OR "models in this unit" (functionally the same claim,
// widened after surveying ~30 real corpus occurrences of the latter with zero counter-examples) ->
// AttachedUnitRuleTarget; a markup-wrapped small-caps keyword ("**^^Adeptus Astartes^^** units",
// GW's own rules-text convention, see RuleTextEmphasisRenderer) or a literal ALL-CAPS run ("SWORD
// BRETHREN SQUAD units") followed by "units" -> KeywordRuleTarget; nothing recognized -> SelfRuleTarget
// (the explicit default, never absent).
//
// Effects recognizes: "has a {N}+ invulnerable save" -> a uniform Set InSv (N, N), EXCLUDING a match
// immediately followed by "against" (any restriction axis, e.g. Psychic Attacks - see below for the
// one axis this IS recognized on); a melee/ranged-attack-type-restricted grant - one-sided (e.g.
// "...invulnerable save against ranged attacks") or two-sided in one sentence (e.g. "...against
// ranged attacks, and a 5+ invulnerable save against melee attacks", the second clause's verb
// elided) - -> Set InSv with the stated value(s), the untargeted side (if one-sided) 0
// (resolve-invulnerable-save-effects: InvulnerableSaveRangedRestricted/
// InvulnerableSaveMeleeRestricted, tried before the uniform pattern, which falls back only when
// neither restricted pattern matched - the uniform pattern's own "against"-exclusion is what then
// correctly extracts nothing for a restriction on any OTHER axis, e.g. Psychic Attacks, per that
// requirement's own "scoped to the melee/ranged attack-type axis only" text). A cheap D5 pre-filter
// gate (RuleEffectClassifier.MayStateInvulnerableSave, requiring the literal substring "invulnerable
// save") runs ahead of this whole pattern family - a strict, provably-safe over-approximation used
// both internally and by the corpus report tool's own default-only bucketing (below); "Add {N} to the
// [X's] {Characteristic} characteristic" (a small closed name-lookup table: Movement/Move/Toughness/
// Save/Wounds/Leadership/Objective Control - "Move" and an optional intervening possessive noun both
// added by widen-rule-effect-classification-coverage, see below) -> Improve {code} N; a shorthand
// "+{N} {Code}" grant (e.g. "+1 OC") naming one of the six Statline scalar codes directly, also
// -> Improve {code} N (ShorthandCharacteristicPlus, same change). Every Effect pattern is
// additionally anchored by SentenceStart (private const, a lookbehind requiring the match begin at
// start-of-text or immediately after a period+whitespace - deliberately NOT a bare newline or bullet
// marker) - without it, a match anywhere in the conditional preamble of a longer sentence ("If it
// does, until the end of the phase, the bearer has a 2+ invulnerable save.") or inside a
// bulleted/headed "select one of the following" menu item (Moment Shackle's two alternatives; Combat
// Drugs'/Noospheric Transference's option lists) wrongly extracted as an unconditional fact - a
// bullet/newline reads exactly like a fresh sentence start but is confirmed, in every real example
// checked, to also separate mutually-exclusive menu alternatives, unlike a period. This SentenceStart
// anchor - a structural "is this clause actually at its sentence's true start" signal, not a growing
// denylist of trigger phrases ("if it does"/"while X"/"each time X") - was the fix the user explicitly
// asked for over the narrower alternative. ShorthandCharacteristicPlus's own subject-run character
// class deliberately excludes a comma (mirroring InvulnerableSaveGrant's) for the identical reason -
// an early draft that allowed one let the subject span an entire comma-joined conditional preamble,
// defeating this same anchor; caught by that pattern's own negative test before the corpus run.
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
                                       // Domain/Catalogue/RuleClassificationBaseline.cs - Note is a
                                       // free-text, optional known-incomplete-gap explanation,
                                       // mirroring AllowlistEntry<T>.Description's own hand-authored
                                       // convention elsewhere. Classification (a computed, [JsonIgnore]
                                       // property) is Text/IsCaveated's RuleTarget+Effects repackaged
                                       // as a real RuleClassification for diffing against a fresh run -
                                       // Names, FullyHandled and Note are all deliberately excluded
                                       // from that repackaging (see each's own entry). IsCaveated
                                       // added by widen-rule-effect-classification-coverage - see that
                                       // change's own tasks.md for why it had to be added here at all
                                       // (without it, --write-baseline could never persist the field,
                                       // so every run would re-surface the same "changed since
                                       // verified" entries forever) and .claude/vnext-ideas.md for a
                                       // related, confirmed, NOT-fixed-here gap in how schema growth is
                                       // detected for a brand-new top-level RuleClassification field
                                       // specifically.
                                       //
                                       // FullyHandled (same change, added slightly later, same day)
                                       // answers a DIFFERENT question than IsCaveated: IsCaveated is a
                                       // purely mechanical, text-only fact ("is there text left over
                                       // after the last match") with no way to distinguish real omitted
                                       // game content (the original 5 caveated entries' shape - a
                                       // keyword grant, a recurring effect) from content the
                                       // classifier's vocabulary was never meant to cover at all (an
                                       // army-composition eligibility restriction). Confirmed real via
                                       // Marshal's Household/Faith-Fuelled Resolve's own "Restrictions:"
                                       // trailing paragraph - and confirmed NOT a one-off: the identical
                                       // shape recurs 12 times across Space Marines chapter Detachments
                                       // in the live clone, though only this one entry currently
                                       // produces an extracted Effect. FullyHandled: true records a
                                       // human's confirmation that nothing further needs reading despite
                                       // IsCaveated staying true forever - the signal a future `/LivePlay`
                                       // wiring of a classified Effect will actually need to decide
                                       // whether to prompt the player to re-read the ability. Deliberately
                                       // NOT a field on RuleClassification itself, for the identical
                                       // reason IsCaveated's own schema-growth problem exists: nothing
                                       // ever freshly computes FullyHandled, so keeping it off
                                       // RuleClassification means it never enters RuleClassificationDiff's
                                       // comparison at all - no diffing machinery needed for a field
                                       // nothing could ever "drift" on. Never touched by
                                       // --write-baseline's refresh loop, mirroring Note exactly.
                                       //
                                       // Names (same change, user-requested) is a purely cosmetic
                                       // findability aid, never part of an entry's identity - Text
                                       // remains the sole key, and two entries are never distinguished
                                       // by Names alone. Confirmed real usability gap: locating a
                                       // specific entry in the checked-in JSON meant searching for a
                                       // snippet of its (sometimes long, sometimes near-duplicate) Text
                                       // - "Army: Shivversplint" was hard to find purely from its own
                                       // classified text. Records every distinct Name currently seen
                                       // carrying that Text (real example: 12 differently-named
                                       // invulnerable-save items sharing one identical sentence, all
                                       // recorded on the one tracked entry). Unlike FullyHandled/Note,
                                       // Names IS refreshed by --write-baseline (it's a corpus-derived
                                       // fact, not a human judgment call) - still excluded from
                                       // RuleClassification/RuleClassificationDiff for the identical
                                       // "nothing to diff" reason FullyHandled is.

RuleClassificationBaselineFile(Entries)   // the root JSON shape - a flat list, Text-indexed only
                                       // in-memory, not in the file itself

RuleClassificationBaseline            // Text-keyed load/query/write wrapper, mirrors RuleGlossary's
                                       // own "load once, query by key" shape
  Load(path) -> RuleClassificationBaseline   // a missing file loads as empty, never throws
  TryGet(text, out entry) -> bool
  Upsert(entry)                       // replaces the tracked entry for its own Text
  Save(path)                          // writes every tracked entry back, ordered by Text for a
                                       // stable diff
  Options                             // shared JsonSerializerOptions - camelCase properties,
                                       // indented, and JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                                       // (this file is trusted checked-in data, not
                                       // attacker-controlled HTML, so the default encoder's
                                       // conservative '/+-style escaping of an apostrophe/
                                       // "+" only hurts the diff's own readability here)

RuleTarget's own [JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")]/[JsonDerivedType] pair
                                       // added directly on the abstract type (mirrors
                                       // StoredArmyImport's own polymorphic-record convention) so the
                                       // baseline JSON's "target": { "kind": "AttachedUnit" } shape
                                       // needs no separate translation layer as RuleTarget grows
                                       // subtypes. EffectVerb gained a matching
                                       // [JsonConverter(typeof(JsonStringEnumConverter))] so it always
                                       // serializes as its own member name ("Improve") regardless of
                                       // caller-supplied options.

RuleClassificationDiff.Compare(baseline, current) -> RuleClassificationDiffResult
                                       // Domain/Catalogue/RuleClassificationDiff.cs - a structural
                                       // JSON diff between two serialized RuleClassification
                                       // snapshots (baseline.Options), not a hand-written
                                       // field-by-field comparator - deliberately sidesteps
                                       // IReadOnlyList<T>'s own default-record reference-equality
                                       // gotcha (Effects would never compare equal via `==`) for
                                       // free, and needs no matching change when RuleTarget/
                                       // CharacteristicEffect grow a field, only that field's own
                                       // "boring default" - read directly off a single
                                       // `new RuleClassification(new SelfRuleTarget())` instance's
                                       // own serialization rather than a separate per-field table.
                                       // A property present in both but unequal -> Drift (always
                                       // surfaced). A property absent from the baseline (schema
                                       // growth, a field added since that entry was verified) ->
                                       // compared against ITS OWN default instead: equal -> silently
                                       // treated as unchanged, unequal -> NewInformation (surfaced
                                       // once, not a request to re-verify unrelated fields).
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
