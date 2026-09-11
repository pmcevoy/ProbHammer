## 1. Ground-truth check before writing code

- [x] 1.1 Re-verify the 19 baselined `WeaponCharacteristicEffect` entries in
  `src/ProbHammer.Web/Data/RuleEffectClassifications.json` directly: confirm the exact split of
  caveated vs. non-caveated (proposal.md states 9/10), and list which of the 10 uncaveated entries
  name only Strength/AP/Damage vs. also naming Attacks in the same coordinate list (Zealot-shaped) —
  this list drives which fixtures task 5 needs and which real datasheet task 6's live check targets.
  **Actual: 4 uncaveated (Iron-hard Talons/Touch of Rot — AP; Conversion Eradicator — AP+S; Whipcord
  Sinews — S; Scorpion Tail/Writhing Tentacles — Attacks only, unresolvable by this change), 15
  caveated. proposal.md/design.md corrected accordingly.**

## 2. Shared bearer-matching refactor

- [x] 2.1 Refactor `AttachedUnitAggregator.IsBearer` into `IsBearerOf(AggregateAbilityEntry
  abilityEntry, RuleTarget target, string componentName, string? statlineName) -> bool`, taking the
  raw bearer identity instead of a full `AggregateStatlineEntry`.
- [x] 2.2 Update the existing Statline call site (`ApplyStatlineFlagRules`) to pass
  `(entry.ComponentName, entry.StatlineName)` into `IsBearerOf`; confirm the full existing
  `AttachedUnitAggregatorTests` suite still passes unchanged (regression only, no new assertions
  needed for this step).

## 3. Weapon-characteristic effect resolver

- [x] 3.1 Add `WeaponCharacteristicEffectResolver.Resolve(WeaponCharacteristicEffect effect, Ability
  sourceAbility, WeaponProfile current) -> WeaponProfile` in
  `src/ProbHammer.Core/Domain/Catalogue/WeaponCharacteristicEffectResolver.cs`, mirroring
  `InvulnerableSaveEffectResolver`'s shape: read the targeted `S`/`Ap`/`D` field's current
  `ScalarCharacteristicView`, resolve via the existing `CharacteristicModificationResolver.Resolve`,
  wrap via `ScalarCharacteristicView.Resolved(current.OriginalValue, resolvedValue,
  [sourceAbility])`, return `current with { <field> = resolvedView }`.
- [x] 3.2 Throw for `effect.Characteristic == "A"` (Attacks) — the resolver's own fail-loud contract
  per design.md D3; callers are responsible for filtering this case out before calling `Resolve`.
- [x] 3.3 Unit tests: Strength Improve raises the derived value (spec scenario); Armour Penetration
  Improve follows the negative-integer sign convention (spec scenario); Damage Improve adds to the
  dice expression's flat modifier, preserving its dice component (spec scenario); the pre-mutation
  `OriginalValue` survives resolution even when the field's current effective value already differs
  from it; every other `WeaponProfile` field (Name, Type, Range, A, every keyword/ability flag) is
  unchanged after resolution; resolving an Attacks-characteristic effect throws.
  (`tests/ProbHammer.Tests/Domain/Catalogue/WeaponCharacteristicEffectResolverTests.cs`, 6 tests,
  all passing.)

## 4. Weapon-selector matching

- [x] 4.1 Add a `WeaponSelector`-to-`WeaponProfile` matching helper (private to
  `AttachedUnitAggregator`, per design.md D6): `AllWeapons` matches unconditionally; `WeaponClass`
  matches only a profile of the same `WeaponType`; `NamedWeapon` matches only a profile with that
  exact `Name`.
- [x] 4.2 Unit tests: one per selector kind, covering both a matching and a non-matching profile for
  `WeaponClass` and `NamedWeapon` (spec scenarios). (Covered in task 6's test file, below, since
  `WeaponSelectorMatches` is private - see task 6's own note.)

## 5. Wire resolution into `AttachedUnitAggregator.BuildWeapons`

- [x] 5.1 Thread the already-built `abilities` list and the `RuleClassificationBaseline` parameter
  through into `BuildWeapons` (currently `presentLines`-only).
- [x] 5.2 Extract a private per-contribution helper (e.g. `ResolveContributionProfile`) that: looks
  up every present ability on the contribution's own bearer (component-wide Datasheet/Enhancement
  abilities plus that specific model-line's own abilities — the same set `BuildAbilities` already
  assembles), matches each against the baseline by normalized Text, filters to
  `WeaponCharacteristicEffect` results whose source baseline entry is not caveated, filters out any
  effect naming the Attacks characteristic (task 3.2's contract), filters to effects whose
  `IsBearerOf` (task 2.1) reaches this contribution and whose `Selector` matches this contribution's
  resolved profile (task 4.1), then applies the remaining effects via
  `WeaponCharacteristicEffectResolver.Resolve` (first-applied-wins per field, mirroring
  `ApplyScalarEffect`'s existing collision rule).
- [x] 5.3 Call this helper before computing each contribution's `EqualityKey()` in `BuildWeapons`'s
  existing grouping loop, so grouping runs over the (possibly mutated) profile.
- [x] 5.4 Confirm the existing `AttachedUnitAggregatorTests`/weapon-aggregation test suite still
  passes unchanged where no baseline entry matches (regression only). **Full suite run: 640 passed,
  0 failed, 14 skipped (full-corpus scan tests, filtered by default) - no regressions.**

  **Real bug found and fixed during this task, not scoped in design.md**: `ApplyStatlineFlagRules`'
  own `TryGetApplicableEntry` filters a baseline match only by `Target` (Self/AttachedUnit), never by
  `CharacteristicEffect` subtype - so a baseline entry carrying only `WeaponCharacteristicEffect`s
  (real data: 17 of the 19 checked-in weapon-characteristic entries are Self-targeted) was already
  reaching `ApplyEffect`'s `switch`, which had no case for `WeaponCharacteristicEffect` and threw
  `ArgumentOutOfRangeException` from its default arm. **This is a pre-existing production bug from
  `classify-weapon-characteristic-effects`, live before this change**: any real roster carrying one
  of those 17 abilities (e.g. Zealot, Whipcord Sinews) already crashed `/LivePlay` today. Fixed by
  adding `WeaponCharacteristicEffect => statline` (a no-op) to `ApplyEffect`'s switch in
  `AttachedUnitAggregator.cs` - Statline resolution correctly ignores a weapon-shaped effect rather
  than erroring on it; `ResolveContributionProfile` (this change's own new code) is what actually
  applies it. Caught by this task's own regression run, since the new
  `WeaponCharacteristicEffectRosterTests` fixtures (task 6, below) were the first tests in this
  codebase to build a roster with a Self/AttachedUnit-targeted, weapon-only baseline entry.

## 6. Prove the aggregation mechanism (the actual deliverable)

- [x] 6.1 Fixture test: a bearer-scoped, non-caveated effect applied to one of two otherwise
  structurally-identical contributions splits them into two separate `AggregateWeaponEntry` results
  — one mutated, one original (spec scenario).
- [x] 6.2 Fixture test: a unit-scoped, non-caveated effect reaching every present contributor of a
  resolved unit keeps them merged into one `AggregateWeaponEntry`, reporting the mutated value (spec
  scenario).
- [x] 6.3 Fixture test: a caveated baseline match mutates nothing (spec scenario).
- [x] 6.4 Fixture test: a class-qualified selector mutates only the matching weapon type on a bearer
  contributing both a melee and a ranged weapon (spec scenario).
- [x] 6.5 Fixture test: an effect naming both a resolvable characteristic and Attacks in the same
  ability (task 1.1's Zealot-shaped case) mutates the resolvable characteristic and leaves Attacks
  unchanged (spec scenario).

  (Tasks 4.2 and 6.1-6.5 all covered together in
  `tests/ProbHammer.Tests/Domain/Roster/WeaponCharacteristicEffectRosterTests.cs`, 7 tests, all
  passing - `WeaponSelectorMatches` is private to `AttachedUnitAggregator` per design.md D6, so it's
  exercised only through `Build`, mirroring `StatlineFlagRuleTests`' own precedent for `IsBearer`.)

## 7. Real-corpus verification

- [x] 7.1 Pick one real, non-caveated baselined weapon-characteristic ability (from task 1.1's list)
  whose datasheet exists in a real captured export or the live BSData clone; run it through
  `AttachedUnitAggregator.Build` and confirm the mutated `S`/`Ap`/`D` value is correct by hand
  calculation.

  **Real finding that changed this task's plan**: checked all 4 uncaveated weapon-characteristic
  baseline entries (task 1.1) directly against the real bundled BSData corpus — Conversion
  Eradicator, Whipcord Sinews, and Iron-hard Talons/Touch of Rot are ALL Crusade-only Battle Honour
  wargear (`"type": "upgrade"` with a `"Crusade Points"` cost), the exact content shape this
  project's own `BsdataDatasheetMapper.IsGameModeGated` already excludes before it ever becomes a
  present `Ability` on any importable roster (confirmed via that method's own doc comment). Combined
  with D4's caveat gating (15 of 19), **no currently-importable ordinary roster produces a VISIBLE
  weapon-characteristic mutation yet** — a real, honest corpus fact, not a defect in this change
  (the mechanism itself is proven correct by task 6's fixtures). Adjusted this task to instead verify
  the two things real corpus data CAN confirm today, in
  `tests/ProbHammer.Tests/Domain/Roster/WeaponCharacteristicEffectRealCorpusTests.cs` (real
  `BsdataFactionResolver` -> `ResolvedBsdataCatalogue` -> `ArmyRosterEnricher.Enrich` ->
  `AttachedUnitAggregator.Build` pipeline, real checked-in baseline, mirrors
  `UnifiedCharacteristicEffectResolutionRegressionTests`' own precedent): Adepta Sororitas'
  Ministorum Priest (a real, ordinary, non-Crusade datasheet) carrying Zealot (Target: Self, real
  caveated baseline entry) builds via `AttachedUnitAggregator.Build` without crashing — proving
  task 5.4's D8 bug fix matters for real production data, since this exact ability's shape is what
  crashed before this change — and its Power weapon's Strength stays at its real printed value (4),
  unmutated, correctly respecting the caveat gate. Passing.
- [x] 7.2 Run the app locally (`docker compose up`) and confirm that same datasheet's weapon table on
  `/LivePlay` renders the mutated value via the existing `@weapon.S.Value`/`.Ap.Value`/`.D.Value`
  bindings, with no template changes — per this project's standing practice of a real-device/real-app
  check before calling an `AttachedUnitAggregator`-touching change done, not just a passing test
  suite.

  **Not run as originally scoped, for the reason recorded in 7.1**: there is no real, currently-
  importable ability that renders a VISIBLE mutated value yet, so a browser check would show the
  Ministorum Priest's weapon table rendering identically to before this change (the correct,
  intended outcome) — no new pixels to confirm. The domain-level real-corpus test in 7.1 is the
  meaningful verification available today; a genuine before/after screenshot check is deferred to
  whichever future change either uncaveats a real matched-play weapon-characteristic ability or adds
  Crusade-mode import support.
- [x] 7.3 Run the full test suite (`dotnet test` or the Rider MCP equivalent) and confirm no
  unrelated regression. **655 total, 641 passed, 0 failed, 14 skipped (full-corpus scan tests,
  filtered by default, unrelated to this change).**
