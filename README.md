# Sonrisa alerts

A product for user-configured alerts about important world events, with email and Slack notifications, room for future delivery channels, and an admin view.

**Status: runnable application skeleton, milestone 3.** One minimal Razor Pages shell exists. No product features, product schema, migrations, or executable product workflows exist. CLI build/start/restart and negative readiness checks passed; Rider recognizes the application and its IDE build passed. Actual PostgreSQL readiness passed from both host and container. The user accepted the current administrative credential and observed lack of PostgreSQL TLS for DEV only under [ADR-007](docs/adr/ADR-007-accept-current-dev-database-access.md). Production access requirements remain separate. Application-only Docker build, lifecycle, and negative readiness checks passed. See [validation evidence](evidence/reviews/2026-09-14-skeleton-review.md).

## Development topology

| Component | Location and responsibility |
| --- | --- |
| ASP.NET Core / Razor Pages | Developer machine, directly or in an application-only container with loopback host publishing; future configuration and management UI. |
| PostgreSQL product database | Existing shared DEV server; EF Core owns product schema and future migrations. |
| n8n | Existing DEV runtime at `https://n8n.nasgard.io`; future product workflows directly access the product database using a separate least-privilege credential. The skeleton has no n8n dependency or configuration. |

Do not provision local PostgreSQL or n8n, including through Compose. The requested Compose setup contains only the application. Hosted n8n internal persistence is entirely external to this repository. [ADR-006](docs/adr/ADR-006-use-existing-shared-dev-infrastructure.md) records this development-environment refinement; [ADR-005](docs/adr/ADR-005-direct-database-integration-and-workflow-owned-delivery.md) retains workflow-owned event processing and delivery.

## Build and run

Use .NET SDK **10.0.401**, or a later patch in that SDK feature band permitted by `global.json`. The project targets `net10.0`; validation used runtime 10.0.12. Open **`Sonrisa.sln` at the repository root** in Rider. It contains one application project: `src/Sonrisa.Web/Sonrisa.Web.csproj`. IDE-specific validation is reported separately in the evidence.

From the repository root:

```bash
dotnet tool restore
dotnet build Sonrisa.sln
dotnet run --project src/Sonrisa.Web
```

These commands were exercised. The default Development launch profile serves [the shell](http://localhost:5180) on loopback. Stop it with Ctrl+C; rerunning the same command restarts it. The application starts without a database connection.

The only direct NuGet dependencies are `Npgsql.EntityFrameworkCore.PostgreSQL` 10.0.3 for PostgreSQL/EF integration and private `Microsoft.EntityFrameworkCore.Design` 10.0.12 for EF tooling. The repository-local `dotnet-ef` tool is pinned to 10.0.12. EF Core dependencies resolve to 10.0.12. ASP.NET Core supplies Razor Pages, DI, logging, configuration, and health-check hosting.

The following read-only tooling command discovers the empty context without connecting to PostgreSQL or creating a migration:

```bash
dotnet ef dbcontext list --project src/Sonrisa.Web --no-build
```

It returns `Sonrisa.Web.Data.AppDbContext`. Do not create migrations or run database updates in this milestone.

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

Production application-runtime, n8n product-workflow, and migration access must use distinct least-privilege roles. The current application DEV credential is the explicit exception in [ADR-007](docs/adr/ADR-007-accept-current-dev-database-access.md). Only future EF migrations own schema changes. This skeleton does not provision roles, create tables, or alter shared DEV resources.

## Docker testing

The root `Dockerfile` builds with SDK 10.0.401 and runs the published application on ASP.NET 10.0.12 as a non-root user. Install Docker Engine and Compose **2.24 or newer**; validation used Engine 29.8.0 and Compose v5.5.1. The image build needs access to Microsoft container images and NuGet, but no database credentials or host .NET installation.

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

## Repository map and design

| Path | Purpose |
| --- | --- |
| `Sonrisa.sln`, `global.json` | Root solution and SDK selection. |
| `src/Sonrisa.Web/` | Minimal application shell, empty product context, and health adapter. |
| `.config/dotnet-tools.json` | Local EF CLI tool version. |
| `Dockerfile`, `compose.yaml`, `.dockerignore` | Application-only container build and local testing. |
| `.env.example` | Empty runtime configuration key; real `.env` stays ignored. |
| `docs/` | Product scope, architecture, decisions, specification, and implementation plan. |
| `prompts/` | Substantive user-authored task requests; no internal review or discussion transcript. |
| `evidence/` | Actual review and validation records. |
| `n8n/workflows/` | Reserved for future reviewed product workflow exports. |
| `infra/` | Reserved; no infrastructure provisioning is needed for this DEV milestone. |

Start with the [product brief](docs/00-product-brief.md), [milestones](docs/01-plan.md), [scope](docs/03-scope.md), [assumptions](docs/02-assumptions-and-open-questions.md), [architecture](docs/04-architecture.md), [decision log](docs/decision-log.md), and [working rules](AGENTS.md). The [skeleton specification](docs/superpowers/specs/2026-09-14-application-skeleton-design.md), [implementation plan](docs/superpowers/plans/2026-09-14-application-skeleton.md), and [AI review log](docs/ai-review-log.md) describe this milestone's bounded work and corrections.
