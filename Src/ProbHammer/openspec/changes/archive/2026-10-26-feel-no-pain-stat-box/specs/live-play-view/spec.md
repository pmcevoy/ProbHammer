## MODIFIED Requirements

### Requirement: Statline Section Rendering
Each unit block SHALL group the view's `Statlines` into contiguous runs, where consecutive entries
belong to the same run only if they share both the same `ComponentName` and an identical Statline
value (M/T/Sv/W/Ld/Oc, and InSv and FNP when present); an entry whose `ComponentName` or Statline value
differs from its immediate predecessor starts a new run. For each run, the unit block SHALL render
one shared stat-tile showing that run's M/T/Sv/W/Ld/Oc values, and SHALL render one header line per
entry in the run — showing that entry's own name and remaining/initial model count as its own
distinct element, never concatenated with another entry's header — above the shared stat-tile, in
the run's order. Each entry's header line SHALL render its own nested loadout breakdown listing
every entry in that statline's `Loadouts`, showing each loadout's own remaining/initial count and a
label computed from that loadout's own weapon list and ability-name list, each independently
reduced by subtracting the multiset (bag) intersection shared by every loadout in that same
statline entry's `Loadouts` — i.e. only the weapons and ability names that distinguish this loadout
from its siblings under the same entry, joined together into one label (distinguishing weapons
before distinguishing ability names). A loadout distinguished from its siblings by neither its
weapons nor its ability names SHALL instead render its own `DisplayName` as its label — the loadout's
own name as recorded by whichever import produced it, distinct from the shared `StatlineName` when
the source data offers something more specific (e.g. "Plague Champion", "Custodian Warden w/
Vexilla"), and equal to the bare `StatlineName` when it does not — so every loadout line always
shows readable, and where available maximally distinguishing, text rather than appearing to be a
rendering gap or repeating an identical label across every tied sibling. This statline rendering
occupies the first of three columns in the unit block's Statline area — see "Statline Ability Column
Rendering" for the other two.

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
  non-caveated tile's background, per "Flagged Statline Characteristic Rendering" below, which
  governs how the save's source is shown to the player.
- A save whose melee and ranged values are both 0 (the default) renders no box at all.

A run's shared Feel No Pain, when present, SHALL render as its own tile in the same family, labelled
"FNP", on the same row as the invulnerable save tile and aligned under the W tile, showing its
threshold (e.g. "5+"). A run with no Feel No Pain renders no FNP tile, unless the tile is highlighted
(see "Value Provenance Highlight"), in which case it shows "–".

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
the invulnerable save box, any other flagged Statline tile the run has, and their shared legend (per
"Flagged Statline Characteristic Rendering" below), hidden — whenever either of two independent
conditions holds for every entry belonging to that run:
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

#### Scenario: Loadout label falls back to a distinguishing ability when weapons don't distinguish
- **WHEN** a statline entry's `Loadouts` contains two entries whose weapon lists are identical
  (e.g. both carry `[Guardian Spear, Guardian Spear]`) but one loadout's `ModelLine` carries an
  ability the other's does not (e.g. one carries a "Vexilla" ability granted by its own wargear
  selection, the other carries none), and neither loadout's `DisplayName` differs from the entry's
  own `StatlineName`
- **THEN** the weapon-only subtraction leaves both loadouts with nothing distinguishing, so the
  label falls to the ability-name subtraction instead: the loadout with no distinguishing ability
  renders its own `DisplayName` (here, equal to the bare `StatlineName`, per the
  no-distinguishing-feature fallback below), and the loadout carrying the extra ability renders that
  ability's own name (`"Vexilla"`) as its label — never leaving both loadouts' labels blank the way
  a weapons-only computation would

#### Scenario: A loadout with nothing distinguishing it in either weapons or abilities falls back to its own DisplayName
- **WHEN** a statline entry's `Loadouts` contains two entries whose weapon lists and ability-name
  lists are both fully shared (nothing left after either subtraction), and the import that produced
  them gave each loadout its own distinct `DisplayName` (e.g. `"Plague Champion"` and `"Plague
  Marine w/ boltgun"`, both resolving to the shared catalogue statline `"Plague Marine"`)
- **THEN** each loadout renders its own `DisplayName` as its label — `"Plague Champion"` and
  `"Plague Marine w/ boltgun"` respectively — rather than both rendering the identical bare
  `StatlineName` `"Plague Marine"`, which would leave the reader with no way to tell the two
  loadouts apart despite the source import distinguishing them clearly

#### Scenario: A loadout with no DisplayName of its own falls back to the bare statline name
- **WHEN** a statline entry's `Loadouts` contains two entries whose weapon lists, ability-name
  lists, and `DisplayName`s are all identical to (or absent and therefore defaulted from) the
  entry's own `StatlineName`
- **THEN** both loadouts render that bare `StatlineName` as their label, rather than an empty one —
  the same outcome this fallback originally specified, now reached as `DisplayName`'s own default
  rather than a separate code path

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

#### Scenario: Feel No Pain renders under W
- **WHEN** a run's shared Statline value has a Feel No Pain of 5+
- **THEN** the run renders a tile labelled "FNP" showing "5+", below the stat row and aligned under
  the W tile

#### Scenario: Feel No Pain is omitted when absent and untouched
- **WHEN** a run has no Feel No Pain and no ability has anything to say about it
- **THEN** the run renders no FNP tile

#### Scenario: Runs differing only in Feel No Pain stay separate
- **WHEN** two consecutive same-component entries share M/T/Sv/W/Ld/Oc and invulnerable save but only
  one has a Feel No Pain
- **THEN** they render under two separate stat-tiles

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
- **THEN** per "Flagged Statline Characteristic Rendering" below, that run renders a legend entry
  naming the associated `CaveatAbility` via an interactive popover trigger, beneath the invulnerable
  save tile — its full descriptive text is available on demand through that trigger, not rendered
  inline by default

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

### Requirement: Value Provenance Highlight
A Statline tile (M, T, Sv, W, Ld, OC, InSv, FNP) or a weapon-table Attacks, BS/WS, Strength, AP or Damage value
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

#### Scenario: A datasheet or wargear Feel No Pain is highlighted
- **WHEN** a run's Feel No Pain 5+ comes from its own "Feel No Pain 5+" ability, on its datasheet or
  through wargear
- **THEN** its FNP tile renders highlighted in amber showing 5+, and its popover lists "Feel No Pain
  5+" as the source of the value

#### Scenario: A granted Feel No Pain is highlighted
- **WHEN** an unconditional Feel No Pain 5+ grant reaches a run with no Feel No Pain
- **THEN** that run's FNP tile renders highlighted in amber showing 5+

#### Scenario: A footnoted Feel No Pain is highlighted
- **WHEN** a run's Feel No Pain is caveated because its datasheet name is "Feel No Pain 3+*" and no
  qualified Feel No Pain 3+ grant reaches it
- **THEN** its FNP tile renders highlighted in amber showing 3+

#### Scenario: A caveated value stays amber
- **WHEN** a run's invulnerable save is still caveated and no other ability reaches it
- **THEN** that InSv tile renders highlighted in amber, not the conditional colour

#### Scenario: An ability-modified weapon skill is highlighted
- **WHEN** Knight Diabolus improves the Weapon Skill of the bearer's melee weapons by 1
- **THEN** each melee weapon's WS value renders as the highlighted tile showing the improved value,
  and tapping it opens a provenance popover listing Knight Diabolus

#### Scenario: An unselected choice-branch skill change is shown as conditional
- **WHEN** a unit has Doctrina Imperatives and no Imperative is selected
- **THEN** its ranged weapons' BS values and its melee weapons' WS values render in the conditional
  colour, each unchanged

#### Scenario: Selecting an Imperative applies only its own skill change
- **WHEN** the player selects the Protector Imperative for a unit with Doctrina Imperatives
- **THEN** its ranged weapons' BS values render highlighted in amber showing the improved value, and
  its melee weapons' WS values render plain

### Requirement: Conditional Effects Are Shown But Not Added
A conditional effect (usage limit, turn restriction, residual condition, or choice branch) reaching
a value SHALL appear in its popover as an ability line with its change, its condition text, and a
note that it was not added. The value shown SHALL NOT include it. A popover holding only such lines
SHALL end with the shown value rather than a total. A conditional effect the player has activated is
not shown this way (see "Activated Effects Are Marked In Provenance").

#### Scenario: A once-per-battle effect is listed but not added
- **WHEN** a player taps the highlighted Strength 8 of a Chaos Lord's Daemon hammer
- **THEN** the popover shows original 8, a Chance for Glory line with +1 noting once per battle and
  not added, and shown value 8

#### Scenario: A conditional effect beside an applied one
- **WHEN** one value is reached by an applied ability change and by a different ability's
  conditional effect
- **THEN** the popover lists both lines, and its total includes only the applied change

#### Scenario: A conditional invulnerable save gives an otherwise absent save a tile
- **WHEN** a unit with no invulnerable save carries Waaagh! (5+ invulnerable save while riled up)
- **THEN** the run renders a highlighted InSv tile showing no save, and its popover lists Waaagh!
  with 5+ and the condition "while the unit is riled up", not added

#### Scenario: A once-per-battle Feel No Pain gives an otherwise absent value a tile
- **WHEN** a Custodian Wardens unit with no Feel No Pain carries Living Fortress (once per battle,
  Feel No Pain 4+ until the end of the phase)
- **THEN** each run renders an FNP tile in the conditional colour showing "–", and its popover lists
  Living Fortress with 4+, noting once per battle and not added

## ADDED Requirements

### Requirement: Qualified Feel No Pain Popover Line
A Feel No Pain effect whose record names a qualifier, reaching a run, SHALL highlight that run's FNP
tile and appear in its popover as a line naming its ability, its threshold and its qualifier, noting
that it is not included in the shown value. It SHALL NOT change the shown value. The line SHALL be
marked not added, with its condition, when the effect is also conditional and not activated.

#### Scenario: An always-on qualified Feel No Pain
- **WHEN** a unit with Feel No Pain 6+ carries an ability classified as an unconditional Feel No
  Pain 5+ against mortal wounds
- **THEN** its FNP tile shows 6+ highlighted in amber, and the popover lists that ability with 5+
  and its qualifier "mortal wounds", noting it is not included in the value

#### Scenario: A qualified Feel No Pain on a unit without one
- **WHEN** a unit with no Feel No Pain carries an ability classified as an unconditional Feel No
  Pain 4+ for CHARACTER models only
- **THEN** each run renders an amber FNP tile showing "–", and the popover lists that ability with
  4+ and its qualifier "CHARACTER models only", noting it is not included in the value

### Requirement: Valueless Feel No Pain Entry Hidden Beside An FNP Tile
An ability named exactly "Feel No Pain", with no value, SHALL NOT be listed in a component's
abilities while any run of that component renders an FNP tile. Otherwise it SHALL be listed as
before.

#### Scenario: Custodian Wardens with Living Fortress
- **WHEN** a Custodian Wardens unit carries a valueless "Feel No Pain" ability and Living Fortress
- **THEN** its runs render the conditional FNP tile, and its ability list shows Living Fortress but
  no "Feel No Pain" entry

#### Scenario: A valueless entry with nothing to show stays listed
- **WHEN** a component carries a valueless "Feel No Pain" ability and none of its runs renders an
  FNP tile
- **THEN** its ability list still shows "Feel No Pain"

#### Scenario: A valued Feel No Pain entry stays listed
- **WHEN** a component carries "Feel No Pain 5+" and its runs render an FNP tile showing 5+
- **THEN** its ability list still shows "Feel No Pain 5+"
