// -*- coding: utf-8 -*-
using System;

namespace ProjectFileManager.Core.Models;

/// <summary>
/// 文件项模型
/// </summary>
public class FileItem
{
    /// <summary>
    /// 唯一标识（基于路径的哈希）
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// 文件名
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 完整路径
    /// </summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// 文件扩展名（小写，不含点）
    /// </summary>
    public string Extension { get; set; } = string.Empty;

    /// <summary>
    /// 文件大小（字节）
    /// </summary>
    public long Size { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// 修改时间
    /// </summary>
    public DateTime ModifiedAt { get; set; }

    /// <summary>
    /// 是否为目录
    /// </summary>
    public bool IsDirectory { get; set; }

    /// <summary>
    /// 是否已收藏
    /// </summary>
    public bool IsFavorite { get; set; }

    /// <summary>
    /// 文件类型分类
    /// </summary>
    public string FileType { get; set; } = "file";

    /// <summary>
    /// 缩略图 Base64 数据（可选）
    /// </summary>
    public string? ThumbnailData { get; set; }

    /// <summary>
    /// 格式化的文件大小
    /// </summary>
    public string FormattedSize => FormatFileSize(Size);

    /// <summary>
    /// 根据扩展名推断文件类型
    /// </summary>
    public static string GetFileType(string extension)
    {
        if (string.IsNullOrEmpty(extension))
            return "file";

        var ext = extension.ToLowerInvariant().TrimStart('.');
        
        return ext switch
        {
            // 图片
            "jpg" or "jpeg" or "png" or "gif" or "bmp" or "webp" or "svg" or "ico" => "image",
            // 视频
            "mp4" or "avi" or "mkv" or "mov" or "wmv" or "flv" or "webm" => "video",
            // 音频
            "mp3" or "wav" or "flac" or "aac" or "ogg" or "wma" or "m4a" => "audio",
            // 文档
            "doc" or "docx" or "pdf" or "txt" or "rtf" or "odt" => "document",
            // 表格
            "xls" or "xlsx" or "csv" or "ods" => "spreadsheet",
            // 演示
            "ppt" or "pptx" or "odp" => "presentation",
            // 代码
            "cs" or "js" or "ts" or "py" or "java" or "cpp" or "c" or "h" or "html" or "css" or "json" or "xml" or "yaml" or "yml" or "md" => "code",
            // 压缩
            "zip" or "rar" or "7z" or "tar" or "gz" or "bz2" => "archive",
            // 可执行
            "exe" or "msi" or "app" or "dmg" or "deb" or "rpm" => "executable",
            _ => "file"
        };
    }

    private static string FormatFileSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        int order = 0;
        double size = bytes;
        
        while (size >= 1024 && order < sizes.Length - 1)
        {
            order++;
            size /= 1024;
        }

        return $"{size:0.##} {sizes[order]}";
    }
}

/// <summary>
/// 目录树节点
/// </summary>
public class DirectoryNode
{
    /// <summary>
    /// 节点ID
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// 目录名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 完整路径
    /// </summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// 子目录
    /// </summary>
    public List<DirectoryNode> Children { get; set; } = new();

    /// <summary>
    /// 是否展开
    /// </summary>
    public bool IsExpanded { get; set; }

    /// <summary>
    /// 是否有子目录（用于懒加载）
    /// </summary>
    public bool HasChildren { get; set; }
}

/// <summary>
/// 分页结果
/// </summary>
public class PagedResult<T>
{
    /// <summary>
    /// 数据列表
    /// </summary>
    public List<T> Items { get; set; } = new();

    /// <summary>
    /// 总数量
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// 当前页码
    /// </summary>
    public int Page { get; set; }

    /// <summary>
    /// 每页数量
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// 总页数
    /// </summary>
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

    /// <summary>
    /// 是否有下一页
    /// </summary>
    public bool HasNextPage => Page < TotalPages;
}
