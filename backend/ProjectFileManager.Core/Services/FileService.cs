// -*- coding: utf-8 -*-
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using ProjectFileManager.Core.Models;
using Serilog;

namespace ProjectFileManager.Core.Services;

/// <summary>
/// 文件管理服务
/// </summary>
public class FileService
{
    private readonly FavoriteService _favoriteService;
    private readonly ConfigService _configService;

    public FileService(FavoriteService favoriteService, ConfigService configService)
    {
        _favoriteService = favoriteService;
        _configService = configService;
    }

    /// <summary>
    /// 获取目录树结构
    /// </summary>
    /// <param name="rootPath">根路径</param>
    /// <param name="depth">展开深度（默认1层）</param>
    public DirectoryNode GetDirectoryTree(string rootPath, int depth = 1)
    {
        if (string.IsNullOrEmpty(rootPath))
        {
            rootPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        }

        if (!Directory.Exists(rootPath))
        {
            throw new DirectoryNotFoundException($"目录不存在: {rootPath}");
        }

        return BuildDirectoryNode(rootPath, depth);
    }

    /// <summary>
    /// 构建目录节点
    /// </summary>
    private DirectoryNode BuildDirectoryNode(string path, int depth)
    {
        var dirInfo = new DirectoryInfo(path);
        var showHidden = _configService.GetConfigValue(ConfigKeys.ShowHiddenFiles) == "true";

        var node = new DirectoryNode
        {
            Id = GenerateId(path),
            Name = dirInfo.Name,
            Path = path,
            HasChildren = HasSubDirectories(path, showHidden)
        };

        if (depth > 0)
        {
            try
            {
                var subDirs = dirInfo.GetDirectories()
                    .Where(d => showHidden || !d.Attributes.HasFlag(FileAttributes.Hidden))
                    .OrderBy(d => d.Name, StringComparer.OrdinalIgnoreCase);

                foreach (var subDir in subDirs)
                {
                    try
                    {
                        node.Children.Add(BuildDirectoryNode(subDir.FullName, depth - 1));
                    }
                    catch (UnauthorizedAccessException)
                    {
                        // 忽略无权限访问的目录
                    }
                }
            }
            catch (UnauthorizedAccessException)
            {
                // 忽略无权限访问的目录
            }
        }

        return node;
    }

    /// <summary>
    /// 检查是否有子目录
    /// </summary>
    private static bool HasSubDirectories(string path, bool showHidden)
    {
        try
        {
            return Directory.EnumerateDirectories(path)
                .Any(d =>
                {
                    if (showHidden) return true;
                    var attr = File.GetAttributes(d);
                    return !attr.HasFlag(FileAttributes.Hidden);
                });
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 获取文件列表（分页，支持懒加载）
    /// </summary>
    public PagedResult<FileItem> GetFiles(string directoryPath, int page = 1, int pageSize = 50)
    {
        if (!Directory.Exists(directoryPath))
        {
            return new PagedResult<FileItem> { Items = new List<FileItem>(), TotalCount = 0, Page = page, PageSize = pageSize };
        }

        var showHidden = _configService.GetConfigValue(ConfigKeys.ShowHiddenFiles) == "true";
        var sortBy = _configService.GetConfigValue(ConfigKeys.SortBy) ?? "name";
        var sortOrder = _configService.GetConfigValue(ConfigKeys.SortOrder) ?? "asc";

        var dirInfo = new DirectoryInfo(directoryPath);
        
        // 获取所有目录
        var directories = dirInfo.GetDirectories()
            .Where(d => showHidden || !d.Attributes.HasFlag(FileAttributes.Hidden))
            .Select(d => CreateFileItem(d))
            .ToList();

        // 获取所有文件
        var files = dirInfo.GetFiles()
            .Where(f => showHidden || !f.Attributes.HasFlag(FileAttributes.Hidden))
            .Select(f => CreateFileItem(f))
            .ToList();

        // 合并并排序
        var allItems = directories.Concat(files).ToList();
        allItems = SortItems(allItems, sortBy, sortOrder);

        // 标记收藏状态
        var favoritePaths = _favoriteService.GetAllFavoritePaths();
        foreach (var item in allItems)
        {
            item.IsFavorite = favoritePaths.Contains(item.Path);
        }

        var totalCount = allItems.Count;
        var pagedItems = allItems
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PagedResult<FileItem>
        {
            Items = pagedItems,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    /// <summary>
    /// 排序文件列表
    /// </summary>
    private static List<FileItem> SortItems(List<FileItem> items, string sortBy, string sortOrder)
    {
        var isAsc = sortOrder.Equals("asc", StringComparison.OrdinalIgnoreCase);

        // 目录始终在前
        var directories = items.Where(i => i.IsDirectory);
        var files = items.Where(i => !i.IsDirectory);

        IOrderedEnumerable<FileItem> sortedDirs;
        IOrderedEnumerable<FileItem> sortedFiles;

        switch (sortBy.ToLowerInvariant())
        {
            case "name":
                sortedDirs = isAsc
                    ? directories.OrderBy(i => i.Name, StringComparer.OrdinalIgnoreCase)
                    : directories.OrderByDescending(i => i.Name, StringComparer.OrdinalIgnoreCase);
                sortedFiles = isAsc
                    ? files.OrderBy(i => i.Name, StringComparer.OrdinalIgnoreCase)
                    : files.OrderByDescending(i => i.Name, StringComparer.OrdinalIgnoreCase);
                break;
            case "size":
                sortedDirs = directories.OrderBy(i => i.Name, StringComparer.OrdinalIgnoreCase);
                sortedFiles = isAsc
                    ? files.OrderBy(i => i.Size)
                    : files.OrderByDescending(i => i.Size);
                break;
            case "date":
                sortedDirs = isAsc
                    ? directories.OrderBy(i => i.ModifiedAt)
                    : directories.OrderByDescending(i => i.ModifiedAt);
                sortedFiles = isAsc
                    ? files.OrderBy(i => i.ModifiedAt)
                    : files.OrderByDescending(i => i.ModifiedAt);
                break;
            case "type":
                sortedDirs = directories.OrderBy(i => i.Name, StringComparer.OrdinalIgnoreCase);
                sortedFiles = isAsc
                    ? files.OrderBy(i => i.Extension).ThenBy(i => i.Name, StringComparer.OrdinalIgnoreCase)
                    : files.OrderByDescending(i => i.Extension).ThenBy(i => i.Name, StringComparer.OrdinalIgnoreCase);
                break;
            default:
                sortedDirs = directories.OrderBy(i => i.Name, StringComparer.OrdinalIgnoreCase);
                sortedFiles = files.OrderBy(i => i.Name, StringComparer.OrdinalIgnoreCase);
                break;
        }

        return sortedDirs.Concat(sortedFiles).ToList();
    }

    /// <summary>
    /// 搜索文件
    /// </summary>
    public List<FileItem> SearchFiles(string directoryPath, string keyword, int maxResults = 100)
    {
        if (string.IsNullOrEmpty(keyword) || !Directory.Exists(directoryPath))
        {
            return new List<FileItem>();
        }

        var results = new List<FileItem>();
        var showHidden = _configService.GetConfigValue(ConfigKeys.ShowHiddenFiles) == "true";

        try
        {
            var searchPattern = $"*{keyword}*";
            var options = new EnumerationOptions
            {
                IgnoreInaccessible = true,
                RecurseSubdirectories = true,
                MaxRecursionDepth = 5,
                AttributesToSkip = showHidden ? FileAttributes.System : FileAttributes.Hidden | FileAttributes.System
            };

            // 搜索目录
            var directories = Directory.EnumerateDirectories(directoryPath, searchPattern, options)
                .Take(maxResults / 2)
                .Select(d => CreateFileItem(new DirectoryInfo(d)));

            // 搜索文件
            var files = Directory.EnumerateFiles(directoryPath, searchPattern, options)
                .Take(maxResults / 2)
                .Select(f => CreateFileItem(new FileInfo(f)));

            results = directories.Concat(files).Take(maxResults).ToList();
        }
        catch (Exception)
        {
            // 搜索过程中可能遇到权限问题，忽略
        }

        // 标记收藏状态
        var favoritePaths = _favoriteService.GetAllFavoritePaths();
        foreach (var item in results)
        {
            item.IsFavorite = favoritePaths.Contains(item.Path);
        }

        return results;
    }

    /// <summary>
    /// 打开文件
    /// </summary>
    public bool OpenFile(string filePath, bool withShift = false)
    {
        if (!File.Exists(filePath) && !Directory.Exists(filePath))
        {
            Log.Warning("文件不存在: {FilePath}", filePath);
            return false;
        }

        try
        {
            Log.Information("打开文件: {FilePath}, Shift键: {WithShift}", filePath, withShift);
            var psi = new ProcessStartInfo
            {
                FileName = filePath,
                UseShellExecute = true
            };

            // Shift 键按下时：在资源管理器/Finder 中显示
            if (withShift)
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    psi.FileName = "explorer.exe";
                    psi.Arguments = $"/select,\"{filePath}\"";
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    psi.FileName = "open";
                    psi.Arguments = $"-R \"{filePath}\"";
                }
            }

            Process.Start(psi);
            return true;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "打开文件失败: {FilePath}", filePath);
            return false;
        }
    }

    /// <summary>
    /// 在文件管理器中显示
    /// </summary>
    public bool RevealInExplorer(string filePath)
    {
        return OpenFile(filePath, withShift: true);
    }

    /// <summary>
    /// 复制文件路径到剪贴板
    /// </summary>
    public string GetFilePath(string filePath)
    {
        return filePath;
    }

    /// <summary>
    /// 创建 FileItem 从 DirectoryInfo
    /// </summary>
    private FileItem CreateFileItem(DirectoryInfo dirInfo)
    {
        return new FileItem
        {
            Id = GenerateId(dirInfo.FullName),
            Name = dirInfo.Name,
            Path = dirInfo.FullName,
            Extension = "",
            Size = 0,
            CreatedAt = dirInfo.CreationTime,
            ModifiedAt = dirInfo.LastWriteTime,
            IsDirectory = true,
            FileType = "folder"
        };
    }

    /// <summary>
    /// 创建 FileItem 从 FileInfo
    /// </summary>
    private FileItem CreateFileItem(FileInfo fileInfo)
    {
        var extension = fileInfo.Extension.TrimStart('.').ToLowerInvariant();
        return new FileItem
        {
            Id = GenerateId(fileInfo.FullName),
            Name = fileInfo.Name,
            Path = fileInfo.FullName,
            Extension = extension,
            Size = fileInfo.Length,
            CreatedAt = fileInfo.CreationTime,
            ModifiedAt = fileInfo.LastWriteTime,
            IsDirectory = false,
            FileType = FileItem.GetFileType(extension)
        };
    }

    /// <summary>
    /// 生成唯一ID
    /// </summary>
    private static string GenerateId(string path)
    {
        using var md5 = MD5.Create();
        var hash = md5.ComputeHash(Encoding.UTF8.GetBytes(path));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
