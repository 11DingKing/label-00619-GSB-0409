# ProjectFileManager - 项目文件管理器

基于 .NET 7.0 + Eto.Forms + WebView 的跨平台项目文件管理桌面应用。支持 Windows (WPF) 和 macOS (Cocoa) 双平台。

## 1. How to Run

### 一键启动（推荐）

**Windows:**
```bash
start.bat
```

**macOS / Linux:**
```bash
chmod +x start.sh
./start.sh
```

启动脚本会自动检测并安装 .NET SDK 7.0（如未安装），然后构建并启动应用。

**重要：** 请在**项目根目录**下执行 `./start.sh`（即包含 `frontend` 和 `backend` 的目录）。这样应用会从本地的 `frontend/` 加载界面，修改 HTML/CSS/JS 后**无需重新构建**，关闭应用再运行一次 `./start.sh` 即可看到效果。

### 手动构建

#### 前置要求
- [.NET SDK 7.0](https://dotnet.microsoft.com/download/dotnet/7.0) 或更高版本

#### Windows (WPF)
```bash
# 还原依赖
dotnet restore backend/ProjectFileManager.sln

# 构建并运行
dotnet run --project backend/ProjectFileManager.Wpf/ProjectFileManager.Wpf.csproj
```

#### macOS
```bash
# 还原依赖
dotnet restore backend/ProjectFileManager.sln

# 构建并运行
dotnet run --project backend/ProjectFileManager.Mac/ProjectFileManager.Mac.csproj
```

### 发布独立可执行文件

```bash
# Windows
dotnet publish backend/ProjectFileManager.Wpf/ProjectFileManager.Wpf.csproj -c Release -r win-x64 --self-contained true

# macOS (Intel)
dotnet publish backend/ProjectFileManager.Mac/ProjectFileManager.Mac.csproj -c Release -r osx-x64 --self-contained true

# macOS (Apple Silicon)
dotnet publish backend/ProjectFileManager.Mac/ProjectFileManager.Mac.csproj -c Release -r osx-arm64 --self-contained true
```

## 2. Services

| 服务 | 端口 | 说明 |
|------|------|------|
| 桌面应用 | - | 本地桌面应用，无需网络端口 |
| SQLite 数据库 | - | 本地文件数据库 (`%APPDATA%/ProjectFileManager/data.db`) |

## 3. 测试账号

本应用为本地桌面应用，无需登录账号。所有数据存储在本地 SQLite 数据库中。

## 4. 题目内容

新建一个.net 7.0 C# 项目， GUI使用ETO， 用WebView结合C#写一个管理项目文件的项目要求如下：
下面的内容用Html写，用WebView显示 
我们需要搭的一个框架有， 

目录树（固定） | 文件区 
---底栏（固定）--- 
左边是目录树， 分上下两个容器，最下面是一个底栏上面有刷新搜索，保存等等的工具，上面分两左右两栏左边是目录树，右边是文件的预览图，每项预览左上角有一个星星收藏，右上角有三点用于打开更多选项，底下是文件的名字，等等，最好是搭好框架， 文件双击可以打开事件，最好可以监测到是否按下Shift键。整个右边的窗口最好是每项目的大小是可以自缩放的，文件最好也是懒加载的，文件可以先用模拟数据，图片可以先用占位的，用ETO WPF，和mac的。就两个平台， 代码不要在入口文件写，需要保持入口文件干净，底样还需要一个滑块来调节文件区每行的数量，

## 5. 项目结构

```
ProjectFileManager/
├── backend/                                # 后端 C# 代码
│   ├── ProjectFileManager.Core/           # 核心业务逻辑（跨平台共享）
│   │   ├── Models/                         # 数据模型
│   │   │   ├── FileItem.cs                # 文件项模型
│   │   │   └── UserConfig.cs              # 用户配置模型
│   │   ├── Services/                       # 业务服务
│   │   │   ├── FileService.cs             # 文件管理服务
│   │   │   ├── FavoriteService.cs         # 收藏管理服务
│   │   │   └── ConfigService.cs           # 配置管理服务
│   │   ├── Data/                           # 数据访问
│   │   │   └── DatabaseContext.cs         # SQLite 数据库上下文
│   │   ├── Logging/                        # 日志模块
│   │   │   └── LoggerFactory.cs           # Serilog 日志工厂
│   │   └── ProjectFileManager.Core.csproj
│   │
│   ├── ProjectFileManager.Desktop/         # 桌面公共层（Eto.Forms）
│   │   ├── MainForm.cs                    # 主窗口
│   │   ├── WebViewHost.cs                 # WebView 宿主（JS ↔ C# 通信）
│   │   └── ProjectFileManager.Desktop.csproj
│   │
│   ├── ProjectFileManager.Wpf/             # Windows (WPF) 平台启动器
│   │   ├── Program.cs                     # 入口点
│   │   └── ProjectFileManager.Wpf.csproj
│   │
│   ├── ProjectFileManager.Mac/             # macOS 平台启动器
│   │   ├── Program.cs                     # 入口点
│   │   └── ProjectFileManager.Mac.csproj
│   │
│   └── ProjectFileManager.sln              # 解决方案文件
│
├── frontend/                               # 前端 HTML/CSS/JS 资源
│   ├── index.html                         # 主页面
│   ├── css/
│   │   └── styles.css                     # 样式表
│   └── js/
│       └── app.js                         # 应用逻辑
│
├── database-sqlite/                        # 数据库
│   └── init.sql                           # 初始化脚本
│
├── docs/                                   # 文档
│   └── project_design.md                  # 设计文档
│
├── start.sh                               # Linux/macOS 启动脚本
├── start.bat                              # Windows 启动脚本
├── README.md                              # 项目说明
└── .gitignore                             # Git 忽略配置
```

## 6. 功能清单

### 核心功能

| 功能 | 状态 | 说明 |
|------|------|------|
| 目录树浏览 | ✅ | 左侧固定目录树，支持展开/折叠 |
| 文件网格视图 | ✅ | 右侧文件预览区，网格布局展示 |
| 文件缩略图 | ✅ | 根据文件类型显示对应图标 |
| 收藏功能 | ✅ | 左上角星星按钮切换收藏状态 |
| 更多选项 | ✅ | 右上角三点按钮打开上下文菜单 |
| 双击打开 | ✅ | 双击文件打开，支持 Shift 键检测 |
| 懒加载 | ✅ | 文件列表滚动懒加载 |
| 底部工具栏 | ✅ | 刷新、搜索、排序等工具 |
| 每行数量滑块 | ✅ | 滑块调节文件区每行显示数量（2-8） |
| 快捷位置 | ✅ | 主目录、桌面、文档、下载快捷入口 |
| 搜索功能 | ✅ | 实时搜索当前目录及子目录 |
| 排序功能 | ✅ | 按名称/日期/大小/类型排序 |
| 显示隐藏文件 | ✅ | 切换隐藏文件的显示 |

### 平台支持

| 平台 | GUI 框架 | 状态 |
|------|---------|------|
| Windows | WPF (Eto.Platform.Wpf) | ✅ |
| macOS | Cocoa (Eto.Platform.Mac64) | ✅ |

### 技术特性

- **跨平台架构**：Core 层业务逻辑完全跨平台共享
- **WebView 渲染**：使用 HTML/CSS/JS 实现现代化 UI
- **JS ↔ C# 桥接**：自定义 Bridge 协议实现双向通信
- **SQLite 持久化**：本地数据库存储收藏和配置
- **响应式设计**：文件项大小可缩放，网格自适应

## 编码说明

本项目所有文件使用 UTF-8 编码，确保中文正常显示：
- 源代码：UTF-8 without BOM
- 数据库：SQLite UTF-8
- HTML/CSS/JS：UTF-8
- 启动脚本：UTF-8 (`chcp 65001` for Windows)

## 键盘快捷键

| 快捷键 | 功能 |
|--------|------|
| F5 | 刷新当前目录 |
| Ctrl/Cmd + S | 保存配置 |
| Ctrl/Cmd + F | 聚焦搜索框 |
| Escape | 关闭菜单 |
| Shift + 双击 | 在文件管理器中显示 |

## 底栏按钮行为说明

### 刷新按钮（`btnRefresh` / `F5`）

- 作用范围：刷新当前目录的文件列表（重置分页后重新加载第 1 页）。
- 成功提示：显示 Toast `刷新成功`。
- 失败提示：显示 Toast `刷新失败：...`（例如正在加载中或文件加载失败）。

### 保存按钮（`btnSave` / `Ctrl/Cmd + S`）

- 持久化内容：
  - 每行显示数量（`itemsPerRow`）
  - 是否显示隐藏文件（`showHiddenFiles`）
  - 排序字段和顺序（`sort_by` / `sort_order`）
  - 当前路径（非“收藏”视图时保存为 `lastOpenedPath`）
- 成功提示：显示 Toast `保存配置成功`。
- 失败提示：显示 Toast `保存配置失败：...`（包含错误原因）。

## 许可证

MIT License
