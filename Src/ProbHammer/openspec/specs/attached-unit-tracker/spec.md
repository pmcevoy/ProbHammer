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
The Attached Unit aggregate view SHALL list one entry per distinct statline name referenced by any
present component Unit's model-lines, for as long as at least one model-line sharing that name has
a remaining count greater than zero, with per-name grouping scoped to a single component Unit's
own model-lines — two different component Units are never merged into one entry even if their
model-lines happen to share a statline name. Each entry SHALL report a remaining count and an
initial count, both summed across every model-line of that component sharing that statline name,
and SHALL list a loadout breakdown containing one entry per model-line sharing that name, each with
its own weapon list, remaining count, and initial count.

Entries SHALL be ordered by component display order, then by that component's Datasheet-declared
statline order: for an AttachedUnit, each Attached Unit's entries render in the AttachedUnit's
Attached-list order, followed by the Bodyguard's entries; for a plain Unit, its own entries render
in its Datasheet's declared order.

#### Scenario: Statline dropped once its models are gone
- **WHEN** every model-line referencing a particular statline name reaches a remaining count of 0
- **THEN** that statline no longer appears in the aggregate view

#### Scenario: Statline survives while any one of its model-lines has models remaining
- **WHEN** a statline name is shared by two model-lines of the same component Unit, and one of them reaches a remaining count of 0 while the other still has models remaining
- **THEN** that statline still appears in the aggregate view, with its remaining count reflecting only the surviving model-line(s)

#### Scenario: Initial count reflects starting strength, not just surviving model-lines
- **WHEN** a statline name is shared by two model-lines of the same component Unit with initial counts of 2 and 3, and the model-line with an initial count of 2 is fully removed as casualties
- **THEN** the aggregate view's initial count for that statline name is still 5 (2 + 3), while its remaining count is 3

#### Scenario: Loadout breakdown lists every model-line sharing a statline name, including a fully-removed one
- **WHEN** a statline name is shared by two model-lines of the same component Unit with different weapon selections, and one of them has been fully removed as casualties
- **THEN** the loadout breakdown includes both model-lines: the removed one showing a remaining count of 0 alongside its original initial count, and the other showing its own current remaining/initial counts

#### Scenario: Distinct statline names with identical values are not collapsed
- **WHEN** two model-lines reference different statline names whose M/T/Sv/W/Ld/Oc values happen to be identical
- **THEN** the aggregate view lists them as two separate statline entries, not merged into one

#### Scenario: Two components sharing a statline name are not merged
- **WHEN** two different component Units of an AttachedUnit (e.g. the Bodyguard and an attached
  Leader) each have a model-line referencing the same statline name
- **THEN** the aggregate view lists them as two separate entries, one per component, each summed
  only from that component's own model-lines — not combined into a single entry

#### Scenario: Attached components render before the Bodyguard, each in declared order
- **WHEN** an AttachedUnit has one or more Attached units and a Bodyguard, and each component's
  Datasheet declares its statlines with the Sergeant/leader-equivalent entry first
- **THEN** the aggregate view's Statlines list renders each Attached unit's entries (in the
  AttachedUnit's Attached-list order) before the Bodyguard's entries, and within each component's
  entries, the Sergeant/leader-equivalent entry renders before the rest, in that component's
  Datasheet-declared order

#### Scenario: A plain Unit's statlines follow its own Datasheet-declared order
- **WHEN** a plain Unit's Datasheet declares its Sergeant-equivalent statline first, followed by
  the rank-and-file statline
- **THEN** the aggregate view's Statlines list renders the Sergeant-equivalent entry first

### Requirement: Aggregate Weapon Count View
The Attached Unit aggregate view SHALL aggregate weapons across all component Units by structural profile equality (matching weapon Type, Skill, Strength, AP, Damage, and every ability/keyword flag the weapon carries — including but not limited to Torrent, Blast, Melta, Rapid Fire, Sustained Hits, Lethal Hits, Devastating Wounds, Twin-Linked, Indirect Fire, Pistol, Ignores Cover, Assault, and Anti — excluding Name, Range, and Attacks), and SHALL report a total Attacks value computed by summing each contributing model-line's per-model Attacks scaled by that model-line's remaining count. Each aggregated entry SHALL retain a list of the individual contributions (owning component name, statline name, remaining count, and per-model Attacks) that the total was built from.

#### Scenario: Same weapon profile from different components is combined
- **WHEN** the Bodyguard unit has 4 models carrying a weapon profile with 3 Attacks each, and the attached Leader carries a wargear item with an identical structural profile but 7 Attacks
- **THEN** the aggregate view shows one combined entry with a total Attacks value of 19 (4 × 3 + 7), not the Attacks value of either contributor alone

#### Scenario: Weapon count reflects casualties
- **WHEN** 2 of the 4 Bodyguard models carrying a given weapon profile are removed as casualties
- **THEN** the aggregate view's total Attacks for that weapon profile decreases by the removed models' share

#### Scenario: Differently-modified copies of a same-named weapon are not combined
- **WHEN** two components each carry a same-named weapon, but one copy has an ability (e.g. Lethal Hits) that the other does not
- **THEN** the aggregate view shows them as two separate entries, each with its own total Attacks

#### Scenario: Weapons differing only by a targeting/eligibility keyword are not combined
- **WHEN** two weapons match on Type, Skill, Strength, AP, and Damage, but differ in a keyword flag such as Pistol, Assault, or Ignores Cover
- **THEN** the aggregate view shows them as two separate entries, each with its own total Attacks — a targeting/eligibility keyword is as much a part of the weapon's identity as a damage-modifying one

#### Scenario: Contributions are retained on a merged entry
- **WHEN** two or more model-lines contribute to the same aggregated weapon entry
- **THEN** the entry's contribution list includes one item per contributing model-line, each reporting that model-line's own remaining count and per-model Attacks

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

