## Why

`Statline`/`WeaponProfile` represent a "characteristic" ad hoc today — six plain `int` properties
on `Statline`, and an inconsistent mix on `WeaponProfile` (`A`/`D` are `DiceExpression`, `S`/`Ap`
are plain `int`, even though a real corpus scan already found dice-notation `S`). There is no
shared representation for a non-numeric (`-`/`*`/`N/A`) value, no way to retain a characteristic's
original catalogue value alongside a value abilities have modified, and no way to record which
abilities are known to modify a given characteristic — today's `InvulnerableSave.Caveated`/
`CaveatAbility` is the one hand-rolled example of this, scoped to exactly one characteristic.

This blocks two things directly: safely growing `StatlineFlagRuleCatalogue` past its current two
hand-written rules (no shared original-value/contributing-ability shape to build on), and any
future UI that shows a modified characteristic's original value on hover plus the ability(ies)
responsible — generalizing what the InSv caveat legend already does for one case today.

## What Changes

- Introduce a closed `CharacteristicValue` hierarchy (numeric / dice / symbolic) as the one shared
  raw-value representation a characteristic can hold, replacing the inconsistent per-field
  int/`DiceExpression` choice on `WeaponProfile` and the plain-int-only fields on `Statline`.
- Introduce a generic `CharacteristicView<T>` wrapper capturing, per characteristic property: its
  original (unmodified, catalogue) value, the abilities classified as touching it, a derived value
  (present only when every contributing ability is classified deterministic), and a caveated flag
  (true the moment even one contributing ability isn't).
- Formalize per-kind compound value shapes for characteristics whose value isn't a single scalar —
  `InvulnerableSave` (existing melee/ranged split) becomes the first confirmed instance of this
  pattern rather than a one-off; a new shape is added for a Movement characteristic's minimum-move
  value (relevant to `Fly`).
- **Purely additive.** New types only, added in isolation with no consumers — `Statline`,
  `WeaponProfile`, `StatlineFlagRule`, `AttachedUnitAggregator`, and every render path are
  unchanged. Wiring these types into the existing model is deliberately out of scope for this
  change, to allow the shape to be reviewed before any refactor begins.

## Capabilities

### New Capabilities
- `characteristic-value`: Core domain types representing a characteristic's underlying value, its
  pre/post-modification state, and the abilities associated with modifying it.

### Modified Capabilities

(none — this change adds new, unconsumed types only; no existing capability's observable behavior
changes)

## Impact

New files only, under `ProbHammer.Core/Domain/Catalogue/` (exact location TBD in design.md). No
existing source file is modified. No test suite currently exercises these types since nothing
consumes them yet; new unit tests cover the types themselves in isolation.
