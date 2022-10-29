using Common;
using Common.Observability;
using Core.Grafana;
using Core.Settings;
using Docker.DotNet;
using Docker.DotNet.Models;
using Prometheus;
using Serilog;
using DockerDotNet = Docker.DotNet;
using Traces = Common.Observability.Traces;

namespace Core.Docker;

public interface IDockerClientWrapper
{
	Task BeginEventMonitoringAsync(CancellationToken cancelToken);
	Task EventHandlerAsync(Message message);
}

public class DockerClient : IDockerClientWrapper
{
	private static readonly ILogger _logger = LogContext.ForClass<DockerClient>();
	private static readonly Counter DockerEventsReceived = Prometheus.Metrics.CreateCounter($"{Statics.MetricPrefix}_docker_events_recv", "Counter of docker events recv.", new CounterConfiguration()
	{
		LabelNames = new[] { Label.DockerEventType, Label.DockerEvent, Label.DockerContainer, Label.DockerImage, Label.DockerImageTag }
	});
	private static readonly Counter DockerEventsRecorded = Prometheus.Metrics.CreateCounter($"{Statics.MetricPrefix}_docker_events_recorded", "Counter of docker events recorded to Grafana.", new CounterConfiguration()
	{
		LabelNames = new[] { Label.DockerEventType, Label.DockerEvent, Label.DockerContainer, Label.DockerImage, Label.DockerImageTag }
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

	private static readonly IReadOnlyDictionary<string, string> DockerToGrafanaEventMapping = new Dictionary<string, string>()
	{
		{ Constants.ContainerEventTypeValue, Grafana.Constants.ContainerEventType },
		{ Constants.ImageEventTypeValue, Grafana.Constants.ImageEventType },
		{ Constants.PluginEventTypeValue, Grafana.Constants.PluginEventType },
		{ Constants.VolumeEventTypeValue, Grafana.Constants.VolumeEventType },
		{ Constants.DaemonEventTypeValue, Grafana.Constants.DaemonEventType },
		{ Constants.ServiceEventTypeValue, Grafana.Constants.ServiceEventType },
		{ Constants.NodeEventTypeValue, Grafana.Constants.NodeEventType },
		{ Constants.SecretEventTypeValue, Grafana.Constants.SecretEventType },
		{ Constants.ConfigEventTypeValue, Grafana.Constants.ConfigEventType },
		{ Constants.NetworkEventTypeValue, Grafana.Constants.NetworkEventType }
	};

	private readonly DockerDotNet.DockerClient _client;
	private readonly IGrafanaClient _grafanaClient;

	public DockerClient(ISettingsService settingsService, IGrafanaClient grafanaClient)
	{
		_grafanaClient = grafanaClient;

		var config = settingsService.GetSettingsAsync().GetAwaiter().GetResult();

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

	public Task BeginEventMonitoringAsync(CancellationToken cancelToken)
	{
		try
		{
			return _client.System.MonitorEventsAsync(new ContainerEventsParameters(), new Progress<Message>(async (m) => await EventHandlerAsync(m)), cancelToken);
		} catch (Exception ex)
		{
			_logger.Error(ex, "Monitoring Docker events failed.");
			return Task.CompletedTask;
		}
	}

	public async Task EventHandlerAsync(Message message)
	{
		try
		{
			Traces.StartNewActiviy("EventHandler");
			using var tracing = Traces.Trace($"{nameof(DockerClient)}.{nameof(EventHandlerAsync)}")
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

			message.Actor.Attributes.TryGetValue(Constants.ContainerNameKey, out var containerName);
			containerName = containerName ?? "unknown";

			message.Actor.Attributes.TryGetValue(Constants.ImageNameKey, out var imageName);
			imageName = imageName ?? "unknown";

			message.Actor.Attributes.TryGetValue(Constants.ImageVersionKey, out var imageTag);
			imageTag = imageTag ?? "unknown";

			DockerEventsReceived.WithLabels(messageType, action, containerName, imageName, imageTag).Inc();

			if (!shouldRecord) return;

			DockerEventsRecorded.WithLabels(messageType, action, containerName, imageName, imageTag).Inc();

			var annotation = $"{action} {containerName} {imageTag}";

			await _grafanaClient.CreateAnnotationAsync(message.TimeNano, annotation, messageType, action, containerName, imageName);

		} finally
		{
			Traces.EndCurrentActivity();
		}
	}

	private string MapToGrafanaEventType(string messageType)
	{
		using var tracing = Traces.Trace($"{nameof(DockerClient)}.{nameof(MapToGrafanaEventType)}");

		if (string.IsNullOrEmpty(messageType))
		{
			_logger.Information($"Found null/empty message type.");
			return "null";
		}

		if (DockerToGrafanaEventMapping.TryGetValue(messageType.ToLowerInvariant(), out var grafanaEventType))
			return grafanaEventType;

		_logger.Information($"Found unhandled message type: {messageType}");
		return messageType;
	}

	private bool ShouldRecordToGrafana(string action)
	{
		using var tracing = Traces.Trace($"{nameof(DockerClient)}.{nameof(ShouldRecordToGrafana)}");

		var shouldRecord = false;
		if (_containerActionsToRecord.Contains(action)
			|| _imageActionsToRecord.Contains(action)
			|| _pluginActionsToRecord.Contains(action)
			|| _volumeActionsToRecord.Contains(action)
			|| _daemonActionsToRecord.Contains(action)
			|| _serviceActionsToRecord.Contains(action)
			|| _nodeActionsToRecord.Contains(action)
			|| _secretActionsToRecord.Contains(action)
			|| _configActionsToRecord.Contains(action))
			shouldRecord = true;

		tracing?.AddTag("docgraf.shouldRecordEvent", shouldRecord);
		return shouldRecord;
	}

	private string CleanAction(string action)
	{
		using var tracing = Traces.Trace($"{nameof(DockerClient)}.{nameof(CleanAction)}");

		if (string.IsNullOrEmpty(action))
		{
			_logger.Information($"Found null/empty action.");
			return "null";
		}

		return action.Split(":").FirstOrDefault() ?? action;
	}
}
