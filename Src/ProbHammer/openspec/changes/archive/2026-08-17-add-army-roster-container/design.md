## Context

See proposal.md - Why. `Domain.Roster` (`Unit`, `AttachedUnit`, `ModelLine`, `Datasheet`) already models a single unit's composition; nothing above it models the army list itself. `Examples/View.MyArmyRoster()` returns a bare `List<ICombatUnit>`, and `src/ProbHammer.Web/Pages/LivePlay.cshtml.cs` calls it directly at two call sites (initial `OnGet` and the casualty-rebuild path) expecting exactly that shape — this change must not touch either call site.

## Goals / Non-Goals

**Goals:**
- Define `ArmyRoster` as a plain container in `Domain.Roster`, sitting one level above `ICombatUnit`.
- Give `Examples/View.cs` one worked example populated with literal values transcribed from a real captured export (`data/gw-app-export.txt`), so the shape is checked against real data rather than invented.
- Leave `MyArmyRoster()`'s existing signature and behavior untouched for `/LivePlay`.

**Non-Goals:**
- No export parser, no BSData/JSON catalogue wiring.
- No `/LivePlay` rendering of any `ArmyRoster` field.
- No points reconciliation between `PointsSpent`/`PointsLimit` and the `Units` collection — points are not tracked anywhere below `ArmyRoster` today (no field on `Unit`, `AttachedUnit`, or `ModelLine`), so there is nothing to reconcile against.
- No enumeration of the canonical Force Disposition value set. Only two real values have been observed ("Purge the Foe", "Reconnaissance"); inventing a partial enum now risks being wrong.

## Decisions

- **`ArmyRoster` is a sealed class with a constructor and get-only properties**, matching the existing style of `Unit`/`AttachedUnit`/`Datasheet` in this namespace, not a `record`.
- **`Faction` and `Detachments` are `IReadOnlyList<string>`**, not fixed named fields (e.g. a `Codex`/`SubFaction?` pair). Real exports have shown 1 or 2 Faction entries and 1-3 Detachment entries; a fixed-arity shape would break the moment a deeper faction hierarchy or a larger battle size's detachment count shows up. The ordering itself is meaningful (parent codex before sub-faction; detachments in selection order) and is documented on the property, not enforced by the type — the same convention `Datasheet.Statlines` already uses for its declared-order guarantee.
- **`PointsSpent` and `PointsLimit` are two separate `int` fields**, not one `Points` field — the sample data itself shows them differing (975 spent vs. 1000 limit in "For the Emperor"), so collapsing them would silently lose information.
- **A new `View.Roster()` method returns the `ArmyRoster`**; `MyArmyRoster()` is reimplemented as `View.Roster().Units.ToList()` (or equivalent), keeping its public contract identical so `LivePlay.cshtml.cs` needs no change. This mirrors `MyArmyRoster()`'s own existing rationale (avoid duplicating the six-unit list in two places) one level up.
- **Sample data is transcribed from the current `data/gw-app-export.txt`** (post Force-Disposition patch: `Name: "For the Emperor"`, `PointsSpent: 1065`, `Faction: ["Space Marines", "Black Templars"]`, `Detachments: ["Companions of Vehemence"]`, `ForceDisposition: "Purge the Foe"`, `BattleSize: "Incursion"`, `PointsLimit: 1000`). The existing six-unit fixture roster (`Units.ScoutSquad()`, `AssaultIntercessorSquad()`, `CrusaderSquad_Helbrecht_Ancient()`, `CrusaderSquad_Marshal_Lieutenant()`, `Impulsor()`, `SwordBretheren_Marshal()`) does not include that export's "Emperor's Champion" unit, so `PointsSpent: 1065` will not sum from `Units` — accepted per the Non-Goals above, since no points reconciliation is in scope.

## Risks / Trade-offs

- [Risk] `Faction`/`Detachments` as plain `IReadOnlyList<string>` could read as unordered/free-form to a future consumer, losing the "first entry is the parent codex" convention → Mitigation: state the ordering explicitly in each property's doc comment, not only in the spec.
- [Risk] `ForceDisposition` as an unvalidated `string` will silently accept a typo'd or unrecognized value once a real parser starts populating it from export text → Mitigation: deliberately deferred (see Non-Goals); revisit once a parser change has enumerated the real fixed set.

## Migration Plan

Purely additive — no existing type changes shape, no existing caller changes behavior. `/LivePlay` is unaffected.
