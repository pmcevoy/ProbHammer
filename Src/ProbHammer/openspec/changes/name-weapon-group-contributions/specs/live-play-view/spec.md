## MODIFIED Requirements

### Requirement: Weapon Section Rendering
Each unit block SHALL group the view's `Weapons` into a Ranged section and a Melee section by each
entry's `Profile.Type`, rendering the Ranged section before the Melee section, and SHALL omit
either section entirely when it has no entries. Within each section, entries SHALL be ordered by
descending expected value of their aggregated `TotalAttacks`. Each entry SHALL show its name (the
composite display Name computed from every distinct weapon Name merged into it — see the Aggregate
Weapon Count View requirement — not an arbitrary single contributor's Name), Skill, Strength, AP,
Damage, the aggregated total Attacks value, and any active ability keywords, each rendered as its
own bordered chip immediately alongside the entry's name rather than in a separate column or joined
with other keywords into a single bracketed group. Ranged weapon entries SHALL additionally show
Range; Melee weapon entries SHALL NOT show a Range value, since it is always the fixed literal
"Melee" for that weapon type and carries no information beyond the section it's already listed
under. No entry's default (collapsed) rendering SHALL display a raw per-model Attacks value
alongside a separate model count.

When the unit has more than one `ModelLine` in total across all of its components, every weapon
entry's name SHALL be an interactive trigger that toggles a contribution breakdown rendered
directly beneath that entry's row, regardless of how many contributions that specific entry has -
knowing which single `ModelLine` a weapon came from is informative on its own when the unit has
other `ModelLine`s it could be distinguished from. When the unit has exactly one `ModelLine` in
total, no weapon entry SHALL render such a trigger, since there is no second source to distinguish
it from. The breakdown SHALL group contributions by `(ComponentName, StatlineName)`: when every
contribution in a group shares the same `PerModelAttacks` (a group of exactly one contribution
trivially satisfies this), the group SHALL render as one row showing that group's name, its summed
`Count`, its shared `PerModelAttacks`, and the product of the two as a subtotal in the Attacks
column, with every other stat column left blank; when a group's contributions disagree on
`PerModelAttacks`, each contributing `ModelLine` SHALL instead render as its own row within that
group, so no subtotal is ever shown that a reader could not verify from the values beside it.
Breakdown rows SHALL be styled visually distinct from primary weapon-entry rows, and SHALL share
the same odd/even row styling as the primary entry row they belong to rather than alternating
independently.

Each weapon entry's rendering in this section is additionally scoped by the unit block's current
statline/loadout selection (see "Selection-Scoped Weapon Filtering"). An entry with at least one
contribution belonging to a currently-selected statline or loadout SHALL render, with its Attacks
total recomputed from only its currently-selected contributions rather than all of them. An entry
with no contribution belonging to a currently-selected statline or loadout SHALL NOT render in this
section at all. The contribution-breakdown group-merge rule above is further scoped by selection
agreement: a group SHALL render as one collapsed row only when every contribution in it shares both
the same `PerModelAttacks` and the same current selection state; a group whose contributions
disagree on `PerModelAttacks`, on selection state, or both, SHALL render each contributing
`ModelLine` as its own row, and any contribution that is not currently selected SHALL be omitted
from the breakdown entirely rather than rendered as a struck-through or otherwise inert row. Each
section's disclosure `<summary>` SHALL render a "filtered" indicator whenever the unit block's
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
- **WHEN** a unit has exactly one `ModelLine` in total across all of its components
- **THEN** none of that unit's weapon entries render an interactive trigger on their name, and no
  breakdown is available for any of them

#### Scenario: Uniform contributions within a group collapse to one row
- **WHEN** a contribution breakdown group's contributions all share the same `PerModelAttacks`
- **THEN** the group renders as a single row showing the group's `(ComponentName, StatlineName)`
  label, the contributions' summed `Count`, the shared `PerModelAttacks`, and their product as
  that row's Attacks subtotal

#### Scenario: Disagreeing contributions within a group render separately
- **WHEN** a contribution breakdown group's contributions do not all share the same
  `PerModelAttacks`
- **THEN** each contributing model-line renders as its own row within that group, each showing
  its own `Count`, its own `PerModelAttacks`, and their product as that row's Attacks subtotal

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
- **WHEN** a weapon entry's `Profile` has more than one active ability flag (e.g. both `Torrent`
  and `Pistol` and `IgnoresCover`)
- **THEN** the rendered entry shows three separate chips immediately alongside the weapon's name —
  one per keyword — not one chip containing all three joined together

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
