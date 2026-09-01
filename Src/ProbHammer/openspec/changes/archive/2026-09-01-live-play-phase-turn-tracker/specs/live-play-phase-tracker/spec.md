## Purpose

Gives `/LivePlay` a genuine server-side notion of the current turn and phase of the battle round,
so this and future features can rely on it, and uses it to reduce on-screen clutter by driving
which of each unit block's own sections open by default.

## ADDED Requirements

### Requirement: Phase/Turn Selector Control
The `/LivePlay` page SHALL render exactly one phase/turn selector control for the whole page — never
one per unit block, unlike the per-unit status toolbar (Half Strength/Battle-shock/Reset
Casualties) — positioned within the Army Header, after its metadata line (Name/Faction/BattleSize/
PointsSpent/PointsLimit/ForceDisposition) and before its Rules section. The control is structured as
a grid with two rows (My Turn, Their Turn) and five phase columns (Command, Movement, Shooting,
Charge, Fight). Each row's own label SHALL also be independently selectable, as
a twelfth and thirteenth cell alongside the ten Turn/Phase cells. Exactly one of these twelve cells
SHALL be the current selection at all times. A row-label cell's selection carries that row's Turn
with no specific Phase. A browser/session that has never recorded a selection SHALL default to the
My Turn / Command Phase cell. Selecting a different cell SHALL replace the current selection with
the newly selected cell; the previously selected cell SHALL no longer be shown as selected.

#### Scenario: The control renders as a 2x5 grid with two selectable row labels
- **WHEN** a player navigates to `/LivePlay`
- **THEN** the page shows one selector control with two rows (My Turn, Their Turn), each with five
  phase cells (Command, Movement, Shooting, Charge, Fight), and each row's own label is itself a
  selectable cell

#### Scenario: Exactly one control renders for the whole page, not one per unit
- **WHEN** a player navigates to `/LivePlay` with a roster containing more than one unit block
- **THEN** exactly one phase/turn selector control renders on the page, within the Army Header,
  regardless of how many unit blocks the roster has — unlike the per-unit status toolbar, it is not
  repeated per unit block

#### Scenario: A session with no prior selection defaults to My Turn / Command Phase
- **WHEN** `/LivePlay` is opened in a session that has never recorded a phase/turn selection
- **THEN** the My Turn / Command cell renders as the current selection

#### Scenario: Selecting a cell replaces the current selection
- **WHEN** a player selects a cell other than the currently selected one
- **THEN** the newly selected cell becomes the current selection and the previously selected cell no
  longer renders as selected

#### Scenario: Selecting a row label selects that turn with no specific phase
- **WHEN** a player selects the "My Turn" (or "Their Turn") row label itself, rather than one of its
  five phase cells
- **THEN** that row label renders as the current selection, distinct from any of its own row's five
  phase cells

#### Scenario: Exactly one cell is ever selected
- **WHEN** the page is in any state
- **THEN** exactly one of the twelve selectable cells (ten Turn/Phase cells, two row labels) renders
  as the current selection, never zero and never more than one

### Requirement: Phase/Turn Selection Is Genuine Server-Side State
The current phase/turn selection SHALL be recorded as server-side state (not merely client-side
browser storage), associated with the same session as the player's imported army list, so that any
`/LivePlay` server-side rendering — including future capabilities — can read the current selection.
The selection SHALL persist across an ordinary page reload for as long as that session remains
valid. A session whose imported army list has been lost (e.g. session expiry, an application
restart, or a browser restart that does not carry the session forward — the same conditions under
which `live-play-view`'s "Live Play Redirects Without An Active Import" requirement already sends
the player back to the import page) SHALL also lose its recorded phase/turn selection; a fresh
import begins again at the default selection.

#### Scenario: Reloading the page preserves the current selection
- **WHEN** a player selects Their Turn / Charge and then reloads the page within the same session
- **THEN** the reloaded page still shows Their Turn / Charge as the current selection

#### Scenario: A lost session resets the selection to default
- **WHEN** a player's session has expired or the server has restarted, and the player re-imports
  their army list
- **THEN** the newly started session's phase/turn selection is the default (My Turn / Command),
  independent of whatever was selected before the session was lost

### Requirement: Section Relevance By Turn And Phase
Each unit block's Statline, Ranged Weapons, Melee Weapons, and Keywords sections (`_UnitBlock`'s
own sections — never the Army Header's own Rules section or All Keywords section, which this
capability never touches, regardless of the current selection) SHALL have their expanded/collapsed
state governed by the current phase/turn selection as follows. For a given selection, an
**Expanded** set (the sections that render open) and a **Forced** set (the sections whose state
this selection actually dictates — a superset of Expanded, since a Forced-but-not-Expanded section
is dictated closed) are:

| Selection | Expanded | Forced |
|---|---|---|
| Either row label (no Phase) | none | Statline, Ranged, Melee, Keywords (all four) |
| My Turn / Command | Statline | Statline |
| My Turn / Movement | Statline | Statline |
| My Turn / Shooting | Statline, Ranged | Statline, Ranged, Melee |
| My Turn / Charge | Statline | Statline |
| My Turn / Fight | Statline, Melee | Statline, Melee, Ranged |
| Their Turn / Command | Statline | Statline, Ranged, Melee, Keywords (all four) |
| Their Turn / Movement | Statline | Statline, Ranged, Melee, Keywords (all four) |
| Their Turn / Shooting | Statline | Statline, Ranged, Melee, Keywords (all four) |
| Their Turn / Charge | Statline | Statline, Ranged, Melee, Keywords (all four) |
| Their Turn / Fight | Statline, Melee | Statline, Ranged, Melee, Keywords (all four) |

A section in the current selection's Forced set SHALL render expanded if it is also in the Expanded
set, and collapsed otherwise. A section NOT in the current selection's Forced set is left exactly as
its own most recent state (see "Selection Change Recomputes Only The Forced Sections" — this
requirement defines the table only; that requirement defines how the table is applied to an
already-rendered page).

#### Scenario: A row label selection forces every section closed
- **WHEN** the current selection is a row label (either "My Turn" or "Their Turn", no Phase)
- **THEN** every unit block's Statline, Ranged Weapons, Melee Weapons, and Keywords sections are all
  in the Forced set and none are in the Expanded set, so all four render collapsed

#### Scenario: My Turn Command/Movement/Charge force only Statline
- **WHEN** the current selection is My Turn / Command, My Turn / Movement, or My Turn / Charge
- **THEN** only the Statline section is in the Forced set (expanded); Ranged Weapons, Melee Weapons,
  and Keywords are not in the Forced set at all

#### Scenario: My Turn Shooting expands Ranged and forces Melee closed
- **WHEN** the current selection is My Turn / Shooting
- **THEN** Statline and Ranged Weapons are in the Forced set and expanded, Melee Weapons is in the
  Forced set and collapsed, and Keywords is not in the Forced set at all

#### Scenario: My Turn Fight expands Melee and forces Ranged closed
- **WHEN** the current selection is My Turn / Fight
- **THEN** Statline and Melee Weapons are in the Forced set and expanded, Ranged Weapons is in the
  Forced set and collapsed, and Keywords is not in the Forced set at all

#### Scenario: Any Their Turn phase forces all four sections
- **WHEN** the current selection is any Their Turn phase (Command, Movement, Shooting, or Charge)
- **THEN** all four sections are in the Forced set; only Statline is also in the Expanded set, so
  Ranged Weapons, Melee Weapons, and Keywords render collapsed

#### Scenario: Their Turn Fight forces all four sections and expands Melee too
- **WHEN** the current selection is Their Turn / Fight
- **THEN** all four sections are in the Forced set; Statline and Melee Weapons are also in the
  Expanded set, so Ranged Weapons and Keywords render collapsed

#### Scenario: This capability never touches the Army Header's own sections
- **WHEN** any phase/turn selection is made
- **THEN** the Army Header's own Rules section and All Keywords section (per `live-play-view`'s
  "Army Header Rendering" and "Army-Wide Keyword Filter Section Rendering" requirements) are
  unaffected — their own expanded/collapsed state is governed solely by their own existing
  requirements

### Requirement: Selection Change Recomputes Only The Forced Sections
Selecting a different phase/turn cell SHALL update, for every currently-rendered unit block, only
the sections in the newly selected cell's Forced set (per "Section Relevance By Turn And Phase") to
that set's dictated expanded/collapsed state — overriding any manual toggle a player had made to one
of those specific sections. Every section NOT in the newly selected cell's Forced set SHALL be left
exactly as it was immediately before the selection changed, whether that state came from an earlier
phase/turn selection, a player's own manual toggle, or the section's original default.

#### Scenario: A row label selection collapses even a manually-expanded section
- **WHEN** a player manually expands a unit block's Keywords section, and then selects a row label
  (My Turn or Their Turn)
- **THEN** that unit block's Keywords section collapses, since a row label's Forced set includes
  Keywords

#### Scenario: Switching to a Their Turn phase overrides a manual toggle on a forced section
- **WHEN** a player manually expands a unit block's Ranged Weapons section, and then selects any
  Their Turn phase
- **THEN** that unit block's Ranged Weapons section collapses, since every Their Turn phase's Forced
  set includes Ranged Weapons

#### Scenario: My Turn Command leaves a manually-expanded Keywords section alone
- **WHEN** a player manually expands a unit block's Keywords section, and then selects My Turn /
  Command
- **THEN** that unit block's Keywords section remains expanded, since My Turn / Command's Forced set
  does not include Keywords at all

#### Scenario: My Turn Shooting leaves a manually-expanded Keywords section alone
- **WHEN** a player manually expands a unit block's Keywords section, and then selects My Turn /
  Shooting
- **THEN** that unit block's Keywords section remains expanded, since My Turn / Shooting's Forced
  set is Statline, Ranged Weapons, and Melee Weapons only

#### Scenario: Switching between two My Turn phases can leave a section exactly as it was
- **WHEN** a unit block's Ranged Weapons section is currently expanded (from an earlier My Turn /
  Shooting selection), and the player selects My Turn / Charge
- **THEN** that unit block's Ranged Weapons section remains expanded, since My Turn / Charge's
  Forced set is Statline only and does not include Ranged Weapons

### Requirement: An Unrelated Unit-Block Re-Render Does Not Reset Section Disclosure
A unit block's Statline, Ranged Weapons, Melee Weapons, and Keywords sections' current
expanded/collapsed state SHALL be unaffected by a re-render triggered by something other than a
phase/turn selection change (e.g. a casualty or unit-status adjustment on that same unit),
regardless of whether that state came from the current selection's Forced set or from a section left
untouched by it.

#### Scenario: A casualty adjustment does not reset any section's disclosure state
- **WHEN** a unit block's sections are in some combination of expanded/collapsed states (from the
  current phase/turn selection, an earlier selection's leftover state, or a manual toggle), and a
  player marks a casualty on that same unit
- **THEN** every one of that unit block's sections renders in the exact same expanded/collapsed
  state after the casualty-triggered re-render as immediately before it

### Requirement: No Rules Interpretation
The phase/turn selection SHALL only reflect what the player asserts and SHALL only affect which
existing sections default open or closed. It SHALL NOT gate, trigger, compute, or auto-apply any
change to a unit's statline, weapon, ability, casualty, half-strength, or Battle-shocked state.

#### Scenario: Selecting a phase/turn cell changes no game data
- **WHEN** a player selects any phase/turn cell
- **THEN** no unit's Statline values, weapon totals, abilities, casualty counts, half-strength
  status, or Battle-shocked status change as a result — only section disclosure state changes
