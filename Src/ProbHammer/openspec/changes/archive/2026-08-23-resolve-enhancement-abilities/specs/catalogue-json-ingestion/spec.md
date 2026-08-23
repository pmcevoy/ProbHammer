## MODIFIED Requirements

### Requirement: Ability Text Extraction
The system SHALL extract an `Ability` from each `Abilities`-typeName profile encountered while
resolving a unit, carrying the profile's `Description` characteristic through as `Ability.Text`
verbatim, without parsing it for behavioral effect, consistent with `datasheet-catalogue`'s
existing stance that ability text is never auto-parsed into behavior. An `Abilities`-typeName
profile found directly on the entry that owns the resolved unit's `Statline` SHALL populate the
Datasheet's intrinsic (always-exposed) ability list; an `Abilities`-typeName profile found nested
inside a `"type": "upgrade"` selection entry anywhere else in the walk SHALL instead populate the
Datasheet's on-demand ability index only (per `datasheet-catalogue`'s On-Demand Ability
Resolution), classified as an Enhancement when that selection entry's own containing selection
group is named "Enhancements", and as a plain optional grant otherwise.

#### Scenario: Ability with descriptive text only
- **WHEN** a resolved unit carries an `Abilities`-typeName profile with a `Description`
  characteristic
- **THEN** an `Ability` is produced whose `Text` equals that characteristic's value unchanged

#### Scenario: An entry-direct ability is extracted as intrinsic
- **WHEN** an `Abilities`-typeName profile sits directly on the entry that owns the resolved
  unit's `Statline` profile
- **THEN** the extracted `Ability` populates the Datasheet's intrinsic ability list

#### Scenario: An ability nested inside an Enhancements selection is extracted as an Enhancement
- **WHEN** an `Abilities`-typeName profile sits on a `"type": "upgrade"` selection entry that is
  itself a member of a selection group named "Enhancements"
- **THEN** the extracted `Ability` populates the Datasheet's on-demand index only, classified as
  an Enhancement, and does NOT appear in the intrinsic ability list

#### Scenario: An ability nested inside a non-Enhancement optional selection is extracted as a plain grant
- **WHEN** an `Abilities`-typeName profile sits on a `"type": "upgrade"` selection entry that is
  not a member of any selection group named "Enhancements" (e.g. Impulsor's "Shield Dome")
- **THEN** the extracted `Ability` populates the Datasheet's on-demand index only, classified as a
  plain optional grant, and does NOT appear in the intrinsic ability list
