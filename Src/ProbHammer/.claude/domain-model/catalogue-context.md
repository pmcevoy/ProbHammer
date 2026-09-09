# Catalogue Context

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
  InSv: InvulnerableSaveCharacteristicView   // init-only, defaults to absent/non-caveated/no
                                         // contributing abilities (unify-invulnerable-save-
                                         // characteristic-view); see the Invulnerable Save
                                         // discussion in bsdata-json-ingestion.md and
                                         // characteristic-value-domain-model.md's own
                                         // InvulnerableSaveCharacteristicView entry

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
Classification" in bsdata-json-ingestion.md. Enhancement and OptionalGrant abilities are resolvable only via
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
  `Simulation/*` is wired onto this domain model — see deliberate-omissions.md.
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
