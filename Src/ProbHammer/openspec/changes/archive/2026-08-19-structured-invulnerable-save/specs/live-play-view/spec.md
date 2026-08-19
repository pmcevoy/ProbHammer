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

A run's shared invulnerable save SHALL render as a single value when its melee and ranged values
are equal and it is not caveated (the pre-existing rendering); otherwise it SHALL render both
values distinguished by attack type, and, when caveated, a visual indicator — distinct from the
non-caveated rendering — that the value depends on an associated ability not otherwise shown by
this rendering.

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
header line. This selection toggle remains available on an entry/loadout whose remaining count is
0, per "Casualty Tracking Is Independent of Selection Filtering" — activating it still updates the
selection state, even though a fully-dead entry or loadout contributes nothing to any weapon total
regardless of its selection state (see the Aggregate Weapon Count View requirement).

A run's shared stat-tile SHALL collapse to a header-only presentation — every entry's header line
(and nested loadout breakdown, when rendered) still shown, but the M/T/Sv/W/Ld/Oc stat-tile itself
hidden — whenever either of two independent conditions holds for every entry belonging to that run:
every entry is in the fully-deselected state, or every entry has a remaining count of 0 (i.e. is
fully dead — for a multi-loadout entry, every one of its loadouts is at 0 remaining). The stat-tile
SHALL expand back to its full presentation the moment any one entry in the run no longer meets
either condition. A run of one entry collapses under the same rule using that entry's own state.
The fully-deselected trigger is purely a rendering consequence of the entries' current, client-side
selection state, and reverses the moment any entry in the run is reselected or becomes partial. The
fully-dead trigger is a consequence of `RemainingCount`, which is known at render time on the
server; the page SHALL render a run already collapsed on this basis in its initial markup, so it is
the default appearance after a reload rather than something a player must re-trigger — unlike the
fully-deselected trigger, it SHALL NOT reverse by reselecting the entry, only by increasing its
remaining count back above 0. Neither trigger changes which weapons render in the Ranged/Melee
sections beyond what "Selection-Scoped Weapon Filtering" and the Aggregate Weapon Count View
requirement already govern.

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
- **WHEN** a run's shared Statline value has an invulnerable save whose melee and ranged values are
  equal and non-caveated
- **THEN** the rendered stat-tile displays that single value

#### Scenario: Invulnerable save is omitted when absent
- **WHEN** a run's shared Statline value has an invulnerable save whose melee and ranged values are
  both 0 (the default)
- **THEN** the rendered stat-tile does not display an invulnerable save value

#### Scenario: Differing melee and ranged values render distinctly
- **WHEN** a run's shared Statline value has a non-caveated invulnerable save whose melee and
  ranged values differ
- **THEN** the rendered stat-tile displays both values, distinguished by attack type, rather than a
  single merged value

#### Scenario: Caveated invulnerable save renders with an indicator
- **WHEN** a run's shared Statline value has a caveated invulnerable save
- **THEN** the rendered stat-tile displays its melee/ranged values together with a visual indicator
  distinct from the non-caveated rendering, showing that additional information exists

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

#### Scenario: A run collapses once every entry in it is fully dead
- **WHEN** every entry belonging to a run has a remaining count of 0 (for a multi-loadout entry,
  every one of its loadouts is at 0)
- **THEN** the run's stat-tile hides, leaving only each entry's own header line (and nested loadout
  breakdown, when rendered) visible, showing 0 against its initial count

#### Scenario: A fully-dead run is collapsed by default after a reload
- **WHEN** a player reloads the page (or reopens the browser) after every entry in a run has
  reached a remaining count of 0
- **THEN** the run's stat-tile renders already collapsed in the page's initial markup, without
  requiring the player to deselect anything first

#### Scenario: Reselecting a fully-dead entry does not expand its run
- **WHEN** a run is collapsed because every entry in it is fully dead, and the player activates one
  of those entries' (or loadouts') selection toggle
- **THEN** the toggle's own selection state changes as usual, but the run's stat-tile remains
  collapsed, since the fully-dead trigger is independent of selection state

#### Scenario: Reverting a casualty on the last entry of a fully-dead run expands it again
- **WHEN** a run is collapsed because every entry in it is fully dead, and the player increases one
  entry's remaining count back above 0
- **THEN** that entry no longer meets the fully-dead condition, and the run's stat-tile expands back
  to its full presentation

#### Scenario: A run with one dead and one living entry does not collapse
- **WHEN** a run has two entries and only one of them has a remaining count of 0, the other still
  having models remaining and being selected
- **THEN** the run's stat-tile remains fully visible, since not every entry in the run meets the
  fully-deselected or fully-dead condition
