# characteristic-modifier-caveats

## Purpose

Applies a classified, data-derived characteristic-modifier candidate to a specific resolved unit
only when its granting selection is confirmed present on that unit, surfacing it as a caveated
CharacteristicView with no computed value — the data-derived counterpart to `statline-flag-rules`'
hand-authored ability-text rules.

## Requirements

### Requirement: Presence-Gated Application, No Unconditional Bake-In
The system SHALL apply a classified characteristic-modifier candidate to a specific resolved unit
only when that unit's currently-present components/model-lines confirm the candidate's own granting
selection is present. The system SHALL NOT apply any candidate unconditionally, regardless of the
candidate's own structural type or classification tier — there is no "always applies" path.

#### Scenario: An absent candidate produces no caveat
- **WHEN** a Datasheet exposes a characteristic-modifier candidate targeting Objective Control, and a
  resolved unit's currently-present selections do not include that candidate's granting selection
- **THEN** the unit's Objective Control is not caveated by that candidate

#### Scenario: A present candidate produces a caveat, not a resolved value
- **WHEN** a resolved unit's currently-present selections include a classified candidate's granting
  selection
- **THEN** the targeted characteristic's CharacteristicView has IsCaveated true, its
  ContributingAbilities reference the candidate's source, and its DerivedValue is absent

### Requirement: Applies Uniformly Alongside Hand-Authored Rules
The system SHALL apply classified candidates in the same aggregation pass, and under the same
liveness semantics, as `statline-flag-rules`' existing rule matches — re-evaluated on every
aggregate rebuild, never cached independently of that recomputation.

#### Scenario: A caveat disappears once its bearer becomes a casualty
- **WHEN** the specific model-line or component carrying a classified candidate's granting selection
  is removed as a casualty
- **THEN** that candidate's caveat no longer applies on the unit's next render, matching
  `statline-flag-rules`' own liveness rule

### Requirement: No Cross-Unit Application
The system SHALL apply a classified candidate only to the unit that itself carries the candidate's
own granting selection — never to a different unit elsewhere in the roster.

#### Scenario: A candidate does not affect a unit that lacks its granting selection
- **WHEN** a resolved roster contains two different units, and only one carries a classified
  candidate's granting selection
- **THEN** only the unit carrying that selection is caveated; the other unit's matching
  characteristic is unaffected
