using Common.Observability;
using PromMetrics = Prometheus.Metrics;
using Prom = Prometheus;
using Flurl.Http;
using Serilog;

namespace Common.Http;

public static class FlurlConfiguration
{
	private static readonly ILogger _logger = LogContext.ForStatic("Flurl");

	public static readonly Prom.Histogram HttpRequestHistogram = PromMetrics.CreateHistogram($"{Metrics.Prefix}_http_duration_seconds", "The histogram of http requests.", new Prom.HistogramConfiguration
	{
		LabelNames = new[]
		{
				Metrics.Label.HttpMethod,
				Metrics.Label.HttpHost,
				Metrics.Label.HttpRequestPath,
				Metrics.Label.HttpRequestQuery,
				Metrics.Label.HttpStatusCode,
				Metrics.Label.HttpMessage
			}
	});

	public static void Configure(IAppConfiguration config)
	{
		Func<FlurlCall, Task> beforeCallAsync = (FlurlCall call) =>
		{
			_logger.Verbose("HTTP Request: {@HttpMethod} - {@Uri} - {@Headers} - {@Content}", 
				call.HttpRequestMessage.Method, call.HttpRequestMessage.RequestUri, call.HttpRequestMessage.Headers.ToString(), call.HttpRequestMessage.Content);
			return Task.CompletedTask;
		};

		Func<FlurlCall, Task> afterCallAsync = async (FlurlCall call) =>
		{
			var method = call.HttpRequestMessage?.Method?.ToString() ?? "null";
			var uri = call.HttpRequestMessage?.RequestUri?.ToString() ?? "null";
			var headers = "null";
			var content = "null";
			var statusCode = "null";

			if (call.HttpResponseMessage is object)
			{
				headers = call.HttpResponseMessage.Headers.ToString() ?? "null";
				content = await call.HttpResponseMessage.Content.ReadAsStringAsync() ?? "null";
				statusCode = ((int)call.HttpResponseMessage.StatusCode).ToString() ?? "null";
			}

			_logger.Verbose("HTTP Response: {@HttpStatusCode} - {@HttpMethod} - {@Uri} - {@Headers} - {@Content}",
				statusCode, method, uri, headers, content);

			if (config.Observability.Prometheus.Enabled)
			{
				HttpRequestHistogram
				.WithLabels(
					method,
					uri,
					call.HttpRequestMessage?.RequestUri?.AbsolutePath ?? "null",
					call.HttpRequestMessage?.RequestUri?.Query ?? "null",
					statusCode,
					call.HttpResponseMessage?.ReasonPhrase ?? "null"
				).Observe(call.Duration.GetValueOrDefault().TotalSeconds);
			}
		};

		Func<FlurlCall, Task> onErrorAsync = async (FlurlCall call) =>
		{
			var response = string.Empty;
			if (call.HttpResponseMessage is object)
				response = await call.HttpResponseMessage.Content.ReadAsStringAsync();
			_logger.Error("Http Call Failed. {@HttpStatusCode} {@Content}", call.HttpResponseMessage?.StatusCode, response);
		};

		FlurlHttp.Configure(settings =>
		{
			settings.Timeout = new TimeSpan(0, 0, 10);
			settings.BeforeCallAsync = beforeCallAsync;
			settings.AfterCallAsync = afterCallAsync;
			settings.OnErrorAsync = onErrorAsync;
			settings.Redirects.ForwardHeaders = true;
		});
	}
}
