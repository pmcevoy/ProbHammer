# Work Journal

## Active Work

Nothing in progress.

---

## Recently Completed

- OpenSpec change `add-live-play-weapon-provenance` implemented: weapon rows on `/LivePlay` now
  expand on click to show which `(ComponentName, StatlineName)` groups contributed how much,
  instead of only ever showing the aggregated total. Contributions sharing a group collapse to one
  row (`Name (Count×PerModelAttacks)`, product as the Attacks subtotal) when their
  `PerModelAttacks` agree; if they disagree — possible since `WeaponProfileEqualityKey` excludes
  Attacks by design, though no real fixture exercises it — each contributing `ModelLine` renders
  separately instead, so no subtotal is ever shown that isn't verifiable from the numbers beside
  it. Grouping/formatting (`LivePlayModel.BuildContributionBreakdown`) is page-layer view-model
  work over data `AttachedUnitAggregator` already computed correctly (`WeaponContribution`, added
  in `rework-attached-unit-aggregate-view` specifically for this), matching the page's existing
  `StatlineBlockViewModel` precedent — no domain-model change. This is `/LivePlay`'s first
  JavaScript (`live-play.js`, click-to-toggle): native `<details>` can't span additional `<tr>`s
  below the row that triggers it, so a small script toggles pre-rendered, hidden breakdown rows
  via a shared `data-weapon-id` attribute.
  **Refined after hands-on layout feedback, twice.** First: zebra striping broke, because
  `:nth-child(even)` counts the always-present-but-`[hidden]` breakdown rows too, silently shifting
  every later primary row's parity (confirmed live — a unit's 4th weapon row, which should read as
  even/grey, rendered white because three hidden rows ahead of it shifted its DOM position to odd).
  Fixed by driving `.weapon-row-alt` from each weapon's own list index instead of DOM position, and
  giving a breakdown row the same class as its parent so an expanded row reads as one continuous
  block rather than a colour gap; also increased the breakdown label's indent for a clearer
  nested-section look. Second, a genuine scope change: the trigger was originally per-weapon
  (`Contributions.Count > 1`), but using the page surfaced that a *single* contributor is still
  worth naming — e.g. a Pyre pistol carried only by a unit's Sword Brother — as long as the unit has
  other `ModelLine`s to distinguish it from. Trigger is now unit-level: `totalModelLinesInUnit > 1`,
  computed once via `view.Statlines.Sum(s => Math.Max(s.Loadouts.Count, 1))` (`Loadouts` is empty
  when exactly one `ModelLine` shares a statline name, so `Max(.,1)` recovers the true per-name
  count). This can only ever exclude a single-`ModelLine` plain `Unit` (e.g. Impulsor) — an
  `AttachedUnit` always totals two or more `ModelLine`s by construction (Bodyguard + at least one
  Attached component, each contributing at least one line), confirmed by a dedicated test asserting
  no weapon on Impulsor ever shows a breakdown. Visually verified via `firefox-devtools-mcp` against
  the real example army: the Crusader Squad's Bolt pistol (A10) expands to Neophyte (4×1), Initiate
  (5×1, collapsed across its two differently-loadout model-lines), and the attached Crusade
  Ancient's own Bolt Pistol (1×1) — a genuine cross-component merge, confirming
  `WeaponProfileEqualityKey`-equal weapons from different components do combine as designed; Pyre
  pistol expands to a single "Sword Brother (1×D6)" row; the Melee table's Astartes chainsword
  breakdown (Neophyte 4×4, chainsword-loadout Initiate 3×4, Power-fist-loadout Initiates correctly
  absent since they don't carry that weapon) confirmed the loadout-differentiation scenario
  discussed during design needed no special-casing. 253 tests, all passing. `.claude/vnext-ideas.md`
  updated: this item removed (now implemented), two new complementary ideas added — an
  ability/weapon-keyword definitions popup, and selection-scoped weapon aggregation (checkbox-driven
  filtering of the Statline section, deliberately sequenced after this change since it reuses the
  same grouping logic).
- OpenSpec change `add-live-play-ability-columns` implemented and archived: the Statline section
  on `/LivePlay` is now a 3-column CSS Grid — STATLINES | MODEL ABILITIES | UNIT ABILITIES —
  filling the previously-unused horizontal space beside each statline block and putting each
  ability directly beside the model(s) it depends on. `AttachedUnitAggregateView.Abilities`
  replaces the old `UnitScopedAbilities`/`ModelScopedAbilities` fields with a single per-component,
  optionally row-bound `AggregateAbilityEntry(ComponentName, StatlineName?, Ability)` list: `null`
  `StatlineName` means Datasheet-sourced (renders beside the first statline row of its component,
  visually spanning every row belonging to that component via CSS Grid's `grid-row` span — not an
  HTML `<table>`/`rowspan`, since the page's existing `<div>`-based cards and rich nested statline
  content don't fit a literal table); a set `StatlineName` means ModelLine-sourced, Enhancement-
  conferred (renders beside that one row only). Column placement is decided purely by
  `Ability.Scope`; row placement purely by ability source — this also surfaces a previously-hidden
  gap, since Datasheet-level `Scope: Model` abilities (`LEADER`, `SUPPORT`) were never rendered
  anywhere before this change. Ability rendering is name-only for now; full-text-on-demand is
  explicitly deferred. The old standalone "Abilities" collapsible section is removed. Visually
  verified via `firefox-devtools-mcp` against the real example army, including the rowspan effect
  spanning a merged statline run through a separate one. 246 tests, all passing.
- OpenSpec change `merge-adjacent-live-play-statlines` implemented and archived: on `/LivePlay`,
  adjacent statline rows within the same component that share an identical Statline value
  (M/T/Sv/W/Ld/Oc, and InSv when present) now render under one shared stat-tile, with each
  contributing entry's own name/count kept as its own header line above it (and its own nested
  loadout breakdown, unaffected by sharing a tile). The merge is adjacency-scoped (only a
  contiguous run in the already-established component/declared-order sequence) and never crosses a
  component boundary, even when values happen to match — this is a rendering-only grouping, not a
  data-level merge; the underlying `AggregateStatlineEntry` list is unchanged, so per-name
  remaining/initial counts and future casualty tracking stay independently addressable per entry.
  `AggregateStatlineEntry` gained a `ComponentName` field (same convention as
  `WeaponContribution.ComponentName`), populated in `AttachedUnitAggregator.BuildStatlines`. The
  grouping itself (a single forward scan starting a new run whenever `ComponentName` or `Statline`
  differs from the current run) lives entirely at the page layer in a new `StatlineBlockViewModel`,
  matching this page's established precedent of rendering-only decisions living in
  `LivePlayModel`. `BuildUnitBlock` was made `internal` (with `InternalsVisibleTo` for
  `ProbHammer.Tests`) so a hand-built `AttachedUnitAggregateView` could exercise a
  same-component/equal-value merge, a value-change split, and a component-boundary split in one
  precise unit test — the real example army doesn't naturally contain the last case adjacently.
  Visually verified via `firefox-devtools-mcp`: Crusader Squad shows Sword Brother + Initiate under
  one shared tile with Neophyte separate; Assault Intercessor Squad shows Sergeant + rank-and-file
  under one shared tile. 243 tests, all passing.
- OpenSpec change `order-live-play-unit-cards` implemented and archived: `/LivePlay` unit-card
  blocks now render attached-sourced units (`AttachedUnit` source) before plain-`Unit`-sourced
  ones; within each group, descending total model count (Bodyguard + every Attached unit's models
  for an AttachedUnit), ties broken by ascending `Name` (ordinal, case-insensitive). An
  `AttachedUnit` with zero attached units still counts as attached-sourced, since the rule keys on
  source type, not rendered content. `AttachedUnitAggregateView` gained a new `IsAttachedUnit`
  field (`combatUnit is AttachedUnit`, set in `AttachedUnitAggregator.Build`) so the page layer can
  tell the two source types apart — the sort itself happens in `LivePlayModel.OnGet()`, matching
  the existing precedent of weapon ordering being a page-layer concern. `ProbHammer.Tests` gained
  its first `ProbHammer.Web` project reference and a `Web/LivePlayModelTests.cs` exercising
  `LivePlayModel.OnGet()` directly against the real example army (no DI needed) — the exact
  expected order was hand-computed from `Examples/Units.cs`'s model counts and confirmed both by
  the test and visually via `firefox-devtools-mcp`. 241 tests, all passing.
- OpenSpec change `order-statlines-by-declaration` implemented and archived: `Datasheet.Statlines`
  changed from `IReadOnlyDictionary<string, Statline>` to an ordered
  `IReadOnlyList<(string Name, Statline Statline)>` — declaration order (Sergeant/leader-equivalent
  entry first, matching the real NewRecruit/GW app export convention) is now a documented,
  load-bearing property instead of an accident of `Dictionary` enumeration. `GetStatline(name)`
  stays O(1), backed by an internal dictionary built once in the constructor. All 9 sites in
  `Examples/Datasheets.cs` and 12 test-fixture sites migrated (all already declared Sergeant/leader
  first, so no reordering was needed beyond the syntax change). `AttachedUnitAggregator
  .BuildStatlines` rewritten to walk components in display order (an `AttachedUnit`'s `Attached`
  list, then `Bodyguard`; a plain `Unit` is just itself) and, within each, that component's
  Datasheet-declared statline order — matching each declared name only against that component's
  own `ModelLine`s. **Behavior change**: per-name statline merging is now scoped to a single
  component, not merged globally across an `AttachedUnit` as before; confirmed acceptable since
  real 40k attachment rules can't produce two identically-named Leader/Support units on one
  Bodyguard. Visually verified via `firefox-devtools-mcp`: an AttachedUnit card renders Attached
  units before its Bodyguard, sergeant-first within each; a plain Unit card renders its Sergeant
  statline first. 237 tests, all passing.
- OpenSpec change `drop-melee-range-column` implemented and archived: the Melee Weapons table on
  `/LivePlay` no longer shows a Range column (always read "Melee", pure noise). Two further
  `/LivePlay` display tweaks landed alongside it (no spec impact — pure CSS/markup): the
  invulnerable save now renders as a smaller sub-line nested under the Sv stat-tile instead of its
  own separate tile (`.stat-subvalue`, matching GW card style more closely), and both weapon
  tables now use `table-layout: fixed` with a shared `<colgroup>` column-width scheme (`.col-weapon`
  / `.col-weapon-melee` / `.col-slot`) so the two independent `<table>` elements' shared columns
  stay pixel-aligned regardless of each table's own content-driven width. All three fixes were
  visually verified against a real Firefox instance via the `firefox-devtools-mcp` MCP server
  (Mozilla's official Firefox DevTools MCP) — Melee Weapons shows no Rng column, the High Marshal
  Helbrecht statline's Sv tile shows a nested `4++` under `2+`, and the Assault Intercessor Squad
  card's Ranged/Melee weapon-table columns (A/S/AP/D) align across both tables.
- OpenSpec change `live-play-gw-layout` implemented (all 28 tasks): `/LivePlay` rewritten around a
  GW-datasheet-inspired light theme, scoped entirely to that page via locally-redeclared CSS
  tokens (`.claude/design-tokens.md` now documents this as an explicit exception). `ICombatUnit`
  gained a computed `Name` (`Unit.Name` from its Datasheet; `AttachedUnit.Name` a humanized join of
  Bodyguard + Attached Datasheet names, Oxford-comma at 3+), threaded through
  `AttachedUnitAggregateView.Name` and rendered as each unit block's header, replacing the old
  "Unit N" ordinal. Statlines render as one header+stat-tile block per `AggregateStatlineEntry`
  (GW mobile-app pattern, never merged by matching value — reinforces the existing no-merge rule
  from `render-aggregate-view`). Weapons split into Ranged/Melee sections ordered by descending
  `DiceExpression.ExpectedValue()` (new helper, added because `DiceExpression` has no natural total
  order for sorting); weapon ability flags render as inline bracketed tags instead of a separate
  column. Each unit block's statline/ranged/melee/abilities sections are independent `<details>`,
  collapsed by default. Still read-only, still rebuilt fresh from `Examples.View.MyArmy()` per
  request — no session/persistence added. Explicitly deferred to a future change: phase-level
  section toggling, Core/Faction/Psychic ability source tagging, per-`ModelLine` keywords (needed
  for a leader's personal keyword to leave the unit when that model is removed), a multi-profile-
  weapon disclaimer, and live casualty mutation itself. Docs: `.claude/domain-model-11e.md` and
  `.claude/design-tokens.md` updated.
- OpenSpec change `promote-dice-expression-to-domain` implemented (all 19 tasks): `DiceExpression`
  moved from `ProbHammer.Core.Simulation` into `ProbHammer.Core.Domain.Catalogue` — it's a
  game-rules value type (`WeaponProfile.A`/`D` already depended on it), not a simulation-owned
  one. `Simulation/*` (`SimulationAdapter`, `CombatSimulator`, `SimWeaponProfile`, `IDiceRoller`,
  plus their test doubles/tests) were repointed via `using` statements only — no behavior change;
  `Simulation/*` remains paused/not wired to the 11e domain model, per `.claude/domain-model-11e.md`.
  Added an implicit `int -> DiceExpression` conversion, `D3`/`D6` static presets, and an
  `operator +(DiceExpression, int)` so fixed weapon stats can be written as plain integers and
  dice notation reads close to game shorthand (`DiceExpression.D6 + 2`), rather than needing
  `DiceExpression.Fixed(n)` wrapping at every call site or a combinatorial set of constructor
  overloads on `RangedWeapon`/`MeleeWeapon`. **Scope addition made mid-implementation** (agreed
  with the user after discovering it during apply): `WeaponProfile.D` (Damage) was also widened
  from `int` to `DiceExpression`, matching `A` — this had been started but abandoned mid-way in
  the prior commit (a `//NOTE: D needs to be able to accept "D6"` comment on a `Fixed(6)`
  placeholder for the Multi-melta), and completing it was necessary to actually fix that weapon's
  damage. `BlackCrusade.cs` and `WeaponFixtures.cs` simplified to drop now-unnecessary
  `DiceExpression.Fixed(...)` wrapping. `DiceExpression` already had a dedicated test suite
  (`tests/.../Simulation/DiceExpressionTests.cs` — the proposal incorrectly claimed it had none);
  relocated it to `tests/.../Domain/DiceExpressionTests.cs` and extended it with coverage for the
  three new additions. Also fixed one pre-existing test bug the build surfaced: `DatasheetTests.cs`
  asserted `profile.A.Should().Be(4)` against a `DiceExpression`-typed property using a bare `int`
  literal — this only started failing at runtime (rather than silently not compiling) once the new
  implicit conversion made it compile; fixed to assert against `DiceExpression.Fixed(...)`
  explicitly. 214 tests, all passing.
- Refinement pass on the `attached-unit-domain-model` change (commits `3cfdfc0`..`d8c53c0`, all
  tasks already closed — no scope change): `WeaponProfile` reshaped to an abstract base with
  sealed `RangedWeapon`/`MeleeWeapon` subtypes (`WeaponAbilities` removed, ability fields
  flattened directly onto `WeaponProfile`); `Skill` is now an abstract *computed* property
  (`RangedWeapon.Skill => Bs`, `MeleeWeapon.Skill => Ws`) rather than a stored value, avoiding a
  shadow-property desync under `with` expressions. `Datasheet`'s constructor now takes
  `weaponProfiles` as `IEnumerable<WeaponProfile>` (keyed internally by `.Name`) instead of a
  caller-built dictionary, removing a key/name-drift bug class found when `WeaponFixtures` was
  introduced. New fixture/test coverage added for two differently-named statlines with identical
  values (`Assault Intercessor` / `Assault Intercessor Sergeant`), proving
  `AttachedUnitAggregator` dedupes statlines by name, not by value. 35 domain tests, all passing.
  Doc: `.claude/domain-model-11e.md` updated to match.
- OpenSpec change `attached-unit-domain-model` implemented (all 28 tasks): new 11th-edition
  domain model under `ProbHammer.Core.Domain.Catalogue` / `ProbHammer.Core.Domain.Roster` —
  `Datasheet` (named statlines, on-demand weapon-profile resolution), `Ability` (`Scope:
  Model|Unit`), `Unit`/`AttachedUnit` (Composite-pattern `ICombatUnit` abstraction), live
  keyword-union and Toughness-resolution rules, per-model-line remaining-count tracking, and
  the Attached Unit aggregate view (`AttachedUnitAggregator`). Proven against hand-built
  fixtures only (`tests/ProbHammer.Tests/Domain/Fixtures/`) — no BSData or export-parser
  wiring. 35 new tests, 207 total, all passing. Doc: `.claude/domain-model-11e.md`.
  **`Contracts/*`, `Catalogue/*`, and `Enrichment/Enricher.cs` are now superseded** by this
  new domain model — they remain in the tree, untouched, only because nothing has rewired
  `ArmyListParser`, `Web`, or `Simulation` onto it yet. **`Simulation/*` is paused** for the
  same reason (its one-flat-Toughness-per-defender assumption doesn't fit Attached Units or
  multi-statline datasheets). A later change is expected to rewire all three onto the new
  model; until then all four old areas keep working exactly as before.
- AP sign convention unified across the sim engine. `SimWeaponProfile.Ap` is now a negative
  integer matching the game value (AP-2 → -2). `AbilityProcessor.EffectiveSave` and
  `CombatSimulator` both use `save - ap`. `SimulationAdapter` passes AP through unchanged
  (removed the negation and `Math.Max(0,...)` guard). All tests updated; 2 new Gherkin tests
  added for the AP-2 → 5+ save boundary. 172 tests, all passing.
- Sub-ability rendering implemented in `_UnitCard.cshtml`.
  Both Abilities and While Leading loops now detect `•` in `ability.Text`,
  split on `\n`, and render sub-ability lines as `.sub-ability` divs with
  `.sub-ability-name` / `.sub-ability-text` spans. Flat abilities unchanged.
  Three CSS rules added to `site.css`. Build: 0 errors, 0 warnings.
- Sub-ability profile parsing implemented in `CatalogueParser.ParseProfiles`.
  Two-pass approach: Pass 1 collects standard types; Pass 2 collects non-standard
  typeNames as sub-ability groups, merging into matching parent abilities or creating
  new ones. 4 new tests added; 170 total, all passing.
- Regeneration v1 complete. Generated UI near-identical to original.
  No spec files were modified during generation — all gaps were minor.
- Sub-ability spec written and merged into `.claude/` docs.

---

## Known Issues

Nothing outstanding.

---

## Spec Debt

Nothing outstanding.

> Keep this section honest. If you change code without updating the
> corresponding spec file, log it here immediately. A growing list
> means the spec is drifting from reality.

---

## Next Session Prompt

> Paste this at the start of the next Claude Code session:

"Read CLAUDE.md and all files in .claude/. Then read PROGRESS.md for current state."
