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

### Detachment Rule Keyword Target Resolution (`apply-detachment-rule-keyword-targets`)

Full requirements: `openspec/changes/apply-detachment-rule-keyword-targets/`. Once every unit and
Detachment rule text is resolved (either pipeline), matches each Detachment rule's own text against
the resolved roster's units, so a rule like Marshal's Household's "Faith-Fuelled Resolve" ("Friendly
SWORD BRETHREN SQUAD units have +1 OC.") - previously reference-only text printed in the Army Header
- is actually applied to the matching unit(s).

```
DetachmentRuleInboundAbilityResolver.Apply(units: IReadOnlyList<ICombatUnit>,
    detachments: IReadOnlyList<ResolvedDetachment>, baseline: RuleClassificationBaseline)
                                       // Domain.Roster, independent of Domain.Catalogue.Bsdata (same
                                       // precedent as ArmyRuleNameLookup/
                                       // InvulnerableSaveCaveatClassifier) - so one implementation
                                       // serves both import pipelines. For each DetachmentRule across
                                       // every ResolvedDetachment, normalizes its Text
                                       // (RuleEffectClassifier.Normalize) and looks it up via
                                       // baseline.TryGet - never a live RuleEffectClassifier.Classify
                                       // call, mirroring AttachedUnitAggregator.
                                       // ApplyStatlineFlagRules's own lookup convention. When a match
                                       // exists and its Target is a KeywordRuleTarget, appends one
                                       // synthesized Ability (Name/Text verbatim from the
                                       // DetachmentRule, Scope Unit, Origin DetachmentRule - see
                                       // AbilityOrigin's own doc comment) to InboundAbilities for
                                       // every unit whose KeywordResolution.EffectiveKeywords
                                       // contains that keyword. A rule with no baseline entry, or
                                       // whose matched Target is not KeywordRuleTarget
                                       // (SelfRuleTarget/UnconditionalRuleTarget - no meaningful
                                       // "self" to bind to for an abstract army-level rule, or no
                                       // confirmed real example yet), attaches nothing.
```

Downstream consumption: `AttachedUnitAggregator.BuildAbilities` reads `InboundAbilities` as a second
"reported once, belonging to no single component" source (see "Roster Context" in
roster-context.md's `Abilities` section); `AttachedUnitAggregator.TryGetApplicableEntry`/`IsBearerOf`
(the `statline-flag-rules` bearer-scope check) treat a `DetachmentRule`-origin ability as
WholeUnit-scoped regardless of its own baseline entry's classified `Target`, since its own
keyword-scoped target has already been evaluated against the resolved roster by this resolver before
it was ever attached as a present ability. `AttachedUnitAggregator.BuildWeapons`' own separate
bearer-scope check is untouched - no real Detachment-rule baseline entry classified so far carries a
`WeaponCharacteristicEffect`.

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
                                       // see interface's own doc comment. Since
                                       // apply-detachment-rule-keyword-targets, calls
                                       // DetachmentRuleInboundAbilityResolver.Apply(roster.Units,
                                       // roster.Detachments, baseline) once, after either pipeline's
                                       // BuildFromText/BuildFromBattleScribe has already constructed
                                       // its own ArmyRoster - one shared call site for both
                                       // pipelines, mutating each matched unit's InboundAbilities in
                                       // place (see "Roster Context" in roster-context.md).
                                       // ArmyRosterProvider gained a RuleClassificationBaseline
                                       // constructor parameter (already a registered DI singleton).

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

`Program.cs` reintroduces `AddSession`/`UseSession` (removed by `archive-10e-pipeline`; brought
back for per-session `StoredArmyImport` storage, not the old `Enricher` cache). The
`IDistributedCache` backing `AddSession` is environment-dependent: `AddDistributedMemoryCache`
(in-process, gone on restart) unless configuration key `Gcs:BucketName` is set, in which case
`GoogleCloudStorageDistributedCache` (`Services/GoogleCloudStorageDistributedCache.cs`) backs it
instead - GCS objects under `sessions/{sessionId}` in the one bucket
`terraform/cloud-run-iap.tf`'s `google_storage_bucket.state` provisions, so a session survives
Cloud Run scaling an idle `min_instance_count = 0` service to zero. `Gcs__BucketName` is only set
by that Terraform (the Cloud Run service's own env var); local `docker compose up` never sets it,
so local dev is unaffected and needs no GCS credentials. The same bucket, under `dataprotection/`,
also takes over from the default (otherwise-ephemeral, per-instance) DataProtection key ring via
`PersistKeysToGoogleCloudStorage` - same conditional, same reasoning: without it, a fresh key on
every cold start silently invalidates every existing session cookie. `GoogleCloudStorageDistributedCache`
deliberately ignores `DistributedCacheEntryOptions`' own expiration (Set/Refresh only bump the
object's `CustomTime`) - a session should keep working across an arbitrarily long gap between
turns, so GCS's own lifecycle rule (delete unset for 14 days) is storage cleanup only, not
session-expiry enforcement. Because of that, `AddSession`'s own `IdleTimeout` option (set to 7 days,
alongside `Cookie.MaxAge`) is largely cosmetic - it feeds `DistributedCacheEntryOptions`, which this
cache ignores, so it doesn't gate whether a session's GCS-backed data is still readable. Both are set
only when `Gcs:BucketName` is configured (the same gate as the cache/DataProtection wiring above) -
local `docker compose up`'s in-memory cache dies with the process regardless of cookie lifetime, so
it keeps ASP.NET Core's own 20-minute default rather than promising a week it can't deliver. The
setting that actually matters for a returning player is `Cookie.MaxAge`: the session cookie is non-persistent
by default (gone once the browser fully closes), so without it a week-old GCS object would be
unreachable regardless of the bucket's own 14-day retention. `Program.cs` also registers
`BsdataCatalogueCache` as a singleton over a
`LocalDiskBsdataCatalogueSource` rooted at `Bsdata:RootDirectory` (default `"BsData"`, resolved
against `IWebHostEnvironment.ContentRootPath`) — the bundled snapshot at
`src/ProbHammer.Web/BsData/`, copied into the Docker image as its own layer, not a live GitHub
fetch. `BsdataCatalogueCache` is untouched by the BattleScribe JSON pipeline, which never reads it.
