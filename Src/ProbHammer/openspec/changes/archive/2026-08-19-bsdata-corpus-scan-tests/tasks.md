## 1. Shared scan infrastructure

- [x] 1.1 Add a shared helper (e.g. `LiveCloneGuard` or similar) that checks
      `Directory.Exists` against the hardcoded clone path `C:\Users\Pete\wh40k-11e` and calls
      `Assert.Skip(...)` when absent, for both scan tests to call before touching
      `LocalDiskBsdataCatalogueSource`.
- [x] 1.2 Add a shared helper that returns the clone's catalogue file names via
      `LocalDiskBsdataCatalogueSource.ListFileNames()`, excluding `Warhammer 40,000.json`.
- [x] 1.3 Add the generic `AllowlistEntry<T>(string Description, Func<T, bool> Matches)` record
      under `tests/ProbHammer.Tests/Domain/Catalogue/Bsdata/CorpusScan/`.
- [x] 1.4 Add a shared bidirectional-check helper: given a list of produced results (`T`) and a
      list of `AllowlistEntry<T>`, returns the set of unmatched results and the set of unmatched
      allowlist entries; used identically by both scan tests' assertions.

## 2. WeaponKeywordParser.UnrecognizedTokens

- [x] 2.1 Refactor `WeaponKeywordParser.Apply`'s per-token `if`/`switch` recognition logic into a
      shared `TryRecognize(string token, WeaponProfile weapon, out WeaponProfile updated)` helper,
      with `Apply` folding it over `keywordsText`'s split tokens exactly as today (no behavior
      change).
- [x] 2.2 Add `public static IReadOnlyList<string> UnrecognizedTokens(string keywordsText)`,
      built from the same token split as `Apply`, filtered to tokens `TryRecognize` returns
      `false` for (`"-"`/empty still yields an empty list, matching `Apply`'s existing handling).
- [x] 2.3 Confirm `WeaponKeywordParserTests.cs`'s existing suite still passes unchanged against the
      refactored `Apply`; add a couple of direct tests for `UnrecognizedTokens` (a recognized-only
      token list returns empty; a mixed list returns only the unrecognized tokens, e.g. covering
      `"Hazardous"`/`"Precision"`/`"Cleave"`/`"Close Combat"` from `datasheet-catalogue`'s existing
      scenarios).

## 3. Characteristic-resolution scan (Test 1)

- [x] 3.1 Add `CharacteristicResolutionAllowlist.cs` under
      `tests/ProbHammer.Tests/Domain/Catalogue/Bsdata/CorpusScan/`, seeded with:
      - the `InSv`-multi-value pattern (`AmbiguousCharacteristicException` with
        `Characteristic == "InSv"`)
      - the dice-notation-`S` pattern (a `FormatException` from `ParsePlainInt`) — see design.md's
        "Confirmed by code inspection" decision
- [x] 3.2 Add `CharacteristicResolutionScanTests.cs` with one `[Fact(Explicit = true)]` test that:
      for each catalogue file in the clone (task 1.2), resolves that file's own closure via
      `BsdataClosureResolver.Resolve`, builds id/group/profile indexes via `BsdataNameResolver`,
      and attempts `BsdataDatasheetMapper.BuildDatasheet` for every entry in that file's own
      `SharedSelectionEntries`, catching and collecting any exception raised.
- [x] 3.3 Assert the collected exceptions against the allowlist (task 1.4), failing with a message
      that separately lists any unmatched exception (characteristic + text) and any unmatched
      allowlist entry.
- [x] 3.4 Run the test against the live clone. Confirm the 3 known Ork dice-Strength weapons
      (Battlewagon, Big Gunz [Legends], Deff Rolla Battle Fortress [Legends]) surface as
      `FormatException`s matching the seeded pattern from 3.1. Extend the allowlist with any
      further real patterns the run turns up, re-running until the test passes.
      **Real finding**: 54 total failures across 45 files, not just the 2+3 seeded patterns. 8
      were the already-known two-value InSv forms; ~43 were a previously-undocumented single
      trailing-`*` InSv footnote (e.g. `"5+*"` on Aeldari Rangers, most Imperial/Chaos Knights,
      Judiciar, Astraeus) that `CharacteristicPattern.InvSvRegex`'s doc comment incorrectly
      claimed was an accepted shape. Investigated by resolving the linked ability behind several
      real examples (paused for explicit user review before touching anything, per apply's
      guardrails) rather than assuming: the footnote almost always links to a shared "Invulnerable
      Save (X+*)" ability restricting the save to one attack type (ranged- or melee-only) - the
      same class of unrepresentable-in-`Statline.InSv` gap as the already-known forms, just
      spelled differently in source data - with one confirmed exception (Ork's Makari) using the
      same `*` for an unrelated "no re-rolls" caveat. Decision: the throw is correct,
      working-as-intended behavior (silently accepting `"5+*"` as bare `5` would misrepresent a
      conditional save as unconditional) - `ParseThreshold`/`CharacteristicPattern` were left
      unchanged; only the (previously inaccurate) doc comments and the allowlist entry's
      description were corrected to reflect this. The 3 known Ork dice-Strength weapons confirmed
      as `FormatException`s exactly as predicted by design.md's code-inspection decision, no
      further patterns beyond InSv/dice-`S` turned up.
- [x] 3.5 Confirm the test reports as skipped (not passed) when the clone path doesn't exist —
      verify by temporarily pointing the guard (task 1.1) at a nonexistent path. Confirmed:
      reports `[SKIP]` with the guard's message, not a bare pass. Path reverted afterward.

## 4. Weapon-keyword token scan (Test 2)

- [x] 4.1 Add `WeaponKeywordAllowlist.cs` under the same `CorpusScan/` directory. Leave the seed
      list empty pending task 4.4's real-corpus results (no confirmed-stable patterns exist yet —
      the prior scratch scan's 43 tokens were never triaged).
- [x] 4.2 Add `WeaponKeywordScanTests.cs` with one `[Fact(Explicit = true)]` test that: for each
      catalogue file in the clone, walks every profile in every `SharedSelectionEntries` /
      `SharedProfiles` entry with `TypeName` `"Ranged Weapons"` or `"Melee Weapons"`, reads its
      `Keywords` characteristic text, and collects every token
      `WeaponKeywordParser.UnrecognizedTokens` (task 2.2) returns.
- [x] 4.3 Assert the collected tokens against the allowlist (task 1.4), same shape as 3.3.
- [x] 4.4 Run the test against the live clone. Check whether `scratchpad/keywordscan/` still
      exists and, if so, whether its prior 43-token findings are still reproducible before reusing
      any of them. Triage each real unrecognized token into either a genuine
      `WeaponKeywordParser` gap worth fixing separately (out of scope for this change — file as a
      follow-up, per `proposal.md`) or an allowlist entry, and extend the allowlist accordingly,
      re-running until the test passes.
      `scratchpad/keywordscan/` no longer exists (confirmed missing, matching PROGRESS.md's note
      that it was never committed) — triaged fresh against this scan instead. First real run: 45
      distinct unrecognized tokens (not the old 43 — new corpus content since that prototype ran).
      All 45 added to the allowlist, none fixed in `WeaponKeywordParser` (out of scope), grouped
      into: 3 already-documented no-flag tokens (Hazardous/Precision/Heavy, matching
      `datasheet-catalogue`'s existing scenarios); 7 case/punctuation variants of an
      already-recognized token (parser case-sensitivity gap, flagged as a future fix candidate);
      4 dice-valued variants of an int-only mechanic (RapidFire/SustainedHits — same class of gap
      as the parked dice-Strength question); and 31 genuinely new/unmodeled mechanics, 3 of them
      real widespread core rules worth a dedicated future addition (`Extra Attacks` 150+
      occurrences, `Psychic`/`PSYCHIC` ~280 combined, `Lance` ~70, `One Shot` ~30+) rather than
      one-off weapon-specific flavor abilities. One data-quality finding: `ANTI-non‑MONSTER/
      VEHICLE 2+` (Aeldari's Stone Stave) uses a U+2011 non-breaking hyphen, not ASCII `-`, plus a
      dual slash-separated exclusion target — a different targeting shape from the modeled
      Anti-X mechanic, not just a spelling variant.

## 5. Wiring and docs

- [x] 5.1 Confirm the new test files build and are discovered via
      `dotnet test --project tests/ProbHammer.Tests/ProbHammer.Tests.csproj` (no `-v`/`--nologo` —
      see `PROGRESS.md`'s test-running gotcha), and that both new `Explicit` facts do not run in
      that default invocation. Confirmed: 162 total (up from 160), both new facts reported
      "skipped ... Not run (due to explicit test filtering)", 160 succeeded, 0 failed.
- [x] 5.2 Run `dotnet test --project tests/ProbHammer.Tests/ProbHammer.Tests.csproj
      --filter "FullyQualifiedName~CorpusScan"` (or equivalent Explicit-inclusive invocation) to
      confirm both scans actually execute and pass on this machine. Confirmed via the in-process
      xUnit v3 runner directly (`ProbHammer.Tests.exe -explicit only`, since `dotnet test`'s own
      `-- --help`/explicit-passthrough crashed the MTP wrapper on this machine): both scans
      execute against the live clone and pass (see 3.4/4.4).
- [x] 5.3 Update `.claude/domain-model-11e.md`'s "BSData JSON Ingestion" section with a short
      pointer to the new `CorpusScan` tests and allowlist files, and note
      `WeaponKeywordParser.UnrecognizedTokens` alongside the existing `Apply` entry. Also
      corrected `ParseThreshold`'s existing paragraph to reflect the single-asterisk InSv finding
      (was previously describing only the two known multi-value forms) and to point at the now-
      built scan instead of saying "not yet built".
- [x] 5.4 Update `PROGRESS.md`: move the relevant "Known Issues" bullets (permanent corpus-scan
      test not yet built; 43 unrecognized keyword tokens not yet triaged) to "Recently Completed",
      recording the scan's first real-run findings and the allowlist's actual seeded contents
      after task 3.4/4.4, and leave the two parked representation decisions (Ork dice-`S`,
      multi-value `InSv`) as the remaining open "Known Issues" entries. Also refreshed "Next
      Session Prompt" (was still the stale prompt that kicked off this exact change) and added the
      weapon-keyword-parser follow-up items as a new "Known Issues" entry.
