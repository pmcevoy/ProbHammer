# Work Journal

## Active Work

Nothing in progress.

---

## Recently Completed

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
