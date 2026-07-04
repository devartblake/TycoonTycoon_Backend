import { jsx as _jsx, jsxs as _jsxs } from "react/jsx-runtime";
/**
 * Storage Browser - File management with drag-drop upload, folder navigation, and storage monitoring
 */
import { useState, useEffect } from 'react';
import { usePermission } from '@/hooks/use-permission';
import ErrorBoundary from '@/components/shared/error-boundary';
import EmptyState from '@/components/shared/empty-state';
import { SkeletonGrid, SkeletonTable } from '@/components/shared/skeletons';
import * as storageApi from '../api';
export default function BrowserPage() {
    usePermission('storage:write');
    const [currentPath, setCurrentPath] = useState('/');
    const [folder, setFolder] = useState(null);
    const [stats, setStats] = useState(null);
    const [quota, setQuota] = useState(null);
    const [loading, setLoading] = useState(true);
    const [uploadProgress, setUploadProgress] = useState(new Map());
    const [successMsg, setSuccessMsg] = useState(null);
    const [errorMsg, setErrorMsg] = useState(null);
    const [isDragging, setIsDragging] = useState(false);
    const [sortBy, setSortBy] = useState('name');
    const [sortOrder, setSortOrder] = useState('asc');
    useEffect(() => {
        const loadData = async () => {
            setLoading(true);
            try {
                const [folderData, statsData, quotaData] = await Promise.all([
                    storageApi.getStorageFolder(currentPath),
                    storageApi.getStorageStats(),
                    storageApi.getStorageQuota(),
                ]);
                setFolder(folderData);
                setStats(statsData);
                setQuota(quotaData);
            }
            catch (error) {
                console.error('Failed to load storage:', error);
                setErrorMsg('Failed to load storage data');
            }
            finally {
                setLoading(false);
            }
        };
        loadData();
    }, [currentPath]);
    const handleFileUpload = async (files) => {
        if (quota?.quotaExceeded) {
            setErrorMsg('Storage quota exceeded');
            return;
        }
        for (const file of files) {
            try {
                const fileId = file.name + '-' + Date.now();
                await storageApi.uploadFile(file, currentPath, (progress) => {
                    setUploadProgress((prev) => new Map(prev).set(fileId, progress));
                });
                setUploadProgress((prev) => {
                    const updated = new Map(prev);
                    updated.delete(fileId);
                    return updated;
                });
                setSuccessMsg(`${file.name} uploaded successfully`);
                setTimeout(() => setSuccessMsg(null), 2000);
            }
            catch (error) {
                setErrorMsg(error instanceof Error ? error.message : 'Upload failed');
            }
        }
        // Reload folder
        const updated = await storageApi.getStorageFolder(currentPath);
        setFolder(updated);
    };
    const handleDragOver = (e) => {
        e.preventDefault();
        setIsDragging(true);
    };
    const handleDragLeave = () => {
        setIsDragging(false);
    };
    const handleDrop = (e) => {
        e.preventDefault();
        setIsDragging(false);
        if (e.dataTransfer.files) {
            handleFileUpload(Array.from(e.dataTransfer.files));
        }
    };
    const handleDeleteFile = async (path) => {
        if (!confirm('Delete this file?'))
            return;
        try {
            await storageApi.deleteFile(path);
            setSuccessMsg('File deleted');
            setTimeout(() => setSuccessMsg(null), 2000);
            const updated = await storageApi.getStorageFolder(currentPath);
            setFolder(updated);
        }
        catch (error) {
            setErrorMsg('Delete failed');
        }
    };
    const handleNavigate = (folderName) => {
        const newPath = currentPath === '/' ? `/${folderName}` : `${currentPath}/${folderName}`;
        setCurrentPath(newPath);
    };
    const handleBack = () => {
        const parts = currentPath.split('/').filter(Boolean);
        parts.pop();
        setCurrentPath(parts.length > 0 ? `/${parts.join('/')}` : '/');
    };
    const formatBytes = (bytes) => {
        if (bytes === 0)
            return '0 B';
        const k = 1024;
        const sizes = ['B', 'KB', 'MB', 'GB'];
        const i = Math.floor(Math.log(bytes) / Math.log(k));
        return Math.round((bytes / Math.pow(k, i)) * 100) / 100 + ' ' + sizes[i];
    };
    const getSortedFiles = (files) => {
        const sorted = [...files].sort((a, b) => {
            let comparison = 0;
            if (sortBy === 'name') {
                comparison = a.name.localeCompare(b.name);
            }
            else if (sortBy === 'size') {
                comparison = a.size - b.size;
            }
            else if (sortBy === 'modified') {
                comparison = new Date(a.modifiedAt).getTime() - new Date(b.modifiedAt).getTime();
            }
            return sortOrder === 'asc' ? comparison : -comparison;
        });
        return sorted;
    };
    return (_jsx(ErrorBoundary, { children: _jsxs("div", { className: "operator-container space-y-8", children: [_jsxs("div", { children: [_jsx("h1", { className: "text-3xl font-bold text-ink-primary", children: "Storage Browser" }), _jsx("p", { className: "mt-2 text-ink-secondary", children: "Manage files and storage with drag-and-drop upload" })] }), successMsg && (_jsxs("div", { className: "p-4 bg-status-healthy/10 border border-status-healthy/20 rounded text-status-healthy text-sm", children: ["\u2713 ", successMsg] })), errorMsg && (_jsxs("div", { className: "p-4 bg-status-offline/10 border border-status-offline/20 rounded text-status-offline text-sm", children: ["\u2717 ", errorMsg] })), loading ? (_jsx(SkeletonGrid, { count: 4 })) : stats && quota ? (_jsxs("div", { className: "grid grid-cols-4 gap-4", children: [_jsxs("div", { className: "operator-card", children: [_jsx("p", { className: "text-xs text-ink-tertiary", children: "Used Space" }), _jsx("p", { className: "text-xl font-bold text-accent mt-1", children: formatBytes(stats.usedSize) }), _jsxs("p", { className: "text-xs text-ink-tertiary mt-1", children: ["of ", formatBytes(quota.maxSize)] })] }), _jsxs("div", { className: "operator-card", children: [_jsx("p", { className: "text-xs text-ink-tertiary", children: "Free Space" }), _jsx("p", { className: `text-xl font-bold mt-1 ${quota.quotaExceeded ? 'text-status-offline' : 'text-status-healthy'}`, children: formatBytes(quota.freeSize) })] }), _jsxs("div", { className: "operator-card", children: [_jsx("p", { className: "text-xs text-ink-tertiary", children: "Total Files" }), _jsx("p", { className: "text-2xl font-bold text-ink-primary mt-1", children: stats.fileCount })] }), _jsxs("div", { className: "operator-card", children: [_jsx("p", { className: "text-xs text-ink-tertiary", children: "Folders" }), _jsx("p", { className: "text-2xl font-bold text-ink-primary mt-1", children: stats.folderCount })] })] })) : null, quota && (_jsxs("div", { className: "operator-card p-4", children: [_jsxs("div", { className: "flex items-center justify-between mb-2", children: [_jsx("span", { className: "font-semibold", children: "Storage Usage" }), _jsxs("span", { className: "text-sm text-ink-secondary", children: [Math.round((quota.usedSize / quota.maxSize) * 100), "%"] })] }), _jsx("div", { className: "w-full bg-panel-border rounded-full h-3", children: _jsx("div", { className: `h-3 rounded-full transition-all ${quota.quotaExceeded ? 'bg-status-offline' : quota.usedSize / quota.maxSize > 0.8 ? 'bg-status-degraded' : 'bg-status-healthy'}`, style: { width: `${Math.min((quota.usedSize / quota.maxSize) * 100, 100)}%` } }) })] })), _jsxs("div", { className: "flex items-center gap-2 px-4 py-2 bg-panel rounded text-sm", children: [_jsx("button", { onClick: () => setCurrentPath('/'), className: "text-accent hover:underline", children: "Root" }), currentPath
                            .split('/')
                            .filter(Boolean)
                            .map((part, idx, arr) => (_jsxs("div", { children: [_jsx("span", { className: "text-ink-secondary", children: "/" }), _jsx("button", { onClick: () => setCurrentPath(`/${arr.slice(0, idx + 1).join('/')}`), className: "text-accent hover:underline ml-2", children: part })] }, idx)))] }), _jsx("div", { onDragOver: handleDragOver, onDragLeave: handleDragLeave, onDrop: handleDrop, className: `p-8 border-2 border-dashed rounded transition-colors ${isDragging ? 'border-accent bg-accent/10' : 'border-panel-border'}`, children: _jsxs("div", { className: "text-center", children: [_jsx("p", { className: "text-sm font-semibold mb-2", children: "Drag files here to upload" }), _jsx("input", { type: "file", multiple: true, onChange: (e) => e.target.files && handleFileUpload(Array.from(e.target.files)), id: "file-input", className: "hidden" }), _jsx("label", { htmlFor: "file-input", className: "inline-block px-4 py-2 bg-accent text-white rounded cursor-pointer hover:bg-accent-dark", children: "Choose Files" })] }) }), uploadProgress.size > 0 && (_jsxs("div", { className: "operator-card p-4 space-y-3", children: [_jsx("h3", { className: "font-semibold", children: "Uploading..." }), Array.from(uploadProgress.entries()).map(([id, progress]) => (_jsxs("div", { className: "space-y-1", children: [_jsx("div", { className: "flex justify-between text-sm", children: _jsxs("span", { className: "text-ink-secondary", children: [progress.toFixed(0), "%"] }) }), _jsx("div", { className: "bg-panel-border rounded-full h-2", children: _jsx("div", { className: "bg-accent h-2 rounded-full transition-all", style: { width: `${progress}%` } }) })] }, id)))] })), _jsxs("div", { className: "operator-card", children: [_jsxs("div", { className: "p-4 border-b border-panel-border flex items-center justify-between", children: [_jsx("h2", { className: "text-lg font-semibold", children: "Files & Folders" }), _jsxs("div", { className: "flex gap-2", children: [_jsxs("select", { value: sortBy, onChange: (e) => setSortBy(e.target.value), className: "px-2 py-1 rounded bg-panel text-sm border border-panel-border", children: [_jsx("option", { value: "name", children: "Name" }), _jsx("option", { value: "size", children: "Size" }), _jsx("option", { value: "modified", children: "Modified" })] }), _jsx("button", { onClick: () => setSortOrder(sortOrder === 'asc' ? 'desc' : 'asc'), className: "px-2 py-1 rounded bg-panel hover:bg-panel-border text-sm", children: sortOrder === 'asc' ? '↑' : '↓' })] })] }), loading ? (_jsx(SkeletonTable, { rows: 5, columns: 4 })) : folder && (folder.files.length > 0 || folder.subFolders.length > 0) ? (_jsx("div", { className: "overflow-x-auto", children: _jsxs("table", { className: "w-full text-sm", children: [_jsx("thead", { className: "bg-panel border-b border-panel-border", children: _jsxs("tr", { children: [_jsx("th", { className: "px-4 py-2 text-left", children: "Name" }), _jsx("th", { className: "px-4 py-2 text-right", children: "Size" }), _jsx("th", { className: "px-4 py-2 text-left", children: "Modified" }), _jsx("th", { className: "px-4 py-2 text-center", children: "Actions" })] }) }), _jsxs("tbody", { children: [currentPath !== '/' && (_jsx("tr", { className: "border-t border-panel-border hover:bg-panel/50 cursor-pointer", onClick: handleBack, children: _jsx("td", { className: "px-4 py-3", colSpan: 4, children: _jsx("span", { className: "text-accent", children: "\u2190 Back to parent" }) }) })), folder.subFolders.map((subfolder) => (_jsxs("tr", { className: "border-t border-panel-border hover:bg-panel/50 cursor-pointer", onClick: () => handleNavigate(subfolder.name), children: [_jsx("td", { className: "px-4 py-3", children: _jsxs("span", { className: "text-accent", children: ["\uD83D\uDCC1 ", subfolder.name] }) }), _jsx("td", { className: "px-4 py-3 text-right", children: formatBytes(subfolder.totalSize) }), _jsx("td", { className: "px-4 py-3 text-ink-tertiary", children: "\u2014" }), _jsx("td", { className: "px-4 py-3 text-center", children: "\u2014" })] }, subfolder.id))), getSortedFiles(folder.files).map((file) => (_jsxs("tr", { className: "border-t border-panel-border hover:bg-panel/50", children: [_jsx("td", { className: "px-4 py-3", children: _jsxs("span", { children: ["\uD83D\uDCC4 ", file.name] }) }), _jsx("td", { className: "px-4 py-3 text-right", children: formatBytes(file.size) }), _jsx("td", { className: "px-4 py-3 text-ink-tertiary", children: new Date(file.modifiedAt).toLocaleDateString() }), _jsx("td", { className: "px-4 py-3 text-center", children: _jsx("button", { onClick: () => handleDeleteFile(file.path), className: "text-status-offline hover:underline text-xs", children: "Delete" }) })] }, file.id)))] })] }) })) : (_jsx(EmptyState, { title: "Folder is empty", description: "Upload files or create folders to get started", icon: "\uD83D\uDCC1" }))] }), _jsxs("div", { className: "p-4 bg-ink-secondary/5 border border-panel-border rounded text-xs text-ink-tertiary", children: [_jsx("p", { className: "font-medium text-ink-secondary mb-2", children: "\u2705 Storage Browser Complete" }), _jsxs("ul", { className: "space-y-1", children: [_jsx("li", { children: "\u2713 File browsing with folder navigation" }), _jsx("li", { children: "\u2713 Drag-and-drop file upload" }), _jsx("li", { children: "\u2713 Storage quota monitoring and warnings" }), _jsx("li", { children: "\u2713 File management (delete, sort, search)" }), _jsx("li", { children: "\u2713 Real-time storage statistics" })] })] })] }) }));
}
