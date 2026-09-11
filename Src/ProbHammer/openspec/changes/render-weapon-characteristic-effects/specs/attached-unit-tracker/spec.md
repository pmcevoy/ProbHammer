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

Before this structural grouping runs, each contributing weapon's own resolved profile SHALL be
mutated by every applicable weapon-characteristic Effect recorded against a checked-in, human-
verified baseline entry whose classification is not caveated: an entry matches when a present
ability on the contribution's own bearer (per Target-Scoped Application below) has normalized text
equal to that baseline entry's own Text, and when the Effect's own weapon selector matches that
contribution's weapon profile (an unqualified selector matches any weapon; a class-qualified
selector matches only a profile of the named weapon type; a name-qualified selector matches only a
profile with that exact name). A caveated baseline entry's Effects SHALL NOT be applied. Grouping
itself is otherwise unaffected — structural profile equality still determines which contributions
combine into one entry, now evaluated against each contribution's own (possibly mutated) profile: a
mutation reaching every current contributor of what would otherwise be one group leaves that group
merged, reporting the mutated value; a mutation reaching only some of those contributors splits them
into a separate entry from the unaffected ones.

A matched baseline entry's own classified target scope SHALL determine which contributions it
mutates: a target scoped to the ability's own bearer SHALL mutate only weapons contributed by that
bearer's own model-line (when the matched ability is model-line-sourced) or by any model-line of
that bearer's owning component (when the matched ability is component-wide); a target scoped to the
bearer's whole attached unit SHALL mutate matching weapons contributed by every component of the
resolved unit, regardless of which component granted the matched ability. A matched entry whose own
classified target is scoped to a named keyword, or is unconditionally roster-wide with no bearer/
unit qualifier, SHALL NOT mutate any contribution — the same outcome as an unmatched ability.

In addition, when a present ability's normalized text matches a checked-in baseline entry whose
classification IS caveated, and that entry's own weapon selector matches a contribution's weapon
profile under the same Target-Scoped Application matching rule above, the aggregate view SHALL
record that contribution as carrying an unresolved ability reference naming the source ability —
without mutating the profile. This unresolved-reference signal is independent of the applied-
mutation signal above: a contribution can carry an applied mutation from one matched ability and an
unresolved reference from a different matched ability at the same time, and an aggregated entry
whose contributions collectively carry at least one unresolved reference anywhere in the group SHALL
report that fact at the entry level as well as at the individual contribution level, so a consumer
can flag the group without inspecting every contribution.

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

#### Scenario: A bearer-scoped weapon-characteristic effect splits an otherwise-merged group
- **WHEN** two model-lines from different components carry structurally identical melee weapons,
  and one of those model-lines' own bearer carries a present ability matching a checked-in,
  non-caveated baseline entry whose weapon selector matches that weapon
- **THEN** the aggregate view shows two separate entries — one reporting the mutated Strength/Armour
  Penetration/Damage value for the affected contribution, one reporting the original value for the
  unaffected one

#### Scenario: A unit-scoped weapon-characteristic effect keeps every reached contributor merged
- **WHEN** every present model-line of a resolved unit carries a structurally identical weapon
  matching a checked-in, non-caveated baseline entry whose classified target is scoped to the
  bearer's whole attached unit
- **THEN** the aggregate view shows one merged entry reporting the mutated value, combining every
  contributor exactly as it would if none of them had been mutated

#### Scenario: A caveated baseline entry does not mutate a weapon profile
- **WHEN** a present ability's normalized text matches a checked-in baseline entry whose
  classification is caveated
- **THEN** no weapon profile is mutated by that entry, and the affected weapon's aggregate entry
  reports its original, unmutated value

#### Scenario: A caveated baseline entry surfaces an unresolved ability reference
- **WHEN** a present ability's normalized text matches a checked-in baseline entry whose
  classification is caveated, and that entry's weapon selector matches a contribution's weapon
  profile under the same bearer/unit target-scoping rule an applied effect would use
- **THEN** the affected contribution carries an unresolved ability reference naming that ability,
  and the aggregated entry containing that contribution reports the group as having an unresolved
  reference, even though no value was mutated

#### Scenario: An unresolved reference and an applied mutation can coexist on one contribution
- **WHEN** a contribution's bearer carries two present abilities matching two different checked-in
  baseline entries with matching weapon selectors — one caveated, one not — each naming a different
  characteristic on the same weapon
- **THEN** the contribution's resolved profile reflects the non-caveated entry's mutation, and the
  contribution still carries an unresolved ability reference naming the caveated entry's own ability

#### Scenario: A class-qualified weapon selector leaves a non-matching weapon type unaffected
- **WHEN** a present ability matches a checked-in, non-caveated baseline entry whose weapon selector
  is scoped to melee weapons, and the same bearer also contributes a ranged weapon
- **THEN** the bearer's melee weapon is mutated and the bearer's ranged weapon is not

#### Scenario: An unresolvable characteristic within an otherwise-applicable effect is left unapplied
- **WHEN** a matched, non-caveated baseline entry's Effects include both a resolvable characteristic
  (Strength, Armour Penetration, or Damage) and the Attacks characteristic, scoped to the same weapon
- **THEN** the resolvable characteristic's value is mutated and the weapon's Attacks value is
  unchanged
