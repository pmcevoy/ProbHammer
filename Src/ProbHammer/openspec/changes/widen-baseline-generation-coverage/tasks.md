## 1. Widen invulnerable-save plural coverage

- [ ] 1.1 Widen `RuleEffectClassifier.InvulnerableSaveRangedRestricted`/`InvulnerableSaveMeleeRestricted`
      to also match a plural subject-verb lead-in ("Models in this unit have..."), alongside the
      existing singular ("This model has...") — mirroring `InvulnerableSaveCaveatClassifier`'s own
      bare/unit template pairing.
- [ ] 1.2 Add unit tests covering the plural form for both the ranged- and melee-restricted patterns,
      plus a regression test confirming the singular form's existing behavior is unchanged.

## 2. Widen `CharacteristicModifierCandidate` and add the raw-operation → verb inverse

- [ ] 2.1 Add a `RawType` field to `CharacteristicModifierCandidate`, populated from `BsModifier.Type`
      in `ClassifyCharacteristicModifierCandidates` alongside its existing fields (design.md Decision
      1) — purely additive, no change to any existing consumer.
- [ ] 2.2 Add `CharacteristicModificationResolver.ResolveVerbFromRawDelta(kind, delta) ->
      (EffectVerb, int amount)` — the inverse of `ResolveDelta`, per design.md Decision 3. Handle
      `"increment"`/`"decrement"` raw types by computing a signed delta and calling this; handle
      `"set"` separately (direct `Set` verb, no sign resolution); leave any other raw type
      unclassified.
- [ ] 2.3 Unit-test `ResolveVerbFromRawDelta` directly: a `Plain`-family case (Auric Mantle's real
      `increment W 2` → `Improve W 2`), and a `RollThreshold`/`ArmourPenetration`-family case
      confirming the sign inversion.

## 3. Structural derivation in the report tool

- [ ] 3.1 In `RuleEffectClassificationReport`, join each collected ability to its own Datasheet's
      `CharacteristicModifierCandidates` by `EntryName == Ability.Name` (case-insensitive), at
      collection time, per design.md Decision 2 — extend `TextOccurrence` to carry any matched
      candidate(s) alongside its existing `Names`/`Locations`.
- [ ] 3.2 Derive a `CharacteristicEffect` from each joined candidate via the task 2 machinery, and add
      a new report section listing structurally-derived results.
- [ ] 3.3 Add the regex-vs-structural disagreement section: for a Text with both a text-classified and
      a structurally-derived Effect on the same characteristic, list it separately when they disagree;
      confirm agreement requires no extra listing (design.md Decision 4).

## 4. Corpus run and baseline growth

- [ ] 4.1 Run the report tool against the full live BSData clone; confirm which raw `Type` values
      actually occur on recognized-field modifiers (design.md's Open Question) and extend task 2.2's
      handling if a real, recognized-but-unhandled shape turns up.
- [ ] 4.2 Manually review every newly-surfaced plural-InSv Effect result, structurally-derived Effect
      result, and any regex-vs-structural disagreement.
- [ ] 4.3 Check in the grown baseline (`src/ProbHammer.Web/Data/RuleEffectClassifications.json`) via
      `--write-baseline` plus any genuinely new entries added by hand, confirming Auric Mantle and
      Consecrating Aura (or their real equivalents found in this run) are captured correctly.

## 5. Tests and verification

- [ ] 5.1 Run the full test suite and confirm it's green — this change touches no runtime consumer, so
      no existing behavioral test should need updating.
- [ ] 5.2 Confirm `openspec validate --strict` passes for the updated `rule-effect-classification`/
      `rule-effect-classification-baseline` deltas.
