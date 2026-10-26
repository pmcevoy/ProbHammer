# Tasks

## 1. Feel No Pain characteristic

- [x] 1.1 Add `Statline.Fnp` (`ScalarCharacteristicView`, init-only, default absent as symbolic "–")
  and a `FeelNoPainEffectResolver.Merge` comparing thresholds (not first-source-wins), replacing the
  source only on an improvement and preserving the original value. Verify with resolver tests
  (grant to absent, better grant over an existing sourced value, worse grant) beside
  `InvulnerableSaveEffectResolverTests`, and a test that two Statlines differing only in FNP are
  unequal.
- [x] 1.2 In `AttachedUnitAggregator`, derive each entry's FNP from its bearer's present "Feel No
  Pain X+" abilities, datasheet or wargear (resolved from absent with that ability as its source;
  `*` or two values → caveated with that ability; bare name → nothing; best threshold wins). Verify
  with `StatlineFlagRuleTests` cases mirroring the `feel-no-pain` spec's Feel No Pain From The
  Bearer's Own Abilities scenarios, including the wargear-row-only and casualty cases.
- [x] 1.3 Apply `FeelNoPainEffect` in `ApplyEffect`, skip qualified ones there, record conditional
  and qualified ones on `NotAppliedEffects`, and add `FeelNoPainEffect` to `Reaches`. Verify with
  tests for an unconditional grant, a qualified grant (value unchanged, recorded), a once-per-battle
  grant (recorded, not applied), an activated one (applied), and an `ActivatableConditionTests` case
  showing the once-per-battle grant offers one condition with no text.
- [x] 1.4 Update `.claude/domain-model/statline-flag-rules.md` for FNP (the bearer's own value,
  threshold merge, qualified effects). Verify it names all three.

## 2. Living Fortress data

- [x] 2.1 In `tools/AbilityPipeline/data/classifications.json`, set Living Fortress's FNP qualifier
  to null with a `ReviewerNote`, then run `dotnet run --project tools/AbilityPipeline/Classifier --
  export`. Verify the export diff touches only that record and `AbilityClassificationCatalogueTests`
  pass.

## 3. `/LivePlay` FNP tile

- [x] 3.1 Render the FNP tile in `_UnitBlock.cshtml` (label "FNP", "N+", "–" when absent but
  highlighted, none when absent and untouched) with `.fnp-tile { grid-column: 4; }` in `site.css`.
  Verify with a rendering test beside `LivePlayInvulnerableSaveRenderingTests` covering present,
  absent, and runs split by FNP.
- [x] 3.2 Build FNP provenance in `ValueProvenanceBuilder`: applied lines, a caveat line for a
  starred value, not-added lines for conditional effects, and a caveat-kind line with the qualifier
  verbatim and "Not included in the value." for a qualified effect (its condition too when
  conditional). Verify with rendering tests for each line kind and the conditional-colour "–" tile.
- [x] 3.3 In `LivePlayModel`, drop a valueless "Feel No Pain" ability entry from a component's
  abilities while any of its runs renders an FNP tile. Verify with tests for the three scenarios in
  the spec's Valueless Feel No Pain Entry Hidden Beside An FNP Tile.
- [x] 3.4 Update `.claude/design-tokens.md` (FNP tile under W, same family as InSv) and remove the
  FNP stat box and valueless-entry items from `.claude/vnext-ideas.md`. Verify neither item remains.

## 4. Integration check

- [x] 4.1 Add an import-flow test importing `data/gw-android-export-custodes.json`, asserting the
  Wardens' conditional FNP tile renders and no valueless "Feel No Pain" entry is listed. Verify it
  passes, and the full suite stays green.
- [x] 4.2 In the running app at 667×315: Custodian Wardens show a blue "–" FNP tile; Living
  Fortress's popover offers its switch, and turning it on shows an amber 4+; a Death Guard unit with
  "Feel No Pain 5+" shows an amber 5+ tile whose popover names "Feel No Pain 5+"; a wargear FNP shows
  only on its own row. (No real export has a wargear FNP; that item is covered by
  `StatlineFlagRuleTests.AWargearFeelNoPain_AppliesToItsOwnRowOnly` only.)
