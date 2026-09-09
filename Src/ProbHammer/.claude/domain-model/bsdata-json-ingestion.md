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
IBsdataCatalogueSource              // "get JSON content for this filename" boundary
  GetJson(fileName) -> string
  ListFileNames() -> IReadOnlyList<string>   // fallback path only, see below

LocalDiskBsdataCatalogueSource(rootDirectory)   // only implementation today; rootDirectory is a
                                                 // caller-supplied config value, never hardcoded

BsdataCatalogueReader.Read(json) -> BsCatalogue
                                       // System.Text.Json; Json/BsCatalogueFile.cs models only the
                                       // fields this loader needs. Reads either root shape a real
                                       // BSData file uses (catalogueSchema or the single
                                       // game-system doc) - both share the same nested field shapes.
                                       // See the method's own doc comment for what's deliberately
                                       // unmodeled.

BsdataClosureResolver.Resolve(source, startingFileName) -> BsdataClosure
                                       // BFS over catalogueLinks[].importRootEntries, starting file
                                       // first, nearer imports before farther. A link's cached
                                       // `name` usually equals its target file name but can drift
                                       // (real: "Chaos - Daemons Library" links to "Chaos - Chaos
                                       // Daemons Library.json") - falls back to matching every
                                       // available file's own catalogue `id` against the link's
                                       // targetId (ListFileNames()'s purpose). Also resolves the
                                       // current catalogue's own GameSystemId against every file's
                                       // own `id` (structured-invulnerable-save), since the base
                                       // rules file's shared "Invulnerable Save" abilities live
                                       // only there. Stored separately as `BsdataClosure.GameSystem
                                       // : BsCatalogue?`, outside `Files` - its generic, cross-
                                       // faction template content (Warlord traits, Enhancements,
                                       // Crusade rules) was never meant to be walked as real
                                       // datasheet content (confirmed by a corpus-scan regression
                                       // when tried more broadly: a T'au entry's dangling "Warlord"/
                                       // "Enhancements"/"Crusade" entryLinks resolved into
                                       // unparseable "*"-placeholder wargear). Only its
                                       // SharedProfiles is actually needed.

BsdataNameResolver.Resolve(closure, name) -> BsSelectionEntry?
                                       // walks closure.Files in order - local always beats imported
  .BuildIdIndex(closure) / .BuildGroupIdIndex(closure) / .BuildProfileIdIndex(closure)
                                       // id -> entry/group/profile maps spanning the closure, used
                                       // only to follow entryLinks/infoLinks during mapping.
                                       // BuildIdIndex/BuildGroupIdIndex read only closure.Files -
                                       // the game system is deliberately unreachable through
                                       // ordinary link resolution, so a link meant to dangle (into
                                       // generic Warlord/Enhancements/Crusade content) keeps
                                       // silently skipping. BuildProfileIdIndex additionally merges
                                       // in closure.GameSystem?.SharedProfiles - the one thing real
                                       // entries actually link into there.

BsdataDatasheetMapper.BuildDatasheet(entry, idIndex, groupIdIndex, profileIdIndex) -> Datasheet
                                       // single recursive tree walk producing Statlines/
                                       // WeaponProfiles/Abilities from the resolved entry - the same
                                       // code path handles both single-model and squad entries with
                                       // no explicit branch. FactionKeywords/Keywords are always
                                       // empty - no requirement governs mapping BSData's
                                       // categoryLinks to them, left unattempted. See the method's
                                       // own doc comment for entryLink/infoLink resolution mechanics
                                       // and ancestry-list threading.
```

**Ability Extraction and Classification** (`resolve-enhancement-abilities`): an "Abilities" profile
is Intrinsic (in `Datasheet.Abilities`) unless the nearest entry that made it reachable is
`"type": "upgrade"`, in which case it's optional — routed to the on-demand index
(`TryResolveAbility`/`OptionalAbilityNames`, mirroring `WeaponProfile`) and further split into
Enhancement vs. plain OptionalGrant by whether the nearest enclosing group's Name contains
"Enhancements" (substring match). Fixes a real bug where unselected Enhancements leaked onto every
eligible unit — see `ProcessAbilityProfile`'s own doc comment for the exact signal and the rejected
alternative (primary-catalogue/detachment gating).

**Attachment-Eligibility Ability Exclusion** (`resolve-known-ability-effects`): an ability named
exactly "Leader", "Support", or "Attached Unit" never appears in `Datasheet.Abilities`, regardless
of whether it was classified Intrinsic or Core Rule (`AbilityOrigin`, catalogue-context.md) — these three names only ever restate
attachment eligibility a resolved army roster's own attachment relationships (Leader/Support +
Bodyguard, see "Roster Context" in roster-context.md) already represent directly. Filtered once, centrally, in
`Datasheet`'s own constructor (`ExcludedAttachmentAbilityNames`) rather than at each mapper's own
call site — both this pipeline and the BattleScribe/NewRecruit pipeline (battlescribe-import-pipeline.md) already funnel
every Ability they build through this one constructor, so this is a genuine single injection point
covering both, not per-pipeline duplication. A corpus-scan test
(`LeaderSupportAttachedUnitNameScanTests`) confirms every real occurrence of these three exact
names across the live BSData clone (849 on the first run) genuinely mentions attachment/"attach" in
its own text — no same-named ability with different intent found.

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
Abilities"; NewRecruit lists them as "Rules". Text isn't carried on the profile — it's resolved via
the closure's `RuleGlossary` (`rules-glossary-popovers`), keyed by the infoLink's own name; the
display name additionally appends any `"append"` modifier value found on the link (e.g. "Deadly
Demise" + "D3"). Extracted with Origin CoreRule or ArmyRule (below) — always exposed like Intrinsic,
never on-demand. See `ProcessRuleInfoLink`'s own doc comment for the exact resolution mechanics,
including why the identical infoLink shape nested inside a weapon's own wargear-option entry (real:
Impulsor's "Ironhail Skytalon Array") is a different, unrelated thing (that weapon's own keyword
cross-reference) and must be excluded rather than extracted.

Since `gate-and-dedupe-core-rule-abilities`, the resolved rule's own `hidden`/`modifiers` are also
checked (via `IsGameModeGated`, below) before building the Ability — a Core rule can be
chapter/sub-faction-exclusive (fixed a real bug where a Black Templars roster showed both Oath of
Moment and its own exclusive replacement, Templar Vows, together). A gated reference produces no
Ability, the same silent-skip as an unresolvable name; this gating has no bearing on Origin.

**Core Versus Army Rule Origin Classification** (`classify-known-army-rules`): Origin is `ArmyRule`
when the resolved rule's Name matches the known army-wide rule names for the roster's Faction, via
`ArmyRuleNameLookup.Resolve` (`Domain.Catalogue`, decoupled from Bsdata so the BattleScribe pipeline
can reuse it) — `CoreRule` otherwise, threaded through `ResolvedBsdataCatalogue.ResolveDatasheet`
into `BuildDatasheet`'s `knownArmyRuleNames` parameter (fail-open default: empty). This replaced an
earlier structural-only signal that real data proved unreliable — several mustering/composition
rules shared the same gating shape as genuine army rules and were misclassified. See
`ArmyRuleNameLookup`'s own doc comment for the curated inclusion/exclusion tables and the specific
false-positive names, and `classify-known-army-rules/design.md` for why the two signals couldn't
just be OR'd together. A separate scan (`ArmyRuleNameCoverageScanTests`) requires every real
playable-faction file to carry an explicit entry (populated, or deliberately empty) — added after
the initial table turned out to omit roughly half the corpus.

A third infoLink type, `"infoGroup"`, was found by this change's corpus scan (129 occurrences) and
is a confirmed, real, not-yet-fixed gap of the same class (targets a `sharedInfoGroups`/
`infoGroups` container with real Abilities content — Adeptus Custodes' "Talons" holds two genuine
mutually-exclusive auras). Deliberately allowlisted (no real infoGroup-carrying export was
available to verify against), tracked as a follow-up. See
`tests/.../CorpusScan/InfoLinkTypeAllowlist.cs`.

**Full-Corpus InfoLink-Type Scan** (`resolve-core-rule-abilities`): same permanent,
manually-triggered `[Fact(Explicit = true)]` pattern as the other CorpusScan tests (below) — added
because "rule" was itself once a silently-dropped infoLink type. Walks every closure's `infoLinks`
at every level, collecting each distinct `Type` value, and asserts that set against
`InfoLinkTypeAllowlist.cs` (`"profile"`, `"rule"` handled; `"infoGroup"` a confirmed, tracked gap).

```
InvulnerableSave(MeleeInSv, RangedInSv)   // Domain/Catalogue/InvulnerableSave.cs; sealed record,
                                       // pure value, no caveat concept of its own (since
                                       // unify-invulnerable-save-characteristic-view) - carried on
                                       // Statline.InSv wrapped in an InvulnerableSaveCharacteristicView
                                       // (None, below = absent).
  None                                  // static readonly (0, 0) - mirrors DiceExpression.D3/D6's
                                       // own preset convention for a specific, named, frequently-
                                       // constructed value (added post-implementation after this
                                       // exact value was found recurring at real call sites - both
                                       // mappers' "no InSv text"/"explicitly N/A" branches,
                                       // Statline.InSv's own default)
  implicit operator InvulnerableSave(int)   // uniform (melee==ranged) is the common case, mirrors
                                       // DiceExpression's own implicit conversion

InvulnerableSaveCharacteristicView(OriginalValue, DerivedValue, ContributingAbilities)
                                       // Domain/Catalogue/CharacteristicView.cs (see "Characteristic
                                       // Value Domain Model" in characteristic-value-domain-model.md) - what Statline.InSv is actually
                                       // typed as. ContributingAbilities holds a resolved footnote's
                                       // linked Ability (parse-time source, below) or a matched
                                       // statline-flag-rules Ability (live source, see statline-flag-rules.md) uniformly -
                                       // no distinguishing shape between the two. IsCaveated is
                                       // `DerivedValue is null`.
  Value -> InvulnerableSave            // `IsCaveated ? OriginalValue : DerivedValue!` - the value a
                                       // caller should actually use/display; the render layer reads
                                       // this rather than re-deriving the same selection itself
                                       // (added post-implementation, same pattern
                                       // ScalarCharacteristicView.Value uses)
  implicit operator InvulnerableSaveCharacteristicView(int)   // uniform, non-caveated, no
                                       // contributing abilities - preserves the `InSv = 4` ergonomics
                                       // fixtures/tests had before InSv became a view; forwards to
                                       // Resolved (below)
  Resolved(InvulnerableSave) -> InvulnerableSaveCharacteristicView
  Resolved(InvulnerableSave, IReadOnlyList<Ability>) -> InvulnerableSaveCharacteristicView
                                       // static factories for the fully-known ("not caveated") shape
                                       // - named rather than positional ctor overloads so the call
                                       // site states its own semantics (matches DiceExpression.Fixed's
                                       // convention). 1-arg forwards to 2-arg with []. Added post-
                                       // implementation after this exact shape was found duplicated
                                       // verbatim as a private helper in both BsdataDatasheetMapper
                                       // and BattleScribeRosterMapper, plus hand-rolled a third time
                                       // in an Examples/ fixture - real production duplication, not
                                       // just test-file repetition. Both mappers' own private
                                       // ResolvedInSv helpers now delegate to the 1-arg form. The
                                       // 2-arg form was added in a second pass, generalizing what was
                                       // first (incorrectly) treated as a third, unpromoted shape -
                                       // "resolved" (IsCaveated false) and "has contributing
                                       // abilities" are independent facts, and a matched, fully-
                                       // understood StatlineFlagRule (e.g. Shield Dome) is resolved
                                       // by definition, just with its own ability recorded; used
                                       // directly by ShieldDomeStatlineFlagRule.Apply.
  Caveated(InvulnerableSave, Ability) -> InvulnerableSaveCharacteristicView
                                       // static factory for the one-unresolved-contributor shape -
                                       // same promotion rationale as Resolved; both mappers' own
                                       // private CaveatedInSv helpers now delegate here.
  None                                  // static readonly, built from InvulnerableSave.None via
                                       // Resolved - same promotion/preset rationale as
                                       // InvulnerableSave.None above. Every real (0, 0)/absent call
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
                                       // to, by id - never by name alone, since the base catalogue
                                       // can define two profiles sharing the same name with opposite
                                       // meanings (ranged vs. melee). Searches ancestry nearest-first,
                                       // since the corpus doesn't always co-locate the link with the
                                       // footnoted characteristic. Throws on no match under either of
                                       // the two recognized naming conventions (see the method's own
                                       // doc comment) - Aeldari's Archon/Ynnari Archon is a confirmed
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
                                       // splits Keywords text (e.g. "Anti-infantry 4+, Devastating
                                       // Wounds") on commas; every token joins
                                       // WeaponProfile.KeywordsText verbatim, and exact-match
                                       // tokens also set the corresponding ability flag ("-" means
                                       // none). No alias/synonym recognition ("Cleave" never sets
                                       // Blast); an unflagged mechanic (Hazardous, Precision, Heavy)
                                       // is retained verbatim with no flag.
WeaponKeywordParser.UnrecognizedTokens(keywordsText) -> IReadOnlyList<string>
                                       // every token Apply would leave unflagged - shares Apply's
                                       // own recognition logic via a private TryRecognize helper so
                                       // the full-corpus scan (below) can't drift from Apply.

BsdataDatasheetMapper.IsGameModeGated(modifiers, ctx) -> bool
                                       // WalkEntry/WalkGroup's guard against content BSData marks
                                       // hidden outside "Army Roster" (matched play) mode - Crusade-
                                       // only content and force-gated mechanics. Real 2-value AND/OR
                                       // evaluation of the condition tree (not plain existence -
                                       // fixed a bug that wrongly hid a real always-available
                                       // Enhancement pool). Since gate-and-dedupe-core-rule-abilities,
                                       // also recognizes `scope: "primary-catalogue"` (chapter/sub-
                                       // faction exclusivity), called from ProcessRuleInfoLink on a
                                       // resolved rule's own Modifiers. See the method's own doc
                                       // comment (and IsProvablyAlwaysTrueInMatchedPlay's) for the
                                       // exact evaluation semantics and both real bugs found while
                                       // building this.
```

`BsdataDatasheetMapper.ParseThreshold(text, CharacteristicPattern)` (Sv/BS/WS/LD only — InSv no
longer a caller) validates against a per-characteristic allowlist regex
(`CharacteristicPattern.Sv`/`.Bs`/`.Ws`/`.Ld` — kept separate so a future divergence can't silently
affect the others) rather than stripping known-bad separators, throwing
`AmbiguousCharacteristicException` (carries the characteristic name and raw text) on a mismatch.

`WeaponProfile.KeywordsText` (`IReadOnlyList<string>`, in `Domain/Catalogue/WeaponProfile.cs`, not
`Bsdata/`) is the one shape change this work made outside the new namespace: every weapon's exact
source keyword text, in source order, included in `EqualityKey()` so rules-identical weapons
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
                                       // Directory.Exists-guards the hardcoded clone path and
                                       // Assert.Skip()s when absent, so a machine without the
                                       // clone reports skipped, never a bare pass.
LiveClone.CatalogueFileNames(source) -> IReadOnlyList<string>
                                       // every file via ListFileNames(), excluding the non-
                                       // catalogue "Warhammer 40,000.json"
AllowlistEntry<T>(Description, Matches: Func<T, bool>)
AllowlistCheck.AssertClean(results, allowlist, describeResult)
                                       // bidirectional check shared by both scans: fails on any
                                       // unmatched result OR any allowlist entry that matched
                                       // nothing this run - a fixed issue can't go stale either

CharacteristicResolutionScanTests.Full_corpus_characteristic_resolution_scan
                                       // every catalogue file gets a turn as its own closure's
                                       // starting file; every top-level SharedSelectionEntries
                                       // is attempted through BuildDatasheet, collecting exceptions.
                                       // CharacteristicResolutionAllowlist.cs covers dice-notation
                                       // WeaponProfile.S text (e.g. Ork Battlewagon "D6+6") failing
                                       // ParsePlainInt, plus one real anomaly (Aeldari's Archon/
                                       // Ynnari Archon - see ResolveCaveatAbility above).
                                       // `structured-invulnerable-save` resolved every other InSv
                                       // shape this scan had allowlisted, and caught two real
                                       // architectural gaps it surfaced (the base rules game-system
                                       // file being unreachable at all; the ancestor-vs-immediate-
                                       // entry ability-resolution gap), both fixed generally.

WeaponKeywordScanTests.Full_corpus_weapon_keyword_token_scan
                                       // walks each file's closure directly (not through
                                       // BuildDatasheet, since ResolveWeaponProfile is on-demand
                                       // only), collecting every weapon's Keywords text and running
                                       // UnrecognizedTokens over it. WeaponKeywordAllowlist.cs seeds
                                       // 45 tokens found on the first run, none fixed in
                                       // WeaponKeywordParser (out of scope) - grouped into no-flag
                                       // tokens, case/punctuation variants, dice-valued variants of
                                       // an int-only mechanic, and genuinely unmodeled mechanics (a
                                       // few widespread enough - Extra Attacks, Psychic, Lance, One
                                       // Shot - to be worth a future addition). See PROGRESS.md.
```
