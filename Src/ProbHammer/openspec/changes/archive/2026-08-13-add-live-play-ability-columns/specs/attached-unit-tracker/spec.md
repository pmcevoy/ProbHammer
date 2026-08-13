## MODIFIED Requirements

### Requirement: Aggregate Ability View
The Attached Unit aggregate view SHALL report abilities per present component Unit, without
combining or deduplicating across components. For each present component, the aggregate view
SHALL report abilities from two sources: the component's Datasheet-level Abilities (not tied to
any one model-line) and abilities carried by any of the component's own present model-lines (e.g.
granted by an Enhancement). Each reported entry SHALL carry the owning component's name, the
Ability itself, and — only for model-line-sourced abilities — the name of the specific statline
that bears it; Datasheet-sourced abilities carry no statline name, since they belong to the
component as a whole rather than to any one of its statlines. This applies uniformly to both
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
