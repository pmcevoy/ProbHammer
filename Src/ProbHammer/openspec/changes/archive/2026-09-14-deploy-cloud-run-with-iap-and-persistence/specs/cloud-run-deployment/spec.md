## Purpose

Defines how the live-game app is hosted, secured, and kept stateful once deployed off a single
developer machine onto a public Cloud Run URL — cost-minimizing scaling, access restricted to an
invited group, and session/key state that survives Cloud Run recycling the instance.

## ADDED Requirements

### Requirement: Cost-Minimizing Autoscaling
The deployed service SHALL scale between zero and one instance (`min_instance_count = 0`,
`max_instance_count = 1`), never running a dedicated always-on instance, so idle periods incur no
compute cost.

#### Scenario: Idle service scales to zero
- **WHEN** the service receives no traffic for longer than Cloud Run's own idle-instance grace
  period
- **THEN** the running instance is reclaimed and no compute cost is incurred until the next request

#### Scenario: A request after idle triggers a cold start
- **WHEN** a request arrives after the service has scaled to zero
- **THEN** Cloud Run starts a new instance to serve it, rather than the request failing

### Requirement: Access Restricted To The Game Group Via IAP
The deployed service SHALL be reachable only by members of one designated Google Group, enforced by
Identity-Aware Proxy in front of the service. An unauthorized request SHALL be rejected before it
reaches the application container.

#### Scenario: A group member reaches the app
- **WHEN** a signed-in user who is a member of the designated Google Group requests the service URL
- **THEN** they are prompted to sign in with Google (if not already) and, once verified, reach the
  application

#### Scenario: A non-member is rejected without reaching the container
- **WHEN** a signed-in user who is not a member of the designated Google Group, or an unauthenticated
  caller, requests the service URL
- **THEN** the request is rejected by IAP and never reaches the application container, so it does
  not trigger a cold start or incur compute cost

### Requirement: Session And DataProtection State Persists Across Instance Recycling
ASP.NET Core Session data and the DataProtection key ring SHALL persist in a Cloud Storage bucket
dedicated to this service, surviving the underlying Cloud Run instance being recycled (including a
scale-to-zero and subsequent cold start). This persistence SHALL be inactive in a local development
environment with no bucket configured, requiring no external credentials to run locally.

#### Scenario: A session survives an instance recycle
- **WHEN** a user's session data was written before the serving instance was recycled, and the user
  makes a subsequent request after a new instance has started
- **THEN** their previously stored session data (e.g. an imported army roster) is still available,
  unchanged by the recycle

#### Scenario: A local development run needs no cloud credentials
- **WHEN** the application starts with no bucket name configured (the local `docker compose up`
  environment)
- **THEN** session and DataProtection state use in-memory/ephemeral storage as before, and no
  Google Cloud Storage credentials are required to start or run the application

#### Scenario: An abandoned session is eventually cleaned up, not treated as expired early
- **WHEN** a session's stored data has not been written or refreshed for 14 days
- **THEN** the underlying storage is eventually deleted for storage-cost reasons; this cleanup is
  independent of, and never triggers, an earlier logical "session expired" response while the data
  still exists
