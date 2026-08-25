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

### Requirement: Detachment Name Resolution
For each Detachment entry in a parsed army list (`army-list-parsing`'s Army Metadata Extraction —
a single captured text that MAY itself name more than one Detachment in natural-language list
form), the system SHALL resolve it against the faction's catalogue closure's own Detachment
choices — nested children of the closure's Detachment selection group, not its top-level named
entries — using a greedy, longest-name-first match: trying the closure's own known Detachment
names in descending order of length, the first one found anywhere in the remaining unclaimed
portion of the text claims that portion, so no shorter name already contained within an
already-claimed portion is ever matched separately. This repeats until no further known name
matches. The system SHALL attach each claimed Detachment's own resolved rule text (per
`catalogue-json-ingestion`'s Detachment Rule Text Extraction) to the `ArmyRoster`, in the order
each was claimed in the text, and SHALL fail with a diagnostic naming the unresolved remainder when
any non-separator text (i.e. anything other than whitespace, commas, or the word `"and"`) is left
unclaimed once no further known name matches.

Longest-name-first matching, rather than a syntactic split on `"and"`/commas, is what correctly
resolves two real, confirmed collision shapes found in the BSData corpus: a single Detachment can
itself be named with the word `"and"` in it (e.g. "Legends of Saga and Song" — a syntactic split
would misidentify it as two nonexistent Detachments), and one Detachment's own name can be a literal
substring of a different, separate Detachment's name in the same faction (e.g. "Warhost" is a
substring of "Armoured Warhost" — matching shorter names first would spuriously claim "Warhost" as
a second, unselected Detachment whenever only "Armoured Warhost" was actually chosen).

#### Scenario: A single Detachment name resolves directly
- **WHEN** a parsed army list's Detachments entry is a name matching one of the faction's own
  Detachment choices, with no other detachment name present in the text (e.g. "Virulent
  Vectorium")
- **THEN** the resulting ArmyRoster carries that one Detachment's own resolved rule text

#### Scenario: A Detachment name containing "and" resolves as one whole entry, not split
- **WHEN** a parsed army list's Detachments entry is a name that itself contains the word `"and"`
  and matches one of the faction's own Detachment choices as a whole (e.g. "Legends of Saga and
  Song")
- **THEN** the resulting ArmyRoster carries that one Detachment, resolved from the whole captured
  text — it is never split into "Legends of Saga" and "Song"

#### Scenario: A shorter Detachment name that is a substring of a longer one is not spuriously matched
- **WHEN** a parsed army list's Detachments entry names only the longer of two Detachments whose
  names overlap as substrings (e.g. "Armoured Warhost", where "Warhost" is also a separate, real
  Detachment name in the same faction)
- **THEN** the resulting ArmyRoster carries only "Armoured Warhost" — "Warhost" is not also
  produced as a spurious second entry

#### Scenario: A two-Detachment joined entry resolves both
- **WHEN** a parsed army list's Detachments entry names two Detachments joined in natural-language
  form (e.g. "Cabal of Chaos and Devotees of Destruction")
- **THEN** the resulting ArmyRoster carries two Detachments, "Cabal of Chaos" and "Devotees of
  Destruction", each with its own independently resolved rule text

#### Scenario: A three-or-more-Detachment joined entry resolves all of them
- **WHEN** a parsed army list's Detachments entry names three or more Detachments joined in
  Oxford-comma-plus-"and" form (e.g. "Fulguris Task Force, Marshal's Household, and Subversion
  Assets")
- **THEN** the resulting ArmyRoster carries three Detachments, each with its own independently
  resolved rule text, in the order listed

#### Scenario: A Detachment with no rule of its own resolves with an empty rule list
- **WHEN** a parsed army list's Detachments includes a name that resolves to a Detachment declaring
  no rule of its own
- **THEN** the resulting ArmyRoster carries that Detachment with an empty rule list, rather than
  failing resolution

#### Scenario: An unresolvable Detachment name fails with a diagnostic
- **WHEN** a parsed army list's Detachments entry contains text that does not match any of the
  faction's own known Detachment names, even after every other name in the text has been claimed
- **THEN** resolution fails with a diagnostic naming the unresolved remainder, including a "did you
  mean...?" suggestion when a close match exists
