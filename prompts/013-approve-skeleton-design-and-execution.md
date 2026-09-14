Date: 2026-09-14

Purpose: Approve the skeleton design and authorize specification, plan review, and implementation within milestone 3.

Approved.

Proceed with this skeleton design and continue through the current milestone.

The proposed structure is intentionally appropriate:

- root-level `Sonrisa.sln`
- one `src/Sonrisa.Web/` ASP.NET Core Razor Pages project
- minimal EF Core/PostgreSQL infrastructure
- external database configuration
- separate liveness and readiness health endpoints

Please keep the following constraints while writing the specification, implementation plan, and then implementing it.

## Solution structure

Keep the application as a single project for now.

Do not introduce separate projects such as:

- Domain
- Application
- Infrastructure
- Contracts
- SharedKernel
- Api

unless implementation reveals a concrete need. No such need is currently demonstrated.

The solution file must remain at repository root and application source under `/src`.

## EF Core

An empty/minimal DbContext is acceptable for establishing the persistence infrastructure.

Do not:

- create product entities
- create product tables
- create migrations
- mutate the shared DEV database merely to prove connectivity

Schema implementation belongs to the next product milestone.

EF Core infrastructure should be ready for future migrations without creating meaningless migrations now.

## Health semantics

Keep liveness and readiness explicitly separate.

Liveness should answer only whether the ASP.NET Core application itself is running.

It must not depend on:

- PostgreSQL
- n8n
- external services

Readiness should include PostgreSQL connectivity.

Expected behavior:

- application can start without a configured database connection
- liveness remains healthy
- readiness clearly reports unhealthy/not-ready when PostgreSQL configuration is missing or connectivity fails

Do not report database readiness unless an actual connection check succeeds.

Use established ASP.NET Core health-check facilities and keep the implementation minimal.

Do not create a custom health framework.

## Missing database configuration

Treat missing database configuration as an expected development state, not as a fatal application-startup error.

However, make the missing configuration visible through:

- readiness health
- clear startup/runtime logging where useful

Do not hide configuration errors.

Do not place credentials in tracked configuration.

If an actual connection string becomes necessary for validation, ask me for it separately at that point.

Never place the provided value in:

- Git
- prompt history
- documentation
- screenshots
- review evidence

## Shared DEV infrastructure

Do not provision or modify infrastructure that already exists.

Specifically:

- no local PostgreSQL
- no Docker Compose for PostgreSQL
- no local n8n
- no n8n product workflows yet
- no modification of unrelated shared DEV resources

The existing n8n environment remains external to the application and the ASP.NET Core project does not need an n8n dependency or configuration value during this milestone.

## Application shell

Keep the Razor Pages shell minimal.

Remove unnecessary template/sample content.

Do not implement:

- Alerts
- Conditions
- Channels
- Admin functionality
- event processing
- workflow integration
- authentication

Do not create fake product pages or sample records.

A minimal application shell sufficient to prove the hosting model is enough.

## Dependencies

Every package added must have a current purpose.

Do not add speculative libraries such as:

- MediatR
- AutoMapper
- FluentValidation
- retry libraries
- CQRS libraries
- frontend frameworks
- JavaScript packages

Use built-in framework functionality where sufficient.

## Specification and implementation plan

You may now write the concise specification and reviewed implementation plan required by the brainstorming workflow.

Keep both scoped strictly to this skeleton milestone.

Do not turn the skeleton specification into a redesign of the product architecture.

The accepted ADRs remain the source of truth.

After completing the specification/plan review required by the brainstorming skill, proceed with implementation without requesting another architecture approval unless:

- a genuine conflict with an accepted ADR is discovered
- a destructive/shared-infrastructure operation would be required
- credentials are required from me
- implementation reveals a material architectural problem

## Validation

Actually validate, where applicable:

- root solution loads correctly
- Rider recognizes the solution/project
- `dotnet build` succeeds
- application starts
- liveness endpoint is healthy
- readiness behavior without database configuration is correct
- PostgreSQL readiness succeeds once safe runtime credentials are available
- no secret is tracked
- no product schema/migration was accidentally created
- no unnecessary generated template content remains

If PostgreSQL credentials are not yet available, do not block all other skeleton work. Complete everything that can be validated independently, then ask for the connection string only when real connectivity validation is the remaining step.

Continue with the milestone:

`feat: add application skeleton and local infrastructure`

Do not proceed into the next product-feature milestone.
