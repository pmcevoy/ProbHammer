## MODIFIED Requirements

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
