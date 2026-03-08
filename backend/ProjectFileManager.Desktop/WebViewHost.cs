// -*- coding: utf-8 -*-
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Eto.Forms;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ProjectFileManager.Core.Models;
using ProjectFileManager.Core.Services;
using Serilog;

namespace ProjectFileManager.Desktop;

/// <summary>
/// WebView 宿主，处理 JS ↔ C# 通信
/// </summary>
public class WebViewHost
{
    // 使用 camelCase 序列化以匹配 JavaScript 命名习惯
    private static readonly JsonSerializerSettings JsonSettings = new()
    {
        ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver()
    };
    private readonly FileService _fileService;
    private readonly FavoriteService _favoriteService;
    private readonly ConfigService _configService;
    private readonly string? _initialWorkDir;

    public WebView WebView { get; }

    public WebViewHost(FileService fileService, FavoriteService favoriteService, ConfigService configService, string? initialWorkDir = null)
    {
        _fileService = fileService;
        _favoriteService = favoriteService;
        _configService = configService;
        _initialWorkDir = initialWorkDir;

        WebView = new WebView
        {
            BrowserContextMenuEnabled = false
        };

        // 注册 JS Bridge
        WebView.DocumentLoading += OnDocumentLoading;
        WebView.DocumentLoaded += OnDocumentLoaded;
    }

    /// <summary>
    /// 加载 HTML 内容
    /// </summary>
    public void LoadContent()
    {
        var htmlContent = GetHtmlContent();
        WebView.LoadHtml(htmlContent);
    }

    /// <summary>
    /// 获取 HTML 内容（从嵌入资源或文件）
    /// </summary>
    private string GetHtmlContent()
    {
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        var pathsToTry = new List<string>();

        // 优先：start.sh 传入的项目目录下的 frontend（修改前端无需重新构建）
        if (!string.IsNullOrEmpty(_initialWorkDir) && Directory.Exists(_initialWorkDir))
        {
            var projectFrontend = Path.Combine(_initialWorkDir, "frontend");
            pathsToTry.Add(projectFrontend);
        }

        // 其余可能位置
        pathsToTry.Add(Path.Combine(baseDir, "WebContent"));
        pathsToTry.Add(Path.Combine(baseDir, "web"));
        pathsToTry.Add(Path.Combine(baseDir, "..", "Resources", "web"));
        pathsToTry.Add(Path.Combine(baseDir, "..", "..", "..", "..", "..", "frontend"));
        pathsToTry.Add(Path.Combine(baseDir, "..", "..", "..", "..", "frontend"));
        pathsToTry.Add(Path.Combine(baseDir, "..", "..", "..", "..", "web"));

        foreach (var webDir in pathsToTry)
        {
            var indexPath = Path.Combine(webDir, "index.html");
            if (File.Exists(indexPath))
            {
                Log.Information("加载 Web 内容: {WebDir}", Path.GetFullPath(webDir));
                
                var html = File.ReadAllText(indexPath, Encoding.UTF8);
                
                // 注入 CSS 和 JS
                var cssPath = Path.Combine(webDir, "css", "styles.css");
                var jsPath = Path.Combine(webDir, "js", "app.js");
                
                if (File.Exists(cssPath))
                {
                    var cssContent = File.ReadAllText(cssPath, Encoding.UTF8);
                    html = html.Replace("<!-- CSS_PLACEHOLDER -->", $"<style>{cssContent}</style>");
                }
                
                if (File.Exists(jsPath))
                {
                    var jsContent = File.ReadAllText(jsPath, Encoding.UTF8);
                    html = html.Replace("<!-- JS_PLACEHOLDER -->", $"<script>{jsContent}</script>");
                }
                
                return html;
            }
        }

        Log.Warning("未找到 Web 内容目录，已搜索路径: {Paths}", string.Join(", ", pathsToTry.Select(p => Path.GetFullPath(p))));
        
        // 从嵌入资源加载（发布模式）
        return GetEmbeddedHtml();
    }

    /// <summary>
    /// 获取嵌入的 HTML（用于发布版本）
    /// </summary>
    private string GetEmbeddedHtml()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceNames = assembly.GetManifestResourceNames();
        
        // 简化：返回内联 HTML（实际项目中应该读取嵌入资源）
        return GetInlineHtml();
    }

    /// <summary>
    /// 内联 HTML（作为后备方案）
    /// </summary>
    private string GetInlineHtml()
    {
        return @"
<!DOCTYPE html>
<html lang=""zh-CN"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>ProjectFileManager</title>
    <style>
        body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif; margin: 0; padding: 20px; }
        h1 { color: #1e40af; }
    </style>
</head>
<body>
    <h1>ProjectFileManager</h1>
    <p>Web 内容加载失败，请检查 web 目录是否存在。</p>
</body>
</html>";
    }

    private void OnDocumentLoading(object? sender, WebViewLoadingEventArgs e)
    {
        Log.Debug("WebView 加载: {Uri}, Scheme: {Scheme}", e.Uri, e.Uri?.Scheme);
        
        // 拦截自定义协议
        if (e.Uri?.Scheme == "bridge")
        {
            e.Cancel = true;
            Log.Debug("拦截 Bridge 调用: {Uri}", e.Uri);
            HandleBridgeCall(e.Uri);
        }
    }

    private void OnDocumentLoaded(object? sender, WebViewLoadedEventArgs e)
    {
        Log.Information("文档加载完成，注入 Bridge");
        
        try
        {
            // 先执行一个简单的测试脚本
            ExecuteScript("console.log('[C#] OnDocumentLoaded 触发');");
            
            // 注入 C# Bridge 对象
            InjectBridge();
            Log.Information("Bridge 注入完成");
            
            // 预加载初始数据
            PreloadInitialData();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "OnDocumentLoaded 处理失败");
        }
    }

    /// <summary>
    /// 预加载初始数据并注入到 JavaScript
    /// </summary>
    private void PreloadInitialData()
    {
        try
        {
            var homePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var downloadsPath = GetDownloadsPath();
            
            var itemsPerRow = _configService.GetItemsPerRow();
            var showHidden = _configService.GetShowHiddenFiles();
            var lastPath = _configService.GetLastOpenedPath();
            var sortSettings = _configService.GetSortSettings();
            
            // 启动目录优先级：保存的最后路径 > 启动参数工作目录 > 用户主目录
            var currentPath = homePath;
            if (!string.IsNullOrWhiteSpace(lastPath) && Directory.Exists(lastPath))
            {
                currentPath = lastPath;
            }
            else if (!string.IsNullOrWhiteSpace(_initialWorkDir) && Directory.Exists(_initialWorkDir))
            {
                currentPath = _initialWorkDir;
            }
            
            Log.Information("初始目录: {CurrentPath}", currentPath);
            
            // 获取目录树
            var tree = _fileService.GetDirectoryTree(currentPath, 1);
            var treeJson = JsonConvert.SerializeObject(tree, JsonSettings);
            var files = _fileService.GetFiles(currentPath, 1, 50);
            var filesJson = JsonConvert.SerializeObject(files, JsonSettings);
            
            var initScript = $@"
                console.log('[Bridge] 注入预加载数据');
                window._preloadedData = {{
                    homePath: {JsonConvert.SerializeObject(homePath)},
                    desktopPath: {JsonConvert.SerializeObject(desktopPath)},
                    documentsPath: {JsonConvert.SerializeObject(documentsPath)},
                    downloadsPath: {JsonConvert.SerializeObject(downloadsPath)},
                    itemsPerRow: {itemsPerRow},
                    showHiddenFiles: {(showHidden ? "true" : "false")},
                    sortBy: {JsonConvert.SerializeObject(sortSettings.SortBy)},
                    sortOrder: {JsonConvert.SerializeObject(sortSettings.SortOrder)},
                    lastOpenedPath: {JsonConvert.SerializeObject(lastPath ?? "")},
                    currentPath: {JsonConvert.SerializeObject(currentPath)},
                    directoryTree: {treeJson},
                    initialFiles: {filesJson}
                }};
                
                // 如果应用正在等待数据，通知它
                if (typeof window.onPreloadedDataReady === 'function') {{
                    window.onPreloadedDataReady();
                }}
            ";
            
            ExecuteScript(initScript);
            Log.Information("预加载数据注入完成");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "预加载数据失败");
            
            // 注入错误信息，让 JavaScript 知道出了问题
            try
            {
                var homePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                var errorScript = $@"
                    console.error('预加载数据失败: {ex.Message.Replace("'", "\\'")}');
                    window._preloadedData = {{
                        homePath: {JsonConvert.SerializeObject(homePath)},
                        currentPath: {JsonConvert.SerializeObject(homePath)},
                        itemsPerRow: 4,
                        showHiddenFiles: false,
                        directoryTree: {{ id: 'root', name: '主目录', path: {JsonConvert.SerializeObject(homePath)}, hasChildren: true, children: [] }},
                        initialFiles: {{ items: [], totalCount: 0, page: 1, pageSize: 50, hasNextPage: false }}
                    }};
                    if (typeof window.onPreloadedDataReady === 'function') {{
                        window.onPreloadedDataReady();
                    }}
                ";
                ExecuteScript(errorScript);
            }
            catch (Exception innerEx)
            {
                Log.Error(innerEx, "注入备用数据也失败了");
            }
        }
    }

    /// <summary>
    /// 注入 JavaScript Bridge（使用 document.title 通信）
    /// </summary>
    private void InjectBridge()
    {
        // 监听标题变化（用于 JS -> C# 通信）
        WebView.DocumentTitleChanged += OnDocumentTitleChanged;
        
        var bridgeScript = @"
            window._bridgeCallbacks = {};
            window._bridgeCallId = 0;
            window._bridgeQueue = [];
            
            window.csharp = {
                call: function(method, params) {
                    return new Promise((resolve, reject) => {
                        const callId = 'c' + (++window._bridgeCallId);
                        window._bridgeCallbacks[callId] = { resolve, reject, method: method };
                        
                        // 将请求加入队列
                        window._bridgeQueue.push({
                            id: callId,
                            method: method,
                            params: params || {}
                        });
                        
                        // 通过修改 title 通知 C#
                        document.title = 'BRIDGE:' + callId + ':' + method + ':' + encodeURIComponent(JSON.stringify(params || {}));
                        
                        console.log('[Bridge] 请求: ' + method + ' (' + callId + ')');
                        
                        // 超时处理
                        setTimeout(() => {
                            if (window._bridgeCallbacks[callId]) {
                                console.warn('[Bridge] 超时: ' + method);
                                window._bridgeCallbacks[callId].reject(new Error('超时'));
                                delete window._bridgeCallbacks[callId];
                            }
                        }, 30000);
                    });
                }
            };
            
            // C# 调用此函数返回结果
            window._bridgeResolve = function(callId, resultJson) {
                const cb = window._bridgeCallbacks[callId];
                if (cb) {
                    console.log('[Bridge] 成功: ' + cb.method);
                    cb.resolve(JSON.parse(resultJson));
                    delete window._bridgeCallbacks[callId];
                }
            };
            
            window._bridgeReject = function(callId, errorCode, errorMessage) {
                const cb = window._bridgeCallbacks[callId];
                if (cb) {
                    console.log('[Bridge] 失败: ' + cb.method + ' - ' + errorMessage);
                    const err = new Error(errorMessage);
                    err.code = errorCode;
                    cb.reject(err);
                    delete window._bridgeCallbacks[callId];
                }
            };
            
            console.log('[Bridge] Title 模式 Bridge 已注入');
            
            // 恢复标题
            setTimeout(() => { document.title = 'ProjectFileManager'; }, 100);
        ";
        
        ExecuteScript(bridgeScript);
        Log.Debug("Bridge 脚本已执行");
    }
    
    /// <summary>
    /// 处理标题变化（JS -> C# 通信）
    /// </summary>
    private void OnDocumentTitleChanged(object? sender, WebViewTitleEventArgs e)
    {
        var title = e.Title;
        
        if (string.IsNullOrEmpty(title) || !title.StartsWith("BRIDGE:"))
            return;
        
        try
        {
            // 解析: BRIDGE:callId:method:encodedParams
            var parts = title.Substring(7).Split(new[] { ':' }, 3);
            if (parts.Length < 3) return;
            
            var callId = parts[0];
            var method = parts[1];
            var paramsJson = Uri.UnescapeDataString(parts[2]);
            
            Log.Debug("Bridge 调用 (Title): {Method} ({CallId})", method, callId);
            
            // 异步处理，避免阻塞 UI
            Task.Run(() => ProcessBridgeCall(callId, method, paramsJson));
        }
        catch (Exception ex)
        {
            Log.Error(ex, "解析 Bridge 请求失败: {Title}", title);
        }
    }
    
    /// <summary>
    /// 处理单个 Bridge 调用
    /// </summary>
    private void ProcessBridgeCall(string callId, string method, string paramsJson)
    {
        try
        {
            var result = InvokeMethod(method, paramsJson);
            var resultJson = JsonConvert.SerializeObject(result, JsonSettings);
            
            // 返回结果到 JS
            var escapedJson = resultJson.Replace("\\", "\\\\").Replace("'", "\\'").Replace("\n", "\\n").Replace("\r", "\\r");
            var script = $"window._bridgeResolve('{callId}', '{escapedJson}');";
            
            Application.Instance.Invoke(() =>
            {
                try
                {
                    WebView.ExecuteScript(script);
                    // 恢复标题
                    WebView.ExecuteScript("document.title = 'ProjectFileManager';");
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "执行回调脚本失败");
                }
            });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Bridge 方法执行失败: {Method}", method);
            
            var errorCode = GetErrorCode(ex);
            var errorMessage = ex.Message.Replace("'", "\\'").Replace("\n", " ");
            var script = $"window._bridgeReject('{callId}', '{errorCode}', '{errorMessage}');";
            
            Application.Instance.Invoke(() =>
            {
                try
                {
                    WebView.ExecuteScript(script);
                    WebView.ExecuteScript("document.title = 'ProjectFileManager';");
                }
                catch { }
            });
        }
    }

    /// <summary>
    /// 处理来自 JS 的 Bridge 调用
    /// </summary>
    private void HandleBridgeCall(Uri uri)
    {
        var method = uri.Host;
        var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
        var callId = query["callId"] ?? "";
        var paramsJson = query["params"] ?? "{}";

        Log.Debug("Bridge 调用: {Method}", method);

        try
        {
            var result = InvokeMethod(method, paramsJson);
            var resultJson = JsonConvert.SerializeObject(result);
            
            // 回调 JS（成功）
            var callbackScript = $@"
                if (window['_callback_{callId}']) {{
                    window['_callback_{callId}'].resolve({resultJson});
                    delete window['_callback_{callId}'];
                }}
            ";
            ExecuteScript(callbackScript);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Bridge 调用失败: {Method}", method);
            
            // 返回结构化错误信息
            var errorInfo = new
            {
                errorCode = GetErrorCode(ex),
                errorMessage = ex.Message,
                errorType = ex.GetType().Name
            };
            var errorJson = JsonConvert.SerializeObject(errorInfo);
            
            var errorScript = $@"
                if (window['_callback_{callId}']) {{
                    var err = new Error({JsonConvert.SerializeObject(ex.Message)});
                    err.code = {JsonConvert.SerializeObject(errorInfo.errorCode)};
                    err.type = {JsonConvert.SerializeObject(errorInfo.errorType)};
                    window['_callback_{callId}'].reject(err);
                    delete window['_callback_{callId}'];
                }}
            ";
            ExecuteScript(errorScript);
        }
    }

    /// <summary>
    /// 获取收藏列表并转换为 FileItem 格式（不抛异常，失败返回空列表）
    /// </summary>
    private List<FileItem> GetFavoritesAsFileItemsSafe()
    {
        try
        {
            return GetFavoritesAsFileItems();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "获取收藏列表失败");
            return new List<FileItem>();
        }
    }

    /// <summary>
    /// 获取收藏列表并转换为 FileItem 格式
    /// </summary>
    private List<FileItem> GetFavoritesAsFileItems()
    {
        var favorites = _favoriteService.GetAllFavorites();
        var fileItems = new List<FileItem>();

        foreach (var fav in favorites)
        {
            var isDirectory = Directory.Exists(fav.FilePath);
            var exists = isDirectory || File.Exists(fav.FilePath);

            var item = new FileItem
            {
                Id = fav.Id,
                Name = fav.FileName,
                Path = fav.FilePath,
                IsDirectory = isDirectory,
                FileType = fav.FileType,
                IsFavorite = true,
                Size = 0,
                ModifiedAt = fav.FavoritedAt,
                Extension = isDirectory ? "" : System.IO.Path.GetExtension(fav.FilePath).TrimStart('.')
            };

            // 如果文件/文件夹存在，获取更多信息
            if (exists)
            {
                try
                {
                    if (isDirectory)
                    {
                        var dirInfo = new DirectoryInfo(fav.FilePath);
                        item.ModifiedAt = dirInfo.LastWriteTime;
                    }
                    else
                    {
                        var fileInfo = new FileInfo(fav.FilePath);
                        item.Size = fileInfo.Length;
                        item.ModifiedAt = fileInfo.LastWriteTime;
                    }
                }
                catch
                {
                    // 忽略错误，使用默认值
                }
            }

            fileItems.Add(item);
        }

        return fileItems;
    }

    /// <summary>
    /// 获取错误代码
    /// </summary>
    private static string GetErrorCode(Exception ex)
    {
        return ex switch
        {
            UnauthorizedAccessException => "ACCESS_DENIED",
            DirectoryNotFoundException => "DIRECTORY_NOT_FOUND",
            FileNotFoundException => "FILE_NOT_FOUND",
            IOException => "IO_ERROR",
            NotSupportedException => "NOT_SUPPORTED",
            ArgumentException => "INVALID_ARGUMENT",
            _ => "UNKNOWN_ERROR"
        };
    }

    /// <summary>
    /// 调用对应的 C# 方法
    /// </summary>
    private object? InvokeMethod(string method, string paramsJson)
    {
        var parameters = JsonConvert.DeserializeObject<Dictionary<string, object>>(paramsJson) 
            ?? new Dictionary<string, object>();

        return method switch
        {
            // 文件管理
            "getDirectoryTree" => _fileService.GetDirectoryTree(
                GetParam<string>(parameters, "rootPath", Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)),
                GetParam<int>(parameters, "depth", 1)
            ),
            "getFiles" => _fileService.GetFiles(
                GetParam<string>(parameters, "directoryPath", ""),
                GetParam<int>(parameters, "page", 1),
                GetParam<int>(parameters, "pageSize", 50)
            ),
            "openFile" => _fileService.OpenFile(
                GetParam<string>(parameters, "filePath", ""),
                GetParam<bool>(parameters, "withShift", false)
            ),
            "searchFiles" => _fileService.SearchFiles(
                GetParam<string>(parameters, "directoryPath", ""),
                GetParam<string>(parameters, "keyword", ""),
                GetParam<int>(parameters, "maxResults", 100)
            ),
            "revealInExplorer" => _fileService.RevealInExplorer(
                GetParam<string>(parameters, "filePath", "")
            ),

            // 收藏管理
            "toggleFavorite" => _favoriteService.ToggleFavorite(
                GetParam<string>(parameters, "filePath", "")
            ),
            "isFavorite" => _favoriteService.IsFavorite(
                GetParam<string>(parameters, "filePath", "")
            ),
            "getFavorites" => GetFavoritesAsFileItemsSafe(),

            // 配置管理
            "getConfig" => _configService.GetConfigValue(
                GetParam<string>(parameters, "key", "")
            ),
            "setConfig" => SetConfigAndReturn(
                GetParam<string>(parameters, "key", ""),
                GetParam<string>(parameters, "value", "")
            ),
            "getItemsPerRow" => _configService.GetItemsPerRow(),
            "setItemsPerRow" => SetItemsPerRowAndReturn(
                GetParam<int>(parameters, "count", 4)
            ),
            "getTheme" => _configService.GetTheme(),
            "setTheme" => SetThemeAndReturn(
                GetParam<string>(parameters, "theme", "light")
            ),
            "getShowHiddenFiles" => _configService.GetShowHiddenFiles(),
            "setShowHiddenFiles" => SetShowHiddenFilesAndReturn(
                GetParam<bool>(parameters, "show", false)
            ),
            "getLastOpenedPath" => _configService.GetLastOpenedPath(),
            "setLastOpenedPath" => SetLastOpenedPathAndReturn(
                GetParam<string>(parameters, "path", "")
            ),

            // 系统信息
            "getUserHome" => Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "getDesktop" => Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
            "getDocuments" => Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "getDownloads" => GetDownloadsPath(),
            "getWorkingDirectory" => Directory.GetCurrentDirectory(),

            _ => throw new NotSupportedException($"方法不支持: {method}")
        };
    }

    private T GetParam<T>(Dictionary<string, object> parameters, string key, T defaultValue)
    {
        if (!parameters.TryGetValue(key, out var value) || value == null)
            return defaultValue;

        // Newtonsoft 反序列化 Dictionary<string,object> 时值可能是 JToken
        if (value is JToken jt)
        {
            try
            {
                if (typeof(T) == typeof(string))
                    return (T)(object)(jt.Type == JTokenType.String ? jt.Value<string>() ?? "" : jt.ToString());
                if (typeof(T) == typeof(int))
                    return (T)(object)(jt.Value<int?>() ?? 0);
                if (typeof(T) == typeof(bool))
                    return (T)(object)(jt.Value<bool?>() ?? false);
                return (T)Convert.ChangeType(jt.ToString(), typeof(T));
            }
            catch
            {
                return defaultValue;
            }
        }

        try
        {
            if (typeof(T) == typeof(int) && value is long longVal)
                return (T)(object)(int)longVal;
            if (typeof(T) == typeof(int) && value is int intVal)
                return (T)(object)intVal;
            if (typeof(T) == typeof(bool) && value is bool boolVal)
                return (T)(object)boolVal;
            if (typeof(T) == typeof(string) && value is string strVal)
                return (T)(object)strVal;
            return (T)Convert.ChangeType(value, typeof(T));
        }
        catch
        {
            return defaultValue;
        }
    }

    // 辅助方法，用于 void 返回的配置操作
    private bool SetConfigAndReturn(string key, string value)
    {
        _configService.SetConfigValue(key, value);
        return true;
    }

    private int SetItemsPerRowAndReturn(int count)
    {
        _configService.SetItemsPerRow(count);
        return count;
    }

    private string SetThemeAndReturn(string theme)
    {
        _configService.SetTheme(theme);
        return theme;
    }

    private bool SetShowHiddenFilesAndReturn(bool show)
    {
        _configService.SetShowHiddenFiles(show);
        return show;
    }

    private string SetLastOpenedPathAndReturn(string path)
    {
        _configService.SetLastOpenedPath(path);
        return path;
    }

    private static string GetDownloadsPath()
    {
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return Path.Combine(userProfile, "Downloads");
    }

    /// <summary>
    /// 执行 JavaScript
    /// </summary>
    public void ExecuteScript(string script)
    {
        Application.Instance.Invoke(() =>
        {
            WebView.ExecuteScript(script);
        });
    }
}
