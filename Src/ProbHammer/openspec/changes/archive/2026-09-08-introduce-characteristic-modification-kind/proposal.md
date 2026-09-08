## Why

`rule-effect-classification` extracts an `Improve`/`Worsen`/`Set` verb plus a fixed amount from
rule/ability text, but deliberately never resolves that verb into a signed number — the rulebook's
own Improve/Worsen arithmetic differs by characteristic family (a roll-threshold like Sv "improves"
by *subtracting*; AP "improves" by subtracting too but "worsens" is capped at 0; every other
characteristic is a plain add/subtract), and no clamp bounds are enforced anywhere. Today the only
things that actually compute a resolved value are two hand-authored `StatlineFlagRule`s (Shield
Dome, Vexilla) that embed this arithmetic ad hoc, one rule at a time, with no shared, tested
sign/clamp logic behind either. Before `rule-effect-classification`'s output can be trusted to
replace or extend that hand-authored pair, the arithmetic itself needs to exist as an independent,
tested unit — proven to reproduce what the two existing rules already do correctly, not adopted on
faith.

## What Changes

- Introduce a closed `CharacteristicModificationKind` concept (`RollThreshold`, `ArmourPenetration`,
  `Plain`) that resolves an `EffectVerb` (`Improve`/`Worsen`/`Set`) plus a signed amount into the
  correct delta for a characteristic's own arithmetic family, per the rulebook text already
  transcribed in `.claude/vnext-ideas.md`.
- Add a per-characteristic clamp table (bounds for `M`/`T`/`Sv`/`InSv`/`W`/`Ld`/`Oc`/`WS`/`BS`/`AP`/
  `A`/`D`/Range) and apply it after a delta is resolved, so a mutated value can never leave its
  rulebook-legal range.
- Add a proving test suite that feeds `CharacteristicEffect`s equivalent to Shield Dome's ("Set InSv
  5") and Vexilla's ("Improve Oc 1") through the new `Kind` resolver and asserts the result matches
  what `ShieldDomeStatlineFlagRule`/`VexillaStatlineFlagRule` already produce today — a regression
  proof, not a replacement wiring.
- No change to `AttachedUnitAggregator`, `StatlineFlagRuleCatalogue`, or any `/LivePlay` rendering —
  the new `Kind` resolver is added as a standalone, unconsumed component, mirroring how
  `rule-effect-classification` itself shipped unwired.

## Capabilities

### New Capabilities
- `characteristic-modification-kind`: the arithmetic-family sign resolution (`ResolveDelta`) and
  per-characteristic clamp table for applying an `Improve`/`Worsen`/`Set` effect to a characteristic
  value.

### Modified Capabilities
(none — `statline-flag-rules` and `rule-effect-classification` are read for context and proven
against, but neither's requirements change)

## Impact

- New code only, under `ProbHammer.Core.Domain.Catalogue` (mirrors where `CharacteristicValue`/
  `CharacteristicView`/`RuleEffectClassifier` already live) — no existing call site is touched.
- New unit tests proving the two real hand-authored rules' own arithmetic is reproduced, plus
  coverage of the AP-worsen-caps-at-0 and Sv/Ld/WS/BS clamp-bound edge cases from the rulebook text.
- Explicitly out of scope, left for later changes: wiring `RuleEffectClassifier`'s output into
  `AttachedUnitAggregator` for real; Marshal's Household or any other roster-wide/keyword-targeted
  effect (a different application shape — `AttachedUnitAggregator` only ever sees one `ICombatUnit`
  at a time, with no cross-unit visibility); tier 3+ (conditional) effects; computing a
  `DerivedValue` for `characteristic-modifier-caveats`' candidates; Set-vs-Set conflict resolution
  and multi-characteristic ability support (both already have a worked-out answer in
  `.claude/vnext-ideas.md`, but no real caller needs them yet).
