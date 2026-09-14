## Why

The app now runs on Cloud Run at a public URL — hosting, cost-minimizing scaling, access control,
and session persistence all took shape through hands-on infra work (Terraform, live debugging of a
Cloud Run IAP OAuth-client gap, a GCS latency question) rather than a proposal, so none of it was
ever documented as a capability. This backfills that record before the app is opened up to the
game group it was built for, and before the deployment story drifts further from being written down
at all.

## What Changes

- The app deploys to a single Cloud Run service (`liveplay`), scaled `min=0`/`max=1` to minimize
  idle cost — no dedicated always-on compute.
- Public access is gated by Identity-Aware Proxy (IAP), restricted to members of one Google Group —
  nobody outside that group can reach the app, and (per IAP's own edge-level check) an unauthorized
  request never reaches the container, so it can't wake an idle instance or incur compute cost
  either.
- ASP.NET Core Session and the DataProtection key ring — both previously in-memory/ephemeral,
  meaning `min=0` would silently drop a live game's imported roster on any idle-triggered
  scale-to-zero — now persist to a dedicated Cloud Storage bucket, so state survives instance
  recycling.
- Local `docker compose up` development is unaffected: the GCS-backed persistence only activates
  when a `Gcs:BucketName` configuration value is present (set only by the Cloud Run deployment
  itself).

## Capabilities

### New Capabilities

- `cloud-run-deployment`: hosting, access control, and state-persistence behavior for the deployed
  app — not previously documented anywhere.

### Modified Capabilities

(none)

## Impact

- `terraform/cloud-run-iap.tf` (new): the Cloud Run service, IAP wiring, a dedicated runtime
  service account, and the shared state bucket plus its IAM binding.
- `src/ProbHammer.Web/Program.cs`: session/DataProtection registration now branches on
  `Gcs:BucketName`.
- `src/ProbHammer.Web/Services/GoogleCloudStorageDistributedCache.cs` (new): the `IDistributedCache`
  implementation backing ASP.NET Core Session in the deployed environment.
- `src/ProbHammer.Web/ProbHammer.Web.csproj`: adds `Google.Cloud.Storage.V1` and
  `Google.Cloud.AspNetCore.DataProtection.Storage`.
- One-time manual step outside Terraform/app code: the project's OAuth consent screen and IAP's
  custom OAuth client, which currently can't be provisioned by the `hashicorp/google` Terraform
  provider (the API it would need was shut down in March 2026).
- `openspec/specs/cloud-run-deployment/spec.md` (new).
