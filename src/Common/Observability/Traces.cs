using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using System.Diagnostics;

namespace Common.Observability
{
	public static class TagKey
	{
		public static string Category = "category";
		public static string App = "app";
		public static string Table = "table";
		public static string Format = "file_type";
	}

	public static class Traces
	{
		public static ActivitySource? Source;

		public static TracerProvider? EnableConsoleTracing(Common.Tracing config)
		{
			TracerProvider? tracing = null;
			if (!config.Enabled)
				return tracing;

			var builder = Sdk.CreateTracerProviderBuilder()
				.ConfigureDefaultBuilder(config)
				.Build();

			Log.Information("Tracing started and exporting to: http://{@Url}", config.Url);

			return tracing;
		}

		private static TracerProviderBuilder ConfigureDefaultBuilder(this TracerProviderBuilder builder, Common.Tracing config)
		{
			return builder
					.AddSource(Statics.TracingService)
					.SetResourceBuilder(
						ResourceBuilder
						.CreateDefault()
						.AddService(serviceName: Statics.TracingService, serviceVersion: Constants.AppVersion)
						.AddAttributes(new List<KeyValuePair<string, object>>()
						{
							new KeyValuePair<string, object>("host.machineName", Environment.MachineName),
							new KeyValuePair<string, object>("host.os", Environment.OSVersion.VersionString),
							new KeyValuePair<string, object>("dotnet.version", Environment.Version.ToString()),
						})
					)
					.SetSampler(new AlwaysOnSampler())
					.SetErrorStatusOnException()
					.AddHttpClientInstrumentation(h =>
					{
						h.RecordException = true;
						h.Enrich = HttpEnricher;
					})
					.AddOtlpExporter(o =>
					{
						o.Endpoint = new Uri(config.Url);
						o.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.Grpc;
					});
		}

		public static void StartNewActiviy(string activityName)
		{
			EndCurrentActivity();
			Activity.Current = Source?.CreateActivity(activityName, ActivityKind.Server);
		}

		public static void EndCurrentActivity()
		{
			Activity.Current?.Dispose();
			Activity.Current = null;
		}

		public static Activity? Trace(string name, string category = "app", ActivityKind kind = ActivityKind.Server)
		{
			if (Activity.Current is null) StartNewActiviy(name);

			var activity = Source?.StartActivity(name, kind);

			activity?
				.SetTag(TagKey.Category, category)
				.SetTag("SpanId", activity.SpanId)
				.SetTag("TraceId", activity.TraceId);

			return activity;
		}

		public static Activity? WithTable(this Activity activity, string table)
		{
			return activity?.SetTag(TagKey.Table, table);
		}

		public static Activity? WithTag(this Activity activity, string key, string value)
		{
			return activity?.SetTag(key, value);
		}

		public static void ValidateConfig(Common.Tracing config)
		{
			if (!config.Enabled)
				return;

			try
			{
				var uri = new Uri(config.Url);
			} catch (Exception e)
			{
				Log.Error("Tracing Agent Url must be a valid Uri: {@ConfigSection}.{@ConfigProperty}.", nameof(Tracing), nameof(config.Url));
				throw new ArgumentException("Invalid Tracing Agent Url.", nameof(config.Url), e);
			}
		}

		public static void HttpEnricher(Activity activity, string name, object rawEventObject)
		{
			if (name.Equals("OnStartActivity"))
				if (rawEventObject is HttpRequestMessage request)
				{
					activity.DisplayName = $"{request.Method} {request?.RequestUri?.AbsolutePath}";
					activity.SetTag("http.request.path", request?.RequestUri?.AbsolutePath);
					activity.SetTag("http.request.query", request?.RequestUri?.Query);
					activity.SetTag("http.request.body", request?.Content?.ReadAsStringAsync().GetAwaiter().GetResult() ?? "no_content");
				}
			else if (name.Equals("OnStopActivity"))
				if (rawEventObject is HttpResponseMessage response)
				{
					var content = response.Content?.ReadAsStringAsync().GetAwaiter().GetResult() ?? "no_content";
					activity.SetTag("http.response.body", content);
				}
			else if (name.Equals("OnException"))
				if (rawEventObject is Exception exception)
					activity.SetTag("stackTrace", exception.StackTrace);
		}
	}
}