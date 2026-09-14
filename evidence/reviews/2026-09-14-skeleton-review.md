# Application skeleton validation

Date: 2026-09-14

## Scope and revision

Milestone 3 work on `feat/application-skeleton`, based on architecture commit `b68e3ad` and the accepted [DEV topology amendment](../../docs/adr/ADR-006-use-existing-shared-dev-infrastructure.md). The approved scope is recorded in [prompt 013](../../prompts/013-approve-skeleton-design-and-execution.md), with the [specification](../../docs/superpowers/specs/2026-09-14-application-skeleton-design.md) and [implementation plan](../../docs/superpowers/plans/2026-09-14-application-skeleton.md). This record is updated as checks actually occur; pending checks are not passing results.

## Specification and plan review

- The independent reviewer received [prompt 014](../../prompts/014-review-skeleton-specification-and-plan.md) and returned `APPROVE_WITH_MINOR_NOTES` with no Critical/Important finding.
- Its Minor note distinguished the preliminary review before credential handoff from final acceptance after a real database result or an explicit accepted limitation. The plan wording was corrected before implementation.
- Baseline inspection confirmed `Sonrisa.sln` did not exist. This is an absent-implementation baseline, not a unit-test red/green result.
- The controller retained the current checkout on a new feature branch for the pending approved documents and Rider path, selected real-process smoke checks without a test project as requested, and retained spec/plan artifacts for the requested milestone commit. These process choices do not expand architecture or claim separate pre-implementation commits.

## Environment and isolation checks

- `rtk proxy dotnet --list-sdks` reported SDK 10.0.401; runtime inventory included Microsoft.NETCore.App and Microsoft.AspNetCore.App 10.0.12. Public NuGet metadata confirmed Npgsql EF provider 10.0.3 and EF design/dotnet-ef 10.0.12 availability.
- A read-only PostgreSQL MCP query, `SELECT 1 AS connectivity_probe, current_setting('server_version') AS server_version;`, returned `1` and PostgreSQL `18.6 (Debian 18.6-1.pgdg13+2)`. It did not read table data, inspect n8n internal storage, or mutate a remote system. This establishes MCP connectivity only; it does not establish product-database identity, application credentials, grants, or application readiness.
- `git check-ignore --no-index -q` probes passed for 12 ignored paths and eight trackable paths. Cases covered nested bin/obj/logs/artifacts, environment files, local settings, Rider personal state, root solution, project/configuration files, the tool manifest, and intentional evidence. No probe files were created and no ignore-rule change was needed.

## Runtime implementation and task review

- The implementer received [prompt 015](../../prompts/015-implement-skeleton-runtime.md), created the root solution, SDK/tool pins, and one minimal Razor Pages application, and removed `src/.gitkeep`. There are 11 new application/tool files. No product entities, migrations, schema operations, frontend assets, or n8n settings were introduced.
- Implementer checks passed: restore/build with zero errors and warnings; local EF tool restore; one-context discovery; compatible package resolution; solution listing; and real-process missing/malformed/refused/stalled connection cases. Development launch-profile validation also passed. The report is summarized here without connection values or raw provider logs.
- A stalled local TCP listener exposed that health cancellation alone did not bound the handshake. After capping only the scoped probe's provider connection timeout, readiness returned HTTP 503 in approximately 5.15 seconds; shell and liveness stayed healthy. All temporary listeners/processes were stopped. Test sentinels were absent from checked HTTP bodies and captured logs. This was a local failure simulation, not a PostgreSQL server provisioned for development.
- The independent `sp_task_reviewer` received [prompt 016](../../prompts/016-review-skeleton-runtime.md) and returned `APPROVE` for both specification compliance and task quality, with no Critical, Important, or Minor findings. It confirmed scoped disposal and timeout/configuration isolation, reviewed reported checks, and independently checked solution membership, whitespace, and absence of schema calls/connection literals.

## Independent controller acceptance checks

- `rtk dotnet build Sonrisa.sln`: passed, zero errors/warnings. RTK's summary counted two build projects; the explicit `dotnet sln Sonrisa.sln list` output confirmed exactly one application project, `src/Sonrisa.Web/Sonrisa.Web.csproj`.
- `dotnet tool restore`: passed for dotnet-ef 10.0.12. `dotnet ef dbcontext list --project src/Sonrisa.Web --no-build`: returned exactly `Sonrisa.Web.Data.ProductDbContext` without database configuration.
- `dotnet list src/Sonrisa.Web package --include-transitive`: direct Npgsql EF provider 10.0.3 and EF Design 10.0.12; EF Core/Abstractions/Analyzers/Relational 10.0.12 and Npgsql 10.0.3. No conflicting dependency family was observed.
- A temporary Python HTTP harness exercised the Development launch profile, stopped/restarted Development, and ran Production with an explicit loopback URL. Each process returned the shell at HTTP 200, three HTTP 200 `Healthy` liveness results, and three HTTP 503 `Unhealthy` readiness results with an empty runtime connection override. Each emitted the missing-key warning and listened on localhost. Owned processes stopped cleanly.
- The exact README `dotnet run --project src/Sonrisa.Web` and both `curl -i` health commands were then exercised with an empty runtime connection override: HTTP 200 `Healthy` for liveness and HTTP 503 `Unhealthy` for readiness. The local process was stopped afterward.
- The README's Bash `read`/`dotnet user-secrets set` sequence was exercised with a dynamically generated malformed test value. A Development process loaded it from the external User Secrets store, returned 200 liveness and 503 readiness, omitted the missing-key warning, and did not expose the test value in command output or logs. The check removed only the unchanged temporary secret file it created. This verifies the mechanism without storing real credentials or connection fixtures in Git.
- The project User Secrets file and current process environment had no real product connection when checked before this test. This is not a claim about other stores or later user changes.

## Rider validation status

Rider MCP initially reported `Miscellaneous Files` and `&`, without `Sonrisa.Web`. The installed Rider launcher accepted the root `Sonrisa.sln` path and exited successfully, but subsequent project discovery still showed that earlier view. An IDE build request did not return a response before its waiting cell was stopped; no IDE build result is claimed. After the user asked to retry following the solution-open request, another project-discovery request also did not respond before its waiting cell was stopped. At that point Rider solution/build validation remained unverified; the CLI results above were not substitutes for it. No local IDE configuration was edited or staged.

The latest retry of `get_solution_projects` also returned no response before its waiting cell was stopped. This does not establish whether the solution is loaded in the UI.

## Preliminary whole-change review

The independent `sp_final_branch_reviewer` received [prompt 017](../../prompts/017-review-skeleton-milestone-before-connectivity.md) and returned `APPROVE` with no Critical, Important, or Minor findings. It inspected the complete review patch and current implementation, documentation, evidence, prompt history, and Git scope. It relied on recorded runtime results rather than rerunning those checks. The approval covers the reviewable change; final milestone acceptance still requires real application PostgreSQL readiness and Rider validation, or explicitly accepted limitations.

The controller's whitespace checks passed for the working and staged diffs; the staging area remains empty. Unrelated Rider files remain outside the milestone change.

## Acceptance checkpoint before Docker implementation

At this checkpoint, successful application readiness against real shared DEV PostgreSQL, the requested Docker addition and its reviews, final acceptance, and the milestone commit remained pending. Later sections record completed Docker implementation and validation. No product database or shared n8n configuration was mutated, and no n8n product workflow was created.

## Rider connection recovered and requested scope refinement

After the user's latest retry/update, `get_solution_projects` returned `Sonrisa.Web` (along with IDE project/folder entries). `build_solution_start` and `build_solution_state` completed IDE build session `21c89a01-b1f1-4db4-a1ce-74debda9198d` successfully, with no build problems. `get_project_problems` at Warning severity returned zero entries. `get_project_dependencies` for `Sonrisa.Web` returned the two expected direct package names. These checks establish actual Rider recognition/build of the root solution before the requested context rename.

[Prompt 018](../../prompts/018-rename-context-and-add-application-docker.md) adds `AppDbContext` naming and application-only Docker testing. The accepted shared DEV database/n8n ownership remains unchanged. Docker Engine client/server 29.8.0 and Compose v5.5.1 were observed; this is availability evidence, not yet an image build or runtime result. The specification/plan refinement was submitted for independent review in [prompt 019](../../prompts/019-review-docker-refinement-plan.md).

## Docker plan review

The independent reviewer returned `REQUEST_CHANGES` on [prompt 019](../../prompts/019-review-docker-refinement-plan.md), with two Important findings: isolate negative Compose tests from any pre-existing `.env`, and require a conditional positive readiness check through the container rather than relying on host/MCP connectivity. The specification and Task 3 were corrected before code changes. The scoped [prompt 020](../../prompts/020-review-docker-plan-corrections.md) re-review returned `APPROVE`, both findings addressed, with no remaining Critical, Important, or Minor findings. Implementation was then dispatched in [prompt 021](../../prompts/021-implement-context-rename-and-docker.md).

## Context rename and Docker implementation

- The implementer applied Rider semantic rename to `AppDbContext`, aligned the filename, and updated DI and health references. The configuration key remained unchanged. CLI build passed with zero warnings/errors, and EF tooling returned `Sonrisa.Web.Data.AppDbContext`.
- The controller ran an IDE build after the rename: session `9627daeb-91eb-43f2-9163-b06ced60fb8b` completed successfully with no problems.
- Root `Dockerfile`, `compose.yaml`, `.dockerignore`, and `.env.example` were added. The worker created a zero-byte ignored `.env` only because it was absent. No negative test loaded it.
- All worker Compose checks used a disposable definition differing only in absolute repository build context and isolated service environment-file path, with an explicitly empty CLI `--env-file`. No user `.env` or real database setting was loaded. Quiet configuration validation passed for absent, empty, and synthetic service environment files; `config --services` returned only `web`.
- Image build passed using SDK 10.0.401 and ASP.NET runtime 10.0.12. The first attempt exposed local `bin/obj` leaking into the context and failed with `NETSDK1064`; the allowlist was corrected. A real scratch build-context export then contained exactly seven required inputs. Runtime checks confirmed no SDK, source, `.env`, Git, IDE, prompt history, or Development appsettings in the published image.
- Empty-configuration container checks returned shell HTTP 200, `/health/live` HTTP 200 `Healthy`, and `/health/ready` HTTP 503 `Unhealthy`. Runtime UID was 1654; the published port was `127.0.0.1:5180`. Stop/start, restart, and recreate succeeded after waiting for the HTTP listener. A disposable single-quoted dollar-containing dotenv value reached the container unchanged and disappeared after recreation with empty input; no value was printed in evidence.
- The worker removed its own test container, network, image, and scratch directory. It did not create any local database/n8n service or access shared DEV resources. Its full sanitized task report was supplied to independent review in [prompt 022](../../prompts/022-review-context-rename-and-docker.md).

## Docker task review and controller checks

The independent task reviewer returned `APPROVE` for specification compliance and code quality on prompt 022, with no Critical, Important, or Minor findings. It reviewed the task diff/current files and recorded validation without rerunning those tests.

The controller independently exercised README operations on a distinct disposable Compose project, using an explicitly empty CLI dotenv input and a service env-file path outside the repository. The definition otherwise retained the repository's service settings and build context. `config --quiet`, `config --services` (only `web`), `up --build -d`, `restart web`, `up -d --force-recreate web`, and `down --rmi local` passed. Initial start, restart, and recreation each returned shell 200, liveness 200/Healthy, and readiness 503/Unhealthy. Both documented `curl -i http://localhost:5180/health/...` commands were exercised successfully. Only the controller's test resources were removed. This is negative-path container evidence; no real database configuration was used.

The controller checked all 47 milestone paths (31 Markdown files): local links, whitespace, selected secret/connection patterns, generated/schema directory exclusions, prompt format, unchanged historical tracked prompts, empty staging, `.env` ignored and `.env.example` trackable, root solution, and the renamed context filename all passed. These are focused checks alongside file review, not a guarantee from ignore rules alone. The prepared empty `.env` was given owner-only mode 0600 with `chmod 600 .env`; no connection value was read or written.

## Whole-change review after Docker integration

The independent final branch reviewer returned `APPROVE` for the current reviewable handoff on [prompt 023](../../prompts/023-review-milestone-with-docker.md), with no Critical, Important, or Minor findings. It inspected the full patch/current files and relied on recorded CLI, Rider, Docker, and HTTP results. During review, an older pending-work paragraph was labeled as a historical checkpoint so it no longer implied the completed Docker work was outstanding.

The implementation and independent checks are complete for the requested skeleton/Docker behavior. Final milestone acceptance remains pending successful real shared DEV PostgreSQL readiness from the host application and the container, or an explicitly accepted connectivity limitation. No real application connection has been supplied, no product schema or workflow has been created, and no milestone commit has been made. The staging area is empty; unrelated `.idea` work remains outside the milestone files.

## Real PostgreSQL readiness and access/security finding

After the user confirmed that runtime configuration was ready, the controller loaded the Compose-resolved connection into process memory without printing or storing it. An explicit empty CLI dotenv input prevented Compose self-configuration from the user's file; the service `env_file` supplied the requested runtime setting. A distinct task-owned Compose project built and started the application using that setting. Its shell, liveness, and readiness each returned HTTP 200; both health bodies were exactly `Healthy`.

The controller then started the unchanged host application with the same setting supplied only through its child environment, Development mode, and a temporary loopback port. Its shell, liveness, and readiness also returned HTTP 200 with `Healthy` health bodies. The full connection value was absent from captured host and container application logs. Captured configuration/logs remained in memory; only sanitized outcomes were retained. The host process and task-owned Docker container/network/image were removed. No schema operation or n8n action occurred. A fresh `dotnet build Sonrisa.sln` also passed with zero errors/warnings.

To check the documented access/transport boundary, a disposable host-side .NET probe reused the already-resolved Npgsql dependency and the same runtime configuration. It opened a connection and issued only this metadata query:

```sql
SELECT s.ssl, r.rolsuper, r.rolcreatedb, r.rolcreaterole
FROM pg_stat_ssl s
JOIN pg_roles r ON r.rolname = current_user
WHERE s.pid = pg_backend_pid();
```

The probe emitted only four booleans: TLS `false`, superuser `true`, database-creation privilege `true`, and role-creation privilege `true`. It did not emit a database/role/host name, connection value, or provider exception message. The temporary probe project was outside the repository and removed afterward. This establishes that the audited PostgreSQL session itself did not use TLS; it does not establish whether an external secure tunnel exists. The superuser privilege directly conflicts with the intended least-privilege application-runtime role.

Positive host and container connectivity are now verified. Final milestone acceptance remains blocked on a restricted application-runtime credential and verified secure transport consistent with ADR-006. No shared role, server setting, schema, or data was modified to address this finding. No milestone commit has been created.

The independent final reviewer assessed these new findings in [prompt 024](../../prompts/024-review-real-connectivity-and-access-findings.md) and returned `APPROVE` for the reviewable handoff, with no documentation findings. Its prior implementation approval stands. It confirmed that final milestone acceptance and the commit remain blocked by the observed superuser credential and unverified secure transport. The controller checked all 48 milestone paths against the actual connection value in memory and selected secret patterns: no match was found. Reviewed runtime files were unchanged; local links and whitespace passed, `.env` remained ignored with mode 0600, and staging remained empty.

## User-accepted DEV exception and milestone closure

The user clarified in [prompt 025](../../prompts/025-accept-current-dev-database-access.md) that the administrative accounts are for DEV only and production will use a normal database user. [ADR-007](../../docs/adr/ADR-007-accept-current-dev-database-access.md) records acceptance of the currently observed administrative credential and lack of PostgreSQL TLS for DEV. This supersedes the acceptance blocker in the preceding checkpoint; it does not change the audit results or establish production privilege/transport security. No runtime or shared-infrastructure change followed this clarification.

All skeleton runtime acceptance checks are satisfied: root solution/single project, actual Rider discovery/build, CLI build and EF context discovery, minimal Razor shell, independent liveness, negative readiness paths, actual host and container PostgreSQL readiness, Docker lifecycle and non-root/loopback checks, and external secret configuration. No product model, migration, workflow, or local PostgreSQL/n8n service was created. The remaining work is final review/staged-file verification and the requested milestone commit; production access/transport setup belongs before any production use.

The final acceptance reviewer returned `APPROVE` in [prompt 026](../../prompts/026-review-accepted-dev-exception-and-final-staging.md), with no Critical, Important, or Minor findings, and explicitly confirmed that the milestone commit may proceed under ADR-007. The reviewed staged diff contained exactly 51 milestone paths, matched the review package, and passed whitespace checks. The controller's fresh CLI build passed with zero errors/warnings; runtime files were unchanged from the actual host/container readiness checks. Historical prompts were preserved and no actual connection value or flagged secret pattern appeared in the staged file set. The real `.env` and unrelated `.idea` files were excluded. These results establish accepted DEV skeleton completion; the commit's identity is recorded by Git rather than a self-referential hash in this file.
