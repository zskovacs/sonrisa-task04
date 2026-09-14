# ADR-008: Use one configured MVP owner without authentication

## Status

**Accepted direction.** Recorded on 2026-09-14 from the user's explicit clarification during milestone 4 brainstorming. The detailed UUID/current-owner design was subsequently approved. ADR-010 adds a user profile for present notification settings without adding authentication, and separately supersedes the destination-allowlisting statements retained below as historical context.

This narrowly supersedes D-001's pre-created user/admin identities, A-07's Development-only identity selector, Q-11/Q-12's identity-selection assumptions, and the corresponding management/access passages in ADR-003, ADR-005, and the active architecture. It does not supersede ADR-002's one-condition limit, runtime ownership, destination allowlisting, DEV topology, or the accepted DEV database exception.

## Context

The original product brief requires users to configure alerts but does not require an authentication mechanism. Demonstrating selectable user/admin identities adds behavior without authenticating anyone. The current MVP has one trusted operator and needs durable ownership that future authentication can reuse.

## Decision

- Operate with one stable configured/default MVP owner. Resolve the owner inside the application; do not offer identity switching or accept an owner identifier from form input.
- Persist ownership on alerts and scope every application alert read/write to the current owner, including list, edit, enable, and disable operations. Child configuration is accessed through that owned alert boundary.
- Keep the current-owner resolver replaceable by a future authenticated-user resolver. Do not introduce a persisted user table solely in anticipation of authentication; the approved design uses UUID ownership. [ADR-010](ADR-010-store-shared-user-notification-destinations.md) now adds a user profile because shared notification destinations are a current requirement.
- Do not implement authentication, authorization infrastructure, ASP.NET Core Identity, login/logout/register, demo roles, or fake admin/user authentication in this MVP.
- Keep local management access under ADR-006/ADR-007. The configured owner is an MVP/development convenience, not a security boundary, and must not be described as production authentication. This applies to the application-only container even though its ASP.NET hosting environment is Production.

“This MVP is single-user and does not implement authentication. The persistence/query model is ownership-aware so authentication can be added later without redesigning alert ownership.”

Authentication is deliberately deferred because it was not part of the requested feature scope. Before independent multi-user or public use, select and implement real authentication and authorization; ownership-aware queries alone do not establish access protection.

## Alternatives and consequences

The pre-created user/admin selector is superseded because it adds simulated identity behavior that the user no longer wants. A full identity system and a speculative user table are unnecessary for the requested MVP. Omitting owner filters because only one owner exists is rejected: it would leave the management boundary unsafe to extend.

Changing the configured owner later selects a different ownership scope; it must not silently transfer existing alerts. Future authentication must map its authenticated subject to the same stable ownership concept. Destination allowlisting remains an independent configuration constraint, not user authentication. Operational visibility remains future work without a simulated admin-role mechanism.

## Validation expectations

After design approval, test all management operations with records for at least two distinct owners, including foreign alert IDs and forged form fields. Verify that owner assignment comes from application context and existing ownership cannot be changed by edits. Document the missing security boundary explicitly. These are planned checks, not completed implementation results.
