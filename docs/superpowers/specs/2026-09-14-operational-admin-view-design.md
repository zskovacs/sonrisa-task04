# Operational admin view

Date: 2026-09-14

## Authority and baseline

The user authorized unattended design, implementation, validation and a single milestone commit in [prompt 027](../../../prompts/027-add-operational-admin-view.md). Routine design approval is not required. Branch `feat/operational-admin-view` starts directly at completed Email milestone `a198778`; the working tree was clean before recording this request. Do not merge or advance to the next milestone.

PostgreSQL contains `users`, `alerts` and EF migration history. Owners have UUID identifiers, optional non-secret Email/Slack destinations, and alerts. The existing constraint requires at least one destination per persisted owner. Alert conditions persist field, operator, value type and numeric value. ADR-008 scopes normal management to the configured owner; ADR-010 defines shared owner destinations; ADR-012 delegates runtime processing to n8n. These decisions support a separate read-only cross-owner product view without a new ADR.

The DEV baseline has four owners and five alerts. The authoritative `Sonrisa - Process Alerts - DEV` workflow is inactive with 26 nodes. No runtime or schema change is needed.

## Recommended design

Add three Razor Pages using the existing layout and Tailwind styles:

| Route | Read-only content |
| --- | --- |
| `/admin` | Owner/alert totals, enabled/disabled alerts, owners with Slack/Email configured; links to the two lists |
| `/admin/users` | Owner UUID, alert/enabled counts and Yes/No destination indicators |
| `/admin/alerts` | Owner UUID, alert name, event type, enabled state, persisted condition and channel labels |

Each PageModel injects the existing scoped `AppDbContext` and `ILogger`. Use direct `AsNoTracking` scalar projections with cancellation tokens and deterministic ordering. Do not inject `ICurrentOwner` into admin pages. Summary queries aggregate owners and alerts separately and sequentially; empty aggregates become zero. Navigation collections allow counts and channel-presence projections without loading destination strings.

Render the persisted condition field and numeric value using invariant formatting, mapping the current `gte` operator to `>=`. Razor encodes text. Channel labels are Slack, Email, Slack + Email or a defensive None fallback. Existing constraints prevent persisting the last combination; do not weaken them for a test. Show owner UUIDs because the model has no display-name field.

Provide a small shared admin navigation partial, simple cards/tables, scoped table headings, responsive overflow and clear empty states. Add an Admin link to the existing layout. Correct the existing alert page's obsolete claim that runtime processing is unimplemented; do not change its management behavior.

Missing/malformed database configuration and database query failures produce a safe unavailable message and HTTP 503. Log a fixed diagnostic without raw exceptions, destinations or connection values, following the existing management pattern. Do not hide query programming errors with a broad catch.

The admin area explicitly describes cross-owner configuration visibility. Runtime workflow executions, failures, retries and integration diagnostics remain in n8n. Authentication/authorization is deferred; these pages are not a production security boundary.

## Global constraints

- No database schema, EF model, migration, dependency or n8n runtime change.
- No sending, runtime API, worker, queue, runtime persistence or n8n client.
- No admin mutation handlers, user management, authentication or role simulation.
- Normal `/alerts` management retains all configured-owner filters.
- Admin queries intentionally span owners and never render full destinations.
- Use existing Razor Pages, Tailwind, EF Core, logging and OpenTelemetry.
- Leave `feat/operational-admin-view` checked out; commit `feat: add operational admin view` without merging.

## Validation

Use existing xUnit/WebApplicationFactory and guarded PostgreSQL testing. Generated test-owned profiles and alerts cover Email-only, Slack-only, both channels, enabled/disabled alerts and an owner without alerts. Clean up only those generated IDs. Run these aggregate tests in an xUnit collection with DisableParallelization = true to avoid concurrent test-fixture changes. Assert baseline-plus-fixture totals, per-owner rows, condition/channel rendering, destination absence and HTML encoding. Verify normal owner-only lists plus foreign edit/status requests with genuine antiforgery tokens cannot mutate another owner's record.

Test missing database configuration and empty results without deleting shared DEV data. A small test-only command interceptor may return an empty reader with the real query's schema; label this as controlled empty-result testing, not a live empty database. Test the defensive None label without persisting an invalid owner.

Run focused tests, build CSS/application, full relevant regression tests and the unchanged workflow test suite. Start the application on a separate loopback port with externally supplied DEV configuration; inspect health endpoints and all requested pages, including browser layout where tooling allows. Compare PostgreSQL schema/data and n8n version/export against the baseline. Store sanitized evidence and obtain task and whole-branch reviews before the signed milestone commit.
