// -*- coding: utf-8 -*-
using System;
using Microsoft.Data.Sqlite;
using ProjectFileManager.Core.Data;
using ProjectFileManager.Core.Models;

namespace ProjectFileManager.Core.Services;

/// <summary>
/// 配置管理服务
/// </summary>
public class ConfigService
{
    private readonly DatabaseContext _db;

    public ConfigService(DatabaseContext db)
    {
        _db = db;
    }

    /// <summary>
    /// 获取配置值
    /// </summary>
    public string? GetConfigValue(string key)
    {
        var sql = "SELECT value FROM user_config WHERE key = @key";
        var result = _db.ExecuteScalar(sql, new SqliteParameter("@key", key));
        return result?.ToString();
    }

    /// <summary>
    /// 设置配置值
    /// </summary>
    public void SetConfigValue(string key, string value)
    {
        var sql = @"
            INSERT OR REPLACE INTO user_config (key, value, updated_at)
            VALUES (@key, @value, CURRENT_TIMESTAMP)
        ";
        _db.ExecuteNonQuery(sql,
            new SqliteParameter("@key", key),
            new SqliteParameter("@value", value));
    }

    /// <summary>
    /// 获取每行显示数量
    /// </summary>
    public int GetItemsPerRow()
    {
        var value = GetConfigValue(ConfigKeys.ItemsPerRow);
        return int.TryParse(value, out var count) ? count : 4;
    }

    /// <summary>
    /// 设置每行显示数量
    /// </summary>
    public void SetItemsPerRow(int count)
    {
        count = Math.Clamp(count, 2, 10);
        SetConfigValue(ConfigKeys.ItemsPerRow, count.ToString());
    }

    /// <summary>
    /// 获取主题
    /// </summary>
    public string GetTheme()
    {
        return GetConfigValue(ConfigKeys.Theme) ?? "light";
    }

    /// <summary>
    /// 设置主题
    /// </summary>
    public void SetTheme(string theme)
    {
        SetConfigValue(ConfigKeys.Theme, theme);
    }

    /// <summary>
    /// 获取是否显示隐藏文件
    /// </summary>
    public bool GetShowHiddenFiles()
    {
        var value = GetConfigValue(ConfigKeys.ShowHiddenFiles);
        return value?.Equals("true", StringComparison.OrdinalIgnoreCase) ?? false;
    }

    /// <summary>
    /// 设置是否显示隐藏文件
    /// </summary>
    public void SetShowHiddenFiles(bool show)
    {
        SetConfigValue(ConfigKeys.ShowHiddenFiles, show ? "true" : "false");
    }

    /// <summary>
    /// 获取排序方式
    /// </summary>
    public (string SortBy, string SortOrder) GetSortSettings()
    {
        var sortBy = GetConfigValue(ConfigKeys.SortBy) ?? "name";
        var sortOrder = GetConfigValue(ConfigKeys.SortOrder) ?? "asc";
        return (sortBy, sortOrder);
    }

    /// <summary>
    /// 设置排序方式
    /// </summary>
    public void SetSortSettings(string sortBy, string sortOrder)
    {
        SetConfigValue(ConfigKeys.SortBy, sortBy);
        SetConfigValue(ConfigKeys.SortOrder, sortOrder);
    }

    /// <summary>
    /// 获取最后打开的路径
    /// </summary>
    public string? GetLastOpenedPath()
    {
        return GetConfigValue(ConfigKeys.LastOpenedPath);
    }

    /// <summary>
    /// 设置最后打开的路径
    /// </summary>
    public void SetLastOpenedPath(string path)
    {
        SetConfigValue(ConfigKeys.LastOpenedPath, path);
    }
}
