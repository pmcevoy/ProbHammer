## 1. Domain: ordered Datasheet statlines

- [x] 1.1 Change `Datasheet.Statlines` to `IReadOnlyList<(string Name, Statline Statline)>` and the
      constructor's `statlines` parameter to match
- [x] 1.2 Build an internal `Dictionary<string, Statline>` from the ordered list in the
      constructor for `GetStatline(name)` lookups; keep its behavior (including the
      not-found exception) unchanged
- [x] 1.3 Add a `DatasheetTests.cs` test asserting enumeration order matches construction order,
      independent of the names involved

## 2. Migrate Datasheet construction call sites

- [x] 2.1 Migrate all 9 `Datasheet` construction sites in `Examples/Datasheets.cs` from
      `new Dictionary<string, Statline> { [...] = ... }` to the ordered list form; for each,
      verify the Sergeant/leader-equivalent statline is listed first, reordering where it isn't
- [x] 2.2 Migrate the 4 sites in `tests/.../Fixtures/DatasheetFixtures.cs`
- [x] 2.3 Migrate the 2 sites in `tests/.../Fixtures/AttachedUnitFixtures.cs`
- [x] 2.4 Migrate the 6 sites in `tests/.../Fixtures/AggregateViewFixtures.cs`
- [x] 2.5 Migrate the 1 inline `Datasheet` construction in
      `tests/.../Roster/AttachedUnitAggregatorTests.cs`
- [x] 2.6 `dotnet build` — confirm no remaining call site still passes a `Dictionary` literal

## 3. Aggregator: per-component, declared-order statline building

- [x] 3.1 Rewrite `AttachedUnitAggregator.BuildStatlines` to take the `ICombatUnit` (not a
      pre-flattened line list): compute component display order (`AttachedUnit` → Attached-list
      order then Bodyguard; plain `Unit` → `Components`), then for each component walk its
      Datasheet's declared `Statlines` in order, matching that component's own `ModelLine`s by
      name (case-insensitive), skipping names with no present model-lines
- [x] 3.2 Update the `Build` call site to pass `combatUnit` into `BuildStatlines` instead of the
      flattened `allLines`
- [x] 3.3 Add an `AttachedUnitAggregatorTests.cs` test: an AttachedUnit with Attached units and a
      Bodyguard, each Datasheet declaring its Sergeant/leader entry first, asserting `Statlines`
      order is Attached-units-first (in their list order) then Bodyguard, sergeant-first within
      each
- [x] 3.4 Add a test: a plain Unit's `Statlines` follow its own Datasheet's declared order
- [x] 3.5 Add a test: two different components sharing a statline name produce two separate
      entries, not one merged entry
- [x] 3.6 Confirm existing same-component merge tests still pass unchanged
      (`StatlineView_InitialCountSumsAcrossModelLines_EvenWhenOneIsFullyRemoved`,
      `StatlineView_LoadoutBreakdownIncludesFullyRemovedModelLine`, etc.)

## 4. Documentation

- [x] 4.1 Update `.claude/domain-model-11e.md`: `Datasheet.Statlines`'s new ordered shape, and
      `AttachedUnitAggregator.BuildStatlines`'s component-order/declared-order/per-component-merge
      rules
- [x] 4.2 Update `PROGRESS.md` once implementation lands, per this project's CLAUDE.md
      doc-maintenance rule

## 5. Verification

- [x] 5.1 Run the full test suite (`dotnet test`) and confirm all tests pass
- [x] 5.2 Manually load `/LivePlay` and visually confirm statline row order within each unit card:
      Attached units first (in attachment order), then Bodyguard, Sergeant/leader entry first
      within each — confirmed via Firefox DevTools MCP: "Crusader Squad with High Marshal
      Helbrecht and Crusade Ancient" renders Helbrecht, Crusade Ancient, then Sword Brother /
      Initiate / Neophyte; "Assault Intercessor Squad" (plain Unit) renders Sergeant before troops
