## 0. Prerequisite check

- [ ] 0.1 Confirm `widen-baseline-generation-coverage` (Stage 1) is implemented, archived, and its
      grown baseline is checked in — do not proceed while it is still pending.

## 1. Resolve open design questions

- [ ] 1.1 Resolve design.md Open Question (b): decide the concrete type/shape that carries a
      deferred, characteristic-specific ability association (no judgment, no `Caveated(...)`) from a
      parser through `Datasheet`/`Statline` into `AttachedUnitAggregator.Build`. Update design.md
      with the decision before starting task 3.
- [ ] 1.2 Resolve design.md Open Question (a): confirm whether reusing
      `AggregateAbilityEntry`'s existing `(ComponentName, StatlineName)` attachment-scope pair is
      sufficient for the shape chosen in 1.1, or whether a small dedicated scope type is actually
      needed. Update design.md with the decision.
- [ ] 1.3 Investigate design.md Open Question (c) against the live BSData corpus: does any real,
      tier-1/tier-2, recognized-field `CharacteristicModifierCandidate` exist whose granting entry has
      no resolvable ability text at all? Record the finding in design.md; if such a case exists, add a
      follow-up task below for its own resolution path before task 4.4 removes
      `ApplyCharacteristicModifierCandidates`.

## 2. InSv rejoins the structural candidate allowlist

- [ ] 2.1 Add InSv's characteristic field id to `BsdataDatasheetMapper.CharacteristicFieldIds`.
- [ ] 2.2 Confirm (via a targeted test against Black Templars' "Consecrating Aura" Enhancement, or its
      real equivalent in the current corpus) that InSv now produces a `CharacteristicModifierCandidate`
      where it previously didn't.

## 3. Simplify parse-time InSv resolution

- [ ] 3.1 Using task 1.1's chosen shape, rework `BsdataDatasheetMapper.ResolveInvulnerableSave` to stop
      calling `InvulnerableSaveCaveatClassifier`: a fully-determinable raw text shape (plain value,
      parenthetical restriction) resolves exactly as today; a footnoted/split value naming a linked
      ability instead records the deferred association at its own correct scope, per task 1.2's
      decision — never at the whole component's scope.
- [ ] 3.2 Apply the identical simplification to `BattleScribeRosterMapper`'s own mirrored
      `ResolveInvulnerableSave`, preserving pipeline parity.
- [ ] 3.3 Update or replace `InvulnerableSaveResolutionTests`/`BattleScribeRosterMapperTests` scenarios
      that asserted the old classifier-driven resolution, to assert the new deferred-association shape
      instead.

## 4. Unify resolution in `AttachedUnitAggregator.Build`

- [ ] 4.1 Extend (or restructure) `ApplyStatlineFlagRules` to also resolve every deferred
      characteristic association recorded by task 3, at its own recorded scope — matching the
      associated ability's normalized Text against the baseline exactly as it already does for a
      present ability, per `statline-flag-rules`' new "Deferred Characteristic Associations Resolve At
      Their Recorded Scope" requirement.
- [ ] 4.2 Confirm an unresolved deferred association (no baseline match) produces a caveated
      characteristic with the associated ability recorded — never a silent, unflagged fallback.
- [ ] 4.3 Confirm the same ability, if also independently present via the general ability-presence
      path, does not flag an already-settled deferred-association characteristic a second time (the
      root-cause bug this whole effort started from).
- [ ] 4.4 Delete `AttachedUnitAggregator.ApplyCharacteristicModifierCandidates` and its own call site
      in `Build`.

## 5. Delete superseded code

- [ ] 5.1 Delete `InvulnerableSaveCaveatClassifier.cs` and `InvulnerableSaveCaveatClassifierTests.cs`.
- [ ] 5.2 Search the solution for any remaining reference to either deleted mechanism
      (`InvulnerableSaveCaveatClassifier`, `ApplyCharacteristicModifierCandidates`) and remove it.
- [ ] 5.3 Delete or rewrite `CharacteristicModifierApplicationTests.cs` scenarios that exercised the
      now-removed live application step, per whatever the corresponding new behavior actually is
      (some may become `statline-flag-rules`-side tests instead of disappearing outright).

## 6. Tests and verification

- [ ] 6.1 Run the full test suite and confirm it's green.
- [ ] 6.2 Run a real captured GW-app export containing an InSv-footnote unit through `/LivePlay`
      (`dotnet run`) and confirm it renders identically to before this change.
- [ ] 6.3 Run a real captured export containing Auric Mantle (or its real current equivalent) through
      `/LivePlay` and confirm its W tile now resolves the same way regardless of which mechanism used
      to reach it first — no longer an ordering accident.
- [ ] 6.4 Confirm `openspec validate --strict` passes for the `statline-flag-rules`/`invulnerable-save`/
      `characteristic-modifier-caveats` deltas, including the capability retirement in the last one.

## 7. Documentation

- [ ] 7.1 Update `.claude/domain-model-11e.md`'s "Statline-Flag Rules", "Invulnerable Save Resolution",
      and "Characteristic-Modifier Caveats" sections to describe the unified mechanism, per root
      `CLAUDE.md`'s documentation-maintenance instruction.
- [ ] 7.2 Update `.claude/vnext-ideas.md`: remove or close out the "caveated characteristic can only
      ever attribute ONE contributing ability" entry if this change's own scope resolution makes it
      moot, or note what of it still applies.
