# Application skeleton specification

User-approved skeleton design following [the milestone request](../../../prompts/010-create-application-skeleton.md). Scope: milestone 3 only. ADR-003's retained UI/persistence choices, ADR-005's processing boundary, and [ADR-006](../../adr/ADR-006-use-existing-shared-dev-infrastructure.md) govern this work. The user authorized specification/plan review followed by implementation without another architecture approval.

## Deliverable and structure

Create root `Sonrisa.sln` and exactly one application project, `src/Sonrisa.Web/Sonrisa.Web.csproj`. Target `net10.0` with nullable reference types and implicit usings. Pin SDK 10.0.401 in root `global.json`, permitting later patches in the same feature band and excluding previews. The installed SDK and .NET 10.0.12 runtime have been observed. .NET 10 is an active LTS release according to [Microsoft's support policy](https://dotnet.microsoft.com/en-us/platform/support/policy).

Use the ASP.NET Core shared framework for Razor Pages, dependency injection, configuration, structured logging, and health-check hosting. Reference `Npgsql.EntityFrameworkCore.PostgreSQL` 10.0.3 for the PostgreSQL provider and `Microsoft.EntityFrameworkCore.Design` 10.0.12 with private assets for design-time tooling; the latter also brings the EF Core 10.0.12 dependency family into resolution. Pin repository-local `dotnet-ef` 10.0.12 in `.config/dotnet-tools.json` so future migration tooling can discover the context now. Verify actual resolved package compatibility during restore/build. Add no other package unless review demonstrates an immediate requirement.

The application contains only `Program.cs`, an empty `Data/AppDbContext.cs`, a small `Health/PostgresHealthCheck.cs` adapter implementing the framework's `IHealthCheck`, and the minimum Razor page/layout files needed to render a plain shell. One heading and a statement that configuration functionality is not yet available are sufficient. No product navigation, sample records, JavaScript, Bootstrap, Privacy page, template branding, controllers, authentication, domain entities, or product behavior.

## Runtime and configuration

Run locally on `http://localhost:5180` using a single Development launch profile. Bind to loopback in documented startup paths; no HTTPS certificate setup is needed for this loopback-only skeleton. The application-only Docker option below is also in scope following prompt 018. `appsettings.json` contains ordinary logging and host configuration only; development logging may be separated into `appsettings.Development.json`. Do not store a connection string, even an example, in any tracked artifact.

Read the key `ConnectionStrings:ProductDatabase` through normal .NET configuration. Support Development User Secrets using a stable project `UserSecretsId` and the equivalent environment-variable key `ConnectionStrings__ProductDatabase`. User Secrets live outside the repository; no `.env` loader or n8n URL setting is needed. Changes to connection configuration are applied by restarting the local process, keeping configuration ownership simple. Never print the connection value, parser exception message, or credential-bearing diagnostic in startup logs, health responses, or evidence. Do not enable EF sensitive-data logging.

Register `AppDbContext` as scoped using Npgsql. Its model has zero entities. Provider configuration can be deferred until context resolution so missing or malformed configuration never prevents the shell and liveness from starting. No `EnsureCreated`, `Migrate`, migration files, database creation, schema writes, or seed operations occur. EF tooling must discover the context without a real connection; connection settings for future database operations use the same external sources, with no custom hardcoded design-time factory.

## Health behavior

Use [ASP.NET Core health-check middleware](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/health-checks?view=aspnetcore-10.0). The PostgreSQL adapter is a small use of that framework, not a separate health system.

| Endpoint/state | Required result |
| --- | --- |
| `/` | HTTP 200 with the minimal server-rendered shell. |
| `/health/live` | HTTP 200 and `Healthy` whenever the process can serve the request. Exclude all dependency checks with the endpoint predicate. |
| `/health/ready`, missing/blank configuration | HTTP 503 and `Unhealthy`; no database attempt. Emit a clear startup warning naming the configuration key, without a value. |
| `/health/ready`, malformed configuration or failed connection | HTTP 503 and `Unhealthy`; sanitize the health description and logs. Liveness stays HTTP 200. |
| `/health/ready`, real successful database connection | HTTP 200 and `Healthy`. This means connectivity only, not schema compatibility or product workflow readiness. |

Tag the PostgreSQL check `ready` and filter the readiness endpoint accordingly. Inside the health adapter, inspect configuration, create an asynchronous DI scope, resolve the context within the guarded operation, and await `Database.CanConnectAsync(cancellationToken)`. Return a fixed non-secret failure description for false results or exceptions; do not attach the raw exception to `HealthCheckResult`. Use a five-second health-check timeout and propagate cancellation. Use the framework's default minimal plaintext response, preventing external exception details from reaching HTTP clients. No n8n check, background probe, retry policy, or remote mutation is added.

## Validation and acceptance

1. Root solution contains exactly the application project and loads in Rider. Record Rider discovery/build diagnostics separately from CLI checks.
2. Restore/build succeed with zero errors and no unexplained warnings; verify resolved package versions. `dotnet tool restore` and `dotnet ef dbcontext list --project src/Sonrisa.Web` discover the empty context without creating a migration or opening a database connection.
3. Exercise the real process over loopback: shell, liveness, and missing-configuration readiness; repeat after stop/restart and rebuild. Check concurrent/repeated liveness while readiness fails.
4. Exercise malformed and unreachable connection configuration through temporary runtime-only test values outside tracked artifacts. Check 503 readiness, 200 liveness, bounded response time, and absence of the test sentinel from captured logs/responses. These are negative-path checks, not real DEV connectivity evidence.
5. When safe credentials are available, validate real PostgreSQL readiness through the application. A read-only PostgreSQL MCP capability check is useful but cannot substitute for application credential/connectivity validation. Do not inspect n8n internal persistence. If application runtime configuration is unavailable, complete all independent work and request that the user configure the connection outside Git at that point.
6. Inspect the diff, ignore behavior, generated content, and absence of product schema/migrations. Review documentation links and distinguish verified setup from pending connectivity. Run task review and final whole-branch review; fix Critical/Important findings before the requested milestone commit.

Use focused process/HTTP smoke checks, without a new test project or testing dependencies. User-approved validation scope overrides blanket test-project scaffolding. No persistent remote operation is needed. A missing runtime credential delays only the positive database check; it must not hide completed independent work or produce a false completion claim.

## Implementation workflow and limits

Work on `feat/application-skeleton` in the existing Rider-associated checkout, preserving the approved uncommitted topology amendment and unrelated `.idea/` files. This avoids moving user work or breaking the IDE path; stage only explicit milestone files. Record the spec and plan with the milestone rather than creating an additional architecture milestone commit. The ten major milestone commits remain unchanged.

The milestone commit is `feat: add application skeleton and local infrastructure`, after applicable verification and review. Do not continue into product schema, alert management, event processing, or n8n workflows.

## Documentation verification sources

Consulted 2026-09-14: [Npgsql EF provider](https://www.npgsql.org/efcore/), [Npgsql package](https://www.nuget.org/packages/Npgsql.EntityFrameworkCore.PostgreSQL/10.0.3), [EF Core design package](https://www.nuget.org/packages/Microsoft.EntityFrameworkCore.Design/10.0.12), and the Microsoft sources above. NuGet public package metadata confirmed the pinned releases. Context7's official ASP.NET documentation confirmed endpoint predicates, readiness tags, and `IHealthCheck`. These references establish documented capability; runtime acceptance requires the checks above.

## Application-only Docker refinement

[Prompt 018](../../../prompts/018-rename-context-and-add-application-docker.md) requests `AppDbContext` naming and Docker testing support. This supersedes the initial specification's omission of containers, without changing ADR-006's external PostgreSQL/n8n ownership. The connection configuration key remains `ConnectionStrings:ProductDatabase`; the rename concerns the context type and file only.

Provide root `Dockerfile`, `compose.yaml`, `.dockerignore`, and `.env.example`, plus an ignored empty `.env` if one does not already exist. Build/publish in a pinned .NET SDK image and run in the matching ASP.NET runtime image as the built-in non-root application user. Do not add OS packages just to probe health. Publish only the application; no SDK, source, Git history, IDE files, prompts, or secret files belong in the runtime image. Restrict the build context to required source/configuration and exclude local overrides/build output.

Compose contains exactly one `web` service, no database/n8n services, volumes, migrations, restart policy, or workflow setup. Map host `127.0.0.1:5180` to container port `8080`; the process must listen on the container interface so port forwarding works. Set the container environment to Production for the published application, without claiming production hosting/security is implemented. Direct Rider development remains unchanged. Do not run direct hosting and Compose on port 5180 simultaneously.

Load the optional ignored `.env` through Compose `env_file` (Compose 2.24 or newer). The example names `ConnectionStrings__ProductDatabase` with an empty value only. Use standard Compose dotenv quoting and verify a synthetic value containing dollar signs is preserved when single-quoted; do not introduce an application dotenv package. No real connection is needed for image build. Never print resolved Compose configuration or container environment containing credentials; use `docker compose config --quiet` for validation. Runtime secret configuration changes require container recreation, not merely restart.

Validate Compose configuration with absent/empty configuration, Docker image build, one-service topology, non-root runtime, loopback publishing, shell 200, liveness 200, readiness 503 without a connection, and stop/restart/recreate behavior. Use only task-owned containers and networks; do not inspect or alter unrelated resources. For every negative test, explicitly disable default dotenv loading with an empty `--env-file` and replace service `env_file` with disposable empty/synthetic input outside the repository; never load or replace a pre-existing user `.env`. Positive PostgreSQL readiness must also be checked through the container endpoint once separately supplied safe runtime configuration is available; host-process or MCP results do not establish container network connectivity. Document only commands exercised and distinguish failed/unavailable checks.

## DEV acceptance clarification

After real host/container readiness passed, the user accepted the current administrative credential and observed lack of PostgreSQL TLS for DEV only. [ADR-007](../../adr/ADR-007-accept-current-dev-database-access.md) records that narrow exception. It removes the current access/transport acceptance blocker without changing the runtime, topology, secret handling, or product scope. Production still requires restricted credentials and verified secure transport; no security validation beyond the recorded facts is claimed.
