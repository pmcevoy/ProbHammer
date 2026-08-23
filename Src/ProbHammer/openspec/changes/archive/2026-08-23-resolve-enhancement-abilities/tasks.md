## 1. Domain model: `Ability.Origin`

- [x] 1.1 Add `AbilityOrigin` enum (`Intrinsic`, `Enhancement`, `OptionalGrant`) in
      `src/ProbHammer.Core/Domain/Catalogue/AbilityOrigin.cs`.
- [x] 1.2 Add a required `Origin: AbilityOrigin` field to `Ability`
      (`src/ProbHammer.Core/Domain/Catalogue/Ability.cs`).
- [x] 1.3 Update every existing `Ability` construction site (fixtures, hand-built test abilities)
      to supply an explicit `Origin`.

## 2. Domain model: `Datasheet` on-demand ability index

- [x] 2.1 Add a new `Datasheet` constructor parameter for optional abilities (separate from the
      existing intrinsic `abilities` parameter), building a private
      `_optionalAbilities: IReadOnlyDictionary<string, Ability>` (case-insensitive, mirroring
      `_weaponProfiles`).
- [x] 2.2 Add `Datasheet.TryResolveAbility(string name, out Ability ability)`, mirroring
      `TryResolveWeaponProfile`.
- [x] 2.3 Add `Datasheet.OptionalAbilityNames: IReadOnlyList<string>`, mirroring `WeaponNames`.
- [x] 2.4 Unit tests: resolving a known optional ability by name succeeds; resolving an unknown
      name fails without a result; `OptionalAbilityNames` enumerates exactly the optional set, not
      the intrinsic one.

## 3. `BsdataDatasheetMapper`: split intrinsic vs. optional ability collection

- [x] 3.1 Extend `WalkContext` with a second ability collection (all optional abilities found,
      keyed/deduped by name) alongside the existing intrinsic `Abilities`/`SeenAbilityNames`.
- [x] 3.2 Thread the nearest enclosing `BsSelectionEntryGroup`'s `Name` alongside the existing
      `ancestry` chain through `WalkGroup`/`WalkEntry`/`WalkLink`/`WalkInfoLink`, resetting to
      `null` on an `entryLink` hop (matching the existing ancestry-reset behavior for a
      shared/reusable target).
- [x] 3.3 In `ProcessProfile`'s `"Abilities"` case, branch on `ancestry[^1].Type == "upgrade"`:
      intrinsic path (existing behavior) when not an upgrade entry; optional path (new) when it is
      — classified as `Enhancement` when the nearest enclosing group name contains
      `"Enhancements"` (case-insensitive substring), else `OptionalGrant`.
- [x] 3.4 `BuildDatasheet` passes both ability collections into the two new `Datasheet` constructor
      parameters (intrinsic list unchanged in shape; optional collection new).
- [x] 3.5 Unit tests against hand-built closure fixtures: an ability profile directly on the
      unit-owning entry is extracted as `Intrinsic` and appears in `Datasheet.Abilities`; an
      ability nested in a `type: upgrade` entry inside a group named "Enhancements" is extracted as
      `Enhancement` and is resolvable via `TryResolveAbility` but absent from `Datasheet.Abilities`;
      an ability nested in a `type: upgrade` entry inside a differently-named group (Shield-Dome
      shape) is extracted as `OptionalGrant` under the same absent-from-`Abilities` rule; an
      Enhancement reached through a nested sub-pool group (e.g. "X Enhancements", not exactly
      "Enhancements") is still classified `Enhancement`.

## 4. `ArmyRosterEnricher`: repoint wargear fallback, add Enhancement resolution

- [x] 4.1 Change `ResolveWargearItem`'s ability-fallback branch from
      `datasheet.Abilities.FirstOrDefault(...)` to `datasheet.TryResolveAbility(...)`.
- [x] 4.2 Add `ResolveEnhancements(IReadOnlyList<string> enhancementNames, Datasheet datasheet) ->
      IReadOnlyList<Ability>`, normalizing each name (`BsdataNameNormalization`) and resolving via
      `TryResolveAbility`, throwing `BsdataNameResolutionException` with a
      `BsdataNameSuggestion.FindClosest` suggestion (sourced from `OptionalAbilityNames`) on a miss.
- [x] 4.3 Wire `BuildUnit` to call `ResolveEnhancements` and pass the resolved list into `Unit`
      instead of the raw parsed strings.
- [x] 4.4 Unit tests: a selected Enhancement name resolves to its full `Ability`; an unresolvable
      Enhancement name throws with a diagnostic naming the offending text; a unit with no
      Enhancements selected produces an empty resolved list (not one containing a Datasheet's
      available-but-unselected Enhancement).
- [x] 4.5 Regression test reading `data/gw-app-export-templars.txt` end-to-end (parse → enrich):
      Black Templars' Crusade Ancient's resulting `Datasheet.Abilities` excludes "Thirst for Glory"
      and its `Unit.Enhancements` is empty; Sword Brethren Squad's `Datasheet.Abilities` excludes
      both "Fervent Exemplars" and "Inheritors of Sigismund". (Also covers the real Impulsor Shield
      Dome wargear-fallback case, in place of the originally-planned "selected Enhancement" case -
      this export has no `Enhancement(s):` line at all; that scenario is covered by the hand-built
      `ArmyRosterEnricherTests` instead.)

## 5. `Unit`: `Enhancements` element type change

- [x] 5.1 Change `Unit.Enhancements` from `IReadOnlyList<string>` to `IReadOnlyList<Ability>`
      (`src/ProbHammer.Core/Domain/Roster/Unit.cs`), updating its constructor parameter type.
- [x] 5.2 Confirm (via build) that no other call site outside `ArmyRosterEnricher` breaks — none
      were found reading `Unit.Enhancements` during investigation, but the compiler is the
      authority.

## 6. Verification

- [x] 6.1 Run the full test suite; confirm no regression in existing wargear-resolution tests
      (Shield Dome), statline-resolution tests, or the full-corpus scan tests
      (`CharacteristicResolutionScanTests`, `WeaponKeywordScanTests`,
      `BracketTokenResolutionScanTests`) — none of these should be affected by this change's scope,
      but they are the cheapest signal if the ancestry/group-name threading regresses something
      unrelated. (292 tests total, all passing, including all 3 permanent full-corpus scans run
      explicitly against the live BSData clone.)
- [x] 6.2 Manually verify via the real `/Import` → `/LivePlay` flow (`dotnet run` + `curl`, cookie
      jar + antiforgery token, mirroring `ImportFlowTests`) using `data/gw-app-export-templars.txt`:
      confirmed 0 occurrences of "Thirst for Glory"/"Fervent Exemplars"/"Inheritors of Sigismund" in
      the rendered `/LivePlay` HTML, while Crusade Ancient, Sword Brethren Squad, Impulsor's "Shield
      Dome", and the genuine intrinsic abilities (Vengeful Exhortation, Martial Honour, Exploit
      Their Cowardice) all still render correctly.

## 7. Documentation

- [x] 7.1 Update `.claude/domain-model-11e.md`'s "Catalogue Context" (`Datasheet`'s on-demand
      ability resolution, `Ability.Origin`) and "Army List Import Pipeline" (`ArmyRosterEnricher`'s
      wargear-fallback repoint and new Enhancement resolution step, `Unit.Enhancements`'s new
      shape) sections to match this change's shipped behavior.
- [x] 7.2 Update `PROGRESS.md` with a summary of the bug, its root cause, and the fix, per this
      project's existing work-journal convention.
