terraform {
  required_providers {
    google = {
      source  = "hashicorp/google"
      version = "~> 8.2"
    }
  }
}

provider "google" {
  project = var.project_id
  region  = var.region
}

variable "project_id" {
  description = "GCP project ID hosting the Cloud Run service"
  type        = string
  default     = "594967610641"
}

variable "region" {
  description = "Cloud Run region"
  type        = string
  default     = "europe-west1"
}

variable "service_name" {
  description = "Cloud Run service name"
  type        = string
  default     = "liveplay"
}

variable "image" {
  description = "Container image to deploy, e.g. ghcr.io/pmcevoy/probhammer:2026-09-14"
  type        = string
}

variable "players_group_email" {
  description = "Google Group whose members are allowed through IAP"
  type        = string
  default     = "probhammer@googlegroups.com"
}

data "google_project" "current" {
  project_id = var.project_id
}

# One bucket, two prefixes: DataProtection's key ring (dataprotection/) and the
# session cache (sessions/) - both need to survive scale-to-zero, neither is worth
# a second bucket. Session objects aren't deleted on logical expiry by the app
# itself (no per-entry TTL in GCS); this lifecycle rule is pure storage-cost
# cleanup for abandoned sessions, not what makes a session "expire".
resource "google_storage_bucket" "state" {
  name                        = "${var.project_id}-${var.service_name}-state"
  location                    = var.region
  storage_class               = "STANDARD"
  uniform_bucket_level_access = true
  force_destroy               = false

  lifecycle_rule {
    condition {
      days_since_custom_time = 14
    }
    action {
      type = "Delete"
    }
  }
}

resource "google_service_account" "probhammer" {
  account_id   = "${var.service_name}-run"
  display_name = "Cloud Run runtime identity for ${var.service_name}"
}

# roles/storage.objectAdmin scoped to just this bucket - full read/write/delete on
# objects (covers both the key ring and the session cache), no bucket-config or
# IAM-management rights, nothing outside this bucket.
resource "google_storage_bucket_iam_member" "probhammer_state_access" {
  bucket = google_storage_bucket.state.name
  role   = "roles/storage.objectAdmin"
  member = "serviceAccount:${google_service_account.probhammer.email}"
}

resource "google_project_service" "run" {
  service            = "run.googleapis.com"
  disable_on_destroy = false
}

resource "google_project_service" "iap" {
  service            = "iap.googleapis.com"
  disable_on_destroy = false
}

# min=0/max=1: scales to zero when idle to minimize cost; the DataProtection key
# ring and session cache both persist to google_storage_bucket.state (wired via
# the Gcs__BucketName env var below) so scale-to-zero no longer loses either.
resource "google_cloud_run_v2_service" "probhammer" {
  name     = var.service_name
  location = var.region
  ingress  = "INGRESS_TRAFFIC_ALL"

  # Equivalent to `gcloud run deploy ... --no-allow-unauthenticated --iap`.
  # Requires provider hashicorp/google >= 8.x (absent in 6.x - see
  # hashicorp/terraform-provider-google#22280, closed by that release).
  iap_enabled = true

  template {
    service_account = google_service_account.probhammer.email

    containers {
      image = var.image

      env {
        name  = "Gcs__BucketName"
        value = google_storage_bucket.state.name
      }
    }

    scaling {
      min_instance_count = 0
      max_instance_count = 1
    }
  }

  depends_on = [
    google_project_service.run,
    google_project_service.iap,
    google_storage_bucket_iam_member.probhammer_state_access,
  ]
}

# Equivalent to:
#   gcloud run services add-iam-policy-binding SERVICE_NAME \
#     --member=serviceAccount:service-PROJECT_NUMBER@gcp-sa-iap.iam.gserviceaccount.com \
#     --role=roles/run.invoker
# `iap_enabled` grants this to the agent automatically via gcloud/Console, but it's
# declared explicitly so `terraform apply` alone leaves the service correctly wired.
resource "google_cloud_run_v2_service_iam_member" "iap_invoker" {
  name     = google_cloud_run_v2_service.probhammer.name
  location = google_cloud_run_v2_service.probhammer.location
  project  = var.project_id
  role     = "roles/run.invoker"
  member   = "serviceAccount:service-${data.google_project.current.number}@gcp-sa-iap.iam.gserviceaccount.com"
}

# Equivalent to:
#   gcloud iap web add-iam-policy-binding \
#     --resource-type=cloud-run --service=SERVICE_NAME --region=REGION \
#     --member=group:GROUP_EMAIL --role=roles/iap.httpsResourceAccessor
resource "google_iap_web_cloud_run_service_iam_member" "players" {
  project                = var.project_id
  location               = google_cloud_run_v2_service.probhammer.location
  cloud_run_service_name = google_cloud_run_v2_service.probhammer.name
  role                   = "roles/iap.httpsResourceAccessor"
  member                 = "group:${var.players_group_email}"
}
