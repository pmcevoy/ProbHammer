## 1. Ground-Truth Verification

- [x] 1.1 Confirm Zealot's, Chance for Glory's, Brutal Raider's, and both Euphoric Strikes texts
      exactly as quoted in `.claude/vnext-ideas.md`'s Phase 1 findings against the live BSData
      clone — Phase 1's spike text was thrown away after use, so these must be re-pulled fresh
      rather than trusted from the summary alone. Record each exact string (including any
      apostrophe/NBSP authoring-variance `RuleEffectClassifier.Normalize` already handles).

      **Findings (2026-09-10, `C:\Users\Pete\wh40k-11e`):** all five confirmed verbatim, no
      apostrophe/NBSP variance found in any of them:
      - Zealot (Adepta Sororitas, Agents of the Imperium — identical text in both files): "Once per
        battle, in the Fight phase, this model can use this ability. If it does, until the end of
        the phase, improve the Strength and Attacks characteristics of melee weapons equipped by
        this model by 3."
      - Chance for Glory (Chaos Space Marines): "Once per battle, at the start of the Fight phase,
        this model can use this ability. If it does, until the end of the phase, improve the
        Strength, Attacks, Armour Penetration and Damage characteristics of melee weapons equipped
        by this model by 1."
      - Brutal Raider (Chaos Space Marines): "Each time this model's unit ends a Charge move, until
        the end of the turn, add 1 to the Strength characteristic of melee weapons equipped by this
        model and improve the Armour Penetration characteristic of those weapons by 1."
      - Euphoric Strikes #1 (Emperor's Children): "Once per battle, at the start of the Fight
        phase, this model can use this ability. If it does so, until the end of the phase, add 3 to
        the Attacks characteristic of melee weapons equipped by this model and improve the Armour
        Penetration characteristic of those weapons by 1." (note: "does **so**", not "does")
      - Euphoric Strikes #2 (Emperor's Children, a second datasheet): identical to #1 except "If it
        does," (matching Brutal Raider's own wording exactly).
- [x] 1.2 While re-pulling the five Phase 1 examples, grep the live clone once more for any further
      "characteristics of ... weapons equipped by this model" / "characteristic of ... weapons
      equipped by this model" occurrences beyond Phase 1's own sample — confirm the two-shape split
      still holds, or note a third shape if one turns up (feeds design.md's Risks/Trade-offs
      mitigation).

      **Findings (2026-09-10):** the two-shape split (dominant coordinate-list, rarer two-verb
      anaphora) still holds for every occurrence anchored on the exact phrase "weapons equipped by
      this model" — no real example was found where that exact anchor phrase produces a third
      grammatical shape. However the wider grep surfaced real corpus variety beyond what this
      change implements, recorded in `.claude/vnext-ideas.md` (see this change's own docs task
      7.3) rather than folded into this change's scope:
      - A real `Set`-verb-shaped weapon effect exists ("...change the Attacks characteristic of
        melee weapons equipped by this model to 12."), contradicting design.md's assumption that no
        real corpus example ever states Set — but it's a mid-sentence "and change ... to N"
        continuation, not either of the two anchored shapes this change extracts, so left
        unimplemented and documented instead.
      - A dice-valued amount exists ("add D3 to the Strength characteristic..."`) — `Amount` is
        `int` per this change's own type; out of scope, documented.
      - Two further real weapon-selector shapes exist beyond `WeaponClass`/`AllWeapons`: an
        ability-flag-qualified selector ("Lethal Hits weapons equipped by this model", "Psychic
        weapons equipped by this model") and a whole-unit-scoped variant ("weapons equipped by
        models in this unit" / "the bearer's melee weapons" / "this model's {named weapon}", no
        "equipped by" at all). This change's selector-qualifier extraction fails closed on all of
        these (qualifier text matches neither "melee"/"ranged"/empty), which is correct given they
        are out of scope, not a bug — documented for a future phase.
      - The four-characteristic weapon-name vocabulary (Strength/Attacks/Armour
        Penetration/Damage) does not cover two more real weapon characteristics found in the
        corpus, Weapon Skill and Ballistic Skill (e.g. "Improve the Weapon Skill characteristic of
        melee weapons equipped by this model by 1."). `CharacteristicModificationKinds` already
        has `WS`/`BS` entries (added by a prior change, unconsumed), but this change's own spec.md
        and task 3.1 both scope the classifier's weapon-characteristic vocabulary to exactly
        S/A/AP/D, so WS/BS stay unrecognized by this classifier for now (documented for a future
        phase; task 4.5 below is satisfied via S/AP instead).

## 2. Core Types

- [x] 2.1 Add `WeaponSelector` (`WeaponSelector.cs`, new file): abstract base,
      `[JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")]`, sealed subtypes `AllWeapons`,
      `WeaponClass(WeaponType Type)`, `NamedWeapon(string Name)`.
- [x] 2.2 Add `WeaponCharacteristicEffect(WeaponSelector Selector, string Characteristic, EffectVerb
      Verb, int Amount)` as a new sealed subtype of `CharacteristicEffect` in
      `CharacteristicEffect.cs`, with a `[JsonDerivedType(typeof(WeaponCharacteristicEffect),
      "Weapon")]` entry added to the base's existing `[JsonDerivedType]` list.
- [x] 2.3 Unit tests: `WeaponSelector` has no null/absent representation (mirrors the existing
      `RuleTarget`/`CharacteristicValue` abstract-base-plus-sealed-subtypes test pattern);
      `WeaponCharacteristicEffect` round-trips through `System.Text.Json` polymorphic
      serialization (mirrors how `RuleClassificationBaseline` already round-trips the other two
      `CharacteristicEffect` subtypes).

## 3. Classifier

- [x] 3.1 Add a weapon-characteristic-name lookup (`"Strength"` → `"S"`, `"Attacks"` → `"A"`,
      `"Armour Penetration"` → `"AP"`, `"Damage"` → `"D"`), disjoint from the existing
      `CharacteristicNames` Statline dictionary, case-insensitive per this classifier's existing
      convention.
- [x] 3.2 Implement the dominant shared-verb/shared-amount coordinate-list extraction path per
      design.md's "Two extraction shapes" decision: recognize "{verb} the {characteristic list} of
      {weapon class} weapons equipped by this model by {amount}", split the characteristic list on
      `,`/"and", and emit one `WeaponCharacteristicEffect` per recognized characteristic, all
      sharing one `WeaponClass` selector/verb/amount. Anchor with the existing `SentenceStart`
      convention, matching every other Effect pattern in this classifier.

      **Design deviation found during implementation (2026-09-10):** the plain `SentenceStart`
      anchor does NOT work for this family — re-verified directly against all five task 1.1
      ground-truth texts. Every one states its actual mutation clause immediately after a
      ", until the end of the phase," / ", until the end of the turn," temporal-scope clause, which
      is itself preceded by an activation preamble ("Once per battle... If it does," / "Each time
      this model's unit ends a Charge move,") not at a true sentence start — under the unmodified
      `SentenceStart` regex, all five would extract zero Effects, contradicting spec.md's own
      Scenario expectations for the exact same texts. Added `WeaponEffectStart`, a structural
      (not phrase-list) widening scoped to only the two dominant-shape weapon patterns —
      `SentenceStart`'s own `^`/`\.\s*` cases plus a third: immediately after that exact
      ", until the end of the phase/turn," clause. `SentenceStart` itself and every existing
      Statline/InSv pattern are untouched — zero regression risk to existing behavior (confirmed:
      full existing `RuleEffectClassifierTests` suite still passes). Both amount-position variants
      implemented as two separate regexes (`WeaponCharacteristicAdd` for "Add N to the X
      characteristic(s)", `WeaponCharacteristicImproveWorsen` for "Improve/Worsen the X
      characteristic(s) ... by N") since real corpus text uses both, mirroring how the existing
      Statline family already needed two separate patterns (`AddCharacteristic`/
      `ShorthandCharacteristicPlus`) for its own two amount-position shapes.
- [x] 3.3 Implement the rarer two-verb anaphora-joined extraction path (Brutal Raider/Euphoric
      Strikes shape): two clause-scoped sub-patterns, the second inferring its selector from the
      first via the "those weapons"/anaphora reference rather than re-extracting it.
- [x] 3.4 Confirm unrecognized weapon-effect-shaped text still fails closed (zero Effects, no
      throw) — e.g. text naming a weapon characteristic without either recognized shape.
- [x] 3.5 Unit tests: the ground-truth examples from task 1.1 each classify to their expected
      `WeaponCharacteristicEffect` list exactly (including the four-way split for Chance for Glory
      and the two-independent-verb split for Brutal Raider/Euphoric Strikes); confirm no differing
      amounts are ever extracted from one shared-amount clause; a handful of negative-control texts
      (plain weapon-keyword prose with no characteristic-mutation language) classify to zero
      Effects.

      13 new tests added to `RuleEffectClassifierTests.cs` (57 total in that file, all passing):
      the five ground-truth texts, the spec's own single-mutation scenario, bare/`AllWeapons` and
      `ranged` selector coverage, a real 3-item Add-form list, an unrecognized-selector negative
      control (real "Psychic weapons" text), an unrecognized-characteristic-list negative control
      (real "Weapon Skill"/"Ballistic Skill" text), two plain-keyword-grant negative controls, and
      an unrelated-conditional-clause negative control.

## 4. Characteristic-Modification-Kind Extension (Damage only — see design.md's Non-Goals for why
   Attacks stays out)

- [x] 4.1 Add `"D"` (Damage) to `CharacteristicModificationKinds`' lookup table, classified
      `Plain`.
- [x] 4.2 Extend `CharacteristicModificationResolver.Resolve` with a `DiceCharacteristicValue`
      branch per design.md's "Damage resolves through the Plain family" decision: `Improve`/
      `Worsen` apply the resolved signed delta to the value's flat modifier via the existing
      `DiceExpression operator +`, preserving Count/Sides; `Set` replaces the value outright with
      `DiceExpression.Fixed(amount)`.
- [x] 4.3 Extend `CharacteristicModificationClamp.Apply` (or add a Damage-specific clamp path
      alongside it) to enforce Damage's guaranteed-minimum-of-1 floor on the dice value's flat
      modifier, per design.md's "Damage's clamp floor" decision — covering both a fixed Damage
      value (plain floor-of-1, like M/T/S/Range) and a dice-valued one (modifier raised just enough
      to keep `Count + Modifier >= 1`).

      Implemented as a new `CharacteristicModificationClamp.ApplyToDice(characteristic,
      DiceExpression)` method (a Damage-specific bound check, per design.md, rather than reusing
      `Apply`'s own `int`-keyed table directly for the dice-rolling case) plus a new `["D"]` entry
      in `Apply`'s own `Bounds` table (floor 1) for the fixed-value case, which `ApplyToDice`
      itself delegates to when `Count == 0`.
- [x] 4.4 Unit tests: every scenario in this change's `characteristic-modification-kind` spec delta
      (Damage classifies as Plain; Improve/Worsen/Set against both a fixed and a dice-shaped Damage
      value; the two clamp scenarios) — mirrors the existing `CharacteristicModificationResolver`
      test file's structure for the three already-covered families.

      **Design.md arithmetic error found while writing this task's own clamp test
      (2026-09-10):** design.md's "Damage's clamp floor" decision originally illustrated the
      dice-clamp case with "D6 worsened by 8 clamps to a modifier of -5" — that number doesn't
      satisfy the decision's own stated invariant (`Count + Modifier = 1` for `Count = 1` requires
      `Modifier = 0`, not `-5`). Corrected directly in design.md (the underlying decision - clamp
      to `1 - Count` - was always right, only the illustrative number was wrong); a second test
      case against `2D6` (where `1 - Count` is genuinely negative, `-1`) added alongside the
      corrected `D6` case so the suite exercises both the "clamps to zero" and "clamps to a real
      negative modifier" shapes. 10 new tests added to `CharacteristicModificationResolverTests.cs`
      (40 total in that file, all passing).
- [x] 4.5 Confirm `WS`/`BS`/`AP`/`S` (already in the lookup table, previously unconsumed by any
      real weapon data) resolve correctly against a real weapon-effect Effect extracted in section
      3 — the first real proving example for each, per design.md's Goals.

      Per task 1.2's finding, this classifier's own weapon-characteristic vocabulary (task 3.1)
      deliberately excludes Weapon Skill/Ballistic Skill (spec.md's own Requirement scopes it to
      exactly Strength/Attacks/Armour Penetration/Damage) - so only S and AP are provable against a
      real *weapon* Effect here; WS/BS stay proven only against Statline data as before, until a
      future phase widens the weapon-characteristic vocabulary. `S`/`AP` proven via
      `Clamp_WeaponCharacteristicsProveTheirExistingBounds` (in `CharacteristicModificationResolverTests.cs`),
      alongside the real Chance for Glory/ranged-selector classifier tests (section 3) that extract
      genuine `S`/`AP` `WeaponCharacteristicEffect`s from real corpus text.

## 5. `WeaponProfile.D` Retype

- [x] 5.1 Retype `WeaponProfile.D` from `DiceExpression` to `ScalarCharacteristicView` in
      `WeaponProfile.cs`, updating `RangedWeapon`/`MeleeWeapon`'s primary constructors and
      `WeaponProfileEqualityKey` (which already reads `D` structurally) to match.

      Added a new `DiceExpression` -> `ScalarCharacteristicView` implicit conversion operator
      (`CharacteristicView.cs`, mirroring the existing `int` one) - lets a real `DiceExpression`
      construction site (e.g. `DiceExpression.D6`) convert the same uniform way an `int` already
      does, rather than every such site needing an explicit `ScalarCharacteristicView.Resolved(...)`
      wrap.
- [x] 5.2 Update every existing `WeaponProfile`/`RangedWeapon`/`MeleeWeapon` construction site
      (`Domain/Catalogue/Bsdata/`, `Domain/Import/BattleScribe/`, `Domain/Examples/`, test
      fixtures) to wrap their Damage value via `ScalarCharacteristicView.Resolved(value, value,
      [])`, mirroring how `S`/`Ap`/`Bs`/`Ws` are already constructed at each of those same sites —
      no behavior change, no contributing abilities recorded yet.

      Only the sites that construct D from a real `DiceExpression` value needed a wrap: the two
      BSData/BattleScribe mappers (each via a new shared-shape `ParseDamage` helper wrapping
      `DiceExpression.Parse`) and `Examples/Datasheets.cs`'s one `DiceExpression.D6`-typed
      Multi-melta (now relying on the new implicit operator from 5.1 instead of an explicit wrap).
      Every other construction site across `Examples/Datasheets.cs`/test fixtures passes a bare
      `int` literal for D and needed no change - see task 5.3's finding for what that bare-int
      path actually produces.
- [x] 5.3 Update every existing `WeaponProfile.D` read site (rendering, aggregation, tests) to
      unwrap `.Value` — confirm each one reads the identical `DiceExpression` it read before this
      task via a targeted diff/test-suite run, since this task must not change any rendered or
      aggregated output.

      Two rendering sites (`_UnitBlock.cshtml`'s ranged/melee weapon tables) and two pre-existing
      test assertions (`DatasheetTests`/`BsdataDatasheetMapperTests`) updated to unwrap `.Value`.

      **Real finding surfaced by the test suite (2026-09-10):** a bare-`int`-literal D construction
      site (every `Examples/Datasheets.cs`/fixture site) resolves through
      `ScalarCharacteristicView`'s own plain-`int` implicit conversion, producing a
      `NumericCharacteristicValue` - NOT a `DiceCharacteristicValue`, which only a real
      `DiceExpression`-typed construction site (the two mappers' `ParseDamage`, the one explicit
      `DiceExpression.D6` site) produces. Confirmed harmless for this task's own actual acceptance
      bar (rendered/aggregated *output*, not the underlying CLR type): both subtypes'
      `ToString()`/`CharacteristicModificationResolver.Resolve` render and resolve a fixed value
      identically, and a fixture-sourced weapon is never grouped via `WeaponProfileEqualityKey`
      against a mapper-sourced one in any real code path - so this is a benign, real-but-inert type
      inconsistency, not a regression, and the two adjusted test assertions now assert the actual
      (correct) per-source behavior instead of a uniform assumption that never held once `D` had two
      real construction paths.
- [x] 5.4 Run the full existing test suite (`dotnet test` or Rider's own test runner) to confirm
      the retype is a pure signature change with zero output difference anywhere in the current
      `/LivePlay`/import pipelines.

      `dotnet test` on the full solution: 640 total, 626 passed, 0 failed, 14 skipped (the
      pre-existing explicit-opt-in `CorpusScan` tests, unaffected by this change).

## 6. Baseline & Corpus Review

- [x] 6.1 Add the `[JsonDerivedType(typeof(WeaponCharacteristicEffect), "Weapon")]` line (task 2.2)
      confirmed round-tripping through `RuleClassificationBaseline`'s own JSON load/save path, not
      just raw `System.Text.Json` (regression risk: the baseline file has its own `Options`/
      converter setup — see `RuleClassificationBaseline.cs`).

      Confirmed two ways: a dedicated new unit test
      (`WeaponCharacteristicEffect_RoundTripsThroughRuleClassificationBaselineFileLoadSave` in
      `RuleClassificationTests.cs`) exercises `RuleClassificationBaseline.Save`/`Load` against a
      real temp file; and task 6.2-6.5's real corpus run below writes 19 real
      `WeaponCharacteristicEffect` entries to the actual checked-in baseline file via
      `--write-baseline`, then re-loads and re-diffs them on a fresh run, confirming they all
      report "unchanged" - the strongest possible proof of this exact round-trip path.
- [x] 6.2 Run `tools/RuleEffectClassificationReport/` against the live BSData clone with the widened
      classifier and inspect the newly-surfaced weapon-characteristic Effect results.

      45 catalogue files scanned, 3809 distinct rule/ability texts. 19 distinct
      weapon-characteristic Effect results surfaced (`=== Effect results: 19 ===`) - every one a
      genuinely new `WeaponCharacteristicEffect` result (0 pre-existing Statline/InSv results were
      unbaselined, confirming the existing baseline already covers all of those).
      `=== Regex vs. structural disagreements: 0 ===` (the old structural mechanism never derives a
      `WeaponCharacteristicEffect` at all, per its own doc comment - irrelevant to this change, as
      expected). Grepped the `Target-only results` section for any further
      "weapons equipped by this model" occurrence with zero Effects (a possible missed extraction)
      - none found.
- [x] 6.3 Manually review every newly-surfaced weapon-characteristic Effect result against its real
      source text (the same rigor `classify-rule-effects-from-text` task 4.3 used) — budget for
      finding and fixing real bugs beyond what section 3's unit tests already cover, not just a
      single implement-and-ship pass. Record each fix inline in this task's own notes, the same way
      task 4.3's precedent above does.

      All 19 results manually checked against their own printed source text (Selector/Characteristic/
      Verb/Amount/Target/IsCaveated all verified correct) - **zero bugs found**, unlike
      `classify-rule-effects-from-text`'s own precedent of finding 6. Notable real-corpus
      confirmations beyond the five ground-truth texts: a real `ranged`-selector example
      (Conversion Eradicator), a real bare-`AllWeapons` example tied to a Command-phase trigger
      (Master of Combat - happens to be the exact real ability behind this change's own
      `WeaponCharacteristic_BareWeaponsQualifier_ResolvesToAllWeaponsSelector` unit test), a real
      3-item Add-form list (Mantra of Strength) and a real 2-item Add-form list (Might of Titan /
      Might of Titan (Psychic)), and two real "...and those weapons have the [KEYWORD] ability"
      continuations (Finest Hour/Instrument of the Emperor's Wrath, Possessed Lord) that correctly
      extract only the real weapon-characteristic Effect and correctly flag `IsCaveated` for the
      unextracted keyword-ability grant, rather than either over-extracting or silently dropping
      the leftover content.
- [x] 6.4 Add a baseline entry (`RuleEffectClassifications.json`) for every manually-confirmed
      weapon-characteristic Effect result via `--write-baseline`, after confirming each one by
      hand — mirrors `baseline-rule-effect-classifications`' own seeding process.

      19 stub entries (Text only) added by hand, then filled in via `--write-baseline` (57 total
      baseline entries refreshed that run: 38 pre-existing-and-matched + 19 new; the checked-in
      file's own pre-existing, unrelated 5-entry "tracked but not matched by any current corpus
      result" gap - 43 tracked vs. 38 matched, before this change touched anything - is untouched
      by this work). 6 of the 19 came back flagged by the "Caveated baseline entries needing
      review" section; each hand-reviewed and given a `note`: two (Finest Hour/Instrument, Possessed
      Lord) record a real, confirmed extraction gap (an unextracted keyword-ability grant, tracked
      in `.claude/vnext-ideas.md`'s existing `KeywordEffect`/`AbilityEffect` idea rather than
      expanded into this change's own scope); four (Betentacled Limbs, Razor Claws, Tearing Claws,
      Warp Strength) record that their trailing content is a Crusade-campaign resource grant, not a
      characteristic mutation at all - out of this classifier's Effect vocabulary entirely, not a
      gap.
- [x] 6.5 Re-run the report tool once more after baselining to confirm every newly-added entry
      reports as "Verified baseline"/unchanged, not Drift/NewInformation.

      Final run: `=== Verified baseline: 62 tracked, 57 unchanged, 0 changed since verified, 7
      fully handled ===` and `=== Caveated baseline entries needing review (no note recorded yet):
      0 ===` - every one of the 19 new entries (and the 6 caveated ones with their new notes)
      confirmed unchanged/reviewed.

## 7. Docs

- [x] 7.1 Update `.claude/domain-model/rule-effect-classification.md` with a new section
      describing the weapon-characteristic Effect shape, the two extraction paths, and the final
      corpus numbers from section 6's review pass (mirroring how prior changes to this same file
      recorded their own numbers).

      New "Weapon-characteristic Effects" section added (between the InSv-widening numbers bullet
      and the pre-existing "related idea" paragraph): the `WeaponSelector`/`WeaponCharacteristicEffect`
      shape, both extraction paths, the `WeaponEffectStart` anchor deviation and why, and the final
      19-result/62-entry corpus numbers.
- [x] 7.2 Update `.claude/domain-model/characteristic-modification-kind.md` to record Damage's
      now-proven dice-aware resolution path and the `WS`/`BS`/`AP`/`S` kinds' first real proving
      example, replacing the "not yet consumed by anything" note where it's no longer accurate.

      Updated: the API surface diagram now lists `ApplyToDice` and notes `Resolve`'s new branch; the
      "Deliberately excludes InSv and Attacks/Damage" note corrected to just Attacks (with a new
      explanation for why Attacks alone stays excluded); a new "Damage is covered" section describes
      the dice-aware resolve/clamp path; the proving-example note now covers `S`/`AP` against real
      weapon data (not just Vexilla's `Oc`) and explains why `WS`/`BS` still don't have a real
      *weapon* proving example; the old "Not yet consumed by anything" note replaced with an accurate
      "consumed today only by the report tool / unit tests, still no `AttachedUnitAggregator`
      wiring" statement.
- [x] 7.3 Update `.claude/vnext-ideas.md`'s "WeaponProfile-targeting rule effects — phased plan"
      entry: mark Phase 2 done with a summary of what shipped, and confirm Phase 3's own bullet
      still accurately reflects what Phase 2 actually left for it (weapon-selector-to-contribution
      resolution, the aggregation mechanism) now that the real shape of `WeaponCharacteristicEffect`
      /`WeaponSelector` is known rather than speculative.

      Phase 2 marked done with a full summary (including the `WeaponEffectStart` deviation and every
      new corpus finding deferred rather than folded in - the Set-verb/dice-amount/extra-selector/
      extra-characteristic/keyword-continuation findings from task 1.2). Phase 3's own bullet
      confirmed accurate and given one addition: named-weapon matching and class matching are exactly
      what Phase 2 left for it, plus a note that real corpus data only ever produces
      `WeaponClass`/`AllWeapons`, never `NamedWeapon` - a fact Phase 3 can now rely on rather than
      guess at.
