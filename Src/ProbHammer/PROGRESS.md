# Work Journal

## Active Work

Nothing in progress.

---

## Recently Completed

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
