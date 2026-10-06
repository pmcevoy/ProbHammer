# Tasks

## 1. Colon-qualified glossary lookup

- [x] 1.1 Make `WeaponKeyword.SplitQualifier` `internal static` and call it from
  `RuleGlossary.Normalize` as the step after lowercasing; drop the colon branch from the Anti
  regex (`^anti[\s\-].*`). Verify `WeaponKeywordTests` still pass unchanged.
- [x] 1.2 Add `RuleGlossaryNormalizedResolutionTests` for `"LETHAL HITS: non-MONSTER/VEHICLE"` and
  `"SUSTAINED HITS 2: MONSTER/VEHICLE"` resolving to their base rules; verify they pass alongside
  the existing negated-Anti and `"Antimatter Drive"` tests.
- [x] 1.3 Update `RuleGlossary`'s normalization comment for the new step and the narrower Anti
  boundary; verify the full test suite passes.

## 2. Core-rules fallback for BattleScribe imports

- [x] 2.1 Add `RuleGlossary.WithFallback(RuleGlossary fallback)` (merged index, receiver's keys
  first). Verify with a unit test: a name in both resolves to the receiver's definition, a name
  only in the fallback resolves to the fallback's.
- [x] 2.2 Make `BsdataClosureResolver.ResolveGameSystem` callable with a source and an id (file
  names from `source.ListFileNames()`), keeping the closure resolver's own call. Verify existing
  closure tests pass.
- [x] 2.3 Add `BsdataCatalogueCache.GetGameSystemGlossary(string gameSystemId)`: memoized per id,
  `RuleGlossary.Build` over an empty-files closure carrying the game system, `null` when no file
  matches. Verify with a test against the bundled `BsData/` that `sys-352e-adc2-7639-d610`
  resolves "Lethal Hits" and an unknown id returns `null`.
- [x] 2.4 Add `GameSystemId` to `BsRoster`; compose `roster.WithFallback(core)` in
  `ArmyRosterProvider.BuildFromBattleScribe` when the cache returns a glossary. Verify with a test
  that a roster whose `gameSystemId` matches no file still imports and resolves its own rules.
- [x] 2.5 Commit `data/nr-orks.json` and add an import-flow test that imports it and asserts a
  `LETHAL HITS: non-MONSTER/VEHICLE` chip and a `CLEAVE` chip render as `weapon-tag-resolved`.
  Verify it fails before 2.4 and passes after.
- [x] 2.6 Update `.claude/domain-model/battlescribe-import-pipeline.md`'s "only as complete as
  that roster's own exported rules" note and `.claude/domain-model/rules-glossary-and-popovers.md`'s
  normalization description; verify neither still describes qualified chips or NR weapon keywords
  as unresolved.

## 3. Integration check

- [x] 3.1 Import `data/nr-orks.json` in the running app and confirm weapon-keyword chips open
  popovers (including a qualified one); import a GW-text export and confirm its chips still
  resolve.
