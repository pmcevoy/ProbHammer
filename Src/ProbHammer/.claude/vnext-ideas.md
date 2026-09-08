# vNext Ideas — Deferred Features

Ideas and follow-on work that came up during 11e development and were deliberately deferred
rather than built. Nothing here is scheduled — this is a parking lot to draw from when picking
the next `openspec` change. Update this file whenever a new idea gets deferred, and remove an
entry once it's been turned into a change (archived changes remain the historical record).

---

## `/LivePlay` — Attached Unit view

- **Per-ability Model/Unit scope marker.** `live-play-landscape-only` merged the Statline grid's
  separate Model Abilities and Unit Abilities columns into one stacked-list column, at the user's
  own explicit request — recalling that the two-column split existed only to distinguish
  model-scoped from unit-scoped abilities, and deciding that distinction doesn't need its own
  column. The user floated a future prefix/symbol on the ability name itself (mirroring the
  Enhancement `✦` prefix, `AbilityDisplayName` in `_UnitBlock.cshtml`) as the way to surface the
  distinction again if wanted. `Ability.Scope` and the view models' `ModelAbilities`/
  `UnitAbilities` split were deliberately left untouched by that change specifically so this stays
  easy to build later with no domain rework.
- **Core/Faction/Psychic ability source tagging** — distinguish where an ability comes from,
  not just its name/scope.
- **Per-`ModelLine` keywords.** Needed so a leader's personal keyword leaves the unit's effective
  keyword union when that specific model is removed as a casualty — currently keywords are only
  tracked at the component (`Unit.Datasheet.Keywords`) level.
- **Multi-profile-weapon "select one profile" disclaimer** — some weapons have multiple firing
  profiles the player picks between; the view doesn't currently flag this.
- **Ability-driven attack modifiers** — e.g. a unit-wide "+1 Attack to melee weapons" ability
  changing a total from A28 to A35. Not modeled at all yet. Expected to be a consumer of the
  "Ability-text interpretation pass" idea below, once that exists, rather than its own bespoke
  parsing.
- **Splitting `Keywords` into unit-wide-union vs. per-component, or adding `FactionKeywords`** —
  currently left as one unioned `IReadOnlySet<string>`.
- **Split `wwwroot/css/site.css` into a `/LivePlay`-only stylesheet.** Deferred by
  `archive-10e-pipeline`: the file's 10e-only, `/LivePlay`-only (`.live-play-page`-scoped),
  and shared base rules are interleaved with no clean physical boundary, and the archive
  change's own goal was safety (no risk to the one live page), not a redesign. `site.css`
  currently still ships several hundred lines of now-dead 10e selectors (harmless — they
  never match anything once `Index`/`ArmyView` markup is gone — but worth tidying up
  eventually). Extracting `/LivePlay`'s rules into their own file would let `_Layout.cshtml`
  stop loading dead CSS and make the stylesheet's scope match the app's actual scope
  (`/LivePlay`-only).

## Domain / data pipeline

- **Ability-text interpretation pass.** A distinct pass, run *after* `BsdataDatasheetMapper
  .BuildDatasheet` (not inside it), that classifies a specific ability's `Description` text
  against a small closed vocabulary of known fixed phrasings and folds the result back into the
  domain model. Explicitly **not** general ability-text-to-behavior parsing (that stays
  permanently out of scope per `domain-model-11e.md`'s Deliberate Omissions) — this is
  pattern-matching known, previously-catalogued sentences, not NLU. `resolve-known-ability-effects`
  shipped two narrow slices of this idea directly: `InvulnerableSaveCaveatClassifier` (resolves a
  footnoted InSv's linked ability text against four known templates) and `statline-flag-rules`
  (Shield Dome/Vexilla, matching one specific ability by exact Name+Text to derive a flagged
  Statline value for a resolved unit) — see `domain-model-11e.md`'s own sections for both. Should
  still resolve Model-vs-Unit scope for Enhancements specifically: today every Enhancement is
  displayed at Unit scope unconditionally (`resolve-enhancement-abilities` shipped Enhancement
  *resolution*, not scope classification), even though a real Enhancement's actual scope depends
  on its rules text and can genuinely be Model — this pass is where that distinction should get
  made instead of guessed at, per the same "no scope on Enhancement" acknowledgment.

  Discussion during `resolve-core-rule-abilities`' follow-up (2026-08-24) sharpened the shape of
  this considerably — noting it here so it isn't relitigated from scratch when this gets picked
  up:
  - **Not just Enhancements — every ability source is Model/Unit-blind today.**
    `BsdataDatasheetMapper.MapAbility` hardcodes `Scope = AbilityScope.Unit` unconditionally for
    every origin (Intrinsic, Enhancement, OptionalGrant, CoreRule); `AbilityScope.Model` is set
    nowhere in real-data code, only in the unused `Examples/Datasheets.cs` fixtures. Real text
    reliably signals the split (confirmed against real Black Templars rule text: `Martial Honour`
    — *"add 5 to **this model's** Objective Control"* — is Model; `Crusade of Wrath` — *"...models
    in **that unit**"* — is Unit) but nothing reads it yet.
  - **Architecture: a composable, user-toggleable rule pipeline, not a single fixed pass.**
    `statline-flag-rules` implements a small, fixed, closed-vocabulary version of this (a rule
    matches one exact ability, mutates a flagged Statline value, re-evaluated live as casualty
    state changes — see `domain-model-11e.md`). Still open beyond that seed: a genuinely
    open-ended, **player-toggleable** rule set (so a player could disable a specific rule's
    effect) covering a much wider vocabulary than InSv/OC — the "Ability-driven attack modifiers"
    idea above (e.g. folding a "+1 Attack to melee weapons" ability into weapon-aggregation
    totals) is the next natural driver for growing this vocabulary, not yet designed.

- **Characteristic-modification domain hardening: the general engine (still not built).**
  `introduce-characteristic-domain-model`, `unify-invulnerable-save-characteristic-view`, and
  `unify-objective-control-characteristic-view` (all archived) wired `CharacteristicValue`/
  `CharacteristicView` into every real consumer — `Statline.M`/`T`/`Sv`/`W`/`Ld`/`Oc`/`InSv` and
  `WeaponProfile.S`/`Ap`/`Bs`/`Ws` all now carry `OriginalValue`/`DerivedValue`/
  `ContributingAbilities` uniformly, and the old `StatlineFlag`/`StatlineFlagCharacteristic`
  side-channel is gone. What's described below is what's still missing: a real modification/
  legality *engine* handling multi-rule stacking order and a characteristic-type-wide clamp table —
  `StatlineFlagRule`/`AttachedUnitAggregator.ApplyStatlineFlagRules`'s own same-field-stacking order
  (when two *different* sources both touch one characteristic on the same unit) is still whatever
  order matches happen to be discovered in, not a rulebook-derived one.

  **A first attempt at the build-time half was built, then reverted (2026-09-07)** —
  `resolve-structured-characteristic-modifiers`, un-archived and removed from the tree; see
  `project_resolve_structured_characteristic_modifiers_change` memory for the full history. It
  resolved BSData's structured `BsModifier` data (1,169 real instances, corrected from an original
  717 estimate) into `Datasheet.StructuralModifierGrants`, applied by `AttachedUnitAggregator` only
  once the granting ability was confirmed present on a live unit — deliberately never baking a
  value into the shared, selection-blind `Datasheet` itself (see
  [[project_datasheet_selection_blind_catalog_boundary]]), *except* for a modifier judged
  "unconditional" by two signals: its own condition tree provably always true, or its granting
  entry not being a player-selectable `"type": "upgrade"` choice. Both signals turned out unreliable
  against real data, independently: the condition check recognized only Force-type gates, silently
  treating every other real condition shape (a specific other character present elsewhere in the
  army, a sibling relic selection, a live attachment state) as unconditional; and a real, capped,
  player-chosen squad slot (Adeptus Custodes' "Allarus Custodian (Vexilla & Misericordia)") is typed
  `"model"`, not `"upgrade"`, so its OC+1 baked into every model in the squad regardless of whether
  that model was actually taken. 42 of the 45 cases the "unconditional" path judged safe were
  confirmed wrong by cross-checking real BSData source text and NewRecruit's own rendering — not a
  scan/test gap (515+ automated tests and full-corpus scans all passed throughout), a genuine blind
  spot in what a scan can check versus a domain-informed manual read.

  **The reframed, two-phase approach shipped for tiers 1-2** —
  `classify-characteristic-modifier-caveats` (implemented, see `.claude/domain-model-11e.md`'s own
  "Characteristic-Modifier Caveats" section). It builds the "caveated, no `DerivedValue`" half only
  (populating `ContributingAbilities`, using the existing `IsCaveated` vocabulary), strictly
  presence-gated at the `AttachedUnitAggregator` roster layer with **no** "provably unconditional"
  exception of any kind — every candidate, regardless of its own granting entry's structural type,
  is presence-checked, which is exactly what structurally excludes the reverted attempt's second bug
  from recurring. It deliberately narrows scope versus the earlier attempt in three ways, each
  informed by a real-corpus finding during implementation: (1) only tier 1 (no condition) and tier 2
  (a condition scoped to the modifier's own granting entry) are classified — every wider condition
  shape (a sibling selection, live attachment state, a self-reference scoped broader than
  "self"/"parent") is left unclassified, never guessed as safe, which is what structurally excludes
  the reverted attempt's first bug; (2) the closed `Field` allowlist covers only the six Statline
  scalars (M/T/Sv/W/Ld/Oc) — InSv and every `WeaponProfile` characteristic are excluded (InSv has
  its own dedicated resolution path already; zero real corpus modifiers reachably target a
  `WeaponProfile` field at all — every occurrence lives inside an unread, Crusade-only
  `modifierGroups` block); (3) a genuine real-corpus overlap with the hand-authored
  `StatlineFlagRuleCatalogue` (Adeptus Custodes' "Vexilla," which carries both a hand-authored rule
  match and a classified structural candidate on the same wargear entry) confirmed the application
  step must skip a field already touched by an earlier rule, rather than risk regressing an
  already-resolved value back to merely caveated.

  **A first, standalone slice of the prose half also shipped** —
  `classify-rule-effects-from-text` (implemented, see `.claude/domain-model-11e.md`'s "Rule Effect
  Classification (Text-Only)" section, and the change's own proposal.md/design.md). A
  `RuleEffectClassifier` extracts a rule/ability's `RuleTarget` (`Self`/`AttachedUnit`/
  `KeywordRuleTarget`/`UnconditionalRuleTarget`) and zero or more unconditional `CharacteristicEffect`s
  (`Improve`/`Worsen`/`Set` one named Statline scalar by a fixed amount) from its Name+Text alone —
  anchored regex/template matching, the same rigor as `InvulnerableSaveCaveatClassifier`, proven
  against four ground-truth examples (Shield Dome, Vexilla, Templar Vows, Black Templars'
  "Faith-Fuelled Resolve"/Marshal's Household) plus a full live-corpus run (final numbers, after the
  six-fix live-review pass below: 3830 distinct real rule/ability texts, 20 with 1+ Effects - every
  one manually reviewed and confirmed correct - 618 with a Target broader than Self but no Effect,
  3192 all-default; the report groups by `RuleEffectClassifier.Normalize`d Text alone, not Name+Text,
  since `Classify` never reads its own `name` argument; see domain-model-11e.md's "Corpus-Wide
  Classification Reporting"). Deliberately standalone: no dependency on
  `Domain.Catalogue.Bsdata`/BSData JSON types at all — the user's own stated discomfort with adding
  more classification responsibility to `BsdataDatasheetMapper` (already the source of several real
  data-misunderstanding bugs) is the central reason this lives as an independent component rather
  than an extension of that mapper's existing ability-extraction walk. **Not yet wired to anything** —
  no `BsdataDatasheetMapper`/`AttachedUnitAggregator`/`/LivePlay` call site consumes a
  `RuleClassification` today; this change only proves the extraction works. Explicitly out of this
  slice's scope, all still real/confirmed remaining needs: conditional effects (a Condition concept,
  plus a player-assertable flag for the genuinely unknowable "Aura"-shaped subset, extending
  `IsBattleShocked`/`IsHalfStrengthOverride`'s existing live-state pattern); `WeaponProfile`-targeting
  effects (confirmed real in prose — a named-weapon Attacks buff, a melee-only WS debuff, an
  incoming-attack Damage halve); `Multiply`/`Divide` verbs (e.g. "halve the Damage characteristic of
  that attack"); evaluating a `KeywordRuleTarget`/`UnconditionalRuleTarget` predicate against an
  actual resolved roster; applying/executing any classified Effect anywhere; and any live/runtime LLM
  call — the offline, batch LLM-assisted discovery pass described below remains real future work,
  unstarted. One real, confirmed extraction gap found by the corpus run, fixed by
  `widen-rule-effect-classification-coverage` (2026-09-08): Marshal's Household's actual "+1 OC"
  shorthand phrasing wasn't recognized — only the "Add N to the X characteristic" phrasing was. A new
  `ShorthandCharacteristicPlus` pattern now recognizes "+N Code" (any of the six Statline scalar
  codes), anchored by `SentenceStart` like every other Effect pattern. A live corpus re-run after
  landing found this pattern also catches several more real occurrences beyond the one originally
  confirmed (e.g. "This model has +2 W.", "This unit has +1 OC.") — see that change's own tasks.md.

  **Six real gaps caught live during a sustained corpus-report review (2026-09-08, same day), all
  fixed** — none caught by the original ground-truth/negative-control unit tests (task 3.5) or the
  first mechanical corpus run (task 4.3), every one surfaced only once a person actually read the
  report's real output and pushed on something that looked off. In the order found: (1) every
  word-literal regex except `AllCapsKeywordPhrase` was case-sensitive, so real corpus capitalization
  drift (`"The bearer has a 4+ Invulnerable save."`, capital I) silently missed its `Set InSv` Effect
  - now case-insensitive, `AllCapsKeywordPhrase` deliberately excepted (capitalization is its actual
  signal there). (2) the corpus report originally grouped by `(Name, Text)`, artificially splitting one
  real classification result across several rows whenever different wargear names shared identical
  text (Astartes shield/Blizzard shield/Storm Shield/etc. all granting the identical sentence) -
  regrouped by Text alone. (3) `Normalize`'s own doc comment claimed it handled a U+00A0 NBSP quirk
  but it never actually did (a stale claim, not code) - surfaced by a stray garbled byte in the
  report's console output that turned out to be a real console-encoding bug too (fixed:
  `Console.OutputEncoding = Encoding.UTF8`) sitting on top of the missing NBSP normalization (also
  fixed). (4) `InvulnerableSaveGrant` matched an attack-type-restricted/split save ("...invulnerable
  save against ranged attacks...") as an unconditional flat grant - user-flagged directly from real
  output (Chaos Knights' "Ensorcelled Shield"/"Veil of Medrengard") - fixed with a negative lookahead.
  (5) the deepest one: both Effect patterns matched anywhere in arbitrarily long prose, including
  inside a comma-joined conditional preamble ("If it does, until the end of the phase, the bearer has
  a 2+ invulnerable save.") or a bulleted/headed "select one of the following" menu item (Moment
  Shackle's two alternatives; Combat Drugs'/Noospheric Transference's option lists) - both wrongly
  extracted as unconditional facts. Fixed with `SentenceStart`, a structural anchor requiring a match
  begin at the true start of its own sentence (a period, deliberately not a bare newline or bullet -
  both are confirmed real menu-item separators, not sentence boundaries) - the user's own explicit
  ask for a general fix over a narrower "deny specific trigger phrases" patch. (6) `AttachedUnitPhrase`
  recognized only "the bearer's unit," missing "models in this unit" (functionally the same claim) -
  user-flagged directly ("Astartes Banner" wrongly landed as `Self`) - widened after surveying ~30 real
  corpus occurrences with zero counter-examples. A live reminder that "the tests pass and the corpus
  scan ran clean" is not the same bar as "a person looked at the real output and it made sense" — see
  [[feedback_verify_bsdata_mapper_changes_against_real_export]]. Full detail and regression-test
  references for each of the six: `classify-rule-effects-from-text/tasks.md`'s task 4.3 notes.

  **Caveated rule-effect classifications - a real, confirmed, NOT-yet-built idea, surfaced by the same
  live review, revisited and given a validated mechanism in a follow-up session (2026-09-08, same
  day)**: several of the 20 real Effect results correctly extract their `CharacteristicEffect`
  but the source text also states additional content `RuleClassification` has no vocabulary for at
  all - silently dropped, with no signal the classification is incomplete. Confirmed real examples -
  **five, not the original four**, a fifth (Scattershield) found by re-walking all 20 results during
  the follow-up session and initially missed by the live-review pass that found the first four:
  Blastajet Force Field (`Set InSv 4` - text also states losing a keyword), Leader-beast (`Set InSv 4`
  - text also grants two keywords and a separate ability), Lesk's Heroes (`Improve Ld 1` - text also
  grants a re-roll ability), Redoubtable Machine Spirit (`Set InSv 5` - text also states a recurring
  per-Command-phase wound regen), Scattershield (`Set InSv 4` - text also states a per-attack Damage
  reduction). The user's own framing: the characteristic mutation fully resolves,
  but the *ability as a whole* is still "caveated" against something - the mirror image of
  `CharacteristicView.IsCaveated` (there, the number can't resolve but everything else is known; here,
  the number resolves but something else can't be represented). Shape for a future fix:
  `RuleClassification` gains something like an `IsCaveated`/`AdditionalContent` signal alongside
  `Target`/`Effects`, populated whenever recognized non-Effect content (a keyword grant/removal, an
  ability grant, a recurring non-characteristic effect) is detected trailing a matched Effect clause -
  not attempting to classify what the extra content IS, just that there is some.

  **Implemented by `widen-rule-effect-classification-coverage` (2026-09-08).** The validated structural
  signal described above shipped unchanged from its hand-designed shape: `RuleClassification` gained
  `IsCaveated` (default `false`), computed only when at least one Effect was extracted, from the end
  position of the last regex match that contributed to either Target or Effects, with the remaining
  text (trimmed of whitespace, then a single trailing period) checked for emptiness. A full corpus
  re-run after landing confirmed the hand-validated split on all 20 original Effect results, correctly
  including the two leading-restriction lookalikes (Sanctuary/Brute-Shield-shaped texts) staying clean.

  **A real, previously-unnoticed sixth caveat was found on re-run, not just the original five**: "Army:
  Shivversplint"'s Toughness buff for Emperor's Children units ("...from your Crusade army.") now
  computes caveated - the trailing "from your Crusade army" names a real Crusade-mode-only scope
  qualifier the classifier's `KeywordRuleTarget` doesn't capture, a genuine, correct new finding rather
  than a false positive.

  **A real false-positive-IN-SPIRIT was found and given a proper fix, not a one-off workaround**:
  Marshal's Household/Faith-Fuelled Resolve's own "+1 OC" shorthand correctly extracts, but its
  trailing "Restrictions: Your army can include BLACK TEMPLARS units, but it cannot include any
  ADEPTUS ASTARTES units..." paragraph trips `IsCaveated` too - even though, unlike the five genuine
  caveated examples above, this trailing text names no game effect at all. It's the same *kind* of
  content an already-excluded LEADING restriction ("Imperial Knights model only.") isn't caveated for
  - just trailing instead of leading, which the purely positional signal has no way to recognize.
  Confirmed NOT a one-off before deciding how to handle it: the identical "Restrictions:" shape recurs
  **12 times** across Space Marines chapter Detachments in the live clone (Black Templars, Space
  Wolves, Blood Angels, Dark Angels, etc.), though only this one entry currently produces an extracted
  Effect - the other 11 grant things outside today's vocabulary (attack-restricted saves, weapon-profile
  buffs, roll modifiers), so they never reach the caveat check at all yet.
  
  User-driven distinction that shaped the fix: `IsCaveated` staying `true` here is *correct* (there
  genuinely is text left over) - what was missing was a way to say "and a human confirmed that leftover
  text is not something the player ever needs to read," as opposed to the five genuine cases where the
  player DOES need to read the ability text. `RuleClassificationBaselineEntry` gained `FullyHandled`
  (bool, default `false`) for exactly this - a permanent human verdict, never computed, deliberately
  kept OFF `RuleClassification` itself (so it can never inherit the schema-growth DRIFT-vs-NEW-INFO
  problem described below - nothing computes it, so nothing needs to diff it) and never touched by
  `--write-baseline` (mirrors `Note`). The report's "needing review" listing now excludes an entry
  marked `FullyHandled` alongside one carrying a `Note`. Marshal's Household's own entry is now marked
  `fullyHandled: true` with a note documenting the 12-occurrence finding.
  
  **The general fix (teaching the caveat signal to recognize a trailing "Restrictions:" section
  structurally, mirroring the existing leading-restriction exclusion) was deliberately deferred, not
  built** - only one of the 12 occurrences is actually reachable through the caveat check today, so
  building classifier logic against a currently-N=1-active pattern would be exactly the kind of
  premature complexity this codebase avoids elsewhere. Revisit if/when a future Effect-pattern widening
  (a `WeaponProfile`-targeting Effect, an "advance/charge roll" verb, etc.) makes one of the other 11
  reachable too - at that point "Restrictions:" would be confirmed as a real, general, worth-automating
  structural marker rather than a single hand-noted exception.

  **A real false NEGATIVE was also found, on newly-widened possessive-phrasing text, not yet fixed**:
  Master Artisan ("Add 1 to the bearer's Wounds characteristic and add 1 to the Toughness
  characteristic of models in the bearer's unit.") extracts only its first clause's `Improve W 1` (the
  same already-parked "and add..." `SentenceStart` gap below), but the caveat signal ALSO fails to flag
  it - the `AttachedUnitPhrase` match that classifies its `AttachedUnit` Target happens to consume the
  tail end of the very same unextracted second clause, leaving nothing trailing to detect. Documented
  on its own baseline entry's `note` rather than fixed - fixing it properly means solving the same
  "and"-boundary ambiguity the parked gap below already describes, not a targeted caveat-signal patch.

  **A real gap in the baseline mechanism's own schema-growth handling was found while backfilling
  `IsCaveated` onto the existing 20 entries, confirmed but not fixed (out of scope for a
  `rule-effect-classification` change - this is a `rule-effect-classification-baseline` concern)**:
  `RuleClassificationBaselineEntry.Classification` always reconstructs a full `RuleClassification` via
  its own record constructor, which means a genuinely-new field like `IsCaveated` is ALWAYS present
  (at its default) in the serialized baseline node, never actually absent the way the "New
  Classification Fields Backfill" requirement's own `NewInformation` status assumes. Practical effect:
  every one of the six entries whose `IsCaveated` newly computed non-default value was reported as
  `[DRIFT]` rather than `[NEW INFO]` on the one transitional run right after this field was added -
  functionally harmless (the value still surfaced correctly, and one `--write-baseline` pass makes it
  permanently `Unchanged` going forward), but a real, confirmed limitation for any FUTURE field added
  to `RuleClassification` itself (as opposed to a field nested inside `RuleTarget`/
  `CharacteristicEffect`, which `RuleClassificationDiff`'s own doc comment's claim was never actually
  exercised against either). A real fix would need the baseline to track "which fields were present in
  the literally-stored JSON," not reconstruct a same-shape-as-current object and diff off its
  serialization - a bigger change to `RuleClassificationBaseline`'s own storage shape, not attempted
  here.

  **The "and add..." mid-sentence continuation widening remains deliberately parked** (see the false
  negative just above, which is this same gap's effect on the caveat signal): "Add 1 to the bearer's
  Wounds characteristic **and add** 1 to the Toughness characteristic of models in the bearer's unit."
  - the second clause has correct phrasing but still misses, since `SentenceStart` only accepts a
  match right after a period, and this one starts after "and" mid-sentence. Naively accepting "and"
  as an additional boundary would reopen exactly the bug `SentenceStart` was built to close: a real
  corpus text (Righteous Zeal - "While the bearer's unit is Righteous, add 2 to the Attacks
  characteristic **and add** 1 to the Damage characteristic...") has a second "and add" clause that
  is STILL conditional on "Righteous," and only fails to false-positive today by the accident that
  Attacks/Damage aren't yet in the `CharacteristicNames` allowlist. Any future "and"-boundary
  widening needs to distinguish "and joins two coordinate effects" from "and continues a still-
  conditional clause" structurally, not just accept any "and" - deliberately parked, not attempted.

  **A separate, unrelated misclassification trap for future `Improve`/`Worsen`-verb work**:
  Sanctified-Orators-style text ("Improve this model's Leadership characteristic **by 1 for every 5
  models in this unit**") is a *scaling* effect tied to unit size, not a flat amount - a future
  "Improve X characteristic by N" pattern that doesn't also detect and exclude a "for every N models"
  qualifier would silently misclassify this as a flat `Improve Ld 1` when it's actually much stronger
  for a large unit. Worth remembering when the Improve/Worsen verb work (see the rulebook arithmetic
  section below) eventually gets built.

  **Still not built**: computing an actual `DerivedValue` for any caveat this shipped work surfaces
  (the explicit "resolved" phase) — the caveat is display-only, "this characteristic is affected,"
  never "by how much, resulting in what." Also still not built: tier 3+ conditions (a sibling
  selection, live attachment state, or a different unit entirely), and prose classification for
  abilities with no structured `BsModifier` data behind them at all (confirmed real: Darnath
  Lysander's "Inspiring Commander," an army-wide OC-affecting ability with zero `BsModifier` data) —
  ties into the paused prose-classification schema described below. Cross-unit application (Lysander
  boosting a different unit elsewhere in the army) remains an explicit, deferred follow-on either
  way, since `AttachedUnitAggregator.Build` takes one `ICombatUnit` at a time with no roster-wide
  visibility today.

  **Tier 1-2-only scoping means this shipped work is, in practice, Enhancement/wargear-only, not
  general-ability-or-rule coverage — confirmed by live testing, 2026-09-07 same day, not a design
  intent.** Neither the classifier nor the presence-check actually discriminates by entry type or
  `Ability.Origin` (an Intrinsic ability, a CoreRule/ArmyRule-origin one, and an Enhancement all match
  identically once a name lines up) - the skew toward Enhancements/wargear in every real *working*
  example found (Auric Mantle, Brazen Form, Blood-forged Armour, Vexilla) is an emergent consequence
  of two things compounding, not a scoping choice: (1) a genuinely unconditional (tier-1) structural
  modifier is rare outside player-choice contexts by BSData's own modeling convention - an always-on
  boost doesn't need a modifier at all, since it can just be printed straight into the base stat text,
  so a modifier tends to exist specifically because the value is conditional on a choice/attachment/
  composition, which is exactly tier 3+; (2) even the rare tier-1 candidate on a non-wargear entry
  (Custodes' "Allarus Custodian (Vexilla & Misericordia)" model slot) still needs a same-named
  resolvable Ability to ever apply, which Enhancements/relics reliably carry and bare wargear-bundle/
  intrinsic-rule entries usually don't. **Practical upshot: reaching real general-ability/rule
  coverage (an Ancient's aura, a squad-wide Ld penalty gated on another model's presence, a CoreRule
  like Oath of Moment touching a characteristic) is the same ask as tier 3+ classification above, not
  a separate gap** - a decent share of real modifiers already carry cross-entity conditions (the
  reverted attempt's own "42 of 45" finding), so tier 3+ is plausibly where most of the "general
  ability" territory actually lives; unconfirmed until that classification tier is built and run
  against the corpus.

  **A caveated characteristic can only ever attribute ONE contributing ability, confirmed as a real
  gap (2026-09-07, same day, live user testing after `classify-characteristic-modifier-caveats`
  shipped)**. `ScalarCharacteristicView.Caveated`/`InvulnerableSaveCharacteristicView.Caveated` both
  take a single `Ability`, not a list (unlike their own `Resolved` factories, which already accept
  `IReadOnlyList<Ability>`) — so when two independently-classified candidates (or a candidate and an
  already-`StatlineFlagRule`-touched field) target the same characteristic on the same unit,
  `AttachedUnitAggregator`'s "skip if already touched" guard (load-bearing for the real Vexilla
  overlap - see above) means only the FIRST one applied is recorded; the second is silently dropped
  from that tile's own attribution, though it still renders normally in the unit's full ability list.
  Confirmed and pinned down by a domain test
  (`CharacteristicModifierApplicationTests.Two_present_candidates_targeting_the_same_characteristic_the_first_applied_wins_the_second_is_dropped`)
  built specifically to demonstrate this, since a full-corpus probe found **no real, naturally-
  reachable example** of two abilities simultaneously caveating the same field on one bearer -
  every near-miss found either (a) resolves to an unresolvable wargear-bundle wrapper entry with no
  matching Ability name (the same fail-closed pattern §1.3 already documented, unrelated to tier
  scoping), or (b) is a set of mutually-exclusive alternative wargear/mount choices (Combat Bike vs.
  Jump Pack vs. Terminator Armour, etc.) a real roster would only ever pick ONE of, not a genuinely
  simultaneous stack. Whether tier-3+ classification (still unbuilt, above) would surface a *real*
  simultaneous-stacking case is an open question, not yet checked in the corpus - a decent share of
  real modifiers do carry cross-entity conditions (the reverted attempt's own "42 of 45" finding
  above), so it's plausible but unconfirmed that some of those are stacking abilities gated on a
  sibling selection, not just alternatives.

  **Fix direction (not yet started)**: widen both `Caveated` factories to accept
  `IReadOnlyList<Ability>` (mirroring their own `Resolved` overloads), change
  `AttachedUnitAggregator`'s application step to ACCUMULATE additional caveat sources onto an
  already-caveated field instead of skipping (while still never touching a field that's already
  RESOLVED, non-caveated - the Vexilla-overlap guard must survive this change unchanged), and extend
  `_UnitBlock.cshtml`'s legend-line rendering to list every contributing ability under one marker
  instead of assuming exactly one. The pinning test above will need updating (from "second is
  dropped" to "both accumulate") once this ships.

  **Remaining gap on the retyped fields**: `Statline`/`WeaponProfile.S`/`Ap`/`Bs`/`Ws` now use
  `ScalarCharacteristicView` (a `CharacteristicValue`, which does support `DiceCharacteristicValue`/
  `SymbolicCharacteristicValue`) but every real construction call site still only ever constructs a
  plain `NumericCharacteristicValue` via the implicit `int` conversion — `CharacteristicResolutionAllowlist.cs`
  still documents the same real, unfixed instance (Ork Battlewagon "D6+6" `S` text skipped rather
  than parsed as `DiceCharacteristicValue`) as before; the domain shape to fix it now exists, the
  parsing to populate it doesn't yet.

  **The prose half has a drafted, paused classification approach**: a confidence-tiered schema
  (Tier 0 no signal → Tier 1 "something's modified" → Tier 2 "this specific characteristic" → Tier 3
  a real hand-written `StatlineFlagRuleCatalogue` entry, promoted only by a person), an LLM pass
  proposed only as a Tier 1/2 discovery accelerant feeding a human-reviewed table — deliberately
  paused before writing any prompt. Ties into `live-play-phase-turn-tracker`: an ability's extracted
  Phase/TurnOwnership becomes automatically evaluable against real page state once classification
  can extract it.

  **One unresolved complication, no answer yet**: the rulebook's "Ignore Modifiers" rule lets a
  player selectively discard some applied modifiers while keeping others — correct resolution isn't
  a pure function of a final delta, it needs each applied modifier tracked as a discrete, toggleable
  item, materially bigger than a clamp layer and not scoped anywhere.

  **The rulebook's own Improve/Worsen text** (verbatim):

  > Improving WS, BS, Sv and Ld: subtract the amount from the number before the `+` (WS 3+ improved
  > by 1 → 2+). Worsening WS, BS, Sv and Ld: add to it (WS 3+ worsened by 1 → 4+). Improving AP:
  > subtract (AP -1 improved by 1 → -2). Reducing/worsening AP: add, **to a maximum of 0** (AP -1
  > worsened by 1 → 0; AP 0 worsened by 1 → 0, stays 0). Improving/worsening any other characteristic
  > (no `+`/`-` symbol): plain add/subtract (S improved by 1 → +1).

  That's 3 distinct arithmetic behaviors — `RollThreshold` (WS/BS/Sv/Ld: inverted, since a lower
  number is an easier die-roll bar), `ArmourPenetration` (AP: inverted, worsen capped at 0), `Plain`
  (everything else) — not one behavior per named characteristic. Clamp bounds are an *orthogonal*
  axis (WS/BS share a bound while sharing `RollThreshold`'s arithmetic with Ld, which has its own
  bound), so they're per-characteristic lookup data, not part of the arithmetic type.

  **Authoritative clamp table** (user-supplied verbatim — "or better"/"or worse" are exclusive of
  the named threshold):

  > Characteristics of '-', '\*' and 'N/A' can never be modified. Rules that modify a model's WS
  > and/or BS characteristic modify the WS and/or BS characteristic of every weapon equipped by that
  > model. After all modifiers have been applied: M cannot be less than 1". T cannot be less than 1.
  > Sv cannot be 1+ or better. InSv cannot be 1+ or better. Ld cannot be 4+ (or better) or 9+ (or
  > worse). OC cannot be less than 0 or '-'. Range characteristics cannot be less than 1". A cannot
  > be less than 1. WS cannot be 1+ (or better) or 7+ (or worse). BS cannot be 1+ (or better) or 7+
  > (or worse). S cannot be less than 1. AP cannot be worse than 0. D cannot be less than 1.

  Worked out: `Sv`/`InSv` floor at 2, no ceiling; `Ld` ∈ `[5,8]`; `WS`/`BS` ∈ `[2,6]`; `Oc` floors at
  0, no ceiling (can never produce `-` — moot, since a symbolic value can never be modified at all);
  `AP` ceilings at 0 (worsening stops there), no floor; `M`/`T`/`A`/`S`/`D`/Range all floor at 1 (1"
  for the length-valued ones), no ceiling.

  **Where `Improve`/`Worsen` belongs**: NOT on `CharacteristicValue` itself (would make the value
  type responsible for computing its own mutation — the same mistake `ComputeDerivedValue` was
  reverted for on `CharacteristicView`, see
  [[feedback_avoid_premature_behavior_on_unconsumed_domain_types]]) and not on a mutator rule
  directly, but on the `RollThreshold`/`ArmourPenetration`/`Plain` "Kind" concept as pure
  sign-resolution: `Kind.ResolveDelta(Improve, amount) -> signed int`. A mutator rule states intent
  in ability-vocabulary terms ("Vexilla improves Oc by 1"); that resolves to a signed `Add` step
  before reaching the engine, which never sees "Improve"/"Worsen" at all. Proposed layering, not
  built:

  ```
  CharacteristicView          pure data - Value/IsCaveated/ContributingAbilities/OriginalValue,
                               computes nothing (unchanged from today)
        ▲ built by
  Modification Engine         groups modifiers by step-type, applies Set→Multiply→Add→Divide→
                               Subtract→round-up (the 6-step algorithm), clamps to the
                               characteristic's bound (the table above)
        ▲ consumes
  CharacteristicModifier      one per (ability, target characteristic) pair - an ability
                               touching 2 stats emits 2 of these; StepType + signed Amount +
                               SourceAbility
        ▲ produced by matching
  Mutator rule                StatlineFlagRule's successor - recognizes an ability by
                               name+text, emits its Modifier(s)
  ```

  **Ordering is simpler than feared**: the rulebook's algorithm only requires *grouping by
  step-type* and applying step-types in the fixed sequence — same-step-type contributors just
  combine (sum for Add/Subtract, product for Multiply/Divide; both commutative, no rule-vs-rule
  priority needed within one step-type). `Set` is the exception — a replace, not a combine, that
  short-circuits every later step for that characteristic.

  **Two design questions have real, worked answers, ready whenever a real caller needs them**:
  - *Set-vs-Set conflicts*: given two abilities both `Set`-type on the same characteristic (e.g.
    "Set InSv 4+" and "Set InSv 5+"), keep the *better* value and discard the other, then apply any
    remaining `Improve`/`Worsen` on top — this is the real invulnerable-save FAQ rule ("only the
    best applies") generalized to any characteristic. Needs a `Kind.Better(a, b)` comparison
    alongside `ResolveDelta`.
  - *Multi-characteristic abilities* (e.g. "Add 1 to Attacks of all melee weapons and 1 to
    Strength"): don't invent a generic `CharacteristicModifier.Target` spanning
    Statline/WeaponProfile — feed both `Statline` and the relevant `WeaponProfile`s in as *context*
    to whichever hand-written rule recognizes the ability, and let that rule mutate directly, the
    same self-contained way `StatlineFlagRule.Apply` already works. Keeps
    `StatlineFlagRuleCatalogue`'s "no general ability-text parsing engine" non-goal intact.
    `Apply`'s signature eventually widens to also carry `WeaponProfile`s when a real rule needs it.

  **Also still open**: a ranged-aura ability ("Improve OC for all units within 6\" of this model")
  can't be safely auto-resolved — no positional/board-state data exists in this domain (root
  `CLAUDE.md`'s Deliberate Omissions) — so a matching rule should emit a `Caveated` view, not
  attempt a `Resolved` computation. No concrete real BSData instance has been pinned down yet; find
  one before scoping this into a change, per
  [[feedback_verify_bsdata_mapper_changes_against_real_export]].

  **Next real step**: wire `CharacteristicValue` into `WeaponProfile.S` (smallest bounded starting
  point — already has a confirmed real bug driving it, the Ork Battlewagon dice-notation skip
  above) as a stress test before designing the engine further in the abstract. Expect friction:
  `CharacteristicValue` has no comparison/ordering operator yet, and every site doing raw int
  arithmetic against these fields needs to unwrap/repack rather than operate directly.

- **Detachment rule structural-modifier detection ("phase 2" of `display-army-header-and-
  detachment-rules`).** That change captures every Detachment's rule text verbatim and renders it
  army-wide in the `/LivePlay` header - deliberately not attempting to tie any rule to a specific
  unit. A live-clone corpus scan (2026-08-25, during that change's exploration) found this is
  possible for a real minority of the corpus: 22 of 280 Detachments (~8%) carry a BSData `modifiers`
  entry - a characteristic increment/decrement/multiply, or a Keywords `append` - directly on a
  specific existing unit's own profile, gated by the identical Detachment-selection condition
  (`selections`/`force`-scope/`childId`) as the Detachment's own visibility gate. Confirmed
  examples: Black Templars' `Marshal's Household` → Sword Brethren Squad's OC +1 (byte-for-byte
  matches a `Statline` characteristic); Adepta Sororitas' `Sanctified Orators` → 14 different units'
  Leadership improved; Chaos Space Marines' `Devotees of Destruction` / Agents of the Imperium's
  `Ordo Hereticus, Purgation Force` → a literal `Keywords` append on several units each. For this
  subset, the Detachment rule could render as an orphaned per-unit ability - Templar-Vows-style,
  same `AbilityOrigin`/attachment shape `resolve-core-rule-abilities` already built for chapter-wide
  Core rules - rather than only the army-wide header block. The detection signal (a Detachment's own
  force-selection-gate id also appearing in some datasheet's own `modifiers`, on a field other than
  `hidden`/`category`) is a natural fit for a permanent, explicit-only corpus-scan test, mirroring
  `InfoLinkTypeScanTests`/`WeaponKeywordScanTests`, so a newly-shipped Detachment with this shape
  gets caught rather than silently missed.

  Deliberately **not** assumed to generalize to "detachment rules are statline modifiers" - the
  other 258 Detachments (92%) either only gate visibility/availability (which Enhancements exist,
  which units the army can include) or are genuinely free-form, phase-conditional, army-state-
  tracking mechanics (Command-phase Doctrine switches, a mutable "Favoured Champions" unit, etc.)
  with no structural per-unit binding at all - much closer to the still-deferred "Gameplay bounded
  context" idea below than to a stat-mutation problem. A real example surfaced during discussion,
  worth keeping as illustrative of the partial-mechanization risk: Black Templars' `Close-Range
  Eradication` ("Ranged weapons equipped by Adeptus Astartes models from your army have the
  [ASSAULT] ability, and each time an attack made [with] such a weapon targets a unit within 12",
  add 1 to the Strength characteristic of that attack") is a plausible classifier target for the
  `[ASSAULT]` keyword grant half, but the "+1 Strength within 12"" half is a range-conditional
  combat-modifier with no structural BSData signal backing it at all - a rule can be *partially*
  mechanizable, and a future classifier needs to handle that split cleanly rather than assume
  all-or-nothing. Same permanently-deferred "no general ability-text-to-behavior parsing" boundary
  as the "Ability-text interpretation pass" idea above applies to the un-mechanizable half.

## Bigger picture

- **A full "Gameplay" bounded context** for live/mutable game state beyond casualty counts — other
  conditions that rules text sometimes references ("while this model is on the battlefield...",
  "gets Lethal Hits while within range of an objective marker"). Some ability text is permanently
  out of scope for auto-parsing because it needs state (positioning) the domain has no way to
  represent even with this context built — see `.claude/domain-model-11e.md`'s Deliberate
  Omissions. Battle-shock, per-unit Objective-control-goes-nothing display, and player-asserted
  turn/Command-phase tracking are no longer part of this deferred item —
  `half-strength-and-battleshock-indicators` and `live-play-phase-turn-tracker` implemented narrow,
  player-reported (never simulated) slices of all three, the latter persisted via `IPhaseTurnStore`
  in session. What's still missing here is the broader picture this bullet originally meant: the
  tracker only ever reflects what the player asserts (no prompting/simulation of the 2D6-vs-
  Leadership Battle-shock test or automatic phase advancement), and any positional/objective-state
  condition-tracking remains entirely absent.
