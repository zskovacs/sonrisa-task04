# Application Skeleton Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development to implement this plan task-by-task. Steps use checkbox (`- [x]`) syntax for tracking.

**Goal:** Deliver a minimal runnable Razor Pages skeleton with safe external PostgreSQL configuration and independently testable liveness/readiness.

**Architecture:** One local management application, one empty product DbContext, and one framework health-check adapter. Existing shared DEV PostgreSQL and n8n remain external under ADR-006; the application does not communicate with n8n.

**Tech Stack:** .NET SDK 10.0.401, ASP.NET Core/net10.0, Npgsql EF provider 10.0.3, EF design tooling 10.0.12, local dotnet-ef 10.0.12.

**Spec:** [Application skeleton specification](../specs/2026-09-14-application-skeleton-design.md).

## Global constraints

- Root `Sonrisa.sln`; exactly one `src/Sonrisa.Web/Sonrisa.Web.csproj`.
- No entities, tables, migrations, seeds, schema writes, local PostgreSQL/n8n servers, product workflows, or product features.
- Configuration key `ConnectionStrings:ProductDatabase`; no connection values in tracked files, including test fixtures, documentation, or examples.
- Missing database settings are nonfatal. `/health/live` excludes dependencies. `/health/ready` checks actual PostgreSQL connectivity, returning 503 until successful.
- Five-second readiness timeout, cancellation propagation, fixed sanitized failures, and no raw exception/connection logging.
- No separate test project or speculative package. Use real-process smoke checks and CLI/IDE verification.
- Preserve unrelated `.idea/` files and the existing approved documentation amendment. Use the current feature branch/check-out for Rider continuity; do not move existing work.
- User approval in prompt 013 permits reviewed execution without another architecture gate. Ask for credentials only after independent validation is complete.

## Task 1: Runnable shell, persistence registration, and health behavior

**Owner:** `sp_integration_debugger`, because DI, EF configuration, health cancellation, and secret-safe failure handling interact. The controller performs independent review preparation and Rider/context inspection alongside implementation. The worker owns only the new application/tool files below and the removal of `src/.gitkeep`; other edits belong to the controller.

**Files:** create `Sonrisa.sln`, `global.json`, `.config/dotnet-tools.json`, `src/Sonrisa.Web/Sonrisa.Web.csproj`, `Program.cs`, `Data/AppDbContext.cs`, `Health/PostgresHealthCheck.cs`, `Pages/Index.cshtml`, minimum Razor layout/import files if used, `Properties/launchSettings.json`, and minimal `appsettings*.json`. Do not keep unused scaffold files. No committed smoke-test connection fixtures.

**Interfaces:** Produce `/`, `/health/live`, `/health/ready`, and `Sonrisa.Web.Data.AppDbContext`. Use the external configuration key above and a stable `UserSecretsId`. The controller consumes these endpoints and paths for acceptance checks and setup documentation.

- [x] Establish the pre-implementation baseline: `test -f Sonrisa.sln` fails because no skeleton exists. Define the HTTP expectations from the spec before implementation; use a temporary smoke harness outside the repository to check the real process after it exists. Do not claim this baseline is a unit-test red/green cycle.
- [x] Scaffold minimally or create the equivalent Web SDK project. Use `dotnet new sln --name Sonrisa --format sln` and `dotnet sln Sonrisa.sln add src/Sonrisa.Web/Sonrisa.Web.csproj`. Target `net10.0`; add the two specified packages and private design-tool assets. Pin the SDK and dotnet-ef manifest as specified.
- [x] Create the empty context:

```csharp
namespace Sonrisa.Web.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options)
    : DbContext(options)
{
}
```

Include the EF namespace. Register it with deferred options using `UseNpgsql` and the externally supplied connection. Never call schema-creation methods. Store the captured runtime connection/configuration consistently so restart applies changes.

- [x] Implement `PostgresHealthCheck` using `IHealthCheck`, `IServiceScopeFactory`, and the captured configuration. Check blank configuration before creating the scope. Resolve the context inside `try`, await `CanConnectAsync(cancellationToken)`, and return Healthy only for true. For false, malformed settings, or failure, return Unhealthy with a constant safe description and no exception attachment. Honor cancellation and the registered timeout.
- [x] Configure the host and health endpoints using framework facilities:

```csharp
builder.Services.AddRazorPages();
builder.Services.AddHealthChecks().AddCheck<PostgresHealthCheck>(
    "postgresql", tags: ["ready"], timeout: TimeSpan.FromSeconds(5));
// Register AppDbContext and the adapter's dependencies before building.
app.MapRazorPages();
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});
```

Complete the normal builder/build/run lifecycle, namespace imports, and missing-configuration warning. Preserve the default plaintext response. Do not introduce controllers or authorization middleware for nonexistent product features.

- [x] Add one plain server-rendered shell and the Development launch profile at `http://localhost:5180`. Remove template Privacy, third-party assets, scripts, sample routes, and unused error/layout content. Keep ordinary environment-aware logging. No tracked connection value or n8n setting.
- [x] Run `dotnet build Sonrisa.sln`, `dotnet tool restore`, `dotnet ef dbcontext list --project src/Sonrisa.Web`, and `dotnet list src/Sonrisa.Web package --include-transitive`. Confirm one context and compatible 10.x dependencies without schema/database operations.
- [x] Run real-process smoke checks with an empty runtime connection override: GET `/` is 200, `/health/live` is 200/Healthy, `/health/ready` is 503/Unhealthy. Confirm the missing-key warning has no value. Test malformed and closed-loopback-port connection settings generated only in temporary runtime configuration outside Git; verify nonfatal startup, failed readiness, healthy liveness, bounded timeout, and sentinel absence from logs/responses. Stop owned processes afterward.
- [x] Inspect generated files and task diff. Return changed files, commands/results, and concerns. Do not stage/commit or access shared DEV; the controller owns those steps. Obtain `sp_task_reviewer` review and fix Critical/Important findings before task completion.

## Task 2: IDE verification, developer documentation, evidence, and milestone handoff

**Owner:** Controller for environment verification, documentation, evidence, and final integration. Custom reviewers independently assess the resulting change. No second implementation agent is necessary for read-only checks and reporting.

**Files:** update `README.md`, `.gitignore` only if relevant probe paths expose gaps, active milestone/status sections in `AGENTS.md` and `docs/01-plan.md` through `docs/05-validation-strategy.md`; append actual evidence under `evidence/reviews/` and meaningful discoveries to `docs/ai-review-log.md`. Preserve earlier evidence/history. Maintain sequential `prompts/` records for all qualifying delegated prompts.

**Interfaces:** Consume Task 1's solution, context, configuration key, and HTTP endpoints. Produce verified setup instructions and evidence, with positive database connectivity either tested or explicitly pending user configuration.

- [x] Use Rider MCP to discover/load the root solution. Query `get_solution_projects`, build through `build_solution_start`/`build_solution_state`, inspect project problems/dependencies, and verify the root solution contains `Sonrisa.Web`. If the IDE is not exposing this checkout or cannot load the new solution, report the exact limitation; do not claim CLI success as Rider validation.
- [x] Independently repeat CLI build and local start/stop/restart checks using the documented Development launch profile and a non-Development run with empty external database override. Confirm local-only binding, shell rendering, dependency-free liveness, and failed readiness. Exercise setup commands before documenting them.
- [x] Check `.gitignore` using synthetic paths only: nested bin/obj, Rider personal state, environment/secret files, and runtime outputs must be ignored; root `.sln`, project/configuration, tool manifest, and intentional evidence remain trackable. Inspect generated content for unnecessary assets and confirm the context model is empty with no migrations or schema calls.
- [x] Inspect only safe PostgreSQL MCP connectivity metadata if useful (read-only, no table data/internal n8n inspection). This is not proof that the application has a working runtime connection. Do not extract credentials from MCP or IDE data sources.
- [x] Write README commands actually exercised: open `Sonrisa.sln`, `dotnet tool restore`, `dotnet build Sonrisa.sln`, `dotnet run --project src/Sonrisa.Web`, health URLs, and the external key/User Secrets mechanism. Explain missing configuration and the loopback application/shared DEV distinction. Never include example connection values. Mark any interactive credential-dependent step not yet exercised.
- [x] Obtain task review and a preliminary whole-change review, and finish all independent checks before requesting credentials. If runtime configuration is unavailable, ask the user to set `ConnectionStrings:ProductDatabase` through their project User Secrets outside Git and confirm when done; never ask for the value in a recorded prompt. Leave the code reviewable while this last validation is pending. Final milestone acceptance follows the actual database result or an explicitly accepted connectivity limitation.
- [x] Once safe runtime configuration is available, restart the application, check `/health/ready` returns 200/Healthy, and retain only sanitized outcome evidence. Do not write tables, migrations, roles, or workflows. Update evidence/status after the actual result.
- [x] Review complete and exact staged diffs. Run focused Markdown links/whitespace and secret-pattern checks. Use `sp_final_branch_reviewer` for the whole milestone, address Critical/Important findings, and commit only explicit milestone paths as `feat: add application skeleton and local infrastructure`. Do not amend prior commits or modify global Git identity. Report the resulting hash and all validation limits.

## Review status

The controller self-reviewed spec coverage, endpoint/configuration names, file ownership, task dependencies, secret handling, and milestone scope. Task 2 consumes the same solution, paths, health endpoints, and configuration key produced by Task 1. Neither task permits product implementation or remote mutation. Independent pre-implementation review is required before Task 1 begins; actual verdicts and progress belong in the review evidence/working ledger rather than being presumed here.

## Task 3: Requested context rename and application-only Docker testing

This bounded addition follows [prompt 018](../../../prompts/018-rename-context-and-add-application-docker.md) and the specification's Docker refinement. It supersedes the initial no-container implementation scope only for the application. No additional architecture approval is needed: the user expressly requested this local testing option, and external-service ownership is unchanged.

**Owner:** `sp_integration_debugger` for the context rename and Docker build/runtime integration. Controller owns active documentation, evidence, and final integration. Review this addition before implementing it; obtain focused task review afterward.

**Files:** worker owns `src/Sonrisa.Web/Data/AppDbContext.cs` (rename old file), context references in `Program.cs` and `Health/PostgresHealthCheck.cs`, root `Dockerfile`, `compose.yaml`, `.dockerignore`, `.env.example`, and creation of an ignored empty `.env` only when absent. Preserve existing `.env` contents without reading or printing secrets. No changes to historical prompts or validation evidence. Controller updates README, affected active architecture/scope/plan sections, decision log, and acceptance evidence; retain historical ADR decisions with a dated refinement note.

- [x] Review the refinement and this task plan before code edits.
- [x] Prefer Rider semantic rename of `ProductDbContext` to `AppDbContext`; align the filename and verify DI and health references. Keep the external connection key unchanged. Build and run EF context discovery.
- [x] Implement a multi-stage root Dockerfile using `mcr.microsoft.com/dotnet/sdk:10.0.401` and `mcr.microsoft.com/dotnet/aspnet:10.0.12`, publish Release without apphost, run under `APP_UID`, expose 8080, and start `Sonrisa.Web.dll`. Copy only required project inputs; keep credentials out of all stages and build arguments.
- [x] Add one-service Compose with optional `.env`, Production environment, and loopback 5180:8080 binding. Add blank configuration-key example and restricted `.dockerignore`. No additional packages, services, volumes, scripts, or automatic schema operation.
- [x] For all negative tests, disable default dotenv loading with an empty `--env-file` and override the service `env_file` with disposable empty/synthetic input outside the repository. Never load, print, or replace a pre-existing user `.env`; use an isolated copy of the Compose definition if needed to substitute only the environment-file path while retaining the repository build context. Validate config quietly, build image, start on a distinct task-owned Compose project, check shell/liveness/readiness without a real connection, verify non-root execution and port binding, restart and recreate, and clean up only owned containers/network. Verify dotenv single-quoted dollar-containing synthetic value is delivered unchanged without printing it. Verify secret/IDE/build artifacts are excluded from build context and runtime image.
- [x] Once separately supplied safe runtime configuration is available, validate HTTP 200/Healthy at the container `/health/ready` endpoint. Host-process or MCP success does not establish container networking. Until then record this exact check as pending; perform no real shared DEV connection in the negative tests.
- [x] Return sanitized commands/results and limitations. Obtain task review, resolve material findings, update developer instructions/evidence, repeat final whole-change review, then complete milestone acceptance when safe real connectivity is available or its limitation is accepted. Do not commit or continue to product work independently.

## Final runtime-configuration acceptance

The user supplied runtime configuration separately in the ignored `.env`. Real host and container readiness returned 200/Healthy. A read-only host session audit using that configuration reported superuser access and no PostgreSQL TLS. The user then explicitly accepted these limitations for current DEV usage; [ADR-007](../../adr/ADR-007-accept-current-dev-database-access.md) records that narrow exception. It removes the milestone blocker without changing shared infrastructure or application code. Production retains restricted runtime access and verified secure transport requirements. Complete the final documentation/staged-diff review and the requested milestone commit; do not continue into product work.
