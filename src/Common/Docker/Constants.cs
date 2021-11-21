namespace Common.Docker;

internal class Constants
{
    public const string ImageNameKey = "image";
    public const string ImageVersionKey = "org.opencontainers.image.version";

    public const string ContainerEventTypeValue = "container";
    public const string ImageEventTypeValue = "image";
    public const string PluginEventTypeValue = "plugin";
    public const string VolumeEventTypeValue = "volume";
    public const string DaemonEventTypeValue = "daemon";
    public const string ServiceEventTypeValue = "service";
    public const string NodeEventTypeValue = "node";
    public const string SecretEventTypeValue = "secret";
    public const string ConfigEventTypeValue = "config";
}
