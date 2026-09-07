## ADDED Requirements

### Requirement: On-Demand Characteristic-Modifier Candidate Exposure
A Datasheet SHALL expose its classified characteristic-modifier candidates on demand, never as part
of its always-enumerated Statlines or Abilities — mirroring the existing on-demand pattern for
optional abilities and weapon profiles. A candidate SHALL describe its own granting selection, its
targeted characteristic, and its modifier data, and SHALL NOT be applied to the Datasheet's own
Statline or WeaponProfile fields — a Datasheet's own base characteristic values remain exactly as
they would if no candidate existed.

#### Scenario: A candidate is retrievable without altering the Datasheet's base values
- **WHEN** a Datasheet has one or more classified characteristic-modifier candidates
- **THEN** its own Statline and WeaponProfile fields are unchanged from their unmodified catalogue
  values, and each candidate is retrievable through its own on-demand accessor

#### Scenario: A Datasheet with no classified candidates exposes none
- **WHEN** none of a Datasheet's selection entries carry a classifiable characteristic-modifier
- **THEN** its on-demand candidate exposure is empty
