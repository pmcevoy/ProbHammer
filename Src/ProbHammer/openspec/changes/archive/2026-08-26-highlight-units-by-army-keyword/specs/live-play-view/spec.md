## ADDED Requirements

### Requirement: Army-Wide Keyword Filter Section Rendering
The Army Header SHALL render an "All Keywords" section directly beneath the existing Rules
section, following the same collapsible `lp-section` disclosure convention used elsewhere on the
page, collapsed by default. This section SHALL contain one pill per distinct keyword currently
present in any unit block's own Keywords section on the page, deduplicated and sorted
alphabetically (case-insensitively). The set SHALL reflect only currently-present keywords: a
keyword contributed solely by a model-line or component that is not currently present (e.g. every
carrier is a casualty) SHALL NOT appear. The section SHALL be omitted entirely when no unit
currently has any keyword.

#### Scenario: All Keywords section lists the deduplicated, sorted union
- **WHEN** the roster's units, combined, currently carry a non-empty set of distinct keywords
  across their own Keywords sections
- **THEN** the Army Header shows an "All Keywords" section beneath the Rules section, containing
  one pill per distinct keyword, sorted alphabetically case-insensitively, with no duplicates

#### Scenario: All Keywords section collapses by default
- **WHEN** a player navigates to `/LivePlay`
- **THEN** the All Keywords section renders in a collapsed state, showing no pills until expanded

#### Scenario: A keyword whose sole carrier is dead is excluded from the union
- **WHEN** every model-line or component that carries a given keyword currently has no present
  models (e.g. marked as a full casualty)
- **THEN** that keyword does not appear as a pill in the All Keywords section

#### Scenario: All Keywords section omitted when no unit has any keyword
- **WHEN** no unit on the page currently has a non-empty Keywords section
- **THEN** the Army Header renders no "All Keywords" section at all

### Requirement: Keyword Filter Toggle Selects Multiple Keywords
Each pill in the All Keywords section SHALL act as an independent toggle control for that specific
keyword's active/inactive filter state. A player SHALL be able to activate more than one keyword's
filter at the same time. A unit qualifies as matching the active filter set when it has AT LEAST
ONE currently-active keyword (a union/OR match across active keywords), not only when it has every
active keyword. This filter state SHALL NOT persist across a page reload or navigation, matching
the existing per-unit selection-filter's own non-persistence.

#### Scenario: Selecting a keyword pill activates its filter
- **WHEN** a player selects a keyword pill in the All Keywords section
- **THEN** that keyword's filter becomes active, and the pill renders in its active-control style

#### Scenario: Deselecting a keyword pill deactivates its filter
- **WHEN** a player selects an already-active keyword pill
- **THEN** that keyword's filter becomes inactive, and the pill returns to its non-active style

#### Scenario: Multiple keywords can be active simultaneously
- **WHEN** a player selects two or more distinct keyword pills without deselecting either
- **THEN** both keywords' filters remain active at the same time

#### Scenario: A unit qualifies by matching any one active keyword
- **WHEN** two or more keyword filters are active, and a given unit's Keywords section contains at
  least one but not all of the active keywords
- **THEN** that unit still qualifies as a match (per the expansion/highlight behavior below)

#### Scenario: Active filters reset on page reload
- **WHEN** a player has one or more keyword filters active and then reloads or re-navigates to
  `/LivePlay`
- **THEN** no keyword filter is active on the freshly loaded page

### Requirement: Keyword Filter Expands And Highlights Matching Units
While a keyword's filter is active, every unit block whose own Keywords section contains that
keyword SHALL have its Keywords section expanded (if not already), without collapsing any section
the player had independently expanded. Within each such matching unit's Keywords section, the
specific pill(s) matching an active keyword SHALL render with a distinct flagged/highlighted style,
separate from the All Keywords section's own active-control pill style, and not itself an
interactive control. Deactivating a keyword's filter SHALL remove that keyword's flagged style from
every unit's pill, but SHALL NOT collapse any Keywords section it had expanded.

#### Scenario: Activating a filter expands every matching unit's Keywords section
- **WHEN** a player activates a keyword's filter, and one or more units have that keyword in their
  own Keywords section
- **THEN** each such unit's Keywords section expands (if collapsed), showing its keyword pills

#### Scenario: A non-matching unit's Keywords section is left alone
- **WHEN** a player activates a keyword's filter
- **THEN** any unit whose Keywords section does not contain that keyword is not forced open or
  closed

#### Scenario: The matching keyword's own pill is flagged within a matching unit
- **WHEN** a unit's Keywords section is expanded due to an active keyword filter
- **THEN** that specific keyword's pill within the unit's own section renders with the flagged
  style, while the unit's other keyword pills render in their ordinary, unflagged style

#### Scenario: Deactivating a filter removes the flag but does not collapse the section
- **WHEN** a player deactivates a keyword filter that had expanded one or more units' Keywords
  sections
- **THEN** the flagged style is removed from every matching pill, but each previously-expanded
  Keywords section remains expanded

#### Scenario: A manually-expanded section is unaffected by an unrelated filter change
- **WHEN** a player has manually expanded a unit's Keywords section, and then activates or
  deactivates a keyword filter that unit does not carry
- **THEN** that unit's Keywords section's expanded/collapsed state is unchanged by the filter change

### Requirement: Keyword Filter Reflects Casualty State
The All Keywords section and any active keyword filter's matching/highlighting SHALL update to
reflect a casualty adjustment once the affected unit's block re-renders. If a keyword's only
carrier(s) become fully dead, that keyword SHALL be removed from the All Keywords section and any
filter that was active for it SHALL become inactive. If casualties are later reverted such that a
keyword's carrier becomes present again, that keyword SHALL reappear in the All Keywords section,
initially inactive (not automatically reactivated).

#### Scenario: A keyword drops out when its last carrier dies
- **WHEN** a player marks casualties such that every carrier of a given keyword is no longer
  present
- **THEN** that keyword's pill is removed from the All Keywords section

#### Scenario: An active filter is cleared when its keyword drops out
- **WHEN** a keyword's filter is currently active, and a casualty adjustment removes every carrier
  of that keyword
- **THEN** that keyword's filter becomes inactive along with its pill's removal

#### Scenario: A keyword reappears, inactive, after casualties are reverted
- **WHEN** a keyword had dropped out of the All Keywords section due to casualties, and a
  subsequent casualty adjustment (e.g. Reset Casualties, or restoring a model) brings a carrier of
  that keyword back to present
- **THEN** that keyword's pill reappears in the All Keywords section in its inactive style, without
  automatically reactivating its filter or re-expanding/re-flagging any unit on its own

#### Scenario: An unaffected active filter keeps matching after another unit's casualty update
- **WHEN** a keyword filter is active, and a casualty adjustment is made to a unit that does not
  carry that keyword
- **THEN** every unit matching that active keyword remains expanded and flagged as before
