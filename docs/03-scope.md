# MVP scope

The original [brief](00-product-brief.md) establishes alerts, email, Slack, extensibility, and admin visibility. This document defines a deliberately narrow implementation target using the user-confirmed architecture and explicit [planning assumptions](02-assumptions-and-open-questions.md). It does not turn untested assumptions into discovered product requirements.

## Current SMTP extension

After milestone 5 completed at `b9cf171`, the user approved [SMTP email in the existing delivery workflow](superpowers/specs/2026-09-14-smtp-delivery-design.md). New email intents become manually selectable alongside Slack, with the same claim/idempotency boundary. Preserve legacy Unsupported email records. SMTP4DEV is the isolated DEV transport; no fourth workflow, automatic send/retry/drain, circuit or application SMTP code is included. The milestone-5 scope below remains its historical boundary.

## Completed milestone-5 boundary

Milestones 2–4 are complete at `b68e3ad`, `5880801` and `98b000c`. Milestone 5 implemented the [approved simplified first runtime slice](superpowers/specs/2026-09-14-first-runtime-design.md), with acceptance tracked separately in evidence. Scope is USGS ingestion, canonical validation, source/external-ID deduplication, n8n-owned one-condition matching across owners, minimal durable intent, and one explicitly selected Slack send. All three workflows remain inactive. Automatic delivery draining/retries, email execution, attempts/circuits, operational views, authentication and multiple conditions remain outside this milestone. [ADR-011](adr/ADR-011-use-n8n-evaluation-and-replay-safe-runtime-writes.md) records the deliberate simplification.

The completed skeleton followed [prompt 010](../prompts/010-create-application-skeleton.md), [ADR-006](adr/ADR-006-use-existing-shared-dev-infrastructure.md), and its reviewed [specification](superpowers/specs/2026-09-14-application-skeleton-design.md) and [plan](superpowers/plans/2026-09-14-application-skeleton.md). Its no-product-model restriction was specific to milestone 3; the broader product behavior below remains the MVP target.

## Target users and deployment

The MVP is single-user, operated locally with one configured/default owner. Persist stable alert ownership and scope all application alert reads/writes to that owner. No identity selector, editable owner field, user/admin simulation, authentication, or authorization infrastructure is included. [ADR-008](adr/ADR-008-use-single-configured-mvp-owner.md) supersedes the earlier demo-role assumption. Future authentication can replace the owner resolver without redesigning alert ownership; the current mechanism is not a security boundary.

Use a local ASP.NET Core/Razor Pages management application, EF Core product migrations, the existing shared DEV PostgreSQL product database, and the hosted n8n runtime at `https://n8n.nasgard.io`. The application remains loopback-only in development; the hosted n8n editor follows its existing external access controls. Do not create or manage local service instances or n8n internal persistence. n8n reads conditions and writes operational state directly in the product database using a dedicated least-privilege runtime credential, separate from the application and migration roles. The application owns configuration/management and operational reads; all event processing, transport retries, and circuit behavior belong in n8n. No n8n/application HTTP endpoints are needed.

## Included product behavior

- **One initial real event type:** earthquakes from the selected public USGS all-hour GeoJSON feed. The user confirmed that RSS in the example described ingestion generally, not a switch to news.
- **Configurable alerts:** owner, name, enabled state, one supported condition, and a user profile holding shared email and/or Slack destinations. Initially the condition is earthquake magnitude greater than or equal to a user-selected finite numeric threshold. Unsupported fields/operators/values are rejected.
- **Reusable event/rule boundary:** distinguish provider identity from canonical event type; use a common envelope and validated type-specific data. Another provider of the same type reuses its contract. New business meanings may need new logic, not a duplicate processing pipeline.
- **Durable event processing:** unique source/external-event identity and Pending/Evaluated state. Retry unfinished evaluation even when the event is no longer new. Read one joined configuration snapshot, evaluate in n8n, create unique delivery intents, then mark evaluation complete in separate replay-safe steps.
- **Durable delivery:** one intent per event/alert/channel, including an immutable matched-content/destination snapshot. Independent scheduled delivery does not depend on new event arrival. Include attempt/outcome records and visible permanent failures.
- **Slack and email:** Slack proves the first external slice; email completes the required MVP and validates the channel boundary. One configured workspace and one sender profile, with at most one email and one Slack destination per user, shared across that user’s alerts under ADR-010. Live demonstrations use explicitly authorized destinations.
- **Workflow-owned retries and circuit breaker:** retry transient/uncertain sends automatically with backoff, accepting possible external duplicates as the user requested. Keep independent persistent circuits per transport profile, pause calls while Open, and permit one recovery probe. No application-owned gate or retry service.
- **Minimum management:** list, create, edit, enable, and disable own alerts through Razor Pages. Hard deletion and historical rematching are not required.
- **Minimum operational admin:** inspect event processing, related delivery/attempt status, sanitized errors, next retry time, and circuit state. Workflow debugging and delivery recovery remain n8n operations; the admin is observational.
- **Deterministic demonstration:** explicit synthetic source IDs/markers enter the same canonical workflow and persistence boundary. Use authorized isolated destinations; show nonmatches, duplicates, failures, retries, and recovery without waiting for an actual earthquake.

Connection-string values, including examples, must not enter tracked files. Use User Secrets for local application/EF tooling. The requested Docker option runs only the application, with an ignored `.env` and loopback host port; shared PostgreSQL/n8n remain external. The current application DEV access/transport exception is accepted under [ADR-007](adr/ADR-007-accept-current-dev-database-access.md); it does not extend to production. Future n8n product workflows use stored credentials; exports and evidence must not contain their values.

## Why earthquakes first

| Candidate | Assessment for the first slice |
| --- | --- |
| Earthquake | A numeric threshold demonstrates structured normalization, identity, matching, and delivery with little interpretation. A [documented structured feed example](https://earthquake.usgs.gov/earthquakes/feed/v1.0/geojson.php) includes magnitude, occurrence/update times, and IDs; USGS was subsequently selected in milestone 5. |
| RSS/news | Keyword matching is plausible, but its usefulness and text semantics need product decisions; the [RSS specification](https://www.rssboard.org/rss-specification) makes item identifiers optional, so provider identity handling needs care. A second type can validate extension after the MVP. |
| Market movement | Requires choosing reference price, measurement window, and data availability before the threshold has a stable meaning. Defer that extra semantic work. |

The user confirmed earthquakes after comparing these options and later clarified that the RSS workflow example did not change the selected slice.

## First complete slice and observable examples

Create an enabled alert with threshold 5.0 before submitting controlled events:

1. Magnitude 4.9 is accepted/evaluated but creates no delivery.
2. Magnitude 5.0 creates one delivery per configured user channel.
3. Replaying its source/external ID creates neither another event nor another intent.
4. Stopping after event insertion leaves Pending work; a later evaluation run completes it.
5. Rolling back evaluation leaves no partial intent/completion state.
6. A Slack outage retains pending Slack work and opens only its circuit; email remains eligible.
7. After cooldown, one permitted probe checks recovery; successful delivery and its recorded outcome are visible.
8. A lost send acknowledgement leads to a retry and may produce a duplicate external message. The internal intent remains unique.

These are acceptance examples to implement and test, not claims that those results exist. The first end-to-end demonstration consumes configuration persisted through the preceding management milestone; temporary hard-coded or seeded configuration is no longer the planned prerequisite. Final MVP acceptance includes the Razor Pages configuration journey and both delivery channels.

## Explicit planning limitations

- Keep the first valid accepted snapshot per source/external ID. Provider corrections/retractions, including later magnitude threshold crossings, are not re-evaluated in this MVP. This is a deliberate simplifying assumption with a real missed-update limitation, not a discovered user requirement.
- Evaluate rules current at each joined configuration read; edits before a pending event is processed can affect it. Do not retrospectively evaluate completed events after creating/editing an alert.
- The first poll processes the selected provider's bounded feed window, potentially including earlier occurrences. No additional historical archive import or completeness/latency guarantee is included.
- Keep product event/intent identity until an explicit isolated demo reset. Automatic retention/deletion and notification expiry are deferred; long outages can leave old pending notifications.
- The broader architecture plans durable recoverable attempts in milestone 6; milestone 5 provides durable intents and an explicit-ID claim, not guaranteed provider availability, inbox placement, human receipt, or exactly-once external delivery.

The architecture documents configurable demo timing defaults, input-contract constraints to finalize with implementation, and failure classification requirements. Validate these against the actual selected nodes/providers; do not quietly relax recovery semantics to make a demo pass.

## Stretch goals

Only after the complete slice, both channels, and failure validation are working:

- A second source type such as news with a supported text comparison, to exercise extension in code/workflows rather than claim it from a diagram.
- Another provider of the existing earthquake type.
- Additional simple operators or operator convenience with a demonstrated use case.

The second source type is explicitly outside the MVP. Do not build unused generic rules or form-generation infrastructure in anticipation of it.

## Deferred work

Production-grade identity, public hosting, extensive user administration, complex multi-tenant Slack OAuth, SMS/push, geospatial filtering, arbitrary/nested rules, runtime LLM importance classification, microservices, brokers/streaming platforms, Kubernetes, distributed caches, CQRS infrastructure, event sourcing, high availability, cloud infrastructure, and a custom workflow engine are deferred. Reconsider only for a demonstrated requirement and an ADR where architectural.

The application HTTP integration and application-owned event/circuit services were explicitly superseded during design; they are not hidden prerequisites for this MVP. [ADR-005](adr/ADR-005-direct-database-integration-and-workflow-owned-delivery.md) records that correction.

## Scope change rule

Record the observation or requirement, its effect on implementation/validation/time, and simpler alternatives before expanding the scope. Preserve meaningful corrections and rejected proposals in their actual context. A useful future feature is future work until deliberately included in a milestone.
