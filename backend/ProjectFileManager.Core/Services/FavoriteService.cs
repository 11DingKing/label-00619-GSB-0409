// -*- coding: utf-8 -*-
using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Data.Sqlite;
using ProjectFileManager.Core.Data;
using ProjectFileManager.Core.Models;
using Serilog;

namespace ProjectFileManager.Core.Services;

/// <summary>
/// 收藏管理服务
/// </summary>
public class FavoriteService
{
    private readonly DatabaseContext _db;

    public FavoriteService(DatabaseContext db)
    {
        _db = db;
    }

    /// <summary>
    /// 切换收藏状态
    /// </summary>
    public bool ToggleFavorite(string filePath)
    {
        var wasFavorite = IsFavorite(filePath);
        
        if (wasFavorite)
        {
            RemoveFavorite(filePath);
            Log.Information("取消收藏: {FilePath}", filePath);
            return false;
        }
        else
        {
            AddFavorite(filePath);
            Log.Information("添加收藏: {FilePath}", filePath);
            return true;
        }
    }

    /// <summary>
    /// 添加收藏
    /// </summary>
    public void AddFavorite(string filePath)
    {
        var fileName = Path.GetFileName(filePath);
        var isDirectory = Directory.Exists(filePath);
        var fileType = isDirectory ? "folder" : FileItem.GetFileType(Path.GetExtension(filePath));
        var id = Guid.NewGuid().ToString("N");

        var sql = @"
            INSERT OR REPLACE INTO favorites (id, file_path, file_name, file_type, favorited_at, sort_order)
            VALUES (@id, @path, @name, @type, CURRENT_TIMESTAMP, 
                    (SELECT COALESCE(MAX(sort_order), 0) + 1 FROM favorites))
        ";

        _db.ExecuteNonQuery(sql,
            new SqliteParameter("@id", id),
            new SqliteParameter("@path", filePath),
            new SqliteParameter("@name", fileName),
            new SqliteParameter("@type", fileType));
    }

    /// <summary>
    /// 移除收藏
    /// </summary>
    public void RemoveFavorite(string filePath)
    {
        var sql = "DELETE FROM favorites WHERE file_path = @path";
        _db.ExecuteNonQuery(sql, new SqliteParameter("@path", filePath));
    }

    /// <summary>
    /// 检查是否已收藏
    /// </summary>
    public bool IsFavorite(string filePath)
    {
        var sql = "SELECT COUNT(*) FROM favorites WHERE file_path = @path";
        var result = _db.ExecuteScalar(sql, new SqliteParameter("@path", filePath));
        return Convert.ToInt32(result) > 0;
    }

    /// <summary>
    /// 获取所有收藏
    /// </summary>
    public List<Favorite> GetAllFavorites()
    {
        var favorites = new List<Favorite>();
        var sql = "SELECT id, file_path, file_name, file_type, favorited_at, sort_order FROM favorites ORDER BY sort_order";

        using var reader = _db.ExecuteReader(sql);
        while (reader.Read())
        {
            favorites.Add(new Favorite
            {
                Id = reader.GetString(0),
                FilePath = reader.GetString(1),
                FileName = reader.GetString(2),
                FileType = reader.GetString(3),
                FavoritedAt = reader.GetDateTime(4),
                SortOrder = reader.GetInt32(5)
            });
        }

        return favorites;
    }

    /// <summary>
    /// 获取所有收藏的路径集合（用于快速查找）
    /// </summary>
    public HashSet<string> GetAllFavoritePaths()
    {
        var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var sql = "SELECT file_path FROM favorites";

        using var reader = _db.ExecuteReader(sql);
        while (reader.Read())
        {
            paths.Add(reader.GetString(0));
        }

        return paths;
    }

    /// <summary>
    /// 更新收藏排序
    /// </summary>
    public void UpdateSortOrder(string id, int newOrder)
    {
        var sql = "UPDATE favorites SET sort_order = @order WHERE id = @id";
        _db.ExecuteNonQuery(sql,
            new SqliteParameter("@order", newOrder),
            new SqliteParameter("@id", id));
    }
}
