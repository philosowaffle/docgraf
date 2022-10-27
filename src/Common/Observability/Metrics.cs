using Prometheus;
using Prometheus.DotNetRuntime;
using Serilog;
using PromMetrics = Prometheus.Metrics;

namespace Common.Observability;

public static class Metrics
{
	public static IMetricServer? EnableMetricsServer(Common.Metrics config)
	{
		IMetricServer? metricsServer = null;
		if (config.Enabled)
		{
			var port = config.Port ?? 4000;
			metricsServer = new KestrelMetricServer(port: port);
			metricsServer.Start();

			Log.Information("Metrics Server started and listening on: http://localhost:{0}/metrics", port);
		}

		return metricsServer;
	}

	public static void ValidateConfig(Common.Metrics config)
	{
		if (!config.Enabled) return;

		if (config.Port.HasValue && config.Port <= 0)
		{
			Log.Error("Metrics Port must be a valid port: {@ConfigSection}.{@ConfigProperty}.", nameof(config), nameof(config.Port));
			throw new ArgumentException("Metrics port must be greater than 0.", nameof(config.Port));
		}
	}

	public static void CreateAppInfo()
	{
		PromMetrics.CreateGauge($"{Statics.MetricPrefix}_build_info", "Build info for the running instance.", new GaugeConfiguration()
		{
			LabelNames = new[] { Label.Version, Label.Os, Label.OsVersion, Label.DotNetRuntime, Label.RunningInDocker }
		}).WithLabels(Constants.AppVersion, SystemInformation.OS, SystemInformation.OSVersion, SystemInformation.RunTimeVersion, SystemInformation.RunningInDocker.ToString())
.Set(1);
	}

	public static IDisposable? EnableCollector(Common.Metrics config)
	{
		if (config.Enabled)
			return DotNetRuntimeStatsBuilder
				.Customize()
				.WithContentionStats()
				.WithJitStats()
				.WithThreadPoolStats()
				.WithGcStats()
				.WithExceptionStats()
				//.WithDebuggingMetrics(true)
				.WithErrorHandler(ex => Log.Error(ex, "Unexpected exception occurred in prometheus-net.DotNetRuntime"))
				.StartCollecting();

		return null;
	}
}

public static class AppMetrics
{
	public static readonly Gauge UpdateAvailable = PromMetrics.CreateGauge($"{Statics.MetricPrefix}_update_available", "Indicates a newer version is availabe.", new GaugeConfiguration()
	{
		LabelNames = new[] { Label.Version, Label.LatestVersion }
	});

	public static void SyncUpdateAvailableMetric(bool isUpdateAvailable, string? latestVersion)
	{
		if (isUpdateAvailable)
			UpdateAvailable
				.WithLabels(Constants.AppVersion, latestVersion ?? string.Empty)
				.Set(1);
		else
			UpdateAvailable
				.WithLabels(Constants.AppVersion, Constants.AppVersion)
				.Set(0);
	}
}
