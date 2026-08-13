## 1. Domain: ICombatUnit.Name

- [x] 1.1 Add a computed `Name` member to `ICombatUnit`
- [x] 1.2 Implement `Unit.Name => Datasheet.Name`
- [x] 1.3 Implement `AttachedUnit.Name` as a humanized join of the Bodyguard's Datasheet name and
      the Attached units' Datasheet names: none → Bodyguard name alone; one → `"with {A}"`; two →
      `"with {A} and {B}"` (no comma); three or more → Oxford-comma join. No deduplication of
      repeated attached-leader names.
- [x] 1.4 Add domain tests covering 0/1/2/3+ attached units, and a duplicate-name case asserting
      the name is listed twice, unmerged

## 2. Domain: DiceExpression.ExpectedValue

- [x] 2.1 Add `DiceExpression.ExpectedValue()`: `Count == 0 ? Modifier : Count * (Sides + 1) / 2.0 + Modifier`
- [x] 2.2 Add tests: fixed value, `D6`, `D3`, `2D6+1`

## 3. Aggregate view: thread Name through

- [x] 3.1 Add `Name` to the `AttachedUnitAggregateView` record
- [x] 3.2 Populate it in `AttachedUnitAggregator.Build` from the source `ICombatUnit.Name`
- [x] 3.3 Add/update aggregator tests asserting `Name` is populated correctly for both a plain
      `Unit` and an `AttachedUnit` input

## 4. LivePlay page: rendering rewrite

- [x] 4.1 Update `LivePlayModel` view-model types as needed to carry `Name`, grouped/ordered
      weapons, and whatever shape the section-collapse markup requires
- [x] 4.2 Replace the "Unit N" header with `unit.Name`
- [x] 4.3 Rewrite statline rendering as one header+stat-tile block per `AggregateStatlineEntry`
      (never merged by matching value)
- [x] 4.4 Render the nested loadout breakdown only when `Loadouts.Count > 1`; fold the
      remaining/initial count into the block header otherwise
- [x] 4.5 Split `Weapons` into Ranged and Melee sections by `Profile.Type`, omitting a section
      entirely when it has no entries
- [x] 4.6 Order weapons within each section by descending `TotalAttacks.ExpectedValue()`
- [x] 4.7 Add a new inline bracketed ability-tag helper for `WeaponProfile`'s flattened ability
      properties (do not reuse `_UnitCard.cshtml`'s `WeaponAbilityText`, which targets the
      unrelated `Contracts.WeaponAbilities` type)
- [x] 4.8 Wrap each unit block's statline / ranged-weapons / melee-weapons / abilities sections in
      independent `<details>` elements, closed by default
- [x] 4.9 Keep Keywords rendering as a plain, non-collapsible footer line

## 5. Styling: GW-inspired theme scoped to /LivePlay

- [x] 5.1 Add a `.live-play-page` block re-declaring the seven design-token custom properties
      (`--bg`, `--bg2`, `--bg3`, `--accent`, `--text`, `--text-dim`, `--amber`, `--border`) with
      GW-inspired light values, local to that selector
- [x] 5.2 Give `.live-play-page` its own explicit `background: var(--bg)` (it currently has none
      and inherits `body`'s dark background)
- [x] 5.3 Style the name header bar, stat-tile blocks, and Ranged/Melee/Abilities section header
      bars per the GW reference layout
- [x] 5.4 Style inline ability tags and the `<details>`/`<summary>` disclosure chrome
- [x] 5.5 Use bold sans-serif typography for stat tiles and weapon numbers within
      `.live-play-page` — do not apply the app-wide monospace-for-stats rule here

## 6. Documentation

- [x] 6.1 Update `.claude/domain-model-11e.md` to document `ICombatUnit.Name` and
      `AttachedUnitAggregateView.Name`
- [x] 6.2 Add a scoped-exception note to `.claude/design-tokens.md` and/or `.claude/web-app.md`
      documenting that `/LivePlay` uses its own GW-inspired theme and typography, distinct from
      the shared dark tactical theme, and why
- [x] 6.3 Update `PROGRESS.md` once implementation lands, per this project's CLAUDE.md
      doc-maintenance rule

## 7. Verification

- [x] 7.1 Run the full test suite (`dotnet test`) — 233/233 passing
- [x] 7.2 Manually load `/LivePlay` and visually check against the GW reference screenshots: name
      headers, stat-tile blocks, Ranged/Melee split with expected-value ordering, inline ability
      tags, and sections collapsed by default — no browser tool was available this session; verified
      structurally instead (rendered HTML inspected via curl: correct names, all sections present,
      correct loadout-breakdown count, correct inline ability tags, CSS brace-balanced). Accepted
      as sufficient by the user in place of an actual visual pass.
