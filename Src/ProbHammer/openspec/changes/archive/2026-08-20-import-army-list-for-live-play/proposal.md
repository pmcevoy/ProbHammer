## Why

`/LivePlay` today renders only `Examples/View.Roster()`, a hand-authored fixture — there is no
way to get a real army list into the app at all. This directly blocks the project's stated core
purpose (`CLAUDE.md`: "paste two army list exports... enrich them against catalogue data"). The
pieces needed to close this gap already exist independently — `catalogue-json-ingestion` resolves
BSData names to `Datasheet` objects, and `add-army-roster-container`/the attached-unit domain work
built the `ArmyRoster`/`Unit`/`AttachedUnit`/`ModelLine` target shapes — but nothing connects a real
exported army-list text to either of them. This change builds that missing pipeline: parse a real
GW-app 11e export, resolve it against BSData, and serve the result to `/LivePlay` per user session.

## What Changes

- New `IArmyListParser` + a structured `ParsedArmyList`-shaped intermediate contract: parses raw
  GW-app 11e export text (army metadata, per-unit wargear/model selections, `ATTACHED UNITS`
  Leader/Support/Bodyguard role tagging) into data ready for enrichment. The 10th-edition
  `ArmyListParser`/`ArmyList` (`legacy/10e-pipeline/`) is **not** reused or patched — the 11e export
  format's attachment-role section has no 10e equivalent, and the old parser is fully archived, not
  merely untested-but-live as previously believed.
- New army-roster enrichment stage: resolves each parsed unit's `Datasheet` against a faction's
  BSData import closure (name resolution already exists via `BsdataNameResolver`) and builds the
  `Unit`/`AttachedUnit`/`ModelLine` graph that `catalogue-json-ingestion` deliberately left
  undesigned. Includes splitting an exported model-count group into multiple `ModelLine`s when
  per-model wargear diverges within the group (e.g. 5 Initiates split 3/2 by melee weapon choice).
  Wargear selections are taken verbatim from the export — no curation or inference about which
  weapon a model would "actually" use.
- New faction → starting BSData catalogue-file resolution: suffix-matches the export's most
  specific `Faction` entry against the catalogue's available file names, excluding any
  `Library`-suffixed file (shared cross-sub-faction content, never itself a playable faction
  identity).
- New app-wide BSData catalogue cache: a resolved closure and its id/group/profile indices are
  expensive to build (multi-file JSON parse, BFS-based import resolution) but static and identical
  for every user of a given faction — cached once per starting file, not rebuilt per request. No
  invalidation logic this round (cache lives until the app restarts).
- New session-backed multi-user support: ASP.NET Core Session, in-memory store. Each user's parsed
  import (`ParsedArmyList`) is stored in session and re-enriched fresh into an `ArmyRoster` on every
  request, including every casualty adjustment — extending the "always rebuild from source of
  truth" principle `casualty-tracking` already established for the fixture path, one level up.
- New import page/flow: paste export text, submit, parse and resolve against BSData, then redirect
  to `/LivePlay`. Parse or resolution failures (unparseable text, a unit name BSData can't resolve)
  are reported back to the user rather than surfacing as a crash.
- BSData catalogue JSON bundled into the `ProbHammer.Web` Docker image as a static snapshot — no
  live GitHub fetch-and-cache this round (explicitly deferred to a later change).
- **BREAKING**: `/LivePlay` stops rendering `Examples/View.Roster()` entirely. A session with no
  imported army list has nothing to render and is redirected to the import page instead.
  `Examples/` (`View.cs`/`Datasheets.cs`/`Units.cs`) stays in the codebase for future domain-model
  exploration but is no longer wired into the running app.

Explicitly out of scope, recorded for later rather than attempted here: the ability-text
"interpretation pass" that would tune a resolved `ArmyRoster` (e.g. flipping a known
`InvulnerableSave.Caveated` phrasing back to `false`) — this change's pipeline has a natural slot
for it (a stage between enrichment and `/LivePlay`) but does not build it; an attacker+defender
two-roster flow or any `Simulation/*` revival; live GitHub fetch-and-cache of BSData (the baked-in
snapshot will drift from real rules updates until that exists).

## Capabilities

### New Capabilities
- `army-list-parsing`: parses raw GW-app 11e export text into a structured intermediate capturing
  army metadata and per-unit wargear/attachment-role selections, independent of any catalogue data.
- `army-roster-enrichment`: resolves a parsed army list against BSData catalogue data — including
  faction-to-catalogue-file lookup and an app-wide catalogue cache — to build a live `ArmyRoster`
  (`Unit`/`AttachedUnit`/`ModelLine` graph).
- `army-list-import`: the paste/submit page and per-session storage that ties parsing and
  enrichment together, feeding `/LivePlay` and surfacing failures back to the user.

### Modified Capabilities
- `live-play-view`: the page's data source changes from the hardcoded `Examples/View.Roster()`
  fixture to the current session's imported-and-enriched `ArmyRoster`; a session with no import
  redirects to the import flow instead of rendering a roster.

## Impact

- New parsing namespace in `ProbHammer.Core` (`IArmyListParser` + intermediate contract types),
  replacing rather than reviving the fully-archived `legacy/10e-pipeline` parser.
- New enrichment/graph-builder code in `ProbHammer.Core.Domain.Roster`, consuming the existing
  `BsdataClosureResolver`/`BsdataNameResolver`/`BsdataDatasheetMapper` from `catalogue-json-ingestion`.
- New app-wide catalogue-cache service (`ProbHammer.Web`, registered as a singleton).
- `Program.cs`: reintroduces ASP.NET Core Session (`AddDistributedMemoryCache`/`AddSession`/
  `UseSession`) — removed by `archive-10e-pipeline` as a 10e-only dependency, brought back here for
  a new purpose (per-session `ParsedArmyList` storage, not the old `Enricher` cache).
- New import Razor Page + `PageModel`.
- `LivePlay.cshtml.cs`: `OnGet`/the casualty endpoint's rebuild-from-scratch logic repointed from
  `Examples/View.MyArmyRoster()` to the session-derived, freshly-enriched `ArmyRoster`.
- `src/ProbHammer.Web/Dockerfile` plus a new bundled BSData snapshot directory (outside `wwwroot`,
  excluded from the `.csproj` content glob so it isn't touched by `dotnet publish`) and matching
  `appsettings.json` config for `LocalDiskBsdataCatalogueSource`'s root path.
- `Examples/View.cs`/`Datasheets.cs`/`Units.cs`: unchanged in content, but no longer referenced from
  any `Web` page once this change lands.
