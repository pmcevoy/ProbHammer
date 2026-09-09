# Army List Import Pipeline

Full requirements: `openspec/changes/import-army-list-for-live-play/`. Connects a pasted GW-app
11e export to `/LivePlay`: `text → IArmyListParser → ParsedArmyList → ArmyRosterEnricher (against a
ResolvedBsdataCatalogue) → ArmyRoster → /LivePlay`, with per-session storage holding only the
`ParsedArmyList` intermediate — every request re-enriches fresh.

### Army List Parsing (`ProbHammer.Core.Domain.Import`)

```
ParsedModelGroup(ModelName, Count, Weapons: IReadOnlyList<string>)
                                       // see record's own doc comment
ParsedUnit(Name, ModelGroups: IReadOnlyList<ParsedModelGroup>, Enhancements: IReadOnlyList<string>)
                                       // see record's own doc comment
ParsedAttachmentGroup(Bodyguard: ParsedUnit, Attached: IReadOnlyList<ParsedUnit>)
                                       // see record's own doc comment
ParsedArmyList(Name, PointsSpent, Faction, Detachments, ForceDisposition, BattleSize, PointsLimit,
               AttachmentGroups: IReadOnlyList<ParsedAttachmentGroup>,
               StandaloneUnits: IReadOnlyList<ParsedUnit>)
                                       // Faction/Detachments/etc. mirror ArmyRoster's own field
                                       // shapes - see Roster Context in roster-context.md

IArmyListParser.Parse(exportText) -> ParsedArmyList
ArmyListParser                        // the only implementation - see the class's own doc comment
                                       // for the iOS/Android bullet+indentation classification
                                       // algorithm (CollectBulletBlocks' own doc comment has the
                                       // two-signal detail; harden-army-list-parsing-for-android-
                                       // exports/design.md has the full format-difference catalogue).
                                       // ForceDisposition is optional (a real Android export omits
                                       // it entirely). A unit's top-level bullets classify as an
                                       // attachment-role line, an Enhancement list, an explicit
                                       // "Nx ModelName" model-group header, or a direct weapon
                                       // selection - see PartitionModelGroup's own doc comment for
                                       // its still-open Custodian Guard partition-ambiguity gap.

ArmyListParseException(message, unitName?, rawText?)
```

### Army Roster Enrichment (`ProbHammer.Core.Domain.Catalogue.Bsdata` + `Domain.Roster`)

```
BsdataFactionResolver.ResolveStartingFileName(faction, availableFileNames) -> string
                                       // see class's own doc comment

ResolvedBsdataCatalogue(Closure, IdIndex, GroupIdIndex, ProfileIdIndex)
                                       // see class's own doc comment
  Build(source, startingFileName) -> ResolvedBsdataCatalogue   // static factory
  ResolveDatasheet(entryName) -> Datasheet   // throws BsdataNameResolutionException with a
                                              // "did you mean...?" suggestion on a miss

BsdataCatalogueCache(source)          // see class's own doc comment
  GetOrBuild(startingFileName) -> ResolvedBsdataCatalogue
                                       // see class's own doc comment (ConcurrentDictionary-backed,
                                       // lazy, valid for the holding singleton's lifetime)

BsdataNameNormalization.Normalize(text) -> string
                                       // see class's own doc comment

BsdataNameSuggestion.FindClosest(target, candidates, maxDistance = 3) -> string?
                                       // see class's own doc comment (Levenshtein "did you mean...?"
                                       // hint, diagnostic only, chosen over a curated mismatch map)

ArmyRosterEnricher.Enrich(ParsedArmyList, ResolvedBsdataCatalogue) -> ArmyRoster
                                       // Every name resolution is eager, each failure throwing
                                       // BsdataNameResolutionException with a "did you mean...?"
                                       // suggestion. Builds one AttachedUnit per
                                       // ParsedAttachmentGroup and one plain Unit per standalone
                                       // ParsedUnit. Statline name resolution tries an exact match
                                       // first, then two independent fallbacks (see
                                       // ResolveStatlineName's own doc comment for both real-data
                                       // examples). Wargear resolution tries an exact WeaponProfile
                                       // match, then multi-profile expansion, then a
                                       // TryResolveAbility match by name (some "weapon" lines are
                                       // actually wargear-granted abilities, e.g. Impulsor's
                                       // "Shield Dome"). Enhancement resolution deliberately does
                                       // NOT check the resolved ability's Origin is actually
                                       // Enhancement vs. some other OptionalGrant - new eligibility-
                                       // checking this project consistently declines to add
                                       // elsewhere; an empty Enhancements list resolves to empty, no
                                       // Enhancement is ever attached just because the Datasheet
                                       // defines one available.

BsdataFactionResolutionException(faction, message)   // see class's own doc comment
BsdataNameResolutionException(text, message)         // see class's own doc comment
```

`Datasheet` also gained `TryGetStatline`/`TryResolveWeaponProfile` (non-throwing variants) and
`WeaponNames` (`IReadOnlyList<string>`) to support the above, plus, since
resolve-enhancement-abilities, `TryResolveAbility`/`OptionalAbilityNames` for the same reason on
the optional-ability side.

### Detachment Resolution

Full requirements: `openspec/changes/display-army-header-and-detachment-rules/`. Resolves each
entry in `ParsedArmyList.Detachments` (raw text that MAY name more than one Detachment in
natural-language list form, e.g. "Fulguris Task Force, Marshal's Household, and Subversion
Assets") against the faction's `ResolvedBsdataCatalogue` into `ArmyRoster.Detachments`'
`ResolvedDetachment` shape (see "Roster Context" in roster-context.md).

```
BsdataNameResolver.ResolveDetachmentEntries(closure) -> IReadOnlyList<BsSelectionEntry>
                                       // Locates the closure's Detachment-choice group and returns
                                       // its direct children. Confirmed by a full-corpus scan
                                       // (below) to need TWO real shapes, not the one originally
                                       // assumed - a bare group named "Detachment"/"Detachments",
                                       // or a same-named wrapper entry around one nested group.
                                       // A handful of factions (Tyranids, Genestealer Cults,
                                       // Drukhari) reach real choices only via a catalogueLink this
                                       // loader doesn't follow - confirmed, allowlisted corpus gap,
                                       // out of scope here. See the method's own doc comment for
                                       // per-faction examples of both shapes.

DetachmentGroupNameScanTests           // tests/.../CorpusScan/ - see the class's own doc comment
                                       // for the permanent-scan shape. First run: 15 failures
                                       // against the single-shape assumption; generalizing to
                                       // shape 2 fixed 11; the remaining 4 are the confirmed
                                       // importRootEntries gap above, allowlisted.

ResolvedBsdataCatalogue.DetachmentEntries / .DetachmentNames / .ResolveDetachment(name)
                                       // built once in Build() (same convention as the other
                                       // indices) - see DetachmentEntries' and ResolveDetachment's
                                       // own doc comments.

DetachmentRuleTextExtractor.Extract(detachmentEntry, glossary) -> IReadOnlyList<(Name, Text)>
                                       // Domain.Catalogue.Bsdata - see the class's own doc comment
                                       // (a locally-declared rule and a "type": "rule" infoLink
                                       // resolved via RuleGlossary.TryResolve, deliberately WITHOUT
                                       // Core Rule Ability Extraction's "type: upgrade" ancestry
                                       // guard). An unresolvable infoLink is skipped, not failed.

RuleGlossary.Build's own generalization (this change)   // A catalogue file's own local sharedRules
                                       // was assumed to appear only on the game-system file - real
                                       // data contradicts that (Necrons.json defines its own local
                                       // sharedRules, not rules). Build now reads every closure
                                       // file's own SharedRules too, purely additive.

DetachmentNameResolver.Resolve(text, catalogue) -> IReadOnlyList<ResolvedDetachment>
                                       // Domain.Roster - see the class's own doc comment (greedy,
                                       // longest-known-name-first "chomp"; masks matched spans with
                                       // spaces, not splicing, so results stay ordered by position
                                       // in the original text; the two real collision shapes this
                                       // resolves and the failure contract).

ArmyRosterEnricher.Enrich             // now also resolves each Detachments entry via
                                       // DetachmentNameResolver.Resolve, flattened (SelectMany)
                                       // into ArmyRoster.Detachments, since one entry may claim
                                       // more than one ResolvedDetachment.

BattleScribeRosterMapper.MapDetachment(selection) -> ResolvedDetachment
                                       // BattleScribe pipeline - no BSData involvement; see
                                       // method's own doc comment (rule text already inline on
                                       // selections[].rules[]).
                                       // FindGroup(force.Selections, "Detachment", "Detachments") -
                                       // see that method's own doc comment for the matching
                                       // convention and the real Custodes plural/singular bug it
                                       // fixes.
```

### Session-Backed Import (`ProbHammer.Web`)

`ISessionArmyListStore`/`IArmyRosterProvider` are not hard-typed to `ParsedArmyList` — they
accept/return `StoredArmyImport` (`Domain.Import.StoredArmyImport`), a small `[JsonPolymorphic]`
wrapper with two variants, `TextArmyImport(ParsedArmyList)` (the GW-app text pipeline, unchanged)
and `BattleScribeArmyImport(BsRoster)` (see the BattleScribe pipeline section in
battlescribe-import-pipeline.md) — so
`/LivePlay`/`LivePlayCasualtyService` stay format-agnostic.

```
ISessionArmyListStore.Save(ISession, StoredArmyImport)
                     .Load(ISession) -> StoredArmyImport?
                                       // see interface's own doc comment; every request re-builds
                                       // fresh via IArmyRosterProvider, never re-reading a built
                                       // ArmyRoster from storage.

IArmyRosterProvider.Build(StoredArmyImport) -> ArmyRosterBuildResult
                                       // see interface's own doc comment

ImportModel (/Import Razor Page)      // paste box + submit - see class's own doc comment (format
                                       // detection via BattleScribeRosterFormat.TryParse - see
                                       // battlescribe-import-pipeline.md; validates via
                                       // IArmyRosterProvider.Build BEFORE ISessionArmyListStore.Save,
                                       // so a failed re-import never touches a previously-successful
                                       // session). Catches ArmyListParseException/
                                       // BsdataFactionResolutionException/
                                       // BsdataNameResolutionException/
                                       // AmbiguousCharacteristicException/
                                       // BattleScribeRosterParseException and reports the message
                                       // on the page; any other exception type is an unhandled bug.

LivePlayModel.OnGet()                 // loads the session's StoredArmyImport; redirects to /Import
                                       // if absent, otherwise builds via IArmyRosterProvider and
                                       // renders unaware of which pipeline produced it.
                                       // RebuildRoster (casualty rebuild) takes the roster's Units
                                       // as an explicit parameter rather than reading
                                       // Examples.View.MyArmyRoster() internally.
LivePlayCasualtyService.SyncAsync()   // loads the session's StoredArmyImport itself before calling
                                       // RebuildRoster; an empty batch OR no active session import
                                       // both short-circuit to an empty response.
```

`Program.cs` reintroduces `AddDistributedMemoryCache`/`AddSession`/`UseSession` (removed by
`archive-10e-pipeline`; brought back for per-session `StoredArmyImport` storage, not the old
`Enricher` cache) and registers `BsdataCatalogueCache` as a singleton over a
`LocalDiskBsdataCatalogueSource` rooted at `Bsdata:RootDirectory` (default `"BsData"`, resolved
against `IWebHostEnvironment.ContentRootPath`) — the bundled snapshot at
`src/ProbHammer.Web/BsData/`, copied into the Docker image as its own layer, not a live GitHub
fetch. `BsdataCatalogueCache` is untouched by the BattleScribe JSON pipeline, which never reads it.
