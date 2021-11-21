using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using System.Diagnostics;

namespace Common.Observability;

public static class TagKey
{
	public static string Category = "category";
	public static string App = "app";
}

public static class TagValue
{
	public static readonly string App = Constants.AppName;
}

public static class Tracing
{
	public static ActivitySource? Source;

	public static TracerProvider? EnableTracing(Common.Tracing config)
	{
		TracerProvider? tracing = null;
		if (config.Enabled)
		{
			tracing = Sdk.CreateTracerProviderBuilder()
						.SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(TagValue.App))
                        .AddHttpClientInstrumentation(config =>
                        {
                            config.RecordException = true;
                            config.Enrich = (activity, name, rawEventObject) =>
                            {

								if (name.Equals("OnStartActivity")
									&& rawEventObject is HttpRequestMessage request)
								{
									activity.SetTag("http.method", request.Method.Method);
									activity.SetTag("http.uri.host", request.RequestUri?.Host ?? "null");
									activity.SetTag("http.uri.path", request.RequestUri?.AbsolutePath ?? "null");
									activity.SetTag("http.uri.query", request.RequestUri?.Query ?? "null");
									activity.SetTag("http.headers", request.Headers.ToString());
									activity.SetTag("http.content", request.Content?.ReadAsStringAsync().GetAwaiter().GetResult()); // todo: not quite working
								}

								if (name.Equals("OnStopActivity")
									&& rawEventObject is HttpResponseMessage response)
								{
									activity.SetTag("http.response.statuscode", response.StatusCode);
									activity.SetTag("http.response.reason", response.ReasonPhrase);
									activity.SetTag("http.response.headers", response.Headers.ToString());
									activity.SetTag("http.response.content", response.Content.ReadAsStringAsync().GetAwaiter().GetResult()); // todo: not quite working
								}

								if (name.Equals("OnException")
									&& rawEventObject is Exception exception)
								{
									activity.SetTag("http.error.message", exception.Message);
									activity.SetTag("http.error.stackTrace", exception.StackTrace);
								}

							};
							config.RecordException = true;
                        })
                        .AddSource(TagValue.App)
						.AddOtlpExporter(c => 
						{
							c.Endpoint = new Uri(config.Url ?? String.Empty);
						})
						.Build();

			Log.Information("Tracing started and exporting to: http://{@Url}", config.Url);
		}

		return tracing;
	}

	public static Activity? Trace(string name, string category = "app")
	{
		var activity = Activity.Current?.Source.StartActivity(name)
			??
			new ActivitySource(TagValue.App)?.StartActivity(name);

		activity?
			.SetTag(TagKey.Category, category);

		return activity;
	}

	public static Activity? WithTag(this Activity activity, string key, string value)
	{
		return activity?.SetTag(key, value);
	}

	public static void ValidateConfig(ObservabilityConfig config)
	{
		if (!config.Tracing.Enabled)
			return;

		if (string.IsNullOrEmpty(config.Tracing.Url))
		{
			Log.Error("Agent Host must be set: {@ConfigSection}.{@ConfigProperty}.", nameof(config), nameof(config.Tracing.Url));
			throw new ArgumentException("Agent Host must be set.", nameof(config.Tracing.Url));
		}
	}
}
