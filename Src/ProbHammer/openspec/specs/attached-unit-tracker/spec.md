# attached-unit-tracker

## Purpose

Tracks live game state for an Attached Unit (casualties, and the aggregate statline/weapon/ability
view derived from its currently-present components) as play proceeds. TBD: expand as this
capability grows beyond its initial domain-model scope.

## Requirements

### Requirement: Model-Line Remaining Count
Each model-line SHALL track a remaining count, initialized to the model-line's original Count, which can be decremented to reflect casualties down to a minimum of zero.

#### Scenario: Decrementing a squad model-line
- **WHEN** a model-line with an original Count of 5 has 2 models removed as casualties
- **THEN** its remaining count is 3

#### Scenario: Removing a single-model line
- **WHEN** a model-line with an original Count of 1 (e.g. an attached Leader) is removed as a casualty
- **THEN** its remaining count is 0

#### Scenario: Remaining count cannot go negative
- **WHEN** a model-line's remaining count is already 0
- **THEN** further removal attempts do not decrement it below 0

### Requirement: Aggregate Statline View
The Attached Unit aggregate view SHALL list every distinct statline referenced by any component Unit's model-lines that still has a remaining count greater than zero.

#### Scenario: Statline dropped once its models are gone
- **WHEN** every model-line referencing a particular statline reaches a remaining count of 0
- **THEN** that statline no longer appears in the aggregate view

### Requirement: Aggregate Weapon Count View
The Attached Unit aggregate view SHALL aggregate weapons across all component Units by structural profile equality (matching weapon Type, Skill, Strength, AP, Damage, and Abilities, excluding count), summing the remaining counts of all model-lines carrying that profile.

#### Scenario: Same weapon profile from different components is combined
- **WHEN** the Bodyguard unit has 4 models carrying a weapon profile and the attached Leader carries a wargear item with an identical structural profile
- **THEN** the aggregate view shows one combined entry with a total count of 5

#### Scenario: Weapon count reflects casualties
- **WHEN** 2 of the 4 Bodyguard models carrying a given weapon profile are removed as casualties
- **THEN** the aggregate view's count for that weapon profile decreases by 2

### Requirement: Aggregate Ability View
The Attached Unit aggregate view SHALL combine all Unit-scoped Abilities from every currently-present component Unit into a single list, and SHALL display Model-scoped Abilities against their originating model-line rather than in the combined list.

#### Scenario: Unit-scoped ability appears in the combined list
- **WHEN** a component Unit has a Unit-scoped Ability
- **THEN** that Ability appears in the aggregate view's combined abilities list

#### Scenario: Model-scoped ability stays with its model
- **WHEN** a component Unit's model-line has a Model-scoped Ability (e.g. granted by an Enhancement)
- **THEN** that Ability appears attached to that specific model-line in the aggregate view, not in the combined list

#### Scenario: Model-scoped ability disappears when its bearer is removed
- **WHEN** the model-line bearing a Model-scoped Ability has its remaining count reduced to 0
- **THEN** that Ability no longer appears anywhere in the aggregate view
