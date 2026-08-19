## 1. `InvulnerableSave` value type

- [x] 1.1 Add `InvulnerableSave` record to `src/ProbHammer.Core/Domain/Catalogue/Statline.cs` (or a
      new sibling file): `MeleeInSv: int`, `RangedInSv: int`, `Caveated: bool`,
      `CaveatAbility: Ability?`.
- [x] 1.2 Replace `Statline.InSv: int` with `Statline.InvulnerableSave: InvulnerableSave`
      (non-nullable; absent = `MeleeInSv == 0 && RangedInSv == 0 && !Caveated`, matching the
      existing "0 means absent" convention). **Amended after review**: renamed the property back to
      `Statline.InSv` (type unchanged) via Rider's Refactor-Rename, at the user's explicit request
      — the type-rename-forces-compile-failures approach was useful for finding every call site
      during 1.1/1.2, but the property itself didn't need to keep the new name once that was done.
- [x] 1.3 Unit tests for the type's own invariants: absent/uniform default shape, and that a
      `Caveated == true` instance always carries a non-null `CaveatAbility` (constructor or
      factory-level guard, not just convention).

## 2. Entry-scoped ability resolution in `BsdataDatasheetMapper`

- [x] 2.1 Change `MapStatline`'s signature to receive the owning `BsSelectionEntry` and the
      `WalkContext` (id/profile indices), not just the bare `BsProfile` — update its one call site
      in `ProcessProfile`.
- [x] 2.2 Implement the entry-scoped candidate search: given a raw digit+marker (e.g. `"5+*"` →
      `"Invulnerable Save (5+*)"`), search the owning entry's own locally-nested `Abilities`
      profiles first, then its own `InfoLinks` where `Type == "profile"`, matching by the
      constructed name; resolve a matched infoLink by `TargetId` through the existing
      `profileIdIndex` — never search `WalkContext.Abilities` by name alone.
- [x] 2.3 Implement raw-text shape recognition for `InSv`: plain digit, `"(Ranged)"`/`"(Melee)"`
      parenthetical, footnoted single value, `"/"`-split with one footnoted side. Each resolves per
      `catalogue-json-ingestion`'s "Invulnerable Save Text Resolution" requirement.
- [x] 2.4 Self-contained shapes (plain, parenthetical, unfootnoted split-half) build a non-caveated
      `InvulnerableSave` directly from the raw text — no candidate search performed.
- [x] 2.5 Footnoted shapes (bare `"*"`, or one side of a `"/"`-split) always build a
      `Caveated = true` `InvulnerableSave`, using the resolved candidate ability from 2.2 as
      `CaveatAbility`, with `MeleeInSv`/`RangedInSv` both set to the one value known without
      interpretation (the unfootnoted digit when one exists, else the footnoted digit itself).
- [x] 2.6 Throw `AmbiguousCharacteristicException` for: a raw shape matching none of the above, or a
      footnote marker present with no candidate ability found in 2.2.
- [x] 2.7 Remove the now-obsolete parts of `ParseThreshold`'s/`CharacteristicPattern.InvSv`'s
      InSv-specific handling that this new logic supersedes; keep `ParseThreshold` as-is for
      Sv/BS/WS/LD, which are unaffected.
- [x] 2.8 Update `BsdataDatasheetMapper.cs`'s doc comments (the long `ParseThreshold`/
      `CharacteristicPattern` comments describing the old throw-always behavior) to match the new
      resolution behavior.
- [x] 2.9 (Discovered via 4.2/4.3) Widen "owning entry" to a full ancestry chain, not one entry —
      Howling Banshees' squad carries its ability infoLink on the top-level entry while the
      footnoted `InSv` sits on a nested child model entry one level down. `WalkEntry`/`WalkGroup`
      now thread an `IReadOnlyList<BsSelectionEntry>` ancestry (fresh on an `entryLink` hop, since a
      shared/reusable target isn't structurally part of whatever linked to it); `ResolveCaveatAbility`
      searches it nearest-first.
- [x] 2.10 (Discovered via 4.2/4.3) Recognize a second real ability-naming convention: generic
      `"*Invulnerable Save"` (no digit), alongside the digit-parameterized
      `"Invulnerable Save ({digit}+*)"` — confirmed on Space Marines' Judiciar/Astraeus (local) and
      Chaos Knights' War Dog family (linked).
- [x] 2.11 (Discovered via 4.2/4.3) Make the base rules game-system file (`Warhammer 40,000.json`)
      resolvable as a closure member, scoped to `SharedProfiles` only — see design.md's "Game-system
      file is a closure member for SharedProfiles only" decision for why this needed a general fix
      (per explicit user direction) and why the first, broader attempt regressed T'au weapon parsing
      and had to be narrowed. `BsCatalogueFile`/`BsCatalogue` gained a `GameSystem`/`GameSystemId`
      shape; `BsdataClosure` gained a separate `GameSystem` property, outside `Files`;
      `BsdataNameResolver.BuildProfileIdIndex` merges it in.

## 3. Test coverage for the new mapper behavior

- [x] 3.1 Fixture-based tests (trimmed real excerpts, per this project's existing
      `tests/ProbHammer.Tests/Domain/Fixtures/Bsdata/` convention) for: plain value, `"(Ranged)"`,
      bare footnote resolving through a local nested ability (Makari shape), bare footnote
      resolving through an infoLink into a shared profile (Canis Rex shape), and the `"/"`-split
      shape (Howling Banshee).
- [x] 3.2 Test the name-collision scenario explicitly: two abilities sharing an identical name with
      different ids/meanings resolve correctly by id, not by whichever the flattened ability list
      happened to keep.
- [x] 3.3 Test both remaining throw conditions: unrecognized raw shape, and a footnote marker with
      no reachable candidate ability.

## 4. Corpus-scan allowlist

- [x] 4.1 Remove the InSv entry from
      `tests/ProbHammer.Tests/Domain/Catalogue/Bsdata/CorpusScan/CharacteristicResolutionAllowlist.cs`.
- [x] 4.2 Run `CharacteristicResolutionScanTests.Full_corpus_characteristic_resolution_scan`
      (`[Fact(Explicit = true)]`, needs the local BSData clone) and confirm it passes with the InSv
      entry removed — i.e. confirm no real datasheet in the corpus still throws for `InSv` after
      this change, matching this session's investigation. Both full-corpus scans pass.
- [x] 4.3 Surfaced two real, unanticipated gaps (game-system closure reachability; ancestry-scoped
      ability resolution) — paused, raised with the user, fixed generally per their direction (see
      2.9–2.11 and design.md). One genuine data anomaly remains (Aeldari's Archon/Ynnari Archon, a
      footnoted `InSv` with no matching ability anywhere in the entry under either naming
      convention) — added as a new, narrowly-scoped allowlist entry rather than chased further.

## 5. Fixtures and `/LivePlay` rendering

- [x] 5.1 Add new hand-authored `Examples/` fixture(s) (in `src/ProbHammer.Core/Examples/`)
      exercising a non-uniform or caveated `InvulnerableSave` — the current example army has none.
      Added `Datasheets.CanisRex()`/`Units.CanisRex()`, using real verified BSData values (caveated
      "5+*" → the real linked ability's exact text), wired into `View.Roster()` as a 7th unit; the
      two test files that hard-coded the previous 6-unit roster (`ViewTests.cs`,
      `LivePlayModelTests.cs`'s unit-order assertion) updated accordingly.
- [x] 5.2 Update `_UnitBlock.cshtml`'s invulnerable-save rendering (currently
      `@if (block.Statline.InSv > 0)` / `@(block.Statline.InSv)++`) for the new shape: unchanged
      "N++" rendering for the uniform, non-caveated case; a distinct rendering for a differing
      melee/ranged value; a distinct visual indicator for a caveated value.
- [x] 5.3 Iterate the new rendering live via `firefox-devtools-mcp` against the new fixture(s),
      per this project's established `/LivePlay` visual-design workflow. Verified against Canis
      Rex's real caveated save: Sv tile shows "3+" with a "5++*" amber sub-value, a `title` tooltip
      ("See unit abilities for the exact condition"), and the linked "Invulnerable Save (5+*)"
      ability correctly appears in the adjacent abilities column. The differing-non-caveated
      render branch (e.g. a self-contained "(Ranged)" case) wasn't separately fixture-verified
      live — no second fixture unit was added for it; its domain-level values are covered by
      `InvulnerableSaveResolutionTests`, and the render branch itself is simple/low-risk, reviewed
      by inspection instead.
- [x] 5.4 Update `.claude/design-tokens.md` if the caveat indicator introduces a new visual token.
      No new token — reused `--amber`'s existing "pay attention, this is different from normal"
      role (already used by the casualty-reset control); updated that entry's description to name
      this second use.

## 6. Documentation

- [x] 6.1 Update `.claude/domain-model-11e.md`'s "BSData JSON Ingestion" section: `InvulnerableSave`
      shape, the entry-scoped resolution mechanism, the narrowed exception conditions, and the
      corpus-scan allowlist change.
- [x] 6.2 Update `PROGRESS.md`: implementation summary, and move the two parked "Known Issues"
      InSv bullets to reflect the resolved state (only the still-deferred ability-text
      interpretation pass and the still-separate Ork dice-Strength issue remain open).
- [x] 6.3 Confirmed `.claude/vnext-ideas.md`'s "Ability-text interpretation pass" entry still
      accurately reflects what this change actually built (no edit needed — it was written at the
      right level of abstraction during exploration) — reminded the user in the final report, as
      explicitly requested.
