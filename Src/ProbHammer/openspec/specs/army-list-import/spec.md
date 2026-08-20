# army-list-import Specification

## Purpose

Provides the page and per-session storage that let a user submit a raw army-list export, have it
parsed and enriched against BSData, and have the result served to `/LivePlay` for that user's
session — reporting failures back to the user rather than crashing.

## Requirements

### Requirement: Import Submission
The system SHALL provide a page where a user can paste raw army-list export text and submit it.
On submission, the system SHALL run the text through parsing (`army-list-parsing`) and enrichment
(`army-roster-enrichment`); on success, it SHALL store the parsed army list in that user's session
and redirect the user to `/LivePlay`.

#### Scenario: A successful import redirects to /LivePlay
- **WHEN** a user submits export text that parses and enriches successfully
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
The system SHALL store a successfully-parsed army list in the submitting user's session, keyed
independently per session, so that concurrent users each see only their own imported army list. On
each subsequent request needing the roster — including `/LivePlay` page renders and casualty
adjustments — the system SHALL rebuild the `ArmyRoster` fresh from the session's stored parsed army
list rather than caching the built roster itself.

#### Scenario: Two concurrent sessions each see their own imported army list
- **WHEN** two different users each submit a different army list in their own session
- **THEN** each user's `/LivePlay` renders only the army list they themselves imported

#### Scenario: A roster is rebuilt fresh from the stored parsed list on every request
- **WHEN** a session's stored parsed army list is used to render `/LivePlay` more than once (e.g.
  across a page reload or a casualty adjustment)
- **THEN** each render rebuilds the `ArmyRoster` from that same stored parsed army list rather than
  reusing a previously-built `ArmyRoster` instance
