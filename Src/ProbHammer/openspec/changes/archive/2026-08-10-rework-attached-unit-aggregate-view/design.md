## Context

`AttachedUnitAggregator.Build()` filters to `presentLines` — every `(Unit, ModelLine)` pair across `combatUnit.Components` with `RemainingCount > 0` — once, up front, and threads that single filtered list into `BuildStatlines` and `BuildWeapons`. `BuildStatlines` dedupes by `ModelLine.StatlineName` (one row per distinct name, first-matching model-line supplies the `Statline` value) and discards which specific model-lines contributed. `BuildWeapons` groups by `WeaponProfile.EqualityKey()` (Type/Skill/S/Ap/D/ability-flags — deliberately excludes Name/Range/Attacks) and sums `RemainingCount` into a `Count`, but keeps only the first-inserted contributor's `WeaponProfile` — so a merged row's `A` silently reflects one arbitrary contributor, not a total. Both gaps were invisible until the view was checked against a real, hand-entered army with an attached Leader whose weapon happened to share a structural profile with the Bodyguard's.

## Goals / Non-Goals

**Goals:**
- Fix `BuildWeapons` to report a true aggregated `TotalAttacks` per weapon group, computed via `DiceExpression.Scale`/`Add`, without changing the existing `EqualityKey` merge boundary (it's already correct).
- Retain per-contribution provenance (which component/model-line, how many models, what per-model Attacks) on both weapon groups and statline groups, so a merged total is never a dead end.
- Add current/initial model counts and a per-model-line loadout breakdown to statlines, correct even across partial casualties that fully remove one loadout-variant while another survives.
- Surface `Statline.InSv` and correct the stale `.claude/domain-model-11e.md`.
- Update `/LivePlay` to the new shapes with plain markup — no visual redesign.

**Non-Goals:**
- Building any UI that lets a player interact with the provenance data (toggle/drill-down) — this change only ensures the data exists.
- Casualty-removal UI/API, session persistence, ability-driven modifiers, Ranged/Melee-specific rendering, Keywords/FactionKeywords split, Simulation integration — all deferred per the proposal.

## Decisions

### 1. `AggregateWeaponEntry` carries a total plus per-contribution provenance, not a single representative `Profile.A`

```csharp
public sealed record WeaponContribution(
    string ComponentName,      // owning Unit.Datasheet.Name, e.g. "Marshal", "Crusader Squad"
    string StatlineName,       // the contributing ModelLine's StatlineName
    int Count,                 // RemainingCount of that model-line
    DiceExpression PerModelAttacks);

public sealed record AggregateWeaponEntry(
    WeaponProfile Profile,             // identity only: Name/Type/Range/Skill/S/Ap/D/ability flags
    DiceExpression TotalAttacks,       // Scale(contribution.Count) + Add(...) across every contribution
    IReadOnlyList<WeaponContribution> Contributions);
```

`Profile` is retained because rendering still needs Name/Range/Skill/S/Ap/D and the ability tags — but `Profile.A` on a merged entry is no longer meaningful (it's whichever contributor happened to be inserted first) and must never be read for display; only `TotalAttacks` is authoritative. This is a documentation-level guard, not a type-level one — noted as a risk below rather than solved by, say, splitting `A` out of `WeaponProfile` entirely, which would ripple into `Simulation/*` and the Catalogue model for no benefit to this change.

**Contribution granularity is per-`ModelLine`, not per-component.** NewRecruit's own reference UI rolls up to component level only (`"Astartes Chainsword (x7) - Crusader Squad"`, not split further into the Initiate/Neophyte model-lines that make up that 7) — but our `AggregateStatlineEntry` loadout breakdown (Decision 3) is already per-`ModelLine`, and matching that granularity here means a future consumer can always roll `Contributions` up to component level for display, whereas the reverse isn't possible if only component subtotals were kept.

**Alternative considered**: keep `BuildWeapons`' current `(Profile, Count)` shape and just fix `Count`'s meaning to be "total attacks" instead of "model count." Rejected — conflates two different quantities (how many dice to roll vs. how many models are still alive) into one field, and silently drops the provenance this change explicitly needs for the future Ranged/Melee toggle.

### 2. Aggregating Attacks: `Scale`/`Add`, not a fresh algorithm

For each `WeaponContribution`, `PerModelAttacks.Scale(Count)` gives that contribution's total; `TotalAttacks` is the `Add`-reduction across all contributions sharing the `EqualityKey`. This reuses `DiceExpression` methods that already exist for exactly this purpose (ported from `SimulationAdapter.AggregateAttacks`), so no new aggregation logic is introduced — `BuildWeapons` changes what it does with the numbers it already has, not how it computes them.

Verified by hand against `Units.CrusaderSquad_Marshal_Lieutenant()`:
- Astartes Chainsword: Initiate(3 × A4) + Neophyte(4 × A4) → `Scale(3)+Scale(4)` → 28 (fixed).
- Master-crafted Power Weapon w/ Lethal Hits: Marshal(1 × A7) + Sword Brother(1 × A3) → 10.
- Master-crafted Power Weapon, no Lethal Hits (Lieutenant only): stays a separate `EqualityKey` group (Lethal Hits differs) → 5.

### 3. `AggregateStatlineEntry` gains counts and a nested per-`ModelLine` loadout breakdown

```csharp
public sealed record ModelLineLoadout(
    string WeaponsLabel,      // human-readable loadout, e.g. "Bolt pistol, Heavy Bolt pistol, Power fist"
    int RemainingCount,
    int InitialCount);

public sealed record AggregateStatlineEntry(
    string StatlineName,
    Statline Statline,
    int RemainingCount,       // summed across every ModelLine sharing this name
    int InitialCount,         // summed across every ModelLine sharing this name, including fully-dead ones
    IReadOnlyList<ModelLineLoadout> Loadouts);
```

`WeaponsLabel` is derived by joining `ModelLine.Weapons` (comma-separated) — matching the loadout-naming pattern already validated against a real design sketch and against how NewRecruit itself auto-labels loadout variants ("Initiate w/Chainsword & Heavy Bolt Pistol").

### 4. `Build()`'s casualty filtering can no longer be a single up-front `presentLines` list

Today, `Build()` filters to `RemainingCount > 0` once, before either builder runs. That's still correct for `BuildWeapons` — a fully-dead model-line contributes nothing to a weapon total, full stop. It's *not* correct for the new statline behavior: if a statline name has two model-lines (e.g. Initiate ×2-with-Power-Fist and ×3-with-Chainsword) and the Power-Fist line is fully wiped, `InitialCount` must still read 5 (not 3), and the Power-Fist `ModelLineLoadout` should still appear in the breakdown showing `0 / 2` — a player needs to see "I have zero Power-Fist Initiates left," not have that loadout silently vanish as if it never existed.

So `BuildStatlines` needs access to *every* model-line belonging to a present component (regardless of that specific line's `RemainingCount`), while `BuildWeapons` keeps using the `RemainingCount > 0`-filtered view exactly as today. The existing top-level rule — a statline disappears from the view entirely once *every* model-line sharing its name has `RemainingCount == 0` — is unchanged; it now applies per statline-name-group rather than per individual line.

**Alternative considered**: keep one shared `presentLines` list and have `BuildStatlines` re-derive the full set from `combatUnit.Components` independently. Rejected in favor of making both builders take explicit, purpose-appropriate inputs (`BuildWeapons(presentLines)`, `BuildStatlines(allLinesByComponent)`) — an implicit "this one list secretly means two different things depending which builder reads it" would be a correctness trap for the next person touching this file.

### 5. Remove `LivePlay.cshtml.cs`'s `GroupStatlinesByValue`

Collapsing statline rows by shared M/T/Sv/W/Ld/Oc value (added in `render-aggregate-view`) actively conflicts with per-name current/initial tracking — it would merge "Initiate" and "Sword Brother" in `CrusaderSquad_Marshal_Lieutenant()` despite them needing independent casualty tracking. `LivePlayModel` renders one row per statline name (as `AttachedUnitAggregator.BuildStatlines` already produces), each with its nested `Loadouts`.

## Risks / Trade-offs

- **[Risk]** `AggregateWeaponEntry.Profile.A` no longer reflects a real quantity once a row is merged (it's an arbitrary contributor's value) → **Mitigation**: doc-comment on the record stating `Profile.A` must not be rendered; `LivePlay.cshtml` only ever reads `TotalAttacks`. Not type-enforced; flagged for anyone extending the view later.
- **[Risk]** `DiceExpression.Add` throws if two contributions sharing an `EqualityKey` have dice-based Attacks with different `Sides` (e.g. D3 vs D6) → **Mitigation**: none added defensively. Every field that differs meaningfully between two weapons (Skill/S/Ap/D/all ability flags) is already part of the `EqualityKey`; two profiles matching on all of those but disagreeing on Attacks' dice type is not expected to occur in real catalogue data. Let it throw loudly, consistent with `ResolveWeaponProfile`'s existing throw-on-missing behavior — a thrown exception here would mean bad fixture/catalogue data, the same class of bug the "Bold Pistol"/"Bolt Pistol" mismatch turned out to be.
- **[Risk]** Splitting `Build()`'s internals into two differently-filtered views of the same underlying model-lines is a bigger change to a load-bearing method than it looks → **Mitigation**: keep the two inputs explicit and separately named in the implementation (no shared variable reused for both purposes); extend existing `AttachedUnitAggregatorTests` coverage for the partial-wipe/fully-dead-loadout scenario specifically, since it's the one behavior change that isn't obvious from either builder in isolation.
- **[Trade-off]** Dropping by-value statline collapsing means `/LivePlay` shows more rows for units like Assault Intercessor Squad (Sergeant/trooper share a statline) than it did after `render-aggregate-view` — a deliberate trade of visual tidiness for live-tracking correctness.

## Migration Plan

No persisted state to migrate (`LivePlay.OnGet()` rebuilds from `Examples.View.MyArmy()` every request). This changes `AttachedUnitAggregateView`'s public shape; its only consumer (`LivePlay.cshtml.cs`) is updated in the same change — no compatibility shim, per project convention. Rollback is a plain revert.

## Open Questions

- Should the render collapse a statline's `Loadouts` list down to nothing when there's exactly one contributing model-line (avoiding a redundant single-child nested row)? Leaning toward: yes, but that's a `LivePlay` presentation decision, not a domain-model one — the domain always returns the full `Loadouts` list regardless of count.
