# Sonrisa alerts

A product for user-configured alerts about important world events, with email and Slack notifications, room for future delivery channels, and an admin view.

**Implemented: the first end-to-end n8n workflow and its SMTP extension.** Persisted alert configuration drives manual USGS ingestion, typed matching and an explicitly selected Slack or email delivery through one shared workflow. EF owns the two runtime tables and the email-state constraint migration; all three shared DEV workflows remain inactive. One synthetic Slack notification and one SMTP4DEV email were accepted and recorded as sent. Repeating each selected intent stopped without another send. See the [runtime specification](docs/superpowers/specs/2026-09-14-first-runtime-design.md), [workflow runbook](n8n/README.md), [initial validation](evidence/reviews/2026-09-14-first-runtime-validation.md) and [SMTP validation](evidence/reviews/2026-09-14-smtp-delivery-validation.md). Automatic retries and queue draining remain deferred. The Razor Pages management UI and OpenTelemetry setup remain unchanged.

This MVP is single-user and does not implement authentication. The persistence/query model is ownership-aware so authentication can be added later without redesigning alert ownership. Authentication was not part of the requested feature scope and is deliberately deferred. The configured-owner resolver is an MVP/development mechanism, not a security boundary; see [ADR-008](docs/adr/ADR-008-use-single-configured-mvp-owner.md).

## Development topology

| Component | Location and responsibility |
| --- | --- |
| ASP.NET Core / Razor Pages | Developer machine, directly or in an application-only container with loopback host publishing; configuration and management UI. |
| PostgreSQL product database | Existing shared DEV server; EF Core owns product schema and migrations. |
| n8n | Existing DEV runtime at `https://n8n.nasgard.io`; product workflows directly access the product database; a separate least-privilege credential is the intended runtime boundary. The skeleton has no n8n dependency or configuration. |

Do not provision local PostgreSQL or n8n, including through Compose. The requested Compose setup contains only the application. Hosted n8n internal persistence is entirely external to this repository. [ADR-006](docs/adr/ADR-006-use-existing-shared-dev-infrastructure.md) records this development-environment refinement; [ADR-005](docs/adr/ADR-005-direct-database-integration-and-workflow-owned-delivery.md) retains workflow-owned event processing and delivery.

## Build and run

Use .NET SDK **10.0.401**, or a later patch in that SDK feature band permitted by `global.json`. The project targets `net10.0`; validation used runtime 10.0.12. Open **`Sonrisa.sln` at the repository root** in Rider. It contains one application project, `src/Sonrisa.Web/Sonrisa.Web.csproj`, and one test project, `tests/Sonrisa.Web.Tests`. IDE-specific validation is reported separately in the evidence.

From the repository root:

```bash
dotnet tool restore
npm ci
npm run css:build
dotnet build Sonrisa.sln
dotnet run --project src/Sonrisa.Web
```

Use Node 24 LTS/npm for the small CSS build; validation used Node 24.21.0 and npm 11.19.0. The default Development launch profile serves [Alerts](http://localhost:5180) on loopback. Stop it with Ctrl+C; rerunning the same command restarts it. The application starts without a database connection.

Tailwind CSS and its CLI are pinned to 4.3.3 in the root package/lock files. One stylesheet scans the Razor Pages source; there is no frontend framework or custom JavaScript. Run `npm run css:watch` while editing markup. Generated `src/Sonrisa.Web/wwwroot/css/site.css`, `node_modules`, and caches stay ignored. A clean checkout must run CSS generation before .NET build or publish; the generated CSS is included in published output. Styling is intentionally limited to readable layout, forms, status and focus states.

Npgsql EF 10.0.3 provides PostgreSQL integration. EF Core/Relational/Design 10.0.12 are explicitly aligned; Design is development-only, and the repository-local EF tool is 10.0.12. ASP.NET Core supplies Razor Pages, DI, configuration and health hosting. The test project uses xUnit, the standard .NET test SDK/runner and ASP.NET Core's test host.

EF tools discover `Sonrisa.Web.Data.AppDbContext`. Offline migration generation/script inspection needs provider metadata only; applying a migration requires external connection configuration. The design-time factory reads User Secrets and then environment variables, so environment settings override local secrets. Review the target identity and generated SQL before applying schema changes:

```bash
dotnet ef migrations script --project src/Sonrisa.Web
dotnet ef database update --project src/Sonrisa.Web
```

Do not apply migrations blindly to shared DEV. See the [database contract](docs/06-alert-configuration-contract.md) and [migration verification](evidence/reviews/2026-09-14-alert-configuration-validation.md).

## Database configuration

Use the key **`ConnectionStrings:ProductDatabase`** in project User Secrets for local Development. User Secrets are stored outside the repository and loaded by the Development host. The project already contains a stable `UserSecretsId`. See [Microsoft's User Secrets documentation](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets?view=aspnetcore-10.0).

For Bash, this verified sequence reads the value without echoing it or placing a literal value in shell history:

```bash
read -r -s -p "DEV product database connection: " SONRISA_DEV_CONNECTION
printf '\n'
dotnet user-secrets set "ConnectionStrings:ProductDatabase" "$SONRISA_DEV_CONNECTION" --project src/Sonrisa.Web
unset SONRISA_DEV_CONNECTION
```

The setup mechanism was tested with temporary malformed input and the temporary file was removed. The subsequently supplied DEV connection returned healthy readiness from both the host and container. A read-only host-side audit using the same configuration found superuser access and no PostgreSQL TLS. The user accepted this DEV-only configuration under [ADR-007](docs/adr/ADR-007-accept-current-dev-database-access.md); it is not a production access or transport-security validation. No external secure tunnel is claimed. Obtain its value separately, follow the selected environment configuration and the accepted DEV exception, and keep the value out of prompts, tracked configuration, screenshots, and evidence. User Secrets are a local development convenience, not encrypted production storage.

An externally supplied environment variable named **`ConnectionStrings__ProductDatabase`** is also supported and overrides User Secrets. The application has no `.env` loader; Compose supplies variables for container execution. Restart the local application after changing connection configuration. Missing settings produce a startup warning naming the key; connection failures use sanitized health output. Provider connection diagnostics are suppressed to keep connection details out of logs.

Production application-runtime, n8n product-workflow, and migration access must use distinct least-privilege roles. The current application DEV credential is the explicit exception in [ADR-007](docs/adr/ADR-007-accept-current-dev-database-access.md). Only reviewed EF migrations own schema changes. The original configuration DEV migrations are `20260914125733_InitialAlertConfiguration` and `20260914140740_SharedUserNotificationDestinations`. The latter preserves the first migration’s history and replaces per-alert channels with shared users/alerts configuration. It locks the old product tables during conversion, rejects conflicting complete target sets, and deliberately does not support a lossy downgrade. Stop the old app for this migration window. A third constraint-only migration, `20260914143004_EnforceUnicodeTrimmedConfiguration`, aligns database whitespace checks with .NET trimming. Those three were applied only after target/source/SQL review. Milestone 5 adds `20260914155714_FirstRuntimeSlice`, creating only `source_events` and `notification_deliveries`; it was likewise reviewed and applied to verified `sonrisa_dev`. The SMTP extension adds constraint-only migration `20260914173558_EnableEmailDeliveryStates`, preserving legacy Unsupported rows and permitting active email delivery states. See the [runtime contract](docs/07-runtime-contract.md). The application never migrates at startup. No roles, unrelated schema, or n8n storage are managed here.

## Alert configuration

`MvpOwner:Id` optionally supplies a nonempty UUID. If absent, the application uses the fixed default `d203a533-6bf8-4a21-98a9-291a74ef9f28`. Keep it stable: changing it selects a different ownership scope and does not transfer alerts. Invalid explicit owner configuration fails startup with a key-only error. Every application list/read/edit/status operation filters by the resolved owner, and forms never accept an owner identifier. Future authentication replaces `ICurrentOwner` and maps authenticated subjects to these existing UUIDs.

Open `/settings/notifications` and save one shared email destination, Slack destination, or both before creating an alert. These settings live in the current owner’s `public.users` row and apply to every alert belonging to that owner. The first successful save creates the profile; no identity seed or destination application configuration is required. Email is one bare mailbox; Slack is an opaque uppercase alphanumeric channel ID. FluentValidation 12.1.1 (Apache-2.0) validates inputs through manual calls, without contacting either service. Transport credentials remain in n8n/runtime configuration; no credentials or webhook URLs belong in product settings.

An alert stores a name, enabled state, one earthquake magnitude `>=` finite numeric threshold, and stable ownership referencing the shared notification profile. New alerts default to disabled. Names need not be unique. Numeric input uses a dot decimal separator; thousands separators, NaN and infinities are invalid. The only persisted condition codes are `earthquake`, `magnitude`, `gte`, `number`; additional operators/types and multiple conditions are deferred. The numeric value is native PostgreSQL `double precision`, not a string to guess at runtime.

The management routes are `/alerts`, `/alerts/create`, `/alerts/{id}/edit`, and `/settings/notifications`. There is no delete action or per-alert channel selection. Stale alert or profile edits produce a conflict instead of silently overwriting changes. Each save commits atomically; updating shared destinations affects later evaluation of all the owner’s alerts. Saving/enabling does not invoke a workflow. Manual n8n evaluation reads enabled alerts across owners; selected-ID delivery remains a separate manual action.

## Logs and traces

OpenTelemetry Hosting, ASP.NET Core instrumentation and the OTLP exporter are pinned to 1.18.0. `ILogger` remains the application API. JSON console output is retained locally; SDK/OTLP logs carry trace/span correlation. Console scopes and identifiers are omitted to avoid exposing framework request data. Request traces retain method, route template, status and SDK duration; raw URLs, query values, arbitrary headers, form data and provider diagnostics are excluded. Npgsql/EF spans are deliberately deferred because their SQL/raw-exception defaults need a separate privacy decision. See [ADR-009](docs/adr/ADR-009-use-opentelemetry-logs-and-request-traces.md).

The default service name is `sonrisa-web`; override it with `OTEL_SERVICE_NAME`. Export is disabled unless an explicit common or applicable signal endpoint is supplied. Configure these externally:

| Common key | Meaning |
| --- | --- |
| `OTEL_EXPORTER_OTLP_ENDPOINT` | Explicit HTTP(S) endpoint; no vendor or production endpoint is built in. |
| `OTEL_EXPORTER_OTLP_PROTOCOL` | `grpc` (default) or `http/protobuf`. |
| `OTEL_EXPORTER_OTLP_HEADERS` | Comma-separated `name=value` headers; keep credentials outside Git/logs. |
| `OTEL_EXPORTER_OTLP_TIMEOUT` | Positive integer milliseconds. |

Each setting also supports the corresponding `OTEL_EXPORTER_OTLP_TRACES_*` and `OTEL_EXPORTER_OTLP_LOGS_*` override. A nonempty signal value takes precedence. With HTTP/protobuf, the common endpoint gets `/v1/traces` or `/v1/logs` appended; signal endpoints are used exactly as provided. Endpoint user information, query strings and fragments are rejected. Malformed endpoint/protocol/header/timeout settings disable the affected signal with a key-only warning.

Export uses SDK background batching, with no startup backend probe or request-time flush. Missing/unavailable OTLP does not prevent startup or change health results. There are no metrics, local Collector, dashboards or backend services in this repository. Shared PostgreSQL does not propagate a synchronous trace context between Sonrisa and future n8n workflows.

## Docker testing

The root `Dockerfile` first compiles CSS using a build-only Node 24.21.0 stage, builds with SDK 10.0.401 and runs the published application on ASP.NET 10.0.12 as a non-root user. Install Docker Engine and Compose **2.24 or newer**; validation used Engine 29.8.0 and Compose v5.5.1. The image build needs container registries, npm and NuGet, but no database credentials or host .NET/Node installation. The runtime image contains published application/CSS assets, with no Node or .NET SDK.

`compose.yaml` contains one `web` service. It runs the published application in the Production ASP.NET environment and publishes **127.0.0.1:5180 → container port 8080**. This remains local DEV testing, with no public hosting, authentication, PostgreSQL, or n8n provisioning. Stop the direct Rider/CLI application before using the same port through Docker.

An ignored `.env` has been prepared in this working copy. In a fresh checkout, create it from `.env.example` without replacing an existing file. The prepared file has owner-only permissions; after creating a new copy on Linux, apply `chmod 600 .env` before entering credentials. Leave its connection key empty for liveness testing. For real database testing, edit only the ignored `.env` and supply `ConnectionStrings__ProductDatabase` with the application-runtime connection obtained separately. Use Compose's single-quoted dotenv values to preserve literal dollar signs; escape an embedded single quote as described in [Docker's dotenv syntax](https://docs.docker.com/compose/how-tos/environment-variables/variable-interpolation/#env-file-syntax). Do not put the value in `.env.example`, shell commands, screenshots, prompts, or evidence. The host User Secrets store is not mounted into the container.

From the repository root:

```bash
docker compose config --quiet
docker compose up --build -d
curl -i http://localhost:5180/health/live
curl -i http://localhost:5180/health/ready
```

The optional `.env` may be absent or empty: the shell and liveness still respond, while readiness returns 503. These operations and lifecycle checks were exercised with disposable empty/synthetic environment input and a separate test project, isolated from the user's `.env`. Shared DEV readiness from the container passed after the user supplied runtime configuration; the access/security limitations above are explicitly accepted for this DEV environment. Never save or share expanded Compose configuration or container environment output containing secrets; the quiet configuration check avoids printing resolved values.

```bash
docker compose restart web
docker compose up -d --force-recreate web
docker compose down
```

Use recreation after changing `.env`; restart alone retains the old container environment. `down` removes this Compose project's application container and network. Docker/Compose access can expose runtime environment values, so keep `.env` and access to the local engine private. `.dockerignore` excludes credentials, IDE files, history, and local build output from the image build context. Its allowlist names the current minimal inputs; update it when a later milestone introduces required source files.

## Health checks

```bash
curl -i http://localhost:5180/health/live
curl -i http://localhost:5180/health/ready
```

| Endpoint | Meaning |
| --- | --- |
| `/health/live` | HTTP 200 `Healthy` when the application can serve requests. No database, n8n, or other dependency checks run. |
| `/health/ready` | Checks PostgreSQL connectivity. HTTP 503 `Unhealthy` for missing, malformed, unreachable, or stalled connection configuration; HTTP 200 `Healthy` only after an actual successful connection. |

Readiness uses a five-second health timeout and caps the scoped provider connection timeout at five seconds. The controlled stalled-handshake check completed in approximately 5.15 seconds. Readiness does not prove schema compatibility, product functionality, or n8n workflow readiness. Missing-configuration startup/restart, malformed configuration, refused/stalled local connection checks, and liveness independence were exercised; successful shared DEV readiness was subsequently observed from both host and container. This connectivity result does not validate privilege isolation or secure transport; the current DEV limitations are accepted as documented above.

## Tests and validation

```bash
dotnet test Sonrisa.sln
```

Without explicit PostgreSQL test configuration, relational cases report skips. To opt in, supply `SONRISA_TEST_DATABASE` externally and `SONRISA_TEST_DATABASE_NAME` with the expected product database name. Tests check `current_database()` before writes, require the reviewed schema to exist, use generated fixture owners/IDs, and roll back or remove exactly their own committed records. They never apply migrations, provision a server, truncate tables or reset the database. Do not point them at an unreviewed/shared target merely to eliminate skips.

The suite covers validation, mapping, ownership, revision conflicts, shared profile changes, atomic visibility/rollback, rendered HTTP status forms, antiforgery and telemetry configuration/privacy. Guarded runtime relational tests additionally cover source/intent uniqueness, immutable snapshots, state constraints and restricted deletion. Exact workflow logic and live n8n execution are validated separately. See [actual evidence](evidence/reviews/2026-09-14-alert-configuration-validation.md) for results, browser/container checks and remaining verification limits; a test plan alone is not evidence.

## Repository map and design

| Path | Purpose |
| --- | --- |
| `Sonrisa.sln`, `global.json` | Root solution and SDK selection. |
| `src/Sonrisa.Web/` | Razor management, alert persistence, owner resolver, observability and health checks. |
| `tests/Sonrisa.Web.Tests/` | Unit, HTTP and guarded PostgreSQL checks. |
| `package.json`, `package-lock.json` | Reproducible Tailwind build-only tooling. |
| `.config/dotnet-tools.json` | Local EF CLI tool version. |
| `Dockerfile`, `compose.yaml`, `.dockerignore` | Application-only container build and local testing. |
| `.env.example` | Empty runtime configuration key; real `.env` stays ignored. |
| `docs/` | Product scope, architecture, decisions, specification, and implementation plan. |
| `prompts/` | Substantive user-authored task requests; no internal review or discussion transcript. |
| `evidence/` | Actual review and validation records. |
| `n8n/workflows/` | Reviewed product workflow exports; credential rebinding is documented with the workflow runbook. |
| `infra/` | Reserved; no infrastructure provisioning is needed for this DEV milestone. |

The current milestone uses the public USGS one-hour feed, a reserved deterministic fixture path, n8n-owned typed matching and one explicitly selected Slack or SMTP email intent. All workflows remain inactive. Deduplication guarantees `(source, external_id)` identity, not physical-earthquake identity after USGS preferred-ID changes. The approved SMTP extension adds email through the same selected-ID delivery workflow and preserves the three-workflow structure. Automatic retries, queue draining, attempts/circuits and operational UI remain future work. See the [runtime contract](docs/07-runtime-contract.md) and [initial runtime validation](evidence/reviews/2026-09-14-first-runtime-validation.md) and [SMTP validation](evidence/reviews/2026-09-14-smtp-delivery-validation.md).

Start with the [product brief](docs/00-product-brief.md), [milestones](docs/01-plan.md), [scope](docs/03-scope.md), [assumptions](docs/02-assumptions-and-open-questions.md), [architecture](docs/04-architecture.md), [decision log](docs/decision-log.md), and [working rules](AGENTS.md). The [skeleton specification](docs/superpowers/specs/2026-09-14-application-skeleton-design.md), [implementation plan](docs/superpowers/plans/2026-09-14-application-skeleton.md), and [AI review log](docs/ai-review-log.md) describe milestone 3's bounded work and corrections.
