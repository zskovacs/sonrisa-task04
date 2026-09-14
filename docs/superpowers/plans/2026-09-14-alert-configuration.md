# Alert Configuration Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development to implement this plan task-by-task. Steps use checkbox (`- [x]`) syntax for tracking.

**Current amendment:** ADR-010 and the explicit user correction supersede the initial destination/validation design. Tasks 1–4 below record work against that earlier design; Task 5 replaces it and Task 6 establishes new acceptance before the original milestone commit. Do not execute the earlier final-commit step ahead of these amendments.

**Goal:** Deliver ownership-aware single-condition configuration management, reproducible Tailwind styling, and exporter-independent OpenTelemetry logs/request traces.

**Architecture:** One Razor Pages application calls a concrete owner-scoped management service and EF Core. PostgreSQL holds two product configuration tables; n8n runtime remains unimplemented. CSS uses an explicit build-only CLI; telemetry uses standard SDK providers without a backend dependency.

**Tech Stack:** Existing net10.0/EF 10.0.12/Npgsql EF 10.0.3; Tailwind/CLI 4.3.3; OpenTelemetry Hosting/AspNetCore/OTLP 1.18.0; one standard .NET test project.

**Spec:** [Approved specification](../specs/2026-09-14-alert-configuration-design.md).

## Global constraints

- Preserve ADR-002/005/006/007/008 and the specification; no multiple conditions, auth infrastructure, runtime tables/workflows/sends, or extra frontend application.
- Keep root solution and Rider checkout; feature branch `feat/alert-configuration`. No agent commits/stages; controller owns the one requested milestone commit.
- No tracked connection-string values, real destinations, credentials, generated CSS, bin/obj, node_modules or local IDE state.
- No agent accesses shared DEV or migrates it except the controller's reviewed Task 4. Tests opt in only with `SONRISA_TEST_DATABASE` and `SONRISA_TEST_DATABASE_NAME` checked via current_database().
- Follow meaningful test-first behavior, focused verification and task review. Generated migration/configuration/style checks do not need artificial implementation-mirroring tests.

## Task 1: Owned persistence and configuration management

**Owner:** sp_integration_debugger. Files: `Data/AppDbContext.cs`, new `Alerts/Alert.cs`, `AlertChannel.cs`, `AlertInput.cs`, `AlertInputValidator.cs`, `AlertManagementService.cs`, `AlertResult.cs`, `Ownership/ICurrentOwner.cs`, `ConfiguredCurrentOwner.cs`, `Configuration/AlertDestinationOptions.cs`, new `Data/Migrations/*`, registration changes in `Program.cs`/`appsettings.json`; root solution; one `tests/Sonrisa.Web.Tests` project and focused validator/model/service tests. Exact class grouping may consolidate tiny declarations without extra abstractions.

**Interfaces:** `ICurrentOwner.OwnerId` is Guid. `AlertInput` exposes Name, Threshold, Enabled, EmailDestination, SlackDestination, Revision. `AlertManagementService` consumes scoped AppDbContext/current owner/validated allowlist/ILogger and provides async ListAsync, GetAsync(Guid), CreateAsync(AlertInput), UpdateAsync(Guid, AlertInput), SetEnabledAsync(Guid, Guid revision, bool enabled), all accepting CancellationToken. Read/write results distinguish Success/NotFound/Conflict/Invalid/Unavailable and field errors. List/edit return only the current owner's data. Record final exact signatures in the task report for Task 2.

- [x] Add the test project using stable xUnit, Microsoft.NET.Test.Sdk, runner and Microsoft.AspNetCore.Mvc.Testing 10.0.12; pin versions verified from public metadata. Write/run focused failing validator tests before behavior implementation. Example behavior:

```csharp
[Theory]
[InlineData("NaN")]
[InlineData("Infinity")]
[InlineData("1e500")]
[InlineData("1,000")]
public void Rejects_nonfinite_or_ambiguous_threshold(string threshold)
{
    var input = ValidInput();
    input.Threshold = threshold;
    Assert.Contains(validator.Validate(input), e => e.Key == "Threshold");
}
```

Include valid negative/zero/5.5 thresholds, blank/oversized name, mailbox syntax/CRLF, unknown destination and channel-less inputs, fixed default owner and invalid configured UUID. No connection values in fixtures.

- [x] Implement only specified input validation/configured owner and native numeric model. Add model mapping tests asserting exact relational table/column/type/PK/FK/check/index/concurrency metadata, not an EF InMemory surrogate.
- [x] Implement owner-scoped service with tracked loads and input copying, no form-bound entity attach. The essential write boundary is:

```csharp
var alert = await db.Alerts.Include(a => a.Channels)
    .SingleOrDefaultAsync(a => a.Id == id && a.OwnerId == owner.OwnerId, cancellationToken);
if (alert is null) return NotFound();
db.Entry(alert).Property(a => a.Revision).OriginalValue = input.Revision;
alert.Revision = Guid.NewGuid();
// Assign validated name/threshold/enabled and reconcile known channel types.
await db.SaveChangesAsync(cancellationToken);
```

Use finite comparison checks in PostgreSQL (reject NaN and both infinities), literal textual discriminators, revision updates for status writes, safe error results/logs. Catch concurrency separately. Do not suppress cancellation into success. Disable without current allowlist validation; enable validates retained channels. All reads/writes scope to owner even without authentication.

- [x] Write opt-in PostgreSQL tests (skipped with explicit reason when external config absent) for two-owner list/get/update/status isolation, stale revisions, create/edit/channel reconciliation, invalid persistence constraints, and transaction rollback. Only test-owned IDs; no migration/reset in automatic test setup. These tests are exercised by controller after Task 4 migration.
- [x] Generate `InitialAlertConfiguration` migration using external-configuration discovery with no connection fallback. `dotnet ef migrations script --project src/Sonrisa.Web` must emit only the two tables, constraints/indexes, and migration history. Inspect generated source; do not apply it. Do not check in connection-bearing output.
- [x] Run `dotnet test Sonrisa.sln` and `dotnet build Sonrisa.sln`, inspect task diff and return actual red/green results, package pins, signatures and outstanding opt-in tests. Obtain task review before Task 2.

## Task 2: Razor management and Tailwind build/publish

**Owner:** sp_normal_implementer, escalate integration failures if needed. Files: `Pages/Alerts/Index/Create/Edit.cshtml[.cs]`, shared layout/form partial/imports, root Index redirect, `wwwroot` source CSS, root package.json/lock, `.gitignore`, `Dockerfile`, `.dockerignore`, static-asset registration in Program, focused HTTP/form tests in the existing test project. No telemetry changes.

**Interfaces:** consumes Task 1 actual service/input outcomes. Produces `/alerts`, `/alerts/create`, `/alerts/{id:guid}/edit`, root redirect, static `/css/site.css`, scripts `css:build`/`css:watch`. Forms use `Input.Name`, `Input.Threshold`, `Input.Enabled`, `Input.EmailDestination`, `Input.SlackDestination`, `Input.Revision`; no OwnerId or child IDs. List status POSTs carry alert ID/revision/desired enabled state and antiforgery.

- [x] Write/run failing route/form tests demonstrating root redirect and create form fixed condition/labels/antiforgery, absence of OwnerId, default disabled, validation preserving input, and missing allowlist guidance. Use test-host in-memory configuration only for non-secret owner and synthetic allowed destination values. PostgreSQL-dependent form tests remain explicitly opt-in.
- [x] Implement minimal layout, shared editor and page models. Service field errors map to ModelState under `Input.*`. NotFound is 404; unavailable is sanitized 503 without raw exceptions; conflict preserves input and asks reload. List empty state is useful. Output encoding is Razor default. No scripts needed.
- [x] Add Tailwind pinned 4.3.3 CLI scripts, e.g.:

```json
{"private":true,"scripts":{"css:build":"tailwindcss -i src/Sonrisa.Web/Styles/site.css -o src/Sonrisa.Web/wwwroot/css/site.css --minify","css:watch":"tailwindcss -i src/Sonrisa.Web/Styles/site.css -o src/Sonrisa.Web/wwwroot/css/site.css --watch"},"devDependencies":{"tailwindcss":"4.3.3","@tailwindcss/cli":"4.3.3"}}
```

Input imports Tailwind with `source(none)` and explicit `../Pages` source. Keep all class tokens literal. Ignore generated site.css/node_modules/caches; commit source and lockfile.
- [x] Add a pinned Node 24 build-only Docker stage running npm ci/css:build; copy required Razor/CSS inputs to that stage and its output into .NET source before publish. Preserve pinned .NET SDK/runtime, non-root runtime and existing Compose topology. Update strict .dockerignore for needed source/migrations/assets, exclude local bin/obj/settings/credentials. Never read real .env for negative tests.
- [x] Run clean `npm ci`, `npm run css:build`, `dotnet build Sonrisa.sln`, focused form tests. Build Docker without credentials. Verify CSS appears in publish/image, no SDK/Node/source/secrets in runtime image. Return actual checks and get focused task review.

## Task 3: Privacy-safe logs and request traces

**Owner:** sp_integration_debugger. Files: new `Observability/ObservabilityExtensions.cs`, Program registration/log privacy changes, project package references, focused tests in existing test project. No feature/persistence schema changes.

**Interfaces:** `builder.AddSonrisaObservability()` (or equivalent one named registration extension) registers SDK logs/traces and console correlation. Default service `sonrisa-web`. OTLP is optional, no Npgsql ActivitySource subscription, no metrics.

- [x] Write/run failing tests for no-endpoint startup, explicit endpoint registration, malformed endpoint/protocol isolation, request log/trace correlation, and sensitive-sentinel exclusion. Tests inject a small capture exporter/processor or loopback OTLP test receiver; no external backend/Collector service. Test unavailable endpoint with real requests and liveness unchanged.
- [x] Add verified stable OpenTelemetry Hosting/AspNetCore/OTLP 1.18.0. Use framework ILogger and standard resources/OTLP options. Register each signal exporter only for explicit valid configuration, honoring common endpoint/protocol/headers/timeout. If signal overrides are supported, implement precedence/path behavior and test it; otherwise document support precisely.
- [x] Keep SDK batch export and no backend health/startup checks. Avoid UseOtlpExporter auto-enabling unwanted signals; register only logs/traces. Keep console provider, enable trace/span correlation without logging form scopes. Sanitize request URL/query attributes and disable exception recording. Suppress raw EF/provider diagnostics; the management service logs safe classifications. Do not add Npgsql/EF instrumentation or custom spans.
- [x] Run focused telemetry and complete test suite/build, including privacy failures and endpoint cases. Return exact supported environment keys and captured signal evidence; obtain task review.

## Task 4: Reviewed DEV migration and complete validation

**Owner:** Controller, with sp_integration_debugger for bounded integration fixes. Files: only fixes demanded by verification, test fixtures where needed, developer/database-contract documentation and actual evidence. No remote workflow/role/service changes.

- [x] Resolve existing runtime connection from project User Secrets or ignored .env in memory; never print/write value or expanded Compose configuration. Inspect only intended product database metadata using that same connection. Confirm database identity and inventory before migration. If target identity is uncertain or expected names collide, stop before applying.
- [x] Review Task 1 migration source and generated SQL; verify absence of unrelated/destructive objects. Apply the reviewed migration only to confirmed product target, using external config. Inspect actual table columns/types/constraints/indexes/history. Preserve administrative DEV exception without changing roles or transport.
- [x] Run opt-in PostgreSQL tests with explicit target-name match and unique owner fixtures. Prove cross-owner access denial and rollback/concurrent visibility using independent connections, not merely SaveChanges return values. Never truncate/reset. A failed test cleans only owned rows.
- [x] Run host on loopback with a dedicated temporary test owner/allowlist via child-process environment. Exercise browser create/edit/enable/disable, invalid number/destination, antiforgery, forged owner/foreign alert, styles/focus/mobile width, reload and process restart persistence. Remove only exact test-owned records after verification. Inspect logs for runtime secret and test sentinels without printing them.
- [x] Run application-only Docker with a distinct owned Compose project. Negative cases replace both CLI dotenv and service env_file with disposable empty/synthetic inputs, retaining build/topology. Positive readiness uses safe supplied settings in memory/external ignored configuration. Verify CSS, liveness/readiness, non-root runtime and restart/recreate; clean only owned resources.
- [x] Fresh CLI test/build and Rider build/inspection. Update README with commands actually run, ownership/allowlist configuration, two-table contract/queries, CSS build, OTLP behavior/limits, and runtime non-goals. Record ADR-009 and actual validation/review findings; preserve historical evidence. Mark plan progress only for completed checks.
- Superseded by Task 6 after the user’s destination correction: final whole-branch review through sp_final_branch_reviewer; fix Critical/Important findings, inspect full/staged diff and secret/ignore checks, stage only milestone files, then commit `feat: add alert configuration model and management UI`. No amend, merge, push, global identity change or milestone 5 work.

## Preflight self-review and acceptance

The controller checked task interfaces: Task 1 produces service/input consumed by Task 2; both share Program and tests sequentially. Task 3 extends Program/project/tests only after UI review. Task 4 consumes migration/routes/telemetry from prior tasks. No task authorizes schema application before reviewed target confirmation. Fixed condition columns, status behavior, owner scoping, privacy and no-runtime boundaries match the approved specification. Baseline CLI/Rider builds passed in context reconstruction. The existing checkout is intentionally retained on a new branch for Rider continuity and pending approved documentation; no unrelated work is moved or committed. Spec/plan are retained with the feature milestone, not extra milestone commits. Independent preflight review is required before Task 1.

## Task 5: Shared PostgreSQL user destinations and FluentValidation

**Owner:** sp_integration_debugger. Read the amended specification and ADR-010. Own user/profile entity/mapping/service/input/validator/settings page, revised alert service/input/pages, FluentValidation package reference/DI, new migration, affected automated tests, necessary CSS/Docker source inclusion. Controller owns docs/evidence, remote migration and final commit. No agent remote access, secrets inspection, subagents, stage or commits.

- [x] Add FluentValidation 12.1.1 core only, explicit IValidator registrations/manual ValidateAsync. Use framework/platform email validation, bounded Slack rule and narrowly necessary finite-number predicate; remove the old custom validation engine/results and AlertDestinationOptions. No Identity, auth infrastructure or runtime processing.
- [x] Add users(id UUID PK, email_destination varchar254 nullable, slack_destination varchar80 nullable, revision UUID required). Require one destination on saved profile and proportional checks. alerts.owner_id FK users.id restrict; all alerts use shared profile destinations. Remove the AlertChannel runtime model and form fields. Profile ID comes only from ICurrentOwner; absent profile becomes settings-first guidance.
- [x] Implement UserNotificationSettingsService owner-only get/save with first-save creation, revision concurrency and safe duplicate-first-create conflict handling. Alert creation requires a profile. Retain owner filtering on every alert/profile operation and sanitized provider diagnostics/results. Reuse/rename the small management result type if needed; no generic repositories or speculative services.
- [x] Add /settings/notifications, accessible email/Slack inputs and Save/Cancel, field/summary validation and stale/unavailable feedback. Update alert forms/nav to shared-settings explanation and remove selectors/allowlist guidance. Preserve fixed condition/status/antiforgery behavior and no client JS.
- [x] Generate a new migration without rewriting InitialAlertConfiguration. Create profiles only where every old alert of an owner has the identical complete normalized channel set (presence/absence as well as values), containing at least one target. Abort with a fixed nonsensitive message on differing values, differing channel subsets, or absent channels; test all three and the successful identical-set case. Acquire ACCESS EXCLUSIVE locks on public.alerts and public.alert_channels inside the migration transaction before validation/backfill/FK/drop, preventing concurrent legacy writes. Drop only the replaced product channel table and add the owner FK. Review source/SQL locally; do not apply. Implement Down as an explicit NotSupportedException before any operations: user profiles without alerts cannot be losslessly represented by the old schema. Do not silently discard destination data.
- [x] Update meaningful tests for FluentValidation rules, no config allowlists, settings creation/edit/stale/create-race behavior, profile owner isolation, alert settings prerequisite, shared destination joins, alert owner scoping and HTTP binding/status/antiforgery. Adapt prior child-atomicity test to profile atomicity (independent reader sees old profile before commit and complete new profile afterward; failed database check leaves old profile). Guard every PostgreSQL test with explicit target and exact fixture cleanup (alerts before users); no reset/EnsureCreated/migrate in test setup. Preserve telemetry isolation tests.
- [x] Run targeted red/green tests, local full suite with PostgreSQL explicitly skipped, build and CSS build. Report exact changed interfaces, migration/data handling, check results and remaining guarded cases. Obtain independent focused task review before controller application.

## Task 6: Revised-model acceptance and milestone commit

**Owner:** Controller, bounded fixes through custom agents. No runtime n8n work.

- [x] Review amendment migration source/SQL; reverify runtime target/current schema and unexpected data. Stop all controller-owned old app hosts before application; inspect the transaction-scoped table locks in generated SQL. Apply only reviewed expected product changes and run only the revised application afterward. Inspect users/alerts relationship/checks/current data; retain original migration history and no unrelated role/schema changes.
- [x] Run guarded PostgreSQL suite and real browser/HTTP settings-first, profile create/edit, alert create/edit/enable/disable, shared destinations, owner isolation/tampering, invalid email/Slack/typed values, stale revisions, restart persistence and exact fixture cleanup. Do not treat old allowlist-design results as new acceptance.
- [x] Rebuild Tailwind/Docker for changed markup/source/package graph; verify CSS, healthy DEV and no-connection behavior. Retain telemetry tests and no-backend behavior; rerun affected privacy checks. CLI/Rider build and focused inspection.
- [x] Update only affected current docs/contracts/spec/decision/AI-review/evidence, preserve historical reviews/prompts/migrations. Record prompt020 as the faithful finalized English version of the user-authored correction; the per-user answer is a clarification and not a separate history record.
- [x] Final whole-branch sp_final_branch_reviewer approved after the constraint correction; no open findings. Exact staged diff/secret exclusions checked. The final action is the single requested feat milestone commit containing this completed plan; no amend/merge/push or runtime milestone work.
