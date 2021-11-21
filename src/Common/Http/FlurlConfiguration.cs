using Common.Observability;
using PromMetrics = Prometheus.Metrics;
using Prom = Prometheus;
using Flurl.Http;
using Serilog;

namespace Common.Http;

public static class FlurlConfiguration
{
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
			Log.Verbose("HTTP Request: {@HttpMethod} - {@Uri} - {@Headers} - {@Content}", 
				call.HttpRequestMessage.Method, call.HttpRequestMessage.RequestUri, call.HttpRequestMessage.Headers.ToString(), call.HttpRequestMessage.Content);
			return Task.CompletedTask;
		};

		Func<FlurlCall, Task> afterCallAsync = async (FlurlCall call) =>
		{
			Log.Verbose("HTTP Response: {@HttpStatusCode} - {@HttpMethod} - {@Uri} - {@Headers} - {@Content}", 
				call.HttpResponseMessage?.StatusCode, call.HttpRequestMessage?.Method?.ToString() ?? "null", call.HttpRequestMessage?.RequestUri?.ToString() ?? "null", call.HttpResponseMessage?.Headers.ToString() ?? "null", await call.HttpResponseMessage?.Content?.ReadAsStringAsync() ?? "null");

			if (config.Observability.Prometheus.Enabled)
			{
				HttpRequestHistogram
				.WithLabels(
					call.HttpRequestMessage?.Method?.ToString() ?? "null",
					call.HttpRequestMessage?.RequestUri?.Host ?? "null",
					call.HttpRequestMessage?.RequestUri?.AbsolutePath ?? "null",
					call.HttpRequestMessage?.RequestUri?.Query ?? "null",
					((int)call.HttpResponseMessage.StatusCode).ToString() ?? "null",
					call.HttpResponseMessage?.ReasonPhrase ?? "null"
				).Observe(call.Duration.GetValueOrDefault().TotalSeconds);
			}
		};

		Func<FlurlCall, Task> onErrorAsync = async (FlurlCall call) =>
		{
			var response = string.Empty;
			if (call.HttpResponseMessage is object)
				response = await call.HttpResponseMessage?.Content?.ReadAsStringAsync();
			Log.Error("Http Call Failed. {@HttpStatusCode} {@Content}", call.HttpResponseMessage?.StatusCode, response);
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
