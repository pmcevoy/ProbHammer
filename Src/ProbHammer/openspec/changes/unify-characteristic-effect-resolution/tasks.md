## 0. Prerequisite check

- [ ] 0.1 Confirm `widen-baseline-generation-coverage` (Stage 1) is implemented, archived, and its
      grown baseline is checked in — do not proceed while it is still pending.

## 1. InSv rejoins the structural candidate allowlist

- [ ] 1.1 Add InSv's characteristic field id to `BsdataDatasheetMapper.CharacteristicFieldIds`.
- [ ] 1.2 Confirm (via a targeted test against Black Templars' "Consecrating Aura" Enhancement, or its
      real equivalent in the current corpus) that InSv now produces a `CharacteristicModifierCandidate`
      where it previously didn't.

## 2. Exclude the InSv-caveat-internal ability names from the general walk

- [ ] 2.1 Add `"Invulnerable Save ({N}+*)"` (pattern) and `"*Invulnerable Save"` (literal) to
      `Datasheet`'s existing exclusion mechanism, alongside `ExcludedAttachmentAbilityNames` — same
      central injection point, both pipelines covered for free (design.md Decision 2).
- [ ] 2.2 Add a corpus-scan regression test mirroring `LeaderSupportAttachedUnitNameScanTests`,
      confirming every real occurrence of either name pattern in the live BSData clone is genuinely
      resolved via `ResolveCaveatAbility`'s own mechanism, not a distinct, independently-meaningful
      ability (design.md's Risks section).
- [ ] 2.3 Confirm an InSv-footnote unit's caveat-internal ability no longer appears in its own rendered
      ability listing (it remains reachable only via `Statline.InSv.ContributingAbilities` and the
      existing flag-legend popover mechanism).

## 3. Simplify parse-time InSv resolution

- [ ] 3.1 Rework `BsdataDatasheetMapper.ResolveInvulnerableSave` to stop calling
      `InvulnerableSaveCaveatClassifier`: a fully-determinable raw text shape (plain value,
      parenthetical restriction) resolves exactly as today; a footnoted/split value naming a linked
      ability produces `Caveated(fallbackValue, ability)` exactly as today, minus the classification
      attempt.
- [ ] 3.2 Apply the identical simplification to `BattleScribeRosterMapper`'s own mirrored
      `ResolveInvulnerableSave`, preserving pipeline parity.
- [ ] 3.3 Update `InvulnerableSaveResolutionTests`/`BattleScribeRosterMapperTests` scenarios that
      asserted the old classifier-driven resolution, to assert the new always-caveat-then-defer shape
      instead (some scenarios simply drop their own "resolves via template" assertion and become
      "stays caveated, ability attached").

## 4. Add the Build-time InSv resolution step; retire `CharacteristicModifierCandidate`'s live path

- [ ] 4.1 Add the new step to `AttachedUnitAggregator`: for every `AggregateStatlineEntry` whose
      `Statline.InSv` is still caveated, resolve its single contributing ability's normalized Text
      against the baseline via `InvulnerableSaveEffectResolver`, replacing the caveated result on a
      match (design.md Decision 3).
- [ ] 4.2 Confirm an unresolved caveat (no baseline match) is left unchanged — same fallback display as
      today.
- [ ] 4.3 Delete `AttachedUnitAggregator.ApplyCharacteristicModifierCandidates` and its own call site
      in `Build`.
- [ ] 4.4 Confirm a present ability whose structural candidate previously always-caveated (e.g. Auric
      Mantle) now resolves through the ordinary `ApplyStatlineFlagRules` present-ability pass alone,
      with no separate mechanism involved.

## 5. Delete superseded code

- [ ] 5.1 Delete `InvulnerableSaveCaveatClassifier.cs` and `InvulnerableSaveCaveatClassifierTests.cs`.
- [ ] 5.2 Search the solution for any remaining reference to either deleted mechanism
      (`InvulnerableSaveCaveatClassifier`, `ApplyCharacteristicModifierCandidates`) and remove it.
- [ ] 5.3 Delete or rewrite `CharacteristicModifierApplicationTests.cs` scenarios that exercised the
      now-removed live application step.

## 6. Tests and verification

- [ ] 6.1 Run the full test suite and confirm it's green.
- [ ] 6.2 Run a real captured GW-app export containing an InSv-footnote unit through `/LivePlay`
      (`dotnet run`) and confirm it renders identically to before this change.
- [ ] 6.3 Run a real captured export containing Auric Mantle (or its real current equivalent) through
      `/LivePlay` and confirm its W tile resolves the same way as before this change — now by design,
      not by ordering accident.
- [ ] 6.4 Confirm `openspec validate --strict` passes for the `invulnerable-save`/
      `characteristic-modifier-caveats` deltas, including the capability retirement in the latter.

## 7. Documentation

- [ ] 7.1 Update `.claude/domain-model-11e.md`'s "Invulnerable Save Resolution" and
      "Characteristic-Modifier Caveats" sections to describe the unified mechanism, per root
      `CLAUDE.md`'s documentation-maintenance instruction.
- [ ] 7.2 Update `.claude/vnext-ideas.md`: remove or close out the "caveated characteristic can only
      ever attribute ONE contributing ability" entry if this change's own scope resolution makes it
      moot, or note what of it still applies.
