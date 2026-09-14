# First runtime milestone: context reconstruction

Date: 2026-09-14. Baseline: `fd2a9e6` on `main`, with milestone 4 at `98b000c`. This is pre-approval investigation, not runtime implementation or acceptance evidence.

## Repository and application

The controller and read-only code/history reviewers inspected repository rules, README, current product/architecture/validation documents, all ten ADRs, prior specifications/plans, retained task prompts, review evidence, actual EF model/migrations, ownership, validation and observability code. No runtime workflow exists; `n8n/workflows/` contains only `.gitkeep`.

Git history records bootstrap at `4fca2f1`, architecture at `b68e3ad`, skeleton at `5880801`, and configuration management at `98b000c`. The topology amendment belongs to the skeleton history. The working tree was clean before archiving the new task. The new requested commit wording differs from the roadmap wording; it does not change milestone order.

Read-only HTTP GETs to the existing local application returned 200 for `/health/live`, `/health/ready`, `/Alerts` and `/Settings/Notifications` on port 5180. These establish current responses, not a fresh browser/CRUD validation.

Rider recognized `Sonrisa.Web` and `Sonrisa.Web.Tests`, returned the expected application packages and reported zero project problems. The code reviewer ran `dotnet test Sonrisa.sln --no-restore --verbosity minimal`: 52 passed, 14 skipped, zero failures (66 reported cases). PostgreSQL integration tests were skipped because their guarded test configuration was absent. Historical PostgreSQL-enabled results are separate evidence and were not reproduced in this phase.

## Product PostgreSQL

Read-only PostgreSQL MCP queries established database `sonrisa_dev`, server 18.6 and inspection role `ai_user`. Public product tables are `users` and `alerts`, plus EF migration history. Information-schema columns, PostgreSQL constraints and indexes match the current configuration contract: textual earthquake/magnitude/gte/number codes, finite double-precision values, restricted owner foreign key, Unicode trim constraints, owner index and partial enabled-event-type index.

Applied migrations are `20260914125733_InitialAlertConfiguration`, `20260914140740_SharedUserNotificationDestinations`, and `20260914143004_EnforceUnicodeTrimmedConfiguration`, all recorded with EF 10.0.12. Aggregate-only inspection found one user, one enabled alert, one email profile and no Slack profile. No destination values were retrieved.

The inspection role is not a superuser and has neither role-creation nor database-creation privilege, but can create objects in public. This is not evidence of a least-privilege n8n runtime credential. No grants, configuration rows, schema or migrations were changed.

## n8n capability inspection

Sonrisa workflow/project searches returned no accessible matches. Credential metadata searches for Slack and PostgreSQL returned no accessible credentials. Team projects are reported disabled. These are visibility results, not proof that no inaccessible workflow or credential exists.

Node discovery exposed Postgres 2.7, Slack 2.7, Schedule Trigger 1.4, Manual Trigger 1 and HTTP Request 4.5. Live type definitions confirm Postgres query parameters and transaction batching and Slack message posting with channel-ID input. No workflow was created, changed, executed or published. No notification was sent.

The available MCP tools do not expose global OpenTelemetry settings. A read-only request to the named n8n host's public settings endpoint returned HTTP 403. The installed n8n release and effective workflow OTEL export remain unverified; node schema versions are not an instance release number. No global telemetry or access-control changes were attempted.

## Source investigation

The public USGS v1.0 all-hour GeoJSON feed returned HTTP 200 without credentials: 6,692 bytes, nine features and nine distinct preferred IDs. A later read returned eight features; all eight IDs were also present in the first response. All eight second-response records had earthquake type and finite numeric magnitude. The observed occurrence timestamp `1789398234382` converted to `2026-09-14T15:03:54.382+00:00`. These reads validate a small observed sample, not long-term identity stability or a deployed n8n adapter.

USGS [feed documentation](https://earthquake.usgs.gov/earthquakes/feed/v1.0/geojson.php) offers a bounded hourly feed updated every minute. Its [catalog API guidance](https://earthquake.usgs.gov/fdsnws/event/1/) recommends real-time feeds for automated display applications where possible. Its [current lifecycle policy](https://earthquake.usgs.gov/earthquakes/feed/policy.php) identifies v1.0 as production.

Material caveat: USGS [ComCat documentation](https://earthquake.usgs.gov/data/comcat/#id) explicitly says the preferred event ID can change and provides associated IDs in `properties.ids`. Repeated IDs in two samples cannot establish immutable identity. The proposed design will account for known aliases and disclose unresolved association/merge limitations; this is not yet an accepted schema decision.

Context7's official n8n documentation and live node definitions confirm parameterized Postgres queries and transactional batching as capabilities. They do not prove that a proposed SQL operation is atomic in the eventual deployed workflow; that requires actual PostgreSQL/n8n tests after approval.

## Approval boundary

Milestone 5 already requires durable unique delivery intent and atomic event evaluation. Full retry/circuit/attempt recovery and email remain milestone 6. No accepted requirement is silently removed. The proposed initial send safeguards, provider identity handling and inactive/manual operation await the user's design approval. Slack credential access, restricted n8n product-database credential access, and an explicitly authorized persisted Slack test destination are still required before live delivery.

An independent pre-approval design review raised two Important concerns: alias handling needs an explicit provider-contract refinement with bounded input and association limitations; first-poll intents must not be silently drained when later automation is enabled. The revised proposal makes alias handling an approval item and persists manual-only dispatch intent, with explicit release review required before future automated sending. These are design corrections, not implemented safeguards or runtime test results.

Focused re-review returned APPROVE_WITH_MINOR_NOTES with no remaining Important design objection. The minor clarification is incorporated: a transient attempted delivery may remain Pending but is ineligible for another milestone-5 attempt; automatic retry/recovery remains unimplemented. User design approval is still required. The observed nine-record source sample included an associated-ID list containing its preferred ID for every record, but each list had only one ID; preferred-ID-change handling therefore needs deterministic validation and remains subject to provider association limits.
