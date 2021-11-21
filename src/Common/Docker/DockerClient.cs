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

    private readonly DockerDotNet.DockerClient _client;
    private readonly IGrafanaClient _grafanaClient;

    public DockerClient(IAppConfiguration config, IGrafanaClient grafanaClient)
    {
        var host = config.Docker.Host ?? "localhost:4243";
        _client = new DockerClientConfiguration(new Uri($"http://{host}"))
                                    .CreateClient();

        _grafanaClient = grafanaClient;
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
            _logger.Verbose("message was null");
            return;
        }
        
        _logger.Verbose("New event: {@Id} {@Action} {@From} {@Actor} {@Scope} {@Status} {@Type} {@Time}",
                        message.ID, message.Action, message.From, message.Actor, message.Scope, message.Status, message.Type, message.Time);

        if (message.Action == "start"
            || message.Action == "stop"
            || message.Action == "restart")
            _grafanaClient.CreateAnnotationAsync(message.Time, "Docker event", message.Action, message.From)
                .GetAwaiter().GetResult();
    }
}
