# ADR-003: Use one server-rendered application with isolated PostgreSQL databases

## Status

**Accepted.** Recorded on 2026-09-14 during milestone 2. The user approved the proposed UI/runtime direction and explicitly requested separate application and n8n databases, EF Core migrations for the application, and no connection strings in the repository. This is a design decision; no projects, packages, database setup, migrations, or secrets have been created.

**DEV topology amendment:** [ADR-006](ADR-006-use-existing-shared-dev-infrastructure.md) supersedes the local-service topology and the requirement for this repository to create/manage two databases, including n8n internal persistence. Use the existing shared DEV services; only product persistence is in application scope. The original topology below is retained as history. Razor Pages, EF Core product migrations, and external secrets remain accepted.

## Context

**Milestone 2 ownership amendment (historical):** [ADR-005](ADR-005-direct-database-integration-and-workflow-owned-delivery.md) replaced the HTTP integration and exclusive application access to the product database below. At that point, Razor Pages, two separate databases, EF Core migrations, and external secret configuration were retained, and n8n received a separate product-access credential in the design. ADR-006 subsequently superseded the two-database provisioning and internal-persistence scope; the other retained choices remain current.

The confirmed MVP is a local demo operated by one trusted operator with pre-created user and admin roles. Its proposed management interactions are alert forms and lists; the operational surface shows events and notification outcomes. No complex client-side interaction or independent frontend delivery requirement has been demonstrated.

n8n remains responsible for integrations and orchestration under ADR-001. The application needs durable product state and supported HTTP boundaries for n8n, regardless of the UI technology. Keeping that state separate from n8n's own workflow, credential, and execution data avoids mixing ownership and schema lifecycles.

## Decision

Use **one ASP.NET Core application** with Razor Pages for management/admin screens, HTTP endpoints for n8n integrations, and shared application services. Razor Pages call those services directly; they do not call back into the application's HTTP API. Domain validation, ownership checks, transactions, and delivery policy belong behind both entry points.

Use **PostgreSQL with EF Core and migrations** for application persistence. The expected local topology is the ASP.NET Core application, n8n, and one PostgreSQL instance. Create two separate databases on that instance when implementation begins:

- The application database holds authoritative product state. Application EF Core migrations manage only this database.
- The n8n database holds n8n's own durable state. n8n manages its schema and upgrades.

Use separate database roles/credentials. Neither component receives the other database's credentials or permissions. n8n interacts with product state through the application HTTP API, not direct product-table reads or writes. Sharing the PostgreSQL process is a local operational simplification, not shared schema ownership or high availability.

Connection strings must remain outside Git, including code, configuration, migration helpers, documentation examples, prompts, logs, workflow exports, and evidence. Do not include even example connection-string values in tracked files. Tracked documentation may name configuration keys and explain how values are supplied.

- Local application development: use .NET User Secrets outside the project tree.
- Future Docker setup: supply runtime configuration through an ignored local `.env` file; tracked container configuration references external values without embedding them.
- Future EF tooling: resolve the application connection from the same external configuration sources. No hardcoded design-time connection or fallback is permitted.
- Fail clearly when required configuration is missing, without logging secret values. Review generated artifacts and logs before accepting them.

User Secrets is a development convenience, not an encrypted production secret store. Production secret management and deployment remain outside the local-demo milestone.

## UI scope and alternatives

The management surface supports alert creation/editing, enabling/disabling, a supported condition, and allowed delivery destination selection. The operational surface shows recent events, related notification deliveries, attempt state, and failures. Exact recovery actions remain part of the delivery design. Detailed workflow inspection stays in n8n; the product admin page does not replicate its editor or execution debugger.

| Option | Benefits | Costs and disposition |
| --- | --- | --- |
| Razor Pages | One application/runtime, form handling and validation, direct access to shared services. | Some future interactions may need JavaScript. Selected because current interactions are forms and operational lists. |
| Native JavaScript/TypeScript with HTTP API | Small initial client and an explicit API boundary. | Requires assembling form/state/error behavior; TypeScript adds a build step. Not selected because there is no demonstrated client interaction that benefits from the additional boundary. |
| Angular | Structured client application with mature forms and HTTP tooling. | Separate application/toolchain and a larger implementation surface. Not selected for this MVP; revisit if interaction complexity justifies it. |

The UI decision is based on the current interaction model, not developer familiarity. External HTTP APIs remain necessary and independently testable with Razor Pages.

## Persistence alternatives

SQLite could reduce the local runtime count and would be a credible option for a smaller isolated demo. PostgreSQL is selected to use one relational database engine for both components while preserving separate databases, and to exercise the application's intended transaction and concurrent-delivery boundaries against the same engine used at runtime. This is not a claim that the expected load exceeds SQLite's capabilities.

Separate PostgreSQL instances would give stronger process-level isolation but add local operational work without a demonstrated need. A shared application/n8n database or direct n8n access to product tables would weaken the application contract and schema ownership; neither is selected.

## Consequences and risks

- The local environment has three major components and two independent database lifecycles. Startup, persistence, and upgrades must be verified during implementation.
- Sharing the database instance means its failure affects both components. High availability and independent database scaling are deferred.
- EF Core does not replace database constraints or define retry semantics automatically. Transactions, uniqueness, and delivery ownership require explicit contracts and integration tests.
- A local demo identity mechanism does not establish protection for independent real users. Public hosting requires a separate access-control decision.
- Ignored files and User Secrets reduce accidental commits but do not prove that generated code, logs, or exported configuration are secret-free.

## Verification basis

Capability-level documentation was consulted on 2026-09-14:

- [ASP.NET Core dependency injection](https://github.com/dotnet/aspnetcore.docs/blob/main/aspnetcore/fundamentals/dependency-injection.md) supports direct service consumption from page models.
- [ASP.NET Core application fundamentals](https://github.com/dotnet/aspnetcore.docs/blob/main/aspnetcore/fundamentals/index.md) documents the application/service model used by the combined UI and HTTP boundary.
- [n8n supported databases](https://github.com/n8n-io/n8n-docs/blob/main/docs/deploy/host-n8n/configure-n8n/choose-n8ns-database.md) documents PostgreSQL support and n8n's own persistent state.
- [ASP.NET Core development secrets](https://github.com/dotnet/aspnetcore.docs/blob/main/aspnetcore/security/app-secrets.md) describes storage outside the project tree and its development-only security limitations.

These checks establish documented capabilities, not a working integration or a selected version matrix. Choose compatible supported .NET, EF Core, PostgreSQL provider, PostgreSQL, and n8n versions before the executable skeleton.

## Revisit conditions

Revisit the UI when demonstrated client-side interactions justify a separate client. Revisit topology and identity before a multi-user pilot or public hosting. Revisit database isolation when operational requirements require independent failure domains. Preserve the application/domain boundary in each case.
