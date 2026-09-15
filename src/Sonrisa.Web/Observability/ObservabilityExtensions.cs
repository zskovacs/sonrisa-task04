using System.Diagnostics;
using System.Globalization;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Sonrisa.Web.Observability;

internal static class ObservabilityExtensions
{
    internal static IReadOnlyList<string> AddSonrisaObservability(this WebApplicationBuilder builder)
    {
        var traces = ExportSettings.Read(builder.Configuration, "TRACES");
        var logs = ExportSettings.Read(builder.Configuration, "LOGS");

        // Framework request-start diagnostics include the raw URL before routing. Application
        // failure logs remain available and contain only bounded operation/classification data.
        builder.Logging.AddFilter("Microsoft.AspNetCore", LogLevel.None);
        builder.Logging.AddFilter("OpenTelemetry", LogLevel.None);
        builder.Logging.AddFilter<OpenTelemetryLoggerProvider>("Microsoft.Hosting.Lifetime", LogLevel.None);
        builder.Logging.AddJsonConsole(options => options.IncludeScopes = false);
        builder.Services.Configure<OpenTelemetryLoggerOptions>(options =>
        {
            options.IncludeFormattedMessage = true;
            options.IncludeScopes = false;
        });

        var serviceName = builder.Configuration["OTEL_SERVICE_NAME"];
        if (string.IsNullOrWhiteSpace(serviceName))
            serviceName = "sonrisa-web";

        builder.Services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(serviceName))
            .WithTracing(tracing =>
            {
                tracing.AddAspNetCoreInstrumentation(options => options.RecordException = false);
                tracing.AddProcessor(new SafeRequestSpanProcessor());
                if (traces is { ErrorKey: null })
                    tracing.AddOtlpExporter(traces.Apply);
            })
            .WithLogging(logging =>
            {
                if (logs is { ErrorKey: null })
                    logging.AddOtlpExporter(logs.Apply);
            });

        return new[] { traces?.ErrorKey, logs?.ErrorKey }
            .Where(key => key is not null)
            .Select(key => key!)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }

    private sealed record ExportSettings(
        Uri Endpoint,
        OtlpExportProtocol Protocol,
        string? Headers,
        int? TimeoutMilliseconds)
    {
        public string? ErrorKey { get; init; }

        public void Apply(OtlpExporterOptions options)
        {
            options.Endpoint = Endpoint;
            options.Protocol = Protocol;
            if (Headers is not null)
                options.Headers = Headers;
            if (TimeoutMilliseconds is not null)
                options.TimeoutMilliseconds = TimeoutMilliseconds.Value;
        }

        public static ExportSettings? Read(IConfiguration configuration, string signal)
        {
            var signalEndpointKey = $"OTEL_EXPORTER_OTLP_{signal}_ENDPOINT";
            const string endpointKey = "OTEL_EXPORTER_OTLP_ENDPOINT";
            var signalEndpoint = configuration[signalEndpointKey];
            var useSignalEndpoint = !string.IsNullOrWhiteSpace(signalEndpoint);
            var endpointText = useSignalEndpoint ? signalEndpoint : configuration[endpointKey];
            if (string.IsNullOrWhiteSpace(endpointText))
                return null;

            var selectedEndpointKey = useSignalEndpoint ? signalEndpointKey : endpointKey;
            if (!Uri.TryCreate(endpointText, UriKind.Absolute, out var endpoint)
                || (endpoint.Scheme != Uri.UriSchemeHttp && endpoint.Scheme != Uri.UriSchemeHttps)
                || !string.IsNullOrEmpty(endpoint.UserInfo)
                || !string.IsNullOrEmpty(endpoint.Query)
                || !string.IsNullOrEmpty(endpoint.Fragment))
                return Invalid(selectedEndpointKey);

            var (protocolText, protocolKey) = Select(configuration, signal, "PROTOCOL");
            var protocol = OtlpExportProtocol.Grpc;
            if (!string.IsNullOrWhiteSpace(protocolText))
            {
                if (protocolText == "http/protobuf")
                    protocol = OtlpExportProtocol.HttpProtobuf;
                else if (protocolText != "grpc")
                    return Invalid(protocolKey);
            }

            var (headers, headersKey) = Select(configuration, signal, "HEADERS");
            if (!string.IsNullOrWhiteSpace(headers) && !ValidHeaders(headers))
                return Invalid(headersKey);

            var (timeoutText, timeoutKey) = Select(configuration, signal, "TIMEOUT");
            int? timeout = null;
            if (!string.IsNullOrWhiteSpace(timeoutText))
            {
                if (!int.TryParse(timeoutText, NumberStyles.None, CultureInfo.InvariantCulture, out var parsed)
                    || parsed <= 0)
                    return Invalid(timeoutKey);
                timeout = parsed;
            }

            if (protocol == OtlpExportProtocol.HttpProtobuf && !useSignalEndpoint)
                endpoint = new Uri(endpoint.AbsoluteUri.TrimEnd('/') + "/v1/" + signal.ToLowerInvariant());
            return new ExportSettings(endpoint, protocol, headers, timeout);
        }

        private static ExportSettings Invalid(string key) => new(new Uri("http://localhost"),
            OtlpExportProtocol.Grpc, null, null) { ErrorKey = key };

        private static (string? Value, string Key) Select(IConfiguration configuration, string signal, string suffix)
        {
            var specific = $"OTEL_EXPORTER_OTLP_{signal}_{suffix}";
            var value = configuration[specific];
            return !string.IsNullOrWhiteSpace(value)
                ? (value, specific)
                : (configuration[$"OTEL_EXPORTER_OTLP_{suffix}"], $"OTEL_EXPORTER_OTLP_{suffix}");
        }

        private static bool ValidHeaders(string headers) => headers.Split(',').All(header =>
        {
            var equals = header.IndexOf('=');
            if (equals <= 0)
                return false;
            var name = header.AsSpan(0, equals).Trim();
            if (name.Length == 0 || name.ToArray().Any(character =>
                character is not (>= 'A' and <= 'Z' or >= 'a' and <= 'z' or >= '0' and <= '9')
                && "!#$%&'*+-.^_`|~".IndexOf(character) < 0))
                return false;
            return !header.AsSpan(equals + 1).ToArray().Any(character =>
                character < ' ' || character == 127);
        });
    }

    private sealed class SafeRequestSpanProcessor : BaseProcessor<Activity>
    {
        public override void OnEnd(Activity activity)
        {
            var route = activity.GetTagItem("http.route") as string;
            if (!string.IsNullOrEmpty(route))
            {
                route = "/" + route.TrimStart('/');
                activity.SetTag("http.route", route);
            }
            var method = activity.GetTagItem("http.request.method") as string;
            method = method is "GET" or "POST" or "HEAD" or "OPTIONS" or "PUT" or "PATCH" or "DELETE"
                ? method : "OTHER";

            // The instrumentation may use an unmatched raw path as its span name. Keep only
            // the known route template, method and response status before any exporter sees it.
            activity.DisplayName = string.IsNullOrWhiteSpace(route) ? $"HTTP {method}" : $"{method} {route}";
            foreach (var tag in activity.TagObjects.ToArray())
            {
                if (tag.Key is not ("http.request.method" or "http.route" or "http.response.status_code"))
                    activity.SetTag(tag.Key, null);
            }
        }
    }
}
