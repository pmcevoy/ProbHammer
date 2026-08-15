## Context

See proposal.md - Why/What Changes for motivation and the file-level move list. This
document covers the mechanics of the move and the handful of places the code doesn't
split cleanly along the `src/` vs `legacy/` line.

Confirmed by reading the actual files before writing this design (not assumed from
docs):
- Both `src/ProbHammer.Core/ProbHammer.Core.csproj` and `src/ProbHammer.Web/ProbHammer.Web.csproj`
  are default SDK-style projects with **no explicit `<Compile Include>` list** - C# files
  compile via the SDK's default glob (`**/*.cs` relative to the project file's own
  directory), and Razor Pages compile the same way relative to `ProbHammer.Web.csproj`.
- `Contracts/ArmyList.cs` (the `ArmyList`/`UnitEntry`/`ModelEntry`/`WeaponEntry` records)
  has no dependency on `Contracts/UnitProfile.cs` or `ScalarValue.cs`, and is the direct
  return type of `ArmyListParser.Parse()`.
- `ASP.NET Session` (`AddDistributedMemoryCache`, `AddSession`, `UseSession`,
  `HttpContext.Session`) is used exclusively by `Index`/`ArmyView`/`SimulationService`
  (the `attacker_army`/`defender_army`/`used_catalogue_ids` keys). `/LivePlay` and its
  casualty endpoint never touch `HttpContext.Session` - their persistence is
  client-`localStorage` + a full-map POST that rebuilds the roster server-side each
  time, per `.claude/domain-model-11e.md`.
- `FuzzySharp` is referenced only from `Enrichment/Enricher.cs`. `YamlDotNet` is
  referenced in `ProbHammer.Core.csproj` but used nowhere in `src/` today - it already
  predates this change and is unrelated to it.
- `wwwroot/js/army-view.js` is 10e-only (ArmyView weapon-selection/combat-panel/pipeline
  UI); `wwwroot/css/site.css` is a single 895-line file with 10e-only, `/LivePlay`-only
  (`.live-play-page`-scoped), and shared base rules interleaved with no clean physical
  section boundary.
- No dedicated `Contracts`/`Enrichment` test files exist (`tests/ProbHammer.Tests` has no
  `Contracts/` or `Enrichment/` folder) - the domain-model.md's documented "integration
  test" and "snapshot tests" for the old pipeline were apparently never actually
  implemented. Nothing to move there beyond what's listed below.

## Goals / Non-Goals

**Goals:**
- Every file that moves stops compiling/running as part of `ProbHammer.Core`,
  `ProbHammer.Web`, or `ProbHammer.Tests` - `dotnet build`/`dotnet test` on the three
  active projects touches none of it.
- `ArmyListParser.cs` keeps compiling and behaving exactly as it does today after the
  move (this change makes zero behavior change to it).
- `/LivePlay` keeps working exactly as it does today, including its visual styling.
- The archived code stays readable in place - a future session (or this one, later) can
  open `legacy/10e-pipeline/...` and read working, syntactically valid C#/Razor without
  needing `git log`/`git show`.

**Non-Goals:**
- Splitting `site.css` into a "10e portion" and a "LivePlay portion." No clean boundary
  exists today; attempting the split risks a visual regression on the one page that
  still matters, for a change whose entire point is archival safety, not a redesign.
  `site.css` stays in place, unsplit - see Decisions.
- Retiring `Contracts/ArmyList.cs`. It's `ArmyListParser.cs`'s own return type and
  `ArmyListParser.cs` is explicitly untouched by this change - see Decisions.
- Cleaning up `appsettings.json`'s now-meaningless `Enricher:CachePath` key, or the
  pre-existing unused `YamlDotNet` package reference. Neither is caused by this change;
  touching them risks unrelated collateral edits for a change meant to be a mechanical
  move.
- Any change to `ArmyListParser.cs`'s parsing logic, output shape, or test expectations
  for the 11e export format. That's the follow-on change this one is clearing space for.

## Decisions

**No `<Compile Remove>` needed - physically move files outside each project's directory
tree.** The proposal's draft language mentioned an explicit `<Compile Remove>`; having
now read both `.csproj` files, that's unnecessary complexity. Because compilation is
driven by the SDK's default glob rooted at the project file's own folder, moving a file
to `legacy/10e-pipeline/...` (outside `src/ProbHammer.Core/` and `src/ProbHammer.Web/`
entirely) removes it from that glob automatically - zero `.csproj` edits required for
exclusion itself. The only `.csproj` edit in this change is removing the now-unused
`FuzzySharp` `PackageReference` from `ProbHammer.Core.csproj` (a real, change-caused
cleanup, not a mechanical-move necessity).

**`legacy/10e-pipeline/` mirrors each file's full original relative path**, e.g.
`legacy/10e-pipeline/src/ProbHammer.Core/Simulation/CombatSimulator.cs`,
`legacy/10e-pipeline/tests/ProbHammer.Tests/Parsing/ArmyListParserTests.cs` - not
flattened or reorganized by project name alone. This means anyone opening the archive
later sees exactly the same relative layout the live tree used, so cross-file
`using`/relative-path reasoning transfers directly if the code is ever revived, and a
`diff` against a pre-move git revision stays meaningful path-for-path.

**`Contracts/ArmyList.cs` stays in `src/ProbHammer.Core/Contracts/`; `UnitProfile.cs`,
`ScalarValue.cs`, and `Placeholder.cs` move.** `ArmyListParser.cs` is explicitly kept in
place, unmodified, as the starting point for the 11e rewrite (per proposal.md), and it
directly returns `Contracts.ArmyList` - moving that file out from under it would break
compilation of code this change is supposed to leave untouched. `UnitProfile.cs` (and
its `ScalarValue` dependency) is exclusively the Enricher-output/Simulation-input/
session-serialization contract - nothing that stays behind references it. This leaves
`Contracts/` as a deliberately asymmetric, partially-archived namespace for now; a
future change that actually changes `ArmyListParser`'s output shape for the 11e format
can retire the remaining `ArmyList.cs` at that point, not this one.

**`army-view.js` moves wholesale; its `<script>` tag is removed from `_Layout.cshtml`.**
Unlike `site.css`, this file has no shared/LivePlay content mixed in - it's 100% ArmyView
weapon-selection/combat-panel/pipeline-display logic. Leaving the `<script>` tag in
`_Layout.cshtml` after the file moves would 404 on every page load (including
`/LivePlay`); leaving the file in place but referencing nothing would be the same kind
of half-archived residue this change exists to avoid. Removing the tag is the one
`_Layout.cshtml` edit this change makes.

**`site.css` stays in `src/ProbHammer.Web/wwwroot/css/` untouched.** Confirmed by reading
the file: 10e-only rules (e.g. `.army-view`), `/LivePlay`-only rules
(`.live-play-page`-prefixed selectors), and shared base tokens are interleaved
throughout its 895 lines with no contiguous section boundary. A dead CSS selector that
never matches any element (once `ArmyView.cshtml`/`Index.cshtml` markup is gone) causes
zero functional harm, unlike dead C# sitting in a compiled project - so there is no
correctness reason to force the split now. Attempting it anyway risks a real visual
regression on `/LivePlay`, the only page still in active use, for a change whose goal is
safety, not tidiness. Flagged as a candidate for `.claude/vnext-ideas.md` instead of
attempted here.

**`AddDistributedMemoryCache()`/`AddSession()`/`UseSession()` are removed from
`Program.cs`, along with the `AddHttpClient("github", ...)` named client and the cache-
path resolution block.** All three are exclusively consumed by code that's moving
(Session by Index/ArmyView/SimulationService; the named HTTP client by
`CatalogueFetcher`). Leaving unused middleware/DI wiring registered is exactly the
half-cleaned state the proposal's Why section calls out as the thing to avoid.

## Risks / Trade-offs

- **[Risk] A file consumed by the archived code but not identified during this design
  survives only partially (compiles-in-`src/` half moved, reference-only half left
  behind), silently breaking the one thing this change promises to keep working
  (`/LivePlay` + `ArmyListParser.cs`).** → Mitigation: `tasks.md` includes an explicit
  `dotnet build` and `dotnet test` pass on the three active projects as its last step,
  after all moves - a stray reference to a moved type fails the build immediately rather
  than silently.
- **[Risk] `legacy/10e-pipeline/` accidentally gets picked up by a future `dotnet build`
  or `dotnet test` invocation run from the repo root** (e.g. a wildcard/solution-wide
  command) if a `.sln` or build script globs directories rather than listing `.csproj`
  files explicitly. → Mitigation: `legacy/` contains no `.csproj`/`.sln` of its own, so
  standard `dotnet build`/`dotnet test` (which operate on project/solution files, not
  arbitrary directories) cannot pick it up regardless of invocation style; verified as
  part of the build/test task above.
- **[Trade-off] `site.css` keeps carrying ~a few hundred lines of now-dead 10e selectors
  indefinitely.** → Accepted per Decisions above; revisit only if/when someone wants to
  actually redesign or retire the file, not as a side effect of this move.

## Open Questions

None - every ambiguity found during file discovery (Contracts split, Session wiring,
army-view.js vs. site.css, FuzzySharp) was resolved above rather than deferred, since
each would have changed the task breakdown if left unresolved.
