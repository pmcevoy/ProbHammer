# attached-unit-tracker

## Purpose

Tracks live game state for an Attached Unit (casualties, and the aggregate statline/weapon/ability
view derived from its currently-present components) as play proceeds. TBD: expand as this
capability grows beyond its initial domain-model scope.
## Requirements
### Requirement: Model-Line Remaining Count
Each model-line SHALL track a remaining count, initialized to the model-line's original Count.
The remaining count SHALL be adjustable in both directions: it can be decreased to reflect
casualties, down to a minimum of zero, and it can be increased back to correct a mistaken
adjustment, up to a maximum of the model-line's original Count. Neither direction SHALL move the
remaining count outside the `[0, Count]` range.

#### Scenario: Decrementing a squad model-line
- **WHEN** a model-line with an original Count of 5 has 2 models removed as casualties
- **THEN** its remaining count is 3

#### Scenario: Removing a single-model line
- **WHEN** a model-line with an original Count of 1 (e.g. an attached Leader) is removed as a
  casualty
- **THEN** its remaining count is 0

#### Scenario: Remaining count cannot go negative
- **WHEN** a model-line's remaining count is already 0
- **THEN** further removal attempts do not decrement it below 0

#### Scenario: Restoring a mistakenly removed casualty
- **WHEN** a model-line with an original Count of 5 and a remaining count of 3 has its remaining
  count increased by 1
- **THEN** its remaining count is 4

#### Scenario: Remaining count cannot exceed the original Count
- **WHEN** a model-line's remaining count is already equal to its original Count
- **THEN** further restore attempts do not increase it above the original Count

#### Scenario: A full round trip returns to the starting count
- **WHEN** a model-line's remaining count is decreased by some amount and then increased back by
  the same amount, with neither adjustment clamped against a bound
- **THEN** its remaining count equals its value before either adjustment

### Requirement: Aggregate Statline View
The Attached Unit aggregate view SHALL list one entry per distinct statline name referenced by any
present component Unit's model-lines, for as long as at least one of that component's model-lines
references that statline name — regardless of those model-lines' remaining count — with per-name
grouping scoped to a single component Unit's own model-lines — two different component Units are
never merged into one entry even if their model-lines happen to share a statline name. A statline
name that no model-line of a component references at all SHALL NOT appear for that component,
whether or not another statline name belonging to the same component is present. Each entry SHALL
report a remaining count and an initial count, both summed across every model-line of that
component sharing that statline name, a loadout breakdown containing one entry per model-line
sharing that name (each with its own weapon list, remaining count, and initial count), and the name
of the owning component (its `Datasheet.Name`) so downstream consumers can identify which component
contributed the entry without re-deriving it.

Entries SHALL be ordered by component display order, then by that component's Datasheet-declared
statline order: for an AttachedUnit, each Attached Unit's entries render in the AttachedUnit's
Attached-list order, followed by the Bodyguard's entries; for a plain Unit, its own entries render
in its Datasheet's declared order.

#### Scenario: Statline dropped once its models are gone
- **WHEN** a component's Datasheet declares a statline name that none of its model-lines ever
  reference — never having had any models under that name in the roster at all (e.g. an unselected
  loadout variant never present in the roster)
- **THEN** that statline name does not appear in the aggregate view for that component; this is now
  the only condition under which a statline name is absent — a name that was referenced by at least
  one model-line persists (see the next scenario) even once every one of those model-lines reaches
  a remaining count of 0

#### Scenario: Statline persists at zero once its models are gone
- **WHEN** every model-line referencing a particular statline name reaches a remaining count of 0
- **THEN** that statline still appears in the aggregate view, reporting a remaining count of 0
  alongside its unchanged initial count

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

#### Scenario: Each entry reports its owning component's name
- **WHEN** the aggregate view is built for an AttachedUnit with a Bodyguard and one Attached unit
- **THEN** every statline entry contributed by the Bodyguard reports the Bodyguard's Datasheet name
  as its component name, and every entry contributed by the Attached unit reports that unit's own
  Datasheet name

### Requirement: Aggregate Weapon Count View
The Attached Unit aggregate view SHALL aggregate weapons across all component Units by structural
profile equality (matching weapon Type, Skill, Strength, AP, Damage, and every ability/keyword flag
the weapon carries — including but not limited to Torrent, Blast, Melta, Rapid Fire, Sustained
Hits, Lethal Hits, Devastating Wounds, Twin-Linked, Indirect Fire, Pistol, Ignores Cover, Assault,
and Anti — excluding Name, Range, and Attacks), and SHALL report a total Attacks value computed by
summing each contributing model-line's per-model Attacks scaled by that model-line's remaining
count. Each aggregated entry SHALL retain a list of the individual contributions (owning component
name, statline name, remaining count, and per-model Attacks) that the total was built from. A
model-line with a remaining count of 0 SHALL NOT produce a contribution at all — neither a zero-
valued entry nor any entry — regardless of whether that statline name's entry still appears in the
Aggregate Statline View.

#### Scenario: Same weapon profile from different components is combined
- **WHEN** the Bodyguard unit has 4 models carrying a weapon profile with 3 Attacks each, and the attached Leader carries a wargear item with an identical structural profile but 7 Attacks
- **THEN** the aggregate view shows one combined entry with a total Attacks value of 19 (4 × 3 + 7), not the Attacks value of either contributor alone

#### Scenario: Weapon count reflects casualties
- **WHEN** 2 of the 4 Bodyguard models carrying a given weapon profile are removed as casualties
- **THEN** the aggregate view's total Attacks for that weapon profile decreases by the removed models' share

#### Scenario: A fully-removed loadout contributes no row to a shared weapon's breakdown
- **WHEN** a statline entry has two loadouts sharing a weapon (e.g. both carry a Bolt Pistol), one
  loadout is fully removed as casualties (0 remaining) while the other survives, and a third,
  unrelated model-line also carries that same weapon profile
- **THEN** that weapon's aggregated entry reports a total Attacks reflecting only the surviving
  loadout and the unrelated model-line, and its contribution list contains no entry at all for the
  fully-removed loadout — not a contribution showing a Count of 0

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
The Attached Unit aggregate view SHALL report abilities per present component Unit, without
combining or deduplicating across components. For each present component, the aggregate view
SHALL report abilities from three sources: the component's Datasheet-level Abilities (not tied to
any one model-line), the component's own resolved Enhancements, and abilities carried by any of
the component's own present model-lines (e.g. granted by an Enhancement). Datasheet-level
Abilities and resolved Enhancements are both reported the same way — not tied to any one
model-line. Each reported entry SHALL carry the owning component's name, the Ability itself, and —
only for model-line-sourced abilities — the name of the specific statline that bears it;
Datasheet-sourced and Enhancement-sourced abilities carry no statline name, since they belong to
the component as a whole rather than to any one of its statlines. This applies uniformly to both
Model-scoped and Unit-scoped abilities — the Ability's own Scope alone determines which of the two
an entry is; the reporting rule itself does not depend on Scope.

#### Scenario: Unit-scoped ability appears in the combined list
- **WHEN** a component Unit has a Unit-scoped Ability, whether Datasheet-sourced or carried by one
  of its own model-lines
- **THEN** that ability appears in the aggregate view, reported against its owning component —
  this reverses the prior behavior of combining every component's Unit-scoped abilities into one
  cross-component list, now that each entry stays attributable to the component (and, when
  applicable, the specific statline) it came from

#### Scenario: Model-scoped ability stays with its model
- **WHEN** a component's model-line carries its own Ability (e.g. granted by an Enhancement),
  regardless of whether that Ability's Scope is Model or Unit
- **THEN** the aggregate view reports that ability with the name of the specific statline it is
  tied to, so it stays attributable to that one model-line rather than any other in the component

#### Scenario: Model-scoped ability disappears when its bearer is removed
- **WHEN** the model-line carrying a model-line-sourced Ability has its remaining count reduced to
  0, regardless of that Ability's Scope
- **THEN** that ability no longer appears anywhere in the aggregate view

#### Scenario: Datasheet-sourced ability is reported without a statline name
- **WHEN** a component's Datasheet has an Ability not tied to any specific model-line
- **THEN** the aggregate view reports that ability for that component with no statline name,
  regardless of its Scope

#### Scenario: Abilities are not combined across components
- **WHEN** two different component Units of an AttachedUnit each have an Ability with the same Name
- **THEN** the aggregate view reports them as two separate entries, one per component, never
  combined into one

#### Scenario: A Datasheet-sourced ability persists while any of its component's model-lines remain
- **WHEN** a component has multiple model-lines and only some of them reach a remaining count of 0
- **THEN** the component's Datasheet-sourced abilities still appear in the aggregate view, as long
  as the component itself is still present

#### Scenario: An applied Enhancement is reported without a statline name
- **WHEN** a present component Unit has one or more resolved Enhancements
- **THEN** the aggregate view reports each of them for that component with no statline name,
  the same treatment as a Datasheet-sourced ability

#### Scenario: An Enhancement disappears once its component is no longer present
- **WHEN** a component with a resolved Enhancement has every one of its model-lines reach a
  remaining count of 0
- **THEN** that Enhancement no longer appears in the aggregate view, the same treatment as a
  Datasheet-sourced ability

#### Scenario: A component with no Enhancements applied reports none
- **WHEN** a present component Unit's resolved Enhancements list is empty
- **THEN** the aggregate view reports no Enhancement-sourced entry for that component

