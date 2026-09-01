## MODIFIED Requirements

### Requirement: Per-Section Disclosure Defaults to Collapsed
Each unit block's statline (including its two ability columns), ranged-weapons, melee-weapons, and
keywords sections SHALL be independently collapsible. Each section's disclosure state on initial
page render (no prior render of that unit block to inherit state from) SHALL follow the current
phase/turn selection's Expanded set (`live-play-phase-tracker`'s "Section Relevance By Turn And
Phase" requirement) rather than always rendering collapsed. A subsequent re-render of that same unit
block (a phase/turn change, a casualty adjustment, or a status adjustment) is governed by
`live-play-phase-tracker`'s own requirements ("Selection Change Recomputes Only The Forced
Sections", "An Unrelated Unit-Block Re-Render Does Not Reset Section Disclosure"), not by this
requirement.

#### Scenario: Sections load collapsed
- **WHEN** a user navigates to `/LivePlay`
- **THEN** every unit block's statline, ranged-weapons, melee-weapons, and keywords sections render
  expanded if they are in the current phase/turn selection's Expanded set, and collapsed otherwise
  — not unconditionally collapsed

#### Scenario: Expanding one section does not affect others
- **WHEN** a user expands one unit's ranged-weapons section
- **THEN** that unit's other sections, and every section of every other unit block, remain in
  their prior collapsed/expanded state
