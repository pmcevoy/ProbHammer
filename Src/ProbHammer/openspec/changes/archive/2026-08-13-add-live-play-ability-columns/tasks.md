## 1. Domain: per-component ability aggregation

- [x] 1.1 Add `AggregateAbilityEntry(ComponentName, StatlineName?, Ability)` to
      `AttachedUnitAggregateView.cs`; remove the `UnitScopedAbilities`/`ModelScopedAbilities`
      fields from `AttachedUnitAggregateView` and replace with a single
      `Abilities: IReadOnlyList<AggregateAbilityEntry>`; remove the now-unused
      `ModelScopedAbilityEntry` record
- [x] 1.2 Replace `BuildUnitScopedAbilities`/`BuildModelScopedAbilities` with a single
      `BuildAbilities` walking components in `BuildStatlines`'s established display order: for
      each component where `IsPresent`, emit one entry per `Datasheet.Ability`
      (`StatlineName: null`), then for each of that component's own `ModelLine`s with
      `RemainingCount > 0`, emit one entry per that line's own `Abilities`
      (`StatlineName: modelLine.StatlineName`) — no cross-component combination, no deduplication
- [x] 1.3 Update `AttachedUnitAggregator.Build`'s call site to use `BuildAbilities`
- [x] 1.4 `dotnet build` — confirm no remaining reference to the removed fields/types (ProbHammer.Core
      builds clean; ProbHammer.Web's LivePlay.cshtml.cs still references the old
      ModelScopedAbilityEntry as expected, fixed in section 3)

## 2. Domain: tests

- [x] 2.1 Add `AttachedUnitAggregatorTests.cs` tests: a Datasheet-sourced ability is reported with
      a null `StatlineName`; a `ModelLine`-sourced ability is reported with the correct
      `StatlineName`; two components each having an ability of the same Name are reported as two
      separate entries, not combined; a `ModelLine`-sourced ability disappears once its bearer's
      remaining count reaches 0; a component's Datasheet-sourced abilities persist while any of
      its model-lines still have models remaining
- [x] 2.2 Grep for and migrate any existing test still referencing the removed
      `UnitScopedAbilities`/`ModelScopedAbilities` properties or the removed
      `ModelScopedAbilityEntry` type

## 3. LivePlay page: view-model

- [x] 3.1 Add `ModelAbilities`/`UnitAbilities: IReadOnlyList<Ability>` to
      `StatlineBlockViewModel`
- [x] 3.2 Add `ComponentAbilitySpanViewModel(int FirstRunIndex, int LastRunIndex,
      IReadOnlyList<Ability> ModelAbilities, IReadOnlyList<Ability> UnitAbilities)` and a new
      `UnitBlockViewModel.ComponentAbilitySpans: IReadOnlyList<ComponentAbilitySpanViewModel>`
- [x] 3.3 In `BuildUnitBlock`, after `GroupStatlines` produces the runs: match each
      `StatlineName`-bound ability entry against whichever run contains a
      same-`ComponentName`+`StatlineName` `AggregateStatlineEntry`, routing by `Ability.Scope`
      into that run's `ModelAbilities`/`UnitAbilities`; group the null-`StatlineName` entries by
      `ComponentName`, find each such component's first/last run index among `Statlines`, and
      build one `ComponentAbilitySpanViewModel` per component that has at least one
- [x] 3.4 Remove `ModelAbilityGroupViewModel` and `GroupModelScopedAbilities` (superseded)
- [x] 3.5 Add a `LivePlayModelTests.cs` test (mirroring the existing `BuildUnitBlock_...` grouping
      test) asserting: a row-bound ability appears only on its own run's `ModelAbilities`/
      `UnitAbilities`; a component-wide ability's span covers exactly `FirstRunIndex` through
      `LastRunIndex` for its component's runs; abilities route to the correct list by `Scope`
      regardless of source

## 4. LivePlay page: markup and CSS

- [x] 4.1 Restructure the Statline `<details>` section into a CSS Grid: STATLINES | MODEL
      ABILITIES | UNIT ABILITIES, with explicit `grid-row`/`grid-column` on every cell — row-bound
      ability cells placed at their run's row; component-wide ability cells spanning
      `grid-row: (FirstRunIndex+1) / (LastRunIndex+2)` (1-indexed, exclusive end)
- [x] 4.2 Render ability Name only (no Text) in both columns; stack multiple abilities within one
      cell vertically; component-wide cells align to the top of their span
- [x] 4.3 Remove the standalone "Abilities" `<details>` section and its Razor helper code
- [x] 4.4 Add the CSS Grid rules to `site.css` (`grid-template-columns`, cell placement); remove
      now-unused CSS from the old standalone Abilities section

## 5. Documentation

- [x] 5.1 Update `.claude/domain-model-11e.md`: `AttachedUnitAggregateView.Abilities`'s new shape
      (replacing `UnitScopedAbilities`/`ModelScopedAbilities`), and
      `AttachedUnitAggregator.BuildAbilities`'s per-component/row-binding rules
- [x] 5.2 Update `PROGRESS.md` once implementation lands, per this project's CLAUDE.md
      doc-maintenance rule

## 6. Verification

- [x] 6.1 Run the full test suite (`dotnet test`) and confirm all tests pass
- [x] 6.2 Manually load `/LivePlay` and visually confirm, on "Crusader Squad with High Marshal
      Helbrecht and Crusade Ancient" — confirmed via `firefox-devtools-mcp`: Model column shows
      `LEADER`/`High Marshal` beside Helbrecht's row and `SUPPORT` beside Crusade Ancient's row
      (correcting an assumption in this task's original text — `High Marshal` is `Scope: Model`,
      not Unit); Unit column shows `TEMPLAR VOWS`/`Crusade of Wrath` beside Helbrecht and
      `TEMPLAR VOWS`/`Vengeful Exhortation`/`Martial Honour` beside Crusade Ancient;
      `TEMPLAR VOWS`/`Righteous Zeal` visibly span the Bodyguard's rows (the merged Sword
      Brother/Initiate row through the separate Neophyte row) in the Unit column; the old
      standalone Abilities section no longer appears anywhere on the page
