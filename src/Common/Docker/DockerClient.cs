using Common.Grafana;
using Common.Observability;
using Docker.DotNet;
using Docker.DotNet.Models;
using Serilog;
using DockerDotNet = Docker.DotNet;

namespace Common.Docker;

public interface IDockerClientWrapper
{
    Task BeginEventMonitoringAsync();
    void EventHandler(Message message);
}

public class DockerClient : IDockerClientWrapper
{
    private static readonly ILogger _logger = LogContext.ForClass<DockerClient>();

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

        var shouldRecord = false;
        var annotationType = Grafana.Constants.ContainerEventType;
        var messageType = message.Type;

        if (string.Equals(messageType, Constants.ContainerEventTypeValue, StringComparison.OrdinalIgnoreCase) 
            && _containerActionsToRecord.Contains(message.Action))
        {
            shouldRecord = true;
            messageType = Grafana.Constants.ContainerEventType;
        } else if (string.Equals(messageType, Constants.ImageEventTypeValue, StringComparison.OrdinalIgnoreCase) 
            && _imageActionsToRecord.Contains(message.Action))
        {
            shouldRecord = true;
            messageType = Grafana.Constants.ImageEventType;
        } else if (string.Equals(messageType, Constants.PluginEventTypeValue, StringComparison.OrdinalIgnoreCase)
           && _pluginActionsToRecord.Contains(message.Action))
        {
            shouldRecord = true;
            messageType = Grafana.Constants.PluginEventType;
        } else if (string.Equals(messageType, Constants.VolumeEventTypeValue, StringComparison.OrdinalIgnoreCase)
           && _volumeActionsToRecord.Contains(message.Action))
        {
            shouldRecord = true;
            messageType = Grafana.Constants.VolumeEventType;
        } else if (string.Equals(messageType, Constants.DaemonEventTypeValue, StringComparison.OrdinalIgnoreCase)
           && _daemonActionsToRecord.Contains(message.Action))
        {
            shouldRecord = true;
            messageType = Grafana.Constants.DaemonEventType;
        } else if (string.Equals(messageType, Constants.ServiceEventTypeValue, StringComparison.OrdinalIgnoreCase)
           && _serviceActionsToRecord.Contains(message.Action))
        {
            shouldRecord = true;
            messageType = Grafana.Constants.ServiceEventType;
        } else if (string.Equals(messageType, Constants.NodeEventTypeValue, StringComparison.OrdinalIgnoreCase)
           && _nodeActionsToRecord.Contains(message.Action))
        {
            shouldRecord = true;
            messageType = Grafana.Constants.NodeEventType;
        } else if (string.Equals(messageType, Constants.ServiceEventTypeValue, StringComparison.OrdinalIgnoreCase)
           && _secretActionsToRecord.Contains(message.Action))
        {
            shouldRecord = true;
            messageType = Grafana.Constants.SecretEventType;
        } else if (string.Equals(messageType, Constants.ConfigEventTypeValue, StringComparison.OrdinalIgnoreCase)
           && _configActionsToRecord.Contains(message.Action))
        {
            shouldRecord = true;
            messageType = Grafana.Constants.ConfigEventType;
        }

        if (!shouldRecord) return;

        var imageName = message.Actor.Attributes.FirstOrDefault(a => string.Equals(a.Key, Constants.ImageNameKey, StringComparison.OrdinalIgnoreCase)).Value;
        var imageTag = message.Actor.Attributes.FirstOrDefault(a => string.Equals(a.Key, Constants.ImageVersionKey, StringComparison.OrdinalIgnoreCase)).Value;

        var annotation = $"{message.Action} {imageName}:{imageTag}";

        _grafanaClient.CreateAnnotationAsync(message.TimeNano, annotation, annotationType, message.Action, message.From)
            .GetAwaiter().GetResult();
    }
}
