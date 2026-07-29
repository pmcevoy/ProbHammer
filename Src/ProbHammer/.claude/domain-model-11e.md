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
  Statlines: IReadOnlyDictionary<string, Statline>   // enumerable, queryable by name
  ResolveWeaponProfile(name) -> WeaponProfile          // on-demand only, never enumerated

Statline(M, T, Sv, W, Ld, Oc)   // value object — field names match official shorthand

WeaponProfile(Name, Type, Range, A, S, Ap, D)   // abstract value object
  Skill: int                                    // abstract computed property, see below
  Torrent, Blast, Melta, RapidFire, SustainedHits, LethalHits, DevastatingWounds,
    TwinLinked, IndirectFire, Pistol, Anti       // init-settable ability properties, flattened
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
```

**Pure functions (not stored state):**
- `KeywordResolution.EffectiveKeywords(ICombatUnit)` — union of `Keywords` over currently-present
  components. Recomputes live; never cached, so it can't go stale when a component is destroyed.
  Model-level keyword checks must read a specific `Unit.Datasheet.Keywords` directly, never this
  union.
- `ToughnessResolution.ResolveDefendingToughness(AttachedUnit)` — highest Toughness among the
  Bodyguard's present models if any remain, else highest among present Leader/Support models.
  Domain fact only; not wired to `Simulation/*` in this change.

**Aggregate view** (`AttachedUnitAggregator.Build(ICombatUnit) -> AttachedUnitAggregateView`):
- `Statlines` — every distinct statline referenced by a present model-line.
- `Weapons` — grouped by `WeaponProfile.EqualityKey()`, summing `RemainingCount` across all
  components' model-lines carrying that profile.
- `UnitScopedAbilities` — combined list of `Scope == Unit` abilities from present components'
  `Datasheet.Abilities` and present model-lines' own `Abilities` (e.g. an Enhancement that buffs
  the whole unit).
- `ModelScopedAbilities` — `Scope == Model` abilities from `ModelLine.Abilities`, kept attached to
  their originating `ModelLine`; disappear once that line's `RemainingCount` reaches 0.
- `Keywords` — wired directly to `KeywordResolution.EffectiveKeywords`.

---

## Deliberate Omissions

- No wargear constraint/composition-rule validation, no points-cost modeling — the exporting app
  is trusted to have already produced a valid list.
- No partial wound-count tracking — alive/dead per model-line only; physical wound markers cover
  the rest at the table.
- Ability text is never auto-parsed into behavioral effects — permanent exclusion, not a
  deferral (some conditions need positional/objective-control state the domain has no way to
  represent even in principle).
- `Simulation/*` (CombatSimulator, SimulationAdapter, WoundPool, DiceExpression) is untouched but
  paused — not wired to this domain model. `Parsing/ArmyListParser.cs` is also untouched;
  fixtures stand in for parsed input until the 11e export format is studied.
