## ADDED Requirements

### Requirement: Army Roster Container
An ArmyRoster SHALL wrap an army list's per-unit roster together with army-level metadata that no individual Unit or AttachedUnit carries: the list's Name, PointsSpent, Faction (one or more ordered names), Detachments (one or more ordered names), ForceDisposition, BattleSize, and PointsLimit.

#### Scenario: Units collection is unchanged from the existing roster
- **WHEN** an ArmyRoster is built from an existing per-unit roster
- **THEN** its Units collection contains exactly those ICombatUnit instances, unmodified

#### Scenario: Single-faction army
- **WHEN** an ArmyRoster is built for an army with no sub-faction (e.g. a Chaos Space Marines list)
- **THEN** its Faction contains exactly one name

#### Scenario: Sub-faction army
- **WHEN** an ArmyRoster is built for an army whose faction is a sub-faction of a parent codex (e.g. Black Templars under Space Marines)
- **THEN** its Faction contains the parent codex name followed by the sub-faction name, in that order

#### Scenario: Multiple detachments
- **WHEN** an ArmyRoster is built for an army that selected more than one detachment
- **THEN** its Detachments contains one entry per selected detachment, in selection order

#### Scenario: Points spent is independent of the unit roster
- **WHEN** an ArmyRoster's Units collection is read
- **THEN** PointsSpent is not derived from or validated against the points of any Unit or AttachedUnit in Units, since no points value is tracked anywhere below ArmyRoster
