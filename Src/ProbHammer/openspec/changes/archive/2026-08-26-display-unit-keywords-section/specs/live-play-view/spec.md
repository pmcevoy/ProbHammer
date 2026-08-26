## MODIFIED Requirements

### Requirement: Keyword Section Rendering
Each unit block SHALL render the view's `Keywords` in a dedicated "Keywords" section, positioned
immediately after the Melee Weapons section, following the same disclosure/section-header
convention as the Statline, Ranged Weapons, and Melee Weapons sections. Each keyword in the set
SHALL render as its own individual pill/chip, not joined with other keywords into a single string.
The section SHALL be omitted entirely when the view's `Keywords` set is empty.

#### Scenario: Keywords render when present
- **WHEN** a unit's aggregate view has a non-empty `Keywords` set
- **THEN** a "Keywords" section renders after the Melee Weapons section, containing one pill per
  keyword in the set

#### Scenario: Keywords section omitted when empty
- **WHEN** a unit's aggregate view has an empty `Keywords` set
- **THEN** no "Keywords" section renders for that unit block

### Requirement: Per-Section Disclosure Defaults to Collapsed
Each unit block's statline (including its two ability columns), ranged-weapons, melee-weapons, and
keywords sections SHALL be independently collapsible, and SHALL render collapsed by default when
the page first loads.

#### Scenario: Sections load collapsed
- **WHEN** a user navigates to `/LivePlay`
- **THEN** every unit block's statline, ranged-weapons, melee-weapons, and keywords sections render
  in a collapsed state, showing no section's detail content until expanded

#### Scenario: Expanding one section does not affect others
- **WHEN** a user expands one unit's ranged-weapons section
- **THEN** that unit's other sections, and every section of every other unit block, remain in
  their prior collapsed/expanded state
