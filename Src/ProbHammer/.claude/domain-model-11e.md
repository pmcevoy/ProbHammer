# Domain Model — 11th Edition (Attached Units)

Full requirements: `openspec/changes/attached-unit-domain-model/` (proposal, design, specs,
tasks). This file summarizes the shape that landed in code.

**Status:** implemented and tested (`src/ProbHammer.Core/Domain/`), proven only against
hand-built fixtures (`tests/ProbHammer.Tests/Domain/Fixtures/`) — no export parser or BSData
wiring yet. Coexists with the 10th-edition model (`.claude/domain-model.md`), which remains
in the tree, untouched, and is what the live Web app and `Simulation/*` still use. See
`PROGRESS.md` for the supersession/pause status of that older code.

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
  InSv: int                     // init-only property, defaults to 0; 0 means "no invulnerable
                                 // save" (consumers must treat 0 as absent, not as a 0+ save)

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

## Roster Context

```
ModelLine(StatlineName, Weapons: IReadOnlyList<string>, Count, Abilities: IReadOnlyList<Ability>)
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
  components. Recomputes live; never cached, so it can't go stale when a component is destroyed.
  Model-level keyword checks must read a specific `Unit.Datasheet.Keywords` directly, never this
  union.
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
  test change in `Simulation/*` itself. `Parsing/ArmyListParser.cs` is also untouched; fixtures
  stand in for parsed input until the 11e export format is studied.
