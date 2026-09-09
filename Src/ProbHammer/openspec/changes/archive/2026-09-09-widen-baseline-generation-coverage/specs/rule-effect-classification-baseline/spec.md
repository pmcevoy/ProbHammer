## ADDED Requirements

### Requirement: Structural Derivation From Characteristic-Modifier Data
The system SHALL provide a second way to derive a candidate classification for a corpus entry's own
Text, alongside text-only classification: when that entry carries a structured, unconditional (or
locally-conditional) characteristic-modifier candidate targeting a recognized `Statline` field, the
system SHALL derive a characteristic Effect directly from that candidate's own structured data — the
targeted field, the raw modifier operation, and the raw stated amount — with no dependency on parsing
the entry's own prose Text. This derivation SHALL resolve the modifier's raw operation into the
correct rulebook verb (`Improve`/`Worsen`/`Set`) for the targeted characteristic's own arithmetic
family, never assume a fixed mapping independent of which characteristic is targeted.

#### Scenario: A structural candidate on a Plain characteristic derives directly
- **WHEN** a corpus entry carries a tier-1 characteristic-modifier candidate that adds a fixed amount
  to a Plain-family characteristic (e.g. an Enhancement that structurally increments Wounds by 2)
- **THEN** a `ScalarCharacteristicEffect` is derived for that characteristic: `Improve` by that same
  amount

#### Scenario: A structural candidate on a roll-threshold characteristic inverts correctly
- **WHEN** a corpus entry carries a tier-1 characteristic-modifier candidate whose raw operation adds
  to the stored value of a roll-threshold-family characteristic (where a numerically higher stored
  value is a worse in-game outcome)
- **THEN** the derived Effect uses the rulebook verb `Worsen`, not `Improve`, reflecting that raising
  the stored number makes the characteristic worse for that family

#### Scenario: A candidate gated by an unrecognized condition is not derived from
- **WHEN** a corpus entry's own characteristic-modifier candidate is not classified as present (its
  granting condition is not one this system's existing tier-1/tier-2 recognition covers)
- **THEN** no structural Effect is derived for that entry from this requirement

### Requirement: Regex And Structural Derivations Are Cross-Checked
When a corpus entry's own Text yields both a text-classified Effect (from unconditional characteristic
effect extraction) and a structurally-derived Effect (from characteristic-modifier data) for the same
characteristic, the system SHALL compare the two. Agreement SHALL require no special handling beyond
normal classification. Disagreement SHALL be surfaced in its own distinct, reviewable report listing,
never silently resolved by preferring one source over the other.

#### Scenario: Agreeing derivations require no special review
- **WHEN** a corpus entry's text-classified Effect and its structurally-derived Effect state the same
  characteristic, verb, and amount
- **THEN** the entry is not placed in the disagreement listing

#### Scenario: Disagreeing derivations are surfaced together
- **WHEN** a corpus entry's text-classified Effect and its structurally-derived Effect state
  different verbs or amounts for the same characteristic
- **THEN** the entry is surfaced in a dedicated disagreement listing showing both derivations, and
  neither is silently preferred over the other

#### Scenario: An entry with only one kind of derivation is unaffected
- **WHEN** a corpus entry yields a structurally-derived Effect but no text-classified Effect for the
  same characteristic, or vice versa
- **THEN** the entry is not placed in the disagreement listing
