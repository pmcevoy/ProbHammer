## MODIFIED Requirements

### Requirement: Statline Section Rendering
Each unit block SHALL group the view's `Statlines` into contiguous runs, where consecutive entries
belong to the same run only if they share both the same `ComponentName` and an identical Statline
value (M/T/Sv/W/Ld/Oc, and InSv when present); an entry whose `ComponentName` or Statline value
differs from its immediate predecessor starts a new run. For each run, the unit block SHALL render
one shared stat-tile showing that run's M/T/Sv/W/Ld/Oc values (and InSv when present), and SHALL
render one header line per entry in the run — showing that entry's own name and remaining/initial
model count as its own distinct element, never concatenated with another entry's header — above
the shared stat-tile, in the run's order. Each entry's header line SHALL render its own nested
loadout breakdown listing every entry in that statline's `Loadouts`, showing each loadout's own
remaining/initial count and a weapon label computed as that loadout's weapon list with the multiset
(bag) intersection of weapons shared by every loadout in that same statline entry's `Loadouts`
subtracted out — i.e. only the weapons that distinguish this loadout from its siblings under the
same entry. This statline rendering occupies the first of three columns in the unit block's
Statline area — see "Statline Ability Column Rendering" for the other two.

Each entry's header line, and each of its nested loadout lines when rendered, SHALL act as an
independent selection toggle within the unit block's shared selection state (see "Selection-Scoped
Weapon Filtering"). A statline entry with more than one loadout SHALL render one of three visually
distinct states: fully selected (every loadout selected), fully deselected (no loadout selected),
or partial (some but not all loadouts selected) — this state SHALL always be derived from the
entry's current loadout selections, never stored independently of them. Activating a loadout line
SHALL toggle only that loadout's own selection. Activating a statline entry's own header line
SHALL select all of its loadouts if the entry's current state is anything other than fully
selected, or deselect all of them if the entry's current state is fully selected. A statline entry
with exactly one loadout (which renders no nested breakdown, per the existing single-loadout rule
below) has only the fully-selected/fully-deselected states, toggled directly by activating its own
header line.

A run's shared stat-tile SHALL collapse to a header-only presentation — every entry's header line
(and nested loadout breakdown, when rendered) still shown, but the M/T/Sv/W/Ld/Oc stat-tile itself
hidden — whenever every entry belonging to that run is in the fully-deselected state. The stat-tile
SHALL expand back to its full presentation the moment any one entry in the run becomes selected or
partial. A run of one entry collapses under the same rule using that entry's own state. This
collapse is purely a rendering consequence of the entries' existing selection states; it never
changes which weapons render in the Ranged/Melee sections beyond what "Selection-Scoped Weapon
Filtering" already governs.

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
- **WHEN** a statline entry's `Loadouts` contains two entries whose weapon lists share some but not
  all weapons (e.g. one loadout carries `[Bolt pistol, Heavy Bolt pistol, Power fist]` and the other
  carries `[Bolt pistol, Heavy Bolt pistol, Astartes chainsword]`)
- **THEN** that entry's header line's nested breakdown shows both, each labeled with only the
  weapons not shared by every loadout in that entry (`"Power fist"` and `"Astartes chainsword"`
  respectively) alongside its own remaining/initial count, regardless of whether the entry is
  sharing a stat-tile with others

#### Scenario: Shared weapons need not be contiguous or equally ordered to be excluded
- **WHEN** a statline entry's `Loadouts` contains two entries whose weapon lists share weapons that
  do not appear at the same list position in each (e.g. one loadout carries
  `[Bolt pistol, Chainsword, Frag grenades]` and the other carries
  `[Chainsword, Plasma pistol, Frag grenades]`)
- **THEN** the shared weapons (`Chainsword`, `Frag grenades`) are excluded from both loadouts'
  labels regardless of position, leaving `"Bolt pistol"` and `"Plasma pistol"` respectively

#### Scenario: A loadout keeps its genuinely-extra copy of an otherwise-shared weapon
- **WHEN** a statline entry's `Loadouts` contains one loadout carrying two copies of a weapon and a
  sibling loadout carrying only one copy of that same weapon
- **THEN** the shared multiset subtracted from each loadout's label contains only one copy of that
  weapon, so the loadout carrying two copies still shows one remaining copy in its label; the
  subtraction never removes more copies of a weapon than a loadout's siblings collectively share

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

#### Scenario: Deselecting one loadout leaves its statline in a partial state
- **WHEN** a statline entry has two loadouts, both currently selected, and the player deselects one
  of them
- **THEN** the statline entry's own rendered state becomes partial (neither fully selected nor
  fully deselected), and the still-selected loadout's own state is unaffected

#### Scenario: Activating a partial or fully-deselected statline selects all of its loadouts
- **WHEN** a statline entry's current state is partial, or fully deselected, and the player
  activates that entry's own header line
- **THEN** every one of its loadouts becomes selected, and the entry's own state becomes fully
  selected

#### Scenario: Activating a fully-selected statline deselects all of its loadouts
- **WHEN** a statline entry's current state is fully selected and the player activates that
  entry's own header line
- **THEN** every one of its loadouts becomes deselected, and the entry's own state becomes fully
  deselected

#### Scenario: A single-entry run collapses when its one entry is fully deselected
- **WHEN** a run has exactly one entry (e.g. Helbrecht, a unique character with no loadout
  variants) and the player deselects it
- **THEN** the run's stat-tile hides, leaving only that entry's own header line (name and count)
  visible in the statline column

#### Scenario: A multi-entry run stays expanded while any of its entries is still selected
- **WHEN** a run has two entries (e.g. Sword Brother and Initiate sharing one Sv3+ stat-tile) and
  the player deselects only one of them
- **THEN** the run's stat-tile remains fully visible, since Initiate is still selected — only once
  Sword Brother and Initiate are both fully deselected does the shared stat-tile hide

#### Scenario: Deselecting one loadout of a multi-loadout entry does not collapse that entry's run
- **WHEN** a statline entry has two loadouts and the player deselects only one, leaving the entry in
  its partial state
- **THEN** the entry's run does not collapse, since a partial entry counts as not fully deselected

#### Scenario: Reselecting any entry in a collapsed run expands it immediately
- **WHEN** a run's stat-tile is currently collapsed because every entry in it is fully deselected,
  and the player reselects any one entry (or any one of its loadouts)
- **THEN** the run's stat-tile becomes visible again immediately

### Requirement: Statline Ability Column Rendering
Each unit block's Statline area SHALL render two additional columns alongside the statline column:
a Model Abilities column for entries whose Ability has Scope Model, and a Unit Abilities column for
entries whose Ability has Scope Unit. Within each column, an ability entry bound to a specific
statline row SHALL render beside that row only; an ability entry with no statline-row binding (a
component-wide ability) SHALL render beside the first rendered statline row belonging to its
component, and SHALL visually span every row belonging to that component. Each ability SHALL
render by Name only; ability descriptive Text is not rendered by this requirement.

A row-bound ability cell SHALL collapse (hide its ability names) whenever its own row's run is
collapsed, per "Statline Section Rendering" — i.e. whenever every entry in that run is fully
deselected. A component-wide, row-spanning ability cell SHALL collapse whenever every run within
its visual span is collapsed — i.e. every entry of every row it spans is fully deselected. It SHALL
remain fully expanded as long as at least one entry in at least one row within its span is selected
or partial. This applies identically to the Model Abilities and Unit Abilities columns.

#### Scenario: A row-bound ability renders beside its own statline row
- **WHEN** an ability entry carries the name of a specific statline
- **THEN** it renders in its Scope's column beside that specific statline row, not beside any
  other row of the same component

#### Scenario: A component-wide ability spans every row of its component
- **WHEN** an ability entry carries no statline name (a Datasheet-sourced, component-wide ability)
  and its owning component renders three statline rows (e.g. two merged into one shared tile and
  one separate)
- **THEN** it renders once, beside the first of those three rows, visually spanning all three

#### Scenario: Column placement is determined by Ability Scope
- **WHEN** a component contributes both a Model-scoped and a Unit-scoped ability
- **THEN** the Model-scoped ability renders in the Model Abilities column and the Unit-scoped
  ability renders in the Unit Abilities column, regardless of whether either is row-bound or
  component-wide

#### Scenario: Only the ability name renders
- **WHEN** an ability entry renders in either column
- **THEN** only its Name is shown; its descriptive Text is not rendered

#### Scenario: A row-bound ability cell collapses with its own row
- **WHEN** a row-bound ability entry's own row is collapsed because every entry in that row's run
  is fully deselected
- **THEN** that ability's name is hidden along with the rest of the row's collapsed content

#### Scenario: A component-wide ability cell stays expanded while any spanned row is still selected
- **WHEN** a component-wide ability visually spans three statline rows and only two of those rows
  are fully deselected, with the third still selected or partial
- **THEN** the ability cell remains fully expanded

#### Scenario: A component-wide ability cell collapses once every spanned row is fully deselected
- **WHEN** a component-wide ability visually spans three statline rows and every entry in all three
  rows becomes fully deselected
- **THEN** the ability cell collapses (its ability names hide)
