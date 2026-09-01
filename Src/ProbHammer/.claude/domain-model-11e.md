# Domain Model — 11th Edition (Attached Units)

Full requirements: `openspec/changes/attached-unit-domain-model/`. This file summarizes the shape
that landed in code.

**Status:** implemented and tested (`src/ProbHammer.Core/Domain/`), proven against hand-built
fixtures plus a real BSData JSON loader (`Domain/Catalogue/Bsdata/` — see "BSData JSON Ingestion"
below). `ProbHammer.Web`/`/LivePlay` is fully wired: a pasted GW-app 11e export is parsed
(`Domain/Import/`), resolved against BSData (`ArmyRosterEnricher`), and rendered — `Examples/`
fixture data is unreferenced by `Web` but kept in-tree for future domain-model exploration.
`/Import` also recognizes a BattleScribe/NewRecruit roster JSON export and routes it through a
second, independent pipeline (`Domain/Import/BattleScribe/` — see that section below) that
synthesizes this same model directly from the JSON's own already-resolved data, no BSData
involved. Coexists with the untouched, fully-superseded 10th-edition model
(`.claude/domain-model.md`) — see `PROGRESS.md` for that history.

---

## Namespaces

- `ProbHammer.Core.Domain.Catalogue` — reference/rules data (Catalogue context)
- `ProbHammer.Core.Domain.Roster` — army-list composition and live game state (Roster context)

---

## Catalogue Context

```
Datasheet
  Name, FactionKeywords, Keywords, Abilities (unit-wide, intrinsic only - see below)
  Statlines: IReadOnlyList<(string Name, Statline Statline)>   // ordered - the Sergeant/leader
                                                      // entry is declared first, matching the real
                                                      // NewRecruit/GW app export convention; a
                                                      // documented guarantee, not incidental
  GetStatline(name) -> Statline         // O(1), backed by an internal Dictionary built once from
                                         // the ordered list
  ResolveWeaponProfile(name) -> WeaponProfile   // on-demand only, never enumerated
  TryResolveAbility(name) -> Ability?   // on-demand only, mirrors ResolveWeaponProfile - resolves
                                         // an Enhancement or other optional ability grant nested in
                                         // its own selection entry (e.g. Impulsor's "Shield Dome")
                                         // that the public Abilities list excludes
  OptionalAbilityNames: IReadOnlyList<string>   // diagnostic "did you mean...?" source only,
                                                 // mirrors WeaponNames - never a full options menu
  ctor(..., statlines: IReadOnlyList<(string Name, Statline Statline)>, weaponProfiles: IEnumerable<WeaponProfile>,
       optionalAbilities: IEnumerable<Ability>? = null)
                                         // weaponProfiles/optionalAbilities keyed internally by
                                         // .Name; optionalAbilities defaults to empty

Statline(M, T, Sv, W, Ld, Oc)           // value object — field names match official shorthand
  InSv: InvulnerableSave                // init-only, defaults to absent; see "Invulnerable Save
                                         // Resolution" below

DiceExpression(Count, Sides, Modifier)  // value object — fixed int (Count=0) or dice roll
                                         // ("D6", "2D3+1")
  Parse(string) -> DiceExpression       // "3" | "D6" | "2D3+1"
  implicit operator DiceExpression(int) // plain ints convert to a fixed value at call sites
  D3, D6                                // static presets for the common single-die case
  operator +(DiceExpression, int)       // non-mutating, e.g. D6 + 2 -> "D6+2"
  Scale(n), Add(other)                  // multi-model attack/damage aggregation
  ExpectedValue() -> double             // Count==0 ? Modifier : Count*(Sides+1)/2.0 + Modifier;
                                         // used only to order weapons by aggregate attacks in
                                         // /LivePlay - DiceExpression has no natural total order

WeaponProfile(Name, Type, Range, A, S, Ap, D)   // abstract value object; A/D are DiceExpression
                                                 // (fixed or dice, e.g. Multi-melta D6 damage);
                                                 // S/Ap stay plain int
  Skill: int                            // abstract computed property, see below
  Torrent, Blast, Melta, RapidFire, SustainedHits, LethalHits, DevastatingWounds,
    TwinLinked, IndirectFire, Pistol, IgnoresCover, Assault, Anti
                                         // init-settable ability flags, flattened directly onto
                                         // WeaponProfile (no nested WeaponAbilities type)
  EqualityKey() -> WeaponProfileEqualityKey   // (Type, Skill, S, Ap, D, ability fields) - excludes
                                               // Name/Range/A - groups weapon instances by profile,
                                               // not identity

RangedWeapon(Name, Range, A, Bs, S, Ap, D) : WeaponProfile   // Type fixed Ranged; Skill => Bs
MeleeWeapon(Name, A, Ws, S, Ap, D) : WeaponProfile           // Type fixed Melee, Range fixed 0;
                                                              // Skill => Ws

Ability(Name, Text, Choices: IReadOnlyList<AbilityChoice>, Scope: Model | Unit, Origin: AbilityOrigin)

AbilityOrigin = Intrinsic | Enhancement | OptionalGrant | CoreRule | ArmyRule
```

`AbilityOrigin` classifies where an ability came from during `BsdataDatasheetMapper`'s walk, not a
player-facing concept the export text states directly. **Intrinsic**: a datasheet-wide fact,
always in `Datasheet.Abilities`. **Enhancement**: nested inside a `type: upgrade` selection entry
whose nearest enclosing group's Name contains "Enhancements" (case-insensitive substring — needed
for a nested sub-pool like "Legends of Saga and Song Enhancements", not just a group literally
named "Enhancements"). **OptionalGrant**: any other ability nested inside its own `type: upgrade`
entry (e.g. Impulsor's "Shield Dome"). **CoreRule**: a datasheet-wide reference to a
separately-defined Core/faction rule (Oath of Moment, a Chapter's own Vows, Deadly Demise, Firing
Deck, Infiltrators, Scouts) — always exposed like Intrinsic, never one of the on-demand optional
grants, since it's never something a player selects. **ArmyRule**: a CoreRule-shaped reference
whose Name matches `classify-known-army-rules`' curated per-faction `ArmyRuleNameLookup` table
(e.g. Oath of Moment, Templar Vows, Nurgle's Gift (Aura)) — see "Core Versus Army Rule Origin
Classification" below. Enhancement and OptionalGrant abilities are resolvable only via
`Datasheet.TryResolveAbility`, never enumerated in the public `Abilities` list.

**Key design points:**
- A Datasheet can have any number of named statlines (not "1 or sometimes 2") — a Chaos Space
  Marine datasheet has 5. The same statline name can be referenced by model-lines with different
  weapon eligibility (Sergeant-style).
- Weapon profiles never materialize as a full options menu — ProbHammer only shows wargear that
  was actually chosen.
- `Ability.Scope` is a property of the ability itself, not of its source (Enhancement, wargear,
  intrinsic) — 11e rules text treats those as examples of sources, not special cases.
- `WeaponProfile.EqualityKey()` mirrors `SimulationAdapter.WeaponGroupKey` (10e code) — same
  concept, ported rather than reimplemented.
- `WeaponProfile` is abstract with sealed `RangedWeapon`/`MeleeWeapon` subtypes; `Skill` is an
  abstract *computed* property rather than a stored value — see the class's own doc comment (and
  `Skill`'s) for why.
- Field names (`Statline.M/T/Sv/W/Ld/Oc`, `WeaponProfile.A/S/Ap/D`) intentionally match official
  40k shorthand rather than spelled-out names.
- `DiceExpression` lives in `Domain.Catalogue`, not `Simulation/*` where it originated — it's a
  game-rules quantity `WeaponProfile.A`/`D` depend on directly. `Simulation/*` now references it
  via a `using`-only repoint (`promote-dice-expression-to-domain`); this does **not** mean
  `Simulation/*` is wired onto this domain model — see Deliberate Omissions below.
- `DiceExpression`'s implicit `int` conversion lets fixed-value weapon stats be written as plain
  integers; variable stats still use `DiceExpression` explicitly (`DiceExpression.D6 + 2`). Chosen
  over constructor overloads on `RangedWeapon`/`MeleeWeapon` because `A` and `D` vary independently
  between fixed and dice-based (e.g. Multi-melta: fixed `A`, dice `D`) — overloads would need one
  combination per shape; the implicit conversion applies per-argument instead.
- `Datasheet`'s constructor takes `weaponProfiles`/`statlines` as plain enumerables/an ordered
  list rather than pre-built dictionaries — see the constructor's own doc comment for why (a real
  fixture-drift bug, and why declared statline order is a tested guarantee, not an accident). Two
  differently-named statlines can share identical `M/T/Sv/W/Ld/Oc` values (e.g. "Assault
  Intercessor" / "Assault Intercessor Sergeant") — why `AttachedUnitAggregator.BuildStatlines`
  dedupes by name, not by value.

---

## BSData JSON Ingestion

Full requirements: `openspec/changes/catalogue-json-ingestion/`. Raw file format (not the C# types
below): `.claude/bsdata-json-schema.md`. Namespace: `ProbHammer.Core.Domain.Catalogue.Bsdata`.
Reads the BattleScribe `catalogueSchema` JSON shape (one file per faction/sub-faction) that
BSData's 11th Edition repository publishes, and resolves it into the
`Datasheet`/`Statline`/`WeaponProfile`/`Ability` shapes above. Resolves names to rules data only —
does not build the `Unit`/`AttachedUnit`/`ModelLine` graph (`ArmyRosterEnricher`'s job, see "Army
List Import Pipeline" below). Wired into `ProbHammer.Web`/`/LivePlay` via the app-wide
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
of whether it was classified Intrinsic or Core Rule above — these three names only ever restate
attachment eligibility a resolved army roster's own attachment relationships (Leader/Support +
Bodyguard, see "Roster Context" below) already represent directly. Filtered once, centrally, in
`Datasheet`'s own constructor (`ExcludedAttachmentAbilityNames`) rather than at each mapper's own
call site — both this pipeline and the BattleScribe/NewRecruit pipeline (below) already funnel
every Ability they build through this one constructor, so this is a genuine single injection point
covering both, not per-pipeline duplication. A corpus-scan test
(`LeaderSupportAttachedUnitNameScanTests`) confirms every real occurrence of these three exact
names across the live BSData clone (849 on the first run) genuinely mentions attachment/"attach" in
its own text — no same-named ability with different intent found.

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
InvulnerableSave(MeleeInSv, RangedInSv, Caveated, CaveatAbility)   // Domain/Catalogue/
                                       // InvulnerableSave.cs; sealed record, non-nullable on
                                       // Statline (0/0/false/null = absent). CaveatAbility is null
                                       // except when Caveated == true, in which case it's always
                                       // set - enforced by the 4-arg constructor (an object
                                       // initializer or `with` can still bypass this).
  implicit operator InvulnerableSave(int)   // uniform (melee==ranged, non-caveated) is the common
                                       // case, mirrors DiceExpression's own implicit conversion

BsdataDatasheetMapper.ResolveInvulnerableSave(text, ancestry, ctx) -> InvulnerableSave
                                       // Resolves a Unit profile's raw InSv text into plain,
                                       // attack-type-restricted, footnoted-caveated, or - since
                                       // resolve-known-ability-effects - footnoted-and-resolved
                                       // shapes. A footnoted value's linked ability is still
                                       // resolved by id only, never interpreted inline here; the
                                       // resulting Ability's own Text is then handed to
                                       // InvulnerableSaveCaveatClassifier (below), using its
                                       // resolved melee/ranged split only when it returns a match -
                                       // otherwise falling back to today's caveated result
                                       // unchanged. See the method's own doc comment for the exact
                                       // shapes recognized and real examples.

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
                                       // entry, allowlisted rather than special-cased.

InvulnerableSaveCaveatClassifier.TryResolveBare(abilityText) -> (Melee, Ranged)?
InvulnerableSaveCaveatClassifier.TryResolveSplit(abilityText, footnotedDigit, plainDigit) -> (Melee, Ranged)?
                                       // Domain.Catalogue (not Bsdata - the BattleScribe pipeline,
                                       // below, needs it too and must not depend on the Bsdata
                                       // namespace). Matches abilityText as an exact, anchored,
                                       // whole-string match against a fixed set of four known
                                       // templates ("This model has a {N}+ invulnerable save
                                       // against {ranged|melee} attacks." / "Models in this unit
                                       // have a {N}+ invulnerable save against {ranged|melee}
                                       // attacks.") - a real BSData authoring quirk (Space Marines'
                                       // Judiciar) uses a U+00A0 no-break space in place of one
                                       // plain space mid-template, normalized away before matching
                                       // rather than left to silently miss resolution. TryResolveBare
                                       // (a single footnoted value, e.g. "5+*") has no digit to
                                       // compare against, so a template match always wins.
                                       // TryResolveSplit (a melee/ranged pair with one footnoted
                                       // side) additionally requires the template's own named value
                                       // to equal footnotedDigit - a real mismatch (confirmed:
                                       // Judiciar's own footnoted "4+*" side links to an ability
                                       // naming a different digit) correctly stays caveated rather
                                       // than trusting either value. Both called from the exact
                                       // point each pipeline's own resolver already computes
                                       // today's fallback, using the result only when non-null.
                                       // Orks' Makari (a re-roll restriction, not an attack-type
                                       // split) is the one confirmed real anomaly that correctly
                                       // never matches any template - see
                                       // InvulnerableSaveCaveatResolutionScanTests.

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

---

## Rules Glossary & Popovers

Full requirements: `openspec/changes/rules-glossary-popovers/`. Resolves BSData's `sharedRules`
(game-system level) and `rules` (faction/library level) arrays — previously read by no code path —
into a name/alias-queryable glossary, and mechanically extracts `[BRACKET]`-style cross-reference
tokens from ability/rule text for lookup against it. Text lookup and display only, never
behavioral interpretation, consistent with "ability text is never auto-parsed into behavior"
(Deliberate Omissions, below). Wired into `/LivePlay`: every weapon-keyword chip and ability name
is a tap trigger opening a native Popover-API panel with that keyword's/ability's full rule text;
a resolved `[BRACKET]` reference inside becomes a further nested trigger.

```
RuleDefinition(Name, Aliases, Text)   // Domain/Catalogue/Bsdata/RuleDefinition.cs - one BSData
                                       // sharedRules/rules entry, carried through as opaque text

RuleGlossary                          // Domain/Catalogue/Bsdata/RuleGlossary.cs
  Build(BsdataClosure closure) -> RuleGlossary
                                       // iterates closure.Files in resolution order (local beats
                                       // imported) collecting Rules, then GameSystem?.SharedRules,
                                       // indexed by a Normalize()d key built from both Name and
                                       // every Alias - first occurrence wins. Indexing by Name too
                                       // is what makes a rule with no declared Alias at all (real
                                       // gaps: "Cleave", "Close-quarters") still resolve. See the
                                       // method's own doc comment for the full rationale.
  TryResolve(string nameOrAlias) -> RuleDefinition?   // non-throwing, same Normalize pipeline

  Normalize(string text) -> string    // private; every Name/Alias and query goes through this
                                       // 4-step pipeline (lowercase, strip trailing value/dice
                                       // suffix, collapse "Anti-*" to bare "anti", strip non-
                                       // alphanumeric) since real text references a generic
                                       // mechanic's bare name with a value or target category
                                       // appended (e.g. "SUSTAINED HITS 1"). See the method's own
                                       // doc comment for the exact ordering constraints (why each
                                       // step must run before the next) and real examples. Landing
                                       // this dropped the full-corpus scan's (below) unresolved
                                       // bracket-token count from 1,964 to exactly 2.

RuleTextTokenizer                     // Domain/Catalogue/Bsdata/RuleTextTokenizer.cs
  ExtractBracketTokens(text) -> IReadOnlyList<string>
                                       // simple non-nested "[...]" regex extraction - markup
                                       // characters are never a reference signal by themselves.
                                       // Normalizes each captured token (below).
  Normalize(string token) -> string   // public, reused by RuleTextEmphasisRenderer - strips
                                       // markup/Unicode noise from a bracket's raw inner text; a
                                       // "light" pass distinct from RuleGlossary's own "aggressive"
                                       // Normalize (a bracket goes through this first, then that
                                       // when resolving). See the method's own doc comment for the
                                       // exact characters handled.

RuleTextEmphasisRenderer              // Domain/Catalogue/Bsdata/RuleTextEmphasisRenderer.cs
  Render(text, renderBracket: Func<string, (string Inline, string Popovers)>)
    -> (string Inline, string Popovers)
                                       // Renders "*italic*"/"**bold**"/"^^small-caps^^" markup
                                       // (including a nested "***bold+italic***" split-close case)
                                       // as HTML styling only - never an interactive trigger by
                                       // itself. Every [BRACKET] token is handed to the caller's
                                       // renderBracket delegate, never resolved here, keeping this
                                       // class free of RuleGlossary/popover-id concerns. Returns
                                       // inline HTML separately from popover markup so a caller can
                                       // emit panels as flow-content siblings rather than nesting
                                       // them inside this text's own emphasis wrapping (which would
                                       // leak styling into a nested popover's inherited CSS).

ResolvedBsdataCatalogue.Glossary: RuleGlossary
                                       // built alongside the rest of ResolvedBsdataCatalogue.Build
                                       // - no new cache, rides BsdataCatalogueCache for free
```

**`/LivePlay` wiring** (`ProbHammer.Web`): `IArmyRosterProvider.Build` returns
`ArmyRosterBuildResult(ArmyRoster Roster, RuleGlossary Glossary)` (was bare `ArmyRoster`) so both
`LivePlayModel.OnGet` and `LivePlayCasualtyService`'s fragment re-render have the glossary;
`UnitBlockRenderModel` carries it as a third field. Since `display-army-header-and-detachment-rules`,
the trigger/panel-building logic previously local to `_UnitBlock.cshtml`
(`BuildRulePopover`/`NextPopoverId`/`RenderNestedReference`) is extracted into
`ProbHammer.Web.Rendering.RulePopoverRenderer` — one instance per render pass, constructed with the
`RuleGlossary` plus an explicit id-scope prefix (`"u{i}"` per unit block, `"hdr"` for the header)
instead of relying implicitly on the unit's page index, so `_ArmyHeader.cshtml` (below) can produce
identical markup with no unit index to scope against. `_UnitBlock.cshtml`'s own
`BuildRulePopover` is now a one-line forwarder. Every weapon-keyword chip and ability name is
looked up via `TryResolve`; a match becomes a `<button popovertarget="p-{id}">` trigger with a
sibling `<div id="p-{id}" popover="auto">` panel (never nested — a `<button>` can't contain
interactive content), positioned via a per-instance `anchor-name`/`position-anchor` CSS
custom-property pair rather than the newer `anchor="..."` HTML attribute (real device testing
found the latter unsupported where the former's properties at least register — see design.md's
"Popover mechanism" Risks section). `RenderNestedReference` recurses into a resolved reference's
own `RuleDefinition.Text` to arbitrary depth (confirmed live to 3 levels), threading a
`shownRuleNames` ancestor-chain set so a bracket resolving back to an already-open rule in its own
chain renders as plain text — several generic mechanics self-reference their own name
(`Sustained Hits`, `Anti`, `Cleave`), and an unguarded first version stack-overflowed rendering a
real weapon's chip.

**Army header** (`display-army-header-and-detachment-rules`): `LivePlay.cshtml` renders a new
`_ArmyHeader.cshtml` partial above the unit-block loop, fed
`ArmyHeaderRenderModel(ArmyHeaderViewModel Header, RuleGlossary Glossary)` — mirrors
`UnitBlockRenderModel`'s pairing. `LivePlayModel.BuildArmyHeader(roster)` builds the view model from
the roster's own metadata (Name/Faction/BattleSize/PointsSpent/PointsLimit/ForceDisposition) plus
two aggregations: the roster's resolved `Detachments` (carried through unchanged), and
`BuildArmyRules(roster)` — every distinctly-named `ArmyRule`-origin ability present on ANY unit,
deduped by Name, computed by walking every component's own `Datasheet.Abilities` directly (NOT
through `AttachedUnitAggregator`'s present-only view) — deliberately independent of casualty state,
since an ArmyRule is a fact about army identity, not which models are alive, so this header entry
never disappears mid-game. A new aggregation, separate from
`AttachedUnitAggregator.PromoteArmyRuleAbilities`'s existing per-unit dedup, which is unchanged —
the per-unit box keeps rendering alongside the new header. The rules section lays out two columns
(Army/Detachment) via the same `<thead><th>` convention the weapon tables use; a Detachment renders
its own name once as plain text with one `RulePopoverRenderer`-built trigger per resolved rule
beneath it. No DP/points cost rendered anywhere on the page.

### Full-Corpus Bracket-Token-Resolution Scan

`tests/ProbHammer.Tests/Domain/Catalogue/Bsdata/CorpusScan/BracketTokenResolutionScanTests.cs` —
same permanent, manually-triggered `[Fact(Explicit = true)]` pattern as the other CorpusScan tests.
Walks every closure's local `rules` text, the game system's `sharedRules` text (once, not per
starting file), and every locally-defined entry's resolved `Ability.Text`, extracting bracket
tokens and resolving each against that closure's glossary. The first run (before `RuleGlossary`'s
normalized-key resolution existed) found 1,964 unresolved occurrences across 71 tokens; after that
landed, exactly 2 remain, individually allowlisted in `BracketTokenResolutionAllowlist.cs` as
one-off BSData authoring anomalies (a bracket-placement typo in one Custodes ability; a stray
non-reference bracket in one Blood Angels ability) — neither fixed, since no general rule could
resolve either without risking a wrong fix elsewhere.

---

## Army List Import Pipeline

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
                                       // shapes - see Roster Context below

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
`ResolvedDetachment` shape (see "Roster Context" below).

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
and `BattleScribeArmyImport(BsRoster)` (see the BattleScribe pipeline section below) — so
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
                                       // below); on a match wraps as BattleScribeArmyImport,
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

---

## BattleScribe/NewRecruit JSON Import Pipeline

Full requirements: `openspec/changes/import-battlescribe-json-rosters/`. A second, independent
import pipeline: `BattleScribe roster JSON text → BattleScribeRosterFormat.TryParse → BsRoster
(session-stored as a BattleScribeArmyImport) → BattleScribeRosterMapper.Map → ArmyRoster`, with no
`Domain.Catalogue.Bsdata`/BSData catalogue involvement anywhere — every
`Datasheet`/`Statline`/`WeaponProfile`/`Ability` is synthesized directly from the roster JSON's
own already-resolved `profiles`/`rules`, since (unlike a GW-app text export) BattleScribe/NewRecruit
JSON already carries every characteristic, weapon profile, and rule text fully resolved inline.
Serves NewRecruit-only users (whose per-army-book content in the official GW app is paywalled)
directly, and gives Android GW-app users a reliable alternative when the native text export hits
one of that pipeline's own still-open ambiguities: export from GW app, clean up in NewRecruit,
export as JSON. Analyzed against exactly one real captured sample
(`data/gw-app-export-templars.json`) — several shapes below remain UNVERIFIED pending a second,
independently-sourced sample.

Namespace: `ProbHammer.Core.Domain.Import.BattleScribe`, JSON DTOs in its own `Json`
sub-namespace.

```
Json/BsRosterFile.cs                  // System.Text.Json-backed types for the subset of the
                                       // BattleScribe rosterSchema this pipeline reads - BsRoster
                                       // (Xmlns, Name, Costs, CostLimits, Forces), BsRosterForce
                                       // (CatalogueName, Rules, Selections), BsRosterSelection (Id,
                                       // Name, Type, Number, Profiles, Rules, Costs, Selections,
                                       // Associations - the same shape reused at every nesting
                                       // level), BsRosterProfile (+ CharacteristicText(name)),
                                       // BsRosterCharacteristic ($text via [JsonPropertyName]),
                                       // BsRosterCost (Name, Value - shared shape for roster totals
                                       // and a selection's own cost tags, e.g. "Enhancements",
                                       // "Detachment Points"), BsRosterAssociation (Type, To, Name).
                                       // Only what this pipeline needs is modeled; everything else
                                       // silently ignored on deserialize.

BattleScribeRosterFormat.TryParse(text, out BsRoster?) -> bool
                                       // Format recognition: true only for JSON containing a
                                       // top-level "roster" object whose "xmlns" equals the
                                       // standard cross-tool BattleScribe roster schema URL - never
                                       // throws, so non-matching text simply isn't recognized
                                       // (falls through to the GW-app text pipeline). Uses a
                                       // CamelCase/case-insensitive JsonSerializerOptions, matching
                                       // BsdataCatalogueReader's own convention.

BattleScribeRosterParseException(message)
                                       // thrown by the mapper when a recognized payload doesn't
                                       // resolve into an ArmyRoster (e.g. a selection with no
                                       // resolvable Unit-typeName profile anywhere reachable) -
                                       // mirrors ArmyListParseException's role
```

**`BattleScribeRosterMapper.Map(BsRoster) -> ArmyRoster`**: army metadata comes from
`roster.costs`/`costLimits`' own "pts" entry (PointsSpent/PointsLimit) and from splitting
`force.catalogueName` on " - " (e.g. "Imperium - Adeptus Astartes - Black Templars" -> 3 segments)
for Faction — not required to match `BsdataFactionResolver`'s own suffix-matching convention, since
that resolver is never invoked here. Detachments/ForceDisposition/BattleSize come from the
"Detachment"/"Force Disposition"/"Battle Size" top-level selections' own nested selection name(s);
BattleSize additionally strips a trailing parenthetical (e.g. "Incursion (1000 Point limit)" ->
"Incursion", matching the text pipeline's convention despite different parenthetical wording).

Attachment resolution (`BuildUnits`): every top-level "unit"/"model" selection's own outgoing
`associations[].name` in `("Leading","Supporting")` targets another top-level selection by id — the
target becomes a Bodyguard, every selection targeting it becomes Attached, and a selection with
neither association is standalone. Confirmed against the sample's three real associations (two
2-member groups, one Leader-only group) — a 3-member group remains UNVERIFIED pending a second
sample.

Unit assembly (`BuildUnit`): "Abilities"-typeName profiles become Intrinsic; "rules" entries become
CoreRule- or ArmyRule-origin, deliberately with NO chapter/mode gating (a roster JSON only ever
contains rules already applicable to the exported army). Origin uses the same
`ArmyRuleNameLookup.Resolve(Faction).Contains(rule.Name)` check the BSData pipeline uses, so
`AttachedUnitAggregator.PromoteArmyRuleAbilities`'s cross-component dedup (e.g. "Templar Vows")
applies here too. Enhancement resolution (`FindEnhancements`) walks the whole selection tree for
any descendant carrying a `costs["Enhancements"]` entry — structurally cannot leak an unselected
Enhancement, since a roster JSON only contains actual selections. See `BuildUnit`'s own doc comment
for the exact wiring.

ModelLine assembly (`BuildModelLines`): one loadout ModelLine per immediate "model"-typed nested
selection, each using its own Unit-typeName profile when it has one or falling back to the
top-level selection's own Unit profile when it doesn't (a shared-statline squad, e.g. Sword
Brethren); with no such nested children, the top-level selection is itself a solo model producing
one ModelLine. No partition inference is ever needed — each already-split loadout node carries its
own count directly (this pipeline structurally cannot reproduce `harden-army-list-parsing-for-
android-exports`'s still-open Custodian Guard case). See the method's own doc comment for the
real-data examples.

Weapon/ability collection (`CollectWeaponsAndAbilities`): walks a loadout node's own nested wargear
`selections` recursively, dividing a weapon-carrying child's own `number` by the loadout's model
count to recover the per-model quantity. A selection with neither weapon nor "Abilities" profiles
(a pure grouping wrapper) is walked one level deeper rather than skipped. An "Abilities"-typeName
child becomes an OptionalGrant-origin Ability on that specific ModelLine, mirroring the BSData
pipeline's equivalent classification; an Enhancement-tagged selection is skipped (resolved once by
`FindEnhancements` above). See the method's own doc comment for the Storm-Bolters-style wrapper
example.

Characteristic-text parsing reuses `DiceExpression.Parse` (A/D) and `WeaponKeywordParser.Apply`
(Keywords) directly — this format's own text shapes are byte-for-byte the same convention BSData
uses. Plain-int/threshold/measurement parsing carries small, functionally-identical local
equivalents rather than reusing `BsdataDatasheetMapper`'s own private (inaccessible) helpers.
Invulnerable-save caveat resolution is this format's own simpler resolution (no BSData-style
entryLink ancestry chain to walk) — UNVERIFIED, since the one real sample analyzed has no caveated
InSv at all. It calls the same shared `InvulnerableSaveCaveatClassifier` (`Domain.Catalogue`, above)
at the exact point it computes today's caveated fallback, exactly mirroring the BSData pipeline's
own wiring (invulnerable-save's "This resolution behavior SHALL be identical regardless of which
import pipeline produced the Ability being matched").

This pipeline's own ability extraction (Statline/Weapon/Ability Extraction above and Core Rule
Extraction below) is subject to the same "Leader"/"Support"/"Attached Unit" exclusion as the BSData
pipeline, via the same central `Datasheet` constructor filter (`resolve-known-ability-effects`) —
`BuildUnit`'s `intrinsicAbilities`/`coreRuleAbilities` are concatenated and passed straight into
`new Datasheet(...)`, the same single injection point the BSData pipeline funnels through, so no
separate filter is needed here.

```
BattleScribeRuleGlossaryBuilder.Build(BsRoster) -> RuleGlossary
                                       // builds a roster-scoped RuleGlossary by walking the WHOLE
                                       // roster once - the force's own `rules`, every top-level
                                       // selection's own `rules`, and every nested wargear
                                       // selection's own `rules` (e.g. a weapon's own "Sustained
                                       // Hits"/"Anti" keyword rule text) - deduped by id, first
                                       // occurrence wins, into RuleDefinitions via RuleGlossary
                                       // .BuildFrom (below). Gives /LivePlay's existing [BRACKET]
                                       // resolution and weapon-keyword popovers working text for a
                                       // BattleScribe-sourced roster too, with ZERO changes to that
                                       // rendering pipeline - /LivePlay only ever needs *a*
                                       // RuleGlossary, not specifically a BSData-sourced one.

RuleGlossary.BuildFrom(IEnumerable<RuleDefinition>) -> RuleGlossary
                                       // indexes an already-known flat set of RuleDefinitions the
                                       // same way as Build(BsdataClosure) (by Name and every Alias,
                                       // normalized, first occurrence wins), for a caller whose
                                       // "closure" isn't a BsdataClosure at all
```

`StoredArmyImport`/`TextArmyImport`/`BattleScribeArmyImport` live in `Domain.Import` itself — see
"Session-Backed Import" above.

---

## Roster Context

```
ModelLine(StatlineName, Weapons: IReadOnlyList<string>, Count, Abilities: IReadOnlyList<Ability>,
          Keywords: IReadOnlySet<string> = [])
                     // Keywords scoped to this specific model-line, distinct from its Datasheet's
                     // Keywords - e.g. a Psyker keyword on one named individual within a shared
                     // statline. Case-insensitive, matching Datasheet.Keywords's convention.
  RemainingCount   // live, adjustable in both directions; alive/dead granularity only, clamped
                   // to [0, Count]
  SetRemainingCount(value)   // the one primitive both directions reduce to, clamped to [0, Count]
  RemoveCasualties(n)         // thin wrapper: SetRemainingCount(RemainingCount - n)

Unit : ICombatUnit
  Datasheet, Enhancements: IReadOnlyList<Ability>, ModelLines
                     // Enhancements holds resolved Ability objects (via ArmyRosterEnricher's
                     // Enhancement resolution above), not raw name strings - was
                     // IReadOnlyList<string>, populated straight from the parsed export with no
                     // cross-reference against the catalogue at all, confirmed unused elsewhere
                     // at the time.
  IsPresent   // any model-line with RemainingCount > 0
  Components -> [this]

AttachedUnit : ICombatUnit
  Bodyguard: Unit, Attached: IReadOnlyList<Unit>   // open collection, not fixed Leader/Support slots
  Components -> [Bodyguard, ..Attached]

ICombatUnit
  Components: IReadOnlyList<Unit>   // Composite-pattern shape; lets aggregate-view logic treat a
                                     // plain Unit and an AttachedUnit uniformly
  Name: string                      // computed, not stored - see Pure functions below
  IsHalfStrengthOverride: bool      // mutable, live-state, player-set only, defaults false.
                                     // Meaningful only when HalfStrengthResolution
                                     // .StartingStrength(this) == 1 (a genuine single-model unit) -
                                     // the real determination there is wound-based, which this app
                                     // doesn't track (Deliberate Omissions); for combined starting
                                     // strength 2+ the computed determination governs instead and
                                     // this value goes unread. One flag per ICombatUnit - an
                                     // AttachedUnit's own flag is unrelated to its Bodyguard's/each
                                     // Attached Unit's own flag.
  IsBattleShocked: bool             // mutable, live-state, player-set only, defaults false; the
                                     // app never simulates the 2D6-vs-Leadership test, only
                                     // records the player's reported result. Never cleared by any
                                     // other domain operation - 11e only clears Battle-shock on a
                                     // *passed* subsequent test, so this is a plain persistent
                                     // latch the player clears themselves (the app has no turn-
                                     // tracking concept to auto-reset it against anyway).
```

**Pure functions (not stored state):**
- `KeywordResolution.EffectiveKeywords(ICombatUnit)` — union of `Keywords` over currently-present
  components' Datasheets, plus the `Keywords` of currently-present (`RemainingCount > 0`)
  `ModelLine`s. Recomputes live, never cached. Model-level keyword checks must read a specific
  `Unit.Datasheet.Keywords` together with that specific model's own `ModelLine.Keywords` directly,
  never a sibling `ModelLine`'s and never this union.
- `ToughnessResolution.ResolveDefendingToughness(AttachedUnit)` — highest Toughness among present
  Bodyguard models if any remain, else highest among present Leader/Support models. Domain fact
  only; not wired to `Simulation/*`.
- `ICombatUnit.Name` — computed on read, not stored: neither BattleScribe/NewRecruit exports nor
  GW's own app support free-form per-instance unit naming, so no external source could populate a
  stored value. `Unit.Name` is its Datasheet's name. `AttachedUnit.Name` is a humanized join:
  none attached → Bodyguard name alone; one → `"{Bodyguard} with {A}"`; two →
  `"{Bodyguard} with {A} and {B}"`; three+ → an Oxford-comma join. Duplicate attached-leader names
  are not deduplicated (two Marshals render as `"...with Marshal and Marshal"`) — a known, accepted
  limitation.
- `HalfStrengthResolution` — `StartingStrength(ICombatUnit)`/`CurrentStrength(ICombatUnit)` sum
  `Σ ModelLine.Count`/`Σ ModelLine.RemainingCount` across every `Components` entry combined,
  matching the real rule that an attached unit's starting strength is combined, not per-component.
  `IsAtOrBelowHalfStrength(ICombatUnit)` is true when `CurrentStrength <= floor(StartingStrength /
  2)` (rounded down — a real bug once used `ceil`, one casualty too eager), but only meaningful
  when `StartingStrength >= 2` — exactly 1 always returns `false` here, since that determination is
  wound-based and this app has no partial-wound data (see `IsHalfStrengthOverride` above).
  `IsAtOrBelowHalfStrengthStatus(ICombatUnit)` is the one combined read most callers want — the
  computed value when `StartingStrength >= 2`, the player-set override when it's exactly 1.

**Aggregate view** (`AttachedUnitAggregator.Build(ICombatUnit) -> AttachedUnitAggregateView`):

```
AttachedUnitAggregateView(Name: string, IsAttachedUnit: bool, Statlines, Weapons,
                           Abilities, Keywords)
  // Name is the source combatUnit.Name, copied through unchanged. IsAttachedUnit is
  // `combatUnit is AttachedUnit` - lets a page-layer consumer distinguish source type without
  // re-deriving it from Statlines/Weapons shape; true even with zero Attached units.

ModelLineLoadout(WeaponsLabel, RemainingCount, InitialCount)
  // WeaponsLabel is ModelLine.Weapons comma-joined, e.g. "Bolt pistol, Heavy Bolt pistol, Power fist"

AggregateStatlineEntry(ComponentName, StatlineName, Statline, RemainingCount, InitialCount,
                        Loadouts: IReadOnlyList<ModelLineLoadout>)
  // ComponentName is the owning component's Datasheet.Name - lets a downstream consumer detect
  // component boundaries without re-deriving them from Statlines' declared order.
  // RemainingCount/InitialCount are summed across every ModelLine sharing StatlineName, including
  // fully-removed casualties. Loadouts has one entry per contributing ModelLine (not collapsed by
  // weapon list) so a fully-wiped loadout-variant still shows 0/InitialCount instead of
  // disappearing.

WeaponContribution(ComponentName, StatlineName, Count, PerModelAttacks)
  // ComponentName is the owning Unit.Datasheet.Name; Count is that ModelLine's RemainingCount

AggregateWeaponEntry(Profile: WeaponProfile, TotalAttacks: DiceExpression,
                      Contributions: IReadOnlyList<WeaponContribution>)
  // Profile is retained for identity fields only - Profile.A is NOT authoritative once a row
  // merges more than one contribution. Only TotalAttacks is safe to render.

AggregateAbilityEntry(ComponentName: string?, StatlineName: string?, Ability: Ability,
                       ContributingComponentNames: IReadOnlyList<string> = [])
  // StatlineName is null for a component-wide ability (Datasheet-sourced or Enhancement), set for
  // a ModelLine-sourced one. No cross-component dedup EXCEPT (gate-and-dedupe-core-rule-abilities)
  // when two+ present components share an identical CoreRule-origin ability by Name - collapsed
  // into one entry (ComponentName/StatlineName null, ContributingComponentNames listing every
  // contributor), rendered as its own row above every component's rows. See the record's own doc
  // comment for the full rationale and collapse rule.
```

- `Statlines` — built by walking components in display order (an `AttachedUnit`'s `Attached` list
  then its `Bodyguard`; a plain `Unit` is just itself), and within each component, its
  `Datasheet.Statlines` in declared order. For each declared name, only that component's own
  `ModelLine`s are matched (case-insensitive) — per-name grouping is scoped to a single component,
  so two components can never merge into one entry even sharing a statline name (real 40k
  attachment rules attach by Leader/Support ability, not by name, and don't allow two identically-
  named Leader/Support units on one Bodyguard). An entry is built from every model-line sharing the
  name regardless of `RemainingCount` (unlike `Weapons` below), and — since `casualty-tracking` —
  is included whenever the component has *any* model-line referencing that name at all, even once
  every one reaches `RemainingCount == 0` (reports `RemainingCount: 0` against the unchanged
  `InitialCount` rather than disappearing). Omitted only when the name was never referenced to
  begin with. This "persist at zero" behavior exists so `/LivePlay`'s casualty control always has a
  row to revert from — see `openspec/specs/live-play-view/spec.md`'s "Statline Section Rendering"
  for the paired UI collapse behavior.
- `Weapons` — grouped by `WeaponProfile.EqualityKey()` (excludes Name/Range/Attacks) across only
  `RemainingCount > 0` model-lines. `TotalAttacks` is computed per contribution as
  `PerModelAttacks.Scale(modelLine.RemainingCount)`, `Add`-reduced across every contributor sharing
  the `EqualityKey` — **not** a representative contributor's raw `A` with only the model count
  summed (the bug this shape fixes: 4 models × A3 and 1 model × A7 sharing an `EqualityKey` must
  total 19, not silently report one contributor's A with Count=5).
- `Abilities` — built by `BuildAbilities`, walking components in `BuildStatlines`'s display order.
  For each component where `IsPresent`: one entry per `Datasheet.Ability` (`StatlineName: null`),
  one entry per resolved `Unit.Enhancements` ability (`StatlineName: null`, reported the same way
  as a Datasheet-sourced ability), then one entry per `Ability` on each present (`RemainingCount >
  0`) `ModelLine` (`StatlineName:` that line's name). No cross-component combination/dedup,
  mirroring `Statlines`' per-component scoping. The explicit `IsPresent` guard covers both
  component-wide sources, since neither has a statline-level gate to fall through the way
  `BuildStatlines` naturally does. A `ModelLine`-sourced entry disappears once that line's
  `RemainingCount` reaches 0. `/LivePlay` renders an Enhancement-classified entry
  (`Ability.Origin == Enhancement`) with a leading `✦ ` wherever an ability name renders (including
  a popover's title bar) — see `_UnitBlock.cshtml`'s `AbilityDisplayName` helper. Renders in the
  Unit Abilities column unconditionally today since `Ability.Scope` is always `Unit` for every
  BSData-resolved ability; a genuine per-Enhancement Model/Unit scope is deferred
  (`.claude/vnext-ideas.md`).
- `Keywords` — wired directly to `KeywordResolution.EffectiveKeywords`.

`AttachedUnitAggregator.Build` computes one `RemainingCount > 0`-filtered `presentLines` list from
`combatUnit.Components`, feeding only `BuildWeapons` (a fully-dead model-line contributes nothing
to a weapon total). `BuildStatlines` and `BuildAbilities` both take `combatUnit` directly rather
than a pre-flattened line list, sharing a private `ComponentDisplayOrder` helper — both already
need each component's own `Datasheet`, so each walks `component.ModelLines` per component, which
is also what makes per-component merge scoping fall out for free. Unfiltered access to
`ModelLine`s (so fully-dead loadout-variants and correct `InitialCount`s stay visible in
`Statlines`) happens naturally since `BuildStatlines` reads `component.ModelLines` directly rather
than through the `presentLines` filter.

---

## Statline-Flag Rules

Full requirements: `openspec/changes/resolve-known-ability-effects/`. A small, closed-vocabulary
mechanism that recognizes a specific ability (exact Name + exact Text, no partial/fuzzy match) and
derives a flagged Statline-characteristic value for display on the *specific resolved unit* it's
currently present on — never mutating the shared `Datasheet`/`Unit` themselves. The matched source
ability always keeps rendering normally in the unit's Abilities/Enhancements listing alongside the
derived value; this mechanism only ever adds a value, never removes or hides anything.

```
ProbHammer.Core.Domain.Roster.StatlineFlagRuleScope = Bearer | WholeUnit
                                       // Bearer: the mutation applies only to the matched ability's
                                       // own bearer row(s) - one specific model-line when the
                                       // matched AggregateAbilityEntry.StatlineName is set, the
                                       // whole owning component when it's null (a Datasheet-level or
                                       // Enhancement-sourced ability). WholeUnit: applies to every
                                       // row of the whole ICombatUnit regardless of which component
                                       // granted it (Vexilla - "the bearer's unit" spans every
                                       // component of an attached formation in real 11e rules, not
                                       // just the bearer's own).

StatlineFlagRule                      // abstract: AbilityName/AbilityText (the exact match key),
                                       // Scope, Characteristic (InvulnerableSave | ObjectiveControl
                                       // - StatlineFlagCharacteristic, Domain/Roster/
                                       // AttachedUnitAggregateView.cs), Matches(Ability) (exact
                                       // Name+Text), Apply(Statline) -> Statline (the mutation
                                       // itself, e.g. `with { Oc = baseStatline.Oc + 1 }`)
ShieldDomeStatlineFlagRule             // Impulsor's Shield Dome - Bearer scope, InvulnerableSave:
                                       // "The bearer has a 5+ invulnerable save." -> InSv(5,5,false,null)
VexillaStatlineFlagRule                // Custodian Guard's Vexilla - WholeUnit scope, ObjectiveControl:
                                       // "Add 1 to the Objective Control characteristic of models in
                                       // the bearer's unit." -> Oc + 1
StatlineFlagRuleCatalogue.All          // the full closed vocabulary - a real third rule joins this
                                       // list directly, no architecture change needed
```

**Wiring** (`AttachedUnitAggregator.Build`): runs as an additional step after `BuildStatlines`/
`BuildAbilities` produce their live, casualty-filtered results (`ApplyStatlineFlagRules`) - matches
every present `AggregateAbilityEntry` against `StatlineFlagRuleCatalogue.All`, then for each
`AggregateStatlineEntry` whose (ComponentName, StatlineName) is that matched ability's own bearer
(or, for a `WholeUnit`-scoped rule, unconditionally), applies the rule's `Apply` to produce a
mutated `Statline` plus a `StatlineFlag` (Characteristic + source `Ability`) recorded on that
entry's own `AggregateStatlineEntry.Flags`. Since `BuildAbilities`' own output is already filtered
to only currently-present sources (the same liveness rule that governs whether the ability itself
renders), a flagged value's liveness falls out for free with no separate tracking — marking the
bearer a casualty removes the matching `AggregateAbilityEntry` on the next `Build`, so the rule
pass simply has nothing to match against and the affected `Statline` reverts to its own Datasheet
base value. Never mutates `Datasheet`/`Unit`; only the returned, decorated copy of the statline
entries carries a rule's effect.

**`/LivePlay` display** (`resolve-known-ability-effects`): replaces the old InSv-only always-visible
`.insv-caveat-text` paragraph with a
general per-run footnote-marker-and-legend mechanism, driven uniformly by an unresolved
invulnerable-save caveat (`InvulnerableSave.Caveated`, above) and a `statline-flag-rules` match
alike — see `.claude/design-tokens.md`'s "Flagged statline legend" for the visual mechanism, and
`live-play-view`'s "Flagged Statline Characteristic Rendering" for the full requirement.
`LivePlayModel.GroupStatlines` reads each run's own `AggregateStatlineEntry.Flags` (and its shared
`Statline.InSv.Caveated`/`CaveatAbility`) to determine that run's own flag source per characteristic
(`StatlineBlockViewModel.ObjectiveControlFlagSource`/`InvulnerableSaveFlagSource`);
`LivePlayModel.AssignFlagMarkers` then walks every run in order, assigning the first distinct
source seen `*`, the next `**`, and so on, reusing an already-assigned marker for the same source
wherever it recurs — so a `WholeUnit`-scoped source affecting every run of a unit keeps one marker
throughout and gets a legend line in every one of those runs, never consolidated into a single
shared location.

**`ArmyRoster`** (`Domain/Roster/ArmyRoster.cs`) wraps the per-unit roster (`Units`, an
`IReadOnlyList<ICombatUnit>`) with army-level metadata: `Name`, `PointsSpent`, `Faction` (ordered —
parent codex before sub-faction, e.g. `["Space Marines", "Black Templars"]`), `Detachments`
(ordered by selection — `IReadOnlyList<ResolvedDetachment>`:
`ResolvedDetachment(Name, Rules: IReadOnlyList<DetachmentRule>)`, `DetachmentRule(Name, Text)` —
see "Detachment Resolution" above), `ForceDisposition`, `BattleSize`, `PointsLimit`. Originally
added ahead of the (then-unbuilt) GW-app export parser so parsing work had a settled target type;
`import-army-list-for-live-play`'s real `ArmyListParser`/`ArmyRosterEnricher` now populate this
exact shape from a real pasted export unchanged. `PointsSpent` is still plain sample data on the
`Examples/` fixture path — not derived from or reconciled against `Units`, since no points value is
tracked anywhere below `ArmyRoster`; a real import's `PointsSpent` comes from the export text
itself. `Examples/View.MyArmyRoster()` remains a thin projection with no `/LivePlay` call site —
`Examples/` stays in the tree for future exploration, unreferenced by `Web`.

---

## Deliberate Omissions

- No wargear constraint/composition-rule validation, no points-cost modeling — the exporting app
  is trusted to have already produced a valid list.
- No partial wound-count tracking — alive/dead per model-line only; physical wound markers cover
  the rest at the table.
- Ability text is never auto-parsed into behavioral effects — permanent exclusion, not a
  deferral (some conditions need positional/objective-control state the domain has no way to
  represent even in principle).
- `Simulation/*` (CombatSimulator, SimulationAdapter, WoundPool) is untouched but paused — not
  wired to this domain model. The one exception is `DiceExpression`, which moved from
  `Simulation/*` into `Domain.Catalogue` (see Catalogue Context above); `Simulation/*` files were
  repointed to the relocated type via `using` statement only — no behavior, method signature, or
  test change in `Simulation/*` itself.
