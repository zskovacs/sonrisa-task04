Date: 2026-09-14

Purpose: Add a minimal read-only cross-owner product administration view.

We are starting the next milestone:

`feat: add operational admin view`

This prompt is the complete task authorization for this milestone.

This task may run unattended. Do not stop for routine brainstorming/design approval.

You are authorized to:

- reconstruct repository context
- review the intended implementation critically
- select the smallest implementation consistent with accepted architecture
- implement it
- validate it
- document it
- commit it

Stop only if you discover:

- a genuine conflict with an accepted ADR that cannot be resolved without changing product architecture
- a destructive shared-infrastructure operation
- a required secret/credential that is unavailable
- an unexpected database migration or runtime architecture change
- an incomplete/failed previous milestone that makes continuing unsafe

Do not invent additional product requirements.

# Sequential branch rule

This milestone must branch from the CURRENT completed milestone, not from `main`.

Before doing anything:

1. inspect the current Git branch and status
2. verify the working tree is clean except for explicitly known ignored/local files
3. verify recent Git history contains the completed previous milestone:

   `feat: add email notification channel to n8n workflow`

4. do NOT checkout `main`
5. create and checkout:

   `feat/operational-admin-view`

from the current HEAD

If the expected previous milestone commit is not present, or the previous milestone appears incomplete, STOP rather than branching from an older state.

Do not merge branches.

At completion, leave this branch checked out so the next milestone can branch directly from it.

# Prompt-history requirement

This is a standalone material task.

Store this exact final English prompt verbatim under `prompts/` according to `AGENTS.md`.

Use the next normal sequential prompt-history number.

Do not combine this prompt with another milestone prompt.

Do not fabricate manual interactions or approvals that did not occur.

# Reconstruct context first

Before designing or implementing anything, read the repository as the source of truth.

At minimum inspect:

- `AGENTS.md`
- `README.md`
- current plan and scope
- assumptions/open questions
- architecture documentation
- validation strategy
- decision log
- AI review log
- every relevant ADR
- recent milestone specifications
- prompt history
- review evidence
- current Razor Pages implementation
- current ownership model
- current alert-management UI
- current EF Core model and migrations
- actual DEV PostgreSQL schema where useful
- current OpenTelemetry setup
- current authoritative n8n workflow export
- current DEV n8n workflow state where useful
- recent Git history

Use Rider, PostgreSQL MCP, and n8n MCP where useful.

Do not modify n8n runtime behavior in this milestone unless a tiny non-functional correction is required to maintain consistency.

# Architecture that must remain unchanged

Do not redesign the runtime.

The accepted architecture is:

## PostgreSQL

Owns product configuration:

- owners/users
- alerts
- condition configuration
- Slack destination configuration
- Email destination configuration

It does NOT contain:

- runtime event queues
- delivery queues
- retry state
- notification history
- workflow execution state

## n8n

Owns runtime processing:

- source acquisition
- canonical normalization
- workflow-owned technical deduplication
- alert loading
- condition evaluation
- channel routing
- Slack
- Email
- bounded best-effort retries
- execution/runtime technical state

## ASP.NET Core Razor Pages

Owns:

- owner-scoped alert management
- product configuration UI
- read-only admin/product visibility
- application observability

Explicitly do NOT reintroduce:

- SourceEvent runtime persistence
- NotificationDelivery
- processing queues
- retry tables
- delivery-state tables
- dead-letter state
- runtime ASP.NET APIs
- background processing services
- custom workflow engines

Do not change the single-primary-n8n-workflow architecture.

# Important admin boundary

The product requirement includes an admin view.

n8n already provides operational/runtime administration for:

- workflow executions
- node failures
- retries
- integration configuration
- workflow debugging
- credentials
- runtime execution history

Do NOT duplicate that functionality in the Sonrisa application.

The boundary is:

Sonrisa admin:
- product/configuration visibility

n8n:
- runtime/workflow visibility

This distinction should be explicit in implementation and documentation.

A useful architectural statement is:

> The Sonrisa admin area provides cross-owner visibility into product configuration. Runtime workflow operations, executions, failures, retries, and integration diagnostics remain intentionally delegated to n8n instead of being duplicated in the application.

# Milestone objective

Implement the minimum read-only product admin surface necessary to satisfy:

> "We need an admin view too."

Keep it deliberately small.

The admin area should make the current configured state of Sonrisa easy to understand across owners.

It should NOT become a complete back-office application.

# Admin pages

Implement only the following minimal admin pages unless repository structure strongly favors an equivalent layout.

## 1. `/admin`

Purpose:

High-level product/configuration overview.

Show a small set of summary values based only on existing product configuration.

Expected summary approximately:

- total owners/users
- total alerts
- enabled alerts
- disabled alerts
- owners with Slack configured
- owners with Email configured

Use simple summary cards/panels.

Do not add charts when a number communicates the same information.

Also provide simple navigation to:

- Owners
- Alerts

Do not add links for functionality that does not exist.

## 2. `/admin/users`

Purpose:

Read-only cross-owner configuration overview.

Show a concise table containing approximately:

- owner/user display identifier
- number of alerts
- number of enabled alerts
- Slack configured: yes/no
- Email configured: yes/no

Do not display:

- credentials
- tokens
- passwords
- secret configuration

Avoid displaying full destination values unless there is a strong product reason.

A simple Yes/No or configured/not-configured indicator is preferable for this overview.

Do not add:

- create user
- edit user
- delete user
- role management
- permission management
- password management

This page is visibility only.

## 3. `/admin/alerts`

Purpose:

Read-only cross-owner alert overview.

Show approximately:

- owner
- event type
- enabled/disabled state
- configured condition
- available notification channel(s)

For the current MVP, condition rendering may be approximately:

`magnitude >= 5.5`

Use the actual persisted configuration rather than hard-coding display assumptions where practical.

Notification-channel display may show:

- Slack
- Email
- Slack + Email
- None

Do not expose transport credentials.

Do not add alert editing from the admin area.

The existing owner-scoped `/alerts` pages remain the alert configuration/editing UI.

# No separate runtime dashboard

Do NOT create pages such as:

- `/admin/events`
- `/admin/deliveries`
- `/admin/retries`
- `/admin/workflows`
- `/admin/executions`

unless an accepted repository requirement already mandates them.

Sonrisa does not persist that runtime state.

n8n already provides the appropriate operational view.

Do not create runtime database tables merely to populate an admin dashboard.

# Authentication remains out of scope

The MVP still does not implement authentication or authorization.

Do not add:

- ASP.NET Core Identity
- login/logout
- admin roles
- role-based authorization
- OAuth/OIDC
- fake role switching
- demo security theater

The `/admin` pages are MVP product/admin surfaces, not production security boundaries.

Document this honestly.

Future authentication/authorization is deferred.

Do not claim these pages are protected from untrusted users.

# Owner visibility semantics

The existing normal management UI uses the configured MVP owner.

Normal alert pages must remain owner-scoped.

For example:

`/alerts`

must still only expose the current MVP owner's alerts.

The admin area is intentionally different.

Admin pages are cross-owner read-only views.

Therefore:

- `/admin`
- `/admin/users`
- `/admin/alerts`

must query across owners where appropriate.

Do NOT accidentally apply `MvpOwner:Id` filtering to admin pages.

At the same time, do NOT remove ownership filtering from the normal alert-management pages.

Explicitly validate both directions.

# Database expectation

The expected result is:

NO database schema change.

Use the existing product entities/tables.

Do not add:

- admin tables
- dashboard aggregate tables
- cached summary tables
- runtime-state tables

Compute the small summary values directly from existing configuration data.

If implementation unexpectedly requires a migration:

STOP and report why before creating it.

A migration should not be necessary for this milestone.

# Query implementation

Keep queries simple and proportionate.

Prefer:

- EF Core
- direct query projection
- `AsNoTracking()` for read-only admin queries where appropriate
- small page-specific/read-model projections

Do not introduce:

- generic repositories
- CQRS infrastructure
- MediatR
- AutoMapper
- admin service frameworks
- speculative caching

unless these already exist as established project patterns and genuinely simplify the implementation.

For three small read-only pages, prefer boring code over architecture layers.

# UI implementation

Use the existing:

- Razor Pages
- Tailwind CSS
- existing application layout/style conventions

Visual design remains secondary.

The admin pages should be:

- clean
- readable
- consistent with existing alert pages
- reasonably responsive
- accessible
- clearly identified as administration/system views

Suitable UI elements include:

- summary cards
- simple tables
- status badges
- basic navigation

Do not introduce:

- Angular
- React
- Vue
- another JavaScript framework
- UI component frameworks
- charts unless genuinely necessary
- animation
- design-system work
- extensive branding work

Do not redesign existing alert-management pages unless a tiny consistency correction is necessary.

# Navigation

Add only the minimum navigation required to reach the implemented pages.

For example:

- Alerts
- Admin

Within Admin:

- Overview
- Owners
- Alerts

Do not add navigation entries for:

- Events
- Deliveries
- Workflow
- Retry
- Credentials
- User Management

because those are not implemented product features.

# Privacy and sensitive information

Admin visibility should still follow sensible data minimization.

Do not unnecessarily expose:

- full Slack destination IDs
- full email destination values
- secrets
- credentials
- tokens

The purpose is to understand configuration state, not inspect credentials.

For owner overview, configured/not-configured is generally sufficient.

If a destination needs to be displayed for an actual usability reason, avoid logging or tracing it unnecessarily.

# n8n relationship

Do not call n8n APIs from the admin pages.

Do not create an ASP.NET n8n client.

Do not synchronize execution history.

Do not copy workflow errors into PostgreSQL.

Do not create a proxy view over n8n.

Operational runtime investigation remains in n8n.

If useful, documentation may mention where runtime operations are inspected, but the Sonrisa product UI does not need an n8n integration for this milestone.

# Observability

Use the existing application:

- `ILogger`
- OpenTelemetry
- request tracing

Do not add a new observability stack.

Database/admin-query failures should remain diagnosable through existing logging/tracing.

Do not emit unnecessary user destinations as telemetry attributes.

# Testing

Add focused tests where they provide meaningful protection.

At minimum validate:

## Overview

- total owner count
- total alert count
- enabled count
- disabled count
- Slack-configured owner count
- Email-configured owner count

## Owner overview

- multiple owners appear
- alert counts are correct
- enabled-alert counts are correct
- Slack/Email indicators are correct
- sensitive values are not unnecessarily displayed

## Alert overview

- alerts from multiple owners are visible
- owner association is correct
- enabled/disabled state is correct
- condition is rendered correctly
- configured channel labels are correct

## Ownership boundary

This is important.

Validate that:

- normal `/alerts` management remains restricted to the configured MVP owner
- another owner's alert cannot be edited through route/form ID manipulation
- `/admin/alerts` intentionally displays cross-owner alerts
- `/admin/users` intentionally displays cross-owner configuration

## Regression

- existing alert-management tests still pass
- application builds
- health checks still work
- no database migration was introduced

Do not build a large testing infrastructure for these pages.

# Empty-state behavior

Pages should behave sensibly if:

- no owners exist
- no alerts exist
- an owner has no alerts
- no notification destination is configured

Use simple clear empty states.

Do not create fake/demo records merely to make the pages look populated.

# Documentation

Update only affected documentation.

Clearly record:

- admin surface is configuration-focused
- `/admin` provides summary
- `/admin/users` provides read-only cross-owner configuration visibility
- `/admin/alerts` provides read-only cross-owner alert visibility
- normal alert management remains owner-scoped
- runtime workflow visibility remains in n8n
- admin authentication/authorization remains deferred
- admin UI is not a production security boundary

Update roadmap/milestone status.

Do not create a new ADR unless implementation reveals a genuinely new architectural decision.

The product-admin versus n8n-runtime-admin boundary can usually be documented as a clarification of the existing architecture rather than a new major ADR.

# AI review

Critically inspect generated implementation.

Look specifically for:

- accidental `MvpOwner:Id` filter on admin queries
- accidental removal of ownership filtering from normal pages
- user-management scope creep
- authentication being added unnecessarily
- runtime tables reintroduced
- n8n API integration added unnecessarily
- admin-specific database tables
- over-designed dashboards
- unnecessary chart libraries
- unnecessary repository/service abstractions
- full destination values exposed without reason
- secrets appearing in logs or rendered pages
- migration generated accidentally

Correct material issues.

Record meaningful AI-generated issues/corrections according to repository conventions.

Do not manufacture review findings.

# Self-approval rule

This milestone may run unattended.

Do not pause for routine questions such as:

- exact Razor Page class structure
- exact Tailwind layout
- summary-card arrangement
- read-model naming
- table layout
- test class organization

Choose the simplest implementation consistent with existing repository patterns.

When several options are reasonable, prefer:

1. smallest implementation
2. existing project patterns
3. no database/schema change
4. no runtime/n8n change
5. no new abstraction unless necessary

Stop only for:

- a real accepted-architecture conflict
- destructive infrastructure action
- unexpectedly required migration
- unavailable required secret/credential
- failed previous milestone

# Validation

Actually validate:

- `dotnet build`
- relevant automated tests
- application startup
- health endpoints
- `/admin`
- `/admin/users`
- `/admin/alerts`
- normal `/alerts` owner scoping
- cross-owner admin visibility
- multiple-owner data
- summary values
- channel indicators
- empty states where practical
- no secrets displayed
- no schema change
- no n8n runtime change

Do not claim checks not actually performed.

# Commit

After implementation and validation, create:

`feat: add operational admin view`

Do not amend previous milestone commits.

Leave branch:

`feat/operational-admin-view`

checked out after the commit.

Do not merge it.

# Final report

Report concisely:

1. branch created
2. repository/context reviewed
3. `/admin` functionality
4. `/admin/users` functionality
5. `/admin/alerts` functionality
6. owner-scope behavior
7. admin cross-owner behavior
8. database/schema changes, expected none
9. n8n/runtime changes, expected none
10. tests/checks performed
11. privacy/security checks
12. meaningful AI corrections
13. documentation updated
14. commit hash
15. remaining risks for the final validation milestone

Do not continue into the next milestone.
