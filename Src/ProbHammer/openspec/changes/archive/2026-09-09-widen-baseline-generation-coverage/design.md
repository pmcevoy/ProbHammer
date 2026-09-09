## Context

See proposal.md - Why for motivation. Current state this design has to work with:

- `RuleEffectClassifier`'s `InvulnerableSaveRangedRestricted`/`InvulnerableSaveMeleeRestricted`
  patterns (`resolve-invulnerable-save-effects`) anchor on a subject-shaped prefix followed by the
  literal word `" has "` — confirmed by direct inspection, there is no alternative branch for a
  plural subject anywhere in either pattern. `InvulnerableSaveCaveatClassifier` (mapper-time, the
  component this whole effort is working toward retiring) already recognizes both forms as two of
  its four fixed templates.
- `RuleEffectClassificationReport` (`tools/RuleEffectClassificationReport/Program.cs`) walks every
  BSData catalogue file, builds each entry's `Datasheet`, and collects every `Ability` Name+Text pair
  it can reach — `Datasheet.Abilities` (always-enumerated) and `Datasheet.OptionalAbilityNames`/
  `TryResolveAbility` (Enhancement/OptionalGrant abilities, e.g. Auric Mantle). It groups everything
  by `RuleEffectClassifier.Normalize`d Text and classifies each group once. It has no awareness of
  `Datasheet.CharacteristicModifierCandidates` at all today.
- `CharacteristicModifierCandidate(EntryName, Characteristic, RawValue)` is built by
  `BsdataDatasheetMapper.ClassifyCharacteristicModifierCandidates`, one per recognized, tier-1/tier-2
  `BsModifier` on an entry. **It does not currently carry the modifier's own raw operation
  (`BsModifier.Type` — `"increment"`/`"decrement"`/`"set"`)** — only the target characteristic and
  the raw stated value. Deriving a rulebook `EffectVerb` needs that operation.
- `CharacteristicModificationKind`/`CharacteristicModificationResolver` already classify each
  characteristic into an arithmetic family (`RollThreshold`/`ArmourPenetration`/`Plain`) and resolve
  a rulebook verb (`Improve`/`Worsen`) plus amount into a signed delta on the *stored* value — but
  only in that one direction (verb → delta). This design needs the reverse (raw stored-value delta →
  verb).

## Goals / Non-Goals

**Goals:**
- Close the plural ("have") gap in `RuleEffectClassifier`'s InSv patterns so the follow-up
  consolidation change can retire `InvulnerableSaveCaveatClassifier` without regressing any real
  corpus unit.
- Give the offline report tool a second, structural way to propose a baseline Effect, sourced from
  BSData's own structured data rather than prose, for entries where that's the only (or most
  reliable) signal.
- Keep this entirely an offline/report-tool/baseline-content change — no behavior change reachable
  from a running `/LivePlay` session.

**Non-Goals:**
- Consuming a structurally-derived Effect anywhere at runtime (see proposal.md's explicit
  out-of-scope list).
- Changing `ApplyCharacteristicModifierCandidates`'s own live application logic. Widening
  `CharacteristicModifierCandidate`'s own shape (see Decision 1) is in scope; changing what
  `AttachedUnitAggregator` does with it is not — that method keeps ignoring every field it doesn't
  already read, the same way it already ignores `RawValue`.
- Building a general "any BSData modifier shape" derivation — only the same tier-1/tier-2,
  recognized-field candidates `CharacteristicModifierCandidate` already restricts itself to.
- Resolving a `Set`-type raw modifier's own arithmetic ambiguity — a raw `"set"` operation has no
  Improve/Worsen sign question at all; it maps directly to the rulebook `Set` verb with its own
  stated value as the amount.

## Decisions

### 1. Widen `CharacteristicModifierCandidate` to also carry the modifier's raw `Type`
Add a `RawType` (or similarly named) field carrying `BsModifier.Type` verbatim (`"increment"`/
`"decrement"`/`"set"`), populated by `ClassifyCharacteristicModifierCandidates` alongside the fields
it already sets. This is additive and non-breaking — every existing consumer (`AttachedUnitAggregator
.ApplyCharacteristicModifierCandidates`, which only reads `EntryName`/`Characteristic`) keeps working
unmodified, exactly as it already does with the still-unused `RawValue`. **Alternative considered**:
have the new derivation code re-read the raw `BsSelectionEntry.Modifiers` directly instead of going
through `Datasheet.CharacteristicModifierCandidates`. Rejected — `CharacteristicModifierCandidate` is
already the tested, tier-1/tier-2-filtered, field-recognized view of exactly the modifiers worth
deriving from; re-deriving that filtering a second time in the report tool would duplicate real,
already-proven logic (`IsTier1OrTier2`, `CharacteristicFieldIds`) rather than reuse it.

### 2. Join an ability to its own candidate by `EntryName` == `Ability.Name`, at collection time, mirroring the existing runtime join
`ApplyCharacteristicModifierCandidates` already joins a candidate to its granting ability by
`string.Equals(ability.Name, candidate.EntryName, StringComparison.OrdinalIgnoreCase)` — the same
join the report tool needs, just performed once per corpus entry during collection (inside the
existing `foreach (var entry in closure.Files[0].Catalogue.SharedSelectionEntries)` loop) rather than
per resolved roster unit. For each ability collected from an entry (`Datasheet.Abilities` or a
resolved `OptionalAbilityNames` entry), look up any of that same `Datasheet`'s
`CharacteristicModifierCandidates` whose `EntryName` matches the ability's own `Name`
case-insensitively, and carry the match(es) alongside that Text's existing `Names`/`Locations`
accumulation (a `TextOccurrence.StructuralCandidates` set, deduplicated the same way `Names` already
is). **Alternative considered**: join by id instead of Name. Rejected — `CharacteristicModifierCandidate`
only ever carried `EntryName` (a Name), matching the exact ambiguity `ApplyCharacteristicModifierCandidates`
already accepts at runtime; introducing an id-based join here would diverge from the semantics this
change is trying to mirror, not just relocate.

### 3. The raw-operation → rulebook-verb inverse is a small, explicit function, not a reuse of `ResolveDelta`
`CharacteristicModificationResolver.ResolveDelta(kind, verb, amount)` is a `(kind, verb) → signed
delta` function; this design needs `(kind, signed delta) → verb` — genuinely the inverse, not a
different call pattern into the same function. A new `ResolveVerbFromRawDelta(kind, delta) ->
(EffectVerb, int amount)` sits alongside it in the same file, using the same three-family switch:
- `Plain`: `delta > 0` → `Improve` `delta`; `delta < 0` → `Worsen` `-delta` (sign carries straight
  through — this is the Auric Mantle case: raw `increment` by `2` on `W` is `Improve W 2`).
- `RollThreshold`/`ArmourPenetration`: sign is inverted relative to `Plain` — `delta > 0` → `Worsen`
  `delta`; `delta < 0` → `Improve` `-delta` (raising the stored number is worse for both families).

A raw `"increment"` op contributes `+RawValue` as its delta; `"decrement"` contributes `-RawValue`.
A raw `"set"` op is handled separately and never reaches this function — it produces `Set` directly
with `RawValue` as the amount, no sign resolution needed (mirrors
`CharacteristicModificationResolver.Resolve`'s own existing `Set`-is-different-from-`Improve`/
`Worsen` handling). A `Type` this function doesn't recognize (anything other than `"increment"`/
`"decrement"`/`"set"`) is left undertived, failing closed — the corpus-scan run (task, below) will
confirm what's actually real before this ships, rather than guessing at a shape not yet seen.

### 4. Disagreement is its own report section, not a hard failure
When both a text-classified and a structurally-derived Effect exist for the same characteristic on
the same Text and they don't match, the report surfaces both side by side rather than either failing
the run or silently picking one. **Alternative considered**: prefer the structural derivation
unconditionally, on the theory that structured data is more reliable than regex. Rejected — a
disagreement is exactly the situation most worth a human's attention (either the regex has a real
gap, or the structural read of `Type`/`Value`/tier is subtly wrong for that entry), and this
project's own established precision-over-recall stance means a live disagreement gets looked at, not
auto-resolved either direction.

## Risks / Trade-offs

- **[Risk]** The "have"/plural pattern widening could over-match some prose shape not yet seen in the
  corpus (the same risk every prior widening of these patterns has carried).
  → **Mitigation**: same corpus-scan-and-review discipline every prior `RuleEffectClassifier` change
  has used — run against the full live BSData clone, manually review every newly-surfaced Effect
  result before checking in a baseline change.
- **[Risk]** The raw-operation → verb inverse function is new, untested-in-production logic layered
  on top of `CharacteristicModificationKind`'s existing families.
  → **Mitigation**: unit-test it directly against known values (Auric Mantle's real `increment W 2`
  → `Improve W 2`, plus a hand-built `RollThreshold`/`ArmourPenetration` case confirming the sign
  inversion) before trusting any corpus-derived output from it.
- **[Risk]** Widening `CharacteristicModifierCandidate`'s shape touches a type the live runtime path
  also constructs and reads, even though this change doesn't change that path's behavior.
  → **Mitigation**: purely additive field, defaulted/populated at the one construction site
  (`ClassifyCharacteristicModifierCandidates`); existing tests for the live path keep passing
  unmodified since they never read the new field.

## Migration Plan

Single-PR, tool-and-data-only change — no runtime code path is touched, so there's no deployment
sequencing concern beyond the usual build-and-test:
1. Widen `RuleEffectClassifier`'s two restricted patterns; add/adjust unit tests for the plural form.
2. Widen `CharacteristicModifierCandidate` with the raw `Type` field; add the
   `ResolveVerbFromRawDelta` inverse function and its own unit tests.
3. Add the join-and-derive step plus the disagreement-report section to
   `RuleEffectClassificationReport`.
4. Run the report against the full live BSData clone, review every newly-surfaced Effect result and
   every disagreement, and grow the checked-in baseline via `--write-baseline` plus any genuinely new
   entries added by hand (per the existing baseline workflow's own "a genuinely new entry must first
   be added to the checked-in JSON by hand" convention).

**Rollback**: revert the commit — no runtime behavior, no schema/data migration beyond the baseline
JSON itself, which is just checked-in data.

## Open Questions

- Exact set of raw `Type` values the live corpus actually uses on a tier-1/tier-2, recognized-field
  modifier, beyond the confirmed `"increment"`/`"set"` — doesn't change the approach (unrecognized
  types fail closed either way), safe to confirm during the corpus-scan step rather than up front.
