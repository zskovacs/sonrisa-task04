using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Trace;
using Xunit;

namespace Sonrisa.Web.Tests;

[CollectionDefinition("Telemetry environment", DisableParallelization = true)]
public sealed class TelemetryEnvironmentCollection;

[Collection("Telemetry environment")]
public sealed class ObservabilityTests
{
    [Fact]
    public async Task Request_trace_and_safe_failure_log_share_trace_id_without_sensitive_request_data()
    {
        var capture = new Capture();
        using var environment = new TelemetryEnvironment();
        using var factory = new TelemetryFactory(capture);
        using var client = factory.CreateClient();
        var pathSentinel = Guid.NewGuid().ToString("D");
        const string querySentinel = "query-secret-sentinel";
        const string userAgentSentinel = "agent-secret-sentinel";
        client.DefaultRequestHeaders.UserAgent.ParseAdd(userAgentSentinel);

        using (var response = await client.GetAsync($"/alerts/{pathSentinel}/edit?probe={querySentinel}"))
            Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        using (var missing = await client.GetAsync($"/missing/{pathSentinel}?probe={querySentinel}"))
            Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        Assert.True(SpinWait.SpinUntil(() => capture.Spans.Count >= 2, TimeSpan.FromSeconds(2)));
        var spans = capture.Spans.ToArray();
        var logs = capture.Logs.ToArray();
        Assert.Contains(spans, span => span.Name == "GET /alerts/{id:guid}/edit");
        Assert.Contains(spans, span => span.Name == "HTTP GET" && span.StatusCode == 404);
        var routed = spans.Single(span => span.Name == "GET /alerts/{id:guid}/edit");
        Assert.Contains(logs, log => log.TraceId == routed.TraceId && log.TraceId != default
            && log.SpanId == routed.SpanId && log.SpanId != default);
        var all = string.Join(" ", spans.Select(s => s.ToString()).Concat(logs.Select(l => l.ToString())));
        Assert.DoesNotContain(pathSentinel, all, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(querySentinel, all, StringComparison.Ordinal);
        Assert.DoesNotContain(userAgentSentinel, all, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Missing_export_endpoint_keeps_liveness_available_and_still_captures_sdk_signals()
    {
        var capture = new Capture();
        using var environment = new TelemetryEnvironment();
        using var factory = new TelemetryFactory(capture);
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/health/live");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(capture.Spans, span => span.Name == "GET /health/live");
    }

    [Fact]
    public async Task Common_http_endpoint_exports_logs_and_traces_with_signal_paths_and_headers()
    {
        await using var receiver = await Receiver.StartAsync();
        using var environment = new TelemetryEnvironment(new Dictionary<string, string?>
        {
            ["OTEL_EXPORTER_OTLP_ENDPOINT"] = receiver.Address + "base",
            ["OTEL_EXPORTER_OTLP_PROTOCOL"] = "http/protobuf",
            ["OTEL_EXPORTER_OTLP_HEADERS"] = "x-telemetry-test=present",
            ["OTEL_EXPORTER_OTLP_TIMEOUT"] = "1000"
        });
        using var factory = new TelemetryFactory(new Capture());
        using var client = factory.CreateClient();
        var pathSentinel = Guid.NewGuid().ToString("D");
        client.DefaultRequestHeaders.UserAgent.ParseAdd("agent-secret-sentinel");
        using (var response = await client.GetAsync("/alerts"))
            Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        using (var missing = await client.GetAsync($"/missing/{pathSentinel}?probe=query-secret-sentinel"))
            Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);

        Assert.True(SpinWait.SpinUntil(() => receiver.Requests.Any(r => r.Path == "/base/v1/traces")
            && receiver.Requests.Any(r => r.Path == "/base/v1/logs"), TimeSpan.FromSeconds(10)),
            string.Join(", ", receiver.Requests.Select(r => r.Path)));
        Assert.All(receiver.Requests, request =>
        {
            Assert.Equal("present", request.Header);
            Assert.NotEmpty(request.Body);
            var payload = Encoding.UTF8.GetString(request.Body);
            Assert.Contains("sonrisa-web", payload, StringComparison.Ordinal);
            Assert.DoesNotContain(pathSentinel, payload, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("query-secret-sentinel", payload, StringComparison.Ordinal);
            Assert.DoesNotContain("agent-secret-sentinel", payload, StringComparison.Ordinal);
        });
    }

    [Fact]
    public async Task Signal_endpoints_override_common_endpoint_without_appending_http_paths()
    {
        await using var receiver = await Receiver.StartAsync();
        using var environment = new TelemetryEnvironment(new Dictionary<string, string?>
        {
            ["OTEL_EXPORTER_OTLP_ENDPOINT"] = receiver.Address + "ignored",
            ["OTEL_EXPORTER_OTLP_PROTOCOL"] = "grpc",
            ["OTEL_EXPORTER_OTLP_HEADERS"] = "x-telemetry-test=common",
            ["OTEL_EXPORTER_OTLP_TRACES_ENDPOINT"] = receiver.Address + "custom-traces",
            ["OTEL_EXPORTER_OTLP_TRACES_PROTOCOL"] = "http/protobuf",
            ["OTEL_EXPORTER_OTLP_TRACES_HEADERS"] = "x-telemetry-test=traces",
            ["OTEL_EXPORTER_OTLP_LOGS_ENDPOINT"] = receiver.Address + "custom-logs",
            ["OTEL_EXPORTER_OTLP_LOGS_PROTOCOL"] = "http/protobuf",
            ["OTEL_EXPORTER_OTLP_LOGS_HEADERS"] = "x-telemetry-test=logs",
            ["OTEL_SERVICE_NAME"] = "sonrisa-test"
        });
        using var factory = new TelemetryFactory(new Capture());
        using var client = factory.CreateClient();
        using (var response = await client.GetAsync("/alerts"))
            Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);

        Assert.True(SpinWait.SpinUntil(() => receiver.Requests.Any(r => r.Path == "/custom-traces")
            && receiver.Requests.Any(r => r.Path == "/custom-logs"), TimeSpan.FromSeconds(10)));
        Assert.DoesNotContain(receiver.Requests, request => request.Path.Contains("ignored", StringComparison.Ordinal));
        Assert.Equal("traces", receiver.Requests.Single(r => r.Path == "/custom-traces").Header);
        Assert.Equal("logs", receiver.Requests.Single(r => r.Path == "/custom-logs").Header);
        Assert.All(receiver.Requests, request => Assert.Contains("sonrisa-test",
            Encoding.UTF8.GetString(request.Body), StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("OTEL_EXPORTER_OTLP_TRACES_ENDPOINT", "not-a-uri")]
    [InlineData("OTEL_EXPORTER_OTLP_TRACES_PROTOCOL", "broken")]
    [InlineData("OTEL_EXPORTER_OTLP_TRACES_TIMEOUT", "-1")]
    [InlineData("OTEL_EXPORTER_OTLP_TRACES_HEADERS", "not-a-header")]
    [InlineData("OTEL_EXPORTER_OTLP_TRACES_HEADERS", "bad:name=value")]
    public async Task Malformed_trace_configuration_disables_only_trace_export(string key, string value)
    {
        await using var receiver = await Receiver.StartAsync();
        var capture = new Capture();
        using var environment = new TelemetryEnvironment(new Dictionary<string, string?>
        {
            ["OTEL_EXPORTER_OTLP_ENDPOINT"] = receiver.Address,
            ["OTEL_EXPORTER_OTLP_PROTOCOL"] = "http/protobuf",
            [key] = value
        });
        using var factory = new TelemetryFactory(capture);
        using var client = factory.CreateClient();
        using (var response = await client.GetAsync("/alerts"))
            Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);

        Assert.True(SpinWait.SpinUntil(() => receiver.Requests.Any(r => r.Path == "/v1/logs"),
            TimeSpan.FromSeconds(10)));
        Assert.DoesNotContain(receiver.Requests, request => request.Path == "/v1/traces");
        Assert.Contains(capture.Logs, log => log.Message.Contains(key, StringComparison.Ordinal));
        Assert.DoesNotContain(capture.Logs, log => log.ToString().Contains(value, StringComparison.Ordinal));
    }

    [Fact]
    public async Task Provider_diagnostics_with_sensitive_payloads_are_suppressed()
    {
        using var environment = new TelemetryEnvironment();
        var capture = new Capture();
        using var factory = new TelemetryFactory(capture);
        using var client = factory.CreateClient();
        using (var response = await client.GetAsync("/health/live"))
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var loggers = factory.Services.GetRequiredService<ILoggerFactory>();
        loggers.CreateLogger("Npgsql.Connection").LogWarning("provider-secret-sentinel");
        loggers.CreateLogger("Microsoft.EntityFrameworkCore.Database.Command")
            .LogWarning("sql-secret-sentinel");
        loggers.CreateLogger("Microsoft.AspNetCore.Hosting.Diagnostics")
            .LogWarning("request-secret-sentinel");
        Assert.DoesNotContain(capture.Logs, log => log.ToString().Contains("secret-sentinel", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Unavailable_export_endpoint_does_not_change_management_or_liveness_responses()
    {
        using var environment = new TelemetryEnvironment(new Dictionary<string, string?>
        {
            ["OTEL_EXPORTER_OTLP_ENDPOINT"] = "http://127.0.0.1:1",
            ["OTEL_EXPORTER_OTLP_PROTOCOL"] = "http/protobuf",
            ["OTEL_EXPORTER_OTLP_TIMEOUT"] = "100"
        });
        using var factory = new TelemetryFactory(new Capture());
        using var client = factory.CreateClient();
        using var unavailable = await client.GetAsync("/alerts");
        using var live = await client.GetAsync("/health/live");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, unavailable.StatusCode);
        Assert.Equal(HttpStatusCode.OK, live.StatusCode);
    }

    private sealed class TelemetryFactory(Capture capture) : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureTestServices(services =>
            {
                services.ConfigureOpenTelemetryTracerProvider(traces => traces.AddProcessor(new SpanCapture(capture)));
                services.ConfigureOpenTelemetryLoggerProvider(logs => logs.AddProcessor(new LogCapture(capture)));
            });
        }
    }

    private sealed class TelemetryEnvironment : IDisposable
    {
        private static readonly string[] Keys =
        [
            "OTEL_EXPORTER_OTLP_ENDPOINT", "OTEL_EXPORTER_OTLP_PROTOCOL",
            "OTEL_EXPORTER_OTLP_HEADERS", "OTEL_EXPORTER_OTLP_TIMEOUT",
            "OTEL_EXPORTER_OTLP_TRACES_ENDPOINT", "OTEL_EXPORTER_OTLP_TRACES_PROTOCOL",
            "OTEL_EXPORTER_OTLP_TRACES_HEADERS", "OTEL_EXPORTER_OTLP_TRACES_TIMEOUT",
            "OTEL_EXPORTER_OTLP_LOGS_ENDPOINT", "OTEL_EXPORTER_OTLP_LOGS_PROTOCOL",
            "OTEL_EXPORTER_OTLP_LOGS_HEADERS", "OTEL_EXPORTER_OTLP_LOGS_TIMEOUT",
            "OTEL_SERVICE_NAME", "ConnectionStrings__ProductDatabase", "MvpOwner__Id"
        ];
        private readonly Dictionary<string, string?> previous = new(StringComparer.Ordinal);

        public TelemetryEnvironment(IReadOnlyDictionary<string, string?>? values = null)
        {
            foreach (var key in Keys)
            {
                previous[key] = Environment.GetEnvironmentVariable(key);
                Environment.SetEnvironmentVariable(key,
                    values is not null && values.TryGetValue(key, out var value) ? value : null);
            }
        }

        public void Dispose()
        {
            foreach (var pair in previous)
                Environment.SetEnvironmentVariable(pair.Key, pair.Value);
        }
    }

    private sealed class Capture
    {
        public ConcurrentBag<SpanSnapshot> Spans { get; } = [];
        public ConcurrentBag<LogSnapshot> Logs { get; } = [];
    }

    private sealed class SpanCapture(Capture capture) : BaseProcessor<Activity>
    {
        public override void OnEnd(Activity data) => capture.Spans.Add(new SpanSnapshot(
            data.DisplayName, data.TraceId, data.SpanId,
            data.GetTagItem("http.response.status_code") is { } status ? Convert.ToInt32(status) : null,
            data.TagObjects.Select(tag => $"{tag.Key}={tag.Value}").ToArray()));
    }

    private sealed class LogCapture(Capture capture) : BaseProcessor<LogRecord>
    {
        public override void OnEnd(LogRecord data) => capture.Logs.Add(new LogSnapshot(
            data.TraceId, data.SpanId, data.FormattedMessage ?? data.Body ?? string.Empty,
            data.Attributes?.Select(attribute => $"{attribute.Key}={attribute.Value}").ToArray() ?? []));
    }

    private sealed record SpanSnapshot(string Name, ActivityTraceId TraceId, ActivitySpanId SpanId,
        int? StatusCode, string[] Tags)
    {
        public override string ToString() => $"{Name} {string.Join(" ", Tags)}";
    }

    private sealed record LogSnapshot(ActivityTraceId TraceId, ActivitySpanId SpanId,
        string Message, string[] Attributes)
    {
        public override string ToString() => $"{Message} {string.Join(" ", Attributes)}";
    }

    private sealed class Receiver(WebApplication app) : IAsyncDisposable
    {
        public ConcurrentBag<ReceivedRequest> Requests { get; } = [];
        public string Address { get; private set; } = string.Empty;

        public static async Task<Receiver> StartAsync()
        {
            var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Testing" });
            builder.WebHost.UseUrls("http://127.0.0.1:0");
            builder.Logging.ClearProviders();
            var app = builder.Build();
            var receiver = new Receiver(app);
            app.MapPost("/{**path}", async context =>
            {
                using var body = new MemoryStream();
                await context.Request.Body.CopyToAsync(body);
                receiver.Requests.Add(new ReceivedRequest(context.Request.Path,
                    context.Request.Headers["x-telemetry-test"].ToString(), body.ToArray()));
                context.Response.StatusCode = 200;
            });
            await app.StartAsync();
            receiver.Address = app.Urls.Single().TrimEnd('/') + "/";
            return receiver;
        }

        public async ValueTask DisposeAsync() => await app.DisposeAsync();
    }

    private sealed record ReceivedRequest(string Path, string Header, byte[] Body);
}
