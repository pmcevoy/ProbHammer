# Domain Model — 11th Edition (Attached Units)

Full requirements: `openspec/changes/attached-unit-domain-model/` (proposal, design, specs,
tasks). This file summarizes the shape that landed in code.

**Status:** implemented and tested (`src/ProbHammer.Core/Domain/`), proven against hand-built
fixtures (`tests/ProbHammer.Tests/Domain/Fixtures/`) plus, since `catalogue-json-ingestion`, a
real BSData JSON loader (`Domain/Catalogue/Bsdata/` — see "BSData JSON Ingestion" below) that can
populate `Datasheet` objects from a local BSData clone. Since `import-army-list-for-live-play`,
`ProbHammer.Web`/`/LivePlay` is fully wired to this model: a pasted GW-app 11e export is parsed
(`Domain/Import/` — see "Army List Import Pipeline" below), resolved against BSData
(`ArmyRosterEnricher`, same section), and rendered — `Examples/`-fixture data is no longer
referenced anywhere in `Web`, though the `Examples/` files themselves remain in the tree unchanged
for future domain-model exploration. Coexists with the 10th-edition model
(`.claude/domain-model.md`), which remains in the tree, untouched and fully unused (superseded, not
just paused — see `PROGRESS.md` for that history).

---

## Namespaces

- `ProbHammer.Core.Domain.Catalogue` — reference/rules data (Catalogue context)
- `ProbHammer.Core.Domain.Roster` — army-list composition and live game state (Roster context)

---

## Catalogue Context

```
Datasheet
  Name, FactionKeywords, Keywords, Abilities (unit-wide)
  Statlines: IReadOnlyList<(string Name, Statline Statline)>   // ordered - declaration order is a
                                                      // load-bearing, documented property (the
                                                      // Sergeant/leader-equivalent entry is declared
                                                      // first, matching the real NewRecruit/GW app
                                                      // export convention); not an incidental
                                                      // collection-enumeration detail
  GetStatline(name) -> Statline                        // O(1) lookup, backed by an internal
                                                      // Dictionary built once in the constructor
                                                      // from the ordered list
  ResolveWeaponProfile(name) -> WeaponProfile          // on-demand only, never enumerated
  ctor(..., statlines: IReadOnlyList<(string Name, Statline Statline)>, weaponProfiles: IEnumerable<WeaponProfile>)
                                                      // weaponProfiles keyed internally by .Name

Statline(M, T, Sv, W, Ld, Oc)   // value object — field names match official shorthand
  InSv: InvulnerableSave               // init-only property, defaults to a fresh (absent)
                                       // instance; see "Invulnerable Save Resolution" below for
                                       // the type's own shape and how BSData text resolves into it

DiceExpression(Count, Sides, Modifier)   // value object — a fixed integer (Count=0) or a dice
                                          // roll ("D6", "2D3+1"); Parse/Fixed/Scale/Add unchanged
                                          // from its original Simulation-namespace shape
  Parse(string) -> DiceExpression        // "3" | "D6" | "2D3+1"
  implicit operator DiceExpression(int)  // plain ints convert to a fixed value at call sites
  D3, D6                                 // static presets for the common single-die case
  operator +(DiceExpression, int)        // e.g. DiceExpression.D6 + 2 → "D6+2"; non-mutating
  Scale(n), Add(other)                   // multi-model attack/damage aggregation
  ExpectedValue() -> double               // Count==0 ? Modifier : Count*(Sides+1)/2.0 + Modifier;
                                           // used only to order weapons by aggregate attacks in
                                           // /LivePlay - DiceExpression has no natural total order

WeaponProfile(Name, Type, Range, A, S, Ap, D)   // abstract value object; A and D are
                                                 // DiceExpression (Attacks/Damage can be fixed
                                                 // or dice-based, e.g. Multi-melta D6 damage);
                                                 // S and Ap stay plain int
  Skill: int                                    // abstract computed property, see below
  Torrent, Blast, Melta, RapidFire, SustainedHits, LethalHits, DevastatingWounds,
    TwinLinked, IndirectFire, Pistol, IgnoresCover, Assault, Anti
                                                  // init-settable ability properties, flattened
                                                  // directly onto WeaponProfile (no nested
                                                  // WeaponAbilities type)
  EqualityKey() -> WeaponProfileEqualityKey   // (Type, Skill, S, Ap, D, ability fields)
                                               // excludes Name/Range/A — used to aggregate
                                               // weapon instances by profile, not identity

RangedWeapon(Name, Range, A, Bs, S, Ap, D) : WeaponProfile   // Type fixed to Ranged
  Skill => Bs

MeleeWeapon(Name, A, Ws, S, Ap, D) : WeaponProfile           // Type fixed to Melee, Range fixed to 0
  Skill => Ws

Ability(Name, Text, Choices: IReadOnlyList<AbilityChoice>, Scope: Model | Unit)
```

**Key design points:**
- A Datasheet can have any number of named statlines (not "1 or sometimes 2") — a Chaos Space
  Marine datasheet has 5. The same statline name can be referenced by model-lines with different
  weapon eligibility (Sergeant-style).
- Weapon profiles never materialize as a full options menu — matches the product principle that
  ProbHammer only shows wargear that was actually chosen.
- `Ability.Scope` is a property of the ability itself, not of its source (Enhancement, wargear,
  datasheet-intrinsic) — 11e rules text treats those as examples of sources, not special cases.
- `WeaponProfile.EqualityKey()` mirrors `SimulationAdapter.WeaponGroupKey` (10e code) — same
  concept (group by structural profile, sum quantities), ported rather than reimplemented.
- `WeaponProfile` is abstract with sealed `RangedWeapon`/`MeleeWeapon` subtypes rather than a
  single record with a `Type` field — makes it impossible to construct a weapon whose `Type` and
  `Range` disagree with its own shape (a melee weapon can't accidentally carry a non-zero range).
- `Skill` is an abstract *computed* property (`RangedWeapon.Skill => Bs`, `MeleeWeapon.Skill =>
  Ws`), not a stored/init value on the base record. This keeps official terminology (`Bs`
  ballistic skill, `Ws` weapon skill) as the single source of truth on each subtype while still
  giving shared logic (`EqualityKey()`, future hit-roll code) one `Skill` name to read regardless
  of weapon type. Earlier draft had `Bs`/`Ws` as extra positional parameters forwarded into a
  stored base `Skill` — that created two independent backing fields that could desync under a
  `with` expression (`weapon with { Skill = x }` wouldn't update `Bs`). Computing `Skill` from
  the subtype's own field instead of storing it separately removes that failure mode structurally.
- Field names throughout (`Statline.M/T/Sv/W/Ld/Oc`, `WeaponProfile.A/S/Ap/D`) intentionally match
  official 40k shorthand rather than spelled-out names — readability for anyone who knows the
  rules trumps self-documenting-variable-name conventions here.
- `DiceExpression` lives here (`Domain.Catalogue`), not in `Simulation/*` where it originated —
  it's a game-rules quantity (a D6 means the same thing whether or not a simulation ever rolls
  it), and `WeaponProfile.A`/`D` depend on it directly. `Simulation/*` now references this type
  instead of owning it; that repoint is a `using`-statement change only (openspec change
  `promote-dice-expression-to-domain`) and does **not** mean `Simulation/*` has been rewired onto
  this domain model — see Deliberate Omissions below, which still holds.
- `DiceExpression`'s implicit `int` conversion exists so fixed-value weapon stats (the common
  case) can be written as a plain integer at construction sites instead of `DiceExpression.Fixed(n)`
  everywhere; variable stats (e.g. `DiceExpression.D6`, `DiceExpression.D6 + 2`) still use
  `DiceExpression` explicitly. Chosen over constructor overloads on `RangedWeapon`/`MeleeWeapon`
  because `A` and `D` vary independently between fixed and dice-based (e.g. Multi-melta: fixed `A`,
  dice `D`) — overloads would need one combination per shape; the implicit conversion applies
  per-argument instead, so mixed cases fall out for free.
- `Datasheet`'s constructor takes `weaponProfiles` as `IEnumerable<WeaponProfile>`, not a pre-built
  dictionary — the internal lookup is built from `.Name` inside the constructor, so a caller can
  never construct a `Datasheet` where a weapon's dictionary key disagrees with its own `Name` (a
  real bug hit when `WeaponFixtures` was introduced: fixture call sites had duplicated the name as
  both the dictionary key and the `WeaponProfile.Name` argument, and the two drifted). `Statlines`
  changed from a caller-supplied `IReadOnlyDictionary<string, Statline>` to an ordered
  `IReadOnlyList<(string Name, Statline Statline)>` — `Dictionary` enumeration order is an
  implementation detail, not a documented .NET contract, and it only happened to preserve insertion
  order because nothing ever removed an entry; declared order is now an explicit, tested guarantee
  instead of an accident. A plain tuple keeps the pairing without a single-use wrapper type, since
  `Statline` still carries no `Name` of its own — the name is genuine external metadata (a role
  label like "Sergeant"), not something derivable from the value. `GetStatline(name)` stays the
  primary lookup API for existing callers (`ToughnessResolution`, tests); internally it's backed by
  a `Dictionary<string, Statline>` built once in the constructor from the ordered list, so lookup
  stays O(1) and the ordered list stays the single source of truth for both lookup and display
  order. Two differently-named statlines with identical `M/T/Sv/W/Ld/Oc` are valid and expected —
  real BSData exports name "Assault Intercessor" and "Assault Intercessor Sergeant" separately even
  though they share a statline — which is exactly why `AttachedUnitAggregator.BuildStatlines`
  dedupes by name, not by value.

---

## BSData JSON Ingestion

Full requirements: `openspec/changes/catalogue-json-ingestion/` (proposal, design, specs, tasks).
Raw file format itself (not the C# types below): `.claude/bsdata-json-schema.md`.
Namespace: `ProbHammer.Core.Domain.Catalogue.Bsdata`. Reads the BattleScribe `catalogueSchema` JSON
shape (one file per faction/sub-faction) that BSData's 11th Edition repository publishes, and
resolves it into the `Datasheet`/`Statline`/`WeaponProfile`/`Ability` shapes above. This loader
itself only resolves names to rules data and does not build the `Unit`/`AttachedUnit`/`ModelLine`
graph — that's `ArmyRosterEnricher`'s job (see "Army List Import Pipeline" below), which consumes
these types directly rather than duplicating them. Since `import-army-list-for-live-play`, this is
fully wired into `ProbHammer.Web`/`/LivePlay` via the app-wide `BsdataCatalogueCache`.

```
IBsdataCatalogueSource            // "get JSON content for this filename" boundary
  GetJson(fileName) -> string
  ListFileNames() -> IReadOnlyList<string>   // fallback path only, see below

LocalDiskBsdataCatalogueSource(rootDirectory)   // only implementation today; rootDirectory is a
                                                 // caller-supplied config value, never hardcoded

BsdataCatalogueReader.Read(json) -> BsCatalogue      // System.Text.Json; Json/BsCatalogueFile.cs
                                                      // models only the fields this loader needs -
                                                      // constraints/modifiers/conditionGroups/
                                                      // associations/costs are simply absent from
                                                      // those types, so real files deserialize
                                                      // clean without special-casing them. Reads
                                                      // either JSON root shape a real BSData file
                                                      // uses - a catalogueSchema document
                                                      // ({ "catalogue": {...} }) or the single
                                                      // game-system document ({ "gameSystem":
                                                      // {...} }, e.g. "Warhammer 40,000.json") -
                                                      // since both share the same nested shape for
                                                      // every field this loader reads.

BsdataClosureResolver.Resolve(source, startingFileName) -> BsdataClosure
                                       // BFS over catalogueLinks[].importRootEntries, starting
                                       // file first, nearer imports before farther ones; a link's
                                       // cached `name` usually equals its target's file name, but
                                       // can drift (real example: "Chaos - Daemons Library" links
                                       // to "Chaos - Chaos Daemons Library.json") - falls back to
                                       // matching every available file's own top-level catalogue
                                       // `id` against the link's targetId when the name guess isn't
                                       // present, which is what ListFileNames() is for. Also
                                       // resolves the current catalogue's own `GameSystemId` (not
                                       // a catalogueLink at all) against every available file's own
                                       // `id` - added by `structured-invulnerable-save`, since the
                                       // base rules file's shared "Invulnerable Save (X+*)"/
                                       // "*Invulnerable Save" abilities live only there. Stored on
                                       // the returned closure as a separate `BsdataClosure
                                       // .GameSystem: BsCatalogue?` property, deliberately outside
                                       // `Files` - its own SharedSelectionEntries/
                                       // SharedSelectionEntryGroups are generic, cross-faction
                                       // template content (Warlord traits, Enhancements, Crusade
                                       // rules) this loader was never designed to walk as if they
                                       // were real datasheet weapons/statlines (confirmed by a real
                                       // corpus-scan regression when this was first tried more
                                       // broadly - a T'au entry's dangling "Warlord"/"Enhancements"/
                                       // "Crusade" entryLinks, previously silently unresolved by
                                       // design, newly resolved into generic wargear with a literal
                                       // "*" placeholder value this loader's weapon parsing can't
                                       // handle). Only its SharedProfiles is actually needed.

BsdataNameResolver.Resolve(closure, name) -> BsSelectionEntry?
                                       // walks closure.Files in resolution order, first
                                       // SharedSelectionEntries name match wins - local always
                                       // beats imported
  .BuildIdIndex(closure) / .BuildGroupIdIndex(closure) / .BuildProfileIdIndex(closure)
                                       // id -> entry/group/profile maps spanning the whole closure,
                                       // used only to follow entryLinks/infoLinks during mapping.
                                       // BuildIdIndex/BuildGroupIdIndex read only closure.Files -
                                       // the game system (closure.GameSystem) is deliberately never
                                       // reachable through ordinary entryLink/link resolution, so a
                                       // link that was always meant to dangle (e.g. into generic,
                                       // cross-faction "Warlord"/"Enhancements"/"Crusade" template
                                       // content this loader was never designed to map as weapons)
                                       // keeps silently skipping exactly as before.
                                       // BuildProfileIdIndex additionally merges in
                                       // closure.GameSystem?.SharedProfiles when present - the one
                                       // thing real catalogue entries actually link into there.

BsdataDatasheetMapper.BuildDatasheet(entry, idIndex, groupIdIndex, profileIdIndex) -> Datasheet
                                       // single recursive walk of the resolved entry's full
                                       // descendant tree (profiles, selectionEntries,
                                       // selectionEntryGroups, entryLinks, infoLinks); collects
                                       // every "Unit"-typeName profile as a named Statline (deduped
                                       // by name, first occurrence wins - this is what makes a
                                       // single-model entry, whose Unit profile sits on itself, and
                                       // a squad entry, whose distinct child model types each carry
                                       // their own, fall out of the same code path with no explicit
                                       // squad-vs-single branch), every "Ranged Weapons"/"Melee
                                       // Weapons"-typeName profile as a resolvable WeaponProfile,
                                       // and every "Abilities"-typeName profile as an Ability
                                       // (Text carried through verbatim, Scope always AbilityScope
                                       // .Unit - BSData JSON gives no per-ability scope signal).
                                       // entryLinks are resolved via idIndex/groupIdIndex;
                                       // infoLinks of type "profile" are resolved via
                                       // profileIdIndex against BsCatalogue.SharedProfiles -
                                       // some model statlines exist only this way (real example:
                                       // Chaos - Chaos Space Marines.json's "Legionaries" squad
                                       // reaches its troop model's Unit profile through a
                                       // profile-type infoLink, not nested in any selectionEntry's
                                       // own Profiles list). FactionKeywords/Keywords are always
                                       // empty - BSData's categoryLinks could plausibly source
                                       // them, but no requirement in this change's spec governs
                                       // that mapping, so it's left unattempted rather than guessed.
                                       // The walk also threads a per-branch ancestry list (every
                                       // BsSelectionEntry from the walk root down to the current
                                       // one, reset to a fresh chain on each entryLink hop) purely
                                       // for InSv resolution - see "Invulnerable Save Resolution"
                                       // below.

InvulnerableSave(MeleeInSv, RangedInSv, Caveated, CaveatAbility)   // Domain/Catalogue/
                                       // InvulnerableSave.cs; sealed record, non-nullable on
                                       // Statline (0/0/false/null = absent, same convention as
                                       // every other threshold field). CaveatAbility: Ability? is
                                       // null except when Caveated == true, in which case it is
                                       // always set - enforced by the 4-argument constructor (an
                                       // object initializer or `with` expression can still bypass
                                       // this, the same class of record-validation gap this
                                       // project already accepts elsewhere).
  implicit operator InvulnerableSave(int)   // uniform (melee == ranged, non-caveated) is the
                                       // common case - mirrors DiceExpression's own implicit int
                                       // conversion so a plain digit reads naturally at call sites
                                       // (e.g. `new Statline(...) { InSv = 4 }`).

BsdataDatasheetMapper.ResolveInvulnerableSave(text, ancestry, ctx) -> InvulnerableSave
                                       // Resolves a Unit profile's raw InSv characteristic text.
                                       // Self-contained shapes resolve directly, no ability
                                       // involved: a plain digit ("4+"); a parenthetical
                                       // attack-type restriction ("5+ (Ranged)", confirmed on the
                                       // four Titans + the Sokar-pattern Stormbird) - the
                                       // unrestricted attack type's value is set to 0. Footnoted
                                       // shapes - a bare trailing "*" (e.g. "5+*", by far the most
                                       // common real shape) or a "/"-split where exactly one side
                                       // carries the marker (e.g. "4+* / 5+", confirmed on Howling
                                       // Banshees/Wyches) - always resolve as Caveated = true: this
                                       // method deliberately never reads what the linked ability's
                                       // Description text says, only which specific ability is
                                       // linked (see ResolveCaveatAbility) - classifying that text
                                       // is a permanently deferred future capability (see
                                       // .claude/vnext-ideas.md's "Ability-text interpretation
                                       // pass"). Both MeleeInSv/RangedInSv are populated with the
                                       // one value known without that interpretation - the
                                       // unfootnoted side of a split when one exists, otherwise the
                                       // footnoted value itself. Throws AmbiguousCharacteristicException
                                       // for a raw shape matching none of the above.

BsdataDatasheetMapper.ResolveCaveatAbility(digit, ancestry, ctx, rawText) -> Ability
                                       // Resolves the specific Ability a footnoted InSv value is
                                       // linked to, by id - never by name alone against the whole
                                       // datasheet's flattened, name-deduped ability list, since
                                       // the real base catalogue contains two different profiles
                                       // both named "Invulnerable Save (4+*)" with opposite
                                       // meanings (one "against ranged attacks", one "against melee
                                       // attacks"). Two real naming conventions are recognized: the
                                       // digit-parameterized "Invulnerable Save ({digit}+*)"
                                       // (confirmed: Imperial Knights' Canis Rex, linked via
                                       // infoLink into the base rules catalogue's shared profile;
                                       // Howling Banshees, same shape) and the generic,
                                       // unparameterized "*Invulnerable Save" (confirmed: Space
                                       // Marines' Judiciar/Astraeus, defined locally; Chaos
                                       // Knights' War Dog family, linked). Searches every entry in
                                       // `ancestry` nearest-first (the entry that actually owns the
                                       // Unit profile, then its container, and so on up to the walk
                                       // root) - the real corpus doesn't always put the link/local
                                       // ability on the same entry as the footnoted characteristic
                                       // itself: confirmed on Howling Banshees, whose squad's own
                                       // top-level entry carries the infoLink while the footnoted
                                       // InSv sits on a nested child model entry one level down
                                       // (inside a selectionEntryGroup). Each candidate entry's own
                                       // locally-nested Abilities profiles are checked before its
                                       // own "profile"-type InfoLinks (confirmed shape: Orks'
                                       // Makari, whose "Invulnerable Save (2+*)" sits directly in
                                       // his own entry.Profiles, not behind any link). Throws when
                                       // no entry in the ancestry has a matching candidate under
                                       // either naming convention - a footnote with no resolvable
                                       // ability is a new, unprecedented shape (or, confirmed once
                                       // in the real corpus - Aeldari's Archon/Ynnari Archon - a
                                       // genuine BSData data anomaly with no ability anywhere in
                                       // the entry at all), never guessed at.

WeaponKeywordParser.Apply(weapon, keywordsText) -> WeaponProfile
                                       // splits a weapon's free-text Keywords characteristic
                                       // (e.g. "Anti-infantry 4+, Devastating Wounds") on commas;
                                       // every token joins WeaponProfile.KeywordsText verbatim
                                       // unconditionally, and separately, exact-match tokens also
                                       // set the corresponding existing ability flag ("-" means no
                                       // keywords at all). No alias/synonym recognition - "Cleave"
                                       // never sets Blast, "Close Combat" never sets Pistol - and a
                                       // token naming a mechanic with no flag at all (Hazardous,
                                       // Precision, Heavy) is retained verbatim with no flag set.
WeaponKeywordParser.UnrecognizedTokens(keywordsText) -> IReadOnlyList<string>
                                       // every token Apply would leave unflagged, for the same
                                       // input - shares Apply's own token-recognition logic via a
                                       // private TryRecognize helper rather than duplicating it,
                                       // added so the full-BSData-corpus weapon-keyword scan (see
                                       // "Full-Corpus Scan Tests" below) can't silently drift from
                                       // what Apply actually recognizes.

BsdataDatasheetMapper.IsGameModeGated(modifiers, ctx) -> bool
                                       // WalkEntry/WalkGroup's guard against a real, user-reported
                                       // bug: a datasheet's full descendant tree can contain content
                                       // BSData itself marks hidden outside "Army Roster" (matched
                                       // play) mode - Crusade-mode-only narrative content (every
                                       // datasheet's "Crusade" entryLink: Battle Honours, Chaos
                                       // Boons, Crusade Relic Upgrades) and force-type-gated
                                       // matched-play mechanics unavailable in this specific game
                                       // mode (confirmed: Chaos Space Marines' "Mark of Chaos",
                                       // verified against NewRecruit's own UI to only appear in
                                       // "Crusade Force" mode, never "Army Roster") - and nothing
                                       // previously distinguished this from genuine datasheet rules,
                                       // since both use the identical "Abilities"-typeName profile
                                       // shape. True when any of an entry/group's own `modifiers`
                                       // sets `hidden: true` gated (anywhere in its condition tree -
                                       // a plain existence check across nested conditionGroups, not
                                       // real AND/OR boolean evaluation) by a condition referencing
                                       // one of two force-type ids resolved by NAME (never a
                                       // hardcoded id, matching this codebase's existing "resolve by
                                       // name" convention) from the game system's own ForceEntries:
                                       // "Army Roster" (real shape: `instanceOf`, `scope: "force"` -
                                       // hidden IN matched play) or "Crusade Force" (real shape:
                                       // `lessThan`, `scope: "roster"`, `field: "forces"` - hidden
                                       // UNLESS a Crusade Force is present) - opposite polarity, same
                                       // underlying "which game mode" concept, both treated as an
                                       // exclusion trigger since ProbHammer only ever represents Army
                                       // Roster mode. Called from WalkEntry/WalkGroup right after the
                                       // visited-id check; a match skips the whole subtree (matches
                                       // and abilities alike - Crusade Relic Upgrades under a "Weapon
                                       // Upgrades" wargear choice are excluded the same way as a
                                       // top-level "Crusade" entryLink). BuildDatasheet takes an
                                       // optional `forceEntries` parameter (default null -> empty
                                       // gate set -> nothing excluded, a fail-open default every
                                       // pre-existing call site keeps unchanged);
                                       // ResolvedBsdataCatalogue.ResolveDatasheet passes
                                       // `Closure.GameSystem?.ForceEntries`. This still does not
                                       // interpret `constraints`/`modifiers`/`conditionGroups` as
                                       // rules in general (composition validation stays explicitly
                                       // out of scope, per catalogue-json-ingestion's design.md) -
                                       // only this one narrow, verified shape is ever read.
```

`BsdataDatasheetMapper.ParseThreshold(text, CharacteristicPattern)` (Sv/BS/WS/LD only — InSv is no
longer one of its callers, see "Invulnerable Save Resolution" above) validates against a
per-characteristic allowlist regex (`CharacteristicPattern.Sv`/`.Bs`/`.Ws`/`.Ld` — digits with an
optional trailing `+`, identical today, kept as separate instances so a future divergence for one
characteristic can't silently affect the others) rather than stripping known-bad separators, and
throws `AmbiguousCharacteristicException` (carrying both the characteristic name and raw text as
properties) for anything that doesn't match.

`WeaponProfile.KeywordsText` (`IReadOnlyList<string>`, in `Domain/Catalogue/WeaponProfile.cs`, not
`Bsdata/`) is the one shape change this ingestion work made outside the new namespace: every
weapon's exact source keyword text, in source order, included in `EqualityKey()` so two weapons
that are rules-identical but spelled differently in source data are never silently aggregated as
one. Rendering (`_UnitBlock.cshtml`'s `WeaponAbilityTags`) still reconstructs text from the typed
flags today — switching it to read `KeywordsText` instead is deliberately deferred, unconnected to
`/LivePlay` still running on fixtures with no `KeywordsText` populated.

Automated tests never read the external local clone (`C:\Users\Pete\wh40k-11e`, this machine only,
not part of the repo) — they use trimmed real excerpts copied into
`tests/ProbHammer.Tests/Domain/Fixtures/Bsdata/` (generated by deserializing a real file through
these same reader types and re-serializing just the entries a test needs, which also strips every
unmapped field automatically) plus a few small hand-built closure fixtures for the import-precedence
scenarios. The full three-captured-export resolution smoke test (every named top-level unit in
`data/gw-app-export*.txt` resolves against its faction's real BSData closure) was run once against
the live clone during implementation and passed, including a genuine finding not worth chasing
here: the captured export text uses a typographic apostrophe where the BSData JSON source uses a
plain ASCII one (`Emperor's Champion`) — normalizing that is the future export-parser/enrichment
step's problem, not this loader's, since this loader resolves against JSON names as-authored.

### Full-Corpus Scan Tests

`tests/ProbHammer.Tests/Domain/Catalogue/Bsdata/CorpusScan/` holds the permanent counterpart to
that one-off smoke test — two xUnit v3 `[Fact(Explicit = true)]` tests (not run by `dotnet test`'s
default invocation or CI; manually triggered), added by the `bsdata-corpus-scan-tests` change,
that *do* read the live clone directly:

```
LiveClone.RequireSource() -> LocalDiskBsdataCatalogueSource
                                       // Directory.Exists-guards the hardcoded clone path and
                                       // calls Assert.Skip(...) (xUnit v3's dynamic skip) when
                                       // absent, so a machine without the clone reports the scan
                                       // as skipped, never as a bare pass.
LiveClone.CatalogueFileNames(source) -> IReadOnlyList<string>
                                       // every file via ListFileNames(), excluding the non-
                                       // catalogue "Warhammer 40,000.json".
AllowlistEntry<T>(Description, Matches: Func<T, bool>)
AllowlistCheck.AssertClean(results, allowlist, describeResult)
                                       // the bidirectional check shared by both scans: fails if
                                       // any result is unmatched by the allowlist, or any
                                       // allowlist entry matched nothing that run - so a fixed
                                       // issue can't silently go stale on the allowlist either.

CharacteristicResolutionScanTests.Full_corpus_characteristic_resolution_scan
                                       // every catalogue file gets a turn as its own closure's
                                       // starting file; every one of that file's own top-level
                                       // SharedSelectionEntries is attempted through
                                       // BsdataDatasheetMapper.BuildDatasheet, catching and
                                       // collecting any exception. CharacteristicResolutionAllowlist.cs
                                       // covers dice-notation WeaponProfile.S text (e.g. Ork
                                       // Battlewagon "D6+6") failing ParsePlainInt's int.Parse, plus
                                       // one confirmed real data anomaly (Aeldari's Archon/Ynnari
                                       // Archon - a footnoted InSv with no matching ability anywhere
                                       // in the entry under either naming convention, see
                                       // ResolveCaveatAbility above). `structured-invulnerable-save`
                                       // resolved every other InSv shape the scan had previously
                                       // allowlisted - this scan is what caught two real
                                       // architectural gaps that change surfaced (the base rules
                                       // game-system file being unreachable at all; the
                                       // ancestor-vs-immediate-entry ability-resolution gap), each
                                       // fixed generally rather than allowlisted around.

WeaponKeywordScanTests.Full_corpus_weapon_keyword_token_scan
                                       // walks each file's own closure directly (not through
                                       // BuildDatasheet, which has no way to enumerate every
                                       // weapon it resolves - Datasheet.ResolveWeaponProfile is
                                       // deliberately on-demand-only), collecting every
                                       // "Ranged Weapons"/"Melee Weapons" profile's Keywords text
                                       // and running WeaponKeywordParser.UnrecognizedTokens over
                                       // it. WeaponKeywordAllowlist.cs seeds 45 tokens found on
                                       // the first real run, none fixed in WeaponKeywordParser
                                       // (out of scope for the change that added this scan) -
                                       // grouped into already-documented no-flag tokens, case/
                                       // punctuation variants of a recognized token, dice-valued
                                       // variants of an int-only mechanic, and genuinely
                                       // unmodeled mechanics (a few widespread enough - Extra
                                       // Attacks, Psychic, Lance, One Shot - to be worth a future
                                       // dedicated addition). See PROGRESS.md's Known Issues.
```

---

## Army List Import Pipeline

Full requirements: `openspec/changes/import-army-list-for-live-play/` (proposal, design, specs,
tasks). Connects a pasted GW-app 11e export to `/LivePlay`: `text → IArmyListParser →
ParsedArmyList → ArmyRosterEnricher (against a ResolvedBsdataCatalogue) → ArmyRoster → /LivePlay`,
with per-session storage holding only the `ParsedArmyList` intermediate — every request
(`/LivePlay` renders, casualty adjustments) re-enriches fresh, one level up from
`casualty-tracking`'s "rebuild from source of truth" precedent for the fixture path.

### Army List Parsing (`ProbHammer.Core.Domain.Import`)

```
ParsedModelGroup(ModelName, Count, Weapons: IReadOnlyList<string>)
                                       // already-split per-loadout sub-group - the shared-vs-
                                       // mutually-exclusive weapon-count partition has already been
                                       // applied by parse time (see below). Weapons is a flat,
                                       // per-model list; duplicates are meaningful (two "Storm
                                       // bolter" entries mean two copies of that weapon on one model).
ParsedUnit(Name, ModelGroups: IReadOnlyList<ParsedModelGroup>, Enhancements: IReadOnlyList<string>)
                                       // Role (Leader/Support/Bodyguard) is not a field - it's
                                       // captured structurally by which list of ParsedAttachmentGroup
                                       // a member ends up in, mirroring AttachedUnit's own
                                       // Bodyguard/Attached split. The export's role-line category
                                       // parenthetical is discarded during parsing, never captured.
ParsedAttachmentGroup(Bodyguard: ParsedUnit, Attached: IReadOnlyList<ParsedUnit>)
ParsedArmyList(Name, PointsSpent, Faction, Detachments, ForceDisposition, BattleSize, PointsLimit,
               AttachmentGroups: IReadOnlyList<ParsedAttachmentGroup>,
               StandaloneUnits: IReadOnlyList<ParsedUnit>)
                                       // Faction/Detachments/etc. mirror ArmyRoster's own field
                                       // shapes exactly - see Roster Context below.

IArmyListParser.Parse(exportText) -> ParsedArmyList
ArmyListParser                        // the only implementation. Line-based: blank lines discarded,
                                       // every remaining line classified either positionally (the
                                       // fixed army-metadata preamble: Name/Points header, 1-2
                                       // Faction lines, one-or-more "(N Detachment Points)" lines,
                                       // ForceDisposition, BattleSize/PointsLimit header) or by a
                                       // small set of regexes (ATTACHED UNITS / a top-level section
                                       // header / "Attached unit N" / a "<Name> (<N> Points)" unit
                                       // header / a bullet line). Bullet nesting is read from the
                                       // bullet CHARACTER itself ("•" top-level, "◦" nested) never
                                       // from indentation depth - both appear at whatever indent the
                                       // export happens to use.
                                       // A unit's top-level bullets classify as: "Attached as: Role
                                       // [(Category)]" (first bullet only, attached mode; Category
                                       // discarded); "Enhancement(s): ..." (-> Enhancements, whole
                                       // remainder as one entry, never split on commas - same
                                       // "commas can be part of a real name" precedent as
                                       // Detachment); "Nx ModelName" WITH nested "◦" weapon lines
                                       // (-> an explicit model-group header, partitioned per below);
                                       // "Nx WeaponName" with NO nested lines (-> a direct weapon
                                       // selection, collected separately); anything else (e.g. bare
                                       // "Warlord") ignored.
                                       // A unit with zero explicit model-group headers (i.e. every
                                       // top-level bullet was a direct weapon selection - the shape
                                       // for single-model units like Helbrecht/Impulsor/Emperor's
                                       // Champion, which have no "Nx ModelName" wrapper at all in the
                                       // export) gets one synthesized ParsedModelGroup(Name: the
                                       // unit's own Name, Count: 1, Weapons: each direct "Nx
                                       // WeaponName" flattened into N repeated entries - "2x Storm
                                       // bolter" on Impulsor becomes two "Storm bolter" list entries,
                                       // not a partition attempt, since a single model can't have
                                       // "alternative" sub-groups by definition).
                                       // An EXPLICIT "Nx ModelName" header's nested weapons DO go
                                       // through the shared-vs-alternating partition, regardless of
                                       // the header's own count (even count=1, e.g. each of Masters
                                       // of the Maelstrom's five named characters - trivially: every
                                       // weapon's own count equals the header's total of 1, so all
                                       // are "shared", producing one sub-group). Partition rule: a
                                       // weapon whose count equals the header's total is common to
                                       // every resulting sub-group; every OTHER weapon is its own
                                       // independent sub-group carrying that weapon's own count -
                                       // never merged with a different weapon sharing the same
                                       // count (verified against a second real squad, `gw-app-
                                       // export-3-dp.txt`'s Company Veteran: "1x Master-crafted bolt
                                       // rifle" / "1x Master-crafted heavy bolter" are two distinct
                                       // 1-model sub-groups, not one 2-weapon sub-group - grouping
                                       // by count value instead of by weapon entry was the original,
                                       // wrong draft of this rule). Throws ArmyListParseException
                                       // (carrying UnitName/RawText) when the non-common counts
                                       // don't sum exactly to the header's total, rather than
                                       // guessing a split.

ArmyListParseException(message, unitName?, rawText?)
```

### Army Roster Enrichment (`ProbHammer.Core.Domain.Catalogue.Bsdata` + `Domain.Roster`)

```
BsdataFactionResolver.ResolveStartingFileName(faction, availableFileNames) -> string
                                       // suffix-matches only the most specific (last) Faction entry:
                                       // a file named exactly "{entry}.json" or ending
                                       // " - {entry}.json", excluding any name containing "Library"
                                       // (shared cross-sub-faction content, never a playable faction
                                       // identity itself, even though it can hold the bulk of a real
                                       // faction's content). Throws BsdataFactionResolutionException
                                       // on zero or multiple matches.

ResolvedBsdataCatalogue(Closure, IdIndex, GroupIdIndex, ProfileIdIndex)
                                       // bundles a resolved BsdataClosure with the three
                                       // BsdataNameResolver indices built over it - everything
                                       // needed to resolve names and build Datasheets, in one value.
  Build(source, startingFileName) -> ResolvedBsdataCatalogue   // static factory
  ResolveDatasheet(entryName) -> Datasheet                     // throws BsdataNameResolutionException
                                                                // (with a "did you mean...?"
                                                                // suggestion) on a miss

BsdataCatalogueCache(source)          // app-wide cache of ResolvedBsdataCatalogue, keyed by
  GetOrBuild(startingFileName) -> ResolvedBsdataCatalogue
                                       // starting file name, ConcurrentDictionary-backed, populated
                                       // lazily, valid for the lifetime of whatever holds the
                                       // instance (a singleton registered in ProbHammer.Web's
                                       // Program.cs) - the expensive part of enrichment (multi-file
                                       // JSON parse, BFS import resolution, index building) is
                                       // static per faction and identical for every user, so it's
                                       // built at most once per starting file, never per request or
                                       // per session.

BsdataNameNormalization.Normalize(text) -> string
                                       // typographic (U+2019) -> plain ASCII apostrophe, applied to
                                       // every piece of export text before any BSData lookup (real
                                       // confirmed cases: "Emperor's Champion", "Reaver's blade").

BsdataNameSuggestion.FindClosest(target, candidates, maxDistance = 3) -> string?
                                       // Levenshtein-based "did you mean...?" hint attached to a
                                       // resolution-failure exception's message - diagnostic only,
                                       // never changes whether a name resolves. Chosen over a
                                       // curated mismatch map (confirmed real case: the captured
                                       // export's "Absolvor bolt pistol" vs. BSData's own "Absolver
                                       // bolt pistol", a genuine BSData spelling difference) - the
                                       // corpus is too large/fluid for a hand-maintained table to
                                       // stay complete; an edit-distance check against whatever
                                       // names actually exist keeps working as the corpus changes.

ArmyRosterEnricher.Enrich(ParsedArmyList, ResolvedBsdataCatalogue) -> ArmyRoster
                                       // Every name resolution is eager (validated during the walk,
                                       // not deferred to later aggregation), each failure throwing
                                       // BsdataNameResolutionException with a "did you mean...?"
                                       // suggestion. Builds one AttachedUnit per ParsedAttachmentGroup
                                       // (Bodyguard/Attached built the same way as a standalone Unit)
                                       // and one plain Unit per standalone ParsedUnit.
                                       // Unit name resolution: ResolvedBsdataCatalogue
                                       // .ResolveDatasheet, normalized first.
                                       // Statline name resolution (per ParsedModelGroup): tries an
                                       // exact match against the Datasheet's own Statlines first
                                       // (the common case - see the parser's own note above on why
                                       // this needs no BSData name-matching for a datasheet like
                                       // Crusader Squad), then two independent fallbacks, BOTH
                                       // confirmed necessary against the live BSData clone - neither
                                       // subsumes the other:
                                       //   1. The Datasheet's own Name, when it's itself one of
                                       //      several Statline names (confirmed: Scout Squad,
                                       //      Legionaries, Eradicator Squad - every troop model
                                       //      shares the squad's own name as its Unit profile name,
                                       //      alongside a separately-named Sergeant/Champion
                                       //      statline).
                                       //   2. The Datasheet's sole Statline, when it has exactly
                                       //      one (confirmed: Emperor's Champion -> "The Emperor's
                                       //      Champion", Impulsor -> "Black Templars Impulsor",
                                       //      Ancient -> "Primaris Ancient", Sword Brethren Squad ->
                                       //      "Sword Brethren" - none of these match the export
                                       //      label or the Datasheet's own name, but with only one
                                       //      Statline there's no ambiguity about what a model group
                                       //      refers to regardless of what it's called).
                                       // Wargear resolution (per weapon name in a ParsedModelGroup):
                                       // tries an exact WeaponProfile match, then a multi-profile
                                       // expansion (some real wargear choices resolve to more than
                                       // one weapon profile - confirmed: Helbrecht's "Sword of the
                                       // High Marshals" -> "➤ ... - Sweep" / "➤ ... - Strike", each
                                       // profile named "{leading marker}{ExportedName} -
                                       // {mode}" - every matching profile is included), then a
                                       // Datasheet.Abilities match by name (some "weapon" lines
                                       // aren't weapons at all - confirmed: Impulsor's "Shield
                                       // Dome", a BSData Abilities-typeName profile granting a 5+
                                       // invulnerable save - attached to that ModelLine's own
                                       // Abilities instead of thrown as an unresolved weapon).

BsdataFactionResolutionException(faction, message)   // carries the offending Faction entry
BsdataNameResolutionException(text, message)         // carries the offending text
```

`Datasheet` (Catalogue Context above) gained `TryGetStatline`/`TryResolveWeaponProfile` (non-
throwing variants) and `WeaponNames` (`IReadOnlyList<string>`) specifically to support the above -
diagnostic/fallback lookups that need to check existence or enumerate candidates without either
throwing on a routine miss or breaking `ResolveWeaponProfile`'s existing on-demand-only contract.

### Session-Backed Import (`ProbHammer.Web`)

```
ISessionArmyListStore.Save(ISession, ParsedArmyList)
                     .Load(ISession) -> ParsedArmyList?
                                       // plain System.Text.Json round-trip through ISession's string
                                       // storage - ParsedArmyList and everything it references are
                                       // plain records of primitives/lists, no Datasheet/abstract
                                       // WeaponProfile graph to serialize. The session stores only
                                       // this intermediate, never the built ArmyRoster (see "Session
                                       // stores the intermediate, not the graph" in design.md) - every
                                       // request re-enriches fresh via IArmyRosterProvider.

IArmyRosterProvider.Build(ParsedArmyList) -> ArmyRoster
                                       // shared three-step orchestration (BsdataFactionResolver ->
                                       // BsdataCatalogueCache.GetOrBuild -> ArmyRosterEnricher.Enrich)
                                       // used by both the import page (to validate before writing to
                                       // session) and /LivePlay's per-request rebuild - one place for
                                       // a sequence needed at three call sites (import, LivePlay GET,
                                       // the casualty sync endpoint).

ImportModel (/Import Razor Page)      // paste box + submit. OnPost parses, then calls
                                       // IArmyRosterProvider.Build to validate BEFORE calling
                                       // ISessionArmyListStore.Save - so a failed re-import never
                                       // touches a previously-successful session import. Catches
                                       // ArmyListParseException / BsdataFactionResolutionException /
                                       // BsdataNameResolutionException / AmbiguousCharacteristicException
                                       // and reports the message on the page rather than crashing;
                                       // any other exception type is an unhandled bug, not a
                                       // reportable import failure.

LivePlayModel.OnGet()                 // loads the session's ParsedArmyList; redirects to /Import if
                                       // absent (live-play-view's "Live Play Redirects Without An
                                       // Active Import"), otherwise builds via IArmyRosterProvider and
                                       // renders exactly as before. RebuildRoster (casualty rebuild)
                                       // now takes the roster's Units as an explicit parameter
                                       // instead of reading Examples.View.MyArmyRoster() internally.
LivePlayCasualtyService.SyncAsync()   // loads the session's ParsedArmyList itself (via
                                       // ISessionArmyListStore) before calling RebuildRoster; an
                                       // empty batch OR no active session import both short-circuit
                                       // to an empty response.
```

`Program.cs` reintroduces `AddDistributedMemoryCache`/`AddSession`/`UseSession` (removed by
`archive-10e-pipeline` as a 10e-only dependency; brought back here for a new purpose - per-session
`ParsedArmyList` storage, not the old `Enricher` cache) and registers `BsdataCatalogueCache` as a
singleton over a `LocalDiskBsdataCatalogueSource` rooted at `Bsdata:RootDirectory` (appsettings.json,
default `"BsData"`, resolved against `IWebHostEnvironment.ContentRootPath`) - the bundled snapshot
at `src/ProbHammer.Web/BsData/`, copied into the Docker image as its own layer (see
`catalogue-json-ingestion`'s packaging notes), not a live GitHub fetch.

---

## Roster Context

```
ModelLine(StatlineName, Weapons: IReadOnlyList<string>, Count, Abilities: IReadOnlyList<Ability>,
          Keywords: IReadOnlySet<string> = [])
                     // Keywords scoped to this specific model-line, distinct from its Datasheet's
                     // Keywords - e.g. a Psyker keyword on one named individual within a shared
                     // statline. Case-insensitive, matching Datasheet.Keywords's convention.
  RemainingCount   // live, adjustable in both directions; alive/dead granularity only, clamped to [0, Count]
  SetRemainingCount(value)   // the one primitive both directions reduce to, clamped to [0, Count]
  RemoveCasualties(n)         // thin wrapper: SetRemainingCount(RemainingCount - n)

Unit : ICombatUnit
  Datasheet, Enhancements, ModelLines
  IsPresent   // any model-line with RemainingCount > 0
  Components -> [this]

AttachedUnit : ICombatUnit
  Bodyguard: Unit, Attached: IReadOnlyList<Unit>   // open collection, not fixed Leader/Support slots
  Components -> [Bodyguard, ..Attached]

ICombatUnit
  Components: IReadOnlyList<Unit>   // Composite-pattern shape; lets aggregate-view logic
                                     // treat a plain Unit and an AttachedUnit uniformly
  Name: string                      // computed, not stored - see Pure functions below
```

**Pure functions (not stored state):**
- `KeywordResolution.EffectiveKeywords(ICombatUnit)` — union of `Keywords` over currently-present
  components' Datasheets, plus the `Keywords` of whichever of those components' `ModelLine`s are
  currently present (`RemainingCount > 0`) — e.g. a `Psyker` keyword scoped to one named
  individual within a shared statline (`ChaosSpaceMarineSquad`-shaped fixtures). Recomputes live;
  never cached, so it can't go stale when a component or model-line is destroyed. Model-level
  keyword checks must read a specific `Unit.Datasheet.Keywords` together with that specific
  model's own `ModelLine.Keywords` directly, never a sibling `ModelLine`'s and never this union.
- `ToughnessResolution.ResolveDefendingToughness(AttachedUnit)` — highest Toughness among the
  Bodyguard's present models if any remain, else highest among present Leader/Support models.
  Domain fact only; not wired to `Simulation/*` in this change.
- `ICombatUnit.Name` — computed on read, not stored, same rationale as the two rules above: neither
  BattleScribe/NewRecruit exports nor GW's own app support free-form per-instance unit naming, so
  there is no external source a stored value could ever be populated from. `Unit.Name` is its
  Datasheet's name. `AttachedUnit.Name` is a humanized join of the Bodyguard's name with the
  Attached units' names: none attached → Bodyguard name alone; one → `"{Bodyguard} with {A}"`; two
  → `"{Bodyguard} with {A} and {B}"` (no comma); three or more → an Oxford-comma join
  (`"{Bodyguard} with {A}, {B}, and {C}"`). Duplicate attached-leader names are not deduplicated
  (e.g. two Marshals render as `"...with Marshal and Marshal"`) — a known, accepted limitation.

**Aggregate view** (`AttachedUnitAggregator.Build(ICombatUnit) -> AttachedUnitAggregateView`):

```
AttachedUnitAggregateView(Name: string, IsAttachedUnit: bool, Statlines, Weapons,
                           Abilities, Keywords)
  // Name is the source combatUnit.Name (see ICombatUnit above), copied through unchanged.
  // IsAttachedUnit is `combatUnit is AttachedUnit`, set in Build - lets a page-layer consumer
  // (e.g. /LivePlay's unit-card ordering) distinguish an AttachedUnit-sourced view from a plain
  // Unit-sourced one without re-deriving it from Statlines/Weapons shape. True even for an
  // AttachedUnit with zero Attached units, since it's a fact about the source type, not the
  // rendered content (which is identical to a plain Unit's in that case).

ModelLineLoadout(WeaponsLabel, RemainingCount, InitialCount)
  // WeaponsLabel is ModelLine.Weapons comma-joined, e.g. "Bolt pistol, Heavy Bolt pistol, Power fist"

AggregateStatlineEntry(ComponentName, StatlineName, Statline, RemainingCount, InitialCount,
                        Loadouts: IReadOnlyList<ModelLineLoadout>)
  // ComponentName is the owning component's Datasheet.Name (same convention as
  // WeaponContribution.ComponentName below) - lets a downstream consumer (e.g. /LivePlay's
  // adjacent-statline-tile grouping) detect component boundaries without re-deriving them from
  // Statlines' declared order.
  // RemainingCount/InitialCount are summed across every ModelLine sharing StatlineName, including
  // model-lines that have been fully removed as casualties. Loadouts has one entry per
  // contributing ModelLine (not collapsed by weapon list) so a fully-wiped loadout-variant still
  // shows up as 0/InitialCount instead of silently disappearing.

WeaponContribution(ComponentName, StatlineName, Count, PerModelAttacks)
  // ComponentName is the owning Unit.Datasheet.Name; Count is that ModelLine's RemainingCount at
  // build time.

AggregateWeaponEntry(Profile: WeaponProfile, TotalAttacks: DiceExpression,
                      Contributions: IReadOnlyList<WeaponContribution>)
  // Profile is retained for its identity fields (Name/Type/Range/Skill/S/Ap/D/ability flags) only.
  // Profile.A is NOT authoritative once a row has merged more than one contribution - it's
  // whichever contributor's WeaponProfile happened to be inserted first. Only TotalAttacks is
  // safe to render.

AggregateAbilityEntry(ComponentName: string, StatlineName: string?, Ability: Ability)
  // StatlineName is null for a Datasheet-sourced ability (Unit.Datasheet.Abilities - not tied to
  // any one model-line, applies to the whole component) and set to a specific statline name for a
  // ModelLine-sourced ability (that ModelLine's own Abilities, e.g. Enhancement-conferred). This
  // applies regardless of Ability.Scope: Scope alone decides which UI column (Model vs Unit) an
  // entry belongs in; source alone decides which statline row(s) it binds to. No cross-component
  // combination or deduplication - two components each having an ability of the same Name produce
  // two separate entries.
```

- `Statlines` — built by walking components in display order (an `AttachedUnit`'s `Attached` list,
  in order, then its `Bodyguard`; a plain `Unit` is just itself), and within each component, that
  component's own `Datasheet.Statlines` in declared order. For each declared name, only that
  component's own `ModelLine`s are matched (case-insensitive) — never another component's — so
  per-name grouping is scoped to a single component and two components can never merge into one
  entry even if they happen to share a statline name (confirmed acceptable: the real 40k attachment
  rules attach by Leader/Support ability, not by name, and don't allow two identically-named
  Leader/Support units on the same Bodyguard, so this can't occur in valid data). A statline name's
  entry is built from *every* model-line of that component sharing the name, regardless of that
  specific line's own `RemainingCount` (unlike `Weapons` below), and — since `casualty-tracking`
  (2026) — is included whenever the component has *any* model-line referencing that name at all,
  even once every one of them reaches `RemainingCount == 0` (reporting `RemainingCount: 0` against
  its unchanged `InitialCount`, rather than disappearing). It is omitted only when the name was
  never referenced by any of the component's model-lines to begin with (unselected loadout
  variant). This flip — from "drop once dead" to "persist at zero" — exists specifically so
  `/LivePlay`'s casualty-marking control always has a row to revert from; see
  `openspec/specs/live-play-view/spec.md`'s "Statline Section Rendering" for the paired UI behavior
  (the run collapses to header-only once every entry in it is fully dead, a second trigger
  alongside the pre-existing fully-deselected one) and PROGRESS.md for the implementation summary.
- `Weapons` — grouped by `WeaponProfile.EqualityKey()` (Type/Skill/S/Ap/D/ability flags — excludes
  Name/Range/Attacks) across only `RemainingCount > 0` model-lines. `TotalAttacks` is computed per
  contribution as `PerModelAttacks.Scale(modelLine.RemainingCount)`, `Add`-reduced across every
  contributor sharing the `EqualityKey` — **not** a representative contributor's raw `A` with only
  the model count summed. (That was the bug this shape fixes: two model-lines sharing an
  `EqualityKey` with different per-model Attacks, e.g. 4 models × A3 and 1 model × A7, must total
  19, not silently keep one contributor's A and report Count=5.)
- `Abilities` — built by `BuildAbilities`, walking components in the same display order
  `BuildStatlines` uses. For each component where `IsPresent`: one entry per
  `Datasheet.Ability` (`StatlineName: null`), then one entry per `Ability` on each of that
  component's own `ModelLine`s with `RemainingCount > 0` (`StatlineName:` that line's own name).
  No cross-component combination or deduplication, regardless of `Ability.Scope` — mirrors
  `Statlines`' per-component scoping exactly. The explicit `IsPresent` guard exists only for
  Datasheet-sourced entries: unlike `BuildStatlines`, where a fully-dead component naturally
  produces zero entries through its per-statline `RemainingCount` check, a Datasheet ability has
  no statline-level gate of its own to fall through. A `ModelLine`-sourced entry (any Scope)
  disappears once that line's `RemainingCount` reaches 0, since it's simply excluded from that
  component's model-line scan on the next `Build`.
- `Keywords` — wired directly to `KeywordResolution.EffectiveKeywords`.

`AttachedUnitAggregator.Build` computes one `RemainingCount > 0`-filtered `presentLines` list from
`combatUnit.Components`, feeding only `BuildWeapons` (a fully-dead model-line contributes nothing
to a weapon total). `BuildStatlines` and `BuildAbilities` both take `combatUnit` directly instead
of a pre-flattened line list, sharing a private `ComponentDisplayOrder` helper — both already need
each component's own `Datasheet` (for declared statline order, or for `Datasheet.Abilities`), so
each walks `component.ModelLines` itself per component rather than being fed a cross-component
flattened set; this is also what makes per-component merge scoping fall out for free in both, with
no separate sort step that could disagree with the grouping. Unfiltered access to a component's
`ModelLine`s (so fully-dead loadout-variants and correct `InitialCount`s stay visible in
`Statlines`) happens naturally since `BuildStatlines` reads `component.ModelLines` directly rather
than through the `presentLines` filter.

**`ArmyRoster`** (`Domain/Roster/ArmyRoster.cs`) wraps the per-unit roster (`Units`, an
`IReadOnlyList<ICombatUnit>`) with the army-level metadata no `Unit`/`AttachedUnit`/`Datasheet`
carries: `Name`, `PointsSpent`, `Faction` (ordered — parent codex before sub-faction, e.g.
`["Space Marines", "Black Templars"]`, or a single entry when there's no split), `Detachments`
(ordered by selection, one entry per detachment a battle size's detachment points bought),
`ForceDisposition`, `BattleSize`, and `PointsLimit`. Originally added ahead of the (then-unbuilt)
GW-app export parser so that parsing work would have a settled target type instead of inventing
one mid-design; the shape and sample values (`Examples/View.Roster()`) were validated against a
real captured export (`data/gw-app-export.txt`) before the parser existed, and
`import-army-list-for-live-play`'s real `ArmyListParser`/`ArmyRosterEnricher` now populate this
exact shape from a real pasted export (see "Army List Import Pipeline" above) - the bet paid off
unchanged. `PointsSpent` is still plain sample data on the `Examples/` fixture path — it is not
derived from or reconciled against `Units`, since no points value is tracked anywhere below
`ArmyRoster` (consistent with the no-points-modeling omission below); a real import's `PointsSpent`
comes from the export text itself via `ArmyListParser`. `Examples/View.MyArmyRoster()` remains a
thin projection over `View.Roster().Units`, but no longer has any `/LivePlay` call site —
`Examples/` stays in the tree for future domain-model exploration, unreferenced by `Web`.

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
