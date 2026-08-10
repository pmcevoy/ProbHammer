## MODIFIED Requirements

### Requirement: Aggregate Statline View
The Attached Unit aggregate view SHALL list one entry per distinct statline name referenced by any present component Unit's model-lines, for as long as at least one model-line sharing that name has a remaining count greater than zero. Each entry SHALL report a remaining count and an initial count, both summed across every model-line sharing that statline name, and SHALL list a loadout breakdown containing one entry per model-line sharing that name, each with its own weapon list, remaining count, and initial count.

#### Scenario: Statline dropped once its models are gone
- **WHEN** every model-line referencing a particular statline name reaches a remaining count of 0
- **THEN** that statline no longer appears in the aggregate view

#### Scenario: Statline survives while any one of its model-lines has models remaining
- **WHEN** a statline name is shared by two model-lines, and one of them reaches a remaining count of 0 while the other still has models remaining
- **THEN** that statline still appears in the aggregate view, with its remaining count reflecting only the surviving model-line(s)

#### Scenario: Initial count reflects starting strength, not just surviving model-lines
- **WHEN** a statline name is shared by two model-lines with initial counts of 2 and 3, and the model-line with an initial count of 2 is fully removed as casualties
- **THEN** the aggregate view's initial count for that statline name is still 5 (2 + 3), while its remaining count is 3

#### Scenario: Loadout breakdown lists every model-line sharing a statline name, including a fully-removed one
- **WHEN** a statline name is shared by two model-lines with different weapon selections, and one of them has been fully removed as casualties
- **THEN** the loadout breakdown includes both model-lines: the removed one showing a remaining count of 0 alongside its original initial count, and the other showing its own current remaining/initial counts

#### Scenario: Distinct statline names with identical values are not collapsed
- **WHEN** two model-lines reference different statline names whose M/T/Sv/W/Ld/Oc values happen to be identical
- **THEN** the aggregate view lists them as two separate statline entries, not merged into one

### Requirement: Aggregate Weapon Count View
The Attached Unit aggregate view SHALL aggregate weapons across all component Units by structural profile equality (matching weapon Type, Skill, Strength, AP, Damage, and every ability/keyword flag the weapon carries — including but not limited to Torrent, Blast, Melta, Rapid Fire, Sustained Hits, Lethal Hits, Devastating Wounds, Twin-Linked, Indirect Fire, Pistol, Ignores Cover, Assault, and Anti — excluding Name, Range, and Attacks), and SHALL report a total Attacks value computed by summing each contributing model-line's per-model Attacks scaled by that model-line's remaining count. Each aggregated entry SHALL retain a list of the individual contributions (owning component name, statline name, remaining count, and per-model Attacks) that the total was built from.

#### Scenario: Same weapon profile from different components is combined
- **WHEN** the Bodyguard unit has 4 models carrying a weapon profile with 3 Attacks each, and the attached Leader carries a wargear item with an identical structural profile but 7 Attacks
- **THEN** the aggregate view shows one combined entry with a total Attacks value of 19 (4 × 3 + 7), not the Attacks value of either contributor alone

#### Scenario: Weapon count reflects casualties
- **WHEN** 2 of the 4 Bodyguard models carrying a given weapon profile are removed as casualties
- **THEN** the aggregate view's total Attacks for that weapon profile decreases by the removed models' share

#### Scenario: Differently-modified copies of a same-named weapon are not combined
- **WHEN** two components each carry a same-named weapon, but one copy has an ability (e.g. Lethal Hits) that the other does not
- **THEN** the aggregate view shows them as two separate entries, each with its own total Attacks

#### Scenario: Weapons differing only by a targeting/eligibility keyword are not combined
- **WHEN** two weapons match on Type, Skill, Strength, AP, and Damage, but differ in a keyword flag such as Pistol, Assault, or Ignores Cover
- **THEN** the aggregate view shows them as two separate entries, each with its own total Attacks — a targeting/eligibility keyword is as much a part of the weapon's identity as a damage-modifying one

#### Scenario: Contributions are retained on a merged entry
- **WHEN** two or more model-lines contribute to the same aggregated weapon entry
- **THEN** the entry's contribution list includes one item per contributing model-line, each reporting that model-line's own remaining count and per-model Attacks
