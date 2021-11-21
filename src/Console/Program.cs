// See https://aka.ms/new-console-template for more information
using Common;
using Common.Http;
using Console;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Primitives;
using Serilog;
using Serilog.Enrichers.Span;

System.Console.WriteLine("Hello, World!");

using IHost host = CreateHostBuilder(args).Build();
host.RunAsync();

static IHostBuilder CreateHostBuilder(string[] args)
{
    return Host.CreateDefaultBuilder(args)
		.ConfigureAppConfiguration(configBuilder =>
		{
			configBuilder.Sources.Clear();

			var configPath = Environment.CurrentDirectory;
			if (args.Length > 0) configPath = args[0];

			configBuilder.AddJsonFile(Path.Join(configPath, "configuration.local.json"), optional: true, reloadOnChange: true)
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
			services.AddSingleton<IAppConfiguration>((serviceProvider) =>
			{
				var config = new Configuration();
				var provider = serviceProvider.GetService<IConfiguration>();
				if (provider is null) return config;

				ConfigurationSetup.LoadConfigValues(provider, config);

				ChangeToken.OnChange(() => provider.GetReloadToken(), () =>
				{
					Log.Information("Config change detected, reloading config values.");
					ConfigurationSetup.LoadConfigValues(provider, config);
					FlurlConfiguration.Configure(config);
					Log.Information("Config reloaded.");
				});

				return config;
			});

			services.AddHostedService<Startup>();
		});
}