using Common.Observability;
using Core.Settings;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace Common.Service
{
	public class FileBasedSettingsService : ISettingsService
	{
		private static readonly ILogger _logger = LogContext.ForClass<FileBasedSettingsService>();

		private readonly IConfiguration _configurationLoader;

		public FileBasedSettingsService(IConfiguration configurationLoader)
		{
			_configurationLoader = configurationLoader;
		}

		public Task<Configuration> GetSettingsAsync()
		{
			using var tracing = Traces.Trace($"{nameof(FileBasedSettingsService)}.{nameof(GetSettingsAsync)}");

			var settings = new Configuration();
			ConfigurationSetup.LoadConfigValues(_configurationLoader, settings);

			return Task.FromResult(settings);
		}
	}
}
