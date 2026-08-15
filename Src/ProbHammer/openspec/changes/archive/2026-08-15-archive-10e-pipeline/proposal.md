## Why

The 11e domain model rewrite is about to touch `ArmyListParser` to target the 11e export
format. `.claude/domain-model-11e.md` already states the eventual plan is to rewire
`ArmyListParser`, `Web`, and `Simulation` onto the new domain model
(`ProbHammer.Core.Domain.Catalogue`/`Roster`) rather than patch the old
`Contracts`-based pipeline in place. A genuine 11e parser targets `Datasheet`/`Unit`/
`ModelLine`, not the old `ArmyList`/`UnitProfile` contracts — so starting that rewrite
orphans `Contracts/*`, `Catalogue/*`, `Enrichment/*`, and `Simulation/*` all at once,
plus the `Web` pages and DI wiring that depend on them. Leaving that code compiled and
wired while it silently stops matching reality would rot into a half-broken app rather
than a clean base for the rewrite. Moving it aside now, before `ArmyListParser` changes,
keeps it intact as reference material and gives the 11e work a clean compilation unit to
build in.

## What Changes

- **BREAKING**: `/Index`, `/ArmyView`, and `POST /api/simulate` are removed from the
  compiled Web app. The live 10th-edition simulator (paste two army lists, enrich
  against BSData 10e catalogues, run Monte Carlo simulations) stops working until a
  future change rebuilds it on the 11e domain model.
- `Contracts/UnitProfile.cs` + `ScalarValue.cs` + `Placeholder.cs`, all of `Catalogue/*`,
  `Enrichment/*`, and `Simulation/*` move from `src/ProbHammer.Core/` to
  `legacy/10e-pipeline/src/ProbHammer.Core/`, mirroring their original relative paths.
  Both `.csproj` files compile via the SDK's default glob rooted at each project's own
  folder, so moving files outside that tree drops them from compilation automatically —
  no `<Compile Remove>` needed (see design.md).
  `Parsing/ArmyListParser.cs`/`Placeholder.cs` are **not** part of this move — they stay
  in `src/ProbHammer.Core/Parsing/`, kept as the starting point for the 11e rewrite.
  `Contracts/ArmyList.cs` also stays, since it's `ArmyListParser.Parse()`'s own return
  type (see design.md for why `Contracts/` ends up split this way).
- The dependent `Web` pieces — `Pages/Index.cshtml(.cs)`, `Pages/ArmyView.cshtml(.cs)`,
  `Pages/Shared/_UnitCard.cshtml`, `Services/SimulationService.cs`,
  `Services/CatalogueStartupService.cs`, `Helpers/SessionJson.cs`, and
  `wwwroot/js/army-view.js` — move to `legacy/10e-pipeline/src/ProbHammer.Web/` the same
  way. `site.css` stays in place, unsplit (10e and `/LivePlay` rules are interleaved
  with no clean boundary — see design.md).
  `Program.cs` loses the DI registrations and endpoint mappings for `ArmyListParser`,
  `Enricher`, `CatalogueStore`/`ICatalogueFetcher`, `CombatSimulator`,
  `SimulationAdapter`, `ISimulationService`, the `CatalogueStartupService` hosted
  service, the `github` named `HttpClient`, and ASP.NET Session (all consumers of
  Session are moving) — plus the `/api/simulate`, `/api/refresh-catalogues`,
  `Index`, and `ArmyView` routes. `_Layout.cshtml` drops its now-dead
  `<script src="~/js/army-view.js">` tag.
- The matching test files — `tests/ProbHammer.Tests/Simulation/*` (6 files),
  `tests/ProbHammer.Tests/Catalogue/*` (`CatalogueParserTests.cs`,
  `CatalogueStoreTests.cs`), `tests/ProbHammer.Tests/Parsing/ArmyListParserTests.cs` and
  `ArmyListParserAndroidTests.cs` (the current 10e iOS/Android format assertions —
  `ArmyListParser.cs` itself is kept, only these format-specific test fixtures move),
  and `tests/ProbHammer.Tests/Fixtures/black-templars-sample.txt` +
  `death-guard.txt` (used only by those two Parsing test files) — move to
  `legacy/10e-pipeline/tests/ProbHammer.Tests/`, the same glob-based exclusion applying.
  No dedicated `Contracts`/`Enrichment` test files exist today (confirmed by search).
- `ProbHammer.Core.csproj` drops its now-unused `FuzzySharp` `PackageReference`
  (used only by the now-moved `Enricher.cs`).
- `.claude/domain-model.md`, `.claude/web-app.md`, `.claude/simulation-engine.md`,
  `.claude/bsdata-parsing.md`, `.claude/rules/combat-rules.md`, and `CLAUDE.md`'s
  Solution Structure section are updated to describe this code as archived reference
  material living under `legacy/10e-pipeline/`, not as the live implementation.
  `PROGRESS.md` gets an entry recording the move.

## Capabilities

### New Capabilities
None.

### Modified Capabilities
None — the 10e pipeline being archived predates this project's spec-tracked
capabilities (`attached-unit-tracker`, `datasheet-catalogue`, `dice-expression`,
`live-play-view`, `roster-model`); none of them describe `/Index`, `/ArmyView`,
`/api/simulate`, or the `Contracts`/`Catalogue`/`Enrichment`/`Simulation` code. No
spec-tracked requirement changes as a result of this move, so `skip_specs: true` is set
on this change.

## Impact

- **Removed from active compilation**: `src/ProbHammer.Core/Contracts/UnitProfile.cs`,
  `ScalarValue.cs`, `Placeholder.cs`; all of `Catalogue/`, `Enrichment/`, `Simulation/`;
  `src/ProbHammer.Web/Pages/Index.*`, `Pages/ArmyView.*`,
  `Pages/Shared/_UnitCard.cshtml`, `Services/SimulationService.cs`,
  `Services/CatalogueStartupService.cs`, `Helpers/SessionJson.cs`,
  `wwwroot/js/army-view.js`; the matching test files listed above.
- **Modified**: `src/ProbHammer.Web/Program.cs` (DI + middleware + route removals),
  `Pages/Shared/_Layout.cshtml` (drops the `army-view.js` script tag),
  `src/ProbHammer.Core/ProbHammer.Core.csproj` (drops the `FuzzySharp` package
  reference), six `.claude/*` docs, `CLAUDE.md`, `PROGRESS.md`. No other `.csproj`
  changes needed — moving files outside a project's directory is enough to exclude them.
- **New location**: `legacy/10e-pipeline/` (new top-level folder, no `.csproj`/`.sln` of
  its own, kept purely for human/agent reference — mirrors each moved file's original
  relative path under `src/`/`tests/`).
- **Not affected**: `ArmyListParser.cs` and `Contracts/ArmyList.cs` (both kept in place,
  unmodified), the entire 11e domain model (`Domain/Catalogue`, `Domain/Roster`,
  `Domain/Examples`), `/LivePlay` and its tests, `wwwroot/css/site.css` (stays in place
  unsplit — see design.md).
- **Downstream**: the currently-live 10e simulator UI (`/Index`, `/ArmyView`,
  `/api/simulate`, `/api/refresh-catalogues`) is gone from the running app until a
  future change rebuilds simulation on the 11e model — the app becomes
  `/LivePlay`-only.
