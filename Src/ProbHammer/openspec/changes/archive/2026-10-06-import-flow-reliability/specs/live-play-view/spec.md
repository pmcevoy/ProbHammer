## ADDED Requirements

### Requirement: Live Play Links To The Import Page
`/LivePlay` SHALL show a link to the import page at the top of the page, above the army header.
Following it SHALL NOT change the session's list or any game state.

#### Scenario: The import link is at the top of the page
- **WHEN** `/LivePlay` renders an imported list
- **THEN** a link to the import page appears above the army header

#### Scenario: Following the link and coming back loses nothing
- **WHEN** a player with marked casualties follows the import link and then returns to
  `/LivePlay` without importing
- **THEN** the same list renders with the same casualties and phase/turn
