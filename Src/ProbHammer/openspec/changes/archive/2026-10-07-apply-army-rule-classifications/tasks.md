# Tasks

## 1. Admit Army Rule origin

- [x] 1.1 Add tests to `StatlineFlagRuleTests`, beside
  `DetachmentRuleOriginAbility_AppliesAsWholeUnitScoped_DespiteItsOwnKeywordClassifiedTarget`: an
  Army-Rule-origin ability with a Keyword target applies to every row of an attached unit; one with
  a Self target does too; a conditional (`never`) InvulnerableSave effect is recorded as not added
  rather than applied. Verify they fail before 1.2.
- [x] 1.2 Extend the `DetachmentRule` condition in `TryGetApplicableClassification` and
  `IsBearerOf` to Army Rule through one shared predicate, and update both methods' comments. Verify
  the 1.1 tests pass and the full suite stays green.
- [x] 1.3 Add a weapon-path test (Army-Rule-origin, Keyword target, a conditional
  `WeaponKeywordGrant` on ranged weapons) asserting a not-added keyword chip on every row's ranged
  weapons. Verify it passes.
- [x] 1.4 Update `.claude/domain-model/statline-flag-rules.md` (and the "Target-Scoped Application"
  wording it mirrors) for the Army Rule exception. Verify it names both exceptions.

## 2. Templar Vows option text

- [x] 2.1 In `tools/AbilityPipeline/data/classifications.json`, replace each `\r`-plus-junk sequence
  with the apostrophe it stands for in Templar Vows (two choice options, residue), Mindlock and
  Radiant Mantle (residue), adding a `ReviewerNote` to each. Run
  `dotnet run --project tools/AbilityPipeline/Classifier -- export`. Verify no string in either file
  contains a carriage return, the export's diff touches only those three records, and
  `AbilityClassificationCatalogueTests` pass.
- [x] 2.2 In `.claude/vnext-ideas.md`, delete the "Admit ArmyRule-origin classifications" entry,
  update the Riled Up entry's reference to it, and add to the prompt v3 entry a reminder to scan the
  resubmitted output for control characters before exporting (v2 wrote a `\r` escape in place of an
  apostrophe in 3 records). Verify no reference to the deleted entry remains.

## 3. Integration check

- [x] 3.1 Add an import-flow test importing `data/nr-orks.json`, asserting a conditional InSv
  (`provenance-cond`) and a `weapon-tag-cond` `ASSAULT` chip render. Verify it fails with 1.2
  reverted and passes with it.
- [x] 3.2 In the running app at 667×315: Orks show the conditional InSv and `ASSAULT`; Waaagh!'s
  popover offers its riled-up switch, and turning it on makes the 5+ InSv amber; Black Templars'
  Templar Vows popover lists four vows with correct apostrophes, and choosing the first turns
  `[PRECISION]` amber on melee weapons.
