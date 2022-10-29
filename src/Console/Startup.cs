using Common;
using Common.Observability;
using Core.Docker;
using Core.Settings;
using Microsoft.Extensions.Hosting;
using Serilog;
using Metrics = Common.Observability.Metrics;

namespace Console;

internal class Startup : BackgroundService
{
	private static readonly ILogger _logger = LogContext.ForClass<Startup>();

	private ISettingsService _settingsService;
	private IDockerClientWrapper _dockerClient;

	public Startup(ISettingsService settingsService, IDockerClientWrapper dockerClient)
	{
		_settingsService = settingsService;
		_dockerClient = dockerClient;

		Traces.Source = new(Statics.TracingService);
		Logging.LogSystemInformation();
	}

	protected override async Task ExecuteAsync(CancellationToken cancelToken)
	{
		_logger.Verbose("Begin.");

		try
		{
			var settings = await _settingsService.GetSettingsAsync();

			Metrics.ValidateConfig(settings.Observability.Metrics);
			Traces.ValidateConfig(settings.Observability.Tracing);

		} catch (Exception ex)
		{
			_logger.Error(ex, "Exception during config validation. Please modify your configuration.local.json and relaunch the application.");
			System.Console.ReadLine();
			Environment.Exit(-1);
		}

		await RunAsync(cancelToken);
	}

	private async Task RunAsync(CancellationToken cancelToken)
	{
		int exitCode = 0;

		var settings = await _settingsService.GetSettingsAsync();

		Log.Information("*********************************************");
		using var metrics = Metrics.EnableMetricsServer(settings.Observability.Metrics);
		using var metricsCollector = Metrics.EnableCollector(settings.Observability.Metrics);
		using var tracing = Traces.EnableConsoleTracing(settings.Observability.Tracing);
		Log.Information("*********************************************");

		Metrics.CreateAppInfo();

		try
		{
			while (!cancelToken.IsCancellationRequested) 
			{ 
				await _dockerClient.BeginEventMonitoringAsync(cancelToken);
			}

		} catch (Exception ex)
		{
			_logger.Fatal(ex, "RunAsync failed.");
			exitCode = -2;

		} finally
		{
			_logger.Verbose("End.");
			Environment.Exit(exitCode);
		}
	}
}
