## MODIFIED Requirements

### Requirement: Live Play Page Lists Example Army Units
The `/LivePlay` page SHALL render one block per `AttachedUnitAggregateView` returned by
`Examples.View.MyArmy()`, in the order returned, and SHALL label each block with that view's
`Name` rather than a positional ordinal.

#### Scenario: Page loads with the example army
- **WHEN** a user navigates to `/LivePlay`
- **THEN** the page displays one rendered block for each unit in `Examples.View.MyArmy()`, with no
  unit omitted or duplicated

#### Scenario: Unit block is labeled with its resolved Name
- **WHEN** a unit's aggregate view has `Name` "Sword Brethren with Marshal"
- **THEN** the rendered block's header displays "Sword Brethren with Marshal", not a positional
  label such as "Unit 3"

### Requirement: Statline Section Rendering
Each unit block SHALL render one named header-and-stat-tile block per statline entry in the view's
`Statlines`, in the order returned, showing the statline's name as the block's header, its
M/T/Sv/W/Ld/Oc values as individual stat tiles (and an InSv tile when present), and its
remaining/initial model count. When a statline entry's `Loadouts` contains more than one entry, the
block SHALL render a nested loadout breakdown listing every entry, each showing its own weapon list
and remaining/initial count; when `Loadouts` contains exactly one entry, no nested breakdown SHALL
be rendered and the block's own remaining/initial count is sufficient.

#### Scenario: Squad with a distinct sergeant statline sharing the same values
- **WHEN** a unit's aggregate view contains two statline entries with identical M/T/Sv/W/Ld/Oc
  values but different names (e.g. "Assault Intercessor" and "Assault Intercessor Sergeant")
- **THEN** the rendered unit displays two separate statline blocks, one per name, neither merged
  with the other, regardless of their values matching

#### Scenario: Statlines with differing values stay on separate rows
- **WHEN** a unit's aggregate view contains two statline entries whose M/T/Sv/W/Ld/Oc values differ
  from each other
- **THEN** the rendered unit displays two separate statline blocks, each under its own header

#### Scenario: Loadout breakdown lists every contributing model-line
- **WHEN** a statline entry's `Loadouts` contains two entries with different weapon lists and
  counts
- **THEN** the rendered block's nested breakdown shows both, each with its own weapon list and
  remaining/initial count

#### Scenario: Single-loadout entry has no redundant breakdown row
- **WHEN** a statline entry's `Loadouts` contains exactly one entry
- **THEN** the rendered block shows that entry's remaining/initial count in the block header and
  renders no nested loadout breakdown

#### Scenario: Invulnerable save renders when present
- **WHEN** a statline's `InSv` value is set
- **THEN** the rendered block displays that invulnerable save value as its own stat tile

#### Scenario: Invulnerable save is omitted when absent
- **WHEN** a statline's `InSv` value is unset (the default)
- **THEN** the rendered block does not display an invulnerable-save tile

### Requirement: Weapon Section Rendering
Each unit block SHALL group the view's `Weapons` into a Ranged section and a Melee section by each
entry's `Profile.Type`, rendering the Ranged section before the Melee section, and SHALL omit
either section entirely when it has no entries. Within each section, entries SHALL be ordered by
descending expected value of their aggregated `TotalAttacks`. Each entry SHALL show the weapon's
name, Range, Skill, Strength, AP, Damage, and the aggregated total Attacks value, with any ability
tags rendered inline immediately next to the weapon's name rather than in a separate column. No
entry SHALL display a raw per-model Attacks value alongside a separate model count.

#### Scenario: Weapon count reflects aggregation across components
- **WHEN** a unit's aggregate view has a weapon entry whose total Attacks was built from
  contributions with different per-model Attacks values
- **THEN** the rendered entry shows only that entry's aggregated total Attacks, and does not show
  any individual contribution's per-model Attacks value or a separate model count

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

## ADDED Requirements

### Requirement: Per-Section Disclosure Defaults to Collapsed
Each unit block's statline, ranged-weapons, melee-weapons, and abilities sections SHALL be
independently collapsible, and SHALL render collapsed by default when the page first loads.

#### Scenario: Sections load collapsed
- **WHEN** a user navigates to `/LivePlay`
- **THEN** every unit block's statline, ranged-weapons, melee-weapons, and abilities sections
  render in a collapsed state, showing no section's detail content until expanded

#### Scenario: Expanding one section does not affect others
- **WHEN** a user expands one unit's ranged-weapons section
- **THEN** that unit's other sections, and every section of every other unit block, remain in
  their prior collapsed/expanded state
