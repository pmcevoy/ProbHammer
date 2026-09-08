## 1. Kind Classification and Lookup Tables

- [x] 1.1 Add `CharacteristicModificationKind` enum (`RollThreshold`, `ArmourPenetration`, `Plain`)
      under `ProbHammer.Core.Domain.Catalogue`.
- [x] 1.2 Add a closed characteristic-name → `CharacteristicModificationKind` lookup table covering
      WS, BS, Sv, Ld (`RollThreshold`), AP (`ArmourPenetration`), and M, T, W, Oc, S, and the
      length-valued Range characteristic (`Plain`). InSv and Attacks/Damage are deliberately excluded
      — see `design.md`'s Context/Non-Goals.
- [x] 1.3 Add a separate closed characteristic-name → clamp-bound lookup table (nullable
      floor/ceiling pair) per the rulebook table in `design.md`/`.claude/vnext-ideas.md`: Sv floor 2;
      Ld [5,8]; WS/BS [2,6]; Oc floor 0; AP ceiling 0; M/T/S/Range floor 1.

## 2. Arithmetic Functions

- [x] 2.1 Implement `ResolveDelta(CharacteristicModificationKind kind, EffectVerb verb, int amount)
      -> int` per the per-family sign rules (`RollThreshold`/`ArmourPenetration` invert
      `Improve`/`Worsen`; `Plain` does not). `Set` is not resolved by this function — callers skip it
      entirely and use the stated amount directly.
- [x] 2.2 Implement `Clamp(string characteristicName, int value) -> int` using the clamp-bound table
      from 1.3, silently capping rather than throwing.
- [x] 2.3 Implement the orchestrating entry point that takes a characteristic name, its current
      `CharacteristicValue`, an `EffectVerb`, and an amount, and returns the resolved
      `CharacteristicValue` — returning the input unchanged when it's the symbolic variant, otherwise
      resolving via 2.1 (skipped for `Set`) and clamping via 2.2, then rewrapping as a
      `NumericCharacteristicValue`.

## 3. Tests

- [x] 3.1 Unit tests for `ResolveDelta` covering all nine (family × verb) combinations from the
      rulebook's worked examples (WS 3+ improved/worsened by 1; AP -1 improved/worsened by 1; S 4
      improved/worsened by 1), including the `Set`-skips-sign-resolution case.
- [x] 3.2 Unit tests for `Clamp` covering every bound in the table: AP worsened past 0; Sv improved
      past 2+; Ld improved past 5+ and worsened past 8+; WS/BS improved past 2+ and worsened past 6+;
      Oc worsened past 0; a Plain characteristic (e.g. Movement) worsened past its floor of 1.
- [x] 3.3 Unit test confirming a symbolic `CharacteristicValue` ("-", "*", "N/A") is returned
      unchanged by the orchestrating entry point regardless of verb/characteristic/amount.
- [x] 3.4 Proving test: resolving an `Improve Oc 1` effect (Vexilla's classified effect) through the
      orchestrating entry point against a model's base Oc produces the identical resolved value
      `VexillaStatlineFlagRule.Apply` already produces for a unit carrying that ability.

## 4. Documentation

- [x] 4.1 Add a "Characteristic Modification Kind" section to `.claude/domain-model-11e.md` (or fold
      into the existing "Characteristic-Modifier Caveats"/"Characteristic Value Domain Model"
      sections, whichever reads more coherently) documenting the new types, their scope (including
      the InSv/Attacks/Damage exclusion), and that nothing consumes them yet.
- [x] 4.2 Update `.claude/vnext-ideas.md`'s "Characteristic-modification domain hardening" entry to
      record that the `Kind`/`ResolveDelta`/`Clamp` layer of the sketched Modification Engine now
      exists for the plain-scalar characteristics and is proven against Vexilla, while InSv coverage,
      the grouping engine, `Set`-vs-`Set` resolution, and multi-characteristic ability support above
      it remain unbuilt.
