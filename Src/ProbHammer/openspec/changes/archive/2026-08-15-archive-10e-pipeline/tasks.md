## 1. Set up the archive location

- [x] 1.1 Create `legacy/10e-pipeline/` at the repo root (no `.csproj`/`.sln`).

## 2. Move `ProbHammer.Core` files

- [x] 2.1 Move `Catalogue/` (`CatalogueEntry.cs`, `CatalogueFetcher.cs`,
      `CatalogueParser.cs`, `CatalogueStore.cs`, `ICatalogueFetcher.cs`,
      `Placeholder.cs`) to `legacy/10e-pipeline/src/ProbHammer.Core/Catalogue/`.
- [x] 2.2 Move `Enrichment/` (`Enricher.cs`, `Placeholder.cs`) to
      `legacy/10e-pipeline/src/ProbHammer.Core/Enrichment/`.
- [x] 2.3 Move `Simulation/` (all 13 files) to
      `legacy/10e-pipeline/src/ProbHammer.Core/Simulation/`.
- [x] 2.4 Move `Contracts/UnitProfile.cs`, `Contracts/ScalarValue.cs`, and
      `Contracts/Placeholder.cs` to `legacy/10e-pipeline/src/ProbHammer.Core/Contracts/`.
      Leave `Contracts/ArmyList.cs` in place in `src/ProbHammer.Core/Contracts/`
      (see design.md - it's `ArmyListParser.cs`'s own return type).
- [x] 2.5 Remove the `FuzzySharp` `PackageReference` from `ProbHammer.Core.csproj`
      (now unused - was only referenced from the moved `Enricher.cs`). Leave
      `YamlDotNet` alone (unused before this change, unrelated to it).

## 3. Move `ProbHammer.Web` files

- [x] 3.1 Move `Pages/Index.cshtml` and `Pages/Index.cshtml.cs` to
      `legacy/10e-pipeline/src/ProbHammer.Web/Pages/`.
- [x] 3.2 Move `Pages/ArmyView.cshtml` and `Pages/ArmyView.cshtml.cs` to
      `legacy/10e-pipeline/src/ProbHammer.Web/Pages/`.
- [x] 3.3 Move `Pages/Shared/_UnitCard.cshtml` to
      `legacy/10e-pipeline/src/ProbHammer.Web/Pages/Shared/`.
- [x] 3.4 Move `Services/SimulationService.cs` and
      `Services/CatalogueStartupService.cs` to
      `legacy/10e-pipeline/src/ProbHammer.Web/Services/`.
- [x] 3.5 Move `Helpers/SessionJson.cs` to
      `legacy/10e-pipeline/src/ProbHammer.Web/Helpers/`.
- [x] 3.6 Move `wwwroot/js/army-view.js` to
      `legacy/10e-pipeline/src/ProbHammer.Web/wwwroot/js/`.
      Leave `wwwroot/css/site.css` in place, unsplit (see design.md).
- [x] 3.7 Remove the `<script src="~/js/army-view.js" ...>` line from
      `Pages/Shared/_Layout.cshtml`.

## 4. Update `Program.cs`

- [x] 4.1 Remove the cache-path resolution block and the
      `builder.Services.AddHttpClient("github", ...)` registration (used only by
      the now-moved `CatalogueFetcher`).
- [x] 4.2 Remove the DI registrations for `ICatalogueFetcher`/`CatalogueFetcher`,
      `CatalogueStore`, `ArmyListParser`, `Enricher`, `IDiceRoller`/`DiceRoller`,
      `CombatSimulator`, `SimulationAdapter`, `ISimulationService`/`SimulationService`.
- [x] 4.3 Remove `builder.Services.AddHostedService<CatalogueStartupService>()`.
- [x] 4.4 Remove `builder.Services.AddDistributedMemoryCache()`,
      `builder.Services.AddSession(...)`, and `app.UseSession()` (no remaining
      consumer of `HttpContext.Session` once `Index`/`ArmyView`/`SimulationService`
      are gone - confirmed in design.md).
- [x] 4.5 Remove the `app.MapPost("/api/simulate", ...)` and
      `app.MapPost("/api/refresh-catalogues", ...)` endpoint mappings.
- [x] 4.6 Remove the now-unused `using` directives (`ProbHammer.Core.Catalogue`,
      `ProbHammer.Core.Contracts` if no longer referenced, `ProbHammer.Core.Enrichment`,
      `ProbHammer.Core.Parsing` if no longer referenced, `ProbHammer.Core.Simulation`,
      `ProbHammer.Web.Pages` if no longer referenced, `ProbHammer.Web.Services` if no
      longer referenced) - check each against what `Program.cs` still uses for
      `/LivePlay` and `/api/live-play/casualties` before removing.
      (Also dropped the now-empty `using ProbHammer.Web.Helpers;` and
      `using System.Text.Json;`, beyond what task 4.6 originally listed - both had
      zero remaining references once the `/api/refresh-catalogues` handler and
      `SessionJson.cs` moved.)

## 5. Move test files

- [x] 5.1 Move `tests/ProbHammer.Tests/Simulation/` (all 6 files) to
      `legacy/10e-pipeline/tests/ProbHammer.Tests/Simulation/`.
- [x] 5.2 Move `tests/ProbHammer.Tests/Catalogue/CatalogueParserTests.cs` and
      `CatalogueStoreTests.cs` to
      `legacy/10e-pipeline/tests/ProbHammer.Tests/Catalogue/`.
- [x] 5.3 Move `tests/ProbHammer.Tests/Parsing/ArmyListParserTests.cs` and
      `ArmyListParserAndroidTests.cs` to
      `legacy/10e-pipeline/tests/ProbHammer.Tests/Parsing/`.
      (No `tests/ProbHammer.Tests/Parsing/Placeholder.cs` existed - nothing to leave
      behind beyond the kept `src/ProbHammer.Core/Parsing/ArmyListParser.cs`.)
- [x] 5.4 Move `tests/ProbHammer.Tests/Fixtures/black-templars-sample.txt` and
      `death-guard.txt` to `legacy/10e-pipeline/tests/ProbHammer.Tests/Fixtures/`
      (confirmed as consumed only by the two Parsing test files moved in 5.3).
      (Emptied `src/ProbHammer.Core/{Catalogue,Enrichment,Simulation}`,
      `src/ProbHammer.Web/Helpers`, and the four emptied `tests/ProbHammer.Tests/*`
      folders were also removed as a tidiness step, beyond what tasks.md listed -
      git doesn't track empty directories, so this has no effect on compilation.)

## 6. Verify

- [x] 6.1 `dotnet build` the solution (or all three active `.csproj` files) - confirm
      zero errors and no reference to anything under `legacy/`.
      Result: Build succeeded, 0 Warning(s), 0 Error(s).
- [x] 6.2 `dotnet test` - confirm the suite runs (fewer tests than before, matching the
      moved files) with zero failures.
      Result: Passed! Failed: 0, Passed: 126, Skipped: 0, Total: 126 (down from 276
      before this change, consistent with the moved Simulation/Catalogue/Parsing
      test files).
- [x] 6.3 Run the app locally and confirm `/LivePlay` still renders and its casualty
      controls still work (manual check or via `firefox-devtools-mcp`, matching this
      project's established verification pattern for `/LivePlay` changes). Confirm
      `/Index` and `/ArmyView` now 404 or redirect as expected rather than crashing.
      Result: verified via `dotnet run` + `curl` (the `firefox-devtools-mcp` MCP
      server was not connected in this session, so used a direct HTTP smoke test
      instead). `/LivePlay` → 200, renders real content (`Crusader Squad`
      confirmed in the body). `POST /api/live-play/casualties` round-tripped a
      casualty adjustment (`Neophyte` 4/4 → 2/4 → reverted to 4/4), confirming the
      endpoint still functions end to end. `/Index`, `/ArmyView`, and `/` (Index's
      former root route) → 404 as expected, not a crash.

## 7. Update documentation

- [x] 7.1 Update `.claude/domain-model.md` to state the 10th-edition shape it
      describes now lives at `legacy/10e-pipeline/src/ProbHammer.Core/` as reference
      material, not the live implementation.
- [x] 7.2 Update `.claude/web-app.md` the same way for `/Index`, `/ArmyView`, and their
      session-storage section.
      (Added a top-level banner note rather than rewriting every section - matches
      `domain-model.md`'s existing pattern for flagging superseded-but-retained content.
      Also points readers at `openspec/specs/live-play-view/spec.md` as where
      `/LivePlay`'s actual behavior is now tracked, since this file never described
      `/LivePlay` in the first place.)
- [x] 7.3 Update `.claude/simulation-engine.md` the same way for `Simulation/*`.
- [x] 7.4 Update `.claude/bsdata-parsing.md` the same way for `Catalogue/*`.
- [x] 7.5 Update `.claude/rules/combat-rules.md` to note it now documents archived
      reference behavior (the rules themselves are unchanged - only their
      implementation location moved).
- [x] 7.6 Update `CLAUDE.md`'s Solution Structure section to show `legacy/` alongside
      `ProbHammer.Web`/`ProbHammer.Core`/`ProbHammer.Tests`, and note `/Index`+
      `/ArmyView` are no longer live routes.
      (Also added a banner to CLAUDE.md's "User Flow" section, beyond what this task
      originally scoped - that section narrates the dead Index/ArmyView flow in detail
      and CLAUDE.md is always-loaded context, so leaving it unmarked would have been
      actively misleading in the one doc every session reads first.)
- [x] 7.7 Add a `PROGRESS.md` entry recording this move (what moved, why, and the
      `Contracts`/`site.css` split decisions from design.md), per this project's
      per-change documentation convention.
- [x] 7.8 Add an entry to `.claude/vnext-ideas.md` noting `site.css`'s 10e/LivePlay
      split as a deferred follow-up (per design.md's Non-Goals).
      (Also removed the pre-existing "Markup convergence with `_UnitCard.cshtml`" idea
      from the same file - it envisioned unifying `/LivePlay` markup with `ArmyView`'s
      unit cards, which no longer exist in the live app after this change, so the idea
      is moot rather than merely superseded.)
