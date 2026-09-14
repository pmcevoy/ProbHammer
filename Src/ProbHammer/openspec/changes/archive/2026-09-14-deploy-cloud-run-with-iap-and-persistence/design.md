## Context

The app's real audience is a phone at the table (see root `CLAUDE.md`'s Project Purpose),
previously only ever run via `docker compose up` on one developer machine. Exposing it on a public
Cloud Run URL introduced three problems that had to be solved together, worked through live rather
than planned up front:

1. Who can reach it, and at what cost if a stranger tries.
2. Cloud Run's own cost/scaling trade-off (`min=0` saves money but recycles the instance on idle).
3. Everything the app previously kept in memory (session, DataProtection keys) is lost on that
   recycle.

## Goals / Non-Goals

**Goals:**
- Minimize idle cost (`min=0`) without breaking a game in progress across a realistic gap between
  turns.
- Restrict access to a known friend group, on a mechanism that also protects the cost goal (an
  unauthorized request shouldn't be able to wake the instance).
- Make persistence "just work" in the deployed environment without touching local dev.

**Non-Goals:**
- General-purpose horizontal scaling — `max=1` deliberately caps this at one instance;
  multi-instance session consistency is out of scope.
- Enforcing per-session expiration precisely — the persisted session cache treats GCS's own
  lifecycle rule as storage cleanup only, not correctness-relevant expiry (see D3).
- Automating the one manual OAuth-client step (see D2) — blocked on Google's own tooling gap, not a
  design choice this change could resolve.

## Decisions

### D1: IAP over in-app authentication
Considered a simple in-app passphrase/login instead. Rejected: an unauthenticated request still
reaches and is billed by the container before the app can reject it, directly undermining the cost
goal. Cloud Run + IAP rejects an unauthorized request at Google's edge, before it reaches the
container, satisfying both the privacy and cost goals with one mechanism. Trade-off: every player
needs a Google account — accepted since the target group already has one.

### D2: OAuth client for IAP is manual, not Terraform-managed
The `google_iap_brand`/`google_iap_client` Terraform resources depended on the IAP OAuth Admin API,
shut down March 2026, and have been removed from the provider entirely (confirmed absent from
`hashicorp/google` 8.2's schema). No Terraform-native replacement exists yet. The OAuth consent
screen and IAP's custom OAuth client are configured once, by hand, in the Cloud Console; everything
that actually gates access (the IAM bindings) stays Terraform-managed.

### D3: A custom GCS-backed IDistributedCache over Redis/Memorystore or Firestore
Considered Cloud Memorystore (Redis) — rejected as always-on billed infrastructure requiring a
Serverless VPC Access connector, directly opposed to the `min=0` cost goal. Considered Firestore via
`Google.Cloud.AspNetCore.Firestore.DistributedCache` — rejected: last published 2020, ~6,600
downloads total, reads as abandoned rather than merely quiet. Chose a small custom
`IDistributedCache` over Cloud Storage instead: pay-per-op, genuinely scales to zero, and reuses the
same bucket and `Google.Cloud.Storage.V1` client already needed for DataProtection key persistence
(`Google.Cloud.AspNetCore.DataProtection.Storage` — 1.5M downloads, actively used in production
despite its own alpha version number).

`GoogleCloudStorageDistributedCache` deliberately does not enforce `DistributedCacheEntryOptions`'
expiration — `Set`/`Refresh` only bump the object's `CustomTime`. A session surviving an arbitrarily
long gap between turns is the entire point of this change; the bucket's lifecycle rule (delete
unset for 14 days) exists only to bound storage cost, not to make a session expire.

### D4: One bucket, two prefixes, one dedicated service account
The session cache (`sessions/`) and DataProtection key ring (`dataprotection/`) share one
`google_storage_bucket`, not two — both need identical survive-scale-to-zero treatment, and a
second bucket would add IAM/config surface for no isolation benefit at this app's scale. The Cloud
Run service runs as a dedicated `google_service_account` (not the default Compute Engine identity),
granted `roles/storage.objectAdmin` scoped to just this bucket — least-privilege, independent of
whatever broader roles the project's default service account might carry.

## Risks / Trade-offs

- **[Risk]** `min=0` still means a long-enough idle gap (Cloud Run's own ~15-minute idle-instance
  grace period, not configurable) recycles the instance mid-game. → Mitigation: this change's whole
  point is that the *state* (session, keys) survives that recycle; an in-flight request during the
  actual scale-down transition is a separate, unmitigated, but rare and fast-failing edge case.
- **[Risk]** The manual OAuth-client step (D2) is a real deployment dependency Terraform can't
  enforce or verify — a fresh environment stand-up will hit the "Empty Google Account OAuth
  client ID(s)/secret(s)" error until it's done by hand. → Mitigation: documented here and in
  `terraform/cloud-run-iap.tf`'s own comments.

## Migration Plan

Already live: this backfills documentation for changes already applied to the running service and
merged into `main`'s branch history. No further migration.
