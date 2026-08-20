## Purpose

Parses raw GW-app 11th-edition army-list export text into a structured intermediate — army
metadata plus, for every named model group, verbatim wargear selections already partitioned into
per-loadout sub-groups — without depending on or resolving against any catalogue data.

## ADDED Requirements

### Requirement: Army Metadata Extraction
The parser SHALL extract the army's Name, PointsSpent, an ordered Faction list (one entry for an
unsplit faction, two for a parent-codex-plus-sub-faction), one or more Detachments (each an
ordered name; a Detachment's own name text MAY contain commas and SHALL be captured as a single
entry, never split on them), a ForceDisposition, and a BattleSize together with its own
PointsLimit — matching `roster-model`'s existing `ArmyRoster` field shapes.

#### Scenario: Single-faction army header
- **WHEN** the export's faction lines contain exactly one entry (e.g. `"Chaos Space Marines"`)
- **THEN** the parsed Faction list contains exactly that one entry

#### Scenario: Sub-faction army header
- **WHEN** the export's faction lines contain two entries in order (e.g. `"Space Marines"` then
  `"Black Templars"`)
- **THEN** the parsed Faction list contains both, in that order

#### Scenario: A Detachment name containing commas is captured as one entry
- **WHEN** the export's detachment line names a single detachment whose own text contains commas
  (e.g. `"Fulguris Task Force, Marshal's Household, and Subversion Assets"`)
- **THEN** the parsed Detachments list contains that text as one entry, not split into several

### Requirement: Attachment Group Recognition
Within the `ATTACHED UNITS` section, the parser SHALL treat each `"Attached unit N"` line as the
start of a new group, and SHALL assign every subsequent unit block up to the next such line (or
the next top-level section) to that group. For each unit within a group, the parser SHALL
recognize its first bullet line as role metadata, not a wargear selection, when it matches
`"Attached as: <Role>"` optionally followed by a parenthetical category; `Role` SHALL be one of
Leader, Support, or Bodyguard, and any parenthetical category text SHALL be discarded — it is not
captured anywhere in the parsed output. Exactly one member of a group SHALL be identified as
Bodyguard; every other member SHALL be identified as Leader or Support, retained in the order they
appear in the export.

#### Scenario: A group with Leader, Support, and Bodyguard members
- **WHEN** an `Attached unit` group contains three members tagged Leader, Support, and Bodyguard
  respectively
- **THEN** the parsed group identifies exactly one Bodyguard member and two Leader/Support members,
  in their original export order

#### Scenario: The parenthetical category is optional and always discarded
- **WHEN** a group member's role line is `"Attached as: Bodyguard"` (no category) in one export and
  `"Attached as: Bodyguard (Battleline)"` (with category) in another
- **THEN** both parse to the same Role (`Bodyguard`) with no category captured in either case

#### Scenario: Bodyguard-role member is identified independent of position within the group
- **WHEN** a group lists its Bodyguard-tagged member last, after its Leader and Support members
- **THEN** the parser still identifies it correctly as the group's one Bodyguard member

### Requirement: Standalone Unit Section Recognition
Outside `ATTACHED UNITS`, the parser SHALL treat each top-level section header (`CHARACTERS`,
`BATTLELINE`, `DEDICATED TRANSPORTS`, `OTHER DATASHEETS`) as introducing a run of standalone
(non-attached) units, ending at the next section header or the end of the export.

#### Scenario: A standalone unit under a top-level section
- **WHEN** a unit block appears under a `CHARACTERS` section header, outside any attachment group
- **THEN** it is parsed as a standalone unit, not as a member of any attachment group

### Requirement: Model Group and Weapon Selection Parsing
For each named unit, the parser SHALL parse its nested `"Nx ModelName"` bullet lines together with
each one's own nested weapon-count bullet lines, and SHALL partition each model group into one or
more per-loadout sub-groups as follows: a weapon whose count equals the model group's total model
count SHALL be treated as common to every resulting sub-group; the remaining weapons SHALL be
treated as mutually-exclusive alternatives, and the parser SHALL partition the group into one
sub-group per distinct remaining-weapon count, each carrying that count, the union of the common
weapons, and its own distinguishing weapon(s). Every weapon listed in the export SHALL be carried
through verbatim into its sub-group — none SHALL be omitted on the basis that another selected
weapon supersedes it.

The parser SHALL fail with a diagnostic identifying the unit and its raw wargear text when a model
group's weapon counts cannot be partitioned this way — i.e. when the non-common weapons' counts do
not sum exactly to the group's total model count.

#### Scenario: Uniform model group needs no split
- **WHEN** a model group's every weapon count equals its total model count (e.g. `"4x Neophyte"`
  with `"4x Astartes chainsword"` and `"4x Bolt pistol"`)
- **THEN** the group parses to a single sub-group covering all 4 models with both weapons

#### Scenario: A model group splits by a mutually-exclusive weapon choice
- **WHEN** a model group's weapon counts include some equal to the total and some that don't (e.g.
  `"5x Initiate"` with `"5x Bolt pistol"`, `"5x Heavy bolt pistol"`, `"5x Close combat weapon"`
  shared, and `"3x Astartes chainsword"` / `"2x Power fist"` alternating)
- **THEN** the group parses to two sub-groups — one of count 3 carrying the shared weapons plus
  Astartes chainsword, one of count 2 carrying the shared weapons plus Power fist

#### Scenario: Every listed weapon is retained even when one appears to supersede another
- **WHEN** a model group lists a baseline weapon (e.g. `"Close combat weapon"`) at a count equal to
  the group total alongside a purchased upgrade weapon
- **THEN** the baseline weapon is retained in every resulting sub-group's weapon list, not dropped
  in favor of the upgrade

#### Scenario: Unpartitionable weapon counts fail loudly rather than guessing
- **WHEN** a model group's non-common weapon counts do not sum to its total model count
- **THEN** parsing fails with a diagnostic identifying the unit and its raw wargear text, rather
  than silently guessing a split

### Requirement: Enhancement Recognition
A bullet line matching `"Enhancement:"` or `"Enhancements:"` SHALL be recognized as naming one or
more Enhancements applied to the unit, distinct from its wargear/model selections.

#### Scenario: A unit with a named Enhancement
- **WHEN** a unit's bullet list includes a line `"Enhancements: Touched by the Warp"`
- **THEN** the parsed unit's Enhancements includes `"Touched by the Warp"`, and that line is not
  treated as a wargear selection
