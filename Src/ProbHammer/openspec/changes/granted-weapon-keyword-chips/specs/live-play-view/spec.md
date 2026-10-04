## ADDED Requirements

### Requirement: Granted Weapon Keyword Chips
A keyword an applied grant added to a weapon, or that replaced one of its own keywords, SHALL render
as an amber chip. A not-added keyword grant SHALL render as a chip in the conditional colour, after
the weapon's own chips. A grant that changed nothing (the weapon already had the keyword at an equal
or better value) SHALL render nothing extra, and the weapon's own chip SHALL render as it does today.

#### Scenario: An applied grant renders an amber chip
- **WHEN** an unconditional Lethal Hits grant reaches a weapon without Lethal Hits
- **THEN** the weapon renders a Lethal Hits chip in amber beside its own chips

#### Scenario: A not-added grant renders a conditional-colour chip
- **WHEN** a conditional Lance grant reaches a weapon without Lance
- **THEN** the weapon renders a Lance chip in the conditional colour, and the weapon's own keywords
  are otherwise unchanged

#### Scenario: A keyword the weapon already has renders once, plainly
- **WHEN** a Lethal Hits grant reaches a weapon that already has Lethal Hits
- **THEN** the weapon renders a single, plain Lethal Hits chip

#### Scenario: A better value replaces the weapon's own chip
- **WHEN** an unconditional Sustained Hits 2 grant reaches a weapon that has Sustained Hits 1
- **THEN** the weapon renders a Sustained Hits 2 chip in amber and no Sustained Hits 1 chip

#### Scenario: A not-added better value sits beside the weapon's own chip
- **WHEN** a conditional Sustained Hits 2 grant reaches a weapon that has Sustained Hits 1
- **THEN** the weapon renders its plain Sustained Hits 1 chip and a Sustained Hits 2 chip in the
  conditional colour

### Requirement: Granted Keyword Chip Popover
A granted keyword chip SHALL be a popover trigger even when its keyword has no glossary entry. Its
popover SHALL list each granting ability as an ability-name trigger, with what it replaces for a
replacing grant, or its condition and that it was not added for a not-added grant, above the
keyword's rule text when the glossary has one.

#### Scenario: An applied grant's popover names its source
- **WHEN** a player taps an amber Lethal Hits chip granted by a Detachment rule
- **THEN** the popover lists that Detachment rule as an ability-name trigger above Lethal Hits' rule
  text

#### Scenario: A replacing grant's popover says what it replaces
- **WHEN** a player taps an amber Sustained Hits 2 chip that replaced the weapon's own Sustained
  Hits 1
- **THEN** the popover lists the granting ability and that it replaces Sustained Hits 1

#### Scenario: A not-added grant's popover gives its condition
- **WHEN** a player taps a conditional-colour Lance chip
- **THEN** the popover lists the granting ability with its condition text and that it was not added

#### Scenario: A weapon's own chip popover is unchanged
- **WHEN** a player taps the plain Lethal Hits chip of a weapon that also received a redundant Lethal
  Hits grant
- **THEN** the popover shows only Lethal Hits' rule text, with no granting ability listed

## MODIFIED Requirements

### Requirement: Weapon Section Rendering
Each unit block SHALL group the view's `Weapons` into a Ranged section and a Melee section by each
entry's `Profile.Type`, rendering the Ranged section before the Melee section, and SHALL omit
either section entirely when it has no entries. Within each section, entries SHALL be ordered by
descending expected value of their aggregated `TotalAttacks`. Each entry SHALL show its name (the
composite display Name computed from every distinct weapon Name merged into it — see the Aggregate
Weapon Count View requirement — not an arbitrary single contributor's Name), Skill, Strength, AP,
Damage, the aggregated total Attacks value, and one chip per token in its keyword text
(`Profile.KeywordsText`, which already includes applied keyword grants — see the Aggregate Weapon
Count View requirement) followed by one chip per not-added keyword grant (per "Granted Weapon
Keyword Chips"), each rendered as its own bordered chip immediately alongside the entry's
name rather than in a separate column or joined with other keywords into a single bracketed group.
Ranged weapon entries SHALL additionally show
Range; Melee weapon entries SHALL NOT show a Range value, since it is always the fixed literal
"Melee" for that weapon type and carries no information beyond the section it's already listed
under. No entry's default (collapsed) rendering SHALL display a raw per-model Attacks value
alongside a separate model count.

When the unit has more than one `ModelLine` in total across all of its components, every weapon
entry's name SHALL be an interactive trigger that toggles a contribution breakdown rendered
directly beneath that entry's row, regardless of how many contributions that specific entry has -
knowing which single `ModelLine` a weapon came from is informative on its own when the unit has
other `ModelLine`s it could be distinguished from. When the unit has exactly one `ModelLine` in
total, a weapon entry SHALL still render its name as an interactive breakdown trigger if that entry
carries a recorded Attacks Effect amount (per "Weapon Attacks Breakdown Ability Lines") — so the
ability line stays reachable even with nothing else to distinguish — and SHALL NOT render such a
trigger otherwise, since there is no second source to distinguish it from; a changed Strength, AP or
Damage value is reachable through its own value provenance highlight instead. The breakdown SHALL group contributions by `(ComponentName, StatlineName)`:
when every contribution in a group shares both the same `PerModelAttacks` and the same set of
recorded Attacks Effect amounts (a group of exactly one contribution trivially satisfies this), the
group SHALL render as one row showing that group's name, its summed `Count`, its shared
`PerModelAttacks`, and the product of the two as a subtotal in the Attacks column, with every other
stat column left blank; when a group's contributions disagree on `PerModelAttacks`, on their own
recorded Attacks Effect amounts, or both, each contributing `ModelLine` SHALL instead render as its
own row within that group, so no subtotal is ever shown that a reader could not verify from the
values beside it. Breakdown rows SHALL be styled visually distinct from primary weapon-entry rows,
and SHALL share the same odd/even row styling as the primary entry row they belong to rather than
alternating independently.

Each weapon entry's rendering in this section is additionally scoped by the unit block's current
statline/loadout selection (see "Selection-Scoped Weapon Filtering"). An entry with at least one
contribution belonging to a currently-selected statline or loadout SHALL render, with its Attacks
total recomputed from only its currently-selected contributions rather than all of them. An entry
with no contribution belonging to a currently-selected statline or loadout SHALL NOT render in this
section at all. The contribution-breakdown group-merge rule above is further scoped by selection
agreement: a group SHALL render as one collapsed row only when every contribution in it shares the
same `PerModelAttacks`, the same set of recorded Attacks Effect amounts, and the same current
selection state; a group whose contributions disagree on any of those, SHALL render each
contributing `ModelLine` as its own row, and any contribution that is not currently selected SHALL be
omitted from the breakdown entirely rather than rendered as a struck-through or otherwise inert row.
Each section's disclosure `<summary>` SHALL render a "filtered" indicator whenever the unit block's
current selection would hide or resize at least one of that specific section's own entries, and
SHALL render no such indicator when the current selection has no effect on that section, even if it
affects the other weapon section of the same unit block.

#### Scenario: Weapon count reflects aggregation across components
- **WHEN** a unit's aggregate view has a weapon entry whose total Attacks was built from
  contributions with different per-model Attacks values
- **THEN** the entry's default (collapsed) rendering shows only that entry's aggregated total
  Attacks, and does not show any individual contribution's per-model Attacks value or a separate
  model count; that detail is available only by activating the entry's contribution breakdown

#### Scenario: Any weapon entry can expand a contribution breakdown when the unit has multiple ModelLines
- **WHEN** a unit has more than one `ModelLine` in total across all of its components
- **THEN** every one of that unit's weapon entries renders its name as an interactive
  trigger, and activating it reveals a breakdown of rows beneath the entry, one row (or group of
  rows) per distinct `(ComponentName, StatlineName)` group among its contributions - including an
  entry whose `Contributions` has only one item

#### Scenario: A single contribution still renders as one breakdown row
- **WHEN** a weapon entry has exactly one contribution, and the unit has more than one `ModelLine`
  in total
- **THEN** activating the entry's trigger reveals exactly one breakdown row naming that single
  contribution's `(ComponentName, StatlineName)`, its `Count`, its `PerModelAttacks`, and their
  product as the subtotal

#### Scenario: No breakdown trigger when the unit has only one ModelLine total
- **WHEN** a unit has exactly one `ModelLine` in total across all of its components, and none of
  that unit's weapon entries carry a resolved value marker, an unresolved ability reference, or a
  recorded Attacks Effect amount
- **THEN** none of that unit's weapon entries render an interactive trigger on their name, and no
  breakdown is available for any of them

#### Scenario: A flagged weapon entry keeps its breakdown trigger even on a single-ModelLine unit
- **WHEN** a unit has exactly one `ModelLine` in total, and one of its weapon entries carries a
  recorded Attacks Effect amount
- **THEN** that entry still renders its name as an interactive breakdown trigger, while an entry
  whose only change is a Strength, AP or Damage value renders no trigger

#### Scenario: Uniform contributions within a group collapse to one row
- **WHEN** a contribution breakdown group's contributions all share the same `PerModelAttacks` and
  the same set of recorded Attacks Effect amounts
- **THEN** the group renders as a single row showing the group's `(ComponentName, StatlineName)`
  label, the contributions' summed `Count`, the shared `PerModelAttacks`, and their product as
  that row's Attacks subtotal

#### Scenario: Disagreeing contributions within a group render separately
- **WHEN** a contribution breakdown group's contributions do not all share the same
  `PerModelAttacks`
- **THEN** each contributing model-line renders as its own row within that group, each showing
  its own `Count`, its own `PerModelAttacks`, and their product as that row's Attacks subtotal

#### Scenario: A recorded Attacks amount splits a breakdown group from an otherwise-identical sibling
- **WHEN** two contributions in the same breakdown group share an identical base `PerModelAttacks`,
  and one of them has a recorded Attacks Effect amount while the other does not
- **THEN** the two contributions no longer collapse to one row; each renders as its own row, showing
  its own `Count`, its own base `PerModelAttacks`, and their product as that row's Attacks subtotal —
  the affected row's own recorded amount(s) render per "Weapon Ability-Contribution Row Rendering",
  nested beneath it

#### Scenario: Breakdown rows are visually distinct from weapon entries
- **WHEN** a contribution breakdown is expanded
- **THEN** its rows render in a style distinguishable from primary weapon-entry rows, and show no
  value in any stat column other than the Attacks subtotal

#### Scenario: Ranged weapons show their Range value
- **WHEN** a unit block renders its Ranged Weapons section
- **THEN** each entry shows that weapon's Range value

#### Scenario: Melee weapons omit the Range column
- **WHEN** a unit block renders its Melee Weapons section
- **THEN** no entry shows a Range value or column, since it would always read "Melee"

#### Scenario: Ranged weapons render before Melee weapons
- **WHEN** a unit's aggregate view has both Ranged- and Melee-typed weapon entries
- **THEN** the rendered block shows all Ranged entries in a section preceding the section
  containing all Melee entries

#### Scenario: A unit with only one weapon type omits the other section
- **WHEN** a unit's aggregate view has weapon entries of only one `WeaponType`
- **THEN** the rendered block shows only the matching section, with no empty section rendered for
  the absent type

#### Scenario: Weapons within a section are ordered by descending expected attacks
- **WHEN** a section has two weapon entries whose `TotalAttacks` expected values differ (e.g. a
  fixed value of `12` and a dice expression `D6`)
- **THEN** the entry with the higher expected value renders above the entry with the lower expected
  value

#### Scenario: Ability tags render inline next to the weapon name
- **WHEN** a weapon entry's `Profile.KeywordsText` has more than one token (e.g. `"Torrent"`,
  `"Pistol"`, and `"Ignores Cover"`, or a token this system has never seen before)
- **THEN** the rendered entry shows one separate chip per token, immediately alongside the weapon's
  name, showing that token's exact source text — not one chip containing all of them joined
  together, and never a chip omitted because the token's meaning is unrecognized

#### Scenario: A keyword chip with no matching glossary entry is not interactive
- **WHEN** a weapon keyword chip's underlying tag text has no matching entry in the glossary (per
  `rules-glossary`'s "Glossary Lookup By Normalized Name Or Alias" requirement)
- **THEN** the chip still renders showing that keyword's text, but is not an interactive trigger
  and opens no popover

#### Scenario: A weapon entry disappears when none of its contributions are selected
- **WHEN** every contribution of a weapon entry belongs to a statline or loadout that is currently
  deselected
- **THEN** that entry does not render in its Ranged or Melee section at all

#### Scenario: A weapon entry's total recomputes when only some of its contributions are selected
- **WHEN** a weapon entry has contributions from more than one statline or loadout, and only some
  of them are currently selected (e.g. Crusader Squad's Bolt Pistol, carried by Neophyte, both
  Initiate loadouts, and the attached Crusade Ancient, totalling 10 Attacks when everything is
  selected)
- **THEN** the entry stays visible with its Attacks total recomputed from only the currently
  selected contributions (e.g. deselecting only the Power-fist-armed Initiate loadout drops the
  total from 10 to 8)

#### Scenario: A deselected contribution's breakdown row disappears rather than rendering inert
- **WHEN** a weapon entry's contribution breakdown group contains a contribution that is currently
  deselected, alongside one or more contributions that remain selected
- **THEN** the deselected contribution's row is entirely absent from the breakdown; it does not
  render struck through, greyed out, or otherwise present-but-inert

#### Scenario: A breakdown group splits when its contributions disagree on selection state
- **WHEN** a contribution breakdown group's contributions all share the same `PerModelAttacks` but
  disagree on current selection state (e.g. Crusader Squad's Bolt Pistol "Initiate" group, after
  the Power-fist-armed Initiate loadout is deselected while the Astartes-chainsword-armed Initiate
  loadout stays selected)
- **THEN** the group no longer renders as one collapsed row; each selected contributing `ModelLine`
  renders as its own row, and the deselected one is omitted

#### Scenario: A weapon section shows a filtered indicator only when it is itself affected
- **WHEN** the unit block's current selection would hide or resize at least one entry in the
  Ranged Weapons section, but has no effect on any entry in the Melee Weapons section
- **THEN** the Ranged Weapons section's disclosure summary shows a filtered indicator and the Melee
  Weapons section's disclosure summary does not

#### Scenario: A weapon entry merged from differently-named weapons renders its composite name
- **WHEN** a weapon entry's contributions were merged from weapons named "Bolt rifle" and "Combat
  rifle" (identical structural profile, different catalogue names)
- **THEN** the entry's rendered name is "Bolt rifle and Combat rifle", and that composite name is
  the interactive trigger for the entry's contribution breakdown when the unit has more than one
  `ModelLine` in total

### Requirement: Value Provenance Highlight
A Statline tile (M, T, Sv, W, Ld, OC, InSv) or a weapon-table Attacks, Strength, AP or Damage value
SHALL render as an inset tile with a corner tick whenever an ability has something to say
about it: an applied ability change, a caveat left unresolved, or a conditional effect that was not
added. The tile SHALL be amber, except that a value reached only by conditional effects that were
not added SHALL use the conditional colour. The tile SHALL be a popover trigger. A value nothing
touches SHALL render plain.

#### Scenario: An ability-modified weapon value is highlighted
- **WHEN** Helbrecht leads a Crusader Squad and Crusade of Wrath adds 1 to the Strength of the
  squad's melee weapons
- **THEN** each affected melee weapon's Strength value renders as the highlighted tile, and its AP
  value, which no ability touches, renders plain

#### Scenario: An ability-modified Statline value is highlighted
- **WHEN** Faith-Fuelled Resolve adds 1 to a Sword Brethren Squad unit's Objective Control
- **THEN** each affected run's OC tile renders highlighted

#### Scenario: A caveated value is highlighted
- **WHEN** a run's invulnerable save is still caveated because its linked ability could not be
  resolved
- **THEN** that InSv tile renders highlighted, the same as a modified value

#### Scenario: A value with only a not-added conditional effect is highlighted
- **WHEN** a Chaos Lord carries Chance for Glory (once per battle, +1 to the Strength, Attacks, AP
  and Damage of its melee weapons)
- **THEN** the Daemon hammer's Attacks, Strength, AP and Damage values each render highlighted in
  the conditional colour, each still showing its unmodified value

#### Scenario: Tapping a highlighted value opens its provenance popover
- **WHEN** a player taps a highlighted value
- **THEN** that value's provenance popover opens, per "Value Provenance Popover Content"

#### Scenario: A value with an applied change and a not-added effect stays amber
- **WHEN** one value is reached by an applied ability change and by a different ability's conditional
  effect that was not added
- **THEN** that value renders highlighted in amber, not the conditional colour

#### Scenario: A caveated value stays amber
- **WHEN** a run's invulnerable save is still caveated and no other ability reaches it
- **THEN** that InSv tile renders highlighted in amber, not the conditional colour
