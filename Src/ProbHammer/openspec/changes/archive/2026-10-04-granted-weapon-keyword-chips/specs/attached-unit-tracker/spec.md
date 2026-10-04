## MODIFIED Requirements

### Requirement: Aggregate Weapon Count View
The Attached Unit aggregate view SHALL aggregate weapons across all component Units by structural
profile equality (matching weapon Type, Skill, Strength, AP, Damage, and the weapon's keyword
text — its `KeywordsText` token list, compared as a case-folded, trimmed, order-independent set,
so a difference in token order or casing alone does not split what should be one aggregated entry
while any real difference in the keyword set still does — excluding Name, Range, and Attacks —
Attacks stays excluded from this grouping identity
regardless of any ability affecting it, since it is the quantity being aggregated, not part of a
weapon's structural identity), and SHALL report a total Attacks value computed by summing each
contributing model-line's per-model Attacks scaled by that model-line's remaining count. Each
aggregated entry SHALL retain a list of the individual contributions (owning component name,
statline name, remaining count, per-model Attacks, and the contributing weapon's own Name) that the
total was built from. A model-line with a remaining count of 0 SHALL NOT produce a contribution at
all — neither a zero-valued entry nor any entry — regardless of whether that statline name's entry
still appears in the Aggregate Statline View.

Each aggregated entry SHALL additionally report a display Name computed from the distinct Name
values among its own contributions, in first-encountered order: unchanged when every contribution
shares one Name; two distinct Names joined as "X and Y"; three or more Names joined with a trailing
Oxford comma ("X, Y, and Z"). This display Name, not any single contributor's own Name, is the
entry's identity for rendering purposes — grouping/equality itself is unaffected, since Name is
already excluded from the structural profile equality above.

Before this structural grouping runs, each contributing weapon's own resolved profile SHALL be
mutated by every applicable, unconditional (per `ability-classification-catalogue`'s Unconditional
Effect Rule) Strength, Armour Penetration, or Damage weapon-characteristic effect in the
ability-classification catalogue: an effect matches when a present ability on the contribution's own
bearer (per Target-Scoped Application below) has a catalogue record containing it, and when the
effect's own weapon selector matches that contribution's weapon profile (an unqualified selector
matches any weapon; a class-qualified selector matches only a profile of the named weapon type; a
name-qualified selector matches only a profile with that exact name, ignoring case). A conditional
effect SHALL NOT be applied. Grouping itself is otherwise unaffected — structural profile equality
still determines which contributions combine into one entry, now evaluated against each
contribution's own (possibly mutated) profile: a mutation reaching every current contributor of what
would otherwise be one group leaves that group merged, reporting the mutated value; a mutation
reaching only some of those contributors splits them into a separate entry from the unaffected ones.

An applicable, unconditional Attacks weapon-characteristic effect, matched under the same
Target-Scoped Application rule, does NOT mutate the contribution's own per-model Attacks value the
way a Strength/Armour Penetration/Damage match does — Attacks is excluded from the structural profile
equality above, so mutating it in place would have no grouping effect to produce and would discard
the per-ability attribution a consumer needs. Instead, each aggregated entry SHALL retain, per
contribution, a list of every matched, unconditional Attacks effect that reaches it — the source
Ability and its own resolved signed per-model amount (per `weapon-characteristic-effect-resolution`'s
"Resolving An Attacks-Characteristic Effect Into A Per-Model Amount") — leaving the contribution's
own base per-model Attacks value unchanged. The aggregated entry's total Attacks value (above) SHALL
be computed from each contribution's base per-model Attacks value plus the sum of that
contribution's own recorded Attacks effect amounts, scaled by its remaining count — a contribution
with no recorded Attacks effects contributes its plain base value, unaffected, exactly as today.

A matched catalogue record's own classified target scope SHALL determine which contributions its
effects reach — for a Strength/Armour Penetration/Damage mutation, an Attacks effect's recorded
per-contribution amount, a keyword grant, or a not-applied effect (below) alike: a target scoped to the ability's
own bearer SHALL reach only weapons contributed by that bearer's own model-line (when the matched
ability is model-line-sourced) or by any model-line of that bearer's owning component (when the
matched ability is component-wide); a target scoped to the bearer's whole attached unit SHALL reach
matching weapons contributed by every component of the resolved unit, regardless of which component
granted the matched ability. A matched record whose own classified target is a set of keywords, or
is unconditionally roster-wide with no bearer/unit qualifier, SHALL NOT reach any contribution — the
same outcome as an unmatched ability — except an ability whose Origin is Detachment Rule, whose
keyword target was already evaluated when it was attached to this unit: it SHALL reach matching
weapons contributed by every component of the resolved unit, the same treatment
`statline-flag-rules`' Target-Scoped Application gives it.

In addition, when a present ability's catalogue record contains a conditional Strength, Armour
Penetration, Damage or Attacks weapon-characteristic effect whose weapon selector matches a
contribution's weapon profile under the same Target-Scoped Application matching rule above, the
aggregate view SHALL record on that contribution a not-applied effect naming the source ability, the
characteristic, the effect's resolved signed per-model amount, and its condition (the record's usage
limit and turn ownership, the effect's condition text, and whether it is one branch of a choice) —
without mutating the profile or recording an Attacks effect amount for that effect.
The applied/not-applied split is decided per effect, not per record: one record can contribute an
applied mutation through one unconditional effect and a not-applied effect through another,
conditional effect. This not-applied signal is independent of the applied-mutation/
recorded-amount signal above: a contribution can carry an applied mutation or a recorded Attacks
amount and a not-applied effect at the same time, and an aggregated entry SHALL report the distinct
not-applied effects of all its contributions at the entry level as well as at the individual
contribution level, so a consumer can show them per characteristic without inspecting every
contribution.

Before structural grouping, each contribution's own keyword text SHALL also be resolved against
every applicable, unconditional weapon keyword grant whose selector matches its weapon profile under
the same Target-Scoped Application rule, per `weapon-characteristic-effect-resolution`'s "Resolving
A Weapon Keyword Grant". Each grant that adds or replaces a keyword SHALL be recorded on the
contribution with its source ability and any replaced keyword. Because keyword text is part of the
structural profile equality, an applied grant reaching only some of a group's contributors splits
them into a separate entry, and a grant reaching all of them leaves the group merged. A conditional
grant SHALL instead be recorded as a not-applied grant (keyword, source ability, condition) on the
contribution and, distinct by source ability and keyword, at the entry level; it SHALL NOT change
the contribution's keywords or its grouping.

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
- **WHEN** two weapons match on Type, Skill, Strength, AP, and Damage, but differ in their keyword
  text (e.g. one carries `"Pistol"`, `"Assault"`, or `"Ignores Cover"` and the other doesn't)
- **THEN** the aggregate view shows them as two separate entries, each with its own total Attacks —
  a targeting/eligibility keyword is as much a part of the weapon's identity as a damage-modifying
  one

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
  and one of those model-lines' own bearer carries a present ability whose catalogue record contains
  an unconditional effect whose weapon selector matches that weapon and names Strength, Armour
  Penetration, or Damage
- **THEN** the aggregate view shows two separate entries — one reporting the mutated value for the
  affected contribution, one reporting the original value for the unaffected one

#### Scenario: A unit-scoped weapon-characteristic effect keeps every reached contributor merged
- **WHEN** every present model-line of a resolved unit carries a structurally identical weapon
  matching an unconditional effect in a catalogue record whose classified target is scoped to the
  bearer's whole attached unit
- **THEN** the aggregate view shows one merged entry reporting the mutated value, combining every
  contributor exactly as it would if none of them had been mutated

#### Scenario: A caveated baseline entry does not mutate a weapon profile
- **WHEN** a present ability's catalogue record contains a weapon-characteristic effect that is
  conditional
- **THEN** no weapon profile is mutated by that effect, and the affected weapon's aggregate entry
  reports its original, unmutated value

#### Scenario: A caveated baseline entry surfaces an unresolved ability reference
- **WHEN** a present ability's catalogue record contains a conditional weapon-characteristic effect
  whose weapon selector matches a contribution's weapon profile under the same bearer/unit
  target-scoping rule an applied effect would use
- **THEN** the affected contribution carries a not-applied effect naming that ability, the
  characteristic, the signed per-model amount and the condition, and the aggregated entry containing
  that contribution reports the same not-applied effect, even though no value was mutated or recorded

#### Scenario: An unresolved reference and an applied mutation can coexist on one contribution
- **WHEN** a contribution's bearer carries two present abilities whose catalogue records each contain
  a weapon-characteristic effect with a matching weapon selector — one conditional, one not — each
  naming a different characteristic on the same weapon
- **THEN** the contribution's resolved profile reflects the unconditional effect's mutation, and the
  contribution still carries a not-applied effect naming the conditional effect's own ability and
  characteristic

#### Scenario: One record can both apply and defer
- **WHEN** a present ability's catalogue record contains an unconditional Strength effect and a
  conditional Damage effect, both matching a contribution's weapon
- **THEN** the contribution's Strength is mutated, its Damage is not, and it carries a not-applied
  Damage effect naming that ability

#### Scenario: A class-qualified weapon selector leaves a non-matching weapon type unaffected
- **WHEN** a present ability's catalogue record contains an unconditional effect whose weapon
  selector is scoped to melee weapons, and the same bearer also contributes a ranged weapon
- **THEN** the bearer's melee weapon is mutated and the bearer's ranged weapon is not

#### Scenario: An unresolvable characteristic within an otherwise-applicable effect is left unapplied
- **WHEN** a matched catalogue record's unconditional effects include both a characteristic this
  mechanism resolves (Strength, Armour Penetration, Damage, or Attacks) and a characteristic it does
  not yet resolve (e.g. Weapon Skill or Ballistic Skill), scoped to the same weapon
- **THEN** the resolvable characteristic's value is mutated or recorded as applicable, and the
  unresolvable characteristic's value is left unapplied, rather than the match being rejected
  outright or the aggregation failing

#### Scenario: A matched effect naming both Attacks and a structural characteristic resolves both, independently
- **WHEN** a matched catalogue record's unconditional effects include both the Attacks characteristic
  and a resolvable structural characteristic (Strength, Armour Penetration, or Damage), scoped to the
  same weapon
- **THEN** the contribution's structural characteristic value is mutated, and a separate Attacks
  effect amount naming the same source ability is recorded against the contribution — neither
  displaces the other

#### Scenario: A matched Attacks effect is recorded as a per-contribution amount, not a profile mutation
- **WHEN** a present ability's catalogue record contains an unconditional Attacks effect whose weapon
  selector matches a contribution's weapon profile
- **THEN** that contribution records the source ability and its own resolved per-model amount, and
  the contribution's own base per-model Attacks value — the one feeding this weapon's structural
  profile equality — is left unchanged

#### Scenario: A contribution's total Attacks reflects its base value plus every recorded amount
- **WHEN** a contribution has one or more recorded Attacks effect amounts alongside its own base
  per-model Attacks value
- **THEN** the aggregated entry's total Attacks value sums, for that contribution, its base value
  plus every recorded amount, scaled by the contribution's own remaining count

#### Scenario: A recorded Attacks amount never splits or merges a group
- **WHEN** two contributions share an identical structural profile, and one of them has a recorded
  Attacks effect amount while the other does not
- **THEN** both contributions remain in the same aggregated entry, since a recorded Attacks amount
  plays no part in the structural profile equality that determines grouping

#### Scenario: A once-per-battle effect is recorded with its usage limit
- **WHEN** a Chaos Lord carries Chance for Glory, whose catalogue record has a once-per-battle usage
  limit and improves the Strength, Attacks, AP and Damage of the bearer's melee weapons by 1
- **THEN** the Daemon hammer's contribution carries four not-applied effects, one per characteristic,
  each with its signed amount and the once-per-battle limit as its condition, and none of those
  values is changed

#### Scenario: A unit-wide keyword grant keeps every reached contributor merged
- **WHEN** every present model-line of a resolved unit carries a structurally identical melee weapon,
  and an unconditional grant of Lethal Hits to melee weapons, scoped to the bearer's whole attached
  unit, reaches all of them
- **THEN** the aggregate view shows one merged entry whose keywords include Lethal Hits, with the
  grant recorded against each contribution

#### Scenario: A bearer-scoped keyword grant splits an otherwise-merged group
- **WHEN** two model-lines carry structurally identical bolt pistols, and an unconditional grant of
  Lethal Hits reaches only one model-line's bearer
- **THEN** the aggregate view shows two separate bolt pistol entries, one with Lethal Hits and one
  without

#### Scenario: A conditional keyword grant is recorded without splitting
- **WHEN** a conditional grant of Lance reaches only some of a merged weapon entry's contributors
- **THEN** the entry stays merged, its keywords do not include Lance, and the entry reports a
  not-applied Lance grant naming its source ability and condition

#### Scenario: A grant the weapon already has is not recorded
- **WHEN** an unconditional grant of Lethal Hits reaches a weapon that already has Lethal Hits
- **THEN** the weapon's keywords are unchanged and no grant is recorded against its contribution

#### Scenario: A Detachment-rule ability reaches every component's weapons
- **WHEN** a present ability whose Origin is Detachment Rule, and whose catalogue record's classified
  target is a set of keywords, grants Assault to ranged weapons
- **THEN** every component's ranged weapons in that resolved unit gain Assault, rather than the
  ability reaching no contribution at all
