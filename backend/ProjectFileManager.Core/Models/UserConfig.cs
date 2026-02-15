// -*- coding: utf-8 -*-
using System;

namespace ProjectFileManager.Core.Models;

/// <summary>
/// 用户配置模型
/// </summary>
public class UserConfig
{
    /// <summary>
    /// 配置键
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// 配置值
    /// </summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// 收藏项模型
/// </summary>
public class Favorite
{
    /// <summary>
    /// 收藏ID
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// 文件路径
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// 文件名
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// 文件类型
    /// </summary>
    public string FileType { get; set; } = "file";

    /// <summary>
    /// 收藏时间
    /// </summary>
    public DateTime FavoritedAt { get; set; }

    /// <summary>
    /// 排序顺序
    /// </summary>
    public int SortOrder { get; set; }
}

/// <summary>
/// 最近访问文件夹
/// </summary>
public class RecentFolder
{
    /// <summary>
    /// 记录ID
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// 文件夹路径
    /// </summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// 访问时间
    /// </summary>
    public DateTime AccessedAt { get; set; }
}

/// <summary>
/// 应用配置常量
/// </summary>
public static class ConfigKeys
{
    public const string ItemsPerRow = "items_per_row";
    public const string Theme = "theme";
    public const string ShowHiddenFiles = "show_hidden_files";
    public const string SortBy = "sort_by";
    public const string SortOrder = "sort_order";
    public const string ViewMode = "view_mode";
    public const string ThumbnailSize = "thumbnail_size";
    public const string LastOpenedPath = "last_opened_path";
}
