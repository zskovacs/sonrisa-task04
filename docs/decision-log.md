# Decision log

Record decisions when they are made. A recorded date is not evidence of an earlier discussion or experiment. Keep proposals and unanswered questions in [the question register](02-assumptions-and-open-questions.md); use ADRs for significant architecture decisions.

| ID | Recorded | Decision | Status | Rationale and consequences |
| --- | --- | --- | --- | --- |
| ADR-001 | 2026-09-14 | Use n8n as the primary integration and workflow orchestration layer. | Accepted; supplied architectural direction. | Reuse integration and workflow capabilities so custom code can focus on domain concerns and durable state. See [ADR-001](adr/ADR-001-use-n8n-for-orchestration.md) for alternatives, boundaries, risks, and mitigations. |
