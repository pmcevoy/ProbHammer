## 1. ArmyRoster Type

- [x] 1.1 Create `src/ProbHammer.Core/Domain/Roster/ArmyRoster.cs`: sealed class with `Name`, `PointsSpent`, `Faction` (`IReadOnlyList<string>`), `Detachments` (`IReadOnlyList<string>`), `ForceDisposition`, `BattleSize`, `PointsLimit`, `Units` (`IReadOnlyList<ICombatUnit>`) — constructor-populated get-only properties, matching `Unit`/`AttachedUnit`/`Datasheet`'s existing style in this namespace.
- [x] 1.2 Document the ordering convention on `Faction` (parent codex before sub-faction) and `Detachments` (selection order) in their XML doc comments.

## 2. Example Data

- [x] 2.1 Add `View.Roster()` returning an `ArmyRoster` built from the existing six-unit fixture list (`Units.ScoutSquad()`, `AssaultIntercessorSquad()`, `CrusaderSquad_Helbrecht_Ancient()`, `CrusaderSquad_Marshal_Lieutenant()`, `Impulsor()`, `SwordBretheren_Marshal()`) plus literal metadata transcribed from `data/gw-app-export.txt`: `Name: "For the Emperor"`, `PointsSpent: 1065`, `Faction: ["Space Marines", "Black Templars"]`, `Detachments: ["Companions of Vehemence"]`, `ForceDisposition: "Purge the Foe"`, `BattleSize: "Incursion"`, `PointsLimit: 1000`.
- [x] 2.2 Reimplement `View.MyArmyRoster()` as a thin projection over `View.Roster().Units` (e.g. `.ToList()`), preserving its existing `List<ICombatUnit>` signature and behavior exactly so `LivePlay.cshtml.cs`'s two call sites need no change.

## 3. Tests

- [x] 3.1 Test that `View.Roster().Units` matches exactly what `View.MyArmyRoster()` returned before this change (same six units, same order) — covers the "Units collection is unchanged" scenario.
- [x] 3.2 Test that `View.Roster()`'s metadata fields match the literal sample values from task 2.1 (`Faction` has 2 entries in order, `Detachments` has 1 entry, `ForceDisposition`/`BattleSize` values, `PointsSpent` != `PointsLimit`).
- [x] 3.3 Test that a directly-constructed `ArmyRoster` with a single-entry `Faction` list (e.g. `["Chaos Space Marines"]`) preserves exactly one entry — covers the single-faction/no-sub-faction scenario, not exercised by the `View.cs` example.
- [x] 3.4 Test that a directly-constructed `ArmyRoster` with three `Detachments` entries preserves all three in order — covers the multi-detachment scenario beyond `View.cs`'s single-detachment example.

## 4. Verification

- [x] 4.1 `dotnet build` — 0 errors, 0 warnings.
- [x] 4.2 `dotnet test` — full suite passes, including the new tests from Section 3.
