# Alert configuration database contract

Current contract follows the [amended specification](superpowers/specs/2026-09-14-alert-configuration-design.md) and [ADR-010](adr/ADR-010-store-shared-user-notification-destinations.md). The user replaced per-alert destination allowlists with shared per-user destinations before the milestone commit. [Evidence](../evidence/reviews/2026-09-14-alert-configuration-validation.md) distinguishes prior checks from revised-model validation.

## Ownership and supported configuration

The management application owns configuration writes; future n8n reads configuration directly. EF migrations own schema. Every application alert/profile read and write is scoped to the current owner UUID under ADR-008. The MVP remains single-user with no authentication. A real minimal users row now stores current product settings; it is not an identity/login record or security boundary.

`public.users` contains `id` UUID primary key, optional `email_destination` varchar(254), optional `slack_destination` varchar(80), and a nonempty `revision` UUID. At least one destination is required on a saved profile. Supplied destinations are trimmed/nonblank/bounded. Database trim checks use an explicit Unicode whitespace set matching .NET Trim, independent of PostgreSQL locale. Email is a bare mailbox; Slack is an opaque uppercase alphanumeric channel ID. No password, transport credential, webhook URL or token belongs in this table. The first successful settings save creates the configured owner's row; no fake seeded identity is needed.

`public.alerts` contains required `id`, `owner_id`, `name`, `event_type`, `enabled`, `revision`, `condition_field`, `condition_operator`, `condition_value_type`, and `condition_value`. UUID owner_id references users.id with restricted deletion. Name is trimmed, nonblank and bounded to 120 characters; enabled is boolean. Text codes are exactly `earthquake`, `magnitude`, `gte`, and `number`. The value is finite PostgreSQL double precision (IEEE-754 binary64). No generic string guessing, integer enum ordering, JSON rules or executable expressions.

Only earthquake magnitude greater-than-or-equal is supported. Management validates configuration; n8n later validates and evaluates actual events. Missing/invalid event magnitude must not become zero. New operators/types/fields require an explicit contract extension; multiple conditions and AND/OR groups remain deferred. String/boolean conditions may require a small typed-storage migration when actually needed.

Every alert uses its owner's common email/Slack destinations. There is no alert_channels table in the revised schema and no per-subscription channel selection. One external email sender and one Slack workspace/profile remain the runtime assumption; transport credentials belong to n8n, not product destination records. Additional delivery channels can extend profile storage and projection without changing the single-condition evaluation model.

## Querying from n8n

Read configuration and destinations together through an explicit projection, for example:

```sql
SELECT a.id, a.owner_id, a.name, a.event_type,
       a.condition_field, a.condition_operator,
       a.condition_value_type, a.condition_value,
       u.email_destination, u.slack_destination
FROM public.alerts AS a
JOIN public.users AS u ON u.id = a.owner_id
WHERE a.enabled AND a.event_type = $1
ORDER BY a.id;
```

This returns one row per alert with one or both shared destinations. It is a documented contract, not an implemented workflow/evaluator. The future delivery-intent operation expands nonnull destinations into the selected channel intents. The application reads only its current owner's records; a future product evaluator may intentionally read enabled alerts across owners using its separately scoped credential.

One PostgreSQL statement sees one consistent committed snapshot. Do not combine unrelated configuration reads across n8n nodes and assume they share a snapshot. The later event-evaluation/intent SQL must preserve ADR-005's atomic completion boundary. Sharing durable PostgreSQL state does not propagate a synchronous trace context between application and n8n.

## Writes, concurrency and lifecycle

The notification-settings page gets/creates/updates only the current owner's profile. Creating an alert requires that profile. Both profile and alert writes use one SaveChanges boundary and compare their own submitted revision token against the original value, assigning a fresh UUID on success. Stale writes conflict. Concurrent first profile saves must also produce a safe conflict rather than overwrite settings or expose a unique-key exception.

Changing user destinations affects later evaluation of all that user's alerts. It must not rewrite previously committed delivery-intent destination/content snapshots. Disable affects future eligibility, not already committed intent. No public alert/user delete, runtime event/delivery table, ingestion, evaluator or send exists in this milestone.

The owner index supports management listing; a partial event-type index supports enabled-alert reads. User lookup uses its UUID primary key; no email/Slack index or uniqueness rule is needed. Connectivity readiness does not prove migration/schema compatibility.

## Migration and validation

Preserve the already applied InitialAlertConfiguration migration and add a new migration for shared user settings. Transfer only identical complete normalized destination sets across all alerts of each owner, including channel presence. Stop on missing channels, different values or different channel subsets. The migration locks both old product tables before validation and transfer inside its transaction; stop owned old app hosts during application. Downgrade explicitly fails before any operations because profiles without alerts cannot be represented losslessly in the old schema. Review source/SQL and target before applying expected product-table changes. Never reset shared data, modify unrelated objects or alter n8n storage/roles.

FluentValidation 12.1.1 core supplies application validation through explicit IValidator calls. Built-in rules and platform mailbox parsing replace the prior bespoke validation implementation; a small finite-number predicate preserves the cross-platform numeric contract. PostgreSQL constraints remain complementary protection for direct database writes. No application-configuration allowlist remains.

`MvpOwner:Id` is the only temporary ownership setting. Changing it selects another profile/scope and does not transfer records. Future authentication maps authenticated subjects to existing UUIDs. The mechanism is deliberately not production authentication; before independent multi-user/public use, real authentication/authorization is required. ADR-007's DEV-only database privilege/TLS exception remains unchanged.
