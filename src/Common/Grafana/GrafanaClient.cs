using Common.Observability;
using Flurl.Http;
using Serilog;

namespace Common.Grafana;

public interface IGrafanaClient
{
    Task CreateAnnotationAsync(long time, string message, string action, string container);
}

public class GrafanaClient : IGrafanaClient
{
    private static readonly ILogger _logger = LogContext.ForClass<GrafanaClient>();

    private readonly string Host;
    private readonly string ApiKey;

    public GrafanaClient(IAppConfiguration configuration)
    {
        Host = configuration.Grafana.Host ?? "localhost:3000";
        ApiKey = configuration.Grafana.ApiKey ?? string.Empty;
    }

    public Task CreateAnnotationAsync(long time, string message, string action, string container)
    {
        using var tracing = Observability.Tracing.Trace($"{nameof(GrafanaClient)}.{nameof(CreateAnnotationAsync)}");

        var request = new PostAnnotationRequest()
        {
            Time = time * 1000, // seconds epoch to milliseconds epoch
            Text = message,
             Tags = new string[] { action, container }
        };

        return $"{Host}/api/annotations"
                .WithOAuthBearerToken(ApiKey)
                .PostJsonAsync(request);
    }
}

