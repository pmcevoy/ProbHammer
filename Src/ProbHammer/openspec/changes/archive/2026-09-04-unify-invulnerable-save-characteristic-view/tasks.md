## 1. Core type changes

- [x] 1.1 Strip `Caveated`/`CaveatAbility` from `InvulnerableSave`
      (`src/ProbHammer.Core/Domain/Catalogue/InvulnerableSave.cs`) — becomes a plain
      `(MeleeInSv, RangedInSv)` sealed record with just the implicit-int conversion; remove the
      4-arg constructor and its guard clause (no invariant left to protect).
- [x] 1.2 Change `Statline.InSv`'s type from `InvulnerableSave` to
      `InvulnerableSaveCharacteristicView`, with an explicit default
      (`new(new InvulnerableSave(0, 0), new InvulnerableSave(0, 0), [])`) replacing the old
      parameterless-`InvulnerableSave` default.
- [x] 1.3 Change `StatlineFlagRule.Apply`'s abstract signature to
      `Apply(Statline baseStatline, Ability matchedAbility) -> Statline`.
- [x] 1.4 Update `ShieldDomeStatlineFlagRule.Apply` to build an
      `InvulnerableSaveCharacteristicView` whose `ContributingAbilities` includes `matchedAbility`
      and whose `OriginalValue`/`DerivedValue` are both the granted `InvulnerableSave(5, 5)` (a
      `StatlineFlagRule` match is always deterministic, never caveated).
- [x] 1.5 Update `VexillaStatlineFlagRule.Apply` to accept the new parameter (unused — OC stays a
      plain `int`, untouched by this change).
- [x] 1.6 Remove `StatlineFlagCharacteristic.InvulnerableSave` from the enum
      (`AttachedUnitAggregateView.cs`) — only `ObjectiveControl` remains.
- [x] 1.7 Update `AttachedUnitAggregator.ApplyStatlineFlagRules` to pass the matched
      `AggregateAbilityEntry.Ability` through to `rule.Apply(mutated, abilityEntry.Ability)`, and
      stop appending a `StatlineFlag` for an InSv-targeting rule match (the view itself now carries
      that fact) — keep appending one for every other characteristic (`ObjectiveControl` today).

## 2. Catalogue resolution

- [x] 2.1 Update `BsdataDatasheetMapper.ResolveInvulnerableSave` to construct an
      `InvulnerableSaveCharacteristicView` at its return point instead of a caveated
      `InvulnerableSave`, per design.md Decision 3 — no change to the footnote-parsing/
      `InvulnerableSaveCaveatClassifier`/ancestry-walking logic upstream of that point.
- [x] 2.2 Update `BattleScribeRosterMapper`'s mirrored InSv resolution the same way.

## 3. Fixtures

- [x] 3.1 Update `Examples/Datasheets.cs`'s three `InSv = new InvulnerableSave(...)` construction
      sites (one caveated — Canis Rex — two presumably uniform) to build
      `InvulnerableSaveCharacteristicView`s instead.

## 4. Rendering

- [x] 4.1 Update `LivePlayModel.GroupStatlines` (`LivePlay.cshtml.cs`) to derive `insvSource` from
      `statline.InSv.ContributingAbilities.FirstOrDefault()` instead of the
      `Caveated ? CaveatAbility : Flags.FirstOrDefault(...)` branch.
- [x] 4.2 Update `_UnitBlock.cshtml`'s InSv tile body to read `inv.IsCaveated`,
      `inv.IsCaveated ? inv.OriginalValue : inv.DerivedValue!` for the melee/ranged numbers
      displayed, in place of `inv.Caveated`/`inv.MeleeInSv`/`inv.RangedInSv` read directly off
      `InvulnerableSave`.
- [x] 4.3 (added post-implementation, raised by the user) Move the `IsCaveated ? OriginalValue :
      DerivedValue!` selection off the `_UnitBlock.cshtml` call site and onto each
      `CharacteristicView` subtype as a `Value` property (`ScalarCharacteristicView.Value`/
      `InvulnerableSaveCharacteristicView.Value`) — see design.md's "Post-implementation
      refinement". `_UnitBlock.cshtml` now reads `inv.Value` directly.
- [x] 4.4 (added post-implementation, raised by the user) Add
      `InvulnerableSaveCharacteristicView.Resolved(value)`/`.Caveated(value, ability)` named
      static factories for the two shapes duplicated across `BsdataDatasheetMapper`,
      `BattleScribeRosterMapper`, and `Examples/Datasheets.cs` — see design.md's second
      "Post-implementation refinement". Both mappers' private `ResolvedInSv`/`CaveatedInSv`
      helpers now delegate to these; `Statline.InSv`'s default and every matching fixture/test
      call site updated to use them.
- [x] 4.5 (added post-implementation, raised by the user) Add `InvulnerableSave.None`/
      `InvulnerableSaveCharacteristicView.None` static presets for the "absent" case (mirroring
      `DiceExpression.D3`/`D6`) — see design.md's third "Post-implementation refinement". Every
      real `(0, 0)` call site (both mappers, `Statline.InSv`'s default) updated to use `None`.
- [x] 4.6 (added post-implementation, raised by the user) Generalize `Resolved` to a 2-arg overload
      (`Resolved(value, contributingAbilities)`) instead of treating `ShieldDomeStatlineFlagRule`'s
      shape as a third, unpromoted combination — see design.md's "Post-implementation refinement
      (2b)". `ShieldDomeStatlineFlagRule.Apply` and the one test constructing the same shape both
      updated to use it.

## 5. Tests

- [x] 5.1 Update `InvulnerableSaveTests.cs` to match the stripped `InvulnerableSave` shape (drop
      the caveated/constructor-guard tests; keep/adjust the uniform-value and default-value cases).
- [x] 5.2 Update `InvulnerableSaveResolutionTests.cs` (and confirm
      `InvulnerableSaveCaveatClassifierTests.cs` needs no change — it operates on raw ability text,
      not `InvulnerableSave`/the view) to assert against the new
      `InvulnerableSaveCharacteristicView` return shape.
- [x] 5.3 Update `StatlineFlagRuleTests.cs` for `Apply`'s new signature and
      `ShieldDomeStatlineFlagRule`'s new return shape.
- [x] 5.4 Update `LivePlayFlaggedStatlineRenderingTests.cs` and
      `LivePlayInvulnerableSaveRenderingTests.cs` for the new data source, confirming rendered
      output (markers, legend, tile values) is byte-for-byte unchanged from before this change.
- [x] 5.5 Update `BattleScribeRosterMapperTests.cs` for the new resolution return shape.
- [x] 5.6 Confirm `InvulnerableSaveCaveatResolutionScanTests.cs` (permanent corpus scan) still
      passes unmodified in its assertions (only the wrapping construction changed, not the
      resolution outcome it checks) — update only if it directly references `.Caveated`/
      `.CaveatAbility`.
- [x] 5.7 (added post-implementation) Add unit tests in `CharacteristicViewTests.cs` for the new
      `Value` property on both subtypes (caveated → `OriginalValue`; not caveated → `DerivedValue`).
- [x] 5.8 (added post-implementation) Add unit tests in `CharacteristicViewTests.cs` for
      `InvulnerableSaveCharacteristicView.Resolved`/`.Caveated`; update
      `LivePlayInvulnerableSaveRenderingTests.cs`'s matching call sites to use them.
- [x] 5.9 (added post-implementation) Add unit tests for `InvulnerableSave.None`/
      `InvulnerableSaveCharacteristicView.None`; update `StatlineFlagRuleTests.cs`/
      `LivePlayInvulnerableSaveRenderingTests.cs`'s matching `(0, 0)`/absent call sites to use them.

## 6. Documentation

- [x] 6.1 Update `.claude/domain-model-11e.md`'s `InvulnerableSave`/Statline-Flag Rules sections to
      describe the new shape (`Statline.InSv: InvulnerableSaveCharacteristicView`,
      `StatlineFlagRule.Apply`'s new signature, retired `StatlineFlagCharacteristic.InvulnerableSave`),
      cross-referencing this change.

## 7. Verification

- [x] 7.1 Build the full solution and run the complete test suite (excluding the explicit-only
      corpus scans, run separately) — zero failures.
- [x] 7.2 Run `InvulnerableSaveCaveatResolutionScanTests.Full_corpus_invulnerable_save_caveat_resolution_scan`
      explicitly against the real BSData clone to confirm no behavioral drift in real-corpus
      resolution outcomes.
