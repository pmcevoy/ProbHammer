# live-play-view Specification

## Purpose
TBD - created by archiving change render-aggregate-view. Update Purpose after archive.
## Requirements
### Requirement: Live Play Page Lists Example Army Units
The `/LivePlay` page SHALL render one block per `AttachedUnitAggregateView` returned by
`Examples.View.MyArmy()`, with no unit omitted or duplicated, ordered as follows: views whose
source `ICombatUnit` is an `AttachedUnit` SHALL render before views sourced from a plain `Unit`;
within each of those two groups, views SHALL be ordered by descending total model count (the sum
of `InitialCount` across the view's `Statlines`). Views with equal total model count within a
group SHALL be ordered by ascending `Name` (ordinal, case-insensitive). Each block SHALL be
labeled with that view's `Name` rather than a positional ordinal.

An `AttachedUnit` with zero attached units still counts as attached-sourced for this ordering —
it sorts ahead of plain `Unit`-sourced views even though its rendered content (statlines,
weapons, abilities) is identical to what a plain `Unit` with the same Bodyguard would produce.

#### Scenario: Page loads with the example army
- **WHEN** a user navigates to `/LivePlay`
- **THEN** the page displays one rendered block for each unit in `Examples.View.MyArmy()`, with no
  unit omitted or duplicated

#### Scenario: Unit block is labeled with its resolved Name
- **WHEN** a unit's aggregate view has `Name` "Sword Brethren with Marshal"
- **THEN** the rendered block's header displays "Sword Brethren with Marshal", not a positional
  label such as "Unit 3"

#### Scenario: Attached-sourced units render before plain units
- **WHEN** the example army contains both `AttachedUnit`-sourced and plain `Unit`-sourced views
- **THEN** every attached-sourced block renders above every plain-unit block, regardless of either
  group's model counts

#### Scenario: Larger units render first within a group
- **WHEN** two views in the same group (both attached-sourced, or both plain-unit-sourced) have
  different total model counts
- **THEN** the view with the larger total model count renders first

#### Scenario: Equal-sized units within a group break ties by name
- **WHEN** two views in the same group have equal total model counts (e.g. two "Crusader Squad
  with ..." blocks with the same total model count)
- **THEN** they render adjacent to each other, ordered by ascending `Name`

#### Scenario: An attached unit with no attachments still sorts as attached
- **WHEN** a view is sourced from an `AttachedUnit` with zero attached units
- **THEN** it is grouped with the attached-sourced units for ordering purposes, ahead of
  plain-`Unit`-sourced views, even though its rendered block looks identical to a plain unit's

### Requirement: Statline Section Rendering
Each unit block SHALL group the view's `Statlines` into contiguous runs, where consecutive entries
belong to the same run only if they share both the same `ComponentName` and an identical Statline
value (M/T/Sv/W/Ld/Oc, and InSv when present); an entry whose `ComponentName` or Statline value
differs from its immediate predecessor starts a new run. For each run, the unit block SHALL render
one shared stat-tile showing that run's M/T/Sv/W/Ld/Oc values (and InSv when present), and SHALL
render one header line per entry in the run — showing that entry's own name and remaining/initial
model count as its own distinct element, never concatenated with another entry's header — above
the shared stat-tile, in the run's order. Each entry's header line SHALL render its own nested
loadout breakdown listing every entry in that statline's `Loadouts`, showing each loadout's weapon
list and its own remaining/initial count, unaffected by whether it is sharing a stat-tile with
other entries.

#### Scenario: Squad with a distinct sergeant statline sharing the same values
- **WHEN** a unit's aggregate view contains two consecutive same-component statline entries with
  identical M/T/Sv/W/Ld/Oc values but different names (e.g. "Sword Brother" and "Initiate", both
  Sv3+ — or "Assault Intercessor Sergeant" and "Assault Intercessor")
- **THEN** the rendered block shows one shared stat-tile with two header lines above it, one per
  name, each showing that entry's own remaining/initial count — this reverses the no-merge
  scenario `live-play-gw-layout` introduced, now that each entry's own name/count/loadouts render
  as independent elements within the shared block rather than being lost to a data-level merge

#### Scenario: Statlines with differing values stay on separate rows
- **WHEN** a unit's aggregate view contains three consecutive same-component statline entries
  where the first two share an identical value and the third has a different value (e.g. Sword
  Brother and Initiate at Sv3+, followed by Neophyte at Sv4+)
- **THEN** the rendered page shows the first two under one shared stat-tile, and Neophyte under its
  own separate stat-tile immediately after

#### Scenario: A component boundary breaks the run even when values match
- **WHEN** an AttachedUnit's last-rendered Attached-unit entry and its first-rendered Bodyguard
  entry are adjacent in the Statlines list and happen to share an identical M/T/Sv/W/Ld/Oc value
- **THEN** they render under two separate stat-tiles, never combined into one

#### Scenario: A statline with no adjacent value-equal neighbor renders alone
- **WHEN** a statline entry's Statline value differs from both its predecessor's and successor's
  values (or it has none)
- **THEN** it renders as a run of one: its own header line above its own stat-tile, identical in
  appearance to how a single entry rendered before this change

#### Scenario: Loadout breakdown lists every contributing model-line
- **WHEN** a statline entry's `Loadouts` contains two entries with different weapon lists and counts
- **THEN** that entry's header line's nested breakdown shows both, each with its own weapon list and remaining/initial count, regardless of whether the entry is sharing a stat-tile with others

#### Scenario: Single-loadout entry has no redundant breakdown row
- **WHEN** a statline entry's `Loadouts` contains exactly one entry
- **THEN** its header line shows that entry's remaining/initial count and renders no nested
  loadout breakdown, regardless of whether the entry is sharing a stat-tile with others

#### Scenario: Invulnerable save renders when present
- **WHEN** a run's shared Statline value has an `InSv` value set
- **THEN** the rendered stat-tile displays that invulnerable save value

#### Scenario: Invulnerable save is omitted when absent
- **WHEN** a run's shared Statline value has an unset `InSv` value (the default)
- **THEN** the rendered stat-tile does not display an invulnerable save value

### Requirement: Weapon Section Rendering
Each unit block SHALL group the view's `Weapons` into a Ranged section and a Melee section by each
entry's `Profile.Type`, rendering the Ranged section before the Melee section, and SHALL omit
either section entirely when it has no entries. Within each section, entries SHALL be ordered by
descending expected value of their aggregated `TotalAttacks`. Each entry SHALL show the weapon's
name, Skill, Strength, AP, Damage, the aggregated total Attacks value, and any ability tags
rendered inline immediately next to the weapon's name rather than in a separate column. Ranged
weapon entries SHALL additionally show Range; Melee weapon entries SHALL NOT show a Range value,
since it is always the fixed literal "Melee" for that weapon type and carries no information
beyond the section it's already listed under. No entry SHALL display a raw per-model Attacks value
alongside a separate model count.

#### Scenario: Weapon count reflects aggregation across components
- **WHEN** a unit's aggregate view has a weapon entry whose total Attacks was built from
  contributions with different per-model Attacks values
- **THEN** the rendered entry shows only that entry's aggregated total Attacks, and does not show
  any individual contribution's per-model Attacks value or a separate model count

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

### Requirement: Ability Section Rendering
Each unit block SHALL render `UnitScopedAbilities` as one combined list, and SHALL render `ModelScopedAbilities` grouped under the name of their owning model-line, separately from the combined list.

#### Scenario: Unit-scoped ability appears once in the combined section
- **WHEN** a unit's aggregate view has a non-empty `UnitScopedAbilities` list
- **THEN** each ability appears once in the block's combined-abilities section

#### Scenario: Model-scoped ability appears under its own model-line
- **WHEN** a unit's aggregate view has a `ModelScopedAbilities` entry tied to a specific model-line
- **THEN** that ability is rendered under that model-line's own heading, not in the combined-abilities section

### Requirement: Keyword Section Rendering
Each unit block SHALL render the view's `Keywords` as a single list of the unit's effective keywords.

#### Scenario: Keywords render when present
- **WHEN** a unit's aggregate view has a non-empty `Keywords` set
- **THEN** every keyword in that set is rendered somewhere in the unit's block

### Requirement: Empty Sections Render Without Error
If any of `Statlines`, `Weapons`, `UnitScopedAbilities`, `ModelScopedAbilities`, or `Keywords` is empty for a given unit, the page SHALL render that unit's block without error, omitting or clearly marking the empty section.

#### Scenario: Unit with no unit-scoped abilities
- **WHEN** a unit's aggregate view has an empty `UnitScopedAbilities` list
- **THEN** the page renders that unit's block successfully, with no exception and no blank/broken markup in the abilities section

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
