## Why

`introduce-characteristic-domain-model` added `CharacteristicValue`/`CharacteristicView` as new,
unconsumed types — nothing in the app builds or reads one yet. `InvulnerableSave` is the exact case
`CharacteristicView` was modeled on (its `Caveated`/`CaveatAbility` fields are a single-ability,
InSv-only instance of the general "original value + contributing abilities + caveated" shape), so
it's the natural first real consumer. Wiring it in also resolves a real, existing duplication:
today InSv's value can be flagged from two *different*, independently-maintained mechanisms —
`InvulnerableSave.Caveated`/`CaveatAbility` (a parse-time fact, resolved once when a Datasheet is
built from a footnoted catalogue value) and `StatlineFlagRule`/`AggregateStatlineEntry.Flags` (a
live, roster-state-dependent fact, recomputed every render when an ability like Shield Dome is
present) — and `LivePlayModel.GroupStatlines` has to branch between them by hand
(`statline.InSv.Caveated ? statline.InSv.CaveatAbility : g.SelectMany(...).FirstOrDefault(...)`).
Both are really the same underlying question — "is this unit's invulnerable save the Datasheet's
own base value, or has some ability changed what it should be, and do we actually know the
resulting value?" — and `CharacteristicView`'s shape already covers both without a hand-rolled
branch.

## What Changes

- `InvulnerableSave` (`Domain.Catalogue`) loses `Caveated`/`CaveatAbility` — becomes a plain
  `(MeleeInSv, RangedInSv)` value with no caveat concept of its own. **BREAKING** for any code
  constructing it with the 4-argument constructor or reading `.Caveated`/`.CaveatAbility`.
- `Statline.InSv`'s type changes from `InvulnerableSave` to `InvulnerableSaveCharacteristicView`
  (`Domain.Catalogue`, from `introduce-characteristic-domain-model`) — `OriginalValue`/
  `DerivedValue` typed as `InvulnerableSave`/`InvulnerableSave?`, `ContributingAbilities` holding
  whichever ability(ies) are responsible for the value not simply being the Datasheet's own base
  value. **BREAKING** for every reader of `Statline.InSv`.
- `BsdataDatasheetMapper.ResolveInvulnerableSave` (and the mirrored resolution in
  `BattleScribeRosterMapper`) build an `InvulnerableSaveCharacteristicView` directly at parse time
  in place of a caveated `InvulnerableSave`: a footnoted-and-unresolved value gets
  `ContributingAbilities: [caveatAbility]`, `DerivedValue: null`; an uncaveated/resolved value gets
  `ContributingAbilities: []`, `DerivedValue: OriginalValue`.
- `StatlineFlagRule.Apply` gains the matched `Ability` as a parameter (today it only receives the
  base `Statline`) so `ShieldDomeStatlineFlagRule` can build a resulting
  `InvulnerableSaveCharacteristicView` whose own `ContributingAbilities` includes its own matched
  ability (`DerivedValue` still set — a `StatlineFlagRule` match is by definition a fully
  understood, deterministic mutation, never caveated). **BREAKING** for
  `StatlineFlagRule.Apply`'s existing signature and any external override.
- `AttachedUnitAggregator.ApplyStatlineFlagRules` threads the matched ability through to `Apply`.
  `StatlineFlagCharacteristic.InvulnerableSave` and `AggregateStatlineEntry.Flags`'s
  InvulnerableSave-specific role are retired — a `StatlineFlag` is no longer produced for InSv,
  since the view itself now carries that information directly.
  `StatlineFlagCharacteristic.ObjectiveControl`/`VexillaStatlineFlagRule`/OC's own `StatlineFlag`
  usage is unaffected — this change is scoped to InSv only.
- `LivePlayModel.GroupStatlines`'s two-path InSv-source branch collapses into one read off
  `statline.InSv.IsCaveated`/`.ContributingAbilities` — no more special-casing the catalogue-caveat
  path separately from the live-rule-match path.
- `_UnitBlock.cshtml`'s InSv tile rendering reads `.OriginalValue`/`.DerivedValue`/`.IsCaveated` off
  the view instead of `.Caveated`/`.MeleeInSv`/`.RangedInSv` directly off `InvulnerableSave`.

## Capabilities

### Modified Capabilities
- `invulnerable-save`: "Invulnerable Save Value Shape" and "Caveated Values Always Carry Their
  Source Ability" change from being properties of `InvulnerableSave` itself to properties of the
  `InvulnerableSaveCharacteristicView` wrapping it; "Footnoted Caveat Text Resolution" is unchanged
  in its own resolution logic, only in what shape its result is now expressed through.
- `characteristic-value`: "Compound Characteristic Value Shapes" gains a confirmed second source of
  `ContributingAbilities` beyond a parse-time caveat — a live `statline-flag-rules` match — proving
  the shape covers both without a dedicated case for either.
- `live-play-view`: "Flagged Statline Characteristic Rendering" no longer names `CaveatAbility` as
  a distinct concept in its own scenario text — a caveated invulnerable save's legend source is now
  just that run's own `ContributingAbilities` entry, the same as any other flagged characteristic.
  Externally observable rendering behavior is unchanged.

## Impact

`src/ProbHammer.Core/Domain/Catalogue/InvulnerableSave.cs`,
`src/ProbHammer.Core/Domain/Catalogue/Bsdata/BsdataDatasheetMapper.cs`,
`src/ProbHammer.Core/Domain/Import/BattleScribe/BattleScribeRosterMapper.cs`,
`src/ProbHammer.Core/Domain/Roster/StatlineFlagRule.cs`,
`src/ProbHammer.Core/Domain/Roster/AttachedUnitAggregator.cs`,
`src/ProbHammer.Core/Domain/Roster/AttachedUnitAggregateView.cs` (whether
`StatlineFlagCharacteristic.InvulnerableSave` is removed or left unused),
`src/ProbHammer.Core/Domain/Examples/Datasheets.cs` (fixture construction),
`src/ProbHammer.Web/Pages/LivePlay.cshtml.cs`, `src/ProbHammer.Web/Pages/Shared/_UnitBlock.cshtml`.
Tests: `InvulnerableSaveTests.cs`, `InvulnerableSaveCaveatClassifierTests.cs` (likely unaffected —
classifier operates on raw ability text, not `InvulnerableSave` itself),
`InvulnerableSaveResolutionTests.cs`, `StatlineFlagRuleTests.cs`,
`LivePlayFlaggedStatlineRenderingTests.cs`, `LivePlayInvulnerableSaveRenderingTests.cs`,
`BattleScribeRosterMapperTests.cs`, plus the permanent corpus-scan test
`InvulnerableSaveCaveatResolutionScanTests.cs`. No BSData/BattleScribe JSON parsing logic changes —
only the shape the resolved result is packaged into.
