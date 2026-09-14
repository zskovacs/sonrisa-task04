# Sonrisa alerts

A product for user-configured alerts about important world events, with email and Slack notifications, room for future delivery channels, and an admin view.

**Status: architecture and product design (milestone 2).** Implementation has intentionally not started. The accepted direction is a local demo using n8n, one ASP.NET Core application with Razor Pages, and separate application/n8n PostgreSQL databases. EF Core migrations manage application persistence. n8n owns event processing and delivery through direct product-database access; the application manages conditions and operational views. See the scope for explicit demo assumptions and remaining integration choices.

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
| `prompts/` | Original repository initialization request and verbatim prompts that advance product implementation |
| `evidence/` | Actual screenshots, test output, and review notes when produced |
| `n8n/workflows/` | Reserved for future reviewed workflow exports |
| `src/` | Reserved for future application code |
| `infra/` | Reserved for future necessary infrastructure configuration |

There is no runnable application, infrastructure setup, or executable workflow in this baseline.
