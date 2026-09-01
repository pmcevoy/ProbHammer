## ADDED Requirements

### Requirement: Attachment-Eligibility Abilities Are Excluded
A synthesized Datasheet's Abilities SHALL NOT include an ability whose Name is exactly "Leader",
"Support", or "Attached Unit", regardless of whether that ability was extracted via Statline,
Weapon, and Ability Extraction or via Core Rule Extraction. These three names carry only a
restatement of attachment eligibility that this pipeline's own Attachment Relationship Resolution
already represents directly.

#### Scenario: An ability profile named "Leader" is excluded
- **WHEN** a unit/model selection's resolved ability profiles include one whose Name is exactly
  "Leader"
- **THEN** that ability does NOT appear in the resulting Unit's Abilities

#### Scenario: A rules-list entry named "Support" is excluded
- **WHEN** a unit/model selection's `rules` list includes an entry whose Name is exactly "Support"
- **THEN** that entry does NOT appear in the resulting Unit's Abilities, even though it would
  otherwise be extracted per Core Rule Extraction

#### Scenario: An ability with a different name is not excluded
- **WHEN** a unit/model selection's resolved abilities include one whose Name is not exactly
  "Leader", "Support", or "Attached Unit"
- **THEN** that ability is unaffected by this exclusion and appears in the resulting Unit's
  Abilities per its usual extraction
