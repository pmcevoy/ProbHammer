## 1. Ground-Truth Verification (resolves design.md's Open Question before writing templates)

- [x] 1.1 Confirm the exact real text of the Black Templars Detachment rule referred to as
      "Marshal's Household" against the live BSData clone (`C:\Users\Pete\wh40k-11e`) or NewRecruit —
      the user's own paraphrase ("Friendly SWORD BRETHREN SQUAD units have +1 OC") is not yet
      confirmed verbatim. Record the exact wording found.
      **Found:** the rule text lives on a nested rule named "Faith-Fuelled Resolve" inside the
      "Marshal's Household" Detachment entry (`Imperium - Space Marines.json`,
      `sharedSelectionEntryGroups[Detachment].selectionEntries["Marshal's Household"].rules[0]`):
      `"Friendly SWORD BRETHREN SQUAD units have +1 OC.\n\n\nRestrictions: Your army can include
      BLACK TEMPLARS units, but it cannot include any ADEPTUS ASTARTES units drawn from any other
      Chapter."` — confirms the paraphrase's first sentence verbatim; the trailing Restrictions
      paragraph is not itself Target/Effect language.
- [x] 1.2 Confirm Shield Dome's, Vexilla's, and Templar Vows' own text exactly as already quoted in
      this change's proposal.md/design.md against the live clone — these were taken from earlier
      exploration and should be spot-checked, not assumed, before being hard-coded into templates.
      **Found:** Shield Dome (`Imperium - Space Marines.json`): `"The bearer has a 5+ invulnerable
      save."` — matches exactly. Vexilla (`Imperium - Adeptus Custodes.json`): `"Add 1 to the
      Objective Control characteristic of models in the bearer's unit."` — matches, but the corpus
      has TWO real variants of this exact sentence: one with a plain ASCII apostrophe (`'`) and one
      with a typographic U+2019 apostrophe (`'`) in "bearer's" — the classifier must normalize this
      the same way `InvulnerableSaveCaveatClassifier` normalizes a U+00A0 NBSP quirk. Templar Vows
      (`Library - Astartes Heresy Legends.json`, resolved via the Black Templars file's own infoLink
      chain): contains `"...select one of the following Vows to be active for
      **^^Adeptus Astartes^^** units from your army."` — confirms the keyword-target paraphrase; the
      rest of the (long) description names per-vow effects nested under sub-headings, not a single
      direct characteristic mutation, so zero Effects is the correct extraction per the spec's own
      scenario.

## 2. Core Types

- [x] 2.1 Add `RuleTarget` (abstract, sealed subtypes: `Self`, `AttachedUnit`,
      `KeywordRuleTarget(string Keyword)`, `UnconditionalRuleTarget`) — no namespace/assembly
      dependency on `Domain.Catalogue.Bsdata`.
      **Named `SelfRuleTarget`/`AttachedUnitRuleTarget`** (not bare `Self`/`AttachedUnit`) to avoid
      colliding with the real `Domain.Roster.AttachedUnit` class and to match `KeywordRuleTarget`/
      `UnconditionalRuleTarget`'s own naming convention.
- [x] 2.2 Add `EffectVerb` (`Improve` | `Worsen` | `Set`).
- [x] 2.3 Add `CharacteristicEffect(string Characteristic, EffectVerb Verb, int Amount)`.
- [x] 2.4 Add `RuleClassification(RuleTarget Target, IReadOnlyList<CharacteristicEffect> Effects)`.
- [x] 2.5 Unit tests: `RuleTarget` has no null/absent representation (a compile-time property of the
      abstract-base-plus-sealed-subtypes shape, confirmed by a quick test attempting to construct
      each subtype); `RuleClassification.Effects` defaults to an empty (not null) list.

## 3. Classifier

- [x] 3.1 Add the classifier component (name TBD during implementation — e.g.
      `RuleEffectClassifier`) with a single `Classify(string name, string text) -> RuleClassification`
      entry point, built via anchored regex/template matching in the same style as
      `InvulnerableSaveCaveatClassifier`.
- [x] 3.2 Implement Target classification: recognize "the bearer" (no further qualification) →
      `Self`; "the bearer's unit"/"models in the bearer's unit" → `AttachedUnit`; a named keyword
      qualifying "units" (e.g. "ADEPTUS ASTARTES units", "SWORD BRETHREN SQUAD units") →
      `KeywordRuleTarget`. Default to `Self` when nothing recognized.
      **Widened post-task-4.3** (live-review finding, see that task's own notes): also recognizes
      "models in this unit" as equivalent to "models in the bearer's unit" — surveyed ~30 real corpus
      occurrences with zero counter-examples before widening.
- [x] 3.3 Implement Effect extraction: recognize a `Set`-shaped grant (Shield Dome: "has a {N}+
      invulnerable save") and an `Improve`-shaped addition (Vexilla: "Add {N} to the {Characteristic}
      characteristic..."), each producing one `CharacteristicEffect`. No condition/multi-verb
      handling — out of scope per design.md's Non-Goals.
      **Hardened post-task-4.3** (live-review findings, see that task's own notes for the full list):
      case-insensitive matching; excludes an attack-type-restricted/split invulnerable save (conditional
      on the incoming attack, not unconditional); anchored to the true start of its own sentence
      (`SentenceStart`) so a match embedded in a conditional preamble or a "select one of the following"
      menu item no longer extracts as an unconditional fact.
- [x] 3.4 Confirm unrecognized text fails closed: arbitrary text neither matching a Target nor an
      Effect pattern classifies to `Self` + empty Effects, never throws.
- [x] 3.5 Unit tests: the four confirmed ground-truth examples (post-task-1 verification) each
      classify to their expected `RuleClassification` exactly; a handful of clear negative-control
      texts (plain descriptive ability text with no target/effect language at all) classify to
      `Self` + empty Effects.

## 4. Corpus-Wide Reporting Tool

- [x] 4.1 Decide the tool's concrete form (new console app project vs. a runnable command in an
      existing project) — behaviorally it only needs to satisfy the "Corpus-Wide Classification
      Reporting" spec requirement.
      **Chose:** a new console app project, `tools/RuleEffectClassificationReport/` (added to
      `ProbHammer.sln` under a "tools" solution folder), referencing `ProbHammer.Core` only — not the
      test project, so no `[Fact(Explicit = true)]` dependency.
- [x] 4.2 Implement it: read a supplied set of real rule/ability Name+Text pairs from the live BSData
      corpus clone (same source convention as this project's existing `CorpusScan` tests), run
      `Classify` against each, and report which produced a non-default result (Target broader than
      `Self`, or 1+ Effects) versus which classified to the all-default result.
      Walks the same closure/glossary machinery `BracketTokenResolutionScanTests` does — local +
      shared rules, every locally-built `Datasheet.Abilities` entry, plus (since Shield Dome/Vexilla
      are OptionalGrant-origin and NOT in the always-enumerated `Abilities` list)
      `Datasheet.OptionalAbilityNames`/`TryResolveAbility`, plus every resolved Detachment's own rule
      text via `BsdataNameResolver.ResolveDetachmentEntries`/`DetachmentRuleTextExtractor` (needed for
      Marshal's Household/Faith-Fuelled Resolve, which is Detachment-nested text, not a Datasheet
      ability).
- [x] 4.3 Run it against the live clone and manually inspect the output — confirm the four
      ground-truth examples appear among the non-default results with the expected classification,
      and spot-check a sample of the default-only results to confirm none of them look like an
      obvious, cheap-to-recognize miss (informational only — no requirement to fix a miss found here
      as part of this change, see design.md's Risks/Trade-offs).
      **Ran against the live clone** (45 catalogue files): all four ground-truth examples appear
      with the expected classification — `Shield Dome -> Target=Self; Effects=[Set InSv 5]`,
      `Vexilla -> Target=AttachedUnit; Effects=[Improve Oc 1]`,
      `Templar Vows -> Target=Keyword("ADEPTUS ASTARTES"); Effects=[no effects]`,
      `Faith-Fuelled Resolve -> Target=Keyword("SWORD BRETHREN SQUAD"); Effects=[no effects]`.
      The AttachedUnit-Target bucket is large (most Enhancement/relic ability text genuinely contains
      the literal phrase "the bearer's unit", confirmed against a real example — Adept of the Codex —
      not a false positive). One informational finding, not fixed here (out of this change's
      Effect-pattern scope per design.md's task 3.3): Marshal's Household's real "+1 OC" shorthand
      phrasing isn't recognized as an Effect — only the "Add N to the X characteristic" phrasing is.

      **A sustained live-review pass (2026-09-08, same day) by the user against the tool's actual
      output found six further real, confirmed defects — none caught by the unit tests or the first
      mechanical corpus run — each fixed in sequence. Recorded here in full since this is the change's
      real completion state, not the state task 4.3 first landed in:**

      1. **Report grouped by `(Name, Text)`**, so wargear items sharing byte-identical text under
         different names (12 differently-named items all granting "The bearer has a 4+ invulnerable
         save.") showed up as separate rows — misleading, not just noisy, since `Classify` never reads
         its own `name` parameter. Regrouped by `RuleEffectClassifier.Normalize`d Text alone (Normalize
         made `public` for this), with every distinct Name seen for a Text reported as data on the row.
      2. **Every word-literal regex except `AllCapsKeywordPhrase` was case-sensitive** — a real
         capitalization variant of the invulnerable-save sentence ("...4+ Invulnerable save.", capital
         I, on Storm Shield/Blizzard shield) silently failed to extract its `Set InSv` Effect. Made
         case-insensitive; `AllCapsKeywordPhrase` deliberately excluded (capitalization is its actual
         signal there).
      3. **`RuleEffectClassifier.Normalize` claimed in its own doc comment to normalize a U+00A0 NBSP
         quirk but never actually did** — a real console-encoding bug (the report tool wrote a raw
         `0xFF` byte instead of erroring) surfaced this while investigating a stray character the user
         spotted in the output. Fixed both: `Normalize` now genuinely replaces NBSP with a plain space,
         and the report tool forces `Console.OutputEncoding = Encoding.UTF8`.
      4. **`InvulnerableSaveGrant` matched an attack-type-restricted/split save** ("...invulnerable save
         against ranged attacks...") as if it were an unconditional flat grant — user-flagged directly
         from real output (Chaos Knights' "Ensorcelled Shield"/"Veil of Medrengard"). Fixed with a
         negative lookahead excluding a match followed by "against".
      5. **The deepest fix**: both Effect patterns matched anywhere in arbitrarily long prose, including
         inside a comma-joined conditional preamble ("If it does, until the end of the phase, the
         bearer has a 2+ invulnerable save.") or a bulleted/headed "select one of the following" menu
         item (Moment Shackle's two alternatives; Combat Drugs'/Noospheric Transference's numbered
         option lists) — both wrongly extracted as unconditional facts. Added `SentenceStart`, a
         structural anchor requiring a match begin at the true start of its own sentence (start-of-text
         or immediately after a period) — deliberately NOT after a bare newline or bullet marker, since
         those are confirmed real menu-item separators, not sentence boundaries. This is the general fix
         the user asked for over the narrower "deny specific trigger phrases" alternative.
      6. **`AttachedUnitPhrase` missed "models in this unit"**, recognizing only "the bearer's unit" —
         user-flagged directly ("Astartes Banner" wrongly landed as `Self`). Surveyed ~30 real corpus
         occurrences of "models in this unit" and found zero counter-examples, so widened the pattern.

      **Final numbers after all six fixes, report restructured into three sections** (Effect / Target-only
      / Default-only — see task 4.2's own note below on why): 3830 distinct texts, **20 Effect results,
      manually reviewed and confirmed fully correct by the user** (the earlier fail-closed default of
      "smaller correct set over larger uncertain set" was explicit user guidance during this pass), 618
      Target-only, 3192 default-only. All four ground-truth examples unchanged. See
      `RuleEffectClassifierTests` for the regression coverage added for each of the six fixes above.

      **A seventh, real finding was captured but deliberately NOT fixed as part of this change** — see
      `.claude/vnext-ideas.md`'s new "Caveated rule-effect classifications" note: several of the 20
      Effect results (Blastajet Force Field, Leader-beast, Lesk's Heroes, Redoubtable Machine Spirit)
      correctly extract their `CharacteristicEffect`, but the source text also states additional
      content (a keyword grant/removal, another ability grant, a recurring non-characteristic effect)
      that `RuleClassification` has no vocabulary to represent at all — silently dropped, with no signal
      that the classification is incomplete. A real design idea (mirroring `CharacteristicView`'s own
      `IsCaveated`), explicitly out of scope for this change, flagged for a follow-up.

      **Also confirmed during this pass**: the report tool's own `nonDefault` filter was briefly
      changed by the user (to `Effects.Count > 0` only, for focused Effect-only review) and this
      silently dropped the Target-only bucket from the printed output entirely (results were still
      counted in the header but never shown) — fixed by splitting into the three sections named above,
      preserving both the user's focused view and full spec-required coverage.

## 5. Docs

- [x] 5.1 Update `.claude/vnext-ideas.md`'s "Characteristic-modification domain hardening" entry to
      record this change's shipped scope (Target/Effect text classification, standalone from
      `BsdataDatasheetMapper`) and keep the still-deferred items (conditional effects,
      `WeaponProfile` effects, `Multiply`/`Divide`, roster-wide predicate evaluation, LLM-assisted
      discovery, live wiring) listed as explicitly remaining.
      Also added a new "Rule Effect Classification (Text-Only)" section to
      `.claude/domain-model-11e.md` (per root `CLAUDE.md`'s "Documentation Maintenance" rule) and
      cross-referenced it from vnext-ideas.md instead of leaving a dangling TODO.
