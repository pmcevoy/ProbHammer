# Roster Context

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
`IReadOnlyList<ICombatUnit>`) with army-level metadata: `Name`, `PointsSpent`, `Faction` (ordered —
parent codex before sub-faction, e.g. `["Space Marines", "Black Templars"]`), `Detachments`
(ordered by selection — `IReadOnlyList<ResolvedDetachment>`:
`ResolvedDetachment(Name, Rules: IReadOnlyList<DetachmentRule>)`, `DetachmentRule(Name, Text)` —
see "Detachment Resolution" in army-list-import-pipeline.md), `ForceDisposition`, `BattleSize`, `PointsLimit`. Originally
added ahead of the (then-unbuilt) GW-app export parser so parsing work had a settled target type;
`import-army-list-for-live-play`'s real `ArmyListParser`/`ArmyRosterEnricher` now populate this
exact shape from a real pasted export unchanged. `PointsSpent` is still plain sample data on the
`Examples/` fixture path — not derived from or reconciled against `Units`, since no points value is
tracked anywhere below `ArmyRoster`; a real import's `PointsSpent` comes from the export text
itself. `Examples/View.MyArmyRoster()` remains a thin projection with no `/LivePlay` call site —
`Examples/` stays in the tree for future exploration, unreferenced by `Web`.
