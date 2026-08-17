## 1. ModelLine Type

- [x] 1.1 Add `Keywords` (`IReadOnlySet<string>`, `StringComparer.OrdinalIgnoreCase`, matching `Datasheet.Keywords`'s convention) to `ModelLine`, with an optional constructor parameter (`IEnumerable<string>? keywords = null`) defaulting to empty so every existing `ModelLine(...)` call site keeps compiling unchanged.

## 2. Keyword Resolution

- [x] 2.1 Extend `KeywordResolution.EffectiveKeywords` to also union the `Keywords` of every currently-present (`RemainingCount > 0`) `ModelLine` across a combat unit's present components, alongside the existing per-component `Datasheet.Keywords` union.
- [x] 2.2 Update the model-level keyword-check convention documented in `.claude/domain-model-11e.md` (and any code comment stating it) to read: a model-level check uses that specific model's own `Unit.Datasheet.Keywords` together with that specific model's own `ModelLine.Keywords`, never a sibling `ModelLine`'s, never the AttachedUnit-wide union.

## 3. Fixture

- [x] 3.1 Add a new fixture to `tests/ProbHammer.Tests/Domain/Fixtures/UnitFixtures.cs` building a `Unit` from `DatasheetFixtures.ChaosSpaceMarineSquad()`: one model-line per named statline entry (Chaos Space Marine, Chaos Space Marine Champion, Icon Bearer, Chaos Space Marine Gunner, Chaos Space Marine Reaper), each `Count: 1`, with `Psyker` present in only one model-line's own `Keywords`.

## 4. Tests

- [x] 4.1 Test: a unit-level keyword check for `PSYKER` against the fixture's `Unit` succeeds (union includes the present model-line's own keyword even though the Datasheet's own `Keywords` don't have it).
- [x] 4.2 Test: a model-level keyword check for `PSYKER` against the tagged model-line succeeds, and the same check against a sibling model-line (sharing the fixture's other statlines) fails — proves the scoping is per-model-line, not per-Unit or per-statline-value.
- [x] 4.3 Test: reducing the tagged model-line's `RemainingCount` to 0 removes `PSYKER` from the unit-level union (given no other present model-line or the Datasheet itself contributes it).
- [x] 4.4 Test: existing `KeywordResolution` scenarios (`AttachedUnitTests.cs` or wherever they currently live) still pass unchanged — confirms the union extension is additive, not a behavior change for `Datasheet.Keywords`-only cases.

## 5. Verification

- [x] 5.1 `dotnet build` — 0 errors, 0 warnings.
- [x] 5.2 `dotnet test` — full suite passes, including the new tests from Section 4.
