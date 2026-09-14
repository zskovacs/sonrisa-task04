# Sonrisa alerts

A product for user-configured alerts about important world events, with email and Slack notifications, room for future delivery channels, and an admin view.

**Status: planning and repository bootstrap.** Implementation has intentionally not started. The product brief is incomplete; event sources, alert semantics, persistence design, and UI technology remain open. n8n is the selected orchestration layer.

## Start here

- [Product brief](docs/00-product-brief.md), [delivery plan and milestones](docs/01-plan.md), and [proposed scope](docs/03-scope.md)
- [Assumptions and open questions](docs/02-assumptions-and-open-questions.md)
- [Initial architecture](docs/04-architecture.md), [ADRs](docs/adr/), and [decision log](docs/decision-log.md)
- [Validation strategy](docs/05-validation-strategy.md) and [agent working rules](AGENTS.md)
- [Prompt history rules](prompts/README.md) and [AI review log](docs/ai-review-log.md)

## Repository map

| Path | Purpose |
| --- | --- |
| `docs/` | Product planning, architecture, ADRs, validation, and later reflection |
| `docs/diagrams/` | Future standalone diagrams; the initial Mermaid diagram is in the architecture document |
| `prompts/` | Verbatim English prompts for material agent work after bootstrap |
| `evidence/` | Actual screenshots, test output, and review notes when produced |
| `n8n/workflows/` | Reserved for future reviewed workflow exports |
| `src/` | Reserved for future application code |
| `infra/` | Reserved for future necessary infrastructure configuration |

There is no runnable application, infrastructure setup, or executable workflow in this baseline.
