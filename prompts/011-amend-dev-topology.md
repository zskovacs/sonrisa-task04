Date: 2026-09-14

Purpose: Amend the development topology to use existing shared services and exclude hosted n8n internal persistence from application scope.

Yes. Record a narrowly scoped architecture amendment / ADR for the development topology and then continue with the skeleton milestone.

Please make the following distinctions explicit.

The previous local-service deployment assumptions are superseded for the current DEV environment:

- Do not provision a local PostgreSQL server.
- Do not provision a local n8n instance.
- The existing DEV n8n instance at `https://n8n.nasgard.io` is the workflow runtime.
- The existing shared DEV PostgreSQL server is the durable product datastore.
- The n8n editor is therefore no longer subject to the previous loopback-only assumption.
- The locally running ASP.NET Core management application may still remain loopback-only during development.

Do NOT treat n8n's own internal persistence as part of this application's database design.

The existing hosted n8n instance owns and manages its own internal persistence. That persistence is external infrastructure and outside this repository's scope.

The application architecture should only concern itself with the product database/schema shared as the explicit integration contract between:

- the ASP.NET Core management application
- the n8n product workflows

Retain the previously accepted decisions unless they directly conflict with this DEV topology change:

- ASP.NET Core Razor Pages for the thin management application
- EF Core owns the product schema and migrations
- n8n directly accesses the product PostgreSQL state where defined by the architecture
- management application and n8n use separate database credentials / roles
- apply least-privilege database permissions
- table/data ownership boundaries remain explicit
- n8n owns runtime event processing and notification workflow execution
- the management application remains intentionally thin

If ADR-003 currently assumes that two databases must be created because one was intended for n8n's internal persistence, supersede that part as well.

Do not create or manage an n8n internal database as part of this project.

The topology should conceptually become:

Developer machine:
- ASP.NET Core / Razor Pages application

Existing shared DEV infrastructure:
- PostgreSQL product database
- n8n workflow runtime at `https://n8n.nasgard.io`

Future n8n workflows and the management application will both interact with the product database according to the documented ownership contract.

Please:

1. create the narrowly scoped ADR
2. update the architecture/documentation sections affected by this topology change
3. update the decision log
4. avoid rewriting unrelated architecture decisions
5. record this as a refinement caused by implementation-environment discovery
6. then continue with the current `feat: add application skeleton and local infrastructure` milestone

Before continuing implementation, show me the resulting topology decision and any remaining architectural conflict.

Do not start product feature implementation.
