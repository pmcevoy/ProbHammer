## 1. Army List Parsing (`army-list-parsing`)

- [x] 1.1 Define the parsed-army-list intermediate contract (army metadata, attachment groups,
      standalone units, per-unit model sub-groups with counts/weapons, Enhancements) in
      `ProbHammer.Core`
- [x] 1.2 Implement `IArmyListParser` + a concrete parser: army metadata line extraction
      (Name/Points, Faction lines, Detachment(s) — including a comma-bearing Detachment name
      captured as one entry, ForceDisposition, BattleSize/PointsLimit)
- [x] 1.3 Implement `ATTACHED UNITS` group parsing (`Attached unit N` boundaries, `Attached as:
      Role[ (Category)]` recognition, Bodyguard/Leader/Support classification, Category discarded)
- [x] 1.4 Implement standalone top-level section parsing (`CHARACTERS`/`BATTLELINE`/`DEDICATED
      TRANSPORTS`/`OTHER DATASHEETS`)
- [x] 1.5 Implement model-group + weapon bullet parsing, including the shared-vs-partition
      arithmetic split and its fail-loud exception on unpartitionable counts
- [x] 1.6 Implement `Enhancements:`/`Enhancement:` bullet-line recognition
- [x] 1.7 Unit tests against all three captured real exports (`data/gw-app-export*.txt`) plus
      hand-built edge-case fixtures (missing category, comma-bearing Detachment name,
      unpartitionable weapon counts, single- vs two-entry Faction lists)

## 2. Army Roster Enrichment (`army-roster-enrichment`)

- [x] 2.1 Implement Faction → starting catalogue file resolution (suffix match on the most
      specific Faction entry, excluding filenames containing `Library`, fail loud on zero or
      multiple matches)
- [x] 2.2 Implement the app-wide catalogue cache (closure + id/group/profile indices keyed by
      starting filename, populated lazily, valid for the process's lifetime)
- [x] 2.3 Implement name normalization (typographic → ASCII apostrophe) ahead of
      `BsdataNameResolver` calls
- [x] 2.4 Implement per-unit and per-weapon name resolution against the closure, with fail-loud
      diagnostics naming the unresolved text
- [x] 2.5 Implement `AttachedUnit`/`Unit`/`ModelLine` graph assembly from resolved names and
      parsed groups (including multiple `ModelLine`s sharing one statline name from a split group)
- [x] 2.6 Implement final `ArmyRoster` assembly (parsed metadata + assembled units)
- [x] 2.7 Unit tests against trimmed BSData fixtures (mirroring `catalogue-json-ingestion`'s
      existing fixture approach): faction lookup (match, no-match, ambiguous-match), cache reuse,
      name resolution failure, split-group `ModelLine` assembly
- [x] 2.8 One-off manual verification against the live BSData clone for all three captured
      exports (mirroring `catalogue-json-ingestion`'s original one-off smoke test) — not a
      permanent automated test

## 3. Session-Backed Import (`army-list-import`)

- [x] 3.1 Reintroduce ASP.NET Core Session in `Program.cs` (`AddDistributedMemoryCache`/
      `AddSession`/`UseSession`), scoped to this new purpose
- [x] 3.2 Add the import Razor Page (paste box + submit)
- [x] 3.3 Wire submission to parsing + enrichment; store the parsed army list in session on
      success; redirect to `/LivePlay`
- [x] 3.4 Surface parse/enrichment failures on the import page without crashing or discarding an
      existing successful session import
- [x] 3.5 Integration tests (extending the existing `WebApplicationFactory`-based suite from
      `casualty-tracking`): successful import + redirect, parse failure reporting, enrichment
      failure reporting, two concurrent sessions staying isolated from each other

## 4. LivePlay Cutover (`live-play-view`)

- [x] 4.1 Repoint `/LivePlay`'s `OnGet` from `Examples.View.MyArmyRoster()` to rebuilding
      `ArmyRoster` fresh from the session's stored parsed army list
- [x] 4.2 Add the "no active session import → redirect to the import page" behavior
- [x] 4.3 Repoint the casualty-adjustment endpoint's rebuild-from-scratch logic at the same
      session-derived roster path
- [x] 4.4 Update or replace existing `/LivePlay` and casualty-endpoint tests that assumed
      `Examples.View.Roster()` as the fixed data source

## 5. Verification

- [x] 5.1 `dotnet build` / `dotnet test` clean
- [x] 5.2 Manual end-to-end check via `firefox-devtools-mcp` (or `dotnet run`): paste each of the
      three real captured exports, confirm `/LivePlay` renders correctly for each; confirm a
      second concurrent session (separate browser profile) sees only its own imported army
- [x] 5.3 Update `.claude/domain-model-11e.md` and `PROGRESS.md` per this project's documentation-
      maintenance convention
