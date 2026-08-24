# live-play-view Specification

## Purpose
TBD - created by archiving change render-aggregate-view. Update Purpose after archive.
## Requirements
### Requirement: Live Play Page Lists Example Army Units
The `/LivePlay` page SHALL render one block per `AttachedUnitAggregateView` produced from the
current session's imported and enriched `ArmyRoster` (see `army-list-import`'s Per-Session Roster
Storage requirement), with no unit omitted or duplicated, ordered as follows: views whose source
`ICombatUnit` is an `AttachedUnit` SHALL render before views sourced from a plain `Unit`; within
each of those two groups, views SHALL be ordered by descending total model count (the sum of
`InitialCount` across the view's `Statlines`). Views with equal total model count within a group
SHALL be ordered by ascending `Name` (ordinal, case-insensitive). Each block SHALL be labeled with
that view's `Name` rather than a positional ordinal.

An `AttachedUnit` with zero attached units still counts as attached-sourced for this ordering —
it sorts ahead of plain `Unit`-sourced views even though its rendered content (statlines, weapons,
abilities) is identical to what a plain `Unit` with the same Bodyguard would produce.

#### Scenario: Page loads with the example army
- **WHEN** a user with an active session import navigates to `/LivePlay`
- **THEN** the page displays one rendered block for each unit in that session's enriched
  `ArmyRoster`, with no unit omitted or duplicated

#### Scenario: Unit block is labeled with its resolved Name
- **WHEN** a unit's aggregate view has `Name` "Sword Brethren with Marshal"
- **THEN** the rendered block's header displays "Sword Brethren with Marshal", not a positional
  label such as "Unit 3"

#### Scenario: Attached-sourced units render before plain units
- **WHEN** the session's imported army contains both `AttachedUnit`-sourced and plain
  `Unit`-sourced views
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

### Requirement: Live Play Redirects Without An Active Import
When `/LivePlay` is requested by a session with no successfully imported army list, the system
SHALL redirect the user to the import page (per `army-list-import`'s Import Submission
requirement) rather than rendering an empty or erroring page.

#### Scenario: A session with no import is redirected
- **WHEN** a user with no prior successful import navigates to `/LivePlay`
- **THEN** they are redirected to the import page instead of seeing a roster

### Requirement: Selection-Scoped Weapon Filtering
Each unit block SHALL maintain one shared statline/loadout selection state, driving both its
Statline section's selection indicators (see "Statline Section Rendering") and its Ranged/Melee
Weapons sections' filtering (see "Weapon Section Rendering") — a single act of selecting or
deselecting a statline or loadout affects all three sections of the same unit block together, never
just one of them. Selection state SHALL default to every statline and loadout selected when the
page loads, matching this page's rendering before this requirement existed. Each unit block SHALL
render a "Clear filter" control that resets its own selection state back to fully selected; this
control SHALL be visible only while that unit block's current selection is not fully selected, and
SHALL NOT be shown otherwise. Selection state SHALL be held only in client-side page state and
SHALL NOT persist across a page reload, consistent with this page's existing architecture of
rebuilding every unit block fresh from the server on each request.

#### Scenario: Everything is selected by default
- **WHEN** the page loads and the player has not yet interacted with any statline or loadout
- **THEN** every statline and loadout in every unit block is selected, and every unit block's
  Ranged/Melee sections render their full, unfiltered totals exactly as they did before this
  requirement existed

#### Scenario: Clear filter control appears only while filtering is active
- **WHEN** a unit block's current selection differs from fully selected
- **THEN** that unit block renders a Clear filter control; deactivating it restores every statline
  and loadout in that unit block to selected, and the control itself stops rendering once the
  selection is fully selected again

#### Scenario: One selection state drives all three sections of a unit block
- **WHEN** a statline or loadout in a unit block's Statline section is deselected
- **THEN** that same unit block's Ranged and Melee Weapons sections immediately reflect the change
  (per "Weapon Section Rendering"), without any separate, section-specific selection state

#### Scenario: Selection state does not persist across a page reload
- **WHEN** the player has deselected one or more statlines or loadouts and then reloads the page
- **THEN** the reloaded page shows every statline and loadout selected again, since selection state
  is not part of any persisted session or server-side state

### Requirement: Statline Section Rendering
Each unit block SHALL group the view's `Statlines` into contiguous runs, where consecutive entries
belong to the same run only if they share both the same `ComponentName` and an identical Statline
value (M/T/Sv/W/Ld/Oc, and InSv when present); an entry whose `ComponentName` or Statline value
differs from its immediate predecessor starts a new run. For each run, the unit block SHALL render
one shared stat-tile showing that run's M/T/Sv/W/Ld/Oc values, and SHALL render one header line per
entry in the run — showing that entry's own name and remaining/initial model count as its own
distinct element, never concatenated with another entry's header — above the shared stat-tile, in
the run's order. Each entry's header line SHALL render its own nested loadout breakdown listing
every entry in that statline's `Loadouts`, showing each loadout's own remaining/initial count and a
weapon label computed as that loadout's weapon list with the multiset (bag) intersection of weapons
shared by every loadout in that same statline entry's `Loadouts` subtracted out — i.e. only the
weapons that distinguish this loadout from its siblings under the same entry. This statline
rendering occupies the first of three columns in the unit block's Statline area — see "Statline
Ability Column Rendering" for the other two.

A run's shared invulnerable save, when present, SHALL render as its own tile in the same visual
family as the M/T/Sv/W/Ld/Oc tiles (a label above its value, inside one bordered box), positioned
below that row and aligned under the Sv tile specifically. Its label is always "InSv" (or "InSv*"
when caveated) — never varying with the save's shape — and any attack-type indicator instead rides
alongside the value itself, inside the tile:
- A uniform, non-caveated save (melee and ranged values equal and non-zero) renders a single value
  with no attack-type indicator, since it applies equally to both.
- A non-caveated save restricted to one attack type (the other value 0) renders that single value
  paired with an indicator for only the attack type it applies to.
- A non-caveated save with two known, non-zero, differing values renders both values, each paired
  with its own attack-type indicator, as two stacked rows within the tile — the ranged value above
  the melee value, mirroring the page's own Ranged-before-Melee weapon section ordering — rather
  than a single merged value or a single line joined by a separator.
- A caveated save renders its single kept value with no attack-type indicator — since which attack
  type it applies to is not known at this layer — in a tile background visually distinct from the
  non-caveated tile's background. The associated `CaveatAbility`'s full descriptive text SHALL
  render beneath the tile, at the unit block's normal reading width. This is the first place in the
  unit block that renders an ability's descriptive text rather than only its name; it is scoped to
  this one rendering and does not extend to any other ability shown elsewhere in the block.
- A save whose melee and ranged values are both 0 (the default) renders no box at all.

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
(and nested loadout breakdown, when rendered) still shown, but the M/T/Sv/W/Ld/Oc stat-tile itself,
and the invulnerable save box and its caveat ability text when the run has one, hidden — whenever
either of two independent conditions holds for every entry belonging to that run:
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
- **WHEN** a run's shared Statline value has a non-caveated invulnerable save whose melee and
  ranged values are equal and non-zero
- **THEN** the rendered tile shows "InSv" as its label and that single value beneath it, with no
  attack-type indicator alongside the value

#### Scenario: Invulnerable save is omitted when absent
- **WHEN** a run's shared Statline value has an invulnerable save whose melee and ranged values are
  both 0 (the default)
- **THEN** the run renders no invulnerable save tile

#### Scenario: Melee-only invulnerable save renders with a melee-specific indicator
- **WHEN** a run's shared Statline value has a non-caveated invulnerable save whose ranged value is
  0 and melee value is non-zero
- **THEN** the rendered tile shows "InSv" as its label and the melee value, paired with an
  indicator for melee attacks only, beneath it

#### Scenario: Ranged-only invulnerable save renders with a ranged-specific indicator
- **WHEN** a run's shared Statline value has a non-caveated invulnerable save whose melee value is
  0 and ranged value is non-zero
- **THEN** the rendered tile shows "InSv" as its label and the ranged value, paired with an
  indicator for ranged attacks only, beneath it

#### Scenario: Differing melee and ranged values render distinctly
- **WHEN** a run's shared Statline value has a non-caveated invulnerable save whose melee and
  ranged values are both non-zero and differ from each other
- **THEN** the rendered tile shows "InSv" as its label and both values as two stacked rows beneath
  it, the ranged value above the melee value, each paired with its own attack-type indicator,
  rather than a single merged value or a single line joined by a separator

#### Scenario: Caveated invulnerable save renders with an indicator
- **WHEN** a run's shared Statline value has a caveated invulnerable save
- **THEN** the rendered tile shows "InSv*" as its label and its single kept value beneath it, with
  no attack-type indicator, in a background visually distinct from a non-caveated tile's background

#### Scenario: Caveated invulnerable save's ability text renders beneath the box
- **WHEN** a run's shared Statline value has a caveated invulnerable save
- **THEN** the associated `CaveatAbility`'s full descriptive text renders beneath the invulnerable
  save tile — the only place in the unit block where an ability's text, rather than only its name,
  is shown

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

### Requirement: Statline Ability Column Rendering
Each unit block's Statline area SHALL render two additional columns alongside the statline column:
a Model Abilities column for entries whose Ability has Scope Model, and a Unit Abilities column for
entries whose Ability has Scope Unit. Within each column, an ability entry bound to a specific
statline row SHALL render beside that row only; an ability entry with no statline-row binding but
belonging to one component (a component-wide ability) SHALL render beside the first rendered
statline row belonging to its component, and SHALL visually span every row belonging to that
component; an ability entry belonging to no single component (an Army Rule-origin ability, always
promoted regardless of contributor count) SHALL render as its own row, above every component's
statline rows, aligned to none of them. Each ability SHALL render by Name only; ability descriptive
Text is not rendered inline by this requirement, but is available on demand via the "Ability And
Rule Text Popover" requirement below.

A row-bound ability cell SHALL collapse (hide its ability names) whenever its own row's run is
collapsed, per "Statline Section Rendering" — i.e. whenever every entry in that run is either
fully deselected or fully dead. A component-wide, row-spanning ability cell SHALL collapse whenever
every run within its visual span is collapsed by that same rule. It SHALL remain fully expanded as
long as at least one entry in at least one row within its span is neither fully deselected nor
fully dead. This applies identically to the Model Abilities and Unit Abilities columns. An ability
entry belonging to no single component SHALL collapse only once every component that contributes
to it has every one of its model-lines reach a remaining count of 0 — not per this-entry's-own-span
like the component-wide case, since the fact it represents remains true as long as any part of the
attached formation still lives.

#### Scenario: A row-bound ability renders beside its own statline row
- **WHEN** an ability entry carries the name of a specific statline
- **THEN** it renders in its Scope's column beside that specific statline row, not beside any
  other row of the same component

#### Scenario: A component-wide ability spans every row of its component
- **WHEN** an ability entry carries no statline name (a Datasheet-sourced, component-wide ability)
  and its owning component renders three statline rows (e.g. two merged into one shared tile and
  one separate)
- **THEN** it renders once, beside the first of those three rows, visually spanning all three

#### Scenario: A component-less ability renders above every component, aligned to none
- **WHEN** an ability entry belongs to no single component (an Army Rule-origin ability, promoted
  whether contributed by one component of a standalone Unit or shared by several of an
  AttachedUnit)
- **THEN** it renders once, in its own row above every component's statline rows, not aligned to
  or spanning any one component's rows specifically

#### Scenario: Column placement is determined by Ability Scope
- **WHEN** a component contributes both a Model-scoped and a Unit-scoped ability
- **THEN** the Model-scoped ability renders in the Model Abilities column and the Unit-scoped
  ability renders in the Unit Abilities column, regardless of whether either is row-bound,
  component-wide, or belongs to no single component

#### Scenario: Only the ability name renders
- **WHEN** an ability entry renders in either column
- **THEN** only its Name is shown inline; its descriptive Text is not rendered inline (see
  "Ability And Rule Text Popover" for on-demand access to it)

#### Scenario: A row-bound ability cell collapses with its own row
- **WHEN** a row-bound ability entry's own row is collapsed because every entry in that row's run
  is fully deselected or fully dead
- **THEN** that ability's name is hidden along with the rest of the row's collapsed content

#### Scenario: A component-wide ability cell stays expanded while any spanned row is still selected
- **WHEN** a component-wide ability visually spans three statline rows and only two of those rows
  are collapsed (fully deselected or fully dead), with the third still having a selected or partial
  entry with a nonzero remaining count
- **THEN** the ability cell remains fully expanded

#### Scenario: A component-wide ability cell collapses once every spanned row is fully deselected
- **WHEN** a component-wide ability visually spans three statline rows and every entry in all three
  rows becomes fully deselected
- **THEN** the ability cell collapses (its ability names hide)

#### Scenario: A component-wide ability cell collapses once every spanned row is fully dead
- **WHEN** a component-wide ability visually spans three statline rows and every entry in all three
  rows reaches a remaining count of 0
- **THEN** the ability cell collapses (its ability names hide), matching the fully-deselected case

#### Scenario: A component-less ability cell stays expanded while any contributing component lives
- **WHEN** a component-less ability entry is contributed by three components, and only two of them
  have every model-line reach a remaining count of 0
- **THEN** the ability cell remains fully expanded, since the third contributing component still
  has a present model-line

#### Scenario: A component-less ability cell collapses once every contributing component is dead
- **WHEN** a component-less ability entry is contributed by three components, and every one of them
  has every model-line reach a remaining count of 0
- **THEN** the ability cell collapses (its ability name hides)

#### Scenario: A row-bound ability sharing a single-row component's span renders together, not hidden
- **WHEN** a component renders exactly one statline row, and that row carries both a row-bound
  ability and a component-wide ability in the same column
- **THEN** both abilities render together in one cell at that row's position — the row-bound
  ability is never rendered as a separate cell that would occupy the identical grid position as
  the component-wide cell and be visually hidden behind it

#### Scenario: A row-bound ability on a multi-row component still renders in its own cell
- **WHEN** a component renders more than one statline row, and one of those rows carries a
  row-bound ability while a component-wide ability spans all of that component's rows
- **THEN** the row-bound ability still renders in its own cell at its specific row, distinct from
  the component-wide cell spanning the full range — only the single-row case merges them

### Requirement: Casualty Marking Control
Each statline entry with exactly one loadout SHALL provide a casualty control on its own header
line; a statline entry with more than one loadout SHALL provide a casualty control on each of its
nested loadout lines instead, never on the parent header line itself, since the header's remaining
count is a sum across its loadouts with no single model-line of its own to adjust. Each such
control SHALL offer a decrease action (mark a casualty) and an increase action (undo a mistaken
mark), each applying only to the one entry or loadout it belongs to, without affecting any sibling
entry, loadout, or statline. A decrease action SHALL have no effect once that entry's or loadout's
remaining count is already 0; an increase action SHALL have no effect once it already equals its
initial count. Activating either action SHALL update the page without a full page reload, and SHALL
NOT change that entry's or loadout's own current selection state (see "Casualty Tracking Is
Independent of Selection Filtering") — including on an entry or loadout whose header/line also acts
as a selection toggle, where activating the casualty control SHALL NOT also toggle that selection.

#### Scenario: Marking a casualty on a single-loadout entry
- **WHEN** a player activates the decrease control on a statline entry showing 5/5 remaining
- **THEN** that entry's remaining count becomes 4/5, and no other entry's count changes, and the
  entry's own current selection state is unchanged

#### Scenario: Marking a casualty on one loadout of a multi-loadout entry
- **WHEN** a player activates the decrease control on one loadout line of a statline entry with two
  loadouts
- **THEN** only that loadout's own remaining count decreases; its sibling loadout's remaining count
  is unaffected, and the parent statline entry's own remaining count (the sum across its loadouts,
  per the Aggregate Statline View requirement) decreases to match

#### Scenario: A multi-loadout entry's own header line has no casualty control
- **WHEN** a statline entry has more than one loadout
- **THEN** its own header line (showing the summed remaining/initial count) provides no casualty
  control; only its individual loadout lines do

#### Scenario: Activating a casualty control does not toggle selection
- **WHEN** a player activates the decrease or increase control on an entry or loadout whose header
  or line also acts as a selection toggle (per "Statline Section Rendering")
- **THEN** that entry's or loadout's remaining count changes as expected, and its current selection
  state (selected/deselected/partial) is unchanged by that activation

#### Scenario: Decrease control has no effect at zero
- **WHEN** a player activates the decrease control on an entry or loadout already at a remaining
  count of 0
- **THEN** its remaining count stays at 0

#### Scenario: Undoing a mistaken casualty mark
- **WHEN** a player activates the increase control on an entry or loadout whose remaining count is
  currently below its initial count
- **THEN** its remaining count increases by 1

#### Scenario: Increase control has no effect at the initial count
- **WHEN** a player activates the increase control on an entry or loadout already at its initial
  count
- **THEN** its remaining count stays at the initial count

#### Scenario: A casualty adjustment does not trigger a page reload
- **WHEN** a player activates a decrease or increase control
- **THEN** the page reflects the updated count without a full page navigation/reload

### Requirement: Casualty State Persists Across Reloads and Restarts
The page SHALL record every casualty adjustment so that reloading the page, or restarting the
browser, on the same browser/device reproduces the same remaining counts shown before the
reload — without the player needing to re-mark anything. This persistence SHALL survive the
server process itself restarting between requests. Casualty state recorded in one browser/device
is not required to appear when `/LivePlay` is opened from a different browser or device.

#### Scenario: Reloading the page preserves marked casualties
- **WHEN** a player marks one or more casualties and then reloads the page
- **THEN** the reloaded page shows the same remaining counts as immediately before the reload

#### Scenario: A browser restart preserves marked casualties
- **WHEN** a player marks one or more casualties, closes the browser entirely, and later reopens it
  to `/LivePlay`
- **THEN** the page shows the same remaining counts as before the browser was closed

#### Scenario: A server restart between requests does not lose casualty state
- **WHEN** the server process restarts after a player has marked one or more casualties, and the
  player then reloads or revisits the page
- **THEN** the page still shows the previously marked remaining counts

#### Scenario: A browser with no recorded adjustments shows the pristine army
- **WHEN** `/LivePlay` is opened in a browser that has never recorded any casualty adjustment
- **THEN** every unit's remaining counts equal its initial counts

### Requirement: Casualty Adjustments Drive the Existing Aggregate View Rules
A casualty adjustment made through the controls in this capability SHALL be treated as a change to
the affected model-line's remaining count for every purpose already governed by
`attached-unit-tracker`'s Aggregate Statline View, Aggregate Weapon Count View, and Aggregate
Ability View requirements — this requirement does not restate those rules, it establishes that a
player-initiated casualty adjustment is an input to them like any other remaining-count change.

#### Scenario: A statline entry persists at 0/N once its last model-line is fully removed
- **WHEN** a player marks casualties until every model-line sharing a statline name reaches a
  remaining count of 0
- **THEN** that statline entry still renders, showing 0 remaining against its initial count, per
  the existing Aggregate Statline View requirement (see "Statline Section Rendering" below for how
  it renders once every entry in its run is in this state)

#### Scenario: A weapon's total recomputes after a casualty
- **WHEN** a player marks a casualty on a model-line that contributes to an aggregated weapon entry
- **THEN** that weapon entry's total Attacks decreases to reflect the reduced remaining count, per
  the existing Aggregate Weapon Count View requirement

#### Scenario: A fully-removed loadout's contribution row disappears from a weapon's breakdown
- **WHEN** a player marks casualties until one loadout of a multi-loadout statline entry reaches a
  remaining count of 0, and that loadout shared a weapon with a still-surviving contributor
- **THEN** that weapon's contribution breakdown no longer includes any row for the fully-removed
  loadout — not a row showing a Count of 0 — per the existing Aggregate Weapon Count View
  requirement

#### Scenario: A model-line-sourced ability disappears once its bearer is fully removed
- **WHEN** a player marks casualties until a model-line carrying its own Ability reaches a
  remaining count of 0
- **THEN** that ability no longer appears in the aggregate view, per the existing Aggregate Ability
  View requirement

### Requirement: Casualty Tracking Is Independent of Selection Filtering
Casualty state and the statline/loadout selection state introduced by selection-scoped weapon
filtering SHALL operate independently: marking or undoing a casualty SHALL NOT change which
statlines or loadouts are currently selected, and an entry's or loadout's current selection state
SHALL NOT gate whether its casualty controls are available or functional.

#### Scenario: Marking a casualty does not change the current filter
- **WHEN** a player has deselected some statlines or loadouts and then marks a casualty on a still-
  selected entry
- **THEN** the deselected entries remain deselected, unaffected by the casualty adjustment

#### Scenario: Casualty controls remain functional on a deselected entry
- **WHEN** a statline entry or loadout is currently deselected
- **THEN** its casualty controls still function, decreasing or increasing its remaining count as
  usual

### Requirement: Weapon Section Rendering
Each unit block SHALL group the view's `Weapons` into a Ranged section and a Melee section by each
entry's `Profile.Type`, rendering the Ranged section before the Melee section, and SHALL omit
either section entirely when it has no entries. Within each section, entries SHALL be ordered by
descending expected value of their aggregated `TotalAttacks`. Each entry SHALL show the weapon's
name, Skill, Strength, AP, Damage, the aggregated total Attacks value, and any active ability
keywords, each rendered as its own bordered chip immediately alongside the weapon's name rather
than in a separate column or joined with other keywords into a single bracketed group. Ranged
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

### Requirement: Ability And Rule Text Popover
Tapping an ability name (in either the Model Abilities or Unit Abilities column) or a weapon
keyword chip that has a matching entry in the glossary (per `rules-glossary`) SHALL open a popover
showing that entry's full descriptive text, without navigating away from or reloading the page. A
weapon keyword chip with no matching glossary entry is not an interactive trigger and opens no
popover (see "Weapon Section Rendering").

#### Scenario: Tapping an ability name opens its rule text
- **WHEN** a player taps an ability name rendered in the Model Abilities or Unit Abilities column
- **THEN** a popover opens showing that ability's full descriptive text

#### Scenario: Tapping a resolvable weapon keyword chip opens its rule text
- **WHEN** a player taps a weapon keyword chip that has a matching glossary entry (e.g. a
  `[LETHAL HITS]` chip resolving to the universal "Lethal Hits" rule)
- **THEN** a popover opens showing that rule's full descriptive text

#### Scenario: Opening a popover does not reload the page
- **WHEN** a player taps an ability name or a resolvable weapon keyword chip
- **THEN** the page's existing state (casualty marks, current statline/loadout selection, expanded
  disclosure sections) is unchanged and no full page navigation occurs

### Requirement: Popover Is Anchored Locally To Its Trigger
A popover SHALL be positioned adjacent to the specific element that was tapped to open it, rather
than centered on the screen or otherwise placed independent of what was tapped, so the player does
not lose their place in the surrounding statline, ability column, or weapon table.

#### Scenario: A popover appears next to what was tapped
- **WHEN** a player taps an ability name or weapon keyword chip anywhere within a unit block
- **THEN** the resulting popover renders positioned adjacent to that specific element, not centered
  on the screen

### Requirement: Popover Remains Functional Without Anchor-Positioning Support
On a browser that does not support local anchor positioning, a popover SHALL still open and remain
fully dismissable exactly as elsewhere in this requirement set — only its placement relative to its
trigger is affected, never whether it appears or can be closed.

#### Scenario: A popover still opens and closes on an unsupporting browser
- **WHEN** a player taps an ability name or a resolvable weapon keyword chip on a browser without
  local anchor-positioning support
- **THEN** a popover still opens showing the expected text and can still be dismissed, even though
  it is not positioned adjacent to its trigger

### Requirement: Popover Dismissal
A popover SHALL be dismissable by tapping anywhere outside it, without requiring a dedicated close
control.

#### Scenario: Tapping outside a popover closes it
- **WHEN** a popover is open and the player taps anywhere outside it
- **THEN** the popover closes

### Requirement: Nested Reference Popover
Within an open popover's own text, a resolved `[BRACKET]` cross-reference (per `rules-glossary`'s
"Glossary Lookup By Normalized Name Or Alias" requirement) SHALL be an interactive trigger, unless
it resolves back to a rule already shown somewhere in that popover's own ancestor chain (a direct
self-reference or a longer cycle), in which case it SHALL be treated the same as an unresolved
token. Tapping a resolved, non-cyclic reference SHALL open a second popover, anchored to that
specific reference, showing the referenced rule's text, without closing the popover it was opened
from. Tapping outside the second popover but still inside the first SHALL close only the second
popover, leaving the first open. Tapping outside both SHALL close both in one action. An unresolved
bracket token (no matching glossary entry) is not an interactive trigger.

#### Scenario: Opening a nested popover keeps the parent open
- **WHEN** a player taps a resolved cross-reference inside an open popover (e.g. tapping
  `[PRECISION]` while reading a detachment rule's own text)
- **THEN** a second popover opens showing the "Precision" rule's text, and the first popover
  remains open and visible

#### Scenario: Dismissing between the two closes only the nested popover
- **WHEN** both a parent and a nested popover are open, and the player taps a location inside the
  parent's own content but outside the nested popover
- **THEN** only the nested popover closes; the parent popover remains open

#### Scenario: Dismissing outside both closes the whole stack
- **WHEN** both a parent and a nested popover are open, and the player taps a location outside both
- **THEN** both popovers close

#### Scenario: An unresolved reference within popover text is not interactive
- **WHEN** an open popover's text contains a `[BRACKET]` token with no matching glossary entry
- **THEN** that token is not an interactive trigger and tapping it opens no further popover

#### Scenario: A self-referencing rule's own reference to itself is not interactive
- **WHEN** a popover is showing a rule whose own text contains a `[BRACKET]` reference that
  resolves back to that same rule (confirmed real shape — the "Sustained Hits", "Anti", and
  "Cleave" sharedRules entries each reference themselves this way in their own description text)
- **THEN** that reference is not an interactive trigger and tapping it opens no further popover,
  the same treatment as an unresolved token — this also applies to a longer reference cycle back
  to any rule already open in the current popover's own ancestor chain, not only a direct
  self-reference

### Requirement: Popover Text Preserves Paragraph Breaks
A popover's rendered text SHALL preserve real line breaks present in the source text as visual
line breaks, rather than collapsing them to a single space or run-on line.

#### Scenario: Multi-paragraph rule text renders with its paragraph break intact
- **WHEN** a popover renders rule text containing a real line break (e.g. the "Lethal Hits" rule's
  Designer's Note, which is a separate paragraph from its main description)
- **THEN** the two paragraphs render as visually distinct lines/paragraphs, not run together as one

### Requirement: Popover Renders Emphasis Markup As Styling, Not As Links
A popover SHALL render source emphasis markup as visual styling only, using three distinct styles:
`*italic*` (single asterisk) as italic text, `**bold**` (double asterisk) as bold text, and
`^^small-caps^^` (double caret) as small-caps text. None of the three SHALL make the wrapped text
an interactive/tappable element merely by virtue of that markup — only a resolved `[BRACKET]`
reference (per "Nested Reference Popover") is tappable, independent of any emphasis markup around
it.

#### Scenario: Single-asterisk text renders italic
- **WHEN** a popover renders text containing `*italic text*`
- **THEN** that text renders in italics

#### Scenario: Double-asterisk text renders bold
- **WHEN** a popover renders text containing `**bold text**`
- **THEN** that text renders in bold

#### Scenario: Double-caret text renders small caps
- **WHEN** a popover renders text containing `^^small caps text^^`
- **THEN** that text renders in small caps

#### Scenario: Bold and small-caps combine when both wrap the same text
- **WHEN** a popover renders text containing `^^**Fabius Bile**^^` (a unit name wrapped in both
  markers at once, confirmed real shape)
- **THEN** that text renders both bold and small-caps, and tapping it opens no popover

#### Scenario: Nested triple-asterisk emphasis renders as combined bold and italic
- **WHEN** a popover renders text containing `***Designer's Note:**` followed by running text that
  stays open until a single closing `*` later in the same paragraph (confirmed real shape — the
  "Lethal Hits" rule's own Designer's Note, where the bold portion closes after two words but the
  italic portion continues to the paragraph's end)
- **THEN** `"Designer's Note:"` renders both bold and italic, and the remaining running text up to
  the closing `*` renders italic only, matching the source markup's own nesting

### Requirement: Keyword Section Rendering
Each unit block SHALL render the view's `Keywords` as a single list of the unit's effective keywords.

#### Scenario: Keywords render when present
- **WHEN** a unit's aggregate view has a non-empty `Keywords` set
- **THEN** every keyword in that set is rendered somewhere in the unit's block

### Requirement: Empty Sections Render Without Error
If any of `Statlines`, `Weapons`, the per-component ability data, or `Keywords` is empty for a
given unit, the page SHALL render that unit's block without error, omitting or clearly marking the
empty section or column.

#### Scenario: Unit with no unit-scoped abilities
- **WHEN** a unit's aggregate view has no Unit-scoped ability entries for any of its components
- **THEN** the page renders that unit's block successfully, with no exception and no blank/broken
  markup in the Unit Abilities column

### Requirement: Per-Section Disclosure Defaults to Collapsed
Each unit block's statline (including its two ability columns), ranged-weapons, and melee-weapons
sections SHALL be independently collapsible, and SHALL render collapsed by default when the page
first loads.

#### Scenario: Sections load collapsed
- **WHEN** a user navigates to `/LivePlay`
- **THEN** every unit block's statline, ranged-weapons, and melee-weapons sections render in a
  collapsed state, showing no section's detail content until expanded

#### Scenario: Expanding one section does not affect others
- **WHEN** a user expands one unit's ranged-weapons section
- **THEN** that unit's other sections, and every section of every other unit block, remain in
  their prior collapsed/expanded state

### Requirement: Enhancement Ability Visual Indicator
Wherever an ability's name renders on the page, an ability whose Origin is Enhancement SHALL
render with a leading `✦` symbol immediately before its name (separated by one space), and an
ability whose Origin is not Enhancement SHALL render its name unprefixed. This applies uniformly
to every place an ability name is displayed, including a rule-text popover's title, since that
title reuses the same rendered name as the trigger that opened it.

#### Scenario: An Enhancement-classified ability renders with the indicator
- **WHEN** an ability entry with Origin Enhancement renders in the Model or Unit Abilities column
- **THEN** its rendered name is prefixed with `✦ ` (the symbol, then a space, then the name)

#### Scenario: A non-Enhancement ability renders with no indicator
- **WHEN** an ability entry whose Origin is not Enhancement (Intrinsic, Optional Grant, or Core
  Rule) renders in the Model or Unit Abilities column
- **THEN** its rendered name has no leading symbol

#### Scenario: A Core Rule ability renders alongside Datasheet abilities with no dedicated section
- **WHEN** a unit's Datasheet carries one or more Core Rule-origin abilities (e.g. a Chapter's own
  Vows, or Oath of Moment)
- **THEN** each renders as its own entry in the Unit Abilities column, in the same list as the
  unit's Intrinsic abilities, with no separate "Core Abilities" or "Faction Abilities" section

#### Scenario: The indicator carries through to the ability's popover title
- **WHEN** a player taps an Enhancement-classified ability's name to open its rule-text popover
- **THEN** the popover's title bar shows the same `✦`-prefixed name as the trigger that opened it

### Requirement: Popover Displays A Title Bar Naming Its Subject
Every rule/ability popover panel SHALL render a title bar above its body text, showing the same
name/label that was tapped to open it (an ability name, a weapon-keyword chip's text, or a nested
`[BRACKET]` reference's rendered label). The title bar SHALL use the same visual style as an
`.lp-section` disclosure summary's title (the section-title style used for the Statline/Ranged
Weapons/Melee Weapons headers): a filled bar spanning the popover's width, positioned flush against
its top edge.

#### Scenario: An ability popover's title bar names the ability
- **WHEN** a player taps an ability name and its popover opens
- **THEN** the popover's title bar shows that same ability's name, above the popover's body text

#### Scenario: A weapon-keyword chip's popover title bar names the keyword
- **WHEN** a player taps a resolvable weapon-keyword chip and its popover opens
- **THEN** the popover's title bar shows that same chip's text, above the popover's body text

#### Scenario: A nested reference popover's title bar names the reference
- **WHEN** a player taps a resolved `[BRACKET]` reference inside an open popover, opening a nested
  popover
- **THEN** the nested popover's title bar shows that reference's own rendered label, above its body
  text

#### Scenario: The title bar shares the section-header visual style
- **WHEN** any rule/ability popover is open
- **THEN** its title bar renders in the same visual style (background, text color, weight, size,
  case, letter-spacing) as an `.lp-section` disclosure summary's title

### Requirement: Unit Header Status Glyphs
Each unit block's name header SHALL render a group of status glyphs right-aligned within that same
header row, alongside the unit's name (which stays left-aligned) — never in a separate row or
section. The group SHALL always render the Battle-shock glyph rightmost.

The half-strength glyph's rendering depends on that combat unit's combined starting strength (per
`attached-unit-tracker`'s Half-Strength Determination requirement):
- When starting strength is exactly 1, the glyph SHALL always render as an interactive control.
  Activating it SHALL toggle that unit's player-set half-strength status (per `attached-unit-
  tracker`'s Single-Model Half-Strength Is Player-Set requirement). It SHALL render in the header's
  ordinary text color while that status is false, and in an amber color while it is true.
- When starting strength is 2 or more, the glyph SHALL render only while the unit is at or below
  half-strength per the computed determination, and SHALL NOT be interactive — activating it SHALL
  have no effect. Whenever shown, it SHALL render in an amber color.

The Battle-shock glyph SHALL always render, for every unit regardless of starting strength, as an
interactive control. Activating it SHALL toggle that unit's Battle-shocked status (per `attached-
unit-tracker`'s Battle-Shocked Status Is Player-Set requirement). It SHALL render as an
outline-only shape, in the header's ordinary text color, while that status is false, and as a
solid shape in an amber color while it is true.

Both glyphs SHALL remain visible, and their interactive cases SHALL remain functional, regardless
of whether that unit block's Statline, Ranged Weapons, or Melee Weapons sections are currently
expanded or collapsed.

#### Scenario: A single-model unit's half-strength glyph is always present and interactive
- **WHEN** a unit block's combat unit has a combined starting strength of 1
- **THEN** its header renders the half-strength glyph regardless of that unit's current
  half-strength status, and activating it toggles that status

#### Scenario: Marking a single-model unit at half-strength changes the glyph's color
- **WHEN** a player activates the half-strength glyph on a unit with a combined starting strength
  of 1, setting its player-set half-strength status to true
- **THEN** the glyph renders in amber; activating it again sets the status back to false and the
  glyph returns to the header's ordinary text color

#### Scenario: A multi-model unit's half-strength glyph is hidden above half-strength
- **WHEN** a unit block's combat unit has a combined starting strength of 2 or more and is not at
  or below half-strength
- **THEN** its header does not render the half-strength glyph

#### Scenario: A multi-model unit's half-strength glyph appears once at or below half-strength
- **WHEN** a unit block's combat unit has a combined starting strength of 2 or more and casualties
  bring it to at or below half-strength
- **THEN** its header renders the half-strength glyph in amber, and activating it has no effect on
  that unit's (computed) half-strength status

#### Scenario: The Battle-shock glyph defaults to an outline
- **WHEN** a unit block's combat unit has a Battle-shocked status of false
- **THEN** its header renders the Battle-shock glyph as an outline-only shape in the header's
  ordinary text color

#### Scenario: Activating the Battle-shock glyph marks the unit Battle-shocked
- **WHEN** a player activates a unit's Battle-shock glyph
- **THEN** that unit's Battle-shocked status becomes true and the glyph renders as a solid amber
  shape; activating it again sets the status back to false and the glyph returns to its outline
  rendering

#### Scenario: The Battle-shock glyph is always rightmost
- **WHEN** a unit block's header renders both the half-strength glyph and the Battle-shock glyph
- **THEN** the Battle-shock glyph renders to the right of the half-strength glyph

#### Scenario: Status glyphs remain visible while every section is collapsed
- **WHEN** a unit block's Statline, Ranged Weapons, and Melee Weapons sections are all collapsed
- **THEN** its header's status glyphs (whichever are currently rendered) remain visible and, for
  their interactive cases, functional

### Requirement: Half-Strength And Battle-Shocked State Persist Across Reloads and Restarts
The page SHALL record every player-set half-strength toggle (for a combined-starting-strength-1
unit) and every Battle-shocked toggle so that reloading the page, or restarting the browser, on the
same browser/device reproduces the same statuses shown before the reload — without the player
needing to re-set anything. This persistence SHALL survive the server process itself restarting
between requests, the same guarantee `live-play-view`'s existing Casualty State Persists Across
Reloads and Restarts requirement makes for casualty state. State recorded in one browser/device is
not required to appear when `/LivePlay` is opened from a different browser or device.

#### Scenario: Reloading the page preserves a marked half-strength status
- **WHEN** a player sets a single-model unit's half-strength status to true and then reloads the
  page
- **THEN** the reloaded page still shows that unit as at or below half-strength

#### Scenario: Reloading the page preserves a marked Battle-shocked status
- **WHEN** a player sets a unit's Battle-shocked status to true and then reloads the page
- **THEN** the reloaded page still shows that unit as Battle-shocked

#### Scenario: A server restart between requests does not lose either status
- **WHEN** the server process restarts after a player has set a half-strength or Battle-shocked
  status, and the player then reloads or revisits the page
- **THEN** the page still shows the previously set status

#### Scenario: A browser with no recorded toggles shows the default statuses
- **WHEN** `/LivePlay` is opened in a browser that has never recorded a half-strength or
  Battle-shocked toggle
- **THEN** every unit shows a player-set half-strength status of false and a Battle-shocked status
  of false, alongside whatever the computed half-strength determination reports for a
  multi-model unit

### Requirement: Battle-Shocked Objective Control Rendering
Every Objective Control stat-tile belonging to any component of a Battle-shocked combat unit SHALL
render using the same visual treatment as a caveated Invulnerable Save tile (a background that
visually distinguishes it from an ordinary stat-tile), with its displayed value replaced by the
same glyph used for the header's Battle-shock control, rendered in the ordinary stat-value text
color — never the tile's numeric Objective Control value, and never in an amber or otherwise
distinct color. The tile's label SHALL remain "OC", unchanged, and no explanatory text SHALL render
beneath it — unlike a caveated Invulnerable Save tile, there is only one fixed reason (the unit is
Battle-shocked), already conveyed by the header glyph, so no per-instance footnote is needed. This
rendering applies to every component's Objective Control tile within a Battle-shocked combat unit,
not only whichever component's header the Battle-shock glyph itself renders against.

#### Scenario: An ordinary unit's Objective Control tile shows its numeric value
- **WHEN** a unit block's combat unit is not Battle-shocked
- **THEN** every Objective Control tile in that block renders its ordinary numeric value with no
  special background

#### Scenario: A Battle-shocked unit's Objective Control tile shows the glyph instead of a number
- **WHEN** a unit block's combat unit is Battle-shocked
- **THEN** every Objective Control tile in that block renders the flagged-tile background and shows
  the Battle-shock glyph, in the ordinary stat-value text color, in place of the numeric value

#### Scenario: Every component's Objective Control tile is affected, not just one
- **WHEN** an AttachedUnit is Battle-shocked and has more than one statline run across its
  Bodyguard and Attached components
- **THEN** every one of those runs' Objective Control tiles renders the flagged treatment, not only
  the run belonging to whichever component the header's own glyph visually sits above

#### Scenario: Clearing Battle-shocked status reverts the tile
- **WHEN** a player clears a unit's Battle-shocked status
- **THEN** every Objective Control tile in that unit's block reverts to its ordinary numeric
  rendering

