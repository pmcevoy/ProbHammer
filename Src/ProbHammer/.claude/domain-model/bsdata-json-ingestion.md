# BSData JSON Ingestion

Full requirements: `openspec/changes/catalogue-json-ingestion/`. Raw file format (not the C# types
below): `.claude/bsdata-json-schema.md`. Namespace: `ProbHammer.Core.Domain.Catalogue.Bsdata`.
Reads the BattleScribe `catalogueSchema` JSON shape (one file per faction/sub-faction) that
BSData's 11th Edition repository publishes, and resolves it into the
`Datasheet`/`Statline`/`WeaponProfile`/`Ability` shapes defined in catalogue-context.md. Resolves
names to rules data only —
does not build the `Unit`/`AttachedUnit`/`ModelLine` graph (`ArmyRosterEnricher`'s job, see "Army
List Import Pipeline" in army-list-import-pipeline.md). Wired into `ProbHammer.Web`/`/LivePlay` via the app-wide
`BsdataCatalogueCache`.

```
IBsdataCatalogueSource              // see interface's own doc comment
  GetJson(fileName) -> string
  ListFileNames() -> IReadOnlyList<string>   // see method's own doc comment (fallback path only)

LocalDiskBsdataCatalogueSource(rootDirectory)   // see class's own doc comment (only implementation
                                                 // today; rootDirectory never hardcoded)

BsdataCatalogueReader.Read(json) -> BsCatalogue
                                       // System.Text.Json only (see class's own doc comment); reads
                                       // either root shape a real BSData file uses (catalogueSchema
                                       // or the single game-system doc) - both share the same nested
                                       // field shapes. See BsCatalogueFile's own doc comment (not
                                       // Read's - it has none) for what's deliberately unmodeled.

BsdataClosureResolver.Resolve(source, startingFileName) -> BsdataClosure
                                       // BFS over catalogueLinks[].importRootEntries - see the
                                       // class's own doc comment, and ResolveImportFileName's own
                                       // doc comment for the cached-name-drift fallback
                                       // (ListFileNames()'s purpose). Also resolves the current
                                       // catalogue's own GameSystemId - see
                                       // BsdataClosure.GameSystem's own doc comment for why it's
                                       // stored separately, outside Files.

BsdataNameResolver.Resolve(closure, name) -> BsSelectionEntry?
                                       // see method's own doc comment (local always beats imported)
  .BuildIdIndex(closure) / .BuildGroupIdIndex(closure) / .BuildProfileIdIndex(closure)
                                       // id -> entry/group/profile maps spanning the closure, used
                                       // only to follow entryLinks/infoLinks during mapping - see
                                       // each method's own doc comment. BuildIdIndex/BuildGroupIdIndex
                                       // deliberately never see BsdataClosure.GameSystem (see its own
                                       // doc comment for why); BuildProfileIdIndex is the one that
                                       // merges it in.

BsdataDatasheetMapper.BuildDatasheet(entry, idIndex, groupIdIndex, profileIdIndex) -> Datasheet
                                       // single recursive tree walk producing Statlines/
                                       // WeaponProfiles/Abilities from the resolved entry - see the
                                       // class's own doc comment (same code path handles both
                                       // single-model and squad entries with no explicit branch;
                                       // entryLink/infoLink resolution mechanics). Ancestry-list
                                       // threading (WalkEntry/WalkGroup) has no doc comment of its
                                       // own. FactionKeywords is always empty; Keywords is populated
                                       // via MapCategoryLinks (see its own doc comment) from
                                       // entry.CategoryLinks.
```

**Ability Extraction and Classification** (`resolve-enhancement-abilities`): an "Abilities" profile's
Intrinsic-vs-optional classification, and the further Enhancement-vs-OptionalGrant split, follow
`ProcessAbilityProfile`'s own doc comment exactly (see also catalogue-context.md's `AbilityOrigin`)
— optional abilities route to the on-demand index (`TryResolveAbility`/`OptionalAbilityNames`,
mirroring `WeaponProfile`). Fixes a real bug where unselected Enhancements leaked onto every
eligible unit.

**Attachment-Eligibility Ability Exclusion** (`resolve-known-ability-effects`): an ability named
exactly "Leader", "Support", or "Attached Unit" never appears in `Datasheet.Abilities`, regardless
of whether it was classified Intrinsic or Core Rule (`AbilityOrigin`, catalogue-context.md) — these three names only ever restate
attachment eligibility a resolved army roster's own attachment relationships (Leader/Support +
Bodyguard, see "Roster Context" in roster-context.md) already represent directly. Filtered once, centrally, in
`Datasheet`'s own constructor (`ExcludedAttachmentAbilityNames`) rather than at each mapper's own
call site — both this pipeline and the BattleScribe/NewRecruit pipeline (battlescribe-import-pipeline.md) already funnel
every Ability they build through this one constructor, so this is a genuine single injection point
covering both, not per-pipeline duplication. A corpus-scan test
(`LeaderSupportAttachedUnitNameScanTests`) confirms this holds across the live BSData clone — see
its own class doc comment for the 849-occurrence first-run result.

Since `unify-characteristic-effect-resolution`, the same constructor filter
(`Datasheet.IsExcludedFromGeneralAbilityWalk`, wrapping `ExcludedAttachmentAbilityNames` plus a new
InSv-caveat-internal check) also excludes the two ability-name conventions
`BsdataDatasheetMapper.ResolveCaveatAbility` looks for by id ("Invulnerable Save ({N}+*)",
"*Invulnerable Save") — see "BSData JSON Ingestion" above for the resolution-scope bug this fixes,
and its own corpus-scan regression test (`InvulnerableSaveCaveatAbilityNameScanTests`, mirroring
`LeaderSupportAttachedUnitNameScanTests` exactly).

**Core Rule Ability Extraction** (`resolve-core-rule-abilities`): a fourth ability-sourcing shape —
an `infoLink` with `"type": "rule"` (real: Impulsor's "Oath of Moment"/"Deadly Demise"/"Firing
Deck"; every Black Templars datasheet's "Templar Vows"). GW's app shows these as "Core"/"Faction
Abilities"; NewRecruit lists them as "Rules". See `ProcessRuleInfoLink`'s own doc comment for the
resolution mechanics (RuleGlossary lookup, Origin CoreRule/ArmyRule, and why the identical infoLink
shape nested inside a weapon's own wargear-option entry — real: Impulsor's "Ironhail Skytalon
Array" — is a different, unrelated thing that must be excluded rather than extracted) and
`BuildAppendedName`'s own doc comment for the append-modifier mechanics (e.g. "Deadly Demise" +
"D3").

Since `gate-and-dedupe-core-rule-abilities`, the resolved rule's own `hidden`/`modifiers` are also
checked (via `IsGameModeGated`, below) before building the Ability — see that method's own doc
comment for the real chapter-exclusivity bug this fixes (Black Templars showing both Oath of Moment
and Templar Vows together). A gated reference produces no Ability, the same silent-skip as an
unresolvable name; this gating has no bearing on Origin.

**Core Versus Army Rule Origin Classification** (`classify-known-army-rules`): Origin is `ArmyRule`
when the resolved rule's Name matches the known army-wide rule names for the roster's Faction, via
`ArmyRuleNameLookup.Resolve` (`Domain.Catalogue`, decoupled from Bsdata so the BattleScribe pipeline
can reuse it) — `CoreRule` otherwise, threaded through `ResolvedBsdataCatalogue.ResolveDatasheet`
into `BuildDatasheet`'s `knownArmyRuleNames` parameter (fail-open default: empty). This replaced an
earlier structural-only signal real data proved unreliable — see `AbilityOrigin.CoreRule`'s own doc
comment (catalogue-context.md) for that history, `ArmyRuleNameLookup`'s own doc comment for the
curated inclusion/exclusion tables, and `classify-known-army-rules/design.md` for why the two
signals couldn't just be OR'd together and for the coverage-scan's roughly-half-the-corpus finding
(`ArmyRuleNameCoverageScanTests`).

A third infoLink type, `"infoGroup"`, was found by this change's corpus scan (129 occurrences) and
is a confirmed, real, not-yet-fixed gap of the same class (targets a `sharedInfoGroups`/
`infoGroups` container with real Abilities content — Adeptus Custodes' "Talons" holds two genuine
mutually-exclusive auras). Deliberately allowlisted (no real infoGroup-carrying export was
available to verify against), tracked as a follow-up. See
`tests/.../CorpusScan/InfoLinkTypeAllowlist.cs`.

**Full-Corpus InfoLink-Type Scan** (`resolve-core-rule-abilities`): see `InfoLinkTypeScanTests`'s
own class doc comment for why this scan exists and how it walks the corpus. Asserts every distinct
`infoLinks[].Type` value against `InfoLinkTypeAllowlist.cs` (`"profile"`, `"rule"` handled;
`"infoGroup"` a confirmed, tracked gap).

```
InvulnerableSave(MeleeInSv, RangedInSv)   // Domain/Catalogue/InvulnerableSave.cs - see the class's
                                       // own doc comment; carried on Statline.InSv wrapped in an
                                       // InvulnerableSaveCharacteristicView (None, below = absent).
  None                                  // see field's own doc comment; the real recurring call
                                       // sites were both mappers' "no InSv text"/"explicitly N/A"
                                       // branches, and Statline.InSv's own default.
  implicit operator InvulnerableSave(int)   // see operator's own doc comment

InvulnerableSaveCharacteristicView(OriginalValue, DerivedValue, ContributingAbilities)
                                       // Domain/Catalogue/CharacteristicView.cs (see "Characteristic
                                       // Value Domain Model" in characteristic-value-domain-model.md) - what Statline.InSv is actually
                                       // typed as. ContributingAbilities holds a resolved footnote's
                                       // linked Ability (parse-time source, below) or a matched
                                       // statline-flag-rules Ability (live source, see statline-flag-rules.md) uniformly -
                                       // no distinguishing shape between the two.
  Value -> InvulnerableSave            // see property's own doc comment; the render layer reads
                                       // this rather than re-deriving the same selection itself
                                       // (added post-implementation, same pattern
                                       // ScalarCharacteristicView.Value uses)
  implicit operator InvulnerableSaveCharacteristicView(int)   // see operator's own doc comment;
                                       // preserves the `InSv = 4` ergonomics fixtures/tests had
                                       // before InSv became a view
  Resolved(InvulnerableSave) -> InvulnerableSaveCharacteristicView
  Resolved(int melee, int ranged) -> InvulnerableSaveCharacteristicView
  Resolved(InvulnerableSave originalValue, InvulnerableSave derivedValue,
           IReadOnlyList<Ability> contributingAbilities) -> InvulnerableSaveCharacteristicView
                                       // static factories for the fully-known ("not caveated") shape
                                       // - see each overload's own doc comment. Added post-
                                       // implementation after this exact shape was found duplicated
                                       // verbatim as a private helper in both BsdataDatasheetMapper
                                       // and BattleScribeRosterMapper, plus hand-rolled a third time
                                       // in an Examples/ fixture - real production duplication, not
                                       // just test-file repetition. Both mappers' own private
                                       // ResolvedInSv helpers now delegate to the 1-arg form. The
                                       // multi-arg (originalValue, derivedValue, contributingAbilities)
                                       // overload was added in a second pass, generalizing what was
                                       // first (incorrectly) treated as a third, unpromoted shape -
                                       // "resolved" (IsCaveated false) and "has contributing
                                       // abilities" are independent facts, and a matched, fully-
                                       // understood StatlineFlagRule (e.g. Shield Dome) is resolved
                                       // by definition, just with its own ability recorded; used
                                       // directly by ShieldDomeStatlineFlagRule.Apply.
  Caveated(InvulnerableSave, Ability) -> InvulnerableSaveCharacteristicView
                                       // see method's own doc comment; same promotion rationale as
                                       // Resolved - both mappers' own private CaveatedInSv helpers
                                       // now delegate here.
  None                                  // see field's own doc comment; every real (0, 0)/absent call
                                       // site (both mappers, Statline.InSv's own default) uses this.

BsdataDatasheetMapper.ResolveInvulnerableSave(text, ancestry, ctx) -> InvulnerableSaveCharacteristicView
                                       // Resolves a Unit profile's raw InSv text into a plain,
                                       // attack-type-restricted, or footnoted-caveated shape.
                                       // Since unify-characteristic-effect-resolution, a footnoted/
                                       // split value ALWAYS resolves to Caveated(fallbackValue,
                                       // ability) here - this method no longer attempts to interpret
                                       // the linked ability's own Text against anything (the retired
                                       // InvulnerableSaveCaveatClassifier used to do that inline; see
                                       // "Statline-Flag Rules" (statline-flag-rules.md) for where that resolution now
                                       // happens instead, at roster-Build time). The linked ability
                                       // is still resolved by id only, never by name. See the
                                       // method's own doc comment for the exact shapes recognized and
                                       // real examples.

BsdataDatasheetMapper.ResolveCaveatAbility(digit, ancestry, ctx, rawText) -> Ability
                                       // Resolves the specific Ability a footnoted InSv is linked
                                       // to, by id, never by name alone - see the method's own doc
                                       // comment for why (a real base-catalogue name collision) and
                                       // for the ancestry-search order and the two recognized naming
                                       // conventions. Aeldari's Archon/Ynnari Archon is a confirmed
                                       // real BSData data anomaly with no ability anywhere in the
                                       // entry, allowlisted rather than special-cased. Since
                                       // unify-characteristic-effect-resolution, both recognized
                                       // naming conventions ("Invulnerable Save ({N}+*)",
                                       // "*Invulnerable Save") are excluded from Datasheet's general
                                       // Abilities/OptionalAbilityNames walk (Datasheet's own
                                       // IsExcludedFromGeneralAbilityWalk, alongside
                                       // ExcludedAttachmentAbilityNames) - the resolved ability is
                                       // reachable ONLY via Statline.InSv.ContributingAbilities, its
                                       // one correct scope, never independently rediscovered at
                                       // component-wide scope by the ordinary ability walk. Confirmed
                                       // by a corpus-scan regression test
                                       // (InvulnerableSaveCaveatAbilityNameScanTests, mirroring
                                       // LeaderSupportAttachedUnitNameScanTests) that every real
                                       // occurrence of either name pattern genuinely is this internal
                                       // mechanism.

WeaponKeywordParser.Apply(weapon, keywordsText) -> WeaponProfile
                                       // see class's and method's own doc comments (splits on
                                       // commas; "-" means none; no alias/synonym recognition). An
                                       // unflagged mechanic (Hazardous, Precision, Heavy) is
                                       // retained verbatim with no flag.
WeaponKeywordParser.UnrecognizedTokens(keywordsText) -> IReadOnlyList<string>
                                       // see method's own doc comment

BsdataDatasheetMapper.IsGameModeGated(modifiers, ctx) -> bool
                                       // see method's own doc comment (and
                                       // IsProvablyAlwaysTrueInMatchedPlay's / IsConditionProvablyTrue's)
                                       // for the exact evaluation semantics and the real bugs each
                                       // fixed.
```

`BsdataDatasheetMapper.ParseThreshold(text, CharacteristicPattern)` (Sv/BS/WS/LD only — InSv no
longer a caller, see `CharacteristicPattern`'s own doc comment) validates against a per-
characteristic allowlist regex rather than stripping known-bad separators, throwing
`AmbiguousCharacteristicException` (see its own doc comment) on a mismatch.

`WeaponProfile.KeywordsText` (`IReadOnlyList<string>`, in `Domain/Catalogue/WeaponProfile.cs`, not
`Bsdata/`) is the one shape change this work made outside the new namespace — see the property's
own doc comment for what it carries and why; included in `EqualityKey()` so rules-identical weapons
spelled differently never silently aggregate. Rendering (`_UnitBlock.cshtml`'s
`WeaponAbilityTags`) still reconstructs its literal ALL-CAPS tag strings from typed flags today,
not from `KeywordsText` — switching remains a deferred future step.

Automated tests never read the external local clone (`C:\Users\Pete\wh40k-11e`, this machine only)
— they use trimmed real excerpts in `tests/ProbHammer.Tests/Domain/Fixtures/Bsdata/` (generated by
deserializing a real file and re-serializing just the needed entries, which also strips unmapped
fields) plus small hand-built closure fixtures for import-precedence scenarios. A one-off smoke
test (every named unit in `data/gw-app-export*.txt` resolves against its faction's real BSData
closure) passed against the live clone during implementation, surfacing one deferred finding: the
captured export uses a typographic apostrophe where BSData JSON uses plain ASCII (`Emperor's
Champion`) — normalizing that is a future export-parser problem, not this loader's.

### Full-Corpus Scan Tests

`tests/ProbHammer.Tests/Domain/Catalogue/Bsdata/CorpusScan/` holds the permanent counterpart to
that smoke test — two xUnit v3 `[Fact(Explicit = true)]` tests (not run by default `dotnet test`
or CI; manually triggered), added by `bsdata-corpus-scan-tests`, that read the live clone directly:

```
LiveClone.RequireSource() -> LocalDiskBsdataCatalogueSource
                                       // see method's own doc comment
LiveClone.CatalogueFileNames(source) -> IReadOnlyList<string>
                                       // see method's own doc comment (and ExcludedFileName's, for
                                       // why "Warhammer 40,000.json" is excluded)
AllowlistEntry<T>(Description, Matches: Func<T, bool>)
AllowlistCheck.AssertClean(results, allowlist, describeResult)
                                       // see AllowlistCheck's own class doc comment

CharacteristicResolutionScanTests.Full_corpus_characteristic_resolution_scan
                                       // see class's own doc comment for the corpus-walk shape.
                                       // CharacteristicResolutionAllowlist.cs covers dice-notation
                                       // WeaponProfile.S text (e.g. Ork Battlewagon "D6+6") failing
                                       // ParsePlainInt, plus one real anomaly (Aeldari's Archon/
                                       // Ynnari Archon - see ResolveCaveatAbility above) - see its
                                       // own class doc comment for the current InSv-gap status.
                                       // `structured-invulnerable-save/design.md` covers the two
                                       // real architectural gaps that change's corpus run surfaced
                                       // and fixed generally.

WeaponKeywordScanTests.Full_corpus_weapon_keyword_token_scan
                                       // see class's own doc comment (walks each file's closure
                                       // directly, not through BuildDatasheet, since
                                       // ResolveWeaponProfile is on-demand only), collecting every
                                       // weapon's Keywords text and running UnrecognizedTokens over
                                       // it. WeaponKeywordAllowlist.cs (see its own class doc
                                       // comment for the seed/scope rationale) groups its 45 tokens
                                       // into no-flag tokens, case/punctuation variants, dice-valued
                                       // variants of an int-only mechanic, and genuinely unmodeled
                                       // mechanics (a few widespread enough - Extra Attacks, Psychic,
                                       // Lance, One Shot - to be worth a future addition). See
                                       // PROGRESS.md.
```
