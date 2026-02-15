// -*- coding: utf-8 -*-
using System;
using System.IO;
using Serilog;
using Serilog.Events;

namespace ProjectFileManager.Core.Logging;

/// <summary>
/// 日志工厂 - 统一日志配置
/// </summary>
public static class LoggerFactory
{
    private static bool _initialized = false;
    private static readonly object _lock = new();

    /// <summary>
    /// 初始化日志系统
    /// </summary>
    public static void Initialize()
    {
        lock (_lock)
        {
            if (_initialized) return;

            var logDirectory = GetLogDirectory();
            var logFilePath = Path.Combine(logDirectory, "app-.log");

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .Enrich.WithProperty("Application", "ProjectFileManager")
                .WriteTo.Console(
                    outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
                .WriteTo.File(
                    logFilePath,
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 7,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}",
                    encoding: System.Text.Encoding.UTF8)
                .CreateLogger();

            _initialized = true;
            Log.Information("日志系统初始化完成，日志目录: {LogDirectory}", logDirectory);
        }
    }

    /// <summary>
    /// 获取日志目录
    /// </summary>
    private static string GetLogDirectory()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var logDir = Path.Combine(appData, "ProjectFileManager", "logs");
        
        if (!Directory.Exists(logDir))
        {
            Directory.CreateDirectory(logDir);
        }
        
        return logDir;
    }

    /// <summary>
    /// 关闭日志系统
    /// </summary>
    public static void Shutdown()
    {
        Log.CloseAndFlush();
    }
}
