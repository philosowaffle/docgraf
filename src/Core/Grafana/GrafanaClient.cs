using Common;
using Common.Observability;
using Flurl.Http;
using Serilog;
using Traces = Common.Observability.Traces;

namespace Core.Grafana;

public interface IGrafanaClient
{
	Task CreateAnnotationAsync(long time, string message, string type, string action, string containerName, string image);
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

	public Task CreateAnnotationAsync(long nanoSinceEpoch, string message, string type, string action, string containerName, string image)
	{
		using var tracing = Traces.Trace($"{nameof(GrafanaClient)}.{nameof(CreateAnnotationAsync)}");

		var request = new PostAnnotationRequest()
		{
			Time = nanoSinceEpoch / 1_000_000, // nano epoch to milliseconds epoch
			Text = message,
			Tags = new string[] { type, action, containerName, image }
		};

		return $"{Uri}/api/annotations"
				.WithOAuthBearerToken(ApiKey)
				.PostJsonAsync(request);
	}
}

