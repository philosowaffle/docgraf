using Microsoft.Extensions.Configuration;

namespace Common;

public interface IAppConfiguration
{
	App App { get; set; }

	ObservabilityConfig Observability { get; set; }
	Developer Developer { get; set; }
}

public static class ConfigurationSetup
{
	public static void LoadConfigValues(IConfiguration provider, IAppConfiguration config)
	{
		provider.GetSection(nameof(App)).Bind(config.App);
		provider.GetSection("Observability").Bind(config.Observability);
		provider.GetSection(nameof(Developer)).Bind(config.Developer);
	}
}

public class Configuration : IAppConfiguration
{
	public Configuration()
	{
		App = new App();
		Observability = new ObservabilityConfig();
		Developer = new Developer();
	}

	public App App { get; set; }

	public ObservabilityConfig Observability { get; set; }
	public Developer Developer { get; set; }
}

public class App
{
	public App()
	{
	}
}

public class ObservabilityConfig
{
	public ObservabilityConfig()
	{
		Prometheus = new Prometheus();
		Tracing = new Tracing();
	}

	public Prometheus Prometheus { get; set; }
	public Tracing Tracing { get; set; }
}

public class Tracing
{
	public bool Enabled { get; set; }
	public string? Url { get; set; }
}

public class Prometheus
{
	public bool Enabled { get; set; }
	public int? Port { get; set; }
}

public class Developer
{
	public string? UserAgent { get; set; }
}

public enum UploadStrategy
{
	PythonAndGuploadInstalledLocally = 0,
	WindowsExeBundledPython = 1,
	NativeImplV1 = 2
}

