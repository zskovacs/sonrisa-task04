# ADR-007: Accept the current DEV database access exception

## Status

**Accepted.** Recorded on 2026-09-14 after the user clarified the intended DEV-only administrative access in [prompt 025](../../prompts/025-accept-current-dev-database-access.md). This narrowly supersedes ADR-006's requirement to establish restricted runtime access and secure transport before accepting the current skeleton in DEV. It does not supersede production requirements or the product ownership contract.

## Context

Actual application readiness succeeded from both the local host and the application container. A separate read-only host-session audit using the supplied configuration reported PostgreSQL superuser, database-creation, and role-creation privileges, with PostgreSQL TLS disabled for that session. These observations initially prevented milestone acceptance under ADR-006. The user explicitly accepted them for the current local DEV usage and stated that production will use a normal database user. See [the recorded checks](../../evidence/reviews/2026-09-14-skeleton-review.md).

## Decision

- Accept the currently supplied administrative database credential and observed lack of PostgreSQL TLS as limitations of the current DEV application configuration. They no longer block the skeleton milestone or its commit.
- Keep the application accessible only on host loopback, directly or through its application-only Compose service. The term local DEV describes the development usage; PostgreSQL and n8n remain the existing external shared DEV services under ADR-006. No new service, database, role, or network setup is authorized.
- Keep real configuration outside Git: User Secrets for direct development and an ignored `.env` for Compose. Do not encode a privileged credential, disabled TLS, or an environment-specific fallback into source or tracked configuration.
- Before production use, provide a restricted application-runtime user, separate migration and workflow credentials, least-privilege permissions, and verified secure database transport. The DEV exception does not establish production readiness. It follows the deployment's purpose, not the `ASPNETCORE_ENVIRONMENT` string: the published container uses Production hosting mode while it is still part of this local DEV topology.
- Preserve EF ownership of product schema/migrations and all table/data ownership boundaries. The application stays thin; n8n owns runtime processing and delivery. Administrative capability is not authorization for the application or agent to create schema, mutate unrelated data, or reconfigure shared services. This decision does not grant future n8n workflows administrative access.

## Consequences and alternatives

Requiring immediate DEV role/transport changes was rejected by the user as unnecessary for this environment. The accepted alternative keeps the existing DEV configuration and completes the verified skeleton. The administrative credential has broader capability than the application needs, and the audited PostgreSQL session itself was not encrypted with TLS; neither fact is reclassified as a passing security check. No claim is made about an external encrypted tunnel. Both limitations remain visible for replacement before production use.

No runtime code or infrastructure change implements this exception. It records the user's deployment-specific acceptance and retains the original validation facts. Revisit before production deployment or moving beyond the current accepted DEV usage. No product feature milestone is authorized by this decision.
