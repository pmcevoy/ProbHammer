# Army List Import Pipeline

Full requirements: `openspec/changes/import-army-list-for-live-play/`. Connects a pasted GW-app
11e export to `/LivePlay`: `text → IArmyListParser → ParsedArmyList → ArmyRosterEnricher (against a
ResolvedBsdataCatalogue) → ArmyRoster → /LivePlay`, with per-session storage holding only the
`ParsedArmyList` intermediate — every request re-enriches fresh.

### Army List Parsing (`ProbHammer.Core.Domain.Import`)

```
ParsedModelGroup(ModelName, Count, Weapons: IReadOnlyList<string>)
                                       // already-split per-loadout sub-group - the shared-vs-
                                       // alternating weapon-count partition has already been
                                       // applied by parse time (below). Weapons is flat and
                                       // per-model; duplicates are meaningful (two "Storm bolter"
                                       // entries mean two copies on one model).
ParsedUnit(Name, ModelGroups: IReadOnlyList<ParsedModelGroup>, Enhancements: IReadOnlyList<string>)
                                       // Role (Leader/Support/Bodyguard) isn't a field - captured
                                       // structurally by which list of ParsedAttachmentGroup a
                                       // member ends up in, mirroring AttachedUnit's Bodyguard/
                                       // Attached split. The export's role-line category
                                       // parenthetical is discarded during parsing.
ParsedAttachmentGroup(Bodyguard: ParsedUnit, Attached: IReadOnlyList<ParsedUnit>)
ParsedArmyList(Name, PointsSpent, Faction, Detachments, ForceDisposition, BattleSize, PointsLimit,
               AttachmentGroups: IReadOnlyList<ParsedAttachmentGroup>,
               StandaloneUnits: IReadOnlyList<ParsedUnit>)
                                       // Faction/Detachments/etc. mirror ArmyRoster's own field
                                       // shapes - see Roster Context in roster-context.md

IArmyListParser.Parse(exportText) -> ParsedArmyList
ArmyListParser                        // the only implementation, covering both iOS- and Android-
                                       // captured exports uniformly via bullet character plus
                                       // per-line indentation (Android drops the nested "◦" glyph
                                       // entirely and relies on indentation alone - see
                                       // CollectBulletBlocks' own doc comment for the two-signal
                                       // algorithm this needs, and harden-army-list-parsing-for-
                                       // android-exports/design.md for the full format-difference
                                       // catalogue). Line-based: blank lines discarded, the fixed
                                       // army-metadata preamble classified positionally, everything
                                       // else by a small set of regexes; ForceDisposition is
                                       // optional (a real Android export omits it entirely). A
                                       // unit's top-level bullets classify as an attachment-role
                                       // line, an Enhancement list, an explicit "Nx ModelName"
                                       // model-group header, or a direct weapon selection - see the
                                       // class's own doc comment for the exact classification
                                       // rules, and PartitionModelGroup's own doc comment for its
                                       // still-open Custodian Guard partition-ambiguity gap.

ArmyListParseException(message, unitName?, rawText?)
```

### Army Roster Enrichment (`ProbHammer.Core.Domain.Catalogue.Bsdata` + `Domain.Roster`)

```
BsdataFactionResolver.ResolveStartingFileName(faction, availableFileNames) -> string
                                       // suffix-matches only the most specific (last) Faction
                                       // entry: a file named exactly "{entry}.json" or ending
                                       // " - {entry}.json", excluding any name containing
                                       // "Library" (shared content, never a playable faction
                                       // identity). Throws BsdataFactionResolutionException on
                                       // zero or multiple matches.

ResolvedBsdataCatalogue(Closure, IdIndex, GroupIdIndex, ProfileIdIndex)
                                       // bundles a resolved closure with the three
                                       // BsdataNameResolver indices over it
  Build(source, startingFileName) -> ResolvedBsdataCatalogue   // static factory
  ResolveDatasheet(entryName) -> Datasheet   // throws BsdataNameResolutionException with a
                                              // "did you mean...?" suggestion on a miss

BsdataCatalogueCache(source)          // app-wide cache of ResolvedBsdataCatalogue, keyed by
  GetOrBuild(startingFileName) -> ResolvedBsdataCatalogue
                                       // starting file name, ConcurrentDictionary-backed, lazy,
                                       // valid for the lifetime of the holding singleton
                                       // (registered in ProbHammer.Web's Program.cs) - the
                                       // expensive part of enrichment is static per faction and
                                       // identical for every user, so built at most once per
                                       // starting file, never per request/session

BsdataNameNormalization.Normalize(text) -> string
                                       // typographic (U+2019) -> plain ASCII apostrophe, applied
                                       // before every BSData lookup (real: "Emperor's Champion")

BsdataNameSuggestion.FindClosest(target, candidates, maxDistance = 3) -> string?
                                       // Levenshtein "did you mean...?" hint on a resolution-
                                       // failure exception - diagnostic only. Chosen over a
                                       // curated mismatch map (real: export's "Absolvor bolt
                                       // pistol" vs. BSData's "Absolver bolt pistol") since the
                                       // corpus is too large/fluid for a hand-maintained table

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

BsdataFactionResolutionException(faction, message)   // carries the offending Faction entry
BsdataNameResolutionException(text, message)         // carries the offending text
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

DetachmentGroupNameScanTests           // tests/.../CorpusScan/ - same permanent [Fact(Explicit =
                                       // true)] pattern. Every real faction file (excluding
                                       // "Library" files) gets a turn as its closure's starting
                                       // file; must resolve at least one Detachment entry. First
                                       // run: 15 failures against the single-shape assumption;
                                       // generalizing to shape 2 fixed 11; the remaining 4 are the
                                       // confirmed importRootEntries gap above, allowlisted.

ResolvedBsdataCatalogue.DetachmentEntries / .DetachmentNames / .ResolveDetachment(name)
                                       // built once in Build() (same convention as the other
                                       // indices). ResolveDetachment(name) is exact-name lookup
                                       // with the same "did you mean...?" contract as
                                       // ResolveDatasheet.

DetachmentRuleTextExtractor.Extract(detachmentEntry, glossary) -> IReadOnlyList<(Name, Text)>
                                       // Domain.Catalogue.Bsdata - extracts zero or more rule pairs
                                       // from a resolved Detachment entry: a locally-declared rule
                                       // (BsSelectionEntry.Rules, mirrors BsCatalogue.Rules) and a
                                       // "type": "rule" infoLink resolved via RuleGlossary.TryResolve
                                       // - the same lookup Core Rule Ability Extraction uses, but
                                       // WITHOUT that extraction's "type: upgrade" ancestry guard,
                                       // since a Detachment entry's direct infoLinks carry no
                                       // equivalent weapon-keyword ambiguity. An unresolvable
                                       // infoLink is skipped, not failed.

RuleGlossary.Build's own generalization (this change)   // A catalogue file's own local sharedRules
                                       // was assumed to appear only on the game-system file - real
                                       // data contradicts that (Necrons.json defines its own local
                                       // sharedRules, not rules). Build now reads every closure
                                       // file's own SharedRules too, purely additive.

DetachmentNameResolver.Resolve(text, catalogue) -> IReadOnlyList<ResolvedDetachment>
                                       // Domain.Roster - a greedy, longest-known-name-first "chomp"
                                       // that disambiguates a natural-language-joined Detachments
                                       // entry without a syntactic split on "and"/commas, masking
                                       // matched spans with spaces (not splicing) so results stay
                                       // ordered by position in the original text. See the class's
                                       // own doc comment for the two real collision shapes this
                                       // resolves (a Detachment name containing "and"; one name a
                                       // literal substring of another) and the failure contract.

ArmyRosterEnricher.Enrich             // now also resolves each Detachments entry via
                                       // DetachmentNameResolver.Resolve, flattened (SelectMany)
                                       // into ArmyRoster.Detachments, since one entry may claim
                                       // more than one ResolvedDetachment.

BattleScribeRosterMapper.MapDetachment(selection) -> ResolvedDetachment
                                       // BattleScribe pipeline - no BSData involvement: a selected
                                       // Detachment's rule text is already inline on its own
                                       // selections[].rules[] (see method's own doc comment).
                                       // FindGroup(force.Selections, "Detachment", "Detachments")
                                       // matches by EITHER name (first match wins) - see that
                                       // method's own doc comment for the real Custodes plural/
                                       // singular bug this fixes.
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
                                       // plain System.Text.Json round-trip through ISession's
                                       // string storage. Stores only the source-level intermediate,
                                       // never the built ArmyRoster - every request re-builds fresh
                                       // via IArmyRosterProvider.

IArmyRosterProvider.Build(StoredArmyImport) -> ArmyRosterBuildResult
                                       // dispatches on variant: TextArmyImport runs the existing
                                       // BsdataFactionResolver -> BsdataCatalogueCache.GetOrBuild ->
                                       // ArmyRosterEnricher.Enrich orchestration; BattleScribeArmyImport
                                       // runs BattleScribeRosterMapper.Map directly (no BSData
                                       // involvement). Used by the import page (validate before
                                       // session-save) and /LivePlay's per-request rebuild - one
                                       // place for a sequence needed at three call sites.

ImportModel (/Import Razor Page)      // paste box + submit. OnPost first attempts
                                       // BattleScribeRosterFormat.TryParse (format recognition -
                                       // see battlescribe-import-pipeline.md); on a match wraps as BattleScribeArmyImport,
                                       // otherwise falls through to ArmyListParser and wraps as
                                       // TextArmyImport. Either way calls IArmyRosterProvider.Build
                                       // to validate BEFORE ISessionArmyListStore.Save, so a failed
                                       // re-import never touches a previously-successful session.
                                       // Catches ArmyListParseException/
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
