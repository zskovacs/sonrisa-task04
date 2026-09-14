# ADR-009: Use OpenTelemetry logs and request traces with optional OTLP export

## Status

**Accepted design.** The user approved the milestone 4 design on 2026-09-14. Implementation and verification are recorded separately in milestone evidence.

## Context

The thin ASP.NET Core application needs diagnosable configuration/database failures and consistent vendor-neutral observability alongside future n8n workflows. The skeleton uses ILogger/console logging and suppresses provider diagnostics to avoid exposing connection details. No observability backend or project telemetry standard exists.

## Decision

Retain ILogger as the application API and useful console logging. Add the OpenTelemetry .NET SDK for logs and incoming ASP.NET Core request traces, default service identity `sonrisa-web`, and natural request trace/span correlation. Use stable compatible hosting, ASP.NET Core instrumentation and OTLP exporter packages. Configure export externally with standard OTEL settings where supported; no hardcoded production/vendor endpoint.

OTLP export is optional. Without an explicit endpoint the app still starts and emits console logs; with an unavailable backend, SDK background batching must not become an application request/startup dependency. Invalid export settings disable affected export with sanitized configuration diagnostics. Telemetry never participates in readiness; PostgreSQL remains its sole dependency. No metrics, Collector, local observability platform, dashboards, or new backend infrastructure is required.

Record safe operation/error classifications, route templates, status and duration. Do not emit request bodies, arbitrary query/form values, destinations, owner identifiers, connection strings, tokens, credentials or raw database exceptions. Keep provider diagnostics suppressed and preserve useful sanitized application failure logs. Do not add custom spans without a demonstrated diagnostic need.

Defer PostgreSQL/Npgsql spans in this milestone. A stable compatible Npgsql tracing package exists, but its current activities include SQL and raw exception information independently of log filters. Adding a custom redaction/export subsystem is not justified for this first foundation. Request traces and correlated safe database-failure logs provide the initial investigation path. Revisit database spans when their privacy contract can be kept simple and verified.

Sharing PostgreSQL does not propagate trace context between application and n8n. Future workflow telemetry uses its own service identity and execution traces; no artificial synchronous distributed trace is implied.

## Alternatives and consequences

Default framework logs alone are simpler but do not establish the requested vendor-neutral export/correlation foundation. A vendor-specific SDK or local observability stack adds unnecessary operational dependencies. Broad automatic instrumentation would emit more information than this milestone needs and increase privacy review costs. The selected scope requires maintaining package compatibility, explicit exporter configuration and privacy tests, while keeping backend choice external.

## Verification basis

Official [SDK hosting](https://www.nuget.org/packages/OpenTelemetry.Extensions.Hosting/1.18.0), [ASP.NET Core instrumentation](https://www.nuget.org/packages/OpenTelemetry.Instrumentation.AspNetCore/1.18.0), and [OTLP exporter](https://www.nuget.org/packages/OpenTelemetry.Exporter.OpenTelemetryProtocol/1.18.0) packages expose .NET 10 support. [Npgsql 10.0.3 source](https://raw.githubusercontent.com/npgsql/npgsql/v10.0.3/src/Npgsql/NpgsqlActivitySource.cs) explains the deferred SQL/exception emissions. These are documentation/source checks, not runtime telemetry evidence.

Acceptance requires actual no-endpoint/unavailable-endpoint startup checks, correlated request logs/traces, exporter configuration tests and sensitive-sentinel inspection. No external backend availability guarantee or database-span validation is claimed.
