## MODIFIED Requirements

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
