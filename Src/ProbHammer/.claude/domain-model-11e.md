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
  Statlines: IReadOnlyDictionary<string, Statline>   // enumerable, queryable by name; caller-supplied
                                                      // keys - Statline carries no Name of its own
  ResolveWeaponProfile(name) -> WeaponProfile          // on-demand only, never enumerated
  ctor(..., statlines: IReadOnlyDictionary<string, Statline>, weaponProfiles: IEnumerable<WeaponProfile>)
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
  stays a caller-supplied `IReadOnlyDictionary<string, Statline>` rather than getting the same
  treatment, because `Statline` carries no `Name` of its own — the name is genuine external
  metadata (a role label like "Sergeant"), not something derivable from the value. Two
  differently-named statlines with identical `M/T/Sv/W/Ld/Oc` are valid and expected — real BSData
  exports name "Assault Intercessor" and "Assault Intercessor Sergeant" separately even though
  they share a statline — which is exactly why `AttachedUnitAggregator.BuildStatlines` dedupes by
  name, not by value.

---

## Roster Context

```
ModelLine(StatlineName, Weapons: IReadOnlyList<string>, Count, Abilities: IReadOnlyList<Ability>)
  RemainingCount   // live, decrementing; alive/dead granularity only, floored at 0
  RemoveCasualties(n)

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
AttachedUnitAggregateView(Name: string, Statlines, Weapons, UnitScopedAbilities,
                           ModelScopedAbilities, Keywords)
  // Name is the source combatUnit.Name (see ICombatUnit above), copied through unchanged.

ModelLineLoadout(WeaponsLabel, RemainingCount, InitialCount)
  // WeaponsLabel is ModelLine.Weapons comma-joined, e.g. "Bolt pistol, Heavy Bolt pistol, Power fist"

AggregateStatlineEntry(StatlineName, Statline, RemainingCount, InitialCount,
                        Loadouts: IReadOnlyList<ModelLineLoadout>)
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
```

- `Statlines` — one entry per distinct statline name referenced by any present component's
  model-lines, built from *every* model-line belonging to a component regardless of that specific
  line's own `RemainingCount` (unlike `Weapons` below). A statline name is included only while at
  least one of its model-lines still has `RemainingCount > 0`.
- `Weapons` — grouped by `WeaponProfile.EqualityKey()` (Type/Skill/S/Ap/D/ability flags — excludes
  Name/Range/Attacks) across only `RemainingCount > 0` model-lines. `TotalAttacks` is computed per
  contribution as `PerModelAttacks.Scale(modelLine.RemainingCount)`, `Add`-reduced across every
  contributor sharing the `EqualityKey` — **not** a representative contributor's raw `A` with only
  the model count summed. (That was the bug this shape fixes: two model-lines sharing an
  `EqualityKey` with different per-model Attacks, e.g. 4 models × A3 and 1 model × A7, must total
  19, not silently keep one contributor's A and report Count=5.)
- `UnitScopedAbilities` — combined list of `Scope == Unit` abilities from present components'
  `Datasheet.Abilities` and present model-lines' own `Abilities` (e.g. an Enhancement that buffs
  the whole unit).
- `ModelScopedAbilities` — `Scope == Model` abilities from `ModelLine.Abilities`, kept attached to
  their originating `ModelLine`; disappear once that line's `RemainingCount` reaches 0.
- `Keywords` — wired directly to `KeywordResolution.EffectiveKeywords`.

`AttachedUnitAggregator.Build` internally computes two differently-filtered views over the same
`combatUnit.Components`: an unfiltered per-component model-line set (feeds `BuildStatlines`, so
fully-dead loadout-variants and correct `InitialCount`s stay visible) and a `RemainingCount > 0`
set (feeds `BuildWeapons`, `BuildUnitScopedAbilities`, `BuildModelScopedAbilities` — a fully-dead
model-line contributes nothing to a weapon total or an ability list). These are kept as two
explicitly separate, purpose-named variables rather than one shared list read two ways.

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
