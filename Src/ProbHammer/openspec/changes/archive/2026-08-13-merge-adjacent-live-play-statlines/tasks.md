## 1. Prerequisite

- [x] 1.1 Confirm `order-statlines-by-declaration` is implemented and merged first — this change's
      `BuildStatlines` edits build directly on that change's rewrite

## 2. Domain: component name on statline entries

- [x] 2.1 Add `ComponentName` to `AggregateStatlineEntry` (`AttachedUnitAggregateView.cs`)
- [x] 2.2 Populate it in `AttachedUnitAggregator.BuildStatlines` from the owning component's
      `Datasheet.Name` (same convention as `WeaponContribution.ComponentName`)
- [x] 2.3 Add/update an `AttachedUnitAggregatorTests.cs` test asserting each entry reports the
      correct owning component's name for an AttachedUnit with a Bodyguard and an Attached unit

## 3. LivePlay page: adjacent-run grouping

- [x] 3.1 Add a page-local `StatlineBlockViewModel` wrapping a non-empty ordered list of
      `AggregateStatlineEntry` sharing one `ComponentName` and one `Statline` value
- [x] 3.2 In `LivePlayModel.BuildUnitBlock`, replace the flat `Statlines` pass-through with a
      single forward scan over `view.Statlines` that starts a new group whenever `ComponentName`
      or `Statline` differs from the current group's
- [x] 3.3 Change `UnitBlockViewModel.Statlines` from `IReadOnlyList<AggregateStatlineEntry>` to
      `IReadOnlyList<StatlineBlockViewModel>`
- [x] 3.4 Add a `LivePlayModel`-level test (or equivalent) asserting: adjacent same-component
      equal-valued entries group into one block; a differently-valued entry starts a new block; an
      adjacent equal-valued entry from a different component starts a new block

## 4. LivePlay page: markup

- [x] 4.1 Update the statline section in `LivePlay.cshtml` to render one shared stat-tile per
      `StatlineBlockViewModel`, with one header line per entry in `Entries` (name +
      remaining/initial count) stacked above it, each as its own distinct element
- [x] 4.2 Keep each entry's own nested loadout breakdown (`Loadouts.Count > 1`) rendered under its
      own header line, unaffected by whether the block has one entry or several

## 5. Documentation

- [x] 5.1 Update `.claude/domain-model-11e.md`: `ComponentName` on `AggregateStatlineEntry`
- [x] 5.2 Update `PROGRESS.md` once implementation lands, per this project's CLAUDE.md
      doc-maintenance rule

## 6. Verification

- [x] 6.1 Run the full test suite (`dotnet test`) and confirm all tests pass
- [x] 6.2 Manually load `/LivePlay` and visually confirm: Crusader Squad renders Sword Brother +
      Initiate under one shared stat-tile with Neophyte separate; Assault Intercessor Squad
      renders Sergeant + rank-and-file under one shared stat-tile (both share Sv3+/T4/etc.) —
      confirmed via Firefox DevTools MCP: both cards render exactly as expected, Initiate's own
      loadout breakdown nested under its own header line within the shared block
