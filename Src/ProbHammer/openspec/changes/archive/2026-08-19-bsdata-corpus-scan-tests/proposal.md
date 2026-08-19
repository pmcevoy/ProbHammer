## Why

`catalogue-json-ingestion`'s `BsdataDatasheetMapper` and `WeaponKeywordParser` were only ever
proven against the live BSData clone (`C:\Users\Pete\wh40k-11e`) through one-off, throwaway checks
run once during implementation — a full-corpus resolution pass and a scratch keyword scan, neither
kept. That leaves two known classes of resolution gap (ambiguous multi-value `InSv` forms, dice-
notation weapon Strength) and 43 unrecognized keyword tokens undetected by anything that runs
again, and no way to tell a newly-introduced parsing regression apart from an already-known,
accepted limitation, or to notice when a limitation has actually been fixed and its allowlist entry
has gone stale. A permanent, re-runnable scan closes that gap without requiring either open design
question (Ork dice-Strength representation, multi-value `InSv` representation) to be resolved
first.

## What Changes

- Add two xUnit v3 `[Fact(Explicit = true)]` tests (not run by default or in CI — manually
  triggered) that walk every real catalogue file in the local BSData clone:
  - **Characteristic-resolution scan**: resolves every named entry across the full corpus and
    catches `AmbiguousCharacteristicException` (and any other resolution exception)
    `BsdataDatasheetMapper` throws.
  - **Weapon-keyword token scan**: runs `WeaponKeywordParser` tokenization across every weapon
    profile in the full corpus and collects tokens that don't exactly match an existing
    `WeaponProfile` ability flag.
- Add a maintained, per-pattern (not per-datasheet) "known limitation" allowlist for each scan,
  checked **bidirectionally**: an actual failure/token not on the allowlist fails the test, and an
  allowlist entry that the run did not actually hit also fails the test — so a fixed issue can't
  silently go stale on the allowlist.
- Seed the characteristic-resolution allowlist with the two already-known `InSv` multi-value
  patterns (`"4+* / 5+"` on Wyches/Howling Banshees; `"5+ (Ranged)"` on the four Titans +
  Stormbird).
- Exclude `Warhammer 40,000.json` from both scans — not a real catalogue file, fails to
  deserialize.
- Both tests no-op (report nothing, do not fail) on a machine without the live clone present.
- During implementation, confirm whether the 3 known Ork dice-Strength weapons (Battlewagon
  `D6+6`, Big Gunz [Legends] `2D6`, Deff Rolla Battle Fortress [Legends] `2D6`) actually throw
  through `ParsePlainInt` today, and record the finding — resolving or representing that value is
  explicitly out of scope for this change.

## Capabilities

### New Capabilities
- `bsdata-corpus-scan`: manually-triggered, permanent full-BSData-corpus regression scan for
  characteristic-resolution failures and unrecognized weapon-keyword tokens, checked against a
  bidirectional, per-pattern allowlist.

### Modified Capabilities
(none — `catalogue-json-ingestion`'s and `datasheet-catalogue`'s resolution/parsing behavior is
unchanged by this change; only new, additive test coverage is added over it)

## Impact

- New test file(s) under `tests/ProbHammer.Tests/` (exact location decided in design.md) added to
  `ProbHammer.Tests.csproj`.
- New allowlist artifact(s) (format/location decided in design.md) checked into the repo and
  maintained going forward.
- One small, behavior-preserving addition to `ProbHammer.Core`: `WeaponKeywordParser` gains a
  public `UnrecognizedTokens` method (reusing its existing recognition logic rather than having
  the new test duplicate it — see design.md) so the keyword scan can't silently drift from what
  `Apply` actually recognizes. No other production code changes.
- Both new tests depend on the local BSData clone (`C:\Users\Pete\wh40k-11e`, this machine only,
  not part of the repo) and are `Explicit`, so they don't affect `dotnet test`'s default run or
  CI.
