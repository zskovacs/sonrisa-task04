# Operational admin view implementation plan

Date: 2026-09-14

Specification: [operational admin view](../specs/2026-09-14-operational-admin-view-design.md). User authority: [prompt 027](../../../prompts/027-add-operational-admin-view.md). Baseline: `a198778`; branch: `feat/operational-admin-view`.

## Global constraints

- No database schema, EF model, migration, dependency or n8n runtime change.
- No sending, runtime API, worker, queue, runtime persistence or n8n client.
- No admin mutation handlers, user management, authentication or role simulation.
- Normal `/alerts` management retains all configured-owner filters.
- Admin queries intentionally span owners and never render full destinations.
- Use existing Razor Pages, Tailwind, EF Core, logging and OpenTelemetry.
- Leave `feat/operational-admin-view` checked out; commit `feat: add operational admin view` without merging.

## Task 1 — Read-only pages and focused protection

Own new `src/Sonrisa.Web/Pages/Admin/` files, the minimal shared-layout navigation addition, the obsolete runtime sentence on `Pages/Alerts/Index.cshtml`, and focused new admin tests under `tests/Sonrisa.Web.Tests/`.

1. Inspect existing PageModels, management failure handling, entity mapping and PostgreSQL/WebApplicationFactory fixtures. Add meaningful failing tests for the new routes and ownership/privacy contracts before implementation.
2. Implement overview with two sequential aggregate projections; default empty results to zero. Implement owners and alerts as ordered scalar projections. Avoid fetching destination values. Use the persisted condition and channel-presence fields for display.
3. Implement safe 503 handling consistent with existing configuration pages: validate a missing or malformed connection string narrowly before querying (NpgsqlConnectionStringBuilder/ArgumentException), and handle expected database failures with fixed warnings and generic responses. Keep query programming errors unmasked. Add no mutation handlers and no current-owner filter on admin reads. Add a small shared admin navigation, responsive cards/tables and empty states. Preserve normal management code.
4. Cover multiple owners, all channel combinations representable in the schema, count/state/association correctness, invariant conditions, HTML encoding and destination privacy. Use generated fixture IDs and guaranteed cleanup. Put aggregate/admin PostgreSQL HTTP tests in an xUnit collection with DisableParallelization = true, which runs separately from other test collections, so existing committed fixtures cannot race baseline-plus-fixture assertions. Do not change production isolation or global suite behavior. Verify foreign-owner edit GET/POST and status POST with valid antiforgery tokens, and unchanged persisted foreign data.
5. Cover both missing and malformed connection configuration through HTTP tests asserting generic 503 responses and absence of raw configuration/exception/destination content. Cover controlled empty-result pages using existing dependencies. An empty-reader interceptor is acceptable test-only machinery; do not add a database provider, change constraints or delete existing rows to get an empty database. Test defensive None rendering separately.
6. Run focused admin tests with the existing guarded DEV helper, existing alert-management regressions, CSS build and application build. Inspect source diff. Report exact commands/results and any limitations. No commit by implementer; controller performs the single milestone commit.
7. Obtain task-scoped specification and quality review; correct Important/Critical findings before continuing.

## Task 2 — Runtime validation, documentation and handoff

Controller owns live verification, evidence and current documentation; no source overlap with Task 1.

1. Run the full .NET suite with guarded external PostgreSQL configuration and the unchanged Node workflow suite. Start the built application on an unused loopback port; inspect `/health/live`, `/health/ready`, `/admin`, `/admin/users`, `/admin/alerts` and `/alerts`.
2. Inspect responsive browser rendering if available. Record actual DEV counts/channel indicators without full destinations. Compare data/schema and remote inactive workflow/version to the read-only baseline; no new migrations or workflow changes are permitted.
3. Update only affected current documentation: README, current task instructions, roadmap, scope, assumptions, architecture, validation guidance and configuration contract where needed. Explain cross-owner product visibility versus n8n runtime operations, preserved owner management, deferred authentication and no schema/runtime changes. Record real review corrections and evidence without inventing interactions.
4. Check exact prompt archival, full diff and secrets/privacy. Obtain final custom whole-branch review and resolve material findings. Stage only milestone files and inspect exact staged diff.
5. Create signed commit `feat: add operational admin view`, verify hash/status/branch and leave branch checked out. No merge, push or next milestone.

## Verification reference

Use existing commands from README and `python3 /tmp/sonrisa-correction-tools/run.py dotnet test Sonrisa.sln --no-restore` for externally configured guarded database tests. Never print connection configuration. Test-owned writes are scoped to generated IDs; ordinary browser validation is read-only. The PostgreSQL baseline has six migrations, four users and five alerts. The remote workflow baseline is version `5880b333-24c6-4cfd-8d52-ae45f5d5f2b1`, inactive, 26 nodes.
