## ADDED Requirements

### Requirement: Site Root Opens The Current List
A request for the site root `/` SHALL redirect to `/LivePlay`, so a session with a current list
sees it, and a session without one reaches the import page through `live-play-view`'s "Live Play
Redirects Without An Active Import" requirement.

#### Scenario: The root shows the current list
- **WHEN** a user whose session has a successfully imported list opens `/`
- **THEN** they are redirected to `/LivePlay`, which renders that list

#### Scenario: The root reaches the import page when there is no list
- **WHEN** a user whose session has no imported list opens `/`
- **THEN** they end up on the import page

### Requirement: Expired Import Form Is Reported
When an import submission fails its request-forgery check (e.g. the page was restored from a
browser's tab cache after the cookie it pairs with was discarded), the system SHALL re-render the
import page with the submitted text still in place and a message asking the user to submit again,
rather than returning an empty error response. The submission SHALL NOT be imported, and the
session's existing list SHALL be left unchanged.

#### Scenario: A stale form shows a message and keeps the pasted text
- **WHEN** a user submits the import form after its request-forgery cookie has been discarded
- **THEN** the import page is shown with a message to submit again, the pasted text is still in
  the text box, and the session's existing list is unchanged

#### Scenario: Submitting again from the re-rendered page succeeds
- **WHEN** the user submits the re-rendered page from the previous scenario with valid export text
- **THEN** the import succeeds and redirects to `/LivePlay`

### Requirement: Import Page Links Back To The Current List
When the session has a successfully imported list, the import page SHALL show a link to
`/LivePlay`. Opening the import page SHALL NOT change the session's list or any game state.

#### Scenario: A user who opened Import by mistake can go back
- **WHEN** a user with a current list opens the import page and follows its link to `/LivePlay`
- **THEN** `/LivePlay` renders the same list with the same casualties, unit status, condition
  activations and phase/turn as before

#### Scenario: No back link without a current list
- **WHEN** a user whose session has no imported list opens the import page
- **THEN** no link to `/LivePlay` is shown

### Requirement: A Successful Import Starts A Fresh Game
A successful import SHALL reset the session's phase/turn selection to its default, and the first
`/LivePlay` render of the new import SHALL clear the browser's recorded casualties, half-strength
overrides, Battle-shocked flags and condition activations. A failed import SHALL reset none of
these. A session saved before imports were distinguishable SHALL keep its browser state.

#### Scenario: A new list starts with no casualties
- **WHEN** a player who has marked casualties on their current list successfully imports a list
- **THEN** `/LivePlay` shows every unit of the new list at its initial counts, with no unit
  half-strength or Battle-shocked and no condition activated

#### Scenario: A new list starts at the default phase/turn
- **WHEN** a player who has selected Their Turn / Charge successfully imports a list
- **THEN** `/LivePlay` shows the default phase/turn selection

#### Scenario: A failed import keeps the game state
- **WHEN** a player with marked casualties submits export text that fails to import
- **THEN** `/LivePlay` still shows the previous list with the same casualties and phase/turn

#### Scenario: Reloading after a new import keeps the new game's state
- **WHEN** a player imports a list, marks casualties on it, and reloads `/LivePlay`
- **THEN** the marked casualties are still shown
