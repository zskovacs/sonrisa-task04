# MVP scope

The [original brief](00-product-brief.md) requires configurable alerts, Slack and email, channel extensibility and an admin view. [ADR-012](adr/ADR-012-use-n8n-native-runtime-state.md) records the approved correction: those requirements do not imply guaranteed delivery, durable queues, delivery history or recovery processors.

## Completed runtime milestones 5–6

One inactive, manual `Sonrisa - Process Alerts - DEV` workflow processes real USGS all-hour data or an explicit synthetic fixture through canonical validation, n8n-native within/across-execution duplicate filtering, enabled configuration reads across owners, deterministic one-condition magnitude matching, destination expansion, and Slack/Email routing. Each transport uses five total native attempts; an exhausted failure is visibly discarded while later notifications continue. Validation covers deterministic and live source paths, native duplicate history, bidirectional transport-failure isolation, existing Slack behavior, and one controlled SMTP4DEV Email submission/capture.

Milestone 5 removed the rejected product runtime entities and applied tables through a reviewed forward EF migration. It preserved old migrations, commits and evidence and archived the three old workflows after replacement validation. The actual tested export stays in Git without credentials or pinned execution data.

During the Email extension, the configuration model, UI, owner resolution and application telemetry remained unchanged. PostgreSQL stores `users` and `alerts` plus EF history. The condition remains earthquake/magnitude/gte/number, with one finite numeric threshold per alert. Destinations are shared per-user fields; runtime processing across owners does not broaden management UI access. Email added only an alert-name query projection/pass-through for user-facing text; it did not change selection or condition evaluation.

## Product administration — milestone 7

The authorized scope is three read-only configuration views: `/admin` summary counts, `/admin/users` cross-owner counts/channel-presence indicators and `/admin/alerts` cross-owner alert state/condition/channels. The existing configured-owner alert/settings management remains unchanged. No destination values, admin mutation actions, role switching or user-management features are added.

The admin area is an unprotected MVP surface, not a production security boundary. Authentication/authorization remains deferred. Runtime executions, failures, retries and integration diagnostics remain in n8n; no runtime dashboard, n8n client, new schema, runtime persistence or observability stack is introduced. Final integrated validation and reflection remain separate milestones.

## Explicit limitations

- The workflow remains inactive; five-minute polling is only future configuration. First live processing considers the bounded current hour, without a cursor or historical import.
- Native deduplication is bounded technical history. Installed n8n 2.38.7 can stop at the history cap rather than automatically roll entries out. Lost/reset/recreated history or changed provider IDs can permit repeats; no globally atomic concurrency claim is made.
- Seen events are not rematched for subsequently created/edited alerts or automatically recovered after downstream failure.
- Slack and Email are best-effort, with bounded retries and possible duplicate/lost messages after ambiguous failures. No permanent or exactly-once notification guarantee exists.
- Malformed events/configuration/destinations fail closed with diagnostics; unrelated valid alerts/channels continue where practical. One exhausted Slack or Email notification must not stop later notifications.
- Authentication, multiple conditions, AND/OR groups, a rules DSL, additional sources, geospatial rules, physical-earthquake alias resolution, runtime APIs, workers and product queues are excluded.

## Preserved topology and history

The application runs locally with Razor Pages/EF, using existing shared DEV PostgreSQL and hosted n8n. Do not provision either service or change shared n8n/global telemetry/credentials without need. Secrets stay in external configuration and n8n credentials; connection-string values never enter Git.

The first runtime at `b9cf171` and SMTP extension at `e1dc337` were genuinely implemented and validated under earlier approvals. Their database state and workflow boundaries are now superseded deliberately, while their historical evidence remains intact. See the [approved correction specification](superpowers/specs/2026-09-14-runtime-simplification-design.md).
