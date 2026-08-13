## 1. Domain: expose source-type on the aggregate view

- [x] 1.1 Add `bool IsAttachedUnit` to the `AttachedUnitAggregateView` record
- [x] 1.2 Populate it in `AttachedUnitAggregator.Build` from `combatUnit is AttachedUnit`
- [x] 1.3 Add/update aggregator tests asserting `IsAttachedUnit` is `true` for an `AttachedUnit`
      source (including one with zero attached units) and `false` for a plain `Unit` source

## 2. LivePlay page: apply the ordering

- [x] 2.1 In `LivePlayModel.OnGet()`, sort `View.MyArmy()`'s result before mapping to
      `UnitBlockViewModel`: descending `IsAttachedUnit`, then descending total model count
      (`view.Statlines.Sum(s => s.InitialCount)`), then ascending `Name`
      (`StringComparer.OrdinalIgnoreCase`)
- [x] 2.2 Add a test exercising `LivePlayModel.OnGet()` directly (no DI dependencies — safe to
      instantiate and call `OnGet()`) asserting: attached-sourced blocks precede plain-unit
      blocks, larger units precede smaller ones within a group, and equal-sized units within a
      group are ordered by ascending name

## 3. Documentation

- [x] 3.1 Update `.claude/domain-model-11e.md` to document the new `IsAttachedUnit` field on
      `AttachedUnitAggregateView`
- [x] 3.2 Update `PROGRESS.md` once implementation lands, per this project's CLAUDE.md
      doc-maintenance rule

## 4. Verification

- [x] 4.1 Run the full test suite (`dotnet test`) and confirm all tests pass
- [x] 4.2 Manually load `/LivePlay` and visually confirm the new block order matches the example
      army's expected attached-first / largest-first ordering — confirmed via Firefox DevTools
      MCP: renders Helbrecht/Ancient, Marshal/Lieutenant, Sword Bretheren/Marshal (attached, 12,
      12, 5 models), then Assault Intercessor, Scout, Impulsor (plain, 5, 5, 1 models), matching
      the hand-computed expected order exactly
