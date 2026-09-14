# ADR-002: Separate source adapters from canonical events and typed alert conditions

## Status

**Accepted design direction.** Recorded on 2026-09-14 during milestone 2. The user accepted this extension boundary after challenging whether the proposed earthquake slice would hardcode source-specific business logic. No application or workflow has been implemented. Exact integration contracts and persistence representation remain subsequent design work within this milestone.

## Context

**Ownership update:** [ADR-005](ADR-005-direct-database-integration-and-workflow-owned-delivery.md) supersedes this ADR's assignment of runtime event processing to the application. The common envelope, separation of providers from event types, and restricted typed conditions remain the accepted extension direction. Read application-processing references below as the original allocation, not the current implementation instruction.

The first proposed slice uses earthquake events and a user-configured magnitude threshold. The product must remain extensible beyond that example. A provider, an event type, and a notification channel are different concepts: two providers may supply the same event type, while email and Slack deliver the result of matching any supported event type.

Embedding a magnitude check in the shared ingestion or delivery flow would couple the application to the first example. Conversely, unrestricted JSON attributes and arbitrary expressions would require a general rules engine before the product has demonstrated that need.

## Decision

Separate three responsibilities:

1. **Source normalization in n8n.** Map a provider's response to a supported canonical event contract. Preserve source identity and the external event identifier for provenance and deduplication. A second provider for the same event type should reuse the existing domain contract and matching behavior.
2. **Canonical event validation in the application.** Use a common envelope for identity, provenance, event type, occurrence time, and display text, with validated data specific to the event type. The application sets ingestion time. JSON may be the wire representation, but it is not permission to accept arbitrary fields as rule inputs. Concrete field names, required values, and version handling must be specified before implementation.
3. **Typed alert conditions in the application.** An alert targets a supported event type and contains one condition: a supported field, an allowed operator, and a value of the correct type. The first capability is an earthquake magnitude compared with a user-selected numeric threshold using greater-than-or-equal. Field and operator choices must be validated server-side. Shared processing invokes the rule boundary; it does not contain provider-specific branches or a hardcoded product-wide magnitude threshold.

Reuse event acceptance, deduplication, alert evaluation coordination, durable notification creation, and channel delivery across event types. Implement comparison capabilities only when a supported type needs them. Do not add nested expressions, AND/OR groups, arbitrary JSONPath, executable user expressions, runtime plugins, or a dynamic form-generation framework for this slice.

For the first slice, importance means matching an enabled user-configured alert, rather than a global importance score or runtime LLM classification. This is the MVP interpretation; it does not claim that a numeric threshold expresses every future user's needs.

## Alternatives considered

| Approach | Assessment |
| --- | --- |
| Earthquake-specific processing throughout the application | Small initially, but couples orchestration and delivery to the first domain. Rejected as the shared architecture. Earthquake-specific validation still has a legitimate home at the event-type boundary. |
| Entirely separate event pipelines and rule implementations for each provider | Duplicates durable state handling and reliability behavior. Rejected: provider differences belong in normalization when the canonical meaning is the same. |
| Common envelope with unrestricted attribute queries and a generic expression language | Flexible configuration, but weakens the supported domain contract and introduces type checking, expression safety, and editor complexity. Rejected for the MVP. |
| Common envelope, validated type-specific data, and a small typed condition boundary | Selected. Shares the stable processing flow while making domain differences explicit. It permits incremental extension without promising that new business semantics require no code. |

A separate class hierarchy for every provider is unnecessary. Internally, strongly typed event payloads or equivalent validated representations may implement the selected contract; the exact serialization and database mapping are not decided by this ADR.

## Consequences and extension examples

| Change | Expected work | Reused behavior |
| --- | --- | --- |
| Add another earthquake provider | Add normalization and contract tests, including identifier semantics. | Earthquake validation, existing condition evaluation, and notification processing. |
| Add news events later | Define validated news data and supported fields; add a text comparison capability and a suitable form if required. | Ingestion coordination, persistence boundaries, deduplication infrastructure, and delivery lifecycle. |
| Add a rule with new meaning, such as movement over a time window | Specify the reference values and time semantics, then implement and test the necessary domain behavior. | The event and notification boundaries where the new behavior fits their contracts. |
| Add a notification channel | Extend destination validation and n8n transport handling. | Event normalization and alert matching. |

These are design walkthroughs, not implemented extensions or test results. Supporting multiple providers does not automatically deduplicate reports of the same real-world occurrence across providers; that is a separate product rule.

## Risks and mitigations

- A field/operator registry could grow into an accidental schema framework. Keep it limited to supported event contracts and comparisons; avoid speculative operators and automatic UI generation.
- Unvalidated attributes could bypass the typed boundary. Validate payloads and condition values on the server; reject unsupported field/operator combinations.
- New domains may require different business semantics. Permit small, explicit domain extensions rather than forcing every future requirement into generic JSON expressions.
- A single implemented event type cannot prove arbitrary extensibility. Review a second-provider and second-type change path now, then add concrete extension tests when that capability enters scope.

## Related records

- [Architecture milestone request](../../prompts/002-design-mvp-architecture.md)
- [Initial orchestration decision](ADR-001-use-n8n-for-orchestration.md)
- [User challenge and design correction](../ai-review-log.md)
- [Remaining design questions](../02-assumptions-and-open-questions.md)
