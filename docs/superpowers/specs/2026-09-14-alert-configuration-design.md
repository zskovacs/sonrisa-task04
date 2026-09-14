# Alert configuration and management specification

Approved by the user on 2026-09-14 after repository reconstruction, then amended by the explicit user correction selecting shared per-user destinations and an existing validation framework under ADR-010. This records the approved in-chat design for milestone 4, `feat: add alert configuration model and management UI`. ADR-002 remains authoritative for one condition; ADR-008 replaces demo identity switching. ADR-005/006/007 retain workflow ownership, shared DEV topology, and the accepted DEV exception.

## Deliverable and boundaries

One .NET 10 Razor Pages application remains under `src/Sonrisa.Web`, with the root `Sonrisa.sln`. Add configuration persistence, management forms, a small Tailwind build, and OpenTelemetry logs/request traces. No authentication, authorization infrastructure, identity switching, runtime evaluator, runtime tables, n8n operations, messages, extra frontend application, or observability platform. The original prompt's multi-condition candidate is rejected; only the approved design applies.

## PostgreSQL contract

Use explicit `public.alerts` and `public.users` mappings, no naming-convention dependency. Inspect the existing product database and object inventory before migration application; collisions or uncertain target identity stop application. EF owns the migration and standard migration-history table.

`alerts` contains:

| Column | Type and rule |
| --- | --- |
| id | UUID primary key, application-generated, nonempty |
| owner_id | Nonempty UUID, assigned by application current-owner resolver |
| name | varchar(120), trimmed and nonblank, duplicate names permitted |
| event_type | text, only `earthquake` |
| enabled | boolean, initially false |
| revision | Nonempty UUID, regenerated on every configuration/status change, optimistic concurrency token |
| condition_field | text, only `magnitude` |
| condition_operator | text, only `gte` |
| condition_value_type | text, only `number` |
| condition_value | double precision, finite IEEE-754 binary64 value |

All columns are required. Database CHECK constraints enforce fixed codes, nonempty identifiers, name bounds/nonblank text, and finite condition values. Store the single required condition on the alert row: no condition collection, identity, or separate table is necessary. A future numeric operator may reuse storage; string/boolean support needs a reviewed typed-storage extension rather than speculative polymorphic storage now. n8n reads the numeric column directly; no guessing or casting a generic string, enum integers, JSONPath, or executable expressions.

`users` has a nonempty UUID `id` primary key, optional `email_destination` varchar(254), optional `slack_destination` varchar(80), and nonempty UUID `revision`. A saved profile requires at least one nonblank destination; supplied values are trimmed and bounded. Add proportional checks for nonempty identifiers, destination nonblank/bounds and at least one destination. The existing `alerts.owner_id` references users.id with restricted deletion; no public user/alert delete exists. Add no email or Slack index because lookup is by current owner UUID. Remove alert_channels through a new reviewed migration; the original applied migration remains historical.

Every alert uses the owner's common destinations. There are no per-alert channel selections. A profile is created on the first successful settings save, before creating an alert. If the earlier schema contains data, every alert of one owner must have the same complete normalized channel set, including presence or absence of each channel, with at least one destination. Abort with a fixed nonsensitive message on differing values, differing channel presence, or absent channels; never union different alert target sets or pick arbitrarily. Within the EF migration transaction, acquire ACCESS EXCLUSIVE locks on public.alerts and public.alert_channels before validating, copying, adding the foreign key, and dropping the old channel table. Stop owned application hosts for this migration window and do not run the old application against the new schema. Downgrade is explicitly unsupported: Down throws a clear NotSupportedException before any operations, because profiles without alerts cannot be represented losslessly in the old schema.

Future n8n uses explicit projections and parameterized SQL, joining enabled alerts to users for shared destinations in one consistent statement snapshot. Management writes commit atomically. n8n receives no configuration-write/schema privileges. This milestone does not create roles, workflows, or runtime processing. Connectivity readiness does not certify schema compatibility.

## Ownership and configuration

Expose a small `ICurrentOwner` with `Guid OwnerId`. Its configured implementation resolves `MvpOwner:Id` using normal .NET configuration; the default is a fixed non-secret nonempty UUID shared across restarts. Invalid explicit owner configuration must not silently select another owner. Invalid owner configuration can fail startup with a key-only validation message; no value is logged. The default means missing owner configuration remains runnable.

All application alert operations go through one concrete `AlertManagementService` and filter by the resolver's owner. Retrieve editable entities with both ID and owner; never attach form-supplied entities or bind owner/child IDs. Child modifications are made only on that retrieved alert. Foreign-owner IDs have the same NotFound result as absent IDs. Future authentication replaces the resolver and maps authenticated subjects to the same owner UUID; changing configuration does not transfer alerts.

This MVP is single-user and does not implement authentication. The persistence/query model is ownership-aware so authentication can be added later without redesigning alert ownership. Authentication was not part of the requested feature scope and is deliberately deferred. The current resolver is not production authentication or a security boundary. Local/loopback development usage remains required, including the published container's Production ASP.NET hosting mode.

Destination configuration belongs solely in users, managed through the current-owner settings page. Remove AlertDestinations options/configuration and allowlist validation. Channel credentials remain in future n8n runtime configuration. No user profile is automatically fabricated at startup; absence is a useful settings-first state. The real profile is required for current product data, not authentication.

## Validation and management service

An `AlertInput` contains name, threshold text, enabled, and revision for edits. A `UserNotificationSettingsInput` contains optional email destination, optional Slack destination and revision. Neither accepts owner/child identifiers or fixed condition discriminators.

Use FluentValidation 12.1.1 core (`AbstractValidator<T>`, registered explicitly as `IValidator<T>`) and explicit ValidateAsync invocation. Its Apache-2.0 package targets net8.0 or later and supports this net10.0 project. Do not add the legacy FluentValidation.AspNetCore automatic pipeline or an assembly-scanning package. Use built-in rules for required values, lengths and email shape; retain only narrowly necessary typed parsing, bare-mailbox/control-character and Slack-ID rules. Do not retain a parallel custom validation-result/engine alongside FluentValidation.

Threshold syntax remains invariant floating point without thousands separators: reject blanks, parse failures, overflow, NaN and infinities; no scientific range bound. Names are trimmed, 1–120 characters. Profile email must be a single bare mailbox, not a display-name form or CR/LF-bearing value; use platform/framework validation rather than a bespoke RFC regex. Slack is an uppercase alphanumeric opaque channel ID bounded to 80 characters, not a URL/name/token. At least one destination is required when saving a profile. No transport is contacted for validation.

A scoped AlertManagementService handles alerts; a small scoped UserNotificationSettingsService handles the current owner's profile using EF directly. All reads/updates filter by owner context. Create alert requires the current owner's persisted settings; otherwise return actionable validation linking to settings. All alerts use the profile's common destinations; disabling does not require revalidating destination state. Settings cannot be saved without any destination. The current-owner ID is the users primary key and is assigned by the service, never bound from the form.

Use one SaveChangesAsync for each alert or complete profile update. Submitted revision is the original concurrency value; assign a new UUID on updates. Concurrent profile creation must return conflict rather than expose a unique-key error. Profile/alert stale forms preserve input and show reload guidance. The current model needs no child collection replacement or distributed transaction. n8n's joined read sees one committed configuration snapshot.

Structured outcomes remain success, not found, conflict, invalid field errors, unavailable. The existing small result type may be named for management when reused by both services; no repository/mediator/mapper architecture is introduced. Never log input/entities, owner/revision IDs, destinations, thresholds, connection strings or raw exceptions. Preserve cancellation and safe unavailable behavior. Razor binding errors must be handled before writes, including boolean/revision conversion; built-in antiforgery remains enabled.

## Razor Pages and styling

Routes `/alerts`, `/alerts/create`, `/alerts/{id:guid}/edit`, plus `/settings/notifications` for shared current-owner destinations; root redirects to Alerts. List name, condition summary, status, Create/Edit and explicit Enable/Disable POST forms. Alert editors group general settings and the fixed magnitude condition, with a clear link/explanation that delivery uses shared notification settings. If a profile is absent, direct the user to save destinations before creating alerts. The settings page has labeled email/Slack inputs, Save/Cancel and near-field validation; only real implemented routes appear in navigation.

Use Tag Helpers/antiforgery, labels, semantic fieldsets, focus visibility, readable status, modest responsive layout, shared alert editor and layout. Render status hidden fields as literal string true/false. No custom JavaScript, design system or SPA is needed. Runtime processing remains explicitly unimplemented.

Pin `tailwindcss` and `@tailwindcss/cli` 4.3.3 with root package.json/package-lock.json. One CSS input with explicit Razor source detection. Scripts `css:build` and `css:watch`. Generated `wwwroot/css/site.css` is ignored, generated before .NET build/publish in documented clean-checkout commands. Docker uses a build-only Node stage and copies CSS into source before publishing; no Node runtime or credential input in image build. Update .dockerignore deliberately for all required source, Razor, migration, and asset inputs while excluding bin/obj, local settings, credentials, tests, docs, and prompts. Serve static CSS with ASP.NET facilities.

## Observability

Use stable `OpenTelemetry.Extensions.Hosting`, `OpenTelemetry.Instrumentation.AspNetCore`, and `OpenTelemetry.Exporter.OpenTelemetryProtocol` 1.18.0. ILogger remains API, console logging retained, service defaults to `sonrisa-web` with standard external OTEL_SERVICE_NAME override. Add SDK logs and incoming request traces, naturally correlated trace/span IDs; no metrics registration, HTTP client instrumentation without a consumer, or manual spans.

Register OTLP log/trace exporters only when an explicit applicable endpoint exists. Support standard common endpoint/protocol/header/timeout configuration; document actual supported signal-specific behavior accurately. Use SDK batch processors, no request-time flush or startup connection check. Missing endpoint exports nothing; unavailable backend does not alter startup, request behavior, liveness/readiness or create recursive exporter logging. Invalid exporter configuration should disable affected export with a sanitized key-only warning rather than take down management.

Use route templates/status/duration, omit raw URLs/query values/request bodies/user input and exception recording. Preserve provider-log privacy and suppress EF command/update/query diagnostics that could carry sensitive values; application handling supplies sanitized failure logs. Do not subscribe to Npgsql activity sources: its current SQL/exception emissions bypass log filters. Database spans are explicitly deferred by the approved design. No shared-database distributed-trace propagation is claimed. ADR-009 records this project-wide observability standard.

## Migration and verification safety

Generate migration and inspect source plus SQL before any application. No EnsureCreated/Migrate at startup. Controller alone authorizes application after comparing runtime-config target with intended product identity and verifying only expected tables/indexes/checks/history are affected. Use external credentials without hardcoded design-time fallback. Preserve unrelated schemas/tables/roles and n8n internals.

One test project under `tests/Sonrisa.Web.Tests` covers configuration validation, form boundary, ownership, edits/status/shared profiles/revisions, and telemetry configuration/privacy. Real PostgreSQL tests require explicit `SONRISA_TEST_DATABASE` plus an explicit expected database name, never automatically use production/runtime config or create/drop databases. Use isolated generated owner/alert IDs. Roll back where possible; committed UI/concurrent visibility fixtures are removed only by exact owned IDs. Do not truncate tables or reset shared data. No EF InMemory, local PostgreSQL or Testcontainers. Baseline/skip status must be honest when external configuration is absent.

Validate CLI/Rider builds and tests, real migration/mapping/constraint behavior, profile/create/edit/status/restart persistence, atomic profile visibility/rollback, foreign-owner and forged OwnerId requests, invalid input, antiforgery, browser rendering and CSS, clean CSS reproduction, application-only Docker startup/lifecycle, liveness/readiness, no-endpoint and unavailable-endpoint startup, actual correlated logs/request traces through a test exporter/receiver, and absence of sentinel secrets in logs/telemetry. Do not send email/Slack. Record real evidence and reviewed corrections. Final whole-branch review and exact staged inspection precede the requested milestone commit.

## Execution process

The user approved same-session implementation without another generic gate. Keep the Rider-associated checkout on `feat/alert-configuration`, preserve existing approved documentation work, and use reviewed bounded custom-agent tasks. Record spec/plan with the feature milestone rather than create extra design milestones. Do not merge/push or begin runtime milestone 5 after completion.
