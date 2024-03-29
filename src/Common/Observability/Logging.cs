﻿using Serilog;

namespace Common.Observability;

public class LogContext
{
	public static ILogger ForClass<T>() => Log.Logger.ForContext(Serilog.Core.Constants.SourceContextPropertyName, typeof(T).Name);
	public static ILogger ForStatic(string name) => Log.Logger.ForContext(Serilog.Core.Constants.SourceContextPropertyName, name);
}

public static class Logging
{
	public static void LogSystemInformation()
	{
		Log.Information("*********************************************");
		Log.Information("Version: {@AppName} {@Version}", Statics.AppType, Constants.AppVersion);
		Log.Information("Operating System: {@Os}", SystemInformation.OS);
		Log.Information("OS Version: {@OsVersion}", SystemInformation.OSVersion);
		Log.Information("DotNet Runtime: {@DotnetRuntime}", SystemInformation.OSVersion);
		Log.Information("Docker Deployment: {@IsDocker}", SystemInformation.RunningInDocker);
		Log.Information("*********************************************");
	}
}
