## MODIFIED Requirements

### Requirement: Aggregate Weapon Count View
The Attached Unit aggregate view SHALL aggregate weapons across all component Units by structural
profile equality (matching weapon Type, Skill, Strength, AP, Damage, and every ability/keyword flag
the weapon carries — including but not limited to Torrent, Blast, Melta, Rapid Fire, Sustained
Hits, Lethal Hits, Devastating Wounds, Twin-Linked, Indirect Fire, Pistol, Ignores Cover, Assault,
and Anti — excluding Name, Range, and Attacks), and SHALL report a total Attacks value computed by
summing each contributing model-line's per-model Attacks scaled by that model-line's remaining
count. Each aggregated entry SHALL retain a list of the individual contributions (owning component
name, statline name, remaining count, per-model Attacks, and the contributing weapon's own Name)
that the total was built from. A model-line with a remaining count of 0 SHALL NOT produce a
contribution at all — neither a zero-valued entry nor any entry — regardless of whether that
statline name's entry still appears in the Aggregate Statline View.

Each aggregated entry SHALL additionally report a display Name computed from the distinct Name
values among its own contributions, in first-encountered order: unchanged when every contribution
shares one Name; two distinct Names joined as "X and Y"; three or more Names joined with a trailing
Oxford comma ("X, Y, and Z"). This display Name, not any single contributor's own Name, is the
entry's identity for rendering purposes — grouping/equality itself is unaffected, since Name is
already excluded from the structural profile equality above.

#### Scenario: Same weapon profile from different components is combined
- **WHEN** the Bodyguard unit has 4 models carrying a weapon profile with 3 Attacks each, and the attached Leader carries a wargear item with an identical structural profile but 7 Attacks
- **THEN** the aggregate view shows one combined entry with a total Attacks value of 19 (4 × 3 + 7), not the Attacks value of either contributor alone

#### Scenario: Weapon count reflects casualties
- **WHEN** 2 of the 4 Bodyguard models carrying a given weapon profile are removed as casualties
- **THEN** the aggregate view's total Attacks for that weapon profile decreases by the removed models' share

#### Scenario: A fully-removed loadout contributes no row to a shared weapon's breakdown
- **WHEN** a statline entry has two loadouts sharing a weapon (e.g. both carry a Bolt Pistol), one
  loadout is fully removed as casualties (0 remaining) while the other survives, and a third,
  unrelated model-line also carries that same weapon profile
- **THEN** that weapon's aggregated entry reports a total Attacks reflecting only the surviving
  loadout and the unrelated model-line, and its contribution list contains no entry at all for the
  fully-removed loadout — not a contribution showing a Count of 0

#### Scenario: Differently-modified copies of a same-named weapon are not combined
- **WHEN** two components each carry a same-named weapon, but one copy has an ability (e.g. Lethal Hits) that the other does not
- **THEN** the aggregate view shows them as two separate entries, each with its own total Attacks

#### Scenario: Weapons differing only by a targeting/eligibility keyword are not combined
- **WHEN** two weapons match on Type, Skill, Strength, AP, and Damage, but differ in a keyword flag such as Pistol, Assault, or Ignores Cover
- **THEN** the aggregate view shows them as two separate entries, each with its own total Attacks — a targeting/eligibility keyword is as much a part of the weapon's identity as a damage-modifying one

#### Scenario: Contributions are retained on a merged entry
- **WHEN** two or more model-lines contribute to the same aggregated weapon entry
- **THEN** the entry's contribution list includes one item per contributing model-line, each reporting that model-line's own remaining count, per-model Attacks, and weapon Name

#### Scenario: A merged entry from differently-named weapons reports a composite display Name
- **WHEN** two contributions share an identical structural profile but come from weapons named
  "Bolt rifle" and "Combat rifle" respectively
- **THEN** the aggregated entry's display Name is "Bolt rifle and Combat rifle", not either name
  alone

#### Scenario: A merged entry from three or more differently-named weapons uses an Oxford comma
- **WHEN** three contributions share an identical structural profile but come from three
  differently-named weapons, encountered in the order "Bolt rifle", "Combat rifle", "Auto rifle"
- **THEN** the aggregated entry's display Name is "Bolt rifle, Combat rifle, and Auto rifle"
