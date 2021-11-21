using Common.Observability;
using Flurl.Http;
using Serilog;

namespace Common.Grafana;

public interface IGrafanaClient
{
    Task CreateAnnotationAsync(long time, string message, string type, string action, string container);
}

public class GrafanaClient : IGrafanaClient
{
    private static readonly ILogger _logger = LogContext.ForClass<GrafanaClient>();

    private readonly string Uri;
    private readonly string ApiKey;

    public GrafanaClient(IAppConfiguration configuration)
    {
        Uri = configuration.Grafana.Uri;
        ApiKey = configuration.Grafana.ApiKey;
    }

    public Task CreateAnnotationAsync(long nanoSinceEpoch, string message, string type, string action, string container)
    {
        using var tracing = Observability.Tracing.Trace($"{nameof(GrafanaClient)}.{nameof(CreateAnnotationAsync)}");

        var request = new PostAnnotationRequest()
        {
            Time = nanoSinceEpoch / 1_000_000, // nano epoch to milliseconds epoch
            Text = message,
             Tags = new string[] { type, action, container }
        };

        return $"{Uri}/api/annotations"
                .WithOAuthBearerToken(ApiKey)
                .PostJsonAsync(request);
    }
}

