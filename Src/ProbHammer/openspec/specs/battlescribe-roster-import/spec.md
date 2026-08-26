# battlescribe-roster-import Specification

## Purpose

Parses and resolves a BattleScribe `rosterSchema` JSON army-list export (as produced by NewRecruit
and other BattleScribe-compatible tools) directly into the same `Domain.Roster` shapes the GW-app
text pipeline produces, without depending on or resolving against BSData catalogue data — every
statline, weapon profile, ability, and attachment relationship needed is already present, resolved,
in the source JSON.

## Requirements

### Requirement: Format Recognition
The system SHALL recognize a submitted payload as a BattleScribe roster export when it parses as
JSON and contains a top-level `roster` object whose `xmlns` value identifies the BattleScribe roster
schema. A payload that does not meet this shape SHALL NOT be treated as a BattleScribe roster export
by this capability.

#### Scenario: A valid BattleScribe roster JSON payload is recognized
- **WHEN** the submitted text parses as JSON and contains `{"roster": {"xmlns": "http://www.battlescribe.net/schema/rosterSchema", ...}}`
- **THEN** the system recognizes it as a BattleScribe roster export and processes it via this
  capability

#### Scenario: Non-matching input is not misidentified
- **WHEN** the submitted text is plain GW-app export text, or JSON that does not contain a `roster`
  object with the expected `xmlns`
- **THEN** the system does not treat it as a BattleScribe roster export

### Requirement: Army Metadata Extraction
The system SHALL extract the army's PointsSpent and PointsLimit from the roster's own `costs`/
`costLimits` totals, its Faction from the force's catalogue name, its Detachments from the roster's
`Detachment` selection (including each detachment's own Detachment Points cost), and its
ForceDisposition and BattleSize from the roster's corresponding `Force Disposition` and
`Battle Size` selections.

#### Scenario: Points totals resolve from the roster's own cost fields
- **WHEN** a roster's `costs` total is 915 and its `costLimits` total is 1000
- **THEN** the parsed army's PointsSpent is 915 and PointsLimit is 1000

#### Scenario: A named Detachment with its own point cost resolves
- **WHEN** the roster's `Detachment` selection contains one nested selection named
  `"Companions of Vehemence"` with a `"Detachment Points"` cost of 2
- **THEN** the parsed army's Detachments list contains `"Companions of Vehemence"`

### Requirement: Attachment Relationship Resolution
For each top-level unit/model selection, the system SHALL recognize an outgoing `"Leading"` or
`"Supporting"` association targeting another top-level selection as that selection attaching to the
target. The system SHALL treat the target of one or more such associations as the group's Bodyguard,
and every selection with an outgoing association to it as an Attached member of that group. A
top-level selection with no outgoing association and never targeted by one SHALL resolve as a
standalone unit.

#### Scenario: A Leader and Support attach to a Bodyguard
- **WHEN** two top-level selections each carry an outgoing `"Leading"`/`"Supporting"` association
  targeting the same third top-level selection
- **THEN** the system resolves one attachment group whose Bodyguard is the targeted selection and
  whose Attached members are the two selections that targeted it

#### Scenario: A Leader-only attachment group has no Support member
- **WHEN** exactly one top-level selection carries an outgoing association targeting another
- **THEN** the system resolves one attachment group with one Attached member and no error, rather
  than requiring a Support member to be present

#### Scenario: An unassociated selection resolves as standalone
- **WHEN** a top-level selection carries no outgoing association and is never the target of one
- **THEN** the system resolves it as a standalone unit, not part of any attachment group

### Requirement: Statline, Weapon, and Ability Extraction
For each unit/model selection, the system SHALL extract its statline from a resolved profile whose
type identifies it as a unit characteristic profile, its weapon profiles from resolved profiles
whose type identifies them as ranged or melee weapon profiles, and its abilities from resolved
profiles whose type identifies them as ability profiles — all already fully resolved in the source
JSON, requiring no external catalogue lookup. When a single wargear selection resolves to more than
one weapon profile, every resolved profile SHALL be retained.

#### Scenario: A model group's per-loadout split requires no partition inference
- **WHEN** a unit's roster selections already split its models into separately-counted,
  separately-named loadout sub-selections (e.g. `"Initiate w/Chainsword & Heavy Bolt Pistol"`
  count 3, `"Initiate w/Power Fist & Heavy Bolt Pistol"` count 2)
- **THEN** each sub-selection resolves to its own `ModelLine` with its own count and weapon list,
  without the system inferring or guessing a split from weapon counts

#### Scenario: A wargear selection resolving to multiple weapon profiles retains every profile
- **WHEN** a wargear selection's resolved profiles include more than one weapon profile (e.g. one
  melee weapon selection resolving to a "Sweep" profile and a "Strike" profile)
- **THEN** every resolved weapon profile is retained on the owning model line

### Requirement: Enhancement Recognition
The system SHALL recognize a selection as a selected Enhancement when it carries a cost entry named
`"Enhancements"`, and SHALL attach its resolved ability to the owning Unit. No Enhancement ability
SHALL be attached to a Unit unless such a selection is actually present for it.

#### Scenario: A selection tagged with an Enhancements cost resolves as an Enhancement
- **WHEN** a unit's selections include one carrying a `costs` entry named `"Enhancements"`
- **THEN** the resulting Unit's Enhancements includes that selection's resolved ability

### Requirement: Core Rule Extraction
For each unit/model selection, the system SHALL extract every entry in that selection's own `rules`
list as an ability attached to the resulting Unit, resolving each rule's full text from a
roster-scoped index built by collecting every distinct rule entry found anywhere in the roster.
Because the roster contains only rules that actually apply to the exported army, the system SHALL
NOT apply any separate chapter, sub-faction, or game-mode gating when extracting these. The
extracted ability's Origin SHALL be Army Rule when its own Name matches one of the known army-wide
rule names resolved for the roster's own Faction (per `army-rule-name-lookup`'s Faction-Scoped
Resolution, using the Faction the mapper derives from the roster's own `catalogueName`), and Core
Rule otherwise.

#### Scenario: A non-army-wide rule resolves with its full text as Core Rule
- **WHEN** a unit selection's `rules` list includes an entry named `"Templar Vows"` with its own
  description text, and its Name is not one of the known army-wide rule names resolved for the
  roster's own Faction
- **THEN** the resulting Unit carries a Core-Rule-origin ability named `"Templar Vows"` with that
  text, with no additional gating check performed

#### Scenario: A rule matching the curated lookup resolves with Army Rule Origin
- **WHEN** a unit selection's `rules` list includes an entry whose Name matches one of the known
  army-wide rule names resolved for the roster's own Faction (e.g. "Nurgle's Gift" for a Death
  Guard roster)
- **THEN** the resulting Unit carries an Army-Rule-origin ability with that Name and its own text

### Requirement: Army Roster Assembly
The system SHALL assemble every resolved Unit/AttachedUnit together with the extracted army metadata
into one `ArmyRoster`, in the same shape the GW-app text pipeline produces.

#### Scenario: A fully resolved roster JSON produces one ArmyRoster
- **WHEN** every top-level selection in a submitted roster resolves successfully
- **THEN** the system produces one `ArmyRoster` containing every resolved unit and the extracted
  army metadata
