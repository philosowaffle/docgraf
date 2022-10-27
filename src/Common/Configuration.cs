using Microsoft.Extensions.Configuration;

namespace Common;

public interface IAppConfiguration
{
	App App { get; set; }
	DockerConfig Docker { get; set; }
	GrafanaConfig Grafana { get; set; }

	ObservabilityConfig Observability { get; set; }
	Developer Developer { get; set; }
}

public static class ConfigurationSetup
{
	public static void LoadConfigValues(IConfiguration provider, IAppConfiguration config)
	{
		provider.GetSection(nameof(App)).Bind(config.App);
		provider.GetSection("Docker").Bind(config.Docker);
		provider.GetSection("Grafana").Bind(config.Grafana);
		provider.GetSection(nameof(Observability)).Bind(config.Observability);
		provider.GetSection(nameof(Developer)).Bind(config.Developer);
	}
}

public class Configuration : IAppConfiguration
{
	public Configuration()
	{
		App = new App();
		Docker = new DockerConfig();
		Grafana = new GrafanaConfig();
		Observability = new ObservabilityConfig();
		Developer = new Developer();
	}

	public App App { get; set; }
	public DockerConfig Docker { get; set; }
	public GrafanaConfig Grafana { get; set; }

	public ObservabilityConfig Observability { get; set; }
	public Developer Developer { get; set; }
}

public class App
{
	public App()
	{
	}
}

public class DockerConfig
{
	public DockerConfig()
	{
		Uri = "http://localhost:4243";
		ContainerEvents = new string[] { "start", "stop", "restart" };
		ImageEvents = new string[] { };
		PluginEvents = new string[] { };
		VolumeEvents = new string[] { };
		DaemonEvents = new string[] { };
		ServiceEvents = new string[] { };
		NodeEvents = new string[] { };
		SecretEvents = new string[] { };
		ConfigEvents = new string[] { };
	}

	public string Uri { get; set; }
	/// <summary>
	/// https://docs.docker.com/engine/reference/commandline/events/#object-types
	/// </summary>
	public string[] ContainerEvents { get; set; }
	public string[] ImageEvents { get; set; }
	public string[] PluginEvents { get; set; }
	public string[] VolumeEvents { get; set; }
	public string[] DaemonEvents { get; set; }
	public string[] ServiceEvents { get; set; }
	public string[] NodeEvents { get; set; }
	public string[] SecretEvents { get; set; }
	public string[] ConfigEvents { get; set; }
}

public class GrafanaConfig
{
	public GrafanaConfig()
	{
		ApiKey = string.Empty;
		Uri = "http://localhost:3000";
	}

	public string ApiKey { get; set; }
	public string Uri { get; set; }
}

public class ObservabilityConfig
{
	public ObservabilityConfig()
	{
		Metrics = new Metrics();
		Tracing = new Tracing();
	}

	public Metrics Metrics { get; set; }
	public Tracing Tracing { get; set; }
}

public class Tracing
{
	public Tracing()
	{
		Url = "http://localhost";
	}

	public bool Enabled { get; set; }
	public string Url { get; set; }
}

public class Metrics
{
	public bool Enabled { get; set; }
	public int? Port { get; set; }
}

public class Developer
{
}

