## 1. JSON model

- [x] 1.1 Add a `Modifiers` (`List<BsModifier>`, default `[]`) property to `BsEntryLink`
  (`src/ProbHammer.Core/Domain/Catalogue/Bsdata/Json/BsCatalogueFile.cs`) — currently absent
  entirely; the real JSON puts the `append`/`field:"name"` modifier directly on the infoLink.

## 2. Domain shape

- [x] 2.1 Add `CoreRule` to `AbilityOrigin`
  (`src/ProbHammer.Core/Domain/Catalogue/AbilityOrigin.cs`), with an XML doc line matching the
  existing style.

## 3. Mapper

- [x] 3.1 In `BsdataDatasheetMapper`, add a helper that resolves a `type:"rule"` infoLink's display
  name: the infoLink's own `Name`, plus each `Modifiers` entry where `Type == "append"` and
  `Field == "name"` (in array order), space-joined. Handle both `JsonValueKind.String` (use
  `GetString()`) and `JsonValueKind.Number` (use `GetRawText()`) for the modifier's `Value`.
- [x] 3.2 Add a `RuleGlossary?` parameter to `WalkContext` and thread it through from a new optional
  `RuleGlossary? glossary = null` parameter on `BuildDatasheet`, mirroring `forceEntries`'s existing
  pattern exactly (default `null` so every current call site keeps compiling unchanged).
- [x] 3.3 Extend `WalkInfoLink` to handle `link.Type == "rule"`: when `ctx.Glossary` is non-null,
  resolve the display name (3.1), resolve `RuleGlossary.TryResolve(link.Name)` for the text, and — if
  resolution succeeds — add an `Ability { Name = <resolved name>, Text = ruleDefinition.Text, Scope
  = AbilityScope.Unit, Origin = AbilityOrigin.CoreRule }` to `ctx.Abilities` via the existing
  `SeenAbilityNames` dedup path (same list/set `Intrinsic` abilities use). When `TryResolve` returns
  null, do nothing (silent skip, per design.md).
- [x] 3.4 In `ResolvedBsdataCatalogue.ResolveDatasheet`, pass `Glossary` into the new
  `BuildDatasheet` parameter.

## 4. Unit tests

- [x] 4.1 Extend the existing `ability-classification.json` fixture (or add a new
  `core-rule-ability.json` fixture, generated the same way — deserialize a real trimmed excerpt and
  re-serialize) with a `type:"rule"` infoLink carrying an `append`/`field:"name"` modifier (mirror
  the real Impulsor "Deadly Demise"/"D3" shape) and a matching `rules`/`sharedRules` entry for it to
  resolve against.
- [x] 4.2 Add scenarios to `AbilityClassificationTests.cs` (or a new test file alongside it,
  matching the project's one-file-per-classification-family convention) covering: a Core Rule
  reference resolves into the intrinsic Abilities list with Origin CoreRule; the appended-value name
  construction (string-valued and number-valued `append` modifier, both real shapes); a rule
  reference with no `append` modifier keeps the bare name; an unresolvable rule reference is
  silently skipped rather than throwing or being added.
- [x] 4.3 Confirm `Datasheet.OptionalAbilityNames`/`TryResolveAbility` are unaffected — a Core Rule
  ability must never appear there, only in the always-exposed `Abilities` list.

## 5. Corpus scan

- [x] 5.1 Add a new scan test (following `WeaponKeywordScanTests`' pattern of walking a closure
  directly rather than through `BuildDatasheet`) that collects the distinct `Type` value of every
  `infoLink` reachable at any depth within every datasheet's descendant tree, across every real
  catalogue file in the local clone (excluding `Warhammer 40,000.json`).
- [x] 5.2 Add the corresponding allowlist file (`InfoLinkTypeAllowlist.cs` or similar, matching
  existing naming) seeded with `"profile"` and `"rule"` as recognized/handled types, using the
  existing bidirectional `AllowlistCheck.AssertClean` mechanism.
- [x] 5.3 Run the new scan against the live clone (`C:\Users\Pete\wh40k-11e`) manually (it's
  `[Fact(Explicit = true)]`, not part of default `dotnet test`) and resolve whatever it finds —
  either it confirms `"profile"`/`"rule"` are exhaustive, or it surfaces a genuinely new infoLink
  type this change should also decide how to handle before merging.
  **Found a third, genuinely new type: `"infoGroup"` (129 occurrences, e.g. Adeptus Custodes'
  "Talons" - a real, confirmed ability-granting gap, same class as `"rule"` was). Per user
  decision: allowlisted here as a deliberately-tracked, not-yet-fixed gap rather than widening
  this change's scope - no export was available to verify a fix against. Tracked as a dedicated
  follow-up change once a real infoGroup-carrying export (a friend's Custodes list, expected
  2026-08-27) is available to verify against.

## 6. Verification against the real bug report

- [x] 6.1 Run the existing spike-style pipeline (parse `data/gw-app-export-templars.txt` → enrich
  against the live clone) and confirm `Oath of Moment`, `Templar Vows`, `Deadly Demise D3`,
  `Firing Deck 6`, `Infiltrators`, and `Scouts 6"` (or whatever the source data's own base name
  turns out to be — see design.md's flagged naming question) now appear on the expected units.
  **All confirmed present on the expected units.** `Oath of Moment` is genuinely absent on
  Impulsor specifically - confirmed as a real BSData data fact (Black Templars' own local
  "Impulsor" override entry declares Deadly Demise/Firing Deck/Templar Vows but not Oath of
  Moment, unlike the generic Space Marines Impulsor it locally overrides), not a bug. This first
  run also surfaced a real false-positive (see 3.5 below) that the fix for 3.3 needed to account
  for.
- [x] 3.5 (found during 6.1, not in the original plan) Fix a false positive the first working
  version produced: a `type:"rule"` infoLink nested inside a `"type": "upgrade"` weapon/wargear
  option entry (confirmed real shape: Impulsor's "Ironhail Skytalon Array" carries its own
  "Sustained Hits"/"Anti" keyword cross-references) was leaking into `Datasheet.Abilities` as a
  bogus unit-wide ability. Fixed by reusing `ProcessAbilityProfile`'s existing ancestry signal
  (`ancestry[^1].Type == "upgrade"`) to exclude it. `design.md` and the `catalogue-json-ingestion`
  spec delta updated to document this; a dedicated fixture/unit test added (see 4.2).
- [x] 6.2 Run the app locally (`docker compose up`) and visually confirm on `/LivePlay` with the
  Black Templars export: the new abilities render in the Unit Abilities column, unprefixed, tap
  open a working rule-text popover, and no existing ability (Shield Dome, Enhancements, Intrinsic
  abilities) regresses.
  **Confirmed via `docker compose up --build` + Firefox against the real export.** All expected
  abilities present exactly where expected (`Scouts 6"`, `Infiltrators`, `Oath of Moment`,
  `Templar Vows` on Scout Squad; `Deadly Demise D3`, `Firing Deck 6`, `Templar Vows` on Impulsor;
  `Templar Vows` on every other unit) with no weapon-keyword pollution. `Templar Vows`'s popover
  opens with the correct full rule text. `Shield Dome` and the Enhancement indicator
  (`✦ Oathbound Exemplar`) both still render correctly, unaffected.
- [x] 6.3 Run the full existing test suite (`dotnet test`, default invocation) and confirm no
  regression — in particular `BsdataDatasheetMapperTests`, `AbilityClassificationTests`,
  `ArmyRosterEnricherTests`, and any `/LivePlay` rendering tests.
  **339 tests total, 335 run + 4 explicit-only (skipped by default), all passing.** Explicit-only
  scans (including the new `InfoLinkTypeScanTests`) also re-run manually and confirmed clean.

## 7. Docs

- [x] 7.1 Update `.claude/domain-model-11e.md`'s "BSData JSON Ingestion" section (the
  `BsdataDatasheetMapper`/"Ability Extraction and Classification" description) to document the
  fourth ability-sourcing shape, following the same level of real-example detail the existing three
  shapes already have.
- [x] 7.2 Update `PROGRESS.md` per this project's standing documentation-maintenance instruction.
