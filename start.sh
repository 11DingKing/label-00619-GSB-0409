#!/bin/bash
# -*- coding: utf-8 -*-
# ProjectFileManager - 一键启动脚本 (Linux/macOS)

set -e
cd "$(dirname "$0")"

# =====================================================
# 版本要求配置
# =====================================================
REQUIRED_DOTNET_VERSION="7.0.0"
TARGET_DOTNET_VERSION="7.0"
APP_NAME="ProjectFileManager"

# =====================================================
echo "============================================"
echo "  $APP_NAME - 一键启动脚本"
echo "  .NET 7.0 + Eto.Forms 跨平台桌面应用"
echo "============================================"
echo ""

# =====================================================
# 工具函数
# =====================================================
command_exists() {
    command -v "$1" >/dev/null 2>&1
}

version_gte() {
    local v1=$(echo "$1" | sed 's/^v//' | sed 's/[^0-9.]//g')
    local v2=$(echo "$2" | sed 's/^v//' | sed 's/[^0-9.]//g')
    
    if [[ "$(printf '%s\n%s' "$v2" "$v1" | sort -V | head -n1)" == "$v2" ]]; then
        return 0
    else
        return 1
    fi
}

print_ok() { echo "  ✓ $1"; }
print_fail() { echo "  ✗ $1"; }
print_info() { echo "  → $1"; }
print_warn() { echo "  ⚠️  $1"; }

# =====================================================
# Step 1: 检测/安装 .NET SDK
# =====================================================
echo "[1/3] 检测 .NET SDK 环境..."

# 禁用 .NET 遥测和首次运行体验（加速启动）
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_NOLOGO=1
export DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1

# 检查 dotnet 是否可用（只检查文件存在，不运行 --version 避免卡住）
DOTNET_CMD=""
if [ -x "$HOME/.dotnet/dotnet" ]; then
    DOTNET_CMD="$HOME/.dotnet/dotnet"
    export DOTNET_ROOT="$HOME/.dotnet"
    export PATH="$DOTNET_ROOT:$PATH"
    print_ok ".NET SDK 已安装 ($DOTNET_ROOT)"
elif command_exists dotnet; then
    DOTNET_CMD="dotnet"
    print_ok ".NET SDK 已安装 (系统路径)"
else
    print_fail ".NET SDK 未安装"
    NEED_INSTALL=true
fi

if [ "$NEED_INSTALL" = true ]; then
    print_info "正在安装 .NET SDK $TARGET_DOTNET_VERSION..."
    
    # 下载官方安装脚本
    INSTALL_SCRIPT="/tmp/dotnet-install.sh"
    print_info "下载 .NET 官方安装脚本..."
    curl -sSL https://dot.net/v1/dotnet-install.sh -o "$INSTALL_SCRIPT"
    chmod +x "$INSTALL_SCRIPT"
    
    # 使用官方安装脚本安装（最可靠的方式）
    print_info "执行安装..."
    "$INSTALL_SCRIPT" --channel $TARGET_DOTNET_VERSION --install-dir "$HOME/.dotnet"
    
    # 添加到 PATH
    export DOTNET_ROOT="$HOME/.dotnet"
    export PATH="$DOTNET_ROOT:$PATH"
    
    # 为后续终端会话添加到 shell 配置
    SHELL_RC=""
    if [ -f "$HOME/.zshrc" ]; then
        SHELL_RC="$HOME/.zshrc"
    elif [ -f "$HOME/.bashrc" ]; then
        SHELL_RC="$HOME/.bashrc"
    elif [ -f "$HOME/.bash_profile" ]; then
        SHELL_RC="$HOME/.bash_profile"
    fi
    
    if [ -n "$SHELL_RC" ]; then
        if ! grep -q "DOTNET_ROOT" "$SHELL_RC" 2>/dev/null; then
            echo "" >> "$SHELL_RC"
            echo "# .NET SDK" >> "$SHELL_RC"
            echo 'export DOTNET_ROOT="$HOME/.dotnet"' >> "$SHELL_RC"
            echo 'export PATH="$DOTNET_ROOT:$PATH"' >> "$SHELL_RC"
            print_info "已添加 .NET 路径到 $SHELL_RC"
        fi
    fi
    
    # 清理安装脚本
    rm -f "$INSTALL_SCRIPT"
    
    # 验证安装
    if [ -x "$HOME/.dotnet/dotnet" ]; then
        INSTALLED_VERSION=$("$HOME/.dotnet/dotnet" --version 2>/dev/null || echo "未知")
        print_ok ".NET SDK $INSTALLED_VERSION 安装完成"
    else
        print_fail ".NET SDK 安装失败"
        echo ""
        echo "请手动安装 .NET SDK 7.0:"
        echo "  https://dotnet.microsoft.com/download/dotnet/7.0"
        exit 1
    fi
fi

# =====================================================
# Step 2: 还原依赖并构建
# =====================================================
echo ""
echo "[2/3] 构建项目..."

# 确保 dotnet 在 PATH 中
if [ -x "$HOME/.dotnet/dotnet" ]; then
    export DOTNET_ROOT="$HOME/.dotnet"
    export PATH="$DOTNET_ROOT:$PATH"
fi

# 设置项目路径
PROJECT="backend/ProjectFileManager.Mac/ProjectFileManager.Mac.csproj"
DLL_NAME="ProjectFileManager.Mac.dll"

if [[ "$OSTYPE" == "darwin"* ]]; then
    # 检测 CPU 架构
    ARCH=$(uname -m)
    if [ "$ARCH" = "arm64" ]; then
        RID="osx-arm64"
    else
        RID="osx-x64"
    fi
    OUTPUT_DIR="backend/ProjectFileManager.Mac/bin/Debug/net7.0/$RID"
else
    # Linux
    OUTPUT_DIR="backend/ProjectFileManager.Mac/bin/Debug/net7.0"
    RID=""
fi

# 检查是否需要重新构建
BUILD_NEEDED=true
if [ -f "$OUTPUT_DIR/$DLL_NAME" ]; then
    # 检查源代码是否比输出新
    NEWEST_SRC=$(find backend -name "*.cs" -newer "$OUTPUT_DIR/$DLL_NAME" 2>/dev/null | head -1)
    NEWEST_WEB=$(find frontend \( -name "*.html" -o -name "*.css" -o -name "*.js" \) -newer "$OUTPUT_DIR/$DLL_NAME" 2>/dev/null | head -1)
    if [ -z "$NEWEST_SRC" ] && [ -z "$NEWEST_WEB" ]; then
        print_ok "项目已构建且为最新"
        BUILD_NEEDED=false
    else
        print_info "检测到源代码更新，重新构建..."
    fi
else
    print_info "首次构建项目..."
fi

if [ "$BUILD_NEEDED" = true ]; then
    # 仅还原当前平台的项目（避免跨平台依赖问题）
    dotnet restore "$PROJECT"
    dotnet build "$PROJECT" -c Debug
    print_ok "构建完成"
fi

# =====================================================
# Step 3: 启动应用
# =====================================================
echo ""
echo "[3/3] 启动应用..."
echo ""
echo "🚀 正在启动 $APP_NAME..."
echo ""

if [[ "$OSTYPE" == "darwin"* ]]; then
    # macOS: 将工作目录写入临时文件，然后启动应用
    CURRENT_DIR="$(pwd)"
    WORKDIR_FILE="/tmp/projectfilemanager_workdir"
    echo "$CURRENT_DIR" > "$WORKDIR_FILE"
    
    export DOTNET_ROOT="$HOME/.dotnet"
    export PATH="$DOTNET_ROOT:$PATH"
    
    APP_BUNDLE="backend/ProjectFileManager.Mac/bin/Debug/net7.0/$RID/ProjectFileManager.Mac.app"
    if [ -d "$APP_BUNDLE" ]; then
        open "$APP_BUNDLE"
    else
        # 回退到 dotnet run
        "$HOME/.dotnet/dotnet" run --project backend/ProjectFileManager.Mac/ProjectFileManager.Mac.csproj --no-build
    fi
else
    # Linux: 使用 dotnet run
    CURRENT_DIR="$(pwd)"
    WORKDIR_FILE="/tmp/projectfilemanager_workdir"
    echo "$CURRENT_DIR" > "$WORKDIR_FILE"
    dotnet run --project backend/ProjectFileManager.Mac/ProjectFileManager.Mac.csproj --no-build
fi
