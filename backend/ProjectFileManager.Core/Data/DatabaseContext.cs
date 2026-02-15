// -*- coding: utf-8 -*-
using System;
using System.IO;
using Microsoft.Data.Sqlite;
using Serilog;

namespace ProjectFileManager.Core.Data;

/// <summary>
/// SQLite 数据库上下文
/// </summary>
public class DatabaseContext : IDisposable
{
    private readonly string _connectionString;
    private SqliteConnection? _connection;
    private bool _disposed;
    private readonly object _connectionLock = new();

    /// <summary>
    /// 数据库文件路径
    /// </summary>
    public string DatabasePath { get; }

    public DatabaseContext(string? databasePath = null)
    {
        DatabasePath = databasePath ?? GetDefaultDatabasePath();
        _connectionString = $"Data Source={DatabasePath}";
        
        Log.Debug("初始化数据库上下文: {DatabasePath}", DatabasePath);
        
        EnsureDirectoryExists();
        Initialize();
    }

    /// <summary>
    /// 获取默认数据库路径
    /// </summary>
    private static string GetDefaultDatabasePath()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var appFolder = Path.Combine(appData, "ProjectFileManager");
        return Path.Combine(appFolder, "data.db");
    }

    /// <summary>
    /// 确保目录存在
    /// </summary>
    private void EnsureDirectoryExists()
    {
        try
        {
            var directory = Path.GetDirectoryName(DatabasePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
                Log.Information("创建数据库目录: {Directory}", directory);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "创建数据库目录失败");
            throw;
        }
    }

    /// <summary>
    /// 获取数据库连接（线程安全）
    /// </summary>
    public SqliteConnection GetConnection()
    {
        lock (_connectionLock)
        {
            try
            {
                if (_connection == null)
                {
                    _connection = new SqliteConnection(_connectionString);
                    _connection.Open();
                    Log.Debug("创建新数据库连接");
                }
                else if (_connection.State != System.Data.ConnectionState.Open)
                {
                    _connection.Open();
                    Log.Debug("重新打开数据库连接");
                }
                
                return _connection;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "获取数据库连接失败");
                throw;
            }
        }
    }

    /// <summary>
    /// 初始化数据库（创建表结构）
    /// </summary>
    private void Initialize()
    {
        try
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();

            var initSql = @"
            -- 启用外键约束
            PRAGMA foreign_keys = ON;

            -- 文件收藏表
            CREATE TABLE IF NOT EXISTS favorites (
                id TEXT PRIMARY KEY,
                file_path TEXT NOT NULL UNIQUE,
                file_name TEXT NOT NULL,
                file_type TEXT DEFAULT 'file',
                favorited_at DATETIME DEFAULT CURRENT_TIMESTAMP,
                sort_order INTEGER DEFAULT 0
            );

            -- 用户配置表
            CREATE TABLE IF NOT EXISTS user_config (
                key TEXT PRIMARY KEY,
                value TEXT NOT NULL,
                updated_at DATETIME DEFAULT CURRENT_TIMESTAMP
            );

            -- 最近访问文件夹表
            CREATE TABLE IF NOT EXISTS recent_folders (
                id TEXT PRIMARY KEY,
                path TEXT NOT NULL UNIQUE,
                accessed_at DATETIME DEFAULT CURRENT_TIMESTAMP
            );

            -- 创建索引
            CREATE INDEX IF NOT EXISTS idx_favorites_path ON favorites(file_path);
            CREATE INDEX IF NOT EXISTS idx_recent_folders_accessed ON recent_folders(accessed_at DESC);

            -- 插入默认配置（如果不存在）
            INSERT OR IGNORE INTO user_config (key, value, updated_at) VALUES 
                ('items_per_row', '4', CURRENT_TIMESTAMP),
                ('theme', 'light', CURRENT_TIMESTAMP),
                ('show_hidden_files', 'false', CURRENT_TIMESTAMP),
                ('sort_by', 'name', CURRENT_TIMESTAMP),
                ('sort_order', 'asc', CURRENT_TIMESTAMP),
                ('view_mode', 'grid', CURRENT_TIMESTAMP),
                ('thumbnail_size', 'medium', CURRENT_TIMESTAMP);
        ";

            using var cmd = new SqliteCommand(initSql, conn);
            cmd.ExecuteNonQuery();
            
            Log.Information("数据库初始化完成");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "数据库初始化失败");
            throw;
        }
    }

    /// <summary>
    /// 执行非查询命令
    /// </summary>
    public int ExecuteNonQuery(string sql, params SqliteParameter[] parameters)
    {
        try
        {
            using var cmd = new SqliteCommand(sql, GetConnection());
            cmd.Parameters.AddRange(parameters);
            var result = cmd.ExecuteNonQuery();
            Log.Debug("执行 SQL (NonQuery): {Sql}, 影响行数: {RowsAffected}", TruncateSql(sql), result);
            return result;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "执行 SQL 失败: {Sql}", TruncateSql(sql));
            throw;
        }
    }

    /// <summary>
    /// 执行标量查询
    /// </summary>
    public object? ExecuteScalar(string sql, params SqliteParameter[] parameters)
    {
        try
        {
            using var cmd = new SqliteCommand(sql, GetConnection());
            cmd.Parameters.AddRange(parameters);
            var result = cmd.ExecuteScalar();
            Log.Debug("执行 SQL (Scalar): {Sql}", TruncateSql(sql));
            return result;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "执行 SQL 失败: {Sql}", TruncateSql(sql));
            throw;
        }
    }

    /// <summary>
    /// 执行查询并返回 Reader
    /// </summary>
    public SqliteDataReader ExecuteReader(string sql, params SqliteParameter[] parameters)
    {
        try
        {
            var cmd = new SqliteCommand(sql, GetConnection());
            cmd.Parameters.AddRange(parameters);
            Log.Debug("执行 SQL (Reader): {Sql}", TruncateSql(sql));
            return cmd.ExecuteReader();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "执行 SQL 失败: {Sql}", TruncateSql(sql));
            throw;
        }
    }

    /// <summary>
    /// 截断 SQL 用于日志输出
    /// </summary>
    private static string TruncateSql(string sql)
    {
        const int maxLength = 100;
        var trimmed = sql.Replace("\r", " ").Replace("\n", " ").Trim();
        return trimmed.Length > maxLength ? trimmed[..maxLength] + "..." : trimmed;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _connection?.Close();
                _connection?.Dispose();
            }
            _disposed = true;
        }
    }
}
