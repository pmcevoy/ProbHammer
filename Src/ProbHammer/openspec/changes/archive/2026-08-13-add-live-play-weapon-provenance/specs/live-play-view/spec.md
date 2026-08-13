## MODIFIED Requirements

### Requirement: Weapon Section Rendering
Each unit block SHALL group the view's `Weapons` into a Ranged section and a Melee section by each
entry's `Profile.Type`, rendering the Ranged section before the Melee section, and SHALL omit
either section entirely when it has no entries. Within each section, entries SHALL be ordered by
descending expected value of their aggregated `TotalAttacks`. Each entry SHALL show the weapon's
name, Skill, Strength, AP, Damage, the aggregated total Attacks value, and any ability tags
rendered inline immediately next to the weapon's name rather than in a separate column. Ranged
weapon entries SHALL additionally show Range; Melee weapon entries SHALL NOT show a Range value,
since it is always the fixed literal "Melee" for that weapon type and carries no information
beyond the section it's already listed under. No entry's default (collapsed) rendering SHALL
display a raw per-model Attacks value alongside a separate model count.

When the unit has more than one `ModelLine` in total across all of its components, every weapon
entry's weapon name SHALL be an interactive trigger that toggles a contribution breakdown rendered
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

#### Scenario: Weapon count reflects aggregation across components
- **WHEN** a unit's aggregate view has a weapon entry whose total Attacks was built from
  contributions with different per-model Attacks values
- **THEN** the entry's default (collapsed) rendering shows only that entry's aggregated total
  Attacks, and does not show any individual contribution's per-model Attacks value or a separate
  model count; that detail is available only by activating the entry's contribution breakdown

#### Scenario: Any weapon entry can expand a contribution breakdown when the unit has multiple ModelLines
- **WHEN** a unit has more than one `ModelLine` in total across all of its components
- **THEN** every one of that unit's weapon entries renders its weapon name as an interactive
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
- **WHEN** a weapon entry's `Profile` has one or more active ability flags (e.g. `RapidFire > 0`)
- **THEN** the rendered entry shows those abilities as tags immediately alongside the weapon's
  name, not in a separate table column
