# Catalogue Context

```
Datasheet
  Name, FactionKeywords, Keywords, Abilities (unit-wide, intrinsic only - see below)
  Statlines: IReadOnlyList<(string Name, Statline Statline)>   // ordered - see the ctor's own doc
                                                      // comment for why
  GetStatline(name) -> Statline         // O(1), backed by an internal Dictionary built once from
                                         // the ordered list
  ResolveWeaponProfile(name) -> WeaponProfile   // see method's own doc comment
  TryResolveAbility(name) -> Ability?   // see method's own doc comment
  OptionalAbilityNames: IReadOnlyList<string>   // see property's own doc comment
  ctor(..., statlines: IReadOnlyList<(string Name, Statline Statline)>, weaponProfiles: IEnumerable<WeaponProfile>,
       optionalAbilities: IEnumerable<Ability>? = null)
                                         // see ctor's own doc comment for the Name-keying
                                         // rationale; optionalAbilities defaults to empty

Statline(M, T, Sv, W, Ld, Oc: ScalarCharacteristicView)   // value object — field names match
                                         // official shorthand; all six scalars are
                                         // ScalarCharacteristicView, not plain int (retyped by
                                         // unify-objective-control-characteristic-view/the
                                         // "Remaining Scalar Characteristics Retyped" work - see
                                         // rules-glossary-and-popovers.md and
                                         // characteristic-value-domain-model.md)
  InSv: InvulnerableSaveCharacteristicView   // init-only, defaults to absent/non-caveated/no
                                         // contributing abilities (unify-invulnerable-save-
                                         // characteristic-view); see the Invulnerable Save
                                         // discussion in bsdata-json-ingestion.md and
                                         // characteristic-value-domain-model.md's own
                                         // InvulnerableSaveCharacteristicView entry

DiceExpression(Count, Sides, Modifier)  // value object — see class's own doc comment
  Parse(string) -> DiceExpression       // "3" | "D6" | "2D3+1"
  implicit operator DiceExpression(int) // see operator's own doc comment
  D3, D6                                // static presets for the common single-die case
  operator +(DiceExpression, int)       // see operator's own doc comment
  Scale(n), Add(other)                  // multi-model attack/damage aggregation
  ExpectedValue() -> double             // Count==0 ? Modifier : Count*(Sides+1)/2.0 + Modifier;
                                         // see method's own doc comment for why

WeaponProfile(Name, Type, Range, A, S, Ap, D)   // abstract value object; A/D are DiceExpression
                                                 // (fixed or dice, e.g. Multi-melta D6 damage);
                                                 // S/Ap are ScalarCharacteristicView, not plain int
                                                 // (see Statline's own note above)
  Skill: ScalarCharacteristicView        // abstract computed property, see below
  Torrent, Blast, Melta, RapidFire, SustainedHits, LethalHits, DevastatingWounds,
    TwinLinked, IndirectFire, Pistol, IgnoresCover, Assault, Anti
                                         // init-settable ability flags, flattened directly onto
                                         // WeaponProfile (no nested WeaponAbilities type)
  EqualityKey() -> WeaponProfileEqualityKey   // see method's own doc comment

RangedWeapon(Name, Range, A, Bs, S, Ap, D) : WeaponProfile   // Type fixed Ranged; Skill => Bs
MeleeWeapon(Name, A, Ws, S, Ap, D) : WeaponProfile           // Type fixed Melee, Range fixed 0;
                                                              // Skill => Ws

Ability(Name, Text, Choices: IReadOnlyList<AbilityChoice>, Scope: Model | Unit, Origin: AbilityOrigin)

AbilityOrigin = Intrinsic | Enhancement | OptionalGrant | CoreRule | ArmyRule
```

`AbilityOrigin` classifies where an ability came from during `BsdataDatasheetMapper`'s walk, not a
player-facing concept the export text states directly. **Intrinsic**, **Enhancement**, and
**OptionalGrant** — see `ProcessAbilityProfile`'s own doc comment for the exact classification
mechanics (the `type: upgrade` signal, and the case-insensitive "Enhancements" substring match
needed for a nested sub-pool like "Legends of Saga and Song Enhancements"). **CoreRule**: a
datasheet-wide reference to a separately-defined Core/faction rule (Oath of Moment, a Chapter's own
Vows, Deadly Demise, Firing Deck, Infiltrators, Scouts) — see `AbilityOrigin.CoreRule`'s own doc
comment for why it's always exposed rather than an on-demand grant. **ArmyRule**: a CoreRule-shaped
reference whose Name matches `classify-known-army-rules`' curated per-faction `ArmyRuleNameLookup`
table (e.g. Oath of Moment, Templar Vows, Nurgle's Gift (Aura)) — see "Core Versus Army Rule Origin
Classification" in bsdata-json-ingestion.md.

**Key design points:**
- A Datasheet can have any number of named statlines (not "1 or sometimes 2") — a Chaos Space
  Marine datasheet has 5. The same statline name can be referenced by model-lines with different
  weapon eligibility (Sergeant-style).
- Weapon profiles never materialize as a full options menu (see `Datasheet`'s own class doc
  comment) — ProbHammer only shows wargear that was actually chosen.
- `Ability.Scope` is a property of the ability itself, not of its source (Enhancement, wargear,
  intrinsic) — 11e rules text treats those as examples of sources, not special cases.
- `WeaponProfile.EqualityKey()` was ported from `SimulationAdapter.WeaponGroupKey` (10e code)
  rather than reimplemented — see the method's own doc comment for what it compares.
- `WeaponProfile` is abstract with sealed `RangedWeapon`/`MeleeWeapon` subtypes; `Skill` is an
  abstract *computed* property rather than a stored value — see the class's own doc comment (and
  `Skill`'s) for why.
- Field names (`Statline.M/T/Sv/W/Ld/Oc`, `WeaponProfile.A/S/Ap/D`) intentionally match official
  40k shorthand rather than spelled-out names.
- `DiceExpression` lives in `Domain.Catalogue`, not `Simulation/*` where it originated — it's a
  game-rules quantity `WeaponProfile.A`/`D` depend on directly. `Simulation/*` now references it
  via a `using`-only repoint (`promote-dice-expression-to-domain`); this does **not** mean
  `Simulation/*` is wired onto this domain model — see deliberate-omissions.md.
- `DiceExpression`'s implicit `int` conversion (see its own doc comment) was chosen over
  constructor overloads on `RangedWeapon`/`MeleeWeapon` because `A` and `D` vary independently
  between fixed and dice-based (e.g. Multi-melta: fixed `A`, dice `D`) — overloads would need one
  combination per shape; the implicit conversion applies per-argument instead.
- `Datasheet`'s constructor takes `weaponProfiles`/`statlines` as plain enumerables/an ordered
  list rather than pre-built dictionaries — see the constructor's own doc comment for why. Two
  differently-named statlines can share identical `M/T/Sv/W/Ld/Oc` values (e.g. "Assault
  Intercessor" / "Assault Intercessor Sergeant") — why `AttachedUnitAggregator.BuildStatlines`
  dedupes by name, not by value.
