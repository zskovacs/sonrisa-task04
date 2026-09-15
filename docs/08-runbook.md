# Sonrisa runbook

This runbook describes the delivered MVP. It uses the existing shared DEV PostgreSQL and n8n services; this repository neither provisions nor manages those services. Keep the workflow inactive unless a separate change explicitly authorizes activation.

## Application

Use .NET SDK 10.0.401 (or its permitted later patch), Node 24 and npm 11. From the repository root:

```bash
dotnet tool restore
npm ci
npm run css:build
dotnet run --project src/Sonrisa.Web
```

The Development host is available at `http://localhost:5180`. Its health endpoints are:

```bash
curl -i http://localhost:5180/health/live
curl -i http://localhost:5180/health/ready
```

Liveness is independent of PostgreSQL. Readiness makes an actual PostgreSQL connectivity check and returns 503 for missing, malformed, unavailable, or stalled configuration. It does not prove that the schema is current, n8n is ready, or transports can deliver.

### Database configuration

For local Development, set `ConnectionStrings:ProductDatabase` in the existing project User Secrets store. An externally supplied `ConnectionStrings__ProductDatabase` environment variable overrides User Secrets. Obtain the value through the selected environment's secret process; never put it in a tracked file, command transcript, prompt, screenshot, or evidence.

The following Bash sequence was validated in the skeleton milestone. It keeps a literal connection value out of shell history and terminal echo:

```bash
read -r -s -p "DEV product database connection: " SONRISA_DEV_CONNECTION
printf '\n'
dotnet user-secrets set "ConnectionStrings:ProductDatabase" "$SONRISA_DEV_CONNECTION" --project src/Sonrisa.Web
unset SONRISA_DEV_CONNECTION
```

The application starts with no connection configured, logs only the configuration key, and keeps provider connection details out of host logs. Restart it after changing configuration. The accepted shared DEV application credential and observed lack of PostgreSQL TLS are DEV-only exceptions documented by [ADR-007](adr/ADR-007-accept-current-dev-database-access.md). They are not a production access or transport-security model.

If readiness fails, first confirm the key is supplied to the process, then confirm the intended shared DEV database is reachable. Do not print expanded environment, connection strings, or provider exceptions. A readiness failure can also result from database unavailability; liveness should remain healthy in that case.

### Owner and alert setup

`MvpOwner:Id` optionally supplies a nonempty UUID; the default is `d203a533-6bf8-4a21-98a9-291a74ef9f28`. Keep it stable: changing it selects another ownership scope and does not transfer alerts. Invalid explicit owner configuration fails startup. This is not authentication.

Open `/settings/notifications` and save at least one destination before creating an alert. Email is one bare mailbox; Slack is an uppercase alphanumeric channel ID. Shared settings apply to every alert for that owner. Create/edit alerts under `/alerts`; use a finite magnitude threshold with a dot decimal separator. New alerts are disabled. Saving or enabling does not invoke n8n. A stale edit conflicts and requires reloading. No delete, per-alert channel selection or user administration is implemented. [Configuration contract](06-alert-configuration-contract.md).

### Tailwind CSS

Run `npm run css:build` before a .NET build or publish from a clean checkout. While editing Razor markup, use:

```bash
npm run css:watch
```

Tailwind scans the Razor Pages source and writes the generated `src/Sonrisa.Web/wwwroot/css/site.css`. That generated asset, `node_modules`, and build caches are ignored. The Docker build runs the CSS build in its build stage.

### EF Core migrations

The intended shared product database is `sonrisa_dev`. Using the selected connection in a trusted database client, verify the target without printing connection settings:

```sql
SELECT current_database();
SELECT "MigrationId" FROM public."__EFMigrationsHistory" ORDER BY "MigrationId";
```

Compare the result with the expected target and the six recorded migrations in [integrated evidence](../evidence/reviews/2026-09-14-end-to-end-validation.md#actual-postgresql-state-and-cleanup). Stop if it is a different database. An MCP connection's target does not establish the EF command's target; check the same externally supplied connection used for that operation.

EF Core owns the product schema only. The application never applies migrations at startup. Review the target identity and generated SQL before applying a migration:

```bash
dotnet ef migrations script --project src/Sonrisa.Web
dotnet ef database update --project src/Sonrisa.Web
```

For a new migration, use a separate migration role supplied externally, inspect the generated SQL and actual target before applying, then recheck migration history and application readiness. Offline script generation needs provider metadata only; the design-time factory reads User Secrets and then environment settings, with environment taking precedence. The application-runtime and n8n credentials should be separate least-privilege roles; ADR-007 preserves the existing DEV application exception. No migration is needed for this completed checkout's already migrated DEV database.

Do not apply migrations blindly to shared DEV. n8n must not receive schema-owner privileges, and no migration should modify n8n internal storage. Product configuration currently consists of `users`, `alerts`, and EF migration history; historical runtime tables were removed by an EF migration.

### Docker

Use Docker Engine and Compose 2.24 or newer; earlier validation used Engine 29.8.0 and Compose v5.5.1. Stop a direct application instance before using the same port with Docker. The application-only Compose topology does not provision PostgreSQL or n8n. Create an ignored `.env` from `.env.example` when container configuration is needed, keep it private, and use an externally supplied value for `ConnectionStrings__ProductDatabase` only when intentionally testing shared DEV connectivity.

Do not overwrite an existing `.env`. For a new Linux copy, apply `chmod 600 .env` before entering credentials. Leave the key empty for liveness-only testing. Compose single-quoted dotenv values preserve literal dollar signs; use its documented escaping if a value contains a quote. User Secrets are not mounted into the container. The application has no direct `.env` loader; Compose supplies its environment. The container uses the Production ASP.NET environment, a non-root user and port 8080 internally.

```bash
docker compose config --quiet
docker compose up --build -d
curl -i http://localhost:5180/health/live
curl -i http://localhost:5180/health/ready
```

Compose publishes only `127.0.0.1:5180`. Use `docker compose up -d --force-recreate web` after changing `.env`, then remove the application-only resources with `docker compose down` when finished. Do not print resolved Compose configuration because it may contain secrets.

### Observability

The application retains JSON console logs. OpenTelemetry request traces are exported when an explicit trace endpoint is configured. OTLP export is disabled until an external endpoint is configured through the supported `OTEL_EXPORTER_OTLP_*` environment variables; the default service name is `sonrisa-web` and can be overridden with `OTEL_SERVICE_NAME`. Do not put OTLP headers or endpoint credentials in source control. n8n global OpenTelemetry is not configured or validated by this project.

| OTLP setting | Behavior |
| --- | --- |
| `OTEL_EXPORTER_OTLP_ENDPOINT` | Explicit HTTP(S) endpoint; no backend is built in. |
| `OTEL_EXPORTER_OTLP_PROTOCOL` | `grpc` default or `http/protobuf`. |
| `OTEL_EXPORTER_OTLP_HEADERS` | Comma-separated header assignments, supplied privately. |
| `OTEL_EXPORTER_OTLP_TIMEOUT` | Positive integer milliseconds. |

Each also supports `OTEL_EXPORTER_OTLP_TRACES_*` and `OTEL_EXPORTER_OTLP_LOGS_*`; a nonempty signal setting overrides the common one. HTTP/protobuf appends `/v1/traces` or `/v1/logs` to a common endpoint; signal endpoints are used exactly. Endpoint user information, queries and fragments are rejected. Invalid settings disable the affected signal with a key-only warning. Export uses background batching; missing/unavailable OTLP does not block startup or health. There are no metrics, Npgsql/EF spans or cross-process trace propagation through PostgreSQL. [ADR-009](adr/ADR-009-use-opentelemetry-logs-and-request-traces.md).

Unexpected request exceptions handled by the application return a generic 500 and emit `UnexpectedRequestFailure` with only `ExceptionType` and `TraceCorrelation`. Use that correlation to find the request trace. Raw exceptions and provider/framework request diagnostics remain suppressed; hosting-lifetime messages stay available locally but are excluded from OTLP logs. Intentional 409/503 page results retain their existing behavior.

## n8n operations

Open [Sonrisa - Process Alerts - DEV](https://n8n.nasgard.io/workflow/aVijfnQr0kdLAJHP) in the existing shared instance. The current local artifact is [n8n/workflows/process.json](../n8n/workflows/process.json). The [independent-review corrections](../evidence/reviews/2026-09-15-independent-review-corrections.md) have not been imported into the hosted workflow; previous live evidence applies to the earlier revision. Its source, local checks, import/rebinding process, native history limits, retry behavior, and diagnostics are documented in [n8n/README.md](../n8n/README.md).

If an import is needed in an authorized target, use the n8n editor’s import-from-file action with `n8n/workflows/process.json`, then inspect its nodes, connections and inactive state. Prefer inspecting the existing authoritative DEV workflow for a review: importing creates another workflow and does not transfer technical history. Importing the sanitized artifact creates a workflow without credentials or prior deduplication history. Before any execution, explicitly rebind only these credential categories:

- `postgres` for the `Load owner alert configuration` node, targeted at the intended product database;
- `slackApi` for `Send Slack notification`;
- `smtp` for `Send Email notification`.

The intended database permission boundary is read-only access to `public.users` and `public.alerts`. Do not edit unrelated credentials, n8n internal persistence, roles, or shared configuration. The artifact remains inactive after import. Inspect the saved graph and keep the workflow inactive; a manual execution can send notifications.

Use n8n execution views for runtime administration: execution IDs, node failures, native retries, and terminal diagnostic items. Sonrisa's `/admin` pages report product configuration only and intentionally do not duplicate n8n operations.

For a no-send local representation check:

```bash
node n8n/build-workflow.mjs process --fixture earthquakes.json
node --test n8n/tests/*.test.mjs
```

This builds/tests local workflow representations; it does not call n8n. For a controlled remote validation, first inspect all matched destinations and suppress/mock transports. Repeat the full workflow entry when testing deduplication. Never replay a downstream Slack or Email node: that bypasses the duplicate boundary and can resend.

Native previous-execution duplicate history is node-scoped and bounded at 10,000 entries. It is neither a permanent ledger nor a global atomic claim. History loss/recreation and changed provider identifiers can admit duplicates; concurrent executions have no globally atomic claim guarantee. In the validated release the engine can fail before filtering when stored plus incoming entries exceed the cap; do not clear history or add PostgreSQL runtime tables as an operational workaround.

Slack and Email each use five total native attempts, with five seconds between failures and a separate error output. An exhausted item is discarded and later notifications continue. This is best-effort behavior, with no queue, recovery worker, dead-letter handling, or exactly-once guarantee.

### Transport checks

Slack requires the bot posting scope `chat:write` and access to the configured channel; the versioned [app manifest](../n8n/slack-app-manifest.json) requests only that scope. Slack destinations are configured product values; transport credentials stay in n8n. Inspect failure data in n8n without copying raw credential or destination values into diagnostics.

Email uses the matched owner's configured bare mailbox and the n8n SMTP credential. SMTP4DEV was used for controlled validation and proves SMTP submission/capture only. It does not prove external-provider or internet-mailbox delivery. The sender `sonrisa@example.test` is fixed in the SDK/artifact and enforced by the exporter for the SMTP4DEV demonstration. An external provider requires a real permitted sender, coordinated SDK/exporter/artifact updates, credentials and controlled validation; changing the credential alone is insufficient.

## Documented incidents

The earlier, superseded three-workflow runtime encountered two real Slack credential conditions: a credential decryption failure before any Slack request, and a missing required OAuth scope after decryption succeeded. Those were operator credential/configuration issues, not evidence that the final one-workflow artifact is currently unhealthy. If a comparable error appears, inspect the intended `slackApi` binding and its required posting scope in n8n, reauthorize through the authorized operator process, and then run a controlled validation. Do not copy tokens, alter global n8n encryption settings, or retry downstream send nodes blindly.

For a missing or unusable PostgreSQL credential, verify only the intended `postgres` binding and database target. For SMTP, verify only the intended `smtp` binding and capture target. Rebinding is expected after importing the sanitized artifact because credential references are deliberately excluded. Credentials must never be exported, committed, or pasted into evidence.

## Local tests and guarded integration tests

```bash
dotnet build Sonrisa.sln
dotnet test Sonrisa.sln
node --test n8n/tests/*.test.mjs
```

Relational tests skip unless `SONRISA_TEST_DATABASE` and the expected `SONRISA_TEST_DATABASE_NAME` are supplied externally. Those tests write generated fixtures only after verifying `current_database()`, then roll back or remove their exact records. They do not migrate, truncate or reset the database. Enable them only for a reviewed test target; the final documentation check deliberately leaves them disabled and reuses the prior guarded result.

For portable recorded counters, run the unguarded suite directly and inspect the generated TRX rather than relying on an abbreviated terminal formatter:

```bash
env -u SONRISA_TEST_DATABASE -u SONRISA_TEST_DATABASE_NAME \
  dotnet test Sonrisa.sln --logger 'trx;LogFileName=local-tests.trx' \
  --results-directory artifacts/test-results
```

Reports are ignored build output. Review them for machine-specific paths and sensitive data before retaining evidence. This direct command does not depend on the historical untracked credential helper. It intentionally does not reproduce database-gated integration checks.

## Evidence and limits

Setup and Docker operations were exercised in [skeleton evidence](../evidence/reviews/2026-09-14-skeleton-review.md); final clean-checkout tool/CSS/build checks are recorded in [application evidence](../evidence/reviews/2026-09-14-e2e-application.md#clean-checkout-reproducibility). This documentation session’s checks are separate in [final evidence](../evidence/reviews/2026-09-15-final-documentation.md).

The [end-to-end validation record](../evidence/reviews/2026-09-14-end-to-end-validation.md) distinguishes real checks from mocks and prior evidence: 83/83 guarded .NET tests, 24/24 Node tests, real USGS reading with suppressed transports, historical Slack acceptance, and one SMTP4DEV capture. It does not prove external Email inbox delivery, continuous scheduling, permanent deduplication, or guaranteed delivery. The [validation strategy](05-validation-strategy.md) and [AI review log](ai-review-log.md) provide the broader rationale and acceptance limits.
