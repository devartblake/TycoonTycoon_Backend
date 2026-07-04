/**
 * Storage Browser API client
 * Handles file operations, uploads, and storage management
 */
import { apiGet, apiPost, apiDelete, apiPut } from '@/lib/api-client';
// ── File Operations ──────────────────────────────────────────────────────────
export async function getStorageFolder(path = '/') {
    return apiGet(`/admin/storage/browse?path=${encodeURIComponent(path)}`);
}
export async function getFileContent(path) {
    const response = await fetch(`/admin/storage/download?path=${encodeURIComponent(path)}`, {
        headers: {
            Authorization: `Bearer ${localStorage.getItem('token') || ''}`,
        },
    });
    return response.blob();
}
export async function deleteFile(path) {
    return apiDelete(`/admin/storage/files?path=${encodeURIComponent(path)}`);
}
export async function renameFile(oldPath, newName) {
    return apiPut(`/admin/storage/files/rename`, { oldPath, newName });
}
export async function moveFile(sourcePath, destinationPath) {
    return apiPost(`/admin/storage/files/move`, { sourcePath, destinationPath });
}
export async function createFolder(path, folderName) {
    return apiPost(`/admin/storage/folders`, { path, folderName });
}
export async function deleteFolder(path, recursive = false) {
    return apiDelete(`/admin/storage/folders?path=${encodeURIComponent(path)}&recursive=${recursive}`);
}
// ── File Upload ──────────────────────────────────────────────────────────────
export async function uploadFile(file, destinationPath, onProgress) {
    const formData = new FormData();
    formData.append('file', file);
    formData.append('path', destinationPath);
    return new Promise((resolve, reject) => {
        const xhr = new XMLHttpRequest();
        const startTime = Date.now();
        let lastProgress = 0;
        let lastTime = startTime;
        if (onProgress) {
            xhr.upload.addEventListener('progress', (e) => {
                if (e.lengthComputable) {
                    const progress = (e.loaded / e.total) * 100;
                    const currentTime = Date.now();
                    const timeDiff = (currentTime - lastTime) / 1000; // seconds
                    const progressDiff = e.loaded - lastProgress;
                    if (timeDiff > 0) {
                        const speed = progressDiff / timeDiff; // bytes per second
                        const remaining = e.total - e.loaded;
                        const estimatedTime = speed > 0 ? remaining / speed : 0;
                        onProgress(progress, speed, estimatedTime);
                        lastProgress = e.loaded;
                        lastTime = currentTime;
                    }
                }
            });
        }
        xhr.addEventListener('load', () => {
            if (xhr.status === 200) {
                resolve(JSON.parse(xhr.responseText));
            }
            else {
                reject(new Error(`Upload failed with status ${xhr.status}`));
            }
        });
        xhr.addEventListener('error', () => reject(new Error('Upload failed')));
        xhr.addEventListener('abort', () => reject(new Error('Upload cancelled')));
        xhr.open('POST', '/admin/storage/upload');
        xhr.setRequestHeader('Authorization', `Bearer ${localStorage.getItem('token') || ''}`);
        xhr.send(formData);
    });
}
export async function uploadMultiple(files, destinationPath, onProgress) {
    const results = [];
    for (let i = 0; i < files.length; i++) {
        const result = await uploadFile(files[i], destinationPath, (progress) => {
            if (onProgress)
                onProgress(i, progress);
        });
        results.push(result);
    }
    return results;
}
// ── Storage Management ───────────────────────────────────────────────────────
export async function getStorageStats() {
    return apiGet('/admin/storage/stats');
}
export async function getStorageQuota() {
    return apiGet('/admin/storage/quota');
}
export async function getTopLargeFiles(limit = 10) {
    return apiGet(`/admin/storage/large-files?limit=${limit}`);
}
export async function searchFiles(query, path = '/') {
    return apiGet(`/admin/storage/search?q=${encodeURIComponent(query)}&path=${encodeURIComponent(path)}`);
}
// ── Cleanup & Maintenance ────────────────────────────────────────────────────
export async function deleteOldFiles(daysOld) {
    return apiPost(`/admin/storage/cleanup`, { daysOld });
}
export async function verifyIntegrity() {
    return apiPost(`/admin/storage/verify`, {});
}
export async function compressFiles(paths) {
    return apiPost(`/admin/storage/compress`, { paths });
}
export async function extractArchive(archivePath, destinationPath) {
    return apiPost(`/admin/storage/extract`, { archivePath, destinationPath });
}
