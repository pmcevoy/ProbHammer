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
for future domain-model exploration. Since `import-battlescribe-json-rosters`, `/Import` also
recognizes and routes a BattleScribe/NewRecruit roster JSON export through a second, independent
pipeline (`Domain/Import/BattleScribe/` — see "BattleScribe/NewRecruit JSON Import Pipeline" below)
that synthesizes this same model directly from the JSON's own already-resolved data, with no BSData
involvement at all. Coexists with the 10th-edition model (`.claude/domain-model.md`), which remains
in the tree, untouched and fully unused (superseded, not just paused — see `PROGRESS.md` for that
history).

---

## Namespaces

- `ProbHammer.Core.Domain.Catalogue` — reference/rules data (Catalogue context)
- `ProbHammer.Core.Domain.Roster` — army-list composition and live game state (Roster context)

---

## Catalogue Context

```
Datasheet
  Name, FactionKeywords, Keywords, Abilities (unit-wide, intrinsic only - see below)
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
  TryResolveAbility(name) -> Ability?                  // on-demand only, never enumerated - since
                                                      // resolve-enhancement-abilities, mirrors
                                                      // ResolveWeaponProfile exactly. Resolves an
                                                      // optional ability (an Enhancement, or any
                                                      // other ability grant nested inside its own
                                                      // selection entry, e.g. Impulsor's "Shield
                                                      // Dome") that the public Abilities list
                                                      // deliberately excludes - see
                                                      // BsdataDatasheetMapper's classification,
                                                      // below, and ArmyRosterEnricher's wargear/
                                                      // Enhancement resolution, in "Army List
                                                      // Import Pipeline".
  OptionalAbilityNames: IReadOnlyList<string>          // diagnostic enumeration only (a "did you
                                                      // mean...?" suggestion source), never a full
                                                      // options menu - mirrors WeaponNames exactly.
  ctor(..., statlines: IReadOnlyList<(string Name, Statline Statline)>, weaponProfiles: IEnumerable<WeaponProfile>,
       optionalAbilities: IEnumerable<Ability>? = null)
                                                      // weaponProfiles keyed internally by .Name;
                                                      // optionalAbilities likewise, defaulting to
                                                      // empty (most datasheets define none) -
                                                      // same "optional trailing parameter" pattern
                                                      // as ModelLine's own Keywords parameter.

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

Ability(Name, Text, Choices: IReadOnlyList<AbilityChoice>, Scope: Model | Unit, Origin: AbilityOrigin)

AbilityOrigin = Intrinsic | Enhancement | OptionalGrant | CoreRule | ArmyRule
                                                      // since resolve-enhancement-abilities (first
                                                      // three) / resolve-core-rule-abilities
                                                      // (CoreRule) - classifies where an ability
                                                      // came from during BsdataDatasheetMapper's
                                                      // walk, not a player-facing concept the
                                                      // export text ever states directly. ArmyRule
                                                      // is a CoreRule-shaped reference whose own
                                                      // Name matches classify-known-army-rules'
                                                      // curated per-faction `ArmyRuleNameLookup`
                                                      // table (e.g. Oath of Moment, Templar Vows,
                                                      // Nurgle's Gift (Aura)) - see
                                                      // BsdataDatasheetMapper's "Core Versus Army
                                                      // Rule Origin Classification" below.
                                                      // Intrinsic: a fact about the datasheet,
                                                      // always in Datasheet.Abilities. Enhancement:
                                                      // found nested inside a "type: upgrade"
                                                      // selection entry whose nearest enclosing
                                                      // selection group's own Name contains
                                                      // "Enhancements" (case-insensitive substring -
                                                      // confirmed necessary for a nested sub-pool
                                                      // like "Legends of Saga and Song
                                                      // Enhancements", not just a group literally
                                                      // named "Enhancements"). OptionalGrant: any
                                                      // other ability nested inside its own "type:
                                                      // upgrade" selection entry (e.g. Impulsor's
                                                      // "Shield Dome"). CoreRule: a datasheet-wide
                                                      // reference to a separately-defined Core or
                                                      // faction rule (Oath of Moment, a Chapter's
                                                      // own Vows, Deadly Demise, Firing Deck,
                                                      // Infiltrators, Scouts) - always in Datasheet
                                                      // .Abilities like Intrinsic, never one of the
                                                      // on-demand optional grants, since it's never
                                                      // something a player did or didn't select -
                                                      // see BsdataDatasheetMapper's "Core Rule
                                                      // Ability Extraction" below. Enhancement and
                                                      // OptionalGrant abilities are resolvable only
                                                      // via Datasheet.TryResolveAbility, never
                                                      // enumerated in the public Abilities list -
                                                      // see BsdataDatasheetMapper's "Ability
                                                      // Extraction and Classification" below.
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
                                       // and every "Abilities"-typeName profile as an Ability (Text
                                       // carried through verbatim, Scope always AbilityScope.Unit -
                                       // BSData JSON gives no per-ability scope signal) -
                                       // classified and routed into either Datasheet.Abilities or
                                       // Datasheet's on-demand-only index (see "Ability Extraction
                                       // and Classification" below, added by
                                       // resolve-enhancement-abilities). entryLinks are resolved
                                       // via idIndex/groupIdIndex; infoLinks of type "profile" are
                                       // resolved via profileIdIndex against
                                       // BsCatalogue.SharedProfiles - some model statlines exist
                                       // only this way (real example: Chaos - Chaos Space
                                       // Marines.json's "Legionaries" squad reaches its troop
                                       // model's Unit profile through a profile-type infoLink, not
                                       // nested in any selectionEntry's own Profiles list).
                                       // FactionKeywords/Keywords are always empty - BSData's
                                       // categoryLinks could plausibly source them, but no
                                       // requirement in this change's spec governs that mapping, so
                                       // it's left unattempted rather than guessed. The walk also
                                       // threads a per-branch ancestry list (every BsSelectionEntry
                                       // from the walk root down to the current one, reset to a
                                       // fresh chain on each entryLink hop) - originally added
                                       // purely for InSv resolution (see "Invulnerable Save
                                       // Resolution" below), now also read by ability
                                       // classification (below) - plus, since
                                       // resolve-enhancement-abilities, the nearest enclosing
                                       // BsSelectionEntryGroup's own Name, threaded and reset the
                                       // same way, read only by ability classification.

BsdataDatasheetMapper's Ability Extraction and Classification (resolve-enhancement-abilities)
                                       // An "Abilities"-typeName profile is Intrinsic (populates
                                       // the public Datasheet.Abilities, deduped by name) when the
                                       // nearest entry in the walk's ancestry chain - the one that
                                       // actually made this profile reachable, whether directly
                                       // (entry.Profiles) or through a "profile"-type infoLink it
                                       // declared - is NOT `"type": "upgrade"`. A `"type": "upgrade"`
                                       // owning entry means the profile is optional (an Enhancement,
                                       // or any other ability grant nested inside its own selection
                                       // entry, e.g. Impulsor's "Shield Dome") - it's routed instead
                                       // into Datasheet's on-demand-only index (TryResolveAbility/
                                       // OptionalAbilityNames), never the public Abilities list,
                                       // mirroring WeaponProfile's own on-demand-only design exactly.
                                       // An optional ability is further classified Enhancement (vs.
                                       // a plain OptionalGrant) when the nearest enclosing selection
                                       // group's own Name contains "Enhancements" (case-insensitive
                                       // substring, not exact match - confirmed necessary against a
                                       // nested sub-pool like "Legends of Saga and Song Enhancements",
                                       // which is the *directly* enclosing group for its own entries,
                                       // not the outer "Enhancements" group one hop up). Found via a
                                       // real user-reported bug: /LivePlay showed unselected
                                       // Enhancements (Black Templars' "Fervent Exemplars"/
                                       // "Inheritors of Sigismund" on Sword Brethren Squad; a
                                       // Space-Wolves-only "Thirst for Glory" on Crusade Ancient) on
                                       // every eligible unit regardless of what the export actually
                                       // selected - Datasheet.Abilities had no on-demand protection
                                       // at all, unlike WeaponProfile. See
                                       // openspec/changes/resolve-enhancement-abilities/ (design.md
                                       // in particular) for the full investigation and rejected
                                       // alternatives (a primary-catalogue/detachment-gating fix was
                                       // considered and explicitly rejected - it doesn't generalize,
                                       // since the Sword Brethren case is genuine, correctly-scoped
                                       // Black Templars content that simply wasn't selected).

BsdataDatasheetMapper's Core Rule Ability Extraction (resolve-core-rule-abilities)
                                       // A fourth, structurally distinct ability-sourcing shape,
                                       // alongside the three above: an `infoLink` with `"type":
                                       // "rule"` (confirmed real examples: Impulsor's "Oath of
                                       // Moment"/"Deadly Demise"/"Firing Deck" infoLinks, Scout
                                       // Squad's "Scouts"/"Infiltrators"/"Oath of Moment", every
                                       // Black Templars datasheet's own "Templar Vows") - GW's own
                                       // app shows these as "Core"/"Faction Abilities" above
                                       // Datasheet Abilities; NewRecruit lists them as "Rules"
                                       // underneath Abilities. Unlike every other ability shape,
                                       // its Text isn't carried on the profile itself - it's
                                       // resolved by looking the infoLink's own Name up against
                                       // the closure's RuleGlossary (rules-glossary-popovers),
                                       // the same glossary a [BRACKET] cross-reference resolves
                                       // against. Its display Name is the infoLink's own Name plus
                                       // any `"append"`/`field: "name"` modifier's value found
                                       // directly on the infoLink (real shapes: a JSON string for
                                       // "Deadly Demise" + "D3", a JSON number for "Firing Deck" +
                                       // 6 - both must be handled). Extracted into
                                       // Datasheet.Abilities with Origin CoreRule or ArmyRule (see
                                       // "Core Versus Army Rule Origin Classification" below) -
                                       // always exposed, like Intrinsic, never routed through the
                                       // on-demand OptionalAbilities index, since a Core/faction
                                       // rule is never something a player did or didn't select.
                                       // An unresolvable rule name (the id isn't reachable in this
                                       // closure) is skipped silently, mirroring WalkLink's own
                                       // "target not found... skipped rather than failing
                                       // resolution" convention - caught in aggregate by the
                                       // Full-Corpus InfoLink-Type Scan below, not by a single
                                       // import failing.
                                       //
                                       // Since gate-and-dedupe-core-rule-abilities: the resolved
                                       // rule's own `hidden`/`modifiers` are ALSO checked (via
                                       // IsGameModeGated, generalized - see its own doc comment
                                       // below) before the Ability is built - a Core rule can be
                                       // chapter/sub-faction-exclusive (confirmed real shapes:
                                       // Oath of Moment's rule definition is hidden unless the
                                       // army's primary catalogue is one of 11 named chapters,
                                       // Black Templars deliberately excluded; Templar Vows' is
                                       // hidden unless it specifically IS Black Templars, by
                                       // catalogue id). Found via a real user-reported bug: a
                                       // Black Templars roster showed BOTH Oath of Moment and its
                                       // own chapter-exclusive replacement Templar Vows together;
                                       // running the same pipeline against a Salamanders closure
                                       // produced the identical (and therefore equally wrong)
                                       // list. A gated reference produces no Ability, the same
                                       // silent-skip treatment as an unresolvable rule name. This
                                       // gating decides only whether an Ability is produced at
                                       // all - it has no bearing on Origin (see next paragraph),
                                       // and is untouched by classify-known-army-rules.
                                       //
                                       // Core Versus Army Rule Origin Classification
                                       // (classify-known-army-rules): Origin is ArmyRule when the
                                       // resolved rule's own Name matches one of the known
                                       // army-wide rule names for the roster's own Faction, per
                                       // `ArmyRuleNameLookup.Resolve` (Domain.Catalogue, decoupled
                                       // from Bsdata so the BattleScribe/JSON pipeline below can
                                       // use it too) - CoreRule otherwise. `ArmyRosterEnricher`
                                       // resolves this name set once per roster from
                                       // `parsedArmyList.Faction` and threads it through
                                       // `ResolvedBsdataCatalogue.ResolveDatasheet` into
                                       // `BuildDatasheet`'s `knownArmyRuleNames` parameter (fail-
                                       // open default: empty, same convention as
                                       // `forceEntries`/`primaryCatalogueId` - every reference
                                       // classifies CoreRule when omitted). This REPLACED an
                                       // earlier structural-only signal - "does this rule's own
                                       // gating carry a `scope: primary-catalogue` condition
                                       // anywhere" (the same signal `IsGameModeGated`/
                                       // `IsConditionProvablyTrue` above still read for the
                                       // gating decision) - once real corpus data proved that
                                       // signal unreliable: `Assigned Agents` (Agents of the
                                       // Imperium), `Disparate Paths` (Aeldari), and `Corsairs and
                                       // Travelling Players` (Drukhari) all carry the identical
                                       // gating shape but are mustering/composition rules, not
                                       // army-wide gameplay rules - the old signal misclassified
                                       // all three as ArmyRule whenever a roster included one of
                                       // those allied units. See `ArmyRuleNameLookup`'s own doc
                                       // comment for the curated inclusion table (verified against
                                       // the bundled corpus) and exclusion list (confirmed
                                       // non-army-rule names sharing the gating shape, consulted
                                       // only by bsdata-corpus-scan's Full-Corpus Primary-
                                       // Catalogue-Gated Rule Triage Scan, never at runtime) - and
                                       // openspec/changes/classify-known-army-rules/design.md for
                                       // the full investigation, including why an OR of the two
                                       // signals couldn't fix the false-positive class (it can
                                       // only ever add matches, never remove a wrong one). A second,
                                       // separate scan (`ArmyRuleNameCoverageScanTests`) enumerates
                                       // every real playable-faction catalogue file in the live
                                       // clone and requires each to be an explicit key in
                                       // `ArmyRuleNameLookup.Entries` - a populated name list, or a
                                       // deliberately empty one for a faction confirmed to have no
                                       // single unifying army-wide rule - so a faction can never
                                       // silently sit uncovered with neither; added after this
                                       // change's own initial table turned out to omit roughly half
                                       // the real corpus (Orks, Necrons, T'au Empire, and 10 others),
                                       // found only by manually diffing the file list, not by any
                                       // automated check.
                                       //
                                       // Critically, a "type: rule" infoLink ALSO appears nested
                                       // inside a "type: upgrade" weapon/wargear-option entry
                                       // (confirmed real shape: the Impulsor's "Ironhail Skytalon
                                       // Array" weapon-option entry carries its own "Sustained
                                       // Hits"/"Anti" infoLinks describing THAT WEAPON's own
                                       // Keywords characteristic, redundant with what
                                       // WeaponAbilityTags/RuleGlossary already independently
                                       // resolve for that weapon's own keyword chip) - resolving
                                       // against the glossary just as successfully as a genuine
                                       // Core rule reference, but semantically unrelated. This is
                                       // excluded via the exact same ancestry signal
                                       // ProcessAbilityProfile already uses to distinguish
                                       // Intrinsic from optional (nearest ancestor entry's own
                                       // Type == "upgrade"): found not via design-time inspection
                                       // but by running the fix against a real Black Templars
                                       // export before shipping - the first working version leaked
                                       // every weapon's own keyword rule-references into
                                       // Datasheet.Abilities as bogus unit-wide entries
                                       // ("Sustained Hits", "Anti", "Melta", "Rapid Fire", "Blast"
                                       // all appearing on the Impulsor).
                                       //
                                       // A third infoLink type, "infoGroup", was found by this
                                       // change's own new corpus scan (129 occurrences) and is a
                                       // confirmed, real, NOT-yet-fixed ability-granting gap of the
                                       // same class (targets a `sharedInfoGroups`/`infoGroups`
                                       // container with its own nested `profiles` array of real
                                       // Abilities content - confirmed: Adeptus Custodes' "Talons"
                                       // holds two genuine, mutually-exclusive-by-modifier auras,
                                       // "Null Aegis (Aura)" and "Deadly Unity (Aura)") -
                                       // deliberately allowlisted rather than fixed here (no real
                                       // infoGroup-carrying export was available to verify against
                                       // at the time), tracked as a dedicated follow-up change. See
                                       // tests/.../CorpusScan/InfoLinkTypeAllowlist.cs.

BsdataDatasheetMapper's Full-Corpus InfoLink-Type Scan (resolve-core-rule-abilities)
                                       // Same permanent, manually-triggered `[Fact(Explicit =
                                       // true)]` pattern as the other CorpusScan tests (see
                                       // "Full-Corpus Scan Tests" below) - added specifically
                                       // because "rule" was itself once a silently-dropped
                                       // infoLink type, the same way an unrecognized weapon
                                       // keyword token or characteristic shape already gets a
                                       // dedicated scan. Walks every closure's own `infoLinks` at
                                       // every level (entry, group, nested - not through
                                       // BuildDatasheet, which has no way to report "I saw this
                                       // infoLink and did nothing with it"), collecting the
                                       // distinct `Type` value of each, and asserts that set
                                       // against InfoLinkTypeAllowlist.cs's maintained set
                                       // (`"profile"`, `"rule"` as handled; `"infoGroup"` as a
                                       // confirmed, tracked, not-yet-fixed gap - see above).

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
                                       // sets `hidden: true` PROVABLY always true in matched play
                                       // (see IsProvablyAlwaysTrueInMatchedPlay - resolve-
                                       // enhancement-abilities replaced this method's original
                                       // "plain existence check anywhere in the condition tree,
                                       // regardless of AND/OR structure" with real, if still narrow,
                                       // 2-value boolean evaluation: a gate-id condition combined
                                       // via AND with an unrelated, unevaluated condition is no
                                       // longer treated as gated, since that other condition could
                                       // make the whole AND false; combined via OR, it still is,
                                       // since OR only needs one always-true branch. Found via a
                                       // second real user-reported bug, after this file's own
                                       // resolve-enhancement-abilities change shipped: Black
                                       // Templars' "Companions of Vehemence Enhancements" - a real,
                                       // always-available Detachment Enhancement pool (Oathbound
                                       // Exemplar, Incendiary Animus, Merciless Denunciation,
                                       // Zealous Vanguard) - is hidden by `AND(forces(Crusade Force)
                                       // < 1, selections(some other id) < 1)`, i.e. "hidden UNLESS
                                       // you have a Crusade Force OR this Detachment selected" - the
                                       // original plain-existence check incorrectly treated the mere
                                       // presence of the Crusade Force reference as sufficient,
                                       // excluding a real matched-play Enhancement for exactly the
                                       // players using that Detachment) by a condition referencing
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
                                       // only this one narrow, verified shape is ever read, now with
                                       // sound (not just existence-based) boolean evaluation over it.
                                       //
                                       // Generalized by gate-and-dedupe-core-rule-abilities to also
                                       // recognize `scope: "primary-catalogue"` (chapter/sub-
                                       // faction exclusivity) - a real, different shape from the
                                       // force/roster gate above, needing its own comparison-type-
                                       // aware evaluation rather than the force-gate's type-agnostic
                                       // "is this id one of the known gates" check: a primary-
                                       // catalogue condition's truth depends on whether it's
                                       // `instanceOf`/`notInstanceOf` AND whether its `childId`
                                       // matches the closure's own starting catalogue id (no name
                                       // lookup needed - `childId` IS already the target catalogue's
                                       // own literal `id`, confirmed by direct comparison against
                                       // Black Templars' own `catalogue.id`). Called from
                                       // ProcessRuleInfoLink on the resolved rule's own `Modifiers`
                                       // (a genuinely different target from every existing call
                                       // site's entry/group modifiers) - `BsRule.Modifiers` was
                                       // added for this (previously silently dropped).
                                       // `BuildDatasheet` gains a `primaryCatalogueId` parameter,
                                       // same fail-open-default convention as `forceEntries`;
                                       // `ResolvedBsdataCatalogue.ResolveDatasheet` passes
                                       // `Closure.Files[0].Catalogue.Id`. A real bug found and
                                       // fixed while wiring this up: the method's own early-return
                                       // guard (`if GameModeGateIds.Count == 0 return false`)
                                       // pre-dated this second gate concept and short-circuited
                                       // before ever reaching a primary-catalogue check whenever no
                                       // `forceEntries` were supplied - fixed to also check whether
                                       // `PrimaryCatalogueId` is set.
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
one. Rendering (`_UnitBlock.cshtml`'s `WeaponAbilityTags`) still reconstructs its literal ALL-CAPS
tag strings from the typed boolean/int flags today, not from `KeywordsText` — switching to read
`KeywordsText` instead remains a separate, still-deferred future step, unconnected to
`rules-glossary-popovers`'s own use of those same tag strings (each one rendered as its own
bordered chip and looked up against a `RuleGlossary` to decide whether it becomes a popover trigger
— see "Weapon-Keyword Chip Rendering" in that change's `tasks.md`, and `RuleGlossary`'s own
normalized-key resolution below, for why a literal ALL-CAPS tag string reliably matches even though
it's a reconstruction rather than the verbatim source text `KeywordsText` would carry).

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

## Rules Glossary & Popovers

Full requirements: `openspec/changes/rules-glossary-popovers/` (proposal, design, specs, tasks).
Resolves BSData's `sharedRules` (game-system level) and `rules` (faction/library level) arrays —
previously read by no code path at all — into a name/alias-queryable glossary, and mechanically
extracts `[BRACKET]`-style cross-reference tokens from ability/rule text for lookup against it.
Text lookup and display only, never behavioral interpretation, consistent with this file's own
"ability text is never auto-parsed into behavior" stance (Deliberate Omissions, below). Wired into
`/LivePlay`: every weapon-keyword chip and ability name is a tap trigger opening a native-Popover-
API panel showing that keyword's/ability's full rule text, with a resolved `[BRACKET]` reference
inside that text itself becoming a further, nested trigger.

```
RuleDefinition(Name, Aliases, Text)   // Domain/Catalogue/Bsdata/RuleDefinition.cs - one BSData
                                       // sharedRules/rules entry, carried through as opaque
                                       // display text.

RuleGlossary                          // Domain/Catalogue/Bsdata/RuleGlossary.cs
  Build(BsdataClosure closure) -> RuleGlossary
                                       // Iterates closure.Files in existing resolution order
                                       // (local beats imported) collecting each file's own Rules,
                                       // then closure.GameSystem?.SharedRules for the universal
                                       // set. Indexes every RuleDefinition by a single Normalize()d
                                       // key built from *both* its Name and every Alias, not Alias
                                       // alone - first occurrence per key wins. Indexing by Name too
                                       // is what makes a rule with no declared Alias at all
                                       // (confirmed real BSData data gaps: "Cleave", "Close-
                                       // quarters" - each self-references itself in its own
                                       // description text via the same bracket convention every
                                       // other generic mechanic uses) still resolve correctly,
                                       // rather than requiring a formally-declared alias BSData
                                       // simply never populated.
  TryResolve(string nameOrAlias) -> RuleDefinition?
                                       // Non-throwing (Datasheet.TryGetStatline's convention).
                                       // Normalizes nameOrAlias the same way before comparing.

  Normalize(string text) -> string    // private; the single ordered pipeline every Name/Alias and
                                       // every query string goes through - no separate case-
                                       // sensitive "exact" path. 1. Lowercase the whole string.
                                       // 2. Strip a trailing value/threshold/dice/placeholder
                                       // suffix ("x", a digit run, a digit run + "+", or "d" +
                                       // digits with an optional "+"-suffix e.g. "d3") preceded by
                                       // whitespace - must run first, while that separator is
                                       // still present. 3. Collapse a string starting with "anti"
                                       // followed immediately by a space/hyphen/colon boundary to
                                       // bare "anti", discarding everything after it - must run
                                       // before step 4 destroys that same boundary character (the
                                       // boundary check matters: without it, an unrelated word
                                       // merely starting with "anti", e.g. "Antimatter", would
                                       // wrongly collapse too - caught by a real unit test, not
                                       // just theoretical). 4. Strip everything that isn't a
                                       // letter or digit - also what makes a casing/hyphenation
                                       // variant like "Twin-Linked" collapse to the same key as
                                       // "TWIN-LINKED" with no separate casing rule needed. Exists
                                       // because a generic mechanic's bare Name/Alias (e.g.
                                       // "Sustained Hits"/"SUSTAINED HITS") is what BSData
                                       // declares, but real ability/weapon text almost always
                                       // references it with a value or (Anti only) target category
                                       // appended (e.g. "SUSTAINED HITS 1", "ANTI-VEHICLE 3+") -
                                       // confirmed by each mechanic's own rule text, which
                                       // documents its own referenced form explicitly
                                       // ("**[SUSTAINED HITS X]**", "**[ANTI-X Y+]**"). A blanket
                                       // "cut at the first hyphen" rule was rejected as unsafe -
                                       // "Twin-linked"'s hyphen is part of the rule's own name, not
                                       // a category separator - so Anti gets this one narrow,
                                       // explicit exception instead of a general one. Re-running
                                       // the full-corpus scan (below) after this pipeline landed
                                       // confirmed unresolved bracket-token occurrences dropped
                                       // from 1,964 to exactly 2, both genuine one-off BSData
                                       // authoring anomalies with no general fix.

RuleTextTokenizer                     // Domain/Catalogue/Bsdata/RuleTextTokenizer.cs
  ExtractBracketTokens(text) -> IReadOnlyList<string>
                                       // Simple non-nested "[...]" extraction via regex, with no
                                       // awareness of "*"/"**"/"^^" emphasis markup as a *signal*
                                       // of whether something is a candidate reference at all -
                                       // that's what makes "Non-Bracket Markup Is Not A Reference
                                       // Signal" hold structurally. It does normalize each already-
                                       // captured token's own text before returning it (see
                                       // Normalize below) - encoding-level cleanup, never
                                       // interpreting or discarding semantic content.
  Normalize(string token) -> string   // public - reused by RuleTextEmphasisRenderer's own bracket
                                       // handling in _UnitBlock.cshtml, not just by
                                       // ExtractBracketTokens internally. Strips "*"/"^" characters
                                       // found *inside* the brackets (confirmed real shape:
                                       // "[^^Lethal Hits^^]", a rule referenced by its display Name
                                       // wrapped in small-caps markup inside the brackets
                                       // themselves - the reverse of markup wrapping a bracket from
                                       // the outside) and replaces known Unicode whitespace/hyphen
                                       // variants (U+00A0 no-break space, U+2011 non-breaking
                                       // hyphen, U+2013 en dash) with their plain-ASCII equivalent
                                       // (confirmed real shapes in the corpus). This is the "light"
                                       // normalization step, distinct from RuleGlossary's own
                                       // "aggressive" Normalize (above) - the two compose: a
                                       // bracket's raw text goes through this one first, then
                                       // whatever's left goes through RuleGlossary's when resolving
                                       // it.

RuleTextEmphasisRenderer              // Domain/Catalogue/Bsdata/RuleTextEmphasisRenderer.cs
  Render(text, renderBracket: Func<string, (string Inline, string Popovers)>)
    -> (string Inline, string Popovers)
                                       // Renders "*italic*"/"**bold**"/"^^small-caps^^" markup
                                       // (including the nested "***bold+italic***" split-close
                                       // case, confirmed real in "Lethal Hits"' own Designer's
                                       // Note) as HTML styling only - never an interactive trigger
                                       // by virtue of markup alone. A single left-to-right token
                                       // scan (longest-first regex alternation over "***"/"**"/
                                       // "*"/"^^"/"[BRACKET]"), tracking three independent
                                       // open/closed booleans (bold/italic/small-caps - "***"
                                       // toggles both bold and italic together) rather than a
                                       // stack, since bold and italic need to close independently
                                       // later, not as a matched pair. Every [BRACKET] token
                                       // encountered is handed to the caller-supplied
                                       // renderBracket delegate, never resolved or interpreted
                                       // here - keeps this class free of any RuleGlossary/popover-
                                       // id concern and independently unit-testable with a trivial
                                       // pass-through delegate. Returns inline HTML separately from
                                       // any popover panel markup the delegate produced, so a
                                       // caller can splice the trigger in place while emitting
                                       // every popover panel as a flow-content sibling elsewhere -
                                       // never nested inside this text's own emphasis wrapping
                                       // (which would leak that styling into a nested popover's own
                                       // inherited CSS, since top-layer promotion doesn't detach an
                                       // element from the DOM tree for inheritance purposes).

ResolvedBsdataCatalogue.Glossary: RuleGlossary
                                       // Built alongside the rest of ResolvedBsdataCatalogue.Build
                                       // - no new cache, rides the existing BsdataCatalogueCache
                                       // (keyed by starting file name) for free.
```

**`/LivePlay` wiring (`ProbHammer.Web`)**: `IArmyRosterProvider.Build` returns
`ArmyRosterBuildResult(ArmyRoster Roster, RuleGlossary Glossary)` (was bare `ArmyRoster`) so both
`LivePlayModel.OnGet` and `LivePlayCasualtyService`'s direct `_UnitBlock.cshtml` fragment
re-render have the glossary available; `UnitBlockRenderModel` carries it as a third field. Since
`display-army-header-and-detachment-rules`, the trigger/panel-building logic previously local to
`_UnitBlock.cshtml` (`BuildRulePopover`/`NextPopoverId`/`RenderNestedReference`) is extracted into
`ProbHammer.Web.Rendering.RulePopoverRenderer` - a small stateful class, one instance per render
pass, constructed with the `RuleGlossary` plus an explicit id-scope prefix (`"u{i}"` for a unit
block, `"hdr"` for the header) instead of implicitly relying on the unit's page index - so the new
`_ArmyHeader.cshtml` partial (below) can produce the identical trigger/panel markup with no unit
index to scope against. `_UnitBlock.cshtml`'s own local `BuildRulePopover` is now a one-line
forwarder onto its own `RulePopoverRenderer` instance, kept only so its many call sites don't all
need touching. `_UnitBlock.cshtml`'s glue code (via `RulePopoverRenderer`) is the only code that
touches `RuleGlossary` directly: every weapon-keyword chip (`WeaponAbilityTags()`'s
existing literal ALL-CAPS tag strings, unchanged - see "BSData JSON Ingestion" above) and every
ability name is looked up via `TryResolve`; a match becomes a `<button popovertarget="p-{id}">`
trigger with a sibling `<div id="p-{id}" popover="auto">` panel (never nested inside the trigger -
a `<button>` cannot validly contain another interactive/flow-content element), positioned via a
per-instance `anchor-name`/`position-anchor` CSS custom-property pair rather than the newer HTML
`anchor="..."` attribute (real device testing found the latter unsupported where the former's
properties at least register as supported - see design.md's "Popover mechanism" decision and its
Risks section for what "supports" turned out to mean in practice). `RenderNestedReference`
recurses into a resolved reference's own `RuleDefinition.Text` to make nesting work to arbitrary
depth (confirmed live to 3 levels), threading a `shownRuleNames` ancestor-chain set so a bracket
resolving back to a rule already open somewhere in its own chain renders as plain text instead of
recursing - several real generic-mechanic rules self-reference their own name in their own text
(confirmed: `Sustained Hits`, `Anti`, `Cleave`), and an unguarded first version of this
stack-overflowed rendering a real weapon's chip.

**Army header (`display-army-header-and-detachment-rules`)**: `LivePlay.cshtml` renders a new
`_ArmyHeader.cshtml` partial above the unit-block loop, fed `ArmyHeaderRenderModel(ArmyHeaderViewModel
Header, RuleGlossary Glossary)` - mirrors `UnitBlockRenderModel`'s own "view model + glossary"
pairing. `LivePlayModel.BuildArmyHeader(roster)` builds the view model directly from the roster's
own metadata (Name/Faction/BattleSize/PointsSpent/PointsLimit/ForceDisposition) plus two
aggregations: the roster's own resolved `Detachments` (unchanged, just carried through), and
`BuildArmyRules(roster)` - every distinctly-named `ArmyRule`-origin ability present on ANY unit in
the roster, deduped by Name, computed by walking every component's own `Datasheet.Abilities`
directly (NOT through `AttachedUnitAggregator`'s present-only view) - deliberately independent of
casualty/`RemainingCount` state, since an `ArmyRule` ability is a fact about the army's chapter
identity, not which models are currently alive, so (unlike the existing per-unit box) this header
entry never disappears over the course of a game. A NEW, separate aggregation from
`AttachedUnitAggregator.PromoteArmyRuleAbilities`'s existing per-unit dedup, which stays exactly
as-is - the existing per-unit box keeps rendering unchanged alongside the new header. The header's
own rules section lays out two columns (Army/Detachment) via the same `<thead><th>` convention the
weapon tables already use; a Detachment renders its own name once as a plain (non-interactive)
label with one `RulePopoverRenderer`-built trigger per resolved rule beneath it. No DP cost, no
unit points cost, anywhere on the page.

### Full-Corpus Bracket-Token-Resolution Scan

`tests/ProbHammer.Tests/Domain/Catalogue/Bsdata/CorpusScan/BracketTokenResolutionScanTests.cs` -
same permanent, manually-triggered `[Fact(Explicit = true)]` pattern as `WeaponKeywordScanTests`/
`CharacteristicResolutionScanTests` (see "Full-Corpus Scan Tests" above). Walks every closure's own
local `rules` text, the game system's `sharedRules` text (once, not once per starting file), and
every locally-defined entry's resolved `Ability.Text`, extracting bracket tokens and resolving each
against that closure's own glossary. The first real run (before `RuleGlossary`'s normalized-key
resolution existed) found 1,964 unresolved occurrences across 71 distinct tokens; after that
resolution landed, exactly 2 remain, both individually allowlisted in
`BracketTokenResolutionAllowlist.cs` as confirmed one-off BSData authoring anomalies (a
bracket-placement typo in one Adeptus Custodes ability; a stray non-reference bracket in one Blood
Angels ability) - neither fixed, since no general rule could resolve either without risking a wrong
fix elsewhere.

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
ArmyListParser                        // the only implementation, covering both iOS-captured
                                       // exports (data/gw-app-export*.txt) and Android-captured
                                       // ones (data/gw-android-export*.txt - see
                                       // harden-army-list-parsing-for-android-exports's design.md
                                       // for the format differences this section describes).
                                       // Line-based: blank lines discarded, every remaining line
                                       // classified either positionally (the fixed army-metadata
                                       // preamble: Name/Points header, 1-2 Faction lines,
                                       // one-or-more "(N Detachment Points)" lines, an OPTIONAL
                                       // ForceDisposition, BattleSize/PointsLimit header - see
                                       // below) or by a small set of regexes (ATTACHED UNITS / a
                                       // top-level section header / "Attached unit N" / a
                                       // "<Name> (<N> Points)" unit header / a bullet line). The
                                       // Points-cost suffix (NamePointsRegex) and the
                                       // ATTACHED UNITS/Attached Unit N markers are matched
                                       // case-insensitively (a real Android export renders these
                                       // "Attached Units"/"Attached Unit 1", title-case); the four
                                       // top-level section headers (CHARACTERS/BATTLELINE/DEDICATED
                                       // TRANSPORTS/OTHER DATASHEETS) and Detachment Points are NOT
                                       // - both confirmed Android samples already render those
                                       // identically to iOS, so widening their match would be an
                                       // unverified guess. ForceDisposition, being free text with
                                       // no fixed shape of its own, is optional: a real Android
                                       // export (gw-android-export-deathguard.txt) omits it
                                       // entirely, going straight from the Detachment line to the
                                       // BattleSize line - detected by peeking whether the next
                                       // line already matches the BattleSize "<Name> (<N> Points)"
                                       // pattern, in which case ForceDisposition is "" rather than
                                       // consuming that line as disposition text.
                                       // Bullet nesting is read from the bullet CHARACTER itself
                                       // ("•" top-level, "◦" nested) on iOS - but Android drops
                                       // "◦" entirely: a nested list's own first item re-adopts
                                       // "•" instead, and every later item in that same list (at
                                       // ANY depth) carries no bullet at all, relying purely on
                                       // indentation. Two signals resolve this without a
                                       // depth-specific special case (see ArmyListParser
                                       // .CollectBulletBlocks' own doc comment for the full
                                       // reasoning, including why bullet-glyph/indentation alone
                                       // can't distinguish a "•" role line like "Attached as:
                                       // Leader" from a genuine model-group header, both followed
                                       // by a same-relative-indent sibling bullet): (1) a "•" line
                                       // nests under the current top-level block only when that
                                       // block's own text looks like an "Nx ModelName" header, it
                                       // has no nested item yet, and this line is indented deeper
                                       // than that block's own bullet; (2) an unbulleted line
                                       // extends whichever list is currently open only when its own
                                       // indentation is at least that list's first item's own text
                                       // column - which is what stops a genuine next unit/section
                                       // header (always column 0) from being swallowed as a stray
                                       // continuation. Each tokenized line therefore carries its own
                                       // leading-indent count alongside its trimmed content
                                       // (ArmyListParser.Line), not just the trimmed text every
                                       // other part of the parser reads.
                                       // A unit's top-level bullets classify as: "Attached as: Role
                                       // [(Category)]" (first bullet only, attached mode; Category
                                       // discarded); "Enhancement(s): ..." (-> Enhancements, whole
                                       // remainder as one entry, never split on commas - same
                                       // "commas can be part of a real name" precedent as
                                       // Detachment); "Nx ModelName" WITH nested weapon lines
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
                                       // guessing a split. A confirmed real case of this (Adeptus
                                       // Custodes' Custodian Guard: "1x Guardian spear" / "3x
                                       // Praesidium Shield" / "3x Sentinel blade" against a total of
                                       // 4 - the only sensible reading pairs Shield+blade as one
                                       // 3-model sub-group, which the partition rule can't infer from
                                       // counts alone without risking a wrong merge on a case like
                                       // Company Veteran's above) is a deliberately deferred, still-
                                       // open gap, not a regression - see
                                       // harden-army-list-parsing-for-android-exports's design.md.

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
                                       // Datasheet.TryResolveAbility match by name (some "weapon"
                                       // lines aren't weapons at all - confirmed: Impulsor's
                                       // "Shield Dome", a BSData Abilities-typeName profile
                                       // granting a 5+ invulnerable save - attached to that
                                       // ModelLine's own Abilities instead of thrown as an
                                       // unresolved weapon). Since resolve-enhancement-abilities,
                                       // this reads the on-demand index (was: a linear scan of the
                                       // public Datasheet.Abilities, before that list narrowed to
                                       // intrinsic-only) - Shield Dome resolves identically either
                                       // way, only where it's looked up changed.
                                       // Enhancement resolution (per name in ParsedUnit
                                       // .Enhancements, resolve-enhancement-abilities): each name
                                       // is resolved against the built Unit's Datasheet
                                       // .TryResolveAbility (same on-demand index the wargear
                                       // fallback above reads), same "resolve by name, fail loud
                                       // with a did-you-mean suggestion on a miss" contract as
                                       // every other resolution in this file. Deliberately does
                                       // NOT check that the resolved ability's Origin is actually
                                       // Enhancement (vs. some other OptionalGrant) - that would be
                                       // new eligibility-checking behavior this project has
                                       // consistently declined to add elsewhere. An empty
                                       // Enhancements list resolves to an empty result - no
                                       // Enhancement is ever attached merely because the Datasheet
                                       // defines one available (this is the actual fix for the bug
                                       // that motivated resolve-enhancement-abilities).

BsdataFactionResolutionException(faction, message)   // carries the offending Faction entry
BsdataNameResolutionException(text, message)         // carries the offending text
```

`Datasheet` (Catalogue Context above) gained `TryGetStatline`/`TryResolveWeaponProfile` (non-
throwing variants) and `WeaponNames` (`IReadOnlyList<string>`) specifically to support the above -
diagnostic/fallback lookups that need to check existence or enumerate candidates without either
throwing on a routine miss or breaking `ResolveWeaponProfile`'s existing on-demand-only contract.
Since resolve-enhancement-abilities, it likewise gained `TryResolveAbility`/`OptionalAbilityNames`
for the same reason, on the optional-ability side.

### Detachment Resolution

Full requirements: `openspec/changes/display-army-header-and-detachment-rules/` (proposal, design,
specs, tasks). Resolves each entry in `ParsedArmyList.Detachments` (a raw captured text that MAY
itself name more than one Detachment in natural-language list form, e.g. "Fulguris Task Force,
Marshal's Household, and Subversion Assets") against the faction's `ResolvedBsdataCatalogue` into
`ArmyRoster.Detachments`' new `ResolvedDetachment` shape (see "Roster Context" below).

```
BsdataNameResolver.ResolveDetachmentEntries(closure) -> IReadOnlyList<BsSelectionEntry>
                                       // Locates the closure's own Detachment-choice group and
                                       // returns its direct children - each one a real Detachment
                                       // choice. Checked per file in closure order (local-over-
                                       // imported precedence, mirroring Resolve's own "first match
                                       // wins" convention). Confirmed by a full-corpus scan
                                       // (DetachmentGroupNameScanTests, below) to need TWO real,
                                       // structurally different shapes, not the one shape design.md
                                       // originally assumed and flagged as merely "unverified":
                                       //   1. A top-level sharedSelectionEntryGroup named
                                       //      "Detachment" or "Detachments" (both real - singular on
                                       //      Space Marines, plural on the Aeldari Library) directly.
                                       //   2. A top-level sharedSelectionEntries ENTRY of the same
                                       //      name (confirmed: Chaos Space Marines, Necrons, Death
                                       //      Guard, Emperor's Children, Thousand Sons, World
                                       //      Eaters, Leagues of Votann, Chaos Daemons) wrapping ONE
                                       //      nested selectionEntryGroup that itself carries the
                                       //      real choices directly. A handful of factions
                                       //      (confirmed: Tyranids, Genestealer Cults) instead wrap
                                       //      an empty-named, empty-entries group reaching the real
                                       //      choices only via its own entryLink into a "Library"
                                       //      file - unreachable today because that catalogueLink's
                                       //      own importRootEntries is false in the source data (a
                                       //      genuine, confirmed, allowlisted corpus gap in
                                       //      BsdataClosureResolver's own link-following, distinct
                                       //      from and out of scope for this method - see
                                       //      DetachmentGroupNameAllowlist.cs for the full list,
                                       //      including Drukhari's own identical-cause gap on its
                                       //      Aeldari Library import). Falling through to a wrapper
                                       //      entry's sole nested group regardless of ITS OWN name
                                       //      is deliberately not attempted as a further fallback -
                                       //      an empty-named, zero-entry group is indistinguishable
                                       //      from "nothing reachable", so the honest answer is "no
                                       //      known Detachments" (a loud downstream resolution
                                       //      failure), not a guessed shape.

DetachmentGroupNameScanTests           // tests/.../CorpusScan/ - same permanent, manually-
                                       // triggered [Fact(Explicit = true)] pattern as every other
                                       // corpus scan (see "Full-Corpus Scan Tests" above). Every
                                       // real faction file (excluding "Library" files, matching
                                       // BsdataFactionResolver's own exclusion) gets a turn as its
                                       // own closure's starting file; its closure must resolve at
                                       // least one Detachment entry. First real run: 15 failures
                                       // against the original single-shape assumption; generalizing
                                       // ResolveDetachmentEntries to the second shape fixed 11 of
                                       // them; the remaining 4 are the confirmed importRootEntries
                                       // gap above, allowlisted.

ResolvedBsdataCatalogue.DetachmentEntries / .DetachmentNames / .ResolveDetachment(name)
                                       // DetachmentEntries/DetachmentNames built once in Build()
                                       // (same convention as IdIndex/GroupIdIndex/ProfileIdIndex -
                                       // "built lazily" means once per closure, not per request).
                                       // ResolveDetachment(name) is an exact-name lookup with the
                                       // same "did you mean...?" contract as ResolveDatasheet.

DetachmentRuleTextExtractor.Extract(detachmentEntry, glossary) -> IReadOnlyList<(Name, Text)>
                                       // Domain.Catalogue.Bsdata (catalogue-json-ingestion) -
                                       // extracts zero or more rule pairs from an already-resolved
                                       // Detachment entry: a locally-declared rule
                                       // (BsSelectionEntry.Rules, new field mirroring
                                       // BsCatalogue.Rules' own shape) and a "type": "rule" infoLink
                                       // resolved via RuleGlossary.TryResolve - the same lookup
                                       // Core Rule Ability Extraction already uses, but WITHOUT that
                                       // extraction's "nested inside a type: upgrade entry" ancestry
                                       // guard, since a Detachment entry's own direct infoLinks
                                       // carry no equivalent weapon-keyword-cross-reference
                                       // ambiguity. An unresolvable infoLink reference is skipped,
                                       // not failed.

RuleGlossary.Build's own generalization (this change)  // A catalogue file's own local sharedRules
                                       // field was originally assumed (BsCatalogue.SharedRules' own
                                       // doc comment) to appear only on the game-system file - real
                                       // data contradicts that: Necrons.json defines its own local
                                       // sharedRules array (not rules), which is where "Command
                                       // Protocols" (Awakened Dynasty's own infoLink target) is
                                       // actually declared. Found only by manually verifying this
                                       // change's own infoLink-shape test case (task 5.2) against
                                       // real bundled data - RuleGlossary.Build now reads every
                                       // closure file's own SharedRules too, not only the game
                                       // system's, purely additive via the same TryAdd
                                       // first-occurrence-wins convention.

DetachmentNameResolver.Resolve(text, catalogue) -> IReadOnlyList<ResolvedDetachment>
                                       // Domain.Roster (army-roster-enrichment) - the greedy,
                                       // longest-known-name-first "chomp" that disambiguates a
                                       // natural-language-joined Detachments entry without a
                                       // syntactic split on "and"/commas: repeatedly finds the
                                       // longest of catalogue.DetachmentNames still present in the
                                       // (progressively space-masked, position-stable) working text,
                                       // "claims" it, and loops. Never splices text out (masks
                                       // matched spans with spaces instead) so a claim's own
                                       // position in the ORIGINAL text stays stable, which is what
                                       // lets the final result be ordered by claim position rather
                                       // than match-discovery order - "in the order listed" per the
                                       // spec, not "in decreasing-length order". Two confirmed real
                                       // collision shapes this resolves correctly: a Detachment name
                                       // that itself contains "and" ("Legends of Saga and Song" -
                                       // never split, since it's matched as one whole name before
                                       // any shorter name gets a chance), and one Detachment name
                                       // that's a literal substring of another
                                       // ("Warhost"/"Armoured Warhost" - longest-first ordering means
                                       // "Armoured Warhost" is already masked out before "Warhost" is
                                       // even checked). Fails with a BsdataNameResolutionException
                                       // naming the unresolved remainder (stripped of separator
                                       // punctuation/whitespace/"and") when any non-separator text is
                                       // left unclaimed once no further known name matches.

ArmyRosterEnricher.Enrich             // now also resolves each entry in parsedArmyList.Detachments
                                       // via DetachmentNameResolver.Resolve, flattened (SelectMany)
                                       // into ArmyRoster's own Detachments list, since one parsed
                                       // entry may itself claim more than one ResolvedDetachment.

BattleScribeRosterMapper.MapDetachment(selection) -> ResolvedDetachment
                                       // BattleScribe pipeline - no BSData involvement, per that
                                       // pipeline's existing design: a selected Detachment's own
                                       // rule text is already inline on its own selections[].rules[]
                                       // (confirmed real: Death Guard's "Virulent Vectorium" carries
                                       // "Worldblight"; Black Templars' "Companions of Vehemence"
                                       // carries "Righteous Fervour" the same way).
                                       //
                                       // FindGroup(force.Selections, "Detachment", "Detachments")
                                       // matches the top-level Detachment-choice selection by EITHER
                                       // name (params string[], first match wins) - a real, confirmed
                                       // user-reported bug found this format has the identical
                                       // singular-vs-plural naming inconsistency the BSData pipeline's
                                       // own DetachmentGroupNameScanTests already documents for
                                       // catalogue group names, just never previously observed on
                                       // this pipeline: Adeptus Custodes' own NewRecruit export names
                                       // its top-level selection "Detachments" (plural; Death Guard/
                                       // Black Templars use singular "Detachment") - the original
                                       // exact-match-only FindGroup silently resolved zero
                                       // Detachments for that one shape, so "Auric Champions"/its
                                       // "Assemblage of Might" rule never appeared on /LivePlay despite
                                       // being present in the roster JSON, with no exception or other
                                       // symptom to flag the miss. Regression fixture:
                                       // tests/.../BattleScribe/Fixtures/custodes-detachments-plural-
                                       // excerpt.json. Same fix pattern applies to Force
                                       // Disposition/Battle Size only if a real export is ever found
                                       // using an alternate name for those - not attempted speculatively
                                       // here since none has been observed.
```

### Session-Backed Import (`ProbHammer.Web`)

Since `import-battlescribe-json-rosters`, `ISessionArmyListStore`/`IArmyRosterProvider` are no
longer hard-typed to `ParsedArmyList` - they accept/return `StoredArmyImport`
(`Domain.Import.StoredArmyImport`), a small `[JsonPolymorphic]` wrapper with two variants,
`TextArmyImport(ParsedArmyList)` (the GW-app text pipeline, unchanged) and
`BattleScribeArmyImport(BsRoster)` (see "BattleScribe/NewRecruit JSON Import Pipeline" below) - so
`/LivePlay`/`LivePlayCasualtyService` stay format-agnostic, never branching on which pipeline
produced the session's stored import.

```
ISessionArmyListStore.Save(ISession, StoredArmyImport)
                     .Load(ISession) -> StoredArmyImport?
                                       // plain System.Text.Json round-trip through ISession's string
                                       // storage, using StoredArmyImport's own [JsonDerivedType]
                                       // attributes for polymorphic (de)serialization - every type
                                       // reachable from either variant is a plain record/class of
                                       // primitives/lists, no Datasheet/abstract WeaponProfile graph
                                       // to serialize. The session stores only this source-level
                                       // intermediate, never the built ArmyRoster (see "Session
                                       // stores the intermediate, not the graph" in design.md) - every
                                       // request re-builds fresh via IArmyRosterProvider.

IArmyRosterProvider.Build(StoredArmyImport) -> ArmyRosterBuildResult
                                       // dispatches on which variant: TextArmyImport runs the
                                       // existing three-step orchestration (BsdataFactionResolver ->
                                       // BsdataCatalogueCache.GetOrBuild -> ArmyRosterEnricher.Enrich);
                                       // BattleScribeArmyImport runs BattleScribeRosterMapper.Map
                                       // directly (no BSData catalogue involvement) plus
                                       // BattleScribeRuleGlossaryBuilder.Build for its own
                                       // roster-scoped RuleGlossary. Used by both the import page (to
                                       // validate before writing to session) and /LivePlay's
                                       // per-request rebuild - one place for a sequence needed at
                                       // three call sites (import, LivePlay GET, the casualty sync
                                       // endpoint), regardless of format.

ImportModel (/Import Razor Page)      // paste box + submit. OnPost first attempts
                                       // BattleScribeRosterFormat.TryParse (format recognition - see
                                       // below); on a match it wraps the result as a
                                       // BattleScribeArmyImport, otherwise it falls through to the
                                       // existing ArmyListParser and wraps as a TextArmyImport. Either
                                       // way it then calls IArmyRosterProvider.Build to validate
                                       // BEFORE calling ISessionArmyListStore.Save - so a failed
                                       // re-import never touches a previously-successful session
                                       // import. Catches ArmyListParseException /
                                       // BsdataFactionResolutionException /
                                       // BsdataNameResolutionException / AmbiguousCharacteristicException
                                       // / BattleScribeRosterParseException and reports the message on
                                       // the page rather than crashing; any other exception type is an
                                       // unhandled bug, not a reportable import failure.

LivePlayModel.OnGet()                 // loads the session's StoredArmyImport; redirects to /Import if
                                       // absent (live-play-view's "Live Play Redirects Without An
                                       // Active Import"), otherwise builds via IArmyRosterProvider and
                                       // renders exactly as before, unaware of which pipeline produced
                                       // it. RebuildRoster (casualty rebuild) takes the roster's Units
                                       // as an explicit parameter instead of reading
                                       // Examples.View.MyArmyRoster() internally.
LivePlayCasualtyService.SyncAsync()   // loads the session's StoredArmyImport itself (via
                                       // ISessionArmyListStore) before calling RebuildRoster; an
                                       // empty batch OR no active session import both short-circuit
                                       // to an empty response.
```

`Program.cs` reintroduces `AddDistributedMemoryCache`/`AddSession`/`UseSession` (removed by
`archive-10e-pipeline` as a 10e-only dependency; brought back here for a new purpose - per-session
`StoredArmyImport` storage, not the old `Enricher` cache) and registers `BsdataCatalogueCache` as a
singleton over a `LocalDiskBsdataCatalogueSource` rooted at `Bsdata:RootDirectory` (appsettings.json,
default `"BsData"`, resolved against `IWebHostEnvironment.ContentRootPath`) - the bundled snapshot
at `src/ProbHammer.Web/BsData/`, copied into the Docker image as its own layer (see
`catalogue-json-ingestion`'s packaging notes), not a live GitHub fetch. `BsdataCatalogueCache` is
untouched by `import-battlescribe-json-rosters` - the BattleScribe JSON pipeline never reads it.

---

## BattleScribe/NewRecruit JSON Import Pipeline

Full requirements: `openspec/changes/import-battlescribe-json-rosters/` (proposal, design, specs,
tasks). A second, independent import pipeline, parallel to "Army List Import Pipeline" above:
`BattleScribe roster JSON text → BattleScribeRosterFormat.TryParse → BsRoster (session-stored, as
a BattleScribeArmyImport) → BattleScribeRosterMapper.Map → ArmyRoster`, with no
`Domain.Catalogue.Bsdata`/BSData catalogue involvement anywhere in this path - every
`Datasheet`/`Statline`/`WeaponProfile`/`Ability` needed is synthesized directly from the roster
JSON's own already-resolved `profiles`/`rules`, since (unlike a GW-app text export) the JSON
BattleScribe/NewRecruit produces already carries every characteristic, weapon profile, and
ability/rule text fully resolved inline. Serves NewRecruit-only users (whose per-army-book content
in the official GW app is paywalled) directly, and gives Android GW-app users a reliable
alternative path to `/LivePlay` when the native text export hits one of that pipeline's own
still-open ambiguities (see `harden-army-list-parsing-for-android-exports`): export from GW app,
clean up in NewRecruit, export as JSON. Analyzed against exactly one real captured sample so far
(`data/gw-app-export-templars.json`, the existing Templars fixture round-tripped through
NewRecruit) - see this section's own callouts for what remains unverified pending a second,
independently-sourced sample.

Namespace: `ProbHammer.Core.Domain.Import.BattleScribe`, JSON DTOs in its own `Json` sub-namespace
(mirrors `Domain.Catalogue.Bsdata`/`Domain.Catalogue.Bsdata.Json`'s own split).

```
Json/BsRosterFile.cs                  // System.Text.Json-backed record/class types for the subset
                                       // of the BattleScribe rosterSchema this pipeline reads -
                                       // BsRoster (Xmlns, Name, Costs, CostLimits, Forces),
                                       // BsRosterForce (CatalogueName, Rules, Selections),
                                       // BsRosterSelection (Id, Name, Type, Number, Profiles, Rules,
                                       // Costs, Selections, Associations - the same shape reused at
                                       // every nesting level, mirroring BsSelectionEntry's own
                                       // precedent), BsRosterProfile (+ CharacteristicText(name),
                                       // mirroring BsProfile's own helper), BsRosterCharacteristic
                                       // ($text via [JsonPropertyName]), BsRosterCost (Name, Value -
                                       // shared shape for the roster's own totals and a single
                                       // selection's own cost tags, e.g. "Enhancements",
                                       // "Detachment Points"), BsRosterAssociation (Type, To, Name).
                                       // Only what this pipeline's mapping needs is modeled -
                                       // everything else (constraints, points-cost-modifier detail,
                                       // etc.) is silently ignored on deserialize, same convention as
                                       // Json/BsCatalogueFile.cs.

BattleScribeRosterFormat.TryParse(text, out BsRoster?) -> bool
                                       // Format Recognition: true only for text that parses as JSON
                                       // AND contains a top-level "roster" object whose "xmlns"
                                       // equals the standard, cross-tool BattleScribe roster schema
                                       // URL - never throws, so non-JSON text or JSON of some other
                                       // shape simply isn't recognized (falls through to the GW-app
                                       // text pipeline at the /Import call site) rather than failing
                                       // loudly. Uses a CamelCase/case-insensitive
                                       // JsonSerializerOptions, matching BsdataCatalogueReader's own
                                       // convention, since the source JSON's own field names are
                                       // camelCase.

BattleScribeRosterParseException(message)
                                       // Thrown by the mapper when a recognized payload doesn't
                                       // resolve into an ArmyRoster (e.g. a selection with no
                                       // resolvable Unit-typeName profile anywhere reachable) -
                                       // mirrors ArmyListParseException's role, caught by
                                       // ImportModel.OnPost and reported on the page.

BattleScribeRosterMapper.Map(BsRoster) -> ArmyRoster
                                       // Army metadata: PointsSpent/PointsLimit from
                                       // roster.costs/costLimits' own "pts" entry; Faction from
                                       // splitting force.catalogueName on " - " (e.g. "Imperium -
                                       // Adeptus Astartes - Black Templars" -> 3 segments) - NOT
                                       // required to match BsdataFactionResolver's own suffix-
                                       // matching convention, since that resolver is never invoked
                                       // by this pipeline (confirmed via a repo-wide search: Faction
                                       // is otherwise unread outside the text pipeline - see
                                       // design.md's Risks); Detachments/ForceDisposition/BattleSize
                                       // from the "Detachment"/"Force Disposition"/"Battle Size"
                                       // top-level selections' own nested selection name(s) -
                                       // BattleSize additionally strips a trailing parenthetical
                                       // (e.g. "Incursion (1000 Point limit)" -> "Incursion",
                                       // matching the text pipeline's own BattleSize convention even
                                       // though this format's own parenthetical wording differs
                                       // ("Point limit" vs. "Points")).
                                       //
                                       // Attachment resolution (BuildUnits): every top-level
                                       // "unit"/"model" selection's own outgoing
                                       // associations[].name in ("Leading","Supporting") targets
                                       // another top-level selection by id - the target becomes a
                                       // Bodyguard, every selection targeting it becomes an Attached
                                       // member, and a selection with neither an outgoing nor an
                                       // incoming association is standalone. Confirmed against all
                                       // three real associations in the sample: two 2-member groups,
                                       // one Leader-only (1-member) group - no 3-member group exists
                                       // in this particular sample, so that shape (mentioned in the
                                       // change's own tasks.md) remains UNVERIFIED pending a second
                                       // sample.
                                       //
                                       // Unit assembly (BuildUnit): a top-level selection's own
                                       // "Abilities"-typeName profiles become Intrinsic Datasheet
                                       // Abilities; its own "rules" entries become CoreRule- or
                                       // ArmyRule-origin Abilities, deliberately with NO additional
                                       // chapter/mode gating of the kind
                                       // gate-and-dedupe-core-rule-abilities added for the BSData
                                       // pipeline - a roster JSON only ever contains rules that
                                       // already apply to the exported army, so that gating need
                                       // simply doesn't arise here. Since classify-known-army-rules,
                                       // Origin is decided by the identical
                                       // `ArmyRuleNameLookup.Resolve(Faction).Contains(rule.Name)`
                                       // check the BSData pipeline uses (Map() resolves it once from
                                       // the roster's own already-computed Faction and threads it into
                                       // BuildUnits/BuildUnit) - ArmyRule on a match, CoreRule
                                       // otherwise. Because Faction here is roster-scoped, not
                                       // per-component, `AttachedUnitAggregator
                                       // .PromoteArmyRuleAbilities`' cross-component dedup (see "Core
                                       // Versus Army Rule Origin Classification" below) now applies to
                                       // this pipeline's output too - e.g. "Templar Vows" collapses to
                                       // one entry across present components here exactly as it does
                                       // for the BSData pipeline, no longer the accepted difference an
                                       // earlier version of this file documented. Enhancement resolution
                                       // (FindEnhancements) walks the WHOLE selection tree (not just
                                       // the model-line weapon subtree) for any descendant selection
                                       // carrying a costs entry named "Enhancements", resolving its
                                       // own "Abilities"-typeName profile into an Enhancement-origin
                                       // Ability on Unit.Enhancements - structurally cannot leak an
                                       // unselected Enhancement, since a roster JSON only contains
                                       // actual selections (see design.md's "Enhancement recognition"
                                       // decision).
                                       //
                                       // ModelLine assembly (BuildModelLines): when the top-level
                                       // selection has one or more immediate "model"-typed nested
                                       // selections, each is its own loadout ModelLine - using its
                                       // own Unit-typeName profile when it has one (Crusader Squad's
                                       // Sword Brother/Initiate/Neophyte, each a genuinely distinct
                                       // per-loadout statline), or falling back to the TOP-LEVEL
                                       // selection's own Unit profile when it has none (Sword
                                       // Brethren Squad's "Sword Brother" wrapper selection carries
                                       // only the shared wargear, not its own statline - the whole
                                       // squad shares "Sword Brethren", declared once on the
                                       // top-level selection itself). When there are NO such nested
                                       // "model" children at all, the top-level selection is itself a
                                       // single/solo model (Helbrecht, Impulsor) and produces exactly
                                       // one ModelLine (Count = its own Number, normally 1). Either
                                       // way, no partition inference is ever needed - each already-
                                       // split loadout node carries its own count directly (see
                                       // battlescribe-roster-import's own "no partition inference"
                                       // scenario) - this is the one class of bug
                                       // (`harden-army-list-parsing-for-android-exports`' still-open
                                       // Custodian Guard case) this pipeline cannot reproduce by
                                       // construction.
                                       //
                                       // Weapon/ability collection (CollectWeaponsAndAbilities):
                                       // walks a loadout node's own nested wargear `selections`
                                       // recursively. A weapon-carrying child (its own "Ranged
                                       // Weapons"/"Melee Weapons"-typeName profile(s) - every
                                       // resolved profile retained, e.g. Helbrecht's "Sword of the
                                       // High Marshals" -> both "- Sweep"/"- Strike" profiles) has
                                       // its own `number` divided by the loadout's model count to
                                       // recover the PER-MODEL weapon quantity (confirmed real shape:
                                       // Impulsor's "2 Storm Bolters" wrapper, itself carrying no
                                       // profiles of its own, nests a "Storm bolter" child with its
                                       // own number=2 against a model count of 1 -> two "Storm
                                       // bolter" Weapons entries, matching ModelLine.Weapons' own
                                       // "duplicates are meaningful" convention). A selection with
                                       // neither weapon nor "Abilities"-typeName profiles of its own
                                       // (a pure grouping wrapper) is walked one level deeper rather
                                       // than skipped - this is what makes the Storm Bolters case
                                       // fall out with no special-cased wrapper handling. An
                                       // "Abilities"-typeName child (e.g. Impulsor's own "Shield
                                       // Dome", granting a 5+ invulnerable save) becomes an
                                       // OptionalGrant-origin Ability on that specific ModelLine -
                                       // mirrors the BSData pipeline's own classification for the
                                       // equivalent shape (a wargear-granted ability nested inside its
                                       // own selection, not a datasheet-wide fact). An
                                       // Enhancement-tagged selection is skipped entirely here (never
                                       // walked into) - it's resolved once, separately, by
                                       // FindEnhancements above, so it can't double-count as a
                                       // ModelLine ability too.
                                       //
                                       // Characteristic-text parsing reuses DiceExpression.Parse (A/D)
                                       // and WeaponKeywordParser.Apply (Keywords) directly - both
                                       // already public, and this format's own text shapes (confirmed
                                       // from the sample) are byte-for-byte the same convention BSData
                                       // uses (e.g. "Anti-infantry 4+, Devastating Wounds"). Plain-int/
                                       // threshold/measurement parsing (S/AP/W/OC/BS/WS/Sv/LD/M/Range)
                                       // is NOT shared with BsdataDatasheetMapper's own private
                                       // ParsePlainInt/ParseThreshold/ParseMeasurement (inaccessible
                                       // outside that class) - this mapper carries its own small,
                                       // functionally-identical local equivalents instead (same "-"/
                                       // "N/A" -> 0 convention, same trailing "+"/`"`-stripping),
                                       // rather than widening that existing file's visibility for a
                                       // marginal reuse gain.
                                       //
                                       // Invulnerable-save caveat resolution (ResolveInvulnerableSave/
                                       // ResolveCaveatAbility) is this format's OWN, simpler
                                       // resolution (per design.md's Decisions) - there is no
                                       // BSData-style entryLink ancestry chain to walk here, so a
                                       // footnoted value's linked ability is searched for directly on
                                       // the owning loadout node, then the top-level selection, under
                                       // the same two BSData naming conventions ("Invulnerable Save
                                       // ({digit}+*)" / "*Invulnerable Save"). UNVERIFIED - the one
                                       // real sample analyzed has no caveated InSv at all.

BattleScribeRuleGlossaryBuilder.Build(BsRoster) -> RuleGlossary
                                       // Builds a roster-scoped RuleGlossary (see design.md's
                                       // "Roster-scoped RuleGlossary, reusing the existing type") by
                                       // walking the WHOLE roster once - the force's own `rules`,
                                       // every top-level selection's own `rules`, and every nested
                                       // wargear selection's own `rules` (e.g. a weapon's own
                                       // "Sustained Hits"/"Anti" keyword rule text) - deduped by id,
                                       // first occurrence wins, into RuleDefinitions via the new
                                       // RuleGlossary.BuildFrom (below). This is what gives
                                       // `/LivePlay`'s existing `[BRACKET]` cross-reference resolution
                                       // and weapon-keyword-chip popovers (rules-glossary-popovers)
                                       // working text for a BattleScribe-sourced roster too, with ZERO
                                       // changes to that existing rendering pipeline - `/LivePlay`
                                       // only ever needs *a* RuleGlossary, not specifically a
                                       // BSData-sourced one.

RuleGlossary.BuildFrom(IEnumerable<RuleDefinition>) -> RuleGlossary
                                       // Added by import-battlescribe-json-rosters alongside the
                                       // existing Build(BsdataClosure) factory - indexes an
                                       // already-known flat set of RuleDefinitions the same way
                                       // (by Name and every Alias, normalized, first occurrence
                                       // wins), for a caller (BattleScribeRuleGlossaryBuilder) whose
                                       // "closure" isn't a BsdataClosure at all.
```

`StoredArmyImport`/`TextArmyImport`/`BattleScribeArmyImport` live in `Domain.Import` itself (not
this namespace) - see "Session-Backed Import" above.

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
  Datasheet, Enhancements: IReadOnlyList<Ability>, ModelLines
                     // Enhancements holds resolved Ability objects (via ArmyRosterEnricher's
                     // Enhancement resolution, above), not raw name strings - since
                     // resolve-enhancement-abilities (element type was previously
                     // IReadOnlyList<string>, populated straight from the parsed export with no
                     // cross-reference against the catalogue at all - confirmed unused by any
                     // other code path in `src/` at the time of that change).
  IsPresent   // any model-line with RemainingCount > 0
  Components -> [this]

AttachedUnit : ICombatUnit
  Bodyguard: Unit, Attached: IReadOnlyList<Unit>   // open collection, not fixed Leader/Support slots
  Components -> [Bodyguard, ..Attached]

ICombatUnit
  Components: IReadOnlyList<Unit>   // Composite-pattern shape; lets aggregate-view logic
                                     // treat a plain Unit and an AttachedUnit uniformly
  Name: string                      // computed, not stored - see Pure functions below
  IsHalfStrengthOverride: bool      // mutable, live-state, since half-strength-and-
                                     // battleshock-indicators - player-set only, defaulting
                                     // to false. Meaningful only when HalfStrengthResolution
                                     // .StartingStrength(this) == 1 (a genuine single-model
                                     // unit, no attachment) - the real at-or-below-half-
                                     // strength determination there is wound-based, which this
                                     // app deliberately does not track (see Deliberate
                                     // Omissions in CLAUDE.md); for a combined starting
                                     // strength of 2+ the computed determination governs
                                     // instead and this value goes unread. One flag per
                                     // ICombatUnit - an AttachedUnit's own flag is unrelated to
                                     // its Bodyguard's/each Attached Unit's own flag (each Unit
                                     // still carries the property, by interface, but only reads
                                     // as meaningful when that Unit itself is the top-level
                                     // ICombatUnit being rendered, never when it's wrapped
                                     // inside an AttachedUnit).
  IsBattleShocked: bool             // mutable, live-state, same change - player-set only,
                                     // defaulting to false; the app never simulates the 2D6-
                                     // vs-Leadership test itself, only records the player's own
                                     // reported result. Never cleared by any other domain
                                     // operation (a casualty adjustment, a half-strength
                                     // change) - 11th edition only clears Battle-shock on a
                                     // *passed* subsequent test, so this is a plain persistent
                                     // latch the player clears themselves, not something this
                                     // app auto-resets each Command phase (it has no turn-
                                     // tracking concept to hang that on anyway).
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
- `HalfStrengthResolution` (since half-strength-and-battleshock-indicators) — `StartingStrength
  (ICombatUnit)`/`CurrentStrength(ICombatUnit)` sum `Σ ModelLine.Count`/`Σ ModelLine
  .RemainingCount` across every one of `Components` together, matching the real rule that an
  attached unit's starting strength is the number of models it contains at the start of the first
  battle round (combined, not per-component). `IsAtOrBelowHalfStrength(ICombatUnit)` is true when
  `CurrentStrength <= floor(StartingStrength / 2)` (rounded down - a 5-model unit's threshold is 2,
  not 3, so it takes 3 casualties, not 2, to reach it; confirmed against a real user-reported bug -
  the initial implementation used `ceil`, one casualty too eager), but only meaningful (and only ever computed)
  when `StartingStrength >= 2` — a combined starting strength of exactly 1 always returns `false`
  here regardless of remaining wounds, since the real determination for that case is wound-based
  and this app has no partial-wound data to compute it from (see `ICombatUnit
  .IsHalfStrengthOverride` above). `IsAtOrBelowHalfStrengthStatus(ICombatUnit)` is the one combined
  read most callers actually want — the computed value when `StartingStrength >= 2`, the player-set
  override when it's exactly 1 — without the caller needing to know which of the two applies.

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

AggregateAbilityEntry(ComponentName: string?, StatlineName: string?, Ability: Ability,
                       ContributingComponentNames: IReadOnlyList<string> = [])
  // StatlineName is null for a Datasheet-sourced ability (Unit.Datasheet.Abilities) or a resolved
  // Enhancement (Unit.Enhancements, since display-resolved-enhancements) - neither is tied to any
  // one model-line, both apply to the whole component - and set to a specific statline name for a
  // ModelLine-sourced ability (that ModelLine's own Abilities, e.g. a wargear-granted ability like
  // Impulsor's "Shield Dome"). This applies regardless of Ability.Scope: Scope alone decides which
  // UI column (Model vs Unit) an entry belongs in; source alone decides which statline row(s) it
  // binds to. Ability.Origin (not this record's own shape) is what /LivePlay reads to render an
  // Enhancement-classified entry with its leading "✦ " indicator - see
  // openspec/specs/live-play-view/spec.md's "Enhancement Ability Visual Indicator". No
  // cross-component combination or deduplication - two components each having an ability of the
  // same Name produce two separate entries - EXCEPT (since gate-and-dedupe-core-rule-abilities)
  // when two or more present components each carry a CoreRule-origin ability with an identical
  // Name: confirmed structurally that every component's own reference resolves the identical
  // BSData targetId, not independently-authored text (unlike e.g. "Leader"/"Support", which
  // genuinely differ per datasheet) - collapsed into ONE entry with ComponentName null,
  // StatlineName null, and ContributingComponentNames listing every contributor. Renders as its
  // own dedicated grid row above every component's own rows (never spanning through them, unlike
  // a component-wide ability). ContributingComponentNames is non-empty only for this one case -
  // used solely to evaluate this entry's own collapse rule ("hidden once every contributing
  // component is fully dead", distinct from a component-wide entry's single-component rule).
  // Falls out for free from AttachedUnitAggregator re-running fresh every request: once fewer
  // than two present components still contribute the same Name, it's simply no longer merged (or,
  // with zero contributors left, doesn't appear at all) - no separate death-tracking needed.
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
  `Datasheet.Ability` (`StatlineName: null`), one entry per resolved `Unit.Enhancements`
  ability (`StatlineName: null`, since `display-resolved-enhancements` - reported the same way as
  a Datasheet-sourced ability, not any new source-specific shape), then one entry per `Ability` on
  each of that component's own `ModelLine`s with `RemainingCount > 0` (`StatlineName:` that line's
  own name). No cross-component combination or deduplication, regardless of `Ability.Scope` —
  mirrors `Statlines`' per-component scoping exactly. The explicit `IsPresent` guard is needed for
  both component-wide sources (Datasheet-sourced and Enhancement-sourced alike): unlike
  `BuildStatlines`, where a fully-dead component naturally produces zero entries through its
  per-statline `RemainingCount` check, neither has a statline-level gate of its own to fall
  through. A `ModelLine`-sourced entry (any Scope) disappears once that line's `RemainingCount`
  reaches 0, since it's simply excluded from that component's model-line scan on the next `Build`.
  `/LivePlay` renders an Enhancement-classified entry (`Ability.Origin == Enhancement`) with a
  leading `✦ ` before its name, wherever an ability name renders (including a rule-text popover's
  own title bar, since it reuses the same rendered name) - see `_UnitBlock.cshtml`'s
  `AbilityDisplayName` helper and `openspec/specs/live-play-view/spec.md`'s "Enhancement Ability
  Visual Indicator". An Enhancement renders in the Unit Abilities column unconditionally today,
  since `Ability.Scope` is already always `Unit` for every ability BSData resolution produces (see
  "Ability Extraction and Classification" above) - not a new scope decision this change adds. A
  genuine per-Enhancement Model/Unit scope is deferred to the future ability-text classification
  pass (`.claude/vnext-ideas.md`).
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
(ordered by selection, one entry per detachment a battle size's detachment points bought - since
`display-army-header-and-detachment-rules`, `IReadOnlyList<ResolvedDetachment>` rather than bare
names: `ResolvedDetachment(Name, Rules: IReadOnlyList<DetachmentRule>)`,
`DetachmentRule(Name, Text)` - see "Detachment Resolution" above for how each entry is resolved),
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
