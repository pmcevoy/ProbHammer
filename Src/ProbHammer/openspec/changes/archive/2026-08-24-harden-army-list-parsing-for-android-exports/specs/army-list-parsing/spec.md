## MODIFIED Requirements

### Requirement: Army Metadata Extraction
The parser SHALL extract the army's Name, PointsSpent, an ordered Faction list (one entry for an
unsplit faction, two for a parent-codex-plus-sub-faction), one or more Detachments (each an
ordered name; a Detachment's own name text MAY contain commas and SHALL be captured as a single
entry, never split on them), a ForceDisposition, and a BattleSize together with its own
PointsLimit — matching `roster-model`'s existing `ArmyRoster` field shapes. The `Points` cost
suffix on the army-name header line and the battle-size line SHALL be matched case-insensitively
(`Points` or `points`). The ForceDisposition line, being free text with no fixed shape, is OPTIONAL:
SHALL the line immediately following the last Detachment line already match the `<BattleSize> (<N>
Points)` pattern, the parser SHALL treat ForceDisposition as an empty string rather than consuming
that line as disposition text.

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

#### Scenario: A lower-case "points" suffix is accepted
- **WHEN** the army-name header or battle-size line uses a lower-case suffix (e.g.
  `"Barra's Army (725 points)"`, `"Incursion (1000 points)"`)
- **THEN** the line parses identically to the equivalent `"Points"`-suffixed form, with no error

#### Scenario: A missing ForceDisposition line is treated as an empty string
- **WHEN** the line immediately after the last Detachment line already matches the
  `<BattleSize> (<N> Points)` pattern (e.g. `"Strike Force (2000 points)"` directly follows
  `"Virulent Vectorium (3 Detachment Points)"`, with no free-text disposition line between them)
- **THEN** the parsed ForceDisposition is an empty string, and that line is parsed as BattleSize,
  not consumed as disposition text

### Requirement: Attachment Group Recognition
Within the `ATTACHED UNITS` section, matched case-insensitively (also accepting `"Attached Units"`),
the parser SHALL treat each `"Attached unit N"` line, also matched case-insensitively (also
accepting `"Attached Unit N"`), as the start of a new group, and SHALL assign every subsequent unit
block up to the next such line (or the next top-level section) to that group. For each unit within
a group, the parser SHALL recognize its first bullet line as role metadata, not a wargear
selection, when it matches `"Attached as: <Role>"` optionally followed by a parenthetical category;
`Role` SHALL be one of Leader, Support, or Bodyguard, and any parenthetical category text SHALL be
discarded — it is not captured anywhere in the parsed output. Exactly one member of a group SHALL
be identified as Bodyguard; every other member SHALL be identified as Leader or Support, retained
in the order they appear in the export.

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

#### Scenario: Differently-cased section and group markers are accepted
- **WHEN** an export uses `"Attached Units"` and `"Attached Unit 1"` (title case) instead of
  `"ATTACHED UNITS"` and `"Attached unit 1"`
- **THEN** the section and its groups are recognized identically to the all-caps/lower-case forms

### Requirement: Model Group and Weapon Selection Parsing
For each named unit, the parser SHALL parse its nested `"Nx ModelName"` bullet lines together with
each one's own nested weapon-count lines, and SHALL partition each model group into one or more
per-loadout sub-groups as follows: a weapon whose count equals the model group's total model count
SHALL be treated as common to every resulting sub-group; the remaining weapons SHALL be treated as
mutually-exclusive alternatives, and the parser SHALL partition the group into one sub-group per
distinct remaining-weapon count, each carrying that count, the union of the common weapons, and its
own distinguishing weapon(s). Every weapon listed in the export SHALL be carried through verbatim
into its sub-group — none SHALL be omitted on the basis that another selected weapon supersedes it.

Within one weapon-count list (a model group's own nested weapons, or a unit's direct weapons when
it has no explicit model-group header), only the first line SHALL carry its own bullet marker;
SHALL a subsequent line in that same list have no bullet marker and be indented no less than the
first line's own text column, the parser SHALL treat it as a further weapon-count entry of that
same list rather than as the start of a new unit or a new nested list. A blank line, a differently
indented line, or a line that itself carries a bullet marker SHALL end the list as before.

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

#### Scenario: A second and later weapon-count line with no bullet is read as part of the same list
- **WHEN** a model group's weapon-count lines render with only the first bulleted (e.g.
  `"• 1x Ectoplasma destructor"` followed by unbulleted, equally-indented `"2x Excruciator cannon"`,
  `"1x Hades lascannon"`, `"1x Heavy reaper autocannon"`, `"1x Shearing claws"`)
- **THEN** all five lines are read as weapon-count entries of the same model group, in the same way
  as if each had carried its own bullet

#### Scenario: An unbulleted continuation line is not mistaken for a new unit header
- **WHEN** a model group's own weapon list ends with an unbulleted, indented continuation line
  immediately followed by the end of the unit's bullet block
- **THEN** parsing continues with the next unit or section header, and the continuation line is
  never passed to unit-header parsing
