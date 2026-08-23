# army-roster-enrichment Specification

## Purpose

Resolves a parsed army list (`army-list-parsing`) against BSData 11th Edition catalogue data —
locating the right catalogue closure for the army's faction and resolving each parsed unit and
weapon name against it — to build a live `ArmyRoster` (`Unit`/`AttachedUnit`/`ModelLine` graph),
with the expensive parts of catalogue resolution cached for reuse across requests.

## Requirements

### Requirement: Faction Catalogue File Resolution
Given a parsed army list's Faction entries, the system SHALL locate the catalogue file to use as
the starting point for closure resolution (per `catalogue-json-ingestion`'s Faction Catalogue
Closure Resolution) by matching the most specific (last) Faction entry against the available
catalogue file names — a file whose name equals that entry, or ends with `" - "` followed by that
entry — excluding any file whose name contains `"Library"`. The system SHALL fail with a
diagnostic naming the Faction entry when no such file is found, or when more than one file
matches.

#### Scenario: A sub-faction resolves via a suffix match, not a literal join
- **WHEN** the parsed Faction list is `["Space Marines", "Black Templars"]`
- **THEN** the system selects `"Imperium - Black Templars.json"` as the starting file, without
  requiring `"Imperium"` to appear anywhere in the parsed Faction list

#### Scenario: An unsplit faction resolves via a direct filename match
- **WHEN** the parsed Faction list is `["Orks"]`
- **THEN** the system selects `"Orks.json"` as the starting file

#### Scenario: A Library file is never selected as the starting file
- **WHEN** the most specific Faction entry could otherwise match a file whose name contains
  `"Library"`
- **THEN** that file is excluded from consideration, even if it contains matching content

#### Scenario: No matching file fails loudly
- **WHEN** no available catalogue file matches the most specific Faction entry
- **THEN** resolution fails with a diagnostic naming that Faction entry

#### Scenario: More than one matching file fails loudly
- **WHEN** more than one available catalogue file matches the most specific Faction entry
- **THEN** resolution fails with a diagnostic naming that Faction entry, rather than guessing which
  file to use

### Requirement: Catalogue Closure Caching
The resolved catalogue closure and its name/id resolution indices for a given starting file SHALL
be cached and reused across requests for as long as the hosting process is running, rather than
re-read and re-parsed from source catalogue files on every request.

#### Scenario: A second request for the same faction reuses the cached closure
- **WHEN** two separate requests both resolve against the same starting catalogue file
- **THEN** the second request reuses the closure and indices built for the first, without
  re-reading or re-parsing the underlying catalogue files

#### Scenario: Two different factions are cached independently
- **WHEN** requests resolve against two different starting catalogue files
- **THEN** each is cached under its own starting file, and resolving one does not evict or
  invalidate the other

### Requirement: Unit and Weapon Name Resolution
For each parsed unit, the system SHALL resolve its name against the faction's catalogue closure
(per `catalogue-json-ingestion`'s Local-Over-Imported Name Precedence) to obtain a `Datasheet`,
normalizing typographic punctuation (e.g. a right single quotation mark) to its plain ASCII
equivalent before resolution. Each parsed model sub-group's own name SHALL resolve to a `Statline`
declared on that `Datasheet`; each of its weapon names SHALL resolve to a `WeaponProfile` via that
`Datasheet`, falling back to that `Datasheet`'s on-demand ability index (per `datasheet-catalogue`'s
On-Demand Ability Resolution) when the name is not a weapon profile — some wargear lines grant an
ability rather than a weapon (e.g. Impulsor's "Shield Dome", a granted invulnerable save). The
system SHALL fail with a diagnostic naming the unresolved text when a name resolves against
neither.

#### Scenario: A unit name containing a typographic apostrophe resolves correctly
- **WHEN** a parsed weapon name uses a typographic right single quotation mark (e.g.
  `"Reaver's blade"`) where the catalogue's own name uses a plain ASCII apostrophe
- **THEN** the name resolves successfully against the catalogue entry

#### Scenario: An unresolvable name fails with a diagnostic
- **WHEN** a parsed unit or weapon name resolves against neither a `WeaponProfile` nor the
  on-demand ability index of the resolved catalogue closure
- **THEN** resolution fails with a diagnostic naming the unresolved text, rather than silently
  omitting that unit or weapon

#### Scenario: A wargear name resolves to a granted ability instead of a weapon
- **WHEN** a parsed model sub-group's wargear name does not match any `WeaponProfile` on its
  resolved `Datasheet`
- **THEN** the system resolves that name against the Datasheet's on-demand ability index instead,
  and attaches the resolved ability to that model-line rather than failing resolution

### Requirement: Enhancement Name Resolution
For each parsed unit, the system SHALL resolve every name in its parsed Enhancements against its
resolved Datasheet's on-demand ability index (per `datasheet-catalogue`'s On-Demand Ability
Resolution), attaching each resolved `Ability` to the resulting `Unit`. The system SHALL fail with
a diagnostic naming the unresolved text when an Enhancement name does not resolve. No Enhancement
ability SHALL be attached to a Unit unless its name was actually present in that unit's parsed
Enhancements — an Enhancement the Datasheet merely defines as available is never attached on its
own.

#### Scenario: A selected Enhancement resolves to its ability text
- **WHEN** a parsed unit's Enhancements list includes a name matching an Enhancement-classified
  ability on its resolved Datasheet (e.g. "Ferocity")
- **THEN** the resulting Unit carries the resolved Ability, including its full rules Text

#### Scenario: An unresolvable Enhancement name fails with a diagnostic
- **WHEN** a parsed unit's Enhancements list includes a name that does not resolve against its
  resolved Datasheet's on-demand ability index
- **THEN** resolution fails with a diagnostic naming the unresolved text

#### Scenario: A unit with no Enhancements selected resolves with an empty Enhancements list
- **WHEN** a parsed unit's Enhancements list is empty
- **THEN** the resulting Unit's Enhancements list is empty — no Enhancement-classified ability is
  attached merely because the unit's Datasheet defines one available

### Requirement: Attached Unit and Unit Graph Assembly
For each parsed attachment group, the system SHALL build an `AttachedUnit` whose `Bodyguard` is
the resolved `Unit` for the group's Bodyguard-role member and whose `Attached` collection contains
the resolved `Unit`s for the group's Leader/Support-role members, in the order captured during
parsing. Each parsed standalone unit SHALL become a plain `Unit`. Each resolved model sub-group
SHALL become one `ModelLine`, referencing its resolved `Statline`'s name, its resolved weapon
names, and its own model count.

#### Scenario: An attachment group assembles into one AttachedUnit
- **WHEN** a parsed attachment group has one Bodyguard-role member and two Leader/Support-role
  members
- **THEN** the resulting `AttachedUnit`'s `Bodyguard` is the Bodyguard-role member's resolved
  `Unit`, and its `Attached` collection contains the other two, in their captured order

#### Scenario: A standalone unit assembles into a plain Unit
- **WHEN** a parsed unit was recognized as standalone (per Standalone Unit Section Recognition)
- **THEN** it resolves to a plain `Unit`, not wrapped in any `AttachedUnit`

#### Scenario: A split model group produces multiple ModelLines sharing one statline name
- **WHEN** a parsed unit's model group was partitioned into two sub-groups during parsing (e.g.
  Crusader Squad's Initiate 3/2 split)
- **THEN** the resolved `Unit` has two `ModelLine`s, both referencing the same resolved `Statline`
  name, each with its own count and weapon list

### Requirement: Army Roster Assembly
The system SHALL assemble every resolved `Unit`/`AttachedUnit` together with the parsed army
metadata into one `ArmyRoster` (per `roster-model`'s existing Army Roster Container shape).

#### Scenario: A fully parsed and resolved army list produces one ArmyRoster
- **WHEN** every unit in a parsed army list resolves successfully
- **THEN** the system produces one `ArmyRoster` containing every resolved unit and the parsed army
  metadata
