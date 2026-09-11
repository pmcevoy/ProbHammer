## ADDED Requirements

### Requirement: Resolving An Attacks-Characteristic Effect Into A Per-Model Amount
Given a classified weapon-characteristic Effect naming the Attacks characteristic and its source
Ability, the system SHALL produce a signed per-model amount reflecting the Effect's stated Verb and
Amount, using this project's existing Improve/Worsen sign convention for a Plain-family
characteristic. This is distinct from "Resolving A Weapon-Characteristic Effect Into A Mutated
Profile", which never covers Attacks: this resolution produces a value meant to be added to a
contributor's own base Attacks value, not a mutated weapon profile.

#### Scenario: An Improve effect resolves to a positive per-model amount
- **WHEN** resolving a weapon-characteristic Effect stating an Improve of `3` against the Attacks
  characteristic
- **THEN** the resolved per-model amount is `+3`

#### Scenario: A Worsen effect resolves to a negative per-model amount
- **WHEN** resolving a weapon-characteristic Effect stating a Worsen of `1` against the Attacks
  characteristic
- **THEN** the resolved per-model amount is `-1`

#### Scenario: A Set effect is rejected rather than silently producing an amount
- **WHEN** resolving a weapon-characteristic Effect stating a Set verb against the Attacks
  characteristic
- **THEN** the system rejects the attempt rather than producing a per-model amount, matching this
  project's existing convention that a Set verb has no delta/sign concept
