## Context

See proposal.md — Why. Two existing classification mechanisms already exist in this codebase and
both inform this one without being reused directly: `classify-characteristic-modifier-caveats`'s
structural `BsModifier` classifier (BSData-JSON-dependent, lives inside `BsdataDatasheetMapper`,
correctly scoped to structural data — deliberately not extended here), and
`InvulnerableSaveCaveatClassifier` (`Domain.Catalogue`, already text-only: anchored, exact-match
regex templates against a fixed set of known phrasings, already proven as a pattern in this exact
codebase — this change generalizes that same style to a wider vocabulary, not a new technique).

`Datasheet` is a selection-blind catalog (an established project rule — see the codebase's own
`.claude/domain-model-11e.md`); this classifier's output is a further, independent instance of that
same "declaration is safe at any time; application needs a resolved roster" split, extended here to
cover *what a rule's text says*, never *whether/to whom it currently applies*.

## Goals / Non-Goals

**Goals:**
- Prove that a rule/ability's Target and unconditional Effects can be reliably extracted from its
  Name+Text alone, decoupled from any catalogue JSON shape.
- Prove it specifically against four real, already-confirmed ground-truth examples: Shield Dome,
  Vexilla, Templar Vows, and a Black Templars Detachment rule (Marshal's Household).
- Provide a way to run this classifier against the live BSData corpus and inspect results directly.

**Non-Goals (deliberately deferred, not eliminated — real, confirmed future needs):**
- **Conditional effects.** All four ground-truth examples are unconditional. A future change adds a
  Condition concept, and — for the genuinely unknowable subset ("Aura"-shaped: e.g. "within 6\"")
  — a player-assertable flag extending the same live-state pattern `IsBattleShocked`/
  `IsHalfStrengthOverride` already establish.
- **`WeaponProfile`-targeting Effects.** Confirmed real in prose (a named-weapon Attacks buff, a
  melee-weapons-only WS debuff, an incoming-attack Damage halve) — unlike the structural classifier's
  own `WeaponProfile` exclusion, which is justified by zero reachable structured occurrences, this
  exclusion is scope discipline, not an absence-of-need finding. None of the four ground-truth
  examples need it.
- **`Multiply`/`Divide` verbs** (needed for real prose like "halve the Damage characteristic of that
  attack" — expressed as `Divide` by `2`, not a dedicated "Halve" verb, to stay general rather than
  add one verb per multiplier). Same "confirmed, deferred" framing as above.
- **Evaluating a `KeywordRuleTarget`/`UnconditionalRuleTarget` predicate against an actual resolved
  roster.** This change classifies text; it does not walk a roster or check `EffectiveKeywords`.
- **Applying or executing any classified Effect anywhere.** No `AttachedUnitAggregator` changes, no
  presence-gating, no accumulation/stacking engine, no clamping. This change produces
  classifications only.
- **Any live/runtime LLM call.** Regex/template classification only in this change. An offline,
  batch LLM-assisted discovery pass (proposing new templates or flagging text for human review,
  output landing in a checked-in human-reviewed table, never live inference) is real future work —
  the project's own paused `project_ability_classification_prose_schema` exploration already drafted
  a confidence-tiered schema and a 15-ability sample set for this; that should be read and reused,
  not re-derived, when that later change happens.
- **Wiring this classifier into `BsdataDatasheetMapper` or any live catalogue-resolution call path.**
  It must remain entirely standalone and callable in isolation — this is the central reason this
  change exists at all: the user is not comfortable adding more classification responsibility to an
  already-complex, already-bug-prone mapper.

## Decisions

**Standalone component, not an `BsdataDatasheetMapper` extension.** The classifier's namespace/
assembly location must not depend on `Domain.Catalogue.Bsdata` or any BSData JSON type
(`BsSelectionEntry`, `BsCatalogue`, etc.) — its only inputs are two plain strings. Rationale: keeps
it independently testable with zero fixture setup (a table of `(text, expected-output)` pairs, no
`BsSelectionEntry`/closure/ancestry machinery), keeps it usable against both this project's import
pipelines uniformly, and — the primary driver — avoids adding risk to `BsdataDatasheetMapper`, which
has repeatedly been the source of real data-misunderstanding bugs only caught by manual NewRecruit
cross-checks. Alternative considered and rejected: extend `BsdataDatasheetMapper`'s existing
ability-extraction walk to also run this classification inline (mirrors how
`CharacteristicModifierCandidate` classification already works) — rejected specifically per the
user's own stated discomfort with that mapper's complexity, not for a technical reason.

**Regex/template matching, same rigor as `InvulnerableSaveCaveatClassifier`.** Anchored, exact
(or template-parameterized) pattern matching against known phrasings — not a general NLP/parsing
approach. Whitespace/typographic normalization (the existing classifier already handles a real
U+00A0 no-break-space quirk found in BSData text) applies here too if a similar quirk surfaces
against the four ground-truth examples or their corpus siblings.

**`RuleTarget` is an abstract base with sealed subtypes, never nullable.** Mirrors this codebase's
own established convention (`WeaponProfile`, `CharacteristicValue`, `CharacteristicView`) — a value
can never be ambiguous or absent. `Self` is the explicit default/fallback result, not the absence of
a result — deliberately corrected mid-design after an earlier draft used `null` for this case and
was found inconsistent with the rest of the domain's own conventions.

**`Self` and `AttachedUnit` are separate cases, not merged.** These map onto the existing
`Ability.Scope` (`Model`/`Unit`) distinction, but must be independently classifiable by this
component rather than deferring to `Ability.Scope` directly, because `DetachmentRule` (a plain
`(Name, Text)` record) carries no `Scope` field at all to defer to. An earlier draft collapsed these
into one `AttachedUnitTarget` case reasoning that `Ability.Scope` already disambiguates them for
ordinary abilities — corrected once it was noted this doesn't hold for `DetachmentRule` inputs, and
that collapsing them re-introduces exactly the kind of "requires cross-referencing another field"
ambiguity the non-nullable `RuleTarget` decision above was meant to eliminate.

**`EffectVerb` uses rulebook vocabulary (`Improve`/`Worsen`/`Set`), not pre-resolved signed
arithmetic.** The sign each verb resolves to depends on the target characteristic's own arithmetic
family (a `RollThreshold`/`ArmourPenetration`/`Plain` "Kind" concept — improving `WS`/`Sv` means
*subtracting* from the printed number) — resolving that is explicitly out of scope for this change,
which only extracts what the text states.

**`Characteristic` is a plain string matching `Statline`'s own scalar property names**, the same
convention `CharacteristicModifierCandidate` already uses — not a new enum. Consistent with existing
code, avoids introducing a second characteristic-naming scheme.

**Corpus-wide reporting is a requirement, not just a nice-to-have CLI**, because the primary way this
change gets verified is by running it against real ability/rule text and inspecting results directly
— per this project's own established discipline of verifying classifier work against real BSData/
NewRecruit data rather than trusting unit tests alone. The exact form (a new console app project vs.
a runnable command in an existing project) is left to tasks.md/implementation — behaviorally, it
only needs to report classified-vs-default-only entries from a supplied corpus, which is what the
spec captures.

## Risks / Trade-offs

- **Regex/template classification is inherently brittle against phrasing variation** — a genuinely
  new phrasing of "give this model an invulnerable save" that doesn't match Shield Dome's exact
  template will silently classify to the safe default (`Self`, no Effects) rather than fail loudly.
  This is the intended fail-closed behavior (see the spec's own "Unrecognized Text Fails Closed"
  requirement) — mitigated by the corpus-wide reporting requirement making this visible for
  inspection, not by trying to make the regex layer smarter than it should be for this change.
- **Four examples is a small ground truth set** — the classifier could pass all four while still
  being wrong on real corpus phrasing variations. Mitigated only partially by this change (reporting
  visibility); a fuller confidence check (a `[Fact(Explicit = true)]` corpus-scan test, matching this
  project's existing convention) is reasonable follow-up work but not required for this change to be
  considered done.

## Open Questions

- Exact real wording of the Black Templars Detachment rule referred to as "Marshal's Household" —
  the user's own paraphrase ("Friendly SWORD BRETHREN SQUAD units have +1 OC") should be confirmed
  against the live BSData clone or NewRecruit during implementation before writing a template against
  it, per this project's "verify BSData claims precisely" discipline.
- Whether `UnconditionalRuleTarget` has any real corpus instance at all — kept in the type for
  completeness, but not required to be exercised by a real example in this change; if the four
  ground-truth examples and any close corpus siblings never produce it, that's an acceptable outcome
  to note, not a defect.
