## 1. Runtime baseline loading

- [x] 1.1 Register a `RuleClassificationBaseline` singleton in `Program.cs`, loaded once from
      `Data/RuleEffectClassifications.json` resolved against `IWebHostEnvironment.ContentRootPath`
      (mirrors `BsdataCatalogueCache`/`LocalDiskBsdataCatalogueSource`'s existing root-resolution
      convention).
- [x] 1.2 Confirm `Data/RuleEffectClassifications.json` is already covered by the Docker image's
      existing `Web/Data` copy step; add it if it isn't.

## 2. Resolution glue

- [x] 2.1 Add a small helper that resolves a `ScalarCharacteristicEffect` against a Statline field's
      current `ScalarCharacteristicView`: call `CharacteristicModificationResolver.Resolve` with the
      field's current effective `Value`, then wrap the result via
      `ScalarCharacteristicView.Resolved(current.OriginalValue, resolvedValue, [ability])` -
      preserving the true pre-mutation `OriginalValue` through a chain (design.md Decision 7).
- [x] 2.2 Add a `RuleTarget` → scope mapping: `SelfRuleTarget` → the existing Bearer scope,
      `AttachedUnitRuleTarget` → the existing WholeUnit scope, `KeywordRuleTarget`/
      `UnconditionalRuleTarget` → excluded (design.md Decision 4).

## 3. `AttachedUnitAggregator` rewiring

- [x] 3.1 Add a `RuleClassificationBaseline` parameter to `AttachedUnitAggregator.Build`.
- [x] 3.2 Rewrite `ApplyStatlineFlagRules` to: look up each present ability's normalized Text
      (`RuleEffectClassifier.Normalize`) against the baseline, filter matches to those whose target
      maps to Bearer/WholeUnit (task 2.2), and apply every classified Effect - `ScalarCharacteristicEffect`
      via the task 2.1 helper, `InvulnerableSaveCharacteristicEffect` via the existing
      `InvulnerableSaveEffectResolver.Resolve` - with no additional gating on `IsCaveated`/
      `FullyHandled` (design.md Decision 5).
- [x] 3.3 Verify `ApplyCharacteristicModifierCandidates`'s existing "skip if the field already carries
      a contributing ability" guard correctly covers same-field stacking between two baseline matches
      too, with no new accumulate logic (design.md Decision 6) - add a regression test if the existing
      guard doesn't already generalize to this case for free.
- [x] 3.4 Update `LivePlay.cshtml.cs` (the sole call site of `AttachedUnitAggregator.Build`) to pass
      the registered `RuleClassificationBaseline` singleton through.

## 4. Retire `StatlineFlagRule`

- [x] 4.1 Delete `src/ProbHammer.Core/Domain/Roster/StatlineFlagRule.cs` (`StatlineFlagRule`,
      `ShieldDomeStatlineFlagRule`, `VexillaStatlineFlagRule`, `StatlineFlagRuleCatalogue`,
      `StatlineFlagRuleScope`) once task 3's new path is proven equivalent.
- [x] 4.2 Delete `tests/ProbHammer.Tests/Domain/Roster/StatlineFlagRuleTests.cs`.
- [x] 4.3 Search the solution for any remaining `StatlineFlagRule*` reference and remove it.

## 5. Tests

- [x] 5.1 Build a small fixture `RuleClassificationBaseline` (not the real 41-entry corpus file)
      reproducing Shield Dome's and Vexilla's baseline entries verbatim, mirroring the BSData
      trimmed-fixture testing convention.
- [x] 5.2 Port the existing `StatlineFlagRuleTests` scenarios onto the new baseline-driven path: a
      matched ability grants a new invulnerable save value, a matched ability adjusts an existing
      characteristic value, an unmatched ability produces no flagged value, the source ability
      remains visible in its normal listing, and a flagged value's liveness follows its bearer's
      presence.
- [x] 5.3 Add a test covering a keyword-scoped baseline match and an unconditional-roster-wide
      baseline match, each producing no flagged value (specs' new "Target-Scoped Application"
      requirement).
- [x] 5.4 Add a test covering same-field stacking between two baseline matches on the same unit:
      the first applied wins, the second is skipped (mirrors
      `CharacteristicModifierApplicationTests`' existing pinning-test convention for the sibling
      mechanism).
- [x] 5.5 Run the full test suite and confirm it's green.

## 6. Real-corpus verification

- [x] 6.1 Run a real captured GW-app export containing a Shield Dome- or Vexilla-bearing unit through
      `/LivePlay` (`dotnet run`) and confirm it renders identically to before this change.
- [x] 6.2 Spot-check any other baseline entries reachable in that export (or another available real
      export) for correct flagged-tile/legend rendering.

## 7. Documentation

- [x] 7.1 Update `.claude/domain-model-11e.md`'s "Statline-Flag Rules" section to describe the
      baseline-driven mechanism, per root `CLAUDE.md`'s documentation-maintenance instruction.
- [x] 7.2 Update `.claude/vnext-ideas.md` if verification (task 6) surfaces any newly-confirmed gap
      beyond the two already-known keyword-scoped entries.
