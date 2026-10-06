# army-list-import Specification

## Purpose

Provides the page and per-session storage that let a user submit a raw army-list export, have it
parsed and enriched against BSData, and have the result served to `/LivePlay` for that user's
session — reporting failures back to the user rather than crashing.

## Requirements

### Requirement: Import Submission
The system SHALL provide a page where a user can paste raw army-list export text and submit it. On
submission, the system SHALL recognize whether the submitted text is a BattleScribe roster JSON
export (per `battlescribe-roster-import`'s Format Recognition) or a GW-app text export, and SHALL
route it through the corresponding pipeline — `battlescribe-roster-import` for the former,
`army-list-parsing` followed by `army-roster-enrichment` for the latter. On success, it SHALL store
the resulting parsed army list in that user's session and redirect the user to `/LivePlay`.

#### Scenario: A successful import redirects to /LivePlay
- **WHEN** a user submits export text that parses and enriches successfully
- **THEN** the user is redirected to `/LivePlay`, which renders the imported army

#### Scenario: A successful BattleScribe JSON import redirects to /LivePlay
- **WHEN** a user submits a BattleScribe roster JSON export that resolves successfully
- **THEN** the user is redirected to `/LivePlay`, which renders the imported army

### Requirement: Import Failure Reporting
When parsing or enrichment fails for submitted text, the system SHALL report the failure back to
the user on the import page, including the diagnostic produced by the failing stage, rather than
allowing an unhandled exception to reach the user or discarding the session's existing import (if
any).

#### Scenario: Unparseable text is reported without crashing
- **WHEN** a user submits text that fails during parsing (per `army-list-parsing`'s fail-loud
  requirements)
- **THEN** the import page shows the parser's diagnostic, and no unhandled exception reaches the
  user

#### Scenario: An unresolvable unit or weapon name is reported without crashing
- **WHEN** a user submits text that parses successfully but fails during enrichment because a name
  doesn't resolve against BSData
- **THEN** the import page shows the enrichment diagnostic, and no unhandled exception reaches the
  user

#### Scenario: A failed import leaves a previously-successful session import untouched
- **WHEN** a user with an already-successful session import submits new export text that fails to
  parse or enrich
- **THEN** the session's existing imported army list is left unchanged, and `/LivePlay` continues
  to render it

### Requirement: Per-Session Roster Storage
The system SHALL store a successfully-submitted army list in the submitting user's session, keyed
independently per session, so that concurrent users each see only their own imported army list,
regardless of which format (GW-app text or BattleScribe JSON) it was submitted in. On each
subsequent request needing the roster — including `/LivePlay` page renders and casualty
adjustments — the system SHALL rebuild the `ArmyRoster` fresh from the session's stored source-level
data rather than caching the built roster itself.

#### Scenario: Two concurrent sessions each see their own imported army list
- **WHEN** two different users each submit a different army list in their own session
- **THEN** each user's `/LivePlay` renders only the army list they themselves imported

#### Scenario: A roster is rebuilt fresh from the stored parsed list on every request
- **WHEN** a session's stored army list is used to render `/LivePlay` more than once (e.g. across a
  page reload or a casualty adjustment)
- **THEN** each render rebuilds the `ArmyRoster` from that same stored source data rather than
  reusing a previously-built `ArmyRoster` instance, whichever format it was originally submitted in

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
