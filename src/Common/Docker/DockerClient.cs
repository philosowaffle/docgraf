using Common.Grafana;
using Common.Observability;
using Docker.DotNet;
using Docker.DotNet.Models;
using Serilog;
using DockerDotNet = Docker.DotNet;
using Prometheus;
using PromMetrics = Prometheus.Metrics;

namespace Common.Docker;

public interface IDockerClientWrapper
{
	Task BeginEventMonitoringAsync();
	void EventHandler(Message message);
}

public class DockerClient : IDockerClientWrapper
{
	private static readonly ILogger _logger = LogContext.ForClass<DockerClient>();
	private static readonly Counter DockerEventsReceived = PromMetrics.CreateCounter($"{Observability.Metrics.Prefix}_docker_events_recv", "Counter of docker events recv.", new CounterConfiguration()
	{
		LabelNames = new[] { Observability.Metrics.Label.DockerEventType, Observability.Metrics.Label.DockerEvent, Observability.Metrics.Label.DockerContainer, Observability.Metrics.Label.DockerImage, Observability.Metrics.Label.DockerImageTag }
	});
	private static readonly Counter DockerEventsRecorded = PromMetrics.CreateCounter($"{Observability.Metrics.Prefix}_docker_events_recorded", "Counter of docker events recorded to Grafana.", new CounterConfiguration()
	{
		LabelNames = new[] { Observability.Metrics.Label.DockerEventType, Observability.Metrics.Label.DockerEvent, Observability.Metrics.Label.DockerContainer, Observability.Metrics.Label.DockerImage, Observability.Metrics.Label.DockerImageTag }
	});

	private readonly HashSet<string> _containerActionsToRecord;
	private readonly HashSet<string> _imageActionsToRecord;
	private readonly HashSet<string> _pluginActionsToRecord;
	private readonly HashSet<string> _volumeActionsToRecord;
	private readonly HashSet<string> _daemonActionsToRecord;
	private readonly HashSet<string> _serviceActionsToRecord;
	private readonly HashSet<string> _nodeActionsToRecord;
	private readonly HashSet<string> _secretActionsToRecord;
	private readonly HashSet<string> _configActionsToRecord;

	private readonly DockerDotNet.DockerClient _client;
	private readonly IGrafanaClient _grafanaClient;

	public DockerClient(IAppConfiguration config, IGrafanaClient grafanaClient)
	{
		_grafanaClient = grafanaClient;
		_client = new DockerClientConfiguration(new Uri(config.Docker.Uri))
									.CreateClient();

		_containerActionsToRecord = config.Docker.ContainerEvents.ToHashSet();
		_imageActionsToRecord = config.Docker.ImageEvents.ToHashSet();
		_pluginActionsToRecord = config.Docker.PluginEvents.ToHashSet();
		_volumeActionsToRecord = config.Docker.VolumeEvents.ToHashSet();
		_daemonActionsToRecord = config.Docker.DaemonEvents.ToHashSet();
		_secretActionsToRecord = config.Docker.SecretEvents.ToHashSet();
		_serviceActionsToRecord = config.Docker.ServiceEvents.ToHashSet();
		_nodeActionsToRecord = config.Docker.NodeEvents.ToHashSet();
		_configActionsToRecord = config.Docker.ConfigEvents.ToHashSet();
	}

	public Task BeginEventMonitoringAsync()
	{
		try
		{
			return _client.System.MonitorEventsAsync(new ContainerEventsParameters(), new Progress<Message>(EventHandler), CancellationToken.None);
		} catch (Exception ex)
		{
			_logger.Error(ex, "Monitoring Docker events failed.");
			return Task.CompletedTask;
		}
	}

	public void EventHandler(Message message)
	{
		using var tracing = Observability.Tracing.Trace($"{nameof(DockerClient)}.{nameof(EventHandler)}")
							?.WithTag("docker.event.type", message.Type)
							?.WithTag("docker.event.action", message.Action)
							?.WithTag("docker.event.id", message.ID)
							?.WithTag("docker.event.from", message.From)
							?.WithTag("docker.event.actor.id", message.Actor.ID)
							?.WithTag("docker.event.scope", message.Scope)
							?.WithTag("docker.event.status", message.Status);

		if (message == null)
		{
			_logger.Warning("Docker Event Message was null.");
			return;
		}
		
		_logger.Verbose("New event: {@Id} {@Action} {@From} {@Actor} {@Scope} {@Status} {@Type} {@Time}",
						message.ID, message.Action, message.From, message.Actor, message.Scope, message.Status, message.Type, message.Time);

		var action = CleanAction(message.Action);
		var shouldRecord = ShouldRecordToGrafana(action);
		var messageType = MapToGrafanaEventType(message.Type);

		var containerName = message.Actor.Attributes.FirstOrDefault(a => string.Equals(a.Key, "name", StringComparison.OrdinalIgnoreCase)).Value;
		var imageName = message.Actor.Attributes.FirstOrDefault(a => string.Equals(a.Key, Constants.ImageNameKey, StringComparison.OrdinalIgnoreCase)).Value;
		var imageTag = message.Actor.Attributes.FirstOrDefault(a => string.Equals(a.Key, Constants.ImageVersionKey, StringComparison.OrdinalIgnoreCase)).Value;

		DockerEventsReceived.WithLabels(messageType, action, containerName, imageName, imageTag ?? "latest").Inc();

		if (!shouldRecord) return;

		DockerEventsRecorded.WithLabels(messageType, action, containerName, imageName, imageTag ?? "latest").Inc();

		var annotation = $"{action} {containerName} {imageTag}";

		_grafanaClient.CreateAnnotationAsync(message.TimeNano, annotation, messageType, action, containerName, imageName)
			.GetAwaiter().GetResult();
	}

	private string MapToGrafanaEventType(string messageType)
	{
		if (string.Equals(messageType, Constants.ContainerEventTypeValue, StringComparison.OrdinalIgnoreCase))
		{
			return Grafana.Constants.ContainerEventType;

		} else if (string.Equals(messageType, Constants.ImageEventTypeValue, StringComparison.OrdinalIgnoreCase))
		{
			return Grafana.Constants.ImageEventType;

		} else if (string.Equals(messageType, Constants.PluginEventTypeValue, StringComparison.OrdinalIgnoreCase))
		{
			return Grafana.Constants.PluginEventType;

		} else if (string.Equals(messageType, Constants.VolumeEventTypeValue, StringComparison.OrdinalIgnoreCase))
		{
			return Grafana.Constants.VolumeEventType;

		} else if (string.Equals(messageType, Constants.DaemonEventTypeValue, StringComparison.OrdinalIgnoreCase))
		{
			return Grafana.Constants.DaemonEventType;

		} else if (string.Equals(messageType, Constants.ServiceEventTypeValue, StringComparison.OrdinalIgnoreCase))
		{
			return Grafana.Constants.ServiceEventType;

		} else if (string.Equals(messageType, Constants.NodeEventTypeValue, StringComparison.OrdinalIgnoreCase))
		{
			return Grafana.Constants.NodeEventType;

		} else if (string.Equals(messageType, Constants.ServiceEventTypeValue, StringComparison.OrdinalIgnoreCase))
		{
			return Grafana.Constants.SecretEventType;

		} else if (string.Equals(messageType, Constants.ConfigEventTypeValue, StringComparison.OrdinalIgnoreCase))
		{
			return Grafana.Constants.ConfigEventType;
		}

		_logger.Information($"Found unhandled message type: {messageType}");
		return messageType;
	}

	private bool ShouldRecordToGrafana(string action)
	{
		if (_containerActionsToRecord.Contains(action)
			|| _imageActionsToRecord.Contains(action)
			|| _pluginActionsToRecord.Contains(action)
			|| _volumeActionsToRecord.Contains(action)
			|| _daemonActionsToRecord.Contains(action)
			|| _serviceActionsToRecord.Contains(action)
			|| _nodeActionsToRecord.Contains(action)
			|| _secretActionsToRecord.Contains(action)
			|| _configActionsToRecord.Contains(action))
		{
			return true;

		}

		return false;
	}
	
	private string CleanAction(string action)
	{
		return action.Split(":").FirstOrDefault() ?? action;
	}
}
