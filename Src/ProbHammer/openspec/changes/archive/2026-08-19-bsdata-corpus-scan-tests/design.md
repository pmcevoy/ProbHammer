## Context

`Domain.Catalogue.Bsdata` (`catalogue-json-ingestion`, archived) resolves BSData 11th Edition JSON
catalogue files into `Datasheet`/`Statline`/`WeaponProfile`/`Ability`. Its two failure-prone spots
are `BsdataDatasheetMapper.ParseThreshold` (throws `AmbiguousCharacteristicException` for a
threshold characteristic — `Sv`/`InSv`/`BS`/`WS`/`LD` — whose text doesn't match the single-value
shape `CharacteristicPattern` expects) and `WeaponKeywordParser.Apply` (silently leaves a weapon
`Keywords` token unrecognized — no flag set — when it doesn't exactly match the fixed vocabulary in
its regex/switch list). Both were last checked against the full local BSData clone
(`C:\Users\Pete\wh40k-11e`, this machine only, not part of the repo) through one-off runs during
`catalogue-json-ingestion`'s implementation — a full-corpus resolution pass and a scratch keyword
scan (`scratchpad/keywordscan/`) — neither kept as permanent, re-runnable coverage. See
`proposal.md` for motivation; this document covers the scan mechanism, the allowlist shape, and the
two implementation questions the proposal flagged as needing a decision here.

## Goals / Non-Goals

**Goals:**
- Walk every real catalogue file in the local clone and surface every characteristic-resolution
  exception and every unrecognized weapon-keyword token as a scan result.
- Give both scans one shared, bidirectional allowlist-matching mechanism, so a result outside the
  allowlist and a stale allowlist entry are both surfaced the same way.
- Keep both scans inert on a machine without the clone, and inert during a normal `dotnet test`
  run.

**Non-Goals:**
- Deciding `WeaponProfile.S`'s representation for dice-notation Strength, or `Statline`'s
  representation for multi-value `InSv` — both stay parked per `proposal.md`; this change only
  makes their failures visible and tracked.
- Recognizing any new weapon-keyword token as an existing `WeaponProfile` flag, or extending
  `CharacteristicPattern`'s accepted shapes — triaging the 43 previously-found unrecognized tokens
  into real parser fixes vs. allowlist entries is follow-up work informed by this scan's first
  real run, not something this change does itself.
- A UI, report file, or dashboard for scan results — `Assert.Fail`/`Assert.Skip` output through
  the normal test runner is the only reporting surface.

## Decisions

**Corpus walk: treat every catalogue file as its own starting file in turn, and attempt
`BsdataDatasheetMapper.BuildDatasheet` on every one of that file's own top-level
`SharedSelectionEntries`, using id/group/profile indexes built from that file's own closure.**
Alternative considered: only resolve names actually referenced by a captured export
(`data/gw-app-export*.txt`), mirroring the one-off check from `catalogue-json-ingestion`. Rejected
as too narrow — the goal here is corpus-wide coverage, not just the units three real armies happen
to field, and the whole point is finding gaps no captured export has exercised yet. Scanning a
file's own entries (rather than the transitive closure reachable from it) avoids re-resolving the
same imported entry once per importer; every file still gets its turn as the starting file, so an
entry that's only ever defined in one place still gets attempted exactly when that file is the
starting point. `BuildDatasheet` is safe to call on a non-unit entry (wargear, an enhancement) —
it just walks whatever profiles that entry's subtree actually contains and produces an
empty-statlines `Datasheet` if there are none, not an error — so the walk doesn't need to
pre-filter entries by `type`.

**Weapon-keyword scan: add a small `WeaponKeywordParser` method reusing its existing
recognition logic, rather than duplicating the regex/switch list in test code.** The alternative —
having the scan re-implement "is this token recognized" by copying `WeaponKeywordParser.Apply`'s
`AntiPattern`/`MeltaPattern`/`RapidFirePattern`/`SustainedHitsPattern` regexes and its literal
switch arms — creates exactly the drift risk this change exists to catch: BSData adds keywords
mid-edition (see `proposal.md`), and a newly-recognized token added to `Apply` but not mirrored in
a duplicated test-side list would make the scan wrongly keep reporting it as unrecognized forever.
`WeaponKeywordParser.Apply` is refactored to route each token through one shared
`TryRecognize(string token, WeaponProfile weapon, out WeaponProfile updated)` helper; `Apply` folds
it over every token as it already does with `weapon` threaded through, and a new public
`WeaponKeywordParser.UnrecognizedTokens(string keywordsText)` filters `keywordsText`'s split tokens
down to the ones `TryRecognize` returns `false` for. Both stay in `WeaponKeywordParser` itself (a
public static class already exposing `Apply`) rather than behind `internal`/`InternalsVisibleTo` —
this is a normal second public entry point on an existing public API, not a test-only escape
hatch, so `catalogue-json-ingestion`'s own recognition behavior (`datasheet-catalogue`'s "Weapon
Profile Verbatim Keyword Text" requirement) is unaffected; token splitting and recognition rules
are unchanged, just no longer duplicated.

**Confirmed by code inspection: the 3 known Ork dice-Strength weapons already throw today, through
the general `catch` in Test 1, not through a special case.** `WeaponProfile.S` is parsed by
`ParsePlainInt`, which only strips a trailing `+` before calling `int.Parse` — dice notation like
`"D6+6"` or `"2D6"` has no trailing `+` to strip and isn't numeric, so `int.Parse` throws
`FormatException`. Test 1 already catches "`AmbiguousCharacteristicException` (and any other
resolution exception)" per `proposal.md`, so `FormatException` needs no special-casing: it's
covered by the same mechanism as an unrecognized threshold shape, just a different allowlist
pattern ("dice-notation `S` characteristic fails `ParsePlainInt`"). This resolves `proposal.md`'s
open verification item — no separate flagging mechanism is needed — but the exact 3 datasheets
should still be confirmed against the live clone during implementation (a `FormatException` could
in principle come from a different, unrelated cause, and the corpus may turn up more than the 3
previously known).

**Allowlist entries are plain C# objects with a match predicate, not a JSON/config file.** A
per-pattern allowlist entry needs to express things like "an `AmbiguousCharacteristicException`
where `Characteristic == "InSv"` and `Text` contains `"/"` or `"("various" — a predicate over a
typed value, not a flat string comparison. A JSON allowlist would need its own small pattern
language (regex-on-message? structured fields?) invented and parsed for no real benefit over just
writing the predicate directly in C#, and this project already prefers no unnecessary dependencies
or abstractions (`CLAUDE.md`). Each entry pairs a short human-readable `Description` (used in
failure output) with a `Func<Exception, bool>` (Test 1) or `Func<string, bool>` (Test 2, matching a
single unrecognized token). Both scans run their full pass first, then check the collected
results against the allowlist in one pass at the end — this is what makes the "every allowlist
entry was actually hit" direction possible, since it needs the complete result set before it can
tell a stale entry from one about to match a not-yet-seen result.

```csharp
public sealed record AllowlistEntry<T>(string Description, Func<T, bool> Matches);
```

used as `AllowlistEntry<Exception>` for Test 1 and `AllowlistEntry<string>` for Test 2 (a single
unrecognized token, not a whole weapon), each scan's list living in its own file next to its test
(`CharacteristicResolutionAllowlist.cs`, `WeaponKeywordAllowlist.cs`) under a new
`tests/ProbHammer.Tests/Domain/Catalogue/Bsdata/CorpusScan/` directory, alongside the two
`[Fact(Explicit = true)]` test classes themselves. Seed `CharacteristicResolutionAllowlist` with
one entry for the `InSv`-multi-value pattern (`Characteristic == "InSv"`) and one for the
dice-notation-`S` pattern (a `FormatException` — matched by, at minimum, message content, since
`FormatException` carries no structured field to check).

**Bidirectional check reports both directions as distinct failure reasons in one assertion
message**, rather than two separate test methods per scan — an unmatched result and an unhit
allowlist entry are both regressions of the same "the allowlist no longer describes reality"
concern, and a single run naturally produces both categories together when relevant.

**Live-clone detection: a small shared helper checks `Directory.Exists` against the clone's
already-documented literal path (`C:\Users\Pete\wh40k-11e`) and calls `Assert.Skip(...)`
(xUnit v3's dynamic skip, already available — `xunit.v3` 4.0.0 is in place per `global.json`) when
absent, before either scan touches `LocalDiskBsdataCatalogueSource`.** The path is hardcoded rather
than sourced from configuration: unlike `LocalDiskBsdataCatalogueSource`'s constructor parameter
(deliberately kept caller-supplied in `catalogue-json-ingestion`'s design so production wiring
never bakes in a developer's machine), this path only ever matters inside a manually-triggered,
this-machine-only test — the same literal path is already written directly into three checked-in
docs (`CLAUDE.md`, `.claude/bsdata-json-schema.md`, `.claude/domain-model-11e.md`), so hardcoding
it a fourth time in test code introduces no new leak. `Assert.Skip` (not a silent early `return`)
is chosen specifically because the spec's "reported as skipped, not as passed" requirement needs
the test result itself to say so — a `return` makes the test report a bare pass, indistinguishable
from a real, clean corpus run.

## Risks / Trade-offs

- **[Risk]** Scanning every `SharedSelectionEntries` entry in every file (not just named units) is
  more file/CPU work than the earlier one-off checks did, since `BuildDatasheet` walks each
  entry's full subtree and every file is deserialized once per its own closure resolution — full
  corpus runtime is unknown until first run. → **Mitigation**: both tests are `Explicit`, so this
  never affects `dotnet test`'s default runtime or CI; acceptable to be slow for a manually-
  triggered check. Revisit with per-file JSON caching (already flagged as a future concern in
  `catalogue-json-ingestion`'s design) only if a real run turns out impractically slow.
- **[Risk]** The corpus may turn up resolution exceptions or keyword tokens in patterns not yet
  anticipated (only the `InSv`-multi-value and dice-`S` patterns are seeded) — first run is
  expected to fail until the allowlist is extended to match what's actually found.
  → **Mitigation**: expected and desired — that failure *is* the scan doing its job the first time
  it runs against the full corpus; `tasks.md` includes running both scans and extending the seed
  allowlist from real results as an explicit implementation step, not treating a clean first run
  as a success criterion.
- **[Risk]** `WeaponKeywordParser.Apply`'s refactor (extracting `TryRecognize`) touches shipped
  production code for a change whose proposal originally scoped itself as test-only.
  → **Mitigation**: behavior-preserving by construction (same regexes, same switch arms, same
  token split, just called from two entry points instead of duplicated) and covered by
  `WeaponKeywordParserTests.cs`'s existing suite, which must still pass unchanged; the alternative
  (duplicating recognition logic in test code) is the actually risky option, since it can silently
  drift from what `Apply` really recognizes.

## Migration Plan

None — purely additive. No existing code calls `WeaponKeywordParser.UnrecognizedTokens` or the new
`CorpusScan` test classes yet, and the `WeaponKeywordParser.Apply`/`TryRecognize` refactor is
behavior-preserving.
