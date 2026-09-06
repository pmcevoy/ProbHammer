## Why

`introduce-characteristic-domain-model` added `ScalarCharacteristicView` as a new, unconsumed
shape; `unify-invulnerable-save-characteristic-view` gave the compound half of that hierarchy
(`InvulnerableSaveCharacteristicView`) its first real consumer and, in doing so, retired InSv's own
parallel `StatlineFlag`-style side-channel for recording "which ability changed this value." Today
`Statline.Oc` is the last piece of unfinished business from that same session — it's still a plain
`int`, and Objective Control's own flagged-value fact (`VexillaStatlineFlagRule`) is still recorded
through the separate `StatlineFlag`/`StatlineFlagCharacteristic`/`AggregateStatlineEntry.Flags`
side-channel `LivePlayModel.GroupStatlines` has to branch on by hand
(`g.SelectMany(e => e.Flags).FirstOrDefault(f => f.Characteristic == StatlineFlagCharacteristic.ObjectiveControl)`),
exactly the duplication InSv used to have and no longer does. Retyping `Oc` to
`ScalarCharacteristicView` closes that gap the same way, gives `ScalarCharacteristicView` its first
real consumer (a plain, non-compound scalar, complementing InSv's compound case), and lets the
`StatlineFlag` side-channel — now unused by both of its only two consumers — be retired outright.

## What Changes

- `ScalarCharacteristicView` (`Domain.Catalogue`) gains `Resolved`/`Caveated` named static factories
  and an implicit `int` conversion, mirroring `InvulnerableSaveCharacteristicView`'s exact shape
  (via `CharacteristicValue`'s own existing implicit `int` conversion) — today it only has its bare
  positional constructor.
- `Statline.Oc`'s type changes from `int` to `ScalarCharacteristicView`. **BREAKING** for every
  reader/constructor of `Statline.Oc`.
- `VexillaStatlineFlagRule.Apply` reads the current OC as a plain int by downcasting
  `baseStatline.Oc.Value` (a `CharacteristicValue`) to `NumericCharacteristicValue` — safe because
  Objective Control has only ever been parsed as a plain int by both mapper pipelines
  (`ParsePlainInt`), never dice/symbolic — adds 1, and rebuilds `Oc` via
  `ScalarCharacteristicView.Resolved(newValue, [matchedAbility])`, so the resulting view carries
  `matchedAbility` in its own `ContributingAbilities` directly — the same pattern
  `ShieldDomeStatlineFlagRule` already uses for InSv. **BREAKING** for
  `StatlineFlagRule.Characteristic`'s existing role (see below).
- `StatlineFlag`/`StatlineFlagCharacteristic`/`AggregateStatlineEntry.Flags` and
  `AttachedUnitAggregator.ApplyStatlineFlagRules`'s flag-building branch are removed outright —
  `StatlineFlagRule.Characteristic` (already nullable, added specifically as an escape hatch for
  "still needs a `StatlineFlag`") has no remaining non-null case once Oc migrates, so the property
  itself is removed rather than left permanently null. **BREAKING** for
  `StatlineFlagRule.Characteristic`'s signature and any external override.
- `LivePlayModel.GroupStatlines`'s `ocSource` lookup (today branching on `AggregateStatlineEntry
  .Flags`) is replaced with a direct read of `statline.Oc.ContributingAbilities.FirstOrDefault()` —
  the identical mechanism already used for `insvSource`, collapsing OC and InSv onto one uniform
  code path with no per-characteristic special-casing.
- `_UnitBlock.cshtml`'s two OC stat-tile render sites read `.Oc.Value` (the resolved
  `CharacteristicValue`, rendered via its own `ToString()` override) in place of `.Oc` directly.

## Capabilities

### Modified Capabilities
- `characteristic-value`: "Characteristic Modification View" gains a confirmed first real
  `ScalarCharacteristicView` consumer (Objective Control) alongside the existing compound
  (invulnerable-save) case, proving the shape covers a plain scalar with no dedicated case needed.

## Impact

`src/ProbHammer.Core/Domain/Catalogue/CharacteristicView.cs`,
`src/ProbHammer.Core/Domain/Catalogue/Statline.cs`,
`src/ProbHammer.Core/Domain/Roster/StatlineFlagRule.cs`,
`src/ProbHammer.Core/Domain/Roster/AttachedUnitAggregator.cs`,
`src/ProbHammer.Core/Domain/Roster/AttachedUnitAggregateView.cs` (removes `StatlineFlag`/
`StatlineFlagCharacteristic`, and `AggregateStatlineEntry.Flags`),
`src/ProbHammer.Core/Domain/Examples/Datasheets.cs` (fixture construction),
`src/ProbHammer.Web/Pages/LivePlay.cshtml.cs`, `src/ProbHammer.Web/Pages/Shared/_UnitBlock.cshtml`.
Tests: `StatlineFlagRuleTests.cs`, `LivePlayFlaggedStatlineRenderingTests.cs`, and any other test
constructing a `Statline` with a plain-int `Oc` or asserting `.Oc` directly against an `int`. No
BSData/BattleScribe JSON parsing logic changes — Objective Control is never footnoted/caveated at
parse time (unlike InSv), so both mapper pipelines only need their plain-int `Oc` construction
sites updated to go through the new implicit conversion, not a resolution-logic change.

Explicitly out of scope: the general modification/legality engine (Set/Multiply/Add/Divide/
Subtract/round-up ordering, per-characteristic clamp bounds — see `.claude/vnext-ideas.md`'s
"Characteristic-modification domain hardening" entry), `CharacteristicModifier`/mutator-rule
generalization, `WeaponProfile.S`/`Ap` dice-notation support, and Set-vs-Set conflict handling —
none of these have a real caller yet, since only Vexilla (Oc) and Shield Dome (InSv) exist today
with no overlap.
