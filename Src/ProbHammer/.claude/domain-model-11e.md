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

Statline(Movement, Toughness, Save, Wounds, Leadership, ObjectiveControl)   // value object

WeaponProfile(Name, Type, Range, Attacks, Skill, Strength, Ap, Damage, Abilities)  // value object
  EqualityKey() -> WeaponProfileEqualityKey   // (Type, Skill, Strength, Ap, Damage, Abilities)
                                               // excludes Name/Range/Attacks — used to aggregate
                                               // weapon instances by profile, not identity

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
