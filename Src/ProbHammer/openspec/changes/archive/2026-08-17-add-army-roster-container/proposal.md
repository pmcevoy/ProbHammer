## Why

`ProbHammer.Core.Domain.Roster` has no type above a single `ICombatUnit` — `Examples/View.MyArmyRoster()` returns a bare `List<ICombatUnit>` with nowhere to hold the army-level facts every real GW app export carries (army name, points spent, faction, detachment(s), Force Disposition, battle size and its points limit). This gap surfaced while studying a real GW app export (`data/gw-app-export.txt` and its variants) in preparation for a future export-parser change: the parser will need somewhere to put this data, and today there is nowhere. Adding the container now, ahead of the parser, gives that future work a settled target type and lets `Examples/View.cs` demonstrate it with real sample data instead of guessing at the shape mid-parser-design.

## What Changes

- New `ArmyRoster` class in `ProbHammer.Core.Domain.Roster`: `Name`, `PointsSpent`, `Faction` (ordered, one or more entries — e.g. `["Chaos Space Marines"]` or `["Space Marines", "Black Templars"]` for a sub-faction with its own parent codex), `Detachments` (ordered, one or more entries), `ForceDisposition`, `BattleSize`, `PointsLimit`, and `Units` (the existing per-unit roster).
- `Examples/View.cs`: the existing six-unit fixture list becomes the `Units` of a new example `ArmyRoster`, populated with literal sample data drawn from `data/gw-app-export.txt` (`"For the Emperor"`, `Space Marines`/`Black Templars`, `Companions of Vehemence`, `Purge the Foe`, `Incursion`, 1000-point limit). `MyArmyRoster()` keeps its existing signature and behavior (still a `List<ICombatUnit>`, still what `/LivePlay` consumes) by reading through the new `ArmyRoster.Units`, so no existing caller changes.
- No parsing, no BSData/JSON catalogue wiring, and no `/LivePlay` rendering of the new fields — purely the domain type and its example data. `ArmyRoster.PointsSpent` is illustrative sample data only; it is not derived from `Units` and is not cross-checked against it, consistent with this project's existing no-points-modeling stance (`Unit`/`ModelLine` carry no points anywhere).

## Capabilities

### New Capabilities
(none)

### Modified Capabilities
- `roster-model`: adds a new requirement for the `ArmyRoster` container type wrapping the existing per-unit roster with army-level metadata.

## Impact

- `src/ProbHammer.Core/Domain/Roster/` gains a new `ArmyRoster.cs` file.
- `src/ProbHammer.Core/Domain/Examples/View.cs` changes its example construction to build an `ArmyRoster`; `MyArmyRoster()`'s public signature and return value are unchanged.
- No impact to `src/ProbHammer.Web` — `/LivePlay` continues to call `View.MyArmyRoster()` exactly as before.
