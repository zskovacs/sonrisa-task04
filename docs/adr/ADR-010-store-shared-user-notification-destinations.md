# ADR-010: Store shared user notification destinations in PostgreSQL

> Runtime boundary amendment (2026-09-14): [ADR-012](ADR-012-use-n8n-native-runtime-state.md) supersedes the runtime persistence, queue, recovery and delivery-guarantee provisions described below. Unrelated decisions remain in force; this text preserves the historical design.

## Status

Accepted on 2026-09-14 after the user rejected application-configuration destination allowlists and explicitly selected shared destinations per user. This supersedes the operator-provisioned destination assumption in ADR-005 and the initial milestone 4 specification. Authentication remains deferred under ADR-008.

## Context

The first implementation stored selected destinations in alert_channels but required their selectable values to come from external application configuration. The user requires destination management and its durable source of truth in PostgreSQL. Destinations belong to the user, shared across that user's alerts, rather than to individual subscriptions.

## Decision

Add a minimal users table keyed by the existing opaque ownership UUID. Store optional email_destination and slack_destination plus an opaque revision token. Require at least one destination for a saved profile. No credentials, identity provider, password, login, role, demo identity selector or authentication infrastructure belongs in this table or UI. The persisted profile now solves a current configuration requirement, not speculative future authentication.

Keep alerts.owner_id, with a foreign key to users.id. All alerts use their owner's shared destinations; remove per-alert destination/channel configuration. The current configured-owner resolver remains unchanged and every application alert/profile read/write remains scoped to it. Future authenticated-subject mapping resolves the same UUID. The current mechanism is not a security boundary.

Provide one Razor notification-settings page for the current owner. Save the profile first, then create alerts. The first settings save creates the configured owner's profile; no fake seeded identity is required. Both email and Slack may be configured, with one destination of each type. Profile edits update a revision token and reject stale changes. Alerts retain their independent revision token and required single condition.

Future n8n joins enabled alerts to users on owner_id and reads shared destinations in the same statement snapshot. It owns transport credentials and delivery. Later delivery intent must snapshot destinations; editing a profile must not mutate already committed intent. No runtime workflow or sending is introduced now.

Use a new EF migration rather than rewrite the already applied initial migration. Preserve existing destinations only when every alert of an owner has the identical complete normalized channel set, including channel presence. Abort on different values, different channel subsets, or absent channels. Acquire ACCESS EXCLUSIVE locks on the old product tables inside the migration transaction before validation and transfer, with owned application hosts stopped. Downgrade is explicitly unsupported and fails before any operations because profiles without alerts have no lossless old representation. Do not silently select one, reset shared data or modify unrelated objects. Review source/SQL and verify the target before application.

## Alternatives and consequences

Per-alert editable destinations would preserve the earlier schema with less code, but the user explicitly selected shared user configuration. External allowlists cannot remain the source for editable product data. A separate user-channel catalog or Identity platform adds unnecessary scope for one email and one Slack destination.

A profile change intentionally affects future evaluation of every alert owned by that user. Creating an alert requires an existing valid profile. This adds a small settings surface and a foreign key, while retaining the thin Razor application and explicit PostgreSQL integration contract.
