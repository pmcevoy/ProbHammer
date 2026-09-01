## ADDED Requirements

### Requirement: Attachment-Eligibility Abilities Are Excluded
A Datasheet's Abilities SHALL NOT include an ability whose Name is exactly "Leader", "Support", or
"Attached Unit", regardless of whether that ability would otherwise be classified as Intrinsic or as
a Core Rule reference. These three names carry only a restatement of attachment eligibility that a
resolved army roster's own attachment relationships already represent directly, and their exclusion
applies uniformly across every source shape a Datasheet's Abilities can be built from.

#### Scenario: A local ability profile named "Leader" is excluded
- **WHEN** catalogue data for a unit includes a local ability profile whose Name is exactly "Leader"
- **THEN** that ability does NOT appear in the Datasheet's Abilities

#### Scenario: A Core Rule reference named "Support" is excluded
- **WHEN** catalogue data for a unit includes a datasheet-wide reference to a separately-defined
  rule whose resolved Name is exactly "Support"
- **THEN** that ability does NOT appear in the Datasheet's Abilities, even though it would otherwise
  be classified as a Core Rule

#### Scenario: An ability with a different name is not excluded
- **WHEN** catalogue data for a unit includes an ability whose Name is not exactly "Leader",
  "Support", or "Attached Unit"
- **THEN** that ability is unaffected by this exclusion and appears in the Datasheet's Abilities per
  its usual classification
