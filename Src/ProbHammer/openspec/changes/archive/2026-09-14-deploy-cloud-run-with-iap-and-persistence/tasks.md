## 1. Cloud Run hosting and cost-minimizing scaling

- [x] 1.1 `google_cloud_run_v2_service` deployed with `scaling { min_instance_count = 0,
  max_instance_count = 1 }`.
- [x] 1.2 Dedicated `google_service_account` for the service's runtime identity, replacing the
  default Compute Engine service account.

## 2. Access control via IAP

- [x] 2.1 `iap_enabled = true` on the Cloud Run service (requires provider `hashicorp/google >=
  8.x` — absent in 6.x, see design.md D2's provider-schema note).
- [x] 2.2 `google_cloud_run_v2_service_iam_member` grants the IAP service agent `roles/run.invoker`.
- [x] 2.3 `google_iap_web_cloud_run_service_iam_member` grants the game group
  `roles/iap.httpsResourceAccessor`.
- [x] 2.4 One-time manual step (outside Terraform, see design.md D2): OAuth consent screen + IAP
  custom OAuth client, configured via Cloud Console.

## 3. Persistent session and DataProtection state

- [x] 3.1 `google_storage_bucket.state`: regional bucket, uniform bucket-level access, 14-day
  `days_since_custom_time` lifecycle deletion rule.
- [x] 3.2 `google_storage_bucket_iam_member` grants the service's runtime SA
  `roles/storage.objectAdmin`, scoped to the bucket.
- [x] 3.3 `Gcs__BucketName` env var wires the bucket name into the running container.
- [x] 3.4 `GoogleCloudStorageDistributedCache` (`IDistributedCache` over GCS objects under
  `sessions/{id}`) replaces `AddDistributedMemoryCache`, gated on `Gcs:BucketName` being
  configured.
- [x] 3.5 `PersistKeysToGoogleCloudStorage` persists the DataProtection key ring under
  `dataprotection/keys.xml` in the same bucket, same gate.
- [x] 3.6 Verified local `docker compose up` is unaffected (no `Gcs:BucketName` set locally →
  unchanged in-memory/ephemeral behavior, no GCS credentials required).

## 4. Verification

- [x] 4.1 `terraform validate`/`terraform fmt` run against the real `hashicorp/google` provider
  schema (not just documentation) before relying on any attribute, catching one wrong assumption
  (`iap_enabled` absent from the provider version first tried) before it shipped.
- [x] 4.2 Full test suite (`dotnet test`) run after the app-side changes; one real regression found
  and fixed (`RedirectToPage` ambiguity from an unrelated earlier change, `/Import` gaining a second
  route) — not introduced by this change's own code, but caught by running the suite for it.
- [x] 4.3 Live end-to-end verification: applied to the real Cloud Run project, confirmed IAP-gated
  access works for a real player after the manual OAuth-client step.
