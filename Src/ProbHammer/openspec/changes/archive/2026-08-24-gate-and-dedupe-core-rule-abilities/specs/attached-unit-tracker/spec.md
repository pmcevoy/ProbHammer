## MODIFIED Requirements

### Requirement: Aggregate Ability View
The Attached Unit aggregate view SHALL report abilities per present component Unit, without
combining or deduplicating across components, with one exception: every present component's own
Army Rule-origin Ability entries SHALL be promoted into a single entry per distinct Name, belonging
to no single component, rather than reported per contributing component. This promotion SHALL
apply unconditionally — including when only one present component currently contributes that Name
— since an Army Rule-origin Ability is an army-wide fact regardless of how many components in a
given roster happen to reference it, not a fact contingent on being shared by two or more of them.
For every other case, the aggregate view SHALL report abilities from three sources: the
component's Datasheet-level Abilities (not tied to any one model-line), the component's own
resolved Enhancements, and abilities carried by any of the component's own present model-lines
(e.g. granted by an Enhancement). Datasheet-level Abilities and resolved Enhancements are both
reported the same way — not tied to any one model-line. Each reported entry SHALL carry the owning
component's name (absent only for the deduplicated Core Rule case above), the Ability itself, and
— only for model-line-sourced abilities — the name of the specific statline that bears it;
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
- **WHEN** two different component Units of an AttachedUnit each have an Ability with the same
  Name, and that Ability's Origin is not Core Rule
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

#### Scenario: An Army Rule ability shared by every present component is reported once
- **WHEN** every present component of an AttachedUnit carries a Datasheet-level Ability with Origin
  Army Rule and an identical Name (e.g. a chapter-wide faction rule referenced independently by
  each component)
- **THEN** the aggregate view reports one entry for that Ability, belonging to no single component,
  rather than one entry per contributing component

#### Scenario: An Army Rule ability shared by only some components is still reported once
- **WHEN** two of an AttachedUnit's three present components carry an Army Rule-origin Ability with
  an identical Name, and the third does not
- **THEN** the aggregate view still reports one entry for that Ability, not two, and not one for
  each of the two contributing components

#### Scenario: An Army Rule ability on a standalone Unit is still promoted, not left as a plain entry
- **WHEN** a standalone Unit (not part of an AttachedUnit) has exactly one component, and that
  component carries a Datasheet-level Ability with Origin Army Rule
- **THEN** the aggregate view still reports that Ability belonging to no single component, the same
  treatment as when two or more components of an AttachedUnit share it — promotion depends on the
  Ability's Origin, not on how many components currently contribute it

#### Scenario: A shared Army Rule ability persists while any contributing component remains present
- **WHEN** an Army Rule ability is shared across multiple components and one of those components has
  every one of its model-lines reach a remaining count of 0, while at least one other contributing
  component still has a present model-line
- **THEN** the promoted entry still appears in the aggregate view

#### Scenario: A shared Army Rule ability disappears once every contributing component is gone
- **WHEN** every component that contributed to a promoted Army Rule ability has every one of
  its model-lines reach a remaining count of 0
- **THEN** the promoted entry no longer appears in the aggregate view
