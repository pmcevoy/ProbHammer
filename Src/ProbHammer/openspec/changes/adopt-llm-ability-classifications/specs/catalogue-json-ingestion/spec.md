## MODIFIED Requirements

### Requirement: No Composition, Validation, or Points Data
The system SHALL NOT interpret BSData's composition/validation apparatus (`constraints`,
`conditionGroups`, or `associations`/eligibility links) for legality-checking purposes, and SHALL
NOT extract points costs, consistent with `datasheet-catalogue`'s existing "No Wargear Constraint or
Points Modeling" requirement — this system resolves names to rules data; it does not validate that a
chosen roster is legal. A profile/entry's own `modifiers` (`BsModifier` data) ARE read, but only for
one narrow, closed purpose: hidden-gating (excluding an entry/group/rule outside a specific game mode
or chapter/sub-faction). That purpose never writes a modifier's own value onto the produced
`Datasheet`, `Statline`, or `WeaponProfile`.

#### Scenario: Composition apparatus is ignored
- **WHEN** resolving a unit entry that carries `constraints` or an `associations` block describing
  which units it may lead
- **THEN** none of that data appears on the produced `Datasheet`, `Statline`, `WeaponProfile`, or
  `Ability` objects, and no legality check is performed against it

#### Scenario: Points cost is ignored
- **WHEN** resolving a unit entry whose `costs` array includes an entry named `"pts"`
- **THEN** the produced `Datasheet` exposes no points value

#### Scenario: A modifier's own value is never written onto catalogue data
- **WHEN** resolving an entry whose `modifiers` array includes a `BsModifier` with a `Value`
- **THEN** hidden-gating does not write that `Value` onto the produced `Datasheet`'s own `Statline`
  or `WeaponProfile` fields

## REMOVED Requirements

### Requirement: Closed-World Characteristic-Modifier Classification (Tiers 1-2 Only)
**Reason**: Its only consumer was the offline `RuleEffectClassificationReport` tool's structural
cross-check, which this change deletes; nothing in the live roster path has read it since
`unify-characteristic-effect-resolution`.
**Migration**: None. Characteristic effects come from the ability-classification catalogue
(`ability-classification-catalogue`).
