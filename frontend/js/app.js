// -*- coding: utf-8 -*-
/**
 * ProjectFileManager - 项目文件管理器
 * 前端应用逻辑
 */

// ============================================
// 应用状态
// ============================================
const state = {
    currentPath: '',
    lastValidPath: '',
    files: [],
    page: 1,
    pageSize: 50,
    totalCount: 0,
    hasMore: true,
    isLoading: false,
    itemsPerRow: 4,
    sortBy: 'name',
    sortOrder: 'asc',
    showHiddenFiles: false,
    searchKeyword: '',
    selectedFile: null,
    _lastSearchValue: ''  // 用于搜索框轮询比对
};

// ============================================
// 错误处理工具
// ============================================
function getErrorMessage(error, defaultMsg) {
    // 处理结构化错误（来自 C# Bridge）
    if (error && error.code) {
        const errorMessages = {
            'ACCESS_DENIED': '访问被拒绝，请检查权限',
            'DIRECTORY_NOT_FOUND': '目录不存在',
            'FILE_NOT_FOUND': '文件不存在',
            'IO_ERROR': '文件操作失败',
            'NOT_SUPPORTED': '不支持的操作',
            'INVALID_ARGUMENT': '无效参数'
        };
        return errorMessages[error.code] || error.message || defaultMsg;
    }
    return error?.message || defaultMsg;
}

// ============================================
// C# Bridge 封装
// ============================================
const bridge = {
    async call(method, params = {}) {
        // 检查是否在 ETO WebView 环境中
        if (typeof window.csharp !== 'undefined') {
            return await window.csharp.call(method, params);
        }
        // 开发环境模拟数据
        return this.mockCall(method, params);
    },

    // 模拟数据（开发调试用）
    mockCall(method, params) {
        console.log(`[Mock] ${method}`, params);

        switch (method) {
            case 'getUserHome':
                return '/Users/demo';
            case 'getDesktop':
                return '/Users/demo/Desktop';
            case 'getDocuments':
                return '/Users/demo/Documents';
            case 'getDownloads':
                return '/Users/demo/Downloads';
            case 'getItemsPerRow':
                return 4;
            case 'getTheme':
                return 'light';
            case 'getShowHiddenFiles':
                return false;
            case 'getDirectoryTree':
                return this.getMockDirectoryTree(params.rootPath);
            case 'getFiles':
                return this.getMockFiles(params.directoryPath, params.page, params.pageSize);
            case 'toggleFavorite':
                return !state.files.find(f => f.path === params.filePath)?.isFavorite;
            case 'isFavorite':
                return false;
            case 'getFavorites':
                return [];
            case 'searchFiles':
                return [];
            default:
                return null;
        }
    },

    getMockDirectoryTree(rootPath) {
        const path = rootPath || '/Users/demo';
        const name = path.split('/').filter(Boolean).pop() || '根目录';
        
        // 根据路径返回不同的 mock 子目录
        let children = [];
        if (path === '/Users/demo' || path.endsWith('/demo')) {
            children = [
                { id: 'desktop', name: '桌面', path: path + '/Desktop', hasChildren: true, children: [] },
                { id: 'documents', name: '文档', path: path + '/Documents', hasChildren: true, children: [] },
                { id: 'downloads', name: '下载', path: path + '/Downloads', hasChildren: true, children: [] },
                { id: 'pictures', name: '图片', path: path + '/Pictures', hasChildren: true, children: [] },
                { id: 'projects', name: '项目', path: path + '/Projects', hasChildren: true, children: [] },
            ];
        } else {
            // 其他路径返回一些通用子目录
            children = [
                { id: 'sub1', name: '子目录1', path: path + '/子目录1', hasChildren: true, children: [] },
                { id: 'sub2', name: '子目录2', path: path + '/子目录2', hasChildren: false, children: [] },
            ];
        }
        
        return {
            id: 'root',
            name: name,
            path: path,
            hasChildren: children.length > 0,
            children: children
        };
    },

    getMockFiles(directoryPath, page, pageSize) {
        const mockItems = [
            { id: '1', name: '项目报告.docx', path: `${directoryPath}/项目报告.docx`, extension: 'docx', size: 245760, isDirectory: false, fileType: 'document', isFavorite: true, createdAt: '2024-01-15', modifiedAt: '2024-01-20' },
            { id: '2', name: '风景照片.jpg', path: `${directoryPath}/风景照片.jpg`, extension: 'jpg', size: 3145728, isDirectory: false, fileType: 'image', isFavorite: false, createdAt: '2024-01-10', modifiedAt: '2024-01-10' },
            { id: '3', name: '演示视频.mp4', path: `${directoryPath}/演示视频.mp4`, extension: 'mp4', size: 52428800, isDirectory: false, fileType: 'video', isFavorite: false, createdAt: '2024-01-05', modifiedAt: '2024-01-05' },
            { id: '4', name: '背景音乐.mp3', path: `${directoryPath}/背景音乐.mp3`, extension: 'mp3', size: 5242880, isDirectory: false, fileType: 'audio', isFavorite: true, createdAt: '2024-01-01', modifiedAt: '2024-01-01' },
            { id: '5', name: '源代码', path: `${directoryPath}/源代码`, extension: '', size: 0, isDirectory: true, fileType: 'folder', isFavorite: false, createdAt: '2024-01-08', modifiedAt: '2024-01-18' },
            { id: '6', name: 'README.md', path: `${directoryPath}/README.md`, extension: 'md', size: 4096, isDirectory: false, fileType: 'code', isFavorite: false, createdAt: '2024-01-12', modifiedAt: '2024-01-15' },
            { id: '7', name: '数据表格.xlsx', path: `${directoryPath}/数据表格.xlsx`, extension: 'xlsx', size: 102400, isDirectory: false, fileType: 'spreadsheet', isFavorite: false, createdAt: '2024-01-14', modifiedAt: '2024-01-14' },
            { id: '8', name: '压缩包.zip', path: `${directoryPath}/压缩包.zip`, extension: 'zip', size: 10485760, isDirectory: false, fileType: 'archive', isFavorite: false, createdAt: '2024-01-11', modifiedAt: '2024-01-11' },
            { id: '9', name: '设计资源', path: `${directoryPath}/设计资源`, extension: '', size: 0, isDirectory: true, fileType: 'folder', isFavorite: true, createdAt: '2024-01-09', modifiedAt: '2024-01-17' },
            { id: '10', name: 'app.js', path: `${directoryPath}/app.js`, extension: 'js', size: 8192, isDirectory: false, fileType: 'code', isFavorite: false, createdAt: '2024-01-13', modifiedAt: '2024-01-16' },
        ];

        const start = (page - 1) * pageSize;
        const items = mockItems.slice(start, start + pageSize);

        return {
            items,
            totalCount: mockItems.length,
            page,
            pageSize,
            hasNextPage: start + pageSize < mockItems.length
        };
    }
};

// ============================================
// UI 渲染函数
// ============================================

// 渲染目录树
function renderDirectoryTree(node, container, level = 0, autoExpand = false) {
    // 兼容 PascalCase 和 camelCase
    const nodePath = node.path || node.Path || '';
    const nodeName = node.name || node.Name || '';
    const nodeChildren = node.children || node.Children || [];
    const nodeHasChildren = node.hasChildren ?? node.HasChildren ?? false;

    const treeNode = document.createElement('div');
    treeNode.className = 'tree-node';
    treeNode.style.setProperty('--level', level);

    const treeItem = document.createElement('div');
    treeItem.className = 'tree-item';
    treeItem.dataset.path = nodePath; // 添加 data-path 属性
    if (nodePath === state.currentPath) {
        treeItem.classList.add('active');
    }

    // 展开图标
    const expandIcon = document.createElementNS('http://www.w3.org/2000/svg', 'svg');
    expandIcon.classList.add('expand-icon');
    if (!nodeHasChildren && nodeChildren.length === 0) {
        expandIcon.classList.add('hidden');
    }
    // 如果自动展开，添加 expanded 类
    if (autoExpand && (nodeHasChildren || nodeChildren.length > 0)) {
        expandIcon.classList.add('expanded');
    }
    expandIcon.setAttribute('viewBox', '0 0 24 24');
    expandIcon.setAttribute('fill', 'none');
    expandIcon.setAttribute('stroke', 'currentColor');
    expandIcon.setAttribute('stroke-width', '2');
    expandIcon.innerHTML = '<polyline points="9 18 15 12 9 6"/>';

    // 文件夹图标
    const folderIcon = document.createElementNS('http://www.w3.org/2000/svg', 'svg');
    folderIcon.classList.add('folder-icon');
    folderIcon.setAttribute('viewBox', '0 0 24 24');
    folderIcon.setAttribute('fill', 'none');
    folderIcon.setAttribute('stroke', 'currentColor');
    folderIcon.setAttribute('stroke-width', '2');
    folderIcon.innerHTML = '<path d="M22 19a2 2 0 0 1-2 2H4a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h5l2 3h9a2 2 0 0 1 2 2z"/>';

    // 文件夹名称
    const folderNameEl = document.createElement('span');
    folderNameEl.className = 'folder-name';
    folderNameEl.textContent = nodeName;

    treeItem.appendChild(expandIcon);
    treeItem.appendChild(folderIcon);
    treeItem.appendChild(folderNameEl);
    treeNode.appendChild(treeItem);

    // 子节点容器
    const childrenContainer = document.createElement('div');
    childrenContainer.className = 'tree-children';
    // 如果自动展开，显示子节点
    childrenContainer.style.display = autoExpand ? 'block' : 'none';

    if (nodeChildren.length > 0) {
        nodeChildren.forEach(child => {
            renderDirectoryTree(child, childrenContainer, level + 1);
        });
    }

    treeNode.appendChild(childrenContainer);
    container.appendChild(treeNode);

    // 事件绑定
    expandIcon.addEventListener('click', (e) => {
        e.stopPropagation();
        const isExpanded = expandIcon.classList.toggle('expanded');
        childrenContainer.style.display = isExpanded ? 'block' : 'none';

        // 懒加载子目录
        if (isExpanded && nodeHasChildren && childrenContainer.children.length === 0) {
            loadSubDirectories(nodePath, childrenContainer, level + 1);
        }
    });

    treeItem.addEventListener('click', () => {
        navigateTo(nodePath);
    });
}

// 加载子目录
async function loadSubDirectories(path, container, level) {
    container.innerHTML = '<div class="tree-loading"><div class="loading-spinner"></div></div>';

    try {
        const tree = await bridge.call('getDirectoryTree', { rootPath: path, depth: 1 });
        container.innerHTML = '';

        // 兼容 PascalCase 和 camelCase
        const children = tree.children || tree.Children || [];
        if (children.length > 0) {
            children.forEach(child => {
                renderDirectoryTree(child, container, level);
            });
        }
    } catch (error) {
        console.error('加载子目录失败:', error);
        container.innerHTML = '<div style="padding: 8px; color: #94a3b8; font-size: 12px;">加载失败</div>';
    }
}

// 渲染文件列表
function renderFiles(files, append = false) {
    const fileItems = document.getElementById('fileItems');
    const loadingState = document.getElementById('loadingState');
    const emptyState = document.getElementById('emptyState');
    if (!fileItems) return;
    const safeFiles = Array.isArray(files) ? files : [];
    if (loadingState) loadingState.style.display = 'none';

    if (!append) {
        fileItems.innerHTML = '';
    }

    if (safeFiles.length === 0 && !append) {
        if (emptyState) emptyState.style.display = 'flex';
        return;
    }

    if (emptyState) emptyState.style.display = 'none';

    safeFiles.forEach((file, index) => {
        const fileItem = createFileItemElement(file);
        // 交错动画延迟
        fileItem.style.animationDelay = `${index * 30}ms`;
        fileItems.appendChild(fileItem);
    });

    updateFileStats();
}

// 创建文件项元素
function createFileItemElement(file) {
    var path = file.path || file.Path || '';
    var name = file.name || file.Name || '';
    var item = document.createElement('div');
    item.className = 'file-item';
    item.dataset.path = path;
    item.dataset.id = file.id || file.Id || '';

    // 缩略图区域
    const thumbnail = document.createElement('div');
    thumbnail.className = 'file-thumbnail';

    // 文件图标
    const icon = document.createElementNS('http://www.w3.org/2000/svg', 'svg');
    icon.classList.add('file-icon', file.fileType);
    icon.setAttribute('viewBox', '0 0 24 24');
    icon.setAttribute('fill', 'none');
    icon.setAttribute('stroke', 'currentColor');
    icon.setAttribute('stroke-width', '1.5');
    icon.innerHTML = getFileIconSvg(file.fileType);
    thumbnail.appendChild(icon);

    // 收藏按钮
    const favoriteBtn = document.createElement('button');
    favoriteBtn.className = `favorite-btn ${file.isFavorite ? 'active' : ''}`;
    favoriteBtn.innerHTML = `
        <svg class="icon" viewBox="0 0 24 24" fill="${file.isFavorite ? 'currentColor' : 'none'}" stroke="currentColor" stroke-width="2">
            <polygon points="12 2 15.09 8.26 22 9.27 17 14.14 18.18 21.02 12 17.77 5.82 21.02 7 14.14 2 9.27 8.91 8.26 12 2"/>
        </svg>
    `;
    favoriteBtn.addEventListener('click', function(e) {
        e.stopPropagation();
        toggleFavorite(path, favoriteBtn);
    });
    thumbnail.appendChild(favoriteBtn);

    // 更多按钮
    const moreBtn = document.createElement('button');
    moreBtn.className = 'more-btn';
    moreBtn.innerHTML = `
        <svg class="icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <circle cx="12" cy="12" r="1"/>
            <circle cx="12" cy="5" r="1"/>
            <circle cx="12" cy="19" r="1"/>
        </svg>
    `;
    moreBtn.addEventListener('click', (e) => {
        e.stopPropagation();
        showContextMenu(e, file);
    });
    thumbnail.appendChild(moreBtn);

    item.appendChild(thumbnail);

    // 文件名
    var fileName = document.createElement('div');
    fileName.className = 'file-name';
    fileName.textContent = name;
    fileName.title = name;
    item.appendChild(fileName);

    // 文件元信息
    if (!file.isDirectory) {
        const fileMeta = document.createElement('div');
        fileMeta.className = 'file-meta';
        fileMeta.textContent = formatFileSize(file.size);
        item.appendChild(fileMeta);
    }

    // 双击打开
    let clickTimer = null;
    item.addEventListener('click', (e) => {
        if (clickTimer) {
            clearTimeout(clickTimer);
            clickTimer = null;
            openFile(file, e.shiftKey);
        } else {
            clickTimer = setTimeout(() => {
                clickTimer = null;
                selectFile(item, file);
            }, 250);
        }
    });

    // 右键菜单
    item.addEventListener('contextmenu', (e) => {
        e.preventDefault();
        selectFile(item, file);
        showContextMenu(e, file);
    });

    return item;
}

// 获取文件图标 SVG
function getFileIconSvg(fileType) {
    const icons = {
        folder: '<path d="M22 19a2 2 0 0 1-2 2H4a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h5l2 3h9a2 2 0 0 1 2 2z"/>',
        image: '<rect x="3" y="3" width="18" height="18" rx="2" ry="2"/><circle cx="8.5" cy="8.5" r="1.5"/><polyline points="21 15 16 10 5 21"/>',
        video: '<polygon points="23 7 16 12 23 17 23 7"/><rect x="1" y="5" width="15" height="14" rx="2" ry="2"/>',
        audio: '<path d="M9 18V5l12-2v13"/><circle cx="6" cy="18" r="3"/><circle cx="18" cy="16" r="3"/>',
        document: '<path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"/><polyline points="14 2 14 8 20 8"/><line x1="16" y1="13" x2="8" y2="13"/><line x1="16" y1="17" x2="8" y2="17"/>',
        spreadsheet: '<path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"/><polyline points="14 2 14 8 20 8"/><line x1="8" y1="13" x2="16" y2="13"/><line x1="8" y1="17" x2="16" y2="17"/><line x1="12" y1="9" x2="12" y2="21"/>',
        presentation: '<path d="M2 3h20v14H2z"/><line x1="12" y1="17" x2="12" y2="22"/><line x1="8" y1="22" x2="16" y2="22"/>',
        code: '<polyline points="16 18 22 12 16 6"/><polyline points="8 6 2 12 8 18"/>',
        archive: '<path d="M21 16V8a2 2 0 0 0-1-1.73l-7-4a2 2 0 0 0-2 0l-7 4A2 2 0 0 0 3 8v8a2 2 0 0 0 1 1.73l7 4a2 2 0 0 0 2 0l7-4A2 2 0 0 0 21 16z"/><polyline points="3.27 6.96 12 12.01 20.73 6.96"/><line x1="12" y1="22.08" x2="12" y2="12"/>',
        executable: '<rect x="2" y="3" width="20" height="14" rx="2" ry="2"/><line x1="8" y1="21" x2="16" y2="21"/><line x1="12" y1="17" x2="12" y2="21"/>',
        file: '<path d="M13 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V9z"/><polyline points="13 2 13 9 20 9"/>'
    };
    return icons[fileType] || icons.file;
}

// 格式化文件大小
function formatFileSize(bytes) {
    if (bytes === 0) return '';
    const sizes = ['B', 'KB', 'MB', 'GB', 'TB'];
    const i = Math.floor(Math.log(bytes) / Math.log(1024));
    return (bytes / Math.pow(1024, i)).toFixed(i > 0 ? 1 : 0) + ' ' + sizes[i];
}

// 更新文件统计
function updateFileStats() {
    var stats = document.getElementById('fileStats');
    if (stats) stats.textContent = state.totalCount + ' 个项目';
}

// 渲染面包屑
function renderBreadcrumb(path) {
    const breadcrumb = document.querySelector('.breadcrumb');
    breadcrumb.innerHTML = '';

    const parts = path.split('/').filter(Boolean);
    let currentPath = '';

    // 根目录
    const rootItem = document.createElement('span');
    rootItem.className = 'breadcrumb-item';
    rootItem.textContent = '/';
    rootItem.addEventListener('click', () => navigateTo('/'));
    breadcrumb.appendChild(rootItem);

    parts.forEach((part, index) => {
        currentPath += '/' + part;

        const separator = document.createElement('span');
        separator.className = 'breadcrumb-separator';
        separator.textContent = '/';
        breadcrumb.appendChild(separator);

        const item = document.createElement('span');
        item.className = 'breadcrumb-item';
        item.textContent = part;
        
        const pathCopy = currentPath;
        if (index < parts.length - 1) {
            item.addEventListener('click', () => navigateTo(pathCopy));
        }
        breadcrumb.appendChild(item);
    });
}

// ============================================
// 业务逻辑
// ============================================

// 导航到指定目录
async function navigateTo(path) {
    if (state.isLoading) return;

    // 保存上一个有效路径（用于从特殊视图返回）
    if (state.currentPath && state.currentPath !== '收藏' && state.currentPath.startsWith('/')) {
        state.lastValidPath = state.currentPath;
    }

    state.currentPath = path;
    state.page = 1;
    state.hasMore = true;
    state.searchKeyword = '';

    // 移除收藏入口高亮
    document.getElementById('favoritesEntry')?.classList.remove('active');

    // 更新 UI
    document.getElementById('searchInput').value = '';
    document.getElementById('searchClear').style.display = 'none';
    renderBreadcrumb(path);

    // 更新当前文件夹名称
    updateCurrentFolderName(path);

    // 刷新目录树显示当前目录的子文件夹
    await refreshDirectoryTree(path);

    // 加载文件
    await loadFiles();

    // 保存最后打开的路径
    bridge.call('setLastOpenedPath', { path });
}

// 更新当前文件夹名称
function updateCurrentFolderName(path) {
    const folderNameEl = document.getElementById('currentFolderName');
    if (folderNameEl) {
        const parts = path.split('/').filter(Boolean);
        const name = parts.length > 0 ? parts[parts.length - 1] : '根目录';
        folderNameEl.textContent = name;
        folderNameEl.title = path;
    }
}

// 刷新目录树
async function refreshDirectoryTree(path) {
    const treeContainer = document.getElementById('directoryTree');
    
    try {
        const tree = await bridge.call('getDirectoryTree', { rootPath: path, depth: 1 });
        treeContainer.innerHTML = '';
        
        // 渲染当前目录作为根节点，并自动展开
        renderDirectoryTree(tree, treeContainer, 0, true);
    } catch (error) {
        console.error('刷新目录树失败:', error);
        treeContainer.innerHTML = '<div class="tree-empty">加载失败</div>';
    }
}

// 加载文件列表
async function loadFiles(append = false, options = {}) {
    const showErrorToast = options.showErrorToast !== false;
    if (state.isLoading || (!append && !state.hasMore)) return;

    state.isLoading = true;

    if (!append) {
        document.getElementById('loadingState').style.display = 'flex';
        document.getElementById('fileItems').innerHTML = '';
        document.getElementById('emptyState').style.display = 'none';
    }

    try {
        const result = await bridge.call('getFiles', {
            directoryPath: state.currentPath,
            page: state.page,
            pageSize: state.pageSize
        });

        // 兼容 PascalCase 和 camelCase
        const items = result.items || result.Items || [];
        const totalCount = result.totalCount ?? result.TotalCount ?? 0;
        const hasNextPage = result.hasNextPage ?? result.HasNextPage ?? false;

        state.files = append ? [...state.files, ...items] : items;
        state.totalCount = totalCount;
        state.hasMore = hasNextPage;
        state.page++;

        renderFiles(items, append);
        return true;
    } catch (error) {
        console.error('加载文件失败:', error);
        const message = getErrorMessage(error, '加载文件失败');
        if (showErrorToast) {
            showToast(message, 'error');
        }
        return false;
    } finally {
        state.isLoading = false;
    }
}

// 打开文件
async function openFile(file, withShift = false) {
    if (file.isDirectory) {
        navigateTo(file.path);
    } else {
        try {
            await bridge.call('openFile', {
                filePath: file.path,
                withShift
            });
        } catch (error) {
            console.error('打开文件失败:', error);
            showToast(getErrorMessage(error, '打开文件失败'), 'error');
        }
    }
}

// 切换收藏
async function toggleFavorite(filePath, button) {
    try {
        const isFavorite = await bridge.call('toggleFavorite', { filePath });

        button.classList.toggle('active', isFavorite);
        const icon = button.querySelector('.icon');
        icon.setAttribute('fill', isFavorite ? 'currentColor' : 'none');

        // 更新状态
        const file = state.files.find(f => f.path === filePath);
        if (file) {
            file.isFavorite = isFavorite;
        }

        showToast(isFavorite ? '已添加到收藏' : '已取消收藏');
    } catch (error) {
        console.error('切换收藏失败:', error);
        showToast(getErrorMessage(error, '操作失败'), 'error');
    }
}

// 显示收藏列表
async function showFavorites() {
    if (state.isLoading) return;

    // 保存上一个有效路径
    if (state.currentPath && state.currentPath !== '收藏' && state.currentPath.startsWith('/')) {
        state.lastValidPath = state.currentPath;
    }

    state.isLoading = true;
    state.currentPath = '收藏';
    state.searchKeyword = '';
    state.page = 1;
    state.hasMore = false;

    // 更新 UI
    const searchInput = document.getElementById('searchInput');
    const searchClear = document.getElementById('searchClear');
    if (searchInput) searchInput.value = '';
    if (searchClear) searchClear.style.display = 'none';
    
    // 更新面包屑显示"收藏"
    const breadcrumbBar = document.getElementById('breadcrumbBar');
    const breadcrumb = breadcrumbBar?.querySelector('.breadcrumb');
    if (breadcrumb) {
        breadcrumb.innerHTML = '<span class="breadcrumb-item active">⭐ 收藏</span>';
    }
    
    // 更新左侧标题
    const currentFolderName = document.getElementById('currentFolderName');
    if (currentFolderName) {
        currentFolderName.textContent = '收藏';
        currentFolderName.title = '收藏';
    }
    
    // 清除目录树选中状态
    document.querySelectorAll('.tree-item').forEach(item => {
        item.classList.remove('active');
    });
    
    // 高亮收藏入口
    const favoritesEntry = document.getElementById('favoritesEntry');
    if (favoritesEntry) {
        favoritesEntry.classList.add('active');
    }

    // 显示加载状态
    const loadingState = document.getElementById('loadingState');
    const fileItems = document.getElementById('fileItems');
    const emptyState = document.getElementById('emptyState');
    
    if (loadingState) loadingState.style.display = 'flex';
    if (fileItems) fileItems.innerHTML = '';
    if (emptyState) emptyState.style.display = 'none';

    try {
        var favorites = await bridge.call('getFavorites');
        var items = [];
        if (Array.isArray(favorites)) {
            items = favorites;
        } else if (favorites && (favorites.items || favorites.Items)) {
            items = favorites.items || favorites.Items;
        }
        state.files = items;
        state.totalCount = items.length;
        if (loadingState) loadingState.style.display = 'none';
        if (items.length === 0) {
            if (emptyState) {
                var p = emptyState.querySelector('p');
                if (p) p.textContent = '暂无收藏项目';
                emptyState.style.display = 'flex';
            }
        } else {
            if (emptyState) emptyState.style.display = 'none';
            renderFiles(items, false);
        }
    } catch (error) {
        if (loadingState) loadingState.style.display = 'none';
        if (emptyState) {
            var p = emptyState.querySelector('p');
            if (p) p.textContent = '加载收藏失败';
            emptyState.style.display = 'flex';
        }
        showToast(getErrorMessage(error, '加载收藏失败'), 'error');
    } finally {
        state.isLoading = false;
    }
}

// 选择文件
function selectFile(element, file) {
    // 移除其他选中
    document.querySelectorAll('.file-item.selected').forEach(item => {
        item.classList.remove('selected');
    });

    element.classList.add('selected');
    state.selectedFile = file;
}

// 显示上下文菜单
function showContextMenu(event, file) {
    const menu = document.getElementById('contextMenu');
    state.selectedFile = file;

    // 更新收藏文字
    const favoriteText = menu.querySelector('.favorite-text');
    favoriteText.textContent = file.isFavorite ? '取消收藏' : '收藏';

    // 定位
    let x = event.clientX;
    let y = event.clientY;

    menu.style.display = 'block';

    // 边界检查
    const rect = menu.getBoundingClientRect();
    if (x + rect.width > window.innerWidth) {
        x = window.innerWidth - rect.width - 10;
    }
    if (y + rect.height > window.innerHeight) {
        y = window.innerHeight - rect.height - 10;
    }

    menu.style.left = `${x}px`;
    menu.style.top = `${y}px`;
}

// 隐藏菜单
function hideMenus() {
    document.getElementById('contextMenu').style.display = 'none';
    document.getElementById('sortMenu').style.display = 'none';
}

// 搜索：仅在当前文件夹/收藏列表内按名称过滤
function searchFiles(keyword) {
    var raw = (keyword || '').trim();
    state._lastSearchValue = raw;
    var k = raw.toLowerCase();
    state.searchKeyword = k ? raw : '';
    var currentList = Array.isArray(state.files) ? state.files : [];

    var loadingState = document.getElementById('loadingState') || document.querySelector('.loading-state');
    var fileItems = document.getElementById('fileItems') || document.querySelector('.file-items');
    var emptyState = document.getElementById('emptyState') || document.querySelector('.empty-state');

    if (!k) {
        state.totalCount = currentList.length;
        if (loadingState) loadingState.style.display = 'none';
        if (emptyState) emptyState.style.display = 'none';
        renderFiles(currentList);
        updateFileStats();
        return;
    }

    var list = currentList.filter(function(f) {
        var name = (f.name || f.Name || '').toLowerCase();
        return name.indexOf(k) >= 0;
    });

    state.totalCount = list.length;
    if (loadingState) loadingState.style.display = 'none';
    if (emptyState) {
        var emptyText = emptyState.querySelector('p');
        if (emptyText) emptyText.textContent = '当前文件夹内未找到匹配项';
        emptyState.style.display = list.length === 0 ? 'flex' : 'none';
    }
    if (!fileItems) {
        updateFileStats();
        return;
    }
    fileItems.innerHTML = '';
    if (list.length > 0) {
        for (var i = 0; i < list.length; i++) {
            var el = createFileItemElement(list[i]);
            el.style.animationDelay = (i * 30) + 'ms';
            fileItems.appendChild(el);
        }
    }
    updateFileStats();
}

// 刷新
async function refresh() {
    if (state.isLoading) {
        showToast('刷新失败：当前正在加载，请稍后重试', 'error');
        return;
    }

    state.page = 1;
    state.hasMore = true;
    const ok = await loadFiles(false, { showErrorToast: false });
    if (ok) {
        showToast('刷新成功', 'success');
    } else {
        showToast('刷新失败：加载文件失败', 'error');
    }
}

// 保存配置
async function saveConfig() {
    try {
        await bridge.call('setItemsPerRow', { count: state.itemsPerRow });
        await bridge.call('setShowHiddenFiles', { show: state.showHiddenFiles });
        await bridge.call('setConfig', { key: 'sort_by', value: state.sortBy });
        await bridge.call('setConfig', { key: 'sort_order', value: state.sortOrder });
        if (state.currentPath && state.currentPath !== '收藏' && state.currentPath.startsWith('/')) {
            await bridge.call('setLastOpenedPath', { path: state.currentPath });
        }
        showToast('保存配置成功', 'success');
    } catch (error) {
        console.error('保存配置失败:', error);
        const message = getErrorMessage(error, '保存配置失败');
        showToast(`保存配置失败：${message}`, 'error');
    }
}

// 更新每行数量
function updateItemsPerRow(count) {
    state.itemsPerRow = count;
    document.getElementById('fileItems').style.setProperty('--items-per-row', count);
    document.getElementById('sliderValue').textContent = count;

    // 保存配置
    bridge.call('setItemsPerRow', { count });
}

// 更新排序
async function updateSort(sortBy, sortOrder) {
    state.sortBy = sortBy || state.sortBy;
    state.sortOrder = sortOrder || state.sortOrder;

    // 更新菜单选中状态
    document.querySelectorAll('.sort-menu-item[data-sort]').forEach(item => {
        item.classList.toggle('active', item.dataset.sort === state.sortBy);
    });
    document.querySelectorAll('.sort-menu-item[data-order]').forEach(item => {
        item.classList.toggle('active', item.dataset.order === state.sortOrder);
    });

    // 保存配置
    await bridge.call('setConfig', { key: 'sort_by', value: state.sortBy });
    await bridge.call('setConfig', { key: 'sort_order', value: state.sortOrder });

    // 重新加载
    state.page = 1;
    state.hasMore = true;
    await loadFiles();
}

// 切换隐藏文件显示
async function toggleHiddenFiles() {
    state.showHiddenFiles = !state.showHiddenFiles;

    const btn = document.getElementById('btnHidden');
    btn.classList.toggle('active', state.showHiddenFiles);

    await bridge.call('setShowHiddenFiles', { show: state.showHiddenFiles });

    state.page = 1;
    state.hasMore = true;
    await loadFiles();

    showToast(state.showHiddenFiles ? '显示隐藏文件' : '隐藏隐藏文件');
}

// Toast 消息
function showToast(message, type = 'default') {
    const container = document.getElementById('toastContainer');

    const toast = document.createElement('div');
    toast.className = `toast toast-${type}`;
    toast.textContent = message;

    container.appendChild(toast);

    setTimeout(() => {
        toast.classList.add('toast-out');
        setTimeout(() => toast.remove(), 150);
    }, 2000);
}

// ============================================
// 初始化
// ============================================
let appInitialized = false;

async function init() {
    console.log('init() 被调用');
    
    // 暴露回调函数供 C# 调用
    window.initApp = initApp;
    window.onPreloadedDataReady = onPreloadedDataReady;
    
    // 如果预加载数据已经存在（C# 已注入），直接初始化
    if (window._preloadedData) {
        console.log('预加载数据已存在，直接初始化');
        initWithPreloadedData();
        return;
    }
    
    // 等待 C# 注入预加载数据（最多等待 3 秒）
    console.log('等待预加载数据...');
    let waited = 0;
    const checkInterval = 50;
    const maxWait = 3000;
    
    const waitForData = setInterval(() => {
        waited += checkInterval;
        
        // 如果预加载数据已注入
        if (window._preloadedData && !appInitialized) {
            clearInterval(waitForData);
            console.log('预加载数据已注入 (' + waited + 'ms)，开始初始化');
            initWithPreloadedData();
            return;
        }
        
        // 如果超时，使用 mock 数据（仅开发/调试模式）
        if (waited >= maxWait && !appInitialized) {
            clearInterval(waitForData);
            console.warn('等待超时 (' + waited + 'ms)，使用 mock 数据');
            initApp();
        }
    }, checkInterval);
}

function onPreloadedDataReady() {
    console.log('收到预加载数据就绪通知');
    if (!appInitialized && window._preloadedData) {
        initWithPreloadedData();
    }
}

function initWithPreloadedData() {
    if (appInitialized) return;
    appInitialized = true;
    
    console.log('使用预加载数据初始化...');
    
    const data = window._preloadedData;
    
    // 应用配置
    state.itemsPerRow = data.itemsPerRow || 4;
    state.showHiddenFiles = data.showHiddenFiles || false;
    state.sortBy = data.sortBy || 'name';
    state.sortOrder = data.sortOrder || 'asc';
    state.currentPath = data.currentPath || data.homePath || '/';
    
    document.getElementById('fileItems').style.setProperty('--items-per-row', state.itemsPerRow);
    document.getElementById('itemsPerRowSlider').value = state.itemsPerRow;
    document.getElementById('sliderValue').textContent = state.itemsPerRow;
    document.getElementById('btnHidden').classList.toggle('active', state.showHiddenFiles);
    
    // 更新当前文件夹名称
    updateCurrentFolderName(state.currentPath);
    
    // 渲染目录树（显示当前目录及其子文件夹）
    const treeContainer = document.getElementById('directoryTree');
    treeContainer.innerHTML = '';
    
    if (data.directoryTree) {
        // 渲染当前目录作为根节点，并自动展开
        renderDirectoryTree(data.directoryTree, treeContainer, 0, true);
    }
    
    // 渲染文件列表
    if (data.initialFiles && data.initialFiles.items) {
        state.files = data.initialFiles.items;
        state.totalCount = data.initialFiles.totalCount;
        state.hasMore = data.initialFiles.hasNextPage;
        state.page = 2; // 下一页
        renderFiles(data.initialFiles.items, false);
    }
    
    // 更新面包屑
    renderBreadcrumb(state.currentPath);
    
    // 绑定事件
    bindEvents();
    
    console.log('初始化完成');
}

async function initApp() {
    // 防止重复初始化
    if (appInitialized) return;
    appInitialized = true;
    
    console.log('ProjectFileManager 初始化 (Bridge 模式)...');

    // 加载配置
    state.itemsPerRow = await bridge.call('getItemsPerRow') || 4;
    state.showHiddenFiles = await bridge.call('getShowHiddenFiles') || false;
    state.sortBy = await bridge.call('getConfig', { key: 'sort_by' }) || 'name';
    state.sortOrder = await bridge.call('getConfig', { key: 'sort_order' }) || 'asc';

    // 应用配置
    document.getElementById('fileItems').style.setProperty('--items-per-row', state.itemsPerRow);
    document.getElementById('itemsPerRowSlider').value = state.itemsPerRow;
    document.getElementById('sliderValue').textContent = state.itemsPerRow;
    document.getElementById('btnHidden').classList.toggle('active', state.showHiddenFiles);

    // 获取初始路径
    const lastPath = await bridge.call('getLastOpenedPath');
    const homePath = await bridge.call('getUserHome');
    state.currentPath = lastPath || homePath || '/';

    // 更新当前文件夹名称
    updateCurrentFolderName(state.currentPath);

    // 加载目录树（当前目录的子文件夹）
    await refreshDirectoryTree(state.currentPath);

    // 加载文件
    renderBreadcrumb(state.currentPath);
    await loadFiles();

    // 事件绑定
    bindEvents();

    console.log('ProjectFileManager 初始化完成');
}

// 绑定事件（每个元素都做空检查，避免某一项缺失导致后续全部未绑定）
function bindEvents() {
    var btnRefresh = document.getElementById('btnRefresh');
    if (btnRefresh) btnRefresh.addEventListener('click', refresh);

    var btnSave = document.getElementById('btnSave');
    if (btnSave) btnSave.addEventListener('click', saveConfig);

    var searchInput = document.getElementById('searchInput');
    var searchClear = document.getElementById('searchClear');
    var searchTimer = null;
    function applySearch() {
        var value = searchInput ? searchInput.value : '';
        if (searchClear) searchClear.style.display = value ? 'flex' : 'none';
        searchFiles(value);
    }
    if (searchInput) {
        searchInput.addEventListener('input', function() {
            clearTimeout(searchTimer);
            searchTimer = setTimeout(applySearch, 150);
        });
        searchInput.addEventListener('keyup', function() {
            clearTimeout(searchTimer);
            searchTimer = setTimeout(applySearch, 150);
        });
        searchInput.addEventListener('keydown', function() {
            clearTimeout(searchTimer);
            searchTimer = setTimeout(applySearch, 150);
        });
        // WebView 可能不触发 input/keyup：轮询搜索框，值变化时立即过滤
        setInterval(function() {
            if (!searchInput) return;
            var value = searchInput.value || '';
            if (value === state._lastSearchValue) return;
            state._lastSearchValue = value;
            if (searchClear) searchClear.style.display = value ? 'flex' : 'none';
            searchFiles(value);
        }, 350);
    }
    if (searchClear) {
        searchClear.addEventListener('click', function() {
            if (searchInput) searchInput.value = '';
            searchClear.style.display = 'none';
            state._lastSearchValue = '';
            searchFiles('');
        });
    }

    var itemsPerRowSlider = document.getElementById('itemsPerRowSlider');
    if (itemsPerRowSlider) {
        itemsPerRowSlider.addEventListener('input', function(e) {
            updateItemsPerRow(parseInt(e.target.value, 10));
        });
    }

    var btnHidden = document.getElementById('btnHidden');
    if (btnHidden) btnHidden.addEventListener('click', toggleHiddenFiles);

    var btnSort = document.getElementById('btnSort');
    var sortMenu = document.getElementById('sortMenu');
    if (btnSort && sortMenu) {
        btnSort.addEventListener('click', function(e) {
            e.stopPropagation();
            var isHidden = sortMenu.style.display === 'none' || !sortMenu.style.display;
            sortMenu.style.display = isHidden ? 'block' : 'none';
            if (!isHidden) return;

            // 始终基于按钮本体定位，避免点到 SVG 子元素导致坐标异常
            var rect = btnSort.getBoundingClientRect();
            var menuRect = sortMenu.getBoundingClientRect();
            var left = rect.right - menuRect.width;
            var top = rect.top - menuRect.height - 8;

            // 顶部空间不足时，改为向下展开
            if (top < 8) {
                top = rect.bottom + 8;
            }

            // 水平边界保护，避免菜单超出视口造成“被遮挡”观感
            if (left + menuRect.width > window.innerWidth - 8) {
                left = window.innerWidth - menuRect.width - 8;
            }
            if (left < 8) {
                left = 8;
            }

            sortMenu.style.left = left + 'px';
            sortMenu.style.top = top + 'px';
            document.querySelectorAll('.sort-menu-item[data-sort]').forEach(function(item) {
                item.classList.toggle('active', item.dataset.sort === state.sortBy);
            });
            document.querySelectorAll('.sort-menu-item[data-order]').forEach(function(item) {
                item.classList.toggle('active', item.dataset.order === state.sortOrder);
            });
        });
    }

    // 排序菜单项
    document.querySelectorAll('.sort-menu-item[data-sort]').forEach(item => {
        item.addEventListener('click', () => {
            updateSort(item.dataset.sort, null);
            hideMenus();
        });
    });
    document.querySelectorAll('.sort-menu-item[data-order]').forEach(item => {
        item.addEventListener('click', () => {
            updateSort(null, item.dataset.order);
            hideMenus();
        });
    });

    // 上下文菜单
    document.querySelectorAll('.context-menu-item').forEach(item => {
        item.addEventListener('click', async () => {
            const action = item.dataset.action;
            const file = state.selectedFile;

            hideMenus();

            if (!file) return;

            switch (action) {
                case 'open':
                    openFile(file);
                    break;
                case 'reveal':
                    await bridge.call('revealInExplorer', { filePath: file.path });
                    break;
                case 'copyPath':
                    navigator.clipboard?.writeText(file.path);
                    showToast('路径已复制');
                    break;
                case 'favorite':
                    const btn = document.querySelector(`.file-item[data-path="${file.path}"] .favorite-btn`);
                    if (btn) toggleFavorite(file.path, btn);
                    break;
            }
        });
    });

    // 左下角「收藏」：侧栏委托（兼容无 closest 的环境）
    var sidebar = document.getElementById('sidebar');
    if (sidebar) {
        sidebar.addEventListener('click', function(e) {
            var t = e.target;
            while (t && t !== sidebar) {
                if (t.id === 'favoritesEntry' || t.getAttribute('data-path') === 'favorites') {
                    e.preventDefault();
                    e.stopPropagation();
                    if (typeof showFavorites === 'function') showFavorites();
                    return;
                }
                t = t.parentNode;
            }
        });
    }
    var favoritesEntry = document.getElementById('favoritesEntry');
    if (favoritesEntry) favoritesEntry.style.cursor = 'pointer';

    // 点击空白处隐藏菜单
    document.addEventListener('click', function(e) {
        if (!e.target.closest('.context-menu') && !e.target.closest('.sort-menu') && !e.target.closest('#btnSort')) {
            hideMenus();
        }
    });

    // 键盘快捷键
    document.addEventListener('keydown', function(e) {
        if (e.key === 'F5') {
            e.preventDefault();
            refresh();
        }
        if (e.key === 'Escape') {
            hideMenus();
        }
        if ((e.ctrlKey || e.metaKey) && e.key === 'f') {
            e.preventDefault();
            if (searchInput) searchInput.focus();
        }
        if ((e.ctrlKey || e.metaKey) && e.key === 's') {
            e.preventDefault();
            saveConfig();
        }
    });

    // 懒加载
    var fileGrid = document.querySelector('.file-grid');
    var loadMoreTrigger = document.getElementById('loadMoreTrigger');
    if (loadMoreTrigger) {
        var observer = new IntersectionObserver(function(entries) {
            entries.forEach(function(entry) {
                if (entry.isIntersecting && state.hasMore && !state.isLoading && !state.searchKeyword) {
                    loadFiles(true);
                }
            });
        }, { root: fileGrid || null, threshold: 0.1 });
        observer.observe(loadMoreTrigger);
    }
}

// 暴露给 C# 和 HTML onclick 使用
window.app = {
    refresh,
    navigateTo,
    searchFiles,
    showFavorites,
    saveConfig
};
window.showFavorites = showFavorites;
window.searchFiles = searchFiles;
window.saveConfig = saveConfig;

// 启动应用
document.addEventListener('DOMContentLoaded', init);
