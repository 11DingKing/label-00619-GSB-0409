// -*- coding: utf-8 -*-
using System;
using Eto.Forms;
using ProjectFileManager.Core.Logging;
using ProjectFileManager.Desktop;
using Serilog;

namespace ProjectFileManager.Mac;

/// <summary>
/// macOS 平台入口
/// </summary>
internal class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        // 设置 UTF-8 编码
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        // 初始化日志系统
        LoggerFactory.Initialize();
        Log.Information("应用程序启动 (macOS Cocoa)");

        // 获取工作目录（优先级：命令行参数 > 临时文件 > 环境变量）
        string? workDir = null;
        
        // 1. 检查命令行参数
        foreach (var arg in args)
        {
            if (arg.StartsWith("--workdir="))
            {
                workDir = arg.Substring("--workdir=".Length);
            }
        }
        
        // 2. 检查临时文件（start.sh 写入）
        if (string.IsNullOrEmpty(workDir))
        {
            var workDirFile = "/tmp/projectfilemanager_workdir";
            if (System.IO.File.Exists(workDirFile))
            {
                try
                {
                    workDir = System.IO.File.ReadAllText(workDirFile).Trim();
                    // 读取后删除文件，避免下次启动时使用旧路径
                    System.IO.File.Delete(workDirFile);
                }
                catch { }
            }
        }
        
        // 3. 检查环境变量
        if (string.IsNullOrEmpty(workDir))
        {
            workDir = Environment.GetEnvironmentVariable("PROJECTFILEMANAGER_WORKDIR");
        }
        
        if (!string.IsNullOrEmpty(workDir))
        {
            Log.Information("启动工作目录: {WorkDir}", workDir);
        }

        try
        {
            // 创建 Mac 平台应用
            var platform = new Eto.Mac.Platform();
            
            using var app = new Application(platform);
            
            // 设置未处理异常处理器
            app.UnhandledException += OnUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += OnDomainUnhandledException;

            // 运行主窗口
            app.Run(new MainForm(workDir));
        }
        finally
        {
            Log.Information("应用程序退出");
            LoggerFactory.Shutdown();
        }
    }

    private static void OnUnhandledException(object? sender, Eto.UnhandledExceptionEventArgs e)
    {
        var ex = e.ExceptionObject as Exception;
        var message = ex != null
            ? $"发生未处理的异常:\n\n{ex.Message}\n\n{ex.StackTrace}"
            : "发生未知错误";

        Log.Error(ex, "未处理的异常");
        MessageBox.Show(message, "错误", MessageBoxButtons.OK, MessageBoxType.Error);
    }

    private static void OnDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        var ex = e.ExceptionObject as Exception;
        var message = ex != null
            ? $"发生严重错误:\n\n{ex.Message}"
            : "发生未知严重错误";

        Log.Fatal(ex, "严重未处理的异常");
        Console.Error.WriteLine(message);
    }
}
