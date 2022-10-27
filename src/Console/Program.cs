// See https://aka.ms/new-console-template for more information
using Common;
using Common.Http;
using Common.Service;
using Console;
using Core.Docker;
using Core.Grafana;
using Core.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Enrichers.Span;

System.Console.WriteLine("Welcome! DocGraf is starting up...");

Statics.AppType = Constants.ConsoleAppName;
Statics.MetricPrefix = Constants.ConsoleAppName;
Statics.TracingService = Constants.ConsoleAppName;

using IHost host = CreateHostBuilder(args).Build();
await host.RunAsync();

static IHostBuilder CreateHostBuilder(string[] args)
{
	return Host.CreateDefaultBuilder(args)
		.ConfigureAppConfiguration(configBuilder =>
		{
			configBuilder.Sources.Clear();

			var configPath = Environment.CurrentDirectory;
			if (args.Length > 0) configPath = args[0];

			configBuilder
				.AddJsonFile(Path.Join(configPath, "configuration.local.json"), optional: true, reloadOnChange: true)
				.AddEnvironmentVariables(prefix: $"{Constants.AppName}_")
				.AddCommandLine(args)
				.Build();
		})
		.UseSerilog((ctx, logConfig) =>
		{
			logConfig
			.ReadFrom.Configuration(ctx.Configuration, sectionName: "Observability:Serilog")
			.Enrich.WithSpan()
			.Enrich.FromLogContext();
		})
		.ConfigureServices((hostContext, services) =>
		{
			services.AddSingleton<ISettingsService, FileBasedSettingsService>();

			services.AddSingleton<IDockerClientWrapper, DockerClient>();
			services.AddSingleton<IGrafanaClient, GrafanaClient>();

			// HTTP
			var config = new Configuration();
			ConfigurationSetup.LoadConfigValues(hostContext.Configuration, config);
			FlurlConfiguration.Configure(config.Observability);

			services.AddHostedService<Startup>();
		});
}