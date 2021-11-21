using Common;
using Common.Docker;
using Common.Http;
using Common.Observability;
using Microsoft.Extensions.Hosting;
using Prometheus;
using Serilog;
using System.Diagnostics;
using System.Reflection;
using Metrics = Common.Observability.Metrics;

namespace Console;

internal class Startup : BackgroundService
{
    private static readonly ILogger _logger = LogContext.ForClass<Startup>();
    private static readonly Gauge BuildInfo = Prometheus.Metrics.CreateGauge($"{Metrics.Prefix}_build_info", "Build info for the running instance.", new GaugeConfiguration()
    {
        LabelNames = new[] { Metrics.Label.Version, Metrics.Label.Os, Metrics.Label.OsVersion, Metrics.Label.DotNetRuntime }
    });

    private IAppConfiguration _config;
    private IDockerClientWrapper _dockerClient;

    public Startup(IAppConfiguration configuration, IDockerClientWrapper dockerClient)
    {
        _config = configuration;
        _dockerClient = dockerClient;

        FlurlConfiguration.Configure(_config);

        var runtimeVersion = Environment.Version.ToString();
        var os = Environment.OSVersion.Platform.ToString();
        var osVersion = Environment.OSVersion.VersionString;
        var assembly = Assembly.GetExecutingAssembly();
        var versionInfo = FileVersionInfo.GetVersionInfo(assembly.Location);
        var version = versionInfo.ProductVersion ?? "unknown";

        BuildInfo.WithLabels(version, os, osVersion, runtimeVersion).Set(1);
        Log.Debug("App Version: {@Version}", version);
        Log.Debug("Operating System: {@Os}", osVersion);
        Log.Debug("DotNet Runtime: {@DotnetRuntime}", runtimeVersion);
    }

    protected override Task ExecuteAsync(CancellationToken cancelToken)
    {
        _logger.Verbose("Begin.");
        return RunAsync(cancelToken);
    }

    private Task RunAsync(CancellationToken cancelToken)
    {
        using var metrics = Metrics.EnableMetricsServer(_config.Observability.Prometheus);
        using var metricsCollector = Metrics.EnableCollector(_config.Observability.Prometheus);
        using var tracing = Common.Observability.Tracing.EnableTracing(_config.Observability.Tracing);
        using var tracingSource = new ActivitySource("ROOT");

        try
        {
            _dockerClient.BeginEventMonitoringAsync();

            while (!cancelToken.IsCancellationRequested) { }

            return Task.CompletedTask;

        } catch (Exception ex)
        {
            _logger.Fatal(ex, "RunAsync failed.");
            return Task.CompletedTask;

        } finally
        {
            _logger.Verbose("End.");
        }
    }
}
