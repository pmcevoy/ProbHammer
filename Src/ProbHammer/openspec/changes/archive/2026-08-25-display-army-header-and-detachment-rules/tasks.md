## 1. BSData Detachment Rule Text Extraction

- [x] 1.1 Add `BsSelectionEntry.Rules: List<BsRule>` to `Json/BsCatalogueFile.cs`
- [x] 1.2 Add a Detachment rule-text extractor covering the locally-declared `rules[]` shape
- [x] 1.3 Extend the extractor to resolve a `"type": "rule"` infoLink shape via
      `RuleGlossary.TryResolve` (no `type: upgrade`-ancestry guard needed here - see design.md),
      skipping an unresolvable reference rather than failing the whole extraction
- [x] 1.4 Unit tests: locally-declared rule, infoLink-resolved rule, a Detachment with more than
      one rule (e.g. Black Spear Task Force's "Kill Teams" + "Mission Tactics"), an unresolvable
      infoLink skipped without failing, a Detachment with zero rules

## 2. Detachment Name Resolution

- [x] 2.1 Build a nested-by-name index over the closure's own `"Detachment"` selectionEntryGroup
      (local-over-imported precedence), exposed via a new resolve-by-name method on
      `ResolvedBsdataCatalogue`, together with its full name list sorted longest-first
- [x] 2.2 Implement the greedy longest-match "chomp" loop: repeatedly find the longest known name
      still present in the remaining text, remove it plus adjacent separator punctuation, and
      repeat against what's left
- [x] 2.3 Fail loudly with a did-you-mean diagnostic naming the unresolved remainder when a pass
      finds no further matching name while non-separator text still remains
- [x] 2.4 Unit tests: a single name resolves directly; `"Legends of Saga and Song"` resolves as one
      whole entry, never split; `"Armoured Warhost"` resolves without spuriously also claiming
      `"Warhost"`; a two-item joined entry resolves both; a three-item Oxford-comma joined entry
      resolves all three; an unresolvable remainder fails with a diagnostic; a resolved Detachment
      with zero rules carries an empty rule list

## 3. ArmyRoster Shape & Enrichment Wiring

- [x] 3.1 Add `DetachmentRule(string Name, string Text)` and
      `ResolvedDetachment(string Name, IReadOnlyList<DetachmentRule> Rules)` records to
      `Domain.Roster`
- [x] 3.2 Change `ArmyRoster.Detachments` from `IReadOnlyList<string>` to
      `IReadOnlyList<ResolvedDetachment>` (**BREAKING**)
- [x] 3.3 Wire `ArmyRosterEnricher.Enrich` to resolve each parsed Detachment name (task 2) and
      attach the resulting `ResolvedDetachment`s, in parsed order
- [x] 3.4 Update `BattleScribeRosterMapper` to populate `ResolvedDetachment.Rules` directly from the
      roster JSON's own `selections[].rules[]` on each selected Detachment selection - no BSData
      involvement, matching that pipeline's existing design
- [x] 3.5 Update every existing test asserting the bare-string `Detachments` shape:
      `ArmyRosterTests`, `ArmyRosterEnricherTests`, `Examples/ViewTests`,
      `BattleScribeRosterMapperTests`
- [x] 3.6 Confirm `ArmyListParserTests.RealExport_CommaBearingDetachmentName_IsCapturedAsOneEntry`
      still passes unchanged (it should - `ArmyListParser` itself is untouched by this change)

## 4. LivePlay Header Rendering

- [x] 4.1 Extract `_UnitBlock.cshtml`'s popover trigger/panel-building logic (`BuildRulePopover`/
      `NextPopoverId`/`RenderNestedReference`) into a shared, reusable location, parameterized by an
      explicit id-scope prefix instead of implicitly relying on the unit's page index; confirm
      `_UnitBlock.cshtml`'s existing popover rendering tests pass unchanged after the extraction
- [x] 4.2 Add a roster-wide `ArmyRule`-origin ability aggregation: walk every `ArmyRoster.Units`
      entry's components' `Datasheet.Abilities`, filter to `Origin == ArmyRule`, dedupe by Name -
      independent of casualty/`RemainingCount` state (see design.md)
- [x] 4.3 Add a header view model carrying the roster's Name/Faction/BattleSize/Points/
      ForceDisposition, the deduped Army-rule list, and the resolved Detachments
- [x] 4.4 Add a new `_ArmyHeader.cshtml` partial: metadata line(s), then a rules section with
      `<thead><th>`-style column labels (reusing the weapon-table convention) for the Army and
      Detachment columns; a Detachment's own name renders once as a plain (non-interactive) label
      with one popover-trigger chip per resolved rule beneath it (zero chips when it resolved
      none); the Army column is omitted entirely when no `ArmyRule` ability is present
- [x] 4.5 Wire the partial into `LivePlay.cshtml`, above the existing unit-block loop
- [x] 4.6 Confirm no DP cost and no unit points cost renders anywhere on the page
- [x] 4.7 New tests: header renders roster metadata; single-rule Detachment; multi-rule Detachment
      (Black Spear Task Force shape); zero-rule Detachment; two simultaneous `ArmyRule` abilities
      (hand-built Drukhari-shaped fixture - "Power from Pain" + "Corsairs and Travelling Players" -
      per design.md's Risk, since no real captured export covers this); Army column omitted when
      empty; the existing per-unit `ArmyRule` box still renders unchanged alongside the new header

## 5. Verification

- [x] 5.1 New permanent, explicit-only corpus-scan test asserting every faction's own
      Detachment-choice group resolves via the `"Detachment"` name (design.md's Risk)
- [x] 5.2 Manually verify via `docker compose up`/`dotnet run` against real captured exports:
      `data/gw-app-export-3-dp.txt` (3-Detachment, Oxford-comma form),
      `data/gw-app-export-templars-multiple-detachments.txt` (2-Detachment, plain "and" form), a
      local-`rules[]`-shape Detachment (Black Templars' "Marshal's Household"), and an
      infoLink-shape Detachment (Necrons' "Awakened Dynasty") - confirm the header renders
      correctly for each
- [x] 5.3 Full test suite green; re-run every existing explicit-only corpus scan against the live
      clone to confirm no regression

## 6. Documentation

- [x] 6.1 Update `.claude/domain-model-11e.md`'s "Army List Import Pipeline"/"BSData JSON
      Ingestion" sections with the new Detachment resolution mechanism and `ArmyRoster.Detachments`
      shape
- [x] 6.2 Update `.claude/design-tokens.md` if the header introduces any new visual pattern beyond
      the reused `<thead><th>`/popover conventions
- [x] 6.3 Update `PROGRESS.md` with the implementation summary, per this project's own convention
- [ ] 6.4 Sync specs and archive the change once implemented and verified
