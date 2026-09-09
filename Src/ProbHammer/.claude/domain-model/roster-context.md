# Roster Context

```
ModelLine(StatlineName, Weapons: IReadOnlyList<string>, Count, Abilities: IReadOnlyList<Ability>,
          Keywords: IReadOnlySet<string> = [])
                     // see Keywords property's own doc comment
  RemainingCount   // live, adjustable in both directions; alive/dead granularity only, clamped
                   // to [0, Count]
  SetRemainingCount(value)   // see method's own doc comment
  RemoveCasualties(n)         // thin wrapper: SetRemainingCount(RemainingCount - n)

Unit : ICombatUnit
  Datasheet, Enhancements: IReadOnlyList<Ability>, ModelLines
                     // Enhancements holds resolved Ability objects (via ArmyRosterEnricher's
                     // Enhancement resolution in army-list-import-pipeline.md), not raw name strings - was
                     // IReadOnlyList<string>, populated straight from the parsed export with no
                     // cross-reference against the catalogue at all, confirmed unused elsewhere
                     // at the time.
  IsPresent   // any model-line with RemainingCount > 0
  Components -> [this]

AttachedUnit : ICombatUnit
  Bodyguard: Unit, Attached: IReadOnlyList<Unit>   // open collection, not fixed Leader/Support slots
  Components -> [Bodyguard, ..Attached]

ICombatUnit
  Components: IReadOnlyList<Unit>   // see interface's own doc comment
  Name: string                      // computed, not stored - see Pure functions below
  IsHalfStrengthOverride: bool      // see property's own doc comment. One flag per ICombatUnit -
                                     // an AttachedUnit's own flag is unrelated to its Bodyguard's/
                                     // each Attached Unit's own flag.
  IsBattleShocked: bool             // see property's own doc comment; 11e only clears Battle-shock
                                     // on a *passed* subsequent test, so this is a plain persistent
                                     // latch the player clears themselves (the app has no turn-
                                     // tracking concept to auto-reset it against anyway).
```

**Pure functions (not stored state):**
- `KeywordResolution.EffectiveKeywords(ICombatUnit)` — see class's and method's own doc comments.
  Model-level keyword checks must read a specific `Unit.Datasheet.Keywords` together with that
  specific model's own `ModelLine.Keywords` directly, never a sibling `ModelLine`'s and never this
  union.
- `ToughnessResolution.ResolveDefendingToughness(AttachedUnit)` — see class's and method's own doc
  comments.
- `ICombatUnit.Name` — see `ICombatUnit.Name`'s own doc comment (computed on read, not stored) and
  `AttachedUnit.Name`'s own doc comment for the humanized-join shape. Duplicate attached-leader
  names are not deduplicated (two Marshals render as `"...with Marshal and Marshal"`) — a known,
  accepted limitation.
- `HalfStrengthResolution` — see each method's own doc comment (`StartingStrength`/
  `CurrentStrength`/`IsAtOrBelowHalfStrength`/`IsAtOrBelowHalfStrengthStatus`). Rounds down (a real
  bug once used `ceil`, one casualty too eager).

**Aggregate view** (`AttachedUnitAggregator.Build(ICombatUnit) -> AttachedUnitAggregateView`):

```
AttachedUnitAggregateView(Name: string, IsAttachedUnit: bool, Statlines, Weapons,
                           Abilities, Keywords)
  // Name is the source combatUnit.Name, copied through unchanged. IsAttachedUnit is
  // `combatUnit is AttachedUnit` - lets a page-layer consumer distinguish source type without
  // re-deriving it from Statlines/Weapons shape; true even with zero Attached units.

ModelLineLoadout(WeaponsLabel, Weapons: IReadOnlyList<string>, RemainingCount, InitialCount)
  // WeaponsLabel is Weapons (== that ModelLine.Weapons) comma-joined, e.g. "Bolt pistol, Heavy Bolt
  // pistol, Power fist" - Weapons itself is carried alongside for a consumer that needs the raw list.

AggregateStatlineEntry(ComponentName, StatlineName, Statline, RemainingCount, InitialCount,
                        Loadouts: IReadOnlyList<ModelLineLoadout>)
  // ComponentName is the owning component's Datasheet.Name - lets a downstream consumer detect
  // component boundaries without re-deriving them from Statlines' declared order.
  // RemainingCount/InitialCount are summed across every ModelLine sharing StatlineName, including
  // fully-removed casualties. Loadouts has one entry per contributing ModelLine (not collapsed by
  // weapon list) so a fully-wiped loadout-variant still shows 0/InitialCount instead of
  // disappearing.

WeaponContribution(ComponentName, StatlineName, Count, PerModelAttacks, LoadoutIndex = -1)
  // ComponentName is the owning Unit.Datasheet.Name; Count is that ModelLine's RemainingCount.
  // LoadoutIndex is the contributing ModelLine's position within its statline's own Loadouts list
  // (-1 when the statline has only one ModelLine, so no Loadouts breakdown renders at all) - see
  // the record's own doc comment for why it's needed (two sibling loadouts under the same statline
  // name are otherwise indistinguishable by ComponentName/StatlineName alone, and Count isn't
  // reliable either, since two loadouts can coincidentally share a model count).

AggregateWeaponEntry(Profile: WeaponProfile, TotalAttacks: DiceExpression,
                      Contributions: IReadOnlyList<WeaponContribution>)
  // see record's own doc comment

AggregateAbilityEntry(ComponentName: string?, StatlineName: string?, Ability: Ability,
                       ContributingComponentNames: IReadOnlyList<string> = [])
  // see the record's own doc comment for the full rationale and collapse rule (StatlineName null
  // vs. set; the gate-and-dedupe-core-rule-abilities cross-component collapse). Also has a 3-arg
  // convenience ctor (ComponentName: string, StatlineName, Ability) forwarding to the 4-arg form
  // with ContributingComponentNames: [] - the ordinary single-component-source case.
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
  merged abilities column (`live-play-landscape-only`) since `Ability.Scope` is always `Unit` for
  every BSData-resolved ability; a genuine per-Enhancement Model/Unit scope is deferred
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

**`ArmyRoster`** (`Domain/Roster/ArmyRoster.cs`) wraps the per-unit roster (`Units`, an
`IReadOnlyList<ICombatUnit>`) with army-level metadata — see `Faction`'s and `Detachments`' own
doc comments for their ordering conventions (`ResolvedDetachment(Name, Rules: IReadOnlyList<DetachmentRule>)`,
`DetachmentRule(Name, Text)` — see "Detachment Resolution" in army-list-import-pipeline.md).
Originally added ahead of the (then-unbuilt) GW-app export parser so parsing work had a settled target type;
`import-army-list-for-live-play`'s real `ArmyListParser`/`ArmyRosterEnricher` now populate this
exact shape from a real pasted export unchanged. `PointsSpent` is still plain sample data on the
`Examples/` fixture path — not derived from or reconciled against `Units`, since no points value is
tracked anywhere below `ArmyRoster`; a real import's `PointsSpent` comes from the export text
itself. `Examples/View.MyArmyRoster()` remains a thin projection with no `/LivePlay` call site —
`Examples/` stays in the tree for future exploration, unreferenced by `Web`.
