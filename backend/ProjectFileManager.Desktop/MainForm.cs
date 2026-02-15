// -*- coding: utf-8 -*-
using System;
using System.IO;
using Eto.Forms;
using Eto.Drawing;
using ProjectFileManager.Core.Data;
using ProjectFileManager.Core.Services;
using Serilog;

namespace ProjectFileManager.Desktop;

/// <summary>
/// 主窗口
/// </summary>
public class MainForm : Form
{
    private readonly WebViewHost _webViewHost;
    private readonly DatabaseContext _dbContext;
    private readonly ConfigService _configService;
    private readonly FavoriteService _favoriteService;
    private readonly FileService _fileService;

    public MainForm() : this(null)
    {
    }

    public MainForm(string? initialWorkDir)
    {
        Log.Information("初始化主窗口");
        
        // 初始化数据库和服务
        Log.Debug("初始化服务...");
        _dbContext = new DatabaseContext();
        _configService = new ConfigService(_dbContext);
        _favoriteService = new FavoriteService(_dbContext);
        _fileService = new FileService(_favoriteService, _configService);
        Log.Debug("服务初始化完成");

        // 初始化 WebView 宿主
        _webViewHost = new WebViewHost(_fileService, _favoriteService, _configService, initialWorkDir);

        InitializeForm();
    }

    private void InitializeForm()
    {
        // 窗口基本设置
        Title = "ProjectFileManager - 项目文件管理器";
        MinimumSize = new Size(1024, 768);
        Size = new Size(1280, 800);

        // 设置图标（如果有）
        // Icon = ...

        // 将 WebView 作为主要内容
        Content = _webViewHost.WebView;

        // 窗口居中
        Location = new Point(
            (int)((Screen.PrimaryScreen.WorkingArea.Width - Width) / 2),
            (int)((Screen.PrimaryScreen.WorkingArea.Height - Height) / 2)
        );

        // 窗口关闭事件
        Closing += (sender, e) =>
        {
            Log.Information("关闭主窗口");
            _dbContext?.Dispose();
        };

        // 加载 Web 内容
        _webViewHost.LoadContent();
    }

    /// <summary>
    /// 刷新当前目录
    /// </summary>
    public void RefreshCurrentDirectory()
    {
        _webViewHost.ExecuteScript("window.app?.refresh?.()");
    }
}
