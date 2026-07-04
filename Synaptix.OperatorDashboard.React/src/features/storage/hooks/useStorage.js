import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import * as api from '../api';
// Folder Navigation
export function useStorageFolder(path = '/') {
    return useQuery({
        queryKey: ['storage-folder', path],
        queryFn: () => api.getStorageFolder(path),
    });
}
// File Operations
export function useDeleteFile() {
    const queryClient = useQueryClient();
    return useMutation({
        mutationFn: (path) => api.deleteFile(path),
        onSuccess: () => queryClient.invalidateQueries({ queryKey: ['storage-folder'] }),
    });
}
export function useRenameFile() {
    const queryClient = useQueryClient();
    return useMutation({
        mutationFn: ({ oldPath, newName }) => api.renameFile(oldPath, newName),
        onSuccess: () => queryClient.invalidateQueries({ queryKey: ['storage-folder'] }),
    });
}
export function useMoveFile() {
    const queryClient = useQueryClient();
    return useMutation({
        mutationFn: ({ sourcePath, destinationPath }) => api.moveFile(sourcePath, destinationPath),
        onSuccess: () => queryClient.invalidateQueries({ queryKey: ['storage-folder'] }),
    });
}
// Folder Operations
export function useCreateFolder() {
    const queryClient = useQueryClient();
    return useMutation({
        mutationFn: ({ path, folderName }) => api.createFolder(path, folderName),
        onSuccess: () => queryClient.invalidateQueries({ queryKey: ['storage-folder'] }),
    });
}
export function useDeleteFolder() {
    const queryClient = useQueryClient();
    return useMutation({
        mutationFn: ({ path, recursive }) => api.deleteFolder(path, recursive),
        onSuccess: () => queryClient.invalidateQueries({ queryKey: ['storage-folder'] }),
    });
}
// File Upload
export function useUploadFile() {
    const queryClient = useQueryClient();
    return useMutation({
        mutationFn: ({ file, destinationPath, onProgress, }) => api.uploadFile(file, destinationPath, onProgress),
        onSuccess: () => queryClient.invalidateQueries({ queryKey: ['storage-folder'] }),
    });
}
export function useUploadMultiple() {
    const queryClient = useQueryClient();
    return useMutation({
        mutationFn: ({ files, destinationPath, onProgress, }) => api.uploadMultiple(files, destinationPath, onProgress),
        onSuccess: () => queryClient.invalidateQueries({ queryKey: ['storage-folder'] }),
    });
}
// Storage Management
export function useStorageStats() {
    return useQuery({
        queryKey: ['storage-stats'],
        queryFn: () => api.getStorageStats(),
        staleTime: 1000 * 60, // 1 minute
    });
}
export function useStorageQuota() {
    return useQuery({
        queryKey: ['storage-quota'],
        queryFn: () => api.getStorageQuota(),
        staleTime: 1000 * 60 * 5, // 5 minutes
    });
}
export function useTopLargeFiles(limit = 10) {
    return useQuery({
        queryKey: ['top-large-files', limit],
        queryFn: () => api.getTopLargeFiles(limit),
        staleTime: 1000 * 60 * 5, // 5 minutes
    });
}
export function useSearchFiles() {
    return useMutation({
        mutationFn: ({ query, path }) => api.searchFiles(query, path),
    });
}
// Cleanup & Maintenance
export function useDeleteOldFiles() {
    const queryClient = useQueryClient();
    return useMutation({
        mutationFn: (daysOld) => api.deleteOldFiles(daysOld),
        onSuccess: () => queryClient.invalidateQueries({ queryKey: ['storage-stats'] }),
    });
}
export function useVerifyIntegrity() {
    return useMutation({
        mutationFn: () => api.verifyIntegrity(),
    });
}
export function useCompressFiles() {
    const queryClient = useQueryClient();
    return useMutation({
        mutationFn: (paths) => api.compressFiles(paths),
        onSuccess: () => queryClient.invalidateQueries({ queryKey: ['storage-folder'] }),
    });
}
export function useExtractArchive() {
    const queryClient = useQueryClient();
    return useMutation({
        mutationFn: ({ archivePath, destinationPath }) => api.extractArchive(archivePath, destinationPath),
        onSuccess: () => queryClient.invalidateQueries({ queryKey: ['storage-folder'] }),
    });
}
