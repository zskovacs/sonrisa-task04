# Operational admin validation

Date: 2026-09-14

## Context and authorization

The standalone [user request](../../prompts/027-add-operational-admin-view.md) authorizes unattended implementation, validation and one milestone commit, with no merge. Initial Git inspection found a clean working tree at completed Email commit `a198778500f27d872fe7908ff4278b606fc70310`. The preceding user-authorized merge had already left that commit on `main`; this task did not check out `main` and created `feat/operational-admin-view` directly from the current HEAD. Only the new task prompt/index were written before design work.

Context review covered repository instructions, README, product brief, roadmap, scope, assumptions, architecture, validation guidance, configuration/runtime contracts, decision/review logs, ADRs, recent specifications/plans, prompt history and existing review evidence. Focused code mapping covered Razor management pages, owner resolution, EF entities/migrations, existing guarded PostgreSQL/HTTP tests, health checks and OpenTelemetry. Historical runtime/Email evidence was inspected rather than rerunning external sends.

Read-only PostgreSQL MCP inspection found only `users`, `alerts` and `__EFMigrationsHistory`, six applied migrations, four owners and five alerts. Summary baseline: one enabled and four disabled alerts, three owners with Slack and three with Email. The default management owner has one alert. Whole-row digests were recorded privately for post-test comparison; destination values were not emitted.

Read-only n8n MCP inspection found the authoritative `Sonrisa - Process Alerts - DEV` (`aVijfnQr0kdLAJHP`) inactive with 26 nodes, version `5880b333-24c6-4cfd-8d52-ae45f5d5f2b1`. No workflow execution, send, activation or modification was needed for context reconstruction.

The existing model supports the requested UI without a migration. Owner UUIDs are the available display identifiers. Saved profiles must have at least one destination, so an owner with neither destination is not a valid persisted fixture. Normal management ownership remains under ADR-008; the new explicitly authorized read-only cross-owner surface clarifies previously deferred product visibility. It does not change the runtime architecture.

## Design review

The [specification](../../docs/superpowers/specs/2026-09-14-operational-admin-view-design.md) and [plan](../../docs/superpowers/plans/2026-09-14-operational-admin-view.md) choose direct read-only page-specific EF projections, existing Tailwind/navigation and no application/n8n integration. The custom plan reviewer identified two material test-plan gaps: explicitly validate malformed as well as absent database configuration, and isolate cross-owner aggregate tests from concurrently committed test fixtures. Both were corrected before implementation. Scoped re-review approved specification compliance and quality.

The existing Chrome MCP could not connect to a browser. The installed local Playwright and cached Chromium 151.0.7922.34 were located and a headless launch succeeded without installing a project dependency. This launch alone is not page validation.

## Implementation and focused tests

Three PageModels query the existing scoped context directly through ordered scalar projections. A small shared page base centralizes fixed-message unavailable handling; it does not introduce a service/repository layer. The overview performs two sequential aggregates. The lists never select destination strings. Conditions render the persisted field/operator/value and value type; numeric formatting is invariant. Existing alert management behavior is unchanged, with only obsolete runtime-eligibility copy corrected in four views and an Admin navigation link added.

The initial HTTP red run failed 4/4 because the routes did not exist. The final first-pass focused command, `python3 /tmp/sonrisa-correction-tools/run.py dotnet test Sonrisa.sln --no-restore --filter 'FullyQualifiedName~AdminPagesTests|FullyQualifiedName~AlertPagesTests|FullyQualifiedName~PostgresPageTests|FullyQualifiedName~PostgresServiceTests'`, passed **18/18**, with no failures or skips. The wrapper supplies existing external configuration privately and verifies the target database. CSS generation and the application build passed with zero build warnings/errors.

Generated PostgreSQL fixtures cover four owners: Email-only, Slack-only, both, and an owner without alerts. Tests assert baseline-plus-fixture counts, owner association, enabled/disabled state, condition/channel labels, encoded alert text and absence of all fixture destinations. Normal `/alerts` includes only the configured test owner's alert. Foreign-owner edit GET/POST and status POST return 404 despite valid antiforgery tokens and known revision values; persisted foreign configuration remains unchanged.

Empty-result tests replace real query readers with schema-compatible empty readers. They demonstrate zero summary values and list empty states without emptying DEV. The None channel fallback is tested without violating the saved-profile constraint. These are controlled test cases, not claims that DEV has no owners/destinations.

An induced timeout exposed NpgsqlExecutionStrategy wrapping a timeout in InvalidOperationException. Handling was narrowed to wrappers with a database/timeout inner exception, preserving ordinary programming errors. Task review subsequently found that parseable but unusable connection settings also needed a narrow configuration-failure path. A new HTTP test reproduced Npgsql ArgumentNullException for a blank Host; pre-query host validation corrected it. The focused guarded admin suite then passed **7/7**, and the application build and diff check passed. Scoped re-review approved specification compliance and quality.

## Controller checks and live browser

- `dotnet build Sonrisa.sln --no-restore`: passed, zero warnings/errors.
- Guarded `dotnet test Sonrisa.sln --no-restore`: **77 passed, zero failed, zero skipped**, including existing management, health and OpenTelemetry coverage. After the unusable-configuration correction, the same guarded full command passed **78/78**, with zero failures/skips.
- `node --test n8n/tests/*.test.mjs`: **24 passed**, zero failed/skipped. No workflow source or export changed.
- `dotnet ef migrations has-pending-model-changes --project src/Sonrisa.Web --no-build`: no model changes since the last migration.

A separately owned application host on loopback port 5297 used external DEV configuration and the existing default owner. Actual Chromium 151.0.7922.34 browser requests returned 200 for `/admin`, `/admin/users`, `/admin/alerts` and `/alerts`; `/health/live` and `/health/ready` returned 200/Healthy. Browser assertions compared all six displayed summary values and every owner/alert row against read-only PostgreSQL projections. Admin displayed four owners and five alerts; normal management displayed the default owner's one alert and none of the foreign-owner names.

All three admin pages omitted every persisted destination value and had no forms. No browser page errors or external/n8n requests occurred. Desktop and 390px mobile rendering were captured and visually inspected; each document stayed within the 390px viewport, with tables scrolling horizontally inside their containers. This is a focused responsive check, not a full accessibility audit.

Screenshots: [overview desktop](operational-admin-ui/admin-desktop.png), [overview mobile](operational-admin-ui/admin-mobile.png), [owners desktop](operational-admin-ui/admin-users-desktop.png), [owners mobile](operational-admin-ui/admin-users-mobile.png), [alerts desktop](operational-admin-ui/admin-alerts-desktop.png), [alerts mobile](operational-admin-ui/admin-alerts-mobile.png).

The final application was restarted after the configuration fix; the same browser/health/owner/privacy assertions passed again. A private scan of the owned host log (961 characters at inspection) found no persisted destinations or resolved connection value. The committed evidence contains no raw configuration or log dump.

## Preserved boundaries

After the full suite and browser checks, PostgreSQL still had six migrations, four users and five alerts; both complete-row digests matched the initial baseline. Test-owned records were cleaned up. No shared schema or existing product row was changed. Source comparison found no changes to EF/model/migrations, management services/owner resolution, Program, telemetry, packages or any n8n file.

A fresh remote n8n read retained the same inactive 26-node workflow version. No workflow execution, SMTP/Slack send, activation, credential change or runtime API integration occurred. Product admin remains read-only configuration visibility; n8n owns runtime operations. Authentication/authorization is deliberately absent, so the admin pages are not a production security boundary.

## Final review and handoff

The custom task reviewer approved the narrow configuration fix and the custom final whole-branch reviewer returned **APPROVE**, with no Critical, Important or Minor findings. The final review inspected the staged implementation, documentation/evidence and representative screenshots; it relied on the recorded runtime checks rather than claiming to rerun them.

The controller inspected the staged source/scope and checked 162 local Markdown links with no broken targets. Staged text contained neither actual DEV destination values nor the externally resolved connection value; private-key/Slack-secret patterns were absent. No model, migration, normal ownership service, runtime, n8n export, package or telemetry change is staged. The exact prompt archive was compared to the user-authored source and matched all 16,792 characters.

Both owned validation hosts were stopped. The final log remained destination-free; test fixtures were removed and existing DEV rows remained unchanged. Only milestone files and six sanitized screenshots are included. The requested commit message is `feat: add operational admin view`; the branch remains `feat/operational-admin-view` without merging or starting the next milestone.

Remaining limits: the admin area has no authentication/authorization and must not be exposed as a production security boundary. Empty-database and failure cases use controlled HTTP fixtures, not destructive DEV changes. The existing DEV privilege/TLS exception and runtime best-effort/deduplication limits remain unchanged; SMTP4DEV evidence still proves capture rather than external inbox delivery. Integrated final validation remains separately authorized work.
