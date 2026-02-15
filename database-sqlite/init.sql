-- ProjectFileManager SQLite 数据库初始化脚本
-- 字符编码：UTF-8

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

-- 文件缓存表（用于搜索优化）
CREATE TABLE IF NOT EXISTS file_cache (
    id TEXT PRIMARY KEY,
    name TEXT NOT NULL,
    path TEXT NOT NULL UNIQUE,
    extension TEXT,
    size INTEGER DEFAULT 0,
    is_directory INTEGER DEFAULT 0,
    created_at DATETIME,
    modified_at DATETIME,
    thumbnail_data TEXT,
    indexed_at DATETIME DEFAULT CURRENT_TIMESTAMP
);

-- 创建索引
CREATE INDEX IF NOT EXISTS idx_favorites_path ON favorites(file_path);
CREATE INDEX IF NOT EXISTS idx_recent_folders_accessed ON recent_folders(accessed_at DESC);
CREATE INDEX IF NOT EXISTS idx_file_cache_name ON file_cache(name);
CREATE INDEX IF NOT EXISTS idx_file_cache_path ON file_cache(path);
CREATE INDEX IF NOT EXISTS idx_file_cache_extension ON file_cache(extension);

-- 插入默认配置
INSERT OR REPLACE INTO user_config (key, value, updated_at) VALUES 
    ('items_per_row', '4', CURRENT_TIMESTAMP),
    ('theme', 'light', CURRENT_TIMESTAMP),
    ('show_hidden_files', 'false', CURRENT_TIMESTAMP),
    ('sort_by', 'name', CURRENT_TIMESTAMP),
    ('sort_order', 'asc', CURRENT_TIMESTAMP),
    ('view_mode', 'grid', CURRENT_TIMESTAMP),
    ('thumbnail_size', 'medium', CURRENT_TIMESTAMP);

-- 插入示例收藏数据（开发测试用）
INSERT OR REPLACE INTO favorites (id, file_path, file_name, file_type, favorited_at, sort_order) VALUES
    ('fav-001', '/Users/demo/Documents/项目报告.docx', '项目报告.docx', 'document', CURRENT_TIMESTAMP, 1),
    ('fav-002', '/Users/demo/Pictures/风景照片.jpg', '风景照片.jpg', 'image', CURRENT_TIMESTAMP, 2),
    ('fav-003', '/Users/demo/Music/背景音乐.mp3', '背景音乐.mp3', 'audio', CURRENT_TIMESTAMP, 3),
    ('fav-004', '/Users/demo/Videos/演示视频.mp4', '演示视频.mp4', 'video', CURRENT_TIMESTAMP, 4),
    ('fav-005', '/Users/demo/Code/项目源码', '项目源码', 'folder', CURRENT_TIMESTAMP, 5);

-- 插入示例最近文件夹
INSERT OR REPLACE INTO recent_folders (id, path, accessed_at) VALUES
    ('recent-001', '/Users/demo/Documents', datetime('now', '-1 hour')),
    ('recent-002', '/Users/demo/Downloads', datetime('now', '-2 hours')),
    ('recent-003', '/Users/demo/Desktop', datetime('now', '-3 hours')),
    ('recent-004', '/Users/demo/Pictures', datetime('now', '-1 day')),
    ('recent-005', '/Users/demo/Projects', datetime('now', '-2 days'));
