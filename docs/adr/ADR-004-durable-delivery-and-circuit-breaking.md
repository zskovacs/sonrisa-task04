# ADR-004: Coordinate durable delivery, retries, and circuit breaking through the application

> Runtime boundary amendment (2026-09-14): [ADR-012](ADR-012-use-n8n-native-runtime-state.md) supersedes the runtime persistence, queue, recovery and delivery-guarantee provisions described below. Unrelated decisions remain in force; this text preserves the historical design.

## Status

**Rejected allocation; superseded by ADR-005.** Recorded on 2026-09-14 during milestone 2. The user explicitly rejected application-owned circuit breaking and n8n/application HTTP integration. [ADR-005](ADR-005-direct-database-integration-and-workflow-owned-delivery.md) records the replacement boundary. The proposal below is retained to explain what changed; it must not be implemented. Durable intent, automatic retries for uncertain email/Slack delivery, and a circuit breaker remain required. None of this proposal was implemented.

## Context

Matching must not lose a notification because a transport is unavailable. Workflow replay and concurrent delivery runs must not manufacture additional product notification intents. External delivery still has an unavoidable ambiguity: the provider may accept an email or Slack message before its acknowledgement is lost, or n8n may stop after sending but before reporting success.

The user prefers retrying that uncertain send even when a duplicate external message may result. A circuit breaker must slow calls to a failing dependency while preserving pending work. It is different from an attempt limit and does not itself establish successful delivery.

## Proposed decision

### Durable intent and channel boundary

Commit accepted event processing and its newly required notification intents in the same application database transaction. No email or Slack call happens inside that transaction. A uniqueness constraint identifies a delivery by event, alert, and channel; the MVP allows one destination per channel on an alert. Exact event revision behavior is defined with the ingestion contract.

Each intent snapshots the matched condition, event content needed for the message, destination reference/address, and logical transport profile. Later alert edits do not silently change an already committed intent. External credentials stay in n8n; a logical profile is an application-approved route, not a user-supplied credential ID or arbitrary URL.

n8n periodically requests eligible work through the application API, sends through the selected transport, and reports an outcome. The application owns retry eligibility, attempt identity, and breaker state. There is no new message broker, scheduler service, or custom workflow engine.

### Small delivery lifecycle

| State | Meaning and transitions |
| --- | --- |
| Pending | Durable work awaiting its due time and an available transport circuit. An atomic claim moves it to Processing. |
| Processing | One bounded attempt owns a lease and attempt identifier. Success moves to Sent; retryable or uncertain failure returns it to Pending with a later due time; a permanent message/destination error moves it to Failed. |
| Sent | The external transport acknowledged acceptance and the application recorded it. This does not mean an email reached the inbox or that a human read a message. |
| Failed | A permanent message/destination/configuration problem needs correction. The intent and error remain inspectable; an explicit admin requeue after correction reuses the same intent. |

Retain delivery attempt identity, timing, outcome classification, sanitized error, and any provider message reference needed to explain the current state. Attempt outcomes can be successful, known failures, or uncertain; uncertainty need not be another top-level delivery state.

Proposed MVP policy: retry transient and uncertain failures with backoff, capped delay, and circuit gating, without silently expiring or deleting the intent after an arbitrary attempt count. Persistent errors remain visible with attempt count and next eligibility; permanent errors need intervention. Exact timing values are implementation configuration, to be given documented demo defaults before delivery implementation. They are not product SLAs or values inferred from the user's five-send example.

### Claim and acknowledgement discipline

- Atomically claim due work and the relevant transport permission in a short database transaction. Limit the initial design to one active attempt per transport profile, which also bounds half-open probes. Do not hold a database transaction open during the external send.
- Assign each attempt a unique identifier, lease expiry, and ownership token. Retry a claim request with the same request identity so a lost claim response does not allocate unrelated extra work. The response must make an expired or completed claim unusable for a fresh send.
- n8n performs at most one transport call per authorized attempt. Disable automatic retry on the actual email/Slack send node; a new transport attempt must return through application eligibility and circuit checks. Safe event submissions and outcome callbacks may use bounded HTTP retries with stable identities.
- An outcome callback is idempotent for its attempt. Repeating success does not create another intent or reopen a Sent delivery. A delayed failure must not overwrite a later success. Preserve historical attempt outcomes without allowing stale attempts to change a newer lease or breaker probe.
- An expired attempt is uncertain and becomes eligible for a later retry through the same policy. Recovery is evaluated by subsequent claim calls; a separate application background scheduler is unnecessary. App/n8n restarts must not reset pending records or open circuits.
- A late success stops future claims for that delivery and is recorded, but it cannot retract an external send already in flight. Lease expiry and lost acknowledgements therefore retain an explicit duplicate risk.
- Resume or retry delivery through the claim entry point; do not replay saved transport-node input as if it were a new authorized attempt. Provider/node timeout and retry behavior must be checked during workflow implementation.

### Circuit breaker at the actual delivery boundary

Persist one small circuit record per logical transport profile in the application database. Initially the configured Slack workspace/credential profile and email sender profile have independent circuits. A Slack outage must not stop email. Future independent provider accounts need independent profiles, not a single global circuit.

| Circuit state | Delivery behavior |
| --- | --- |
| Closed | Eligible work may receive a send attempt. Track qualifying consecutive dependency failures; reset after a successful attempt. |
| Open | Do not authorize new sends for this profile until its recovery time. Notifications remain Pending; waiting does not count as another send attempt. |
| Half-open | After the recovery time, atomically allow one due delivery as a probe. Success closes the circuit; qualifying failure or an expired uncertain probe reopens it. Other work remains Pending. |

Use transport outcomes reported by n8n to update the circuit once per attempt. Availability failures and timeouts contribute to opening it. Apply provider Retry-After as a minimum delay where present. Shared authentication/configuration failures pause that profile for correction and controlled later probing. A message-specific rejection, such as an invalid recipient, fails that delivery without blocking healthy destinations on the same profile. An unclassified error must be inspectable; do not assume it proves that no external send occurred.

The application protects the send by withholding a claim; it does not perform the transport itself. A circuit breaker around an unrelated application HTTP client would not observe email/Slack calls made by n8n. Nor would a per-execution in-memory breaker retain outage state across separately scheduled workflow runs. Keeping the gate beside durable delivery state avoids those gaps and an additional state store.

Breaker thresholds and cooldown are configurable and deterministic for tests. The complete design must specify demonstrable defaults and how configuration validation rejects invalid values before implementation. Do not select a resilience package merely to name the pattern; the external execution boundary and durable state semantics come first.

### Operations and practical guarantees

Admin visibility includes delivery state, attempts, next retry time, uncertain outcomes, and per-profile circuit state/recovery time. Failed work can be explicitly requeued after its cause is corrected; requeue does not bypass an open circuit. Pending work is retained while a circuit is open. No provider availability, inbox acceptance, or eventual human receipt is guaranteed by these records.

Target durable, at-least-once-oriented delivery attempts after failures and recovery. Internally deduplicate events/intents and make callbacks idempotent; externally accept possible duplicates as the user requested. Do not describe this as exactly-once delivery.

## Alternatives considered

| Alternative | Assessment |
| --- | --- |
| Send directly while matching | Couples database acceptance to external availability and creates a gap between committed matching and transport. Rejected. |
| Let n8n node retries be the only delivery policy | Can repeat a transport call without updating durable attempts or consulting a shared circuit. Rejected as the authoritative policy; retain bounded retries only at safe integration boundaries. |
| Stop every uncertain send for manual inspection | Reduces automatic repeats but conflicts with the user's explicit preference for automatic retries. Rejected. |
| One global circuit for all notification channels | Lets a Slack outage suppress healthy email delivery. Rejected. |
| Add a broker, distributed cache, or separate retry worker | Adds runtime and ownership complexity without a demonstrated requirement. Rejected; use existing durable state and n8n scheduling. |
| Application-owned durable eligibility/circuit gate, n8n-owned transport | Proposed: fits the existing responsibility boundary while coordinating retries across workflow executions and restarts. |

## Risks and mitigations

- This adds durable circuit and attempt state. Keep the profile model small and test transitions/concurrency instead of introducing a general messaging framework.
- Misclassifying a permanent message error as a provider outage can block unrelated work. Specify and test transport-specific error mapping during integration work.
- Retrying uncertain work can deliver duplicates or stale notifications. Expose the event occurrence time and attempt history; freshness/expiry behavior requires a separate explicit product decision.
- Retries without an expiry policy retain work during long outages and need storage/operational visibility. Retention and production capacity are not established by the local demo.
- Lease timeouts must exceed bounded transport-call duration plus outcome-report allowance. Test crashes, lost responses, stale callbacks, and half-open probe recovery with controlled time.

## Verification basis and planned checks

[Microsoft's Circuit Breaker pattern](https://learn.microsoft.com/en-us/azure/architecture/patterns/circuit-breaker), consulted on 2026-09-14, distinguishes retries from temporary blocking, describes Closed/Open/Half-open behavior, and highlights failure-domain separation and concurrency. This design adapts that guidance to the application/n8n boundary; it does not introduce Azure infrastructure.

[n8n retry guidance](https://github.com/n8n-io/n8n-docs/blob/main/docs/integrations/builtin/handle-rate-limits.md) confirms that Retry On Fail repeats a node request. [EF Core transaction guidance](https://github.com/dotnet/entityframework.docs/blob/main/entity-framework/core/saving/transactions.md) documents atomic relational operations. These are capability checks, not workflow or database test results.

Future validation must cover atomic intent creation, duplicate claims/callbacks, independent channel failure, retry backoff, circuit transitions, a single half-open probe under concurrent claims, persisted open state across restarts, permanent versus transient errors, and accepted-but-unacknowledged sends. No such tests exist yet.
