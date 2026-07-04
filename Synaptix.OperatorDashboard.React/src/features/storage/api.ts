/**
 * Storage Browser API client
 * Handles file operations, uploads, and storage management
 */

import { apiGet, apiPost, apiDelete, apiPut } from '@/lib/api-client'
import type { StorageFolder, StorageStats, StorageQuota, FileMetadata } from './types'

// ── File Operations ──────────────────────────────────────────────────────────

export async function getStorageFolder(path: string = '/'): Promise<StorageFolder> {
  return apiGet(`/admin/storage/browse?path=${encodeURIComponent(path)}`)
}

export async function getFileContent(path: string): Promise<Blob> {
  const response = await fetch(`/admin/storage/download?path=${encodeURIComponent(path)}`, {
    headers: {
      Authorization: `Bearer ${localStorage.getItem('token') || ''}`,
    },
  })
  return response.blob()
}

export async function deleteFile(path: string): Promise<{ success: boolean }> {
  return apiDelete(`/admin/storage/files?path=${encodeURIComponent(path)}`)
}

export async function renameFile(oldPath: string, newName: string): Promise<FileMetadata> {
  return apiPut(`/admin/storage/files/rename`, { oldPath, newName })
}

export async function moveFile(sourcePath: string, destinationPath: string): Promise<{ success: boolean }> {
  return apiPost(`/admin/storage/files/move`, { sourcePath, destinationPath })
}

export async function createFolder(path: string, folderName: string): Promise<{ success: boolean }> {
  return apiPost(`/admin/storage/folders`, { path, folderName })
}

export async function deleteFolder(path: string, recursive: boolean = false): Promise<{ success: boolean }> {
  return apiDelete(`/admin/storage/folders?path=${encodeURIComponent(path)}&recursive=${recursive}`)
}

// ── File Upload ──────────────────────────────────────────────────────────────

export async function uploadFile(
  file: File,
  destinationPath: string,
  onProgress?: (progress: number, speed: number, estimatedTime: number) => void
): Promise<FileMetadata> {
  const formData = new FormData()
  formData.append('file', file)
  formData.append('path', destinationPath)

  return new Promise((resolve, reject) => {
    const xhr = new XMLHttpRequest()
    const startTime = Date.now()
    let lastProgress = 0
    let lastTime = startTime

    if (onProgress) {
      xhr.upload.addEventListener('progress', (e) => {
        if (e.lengthComputable) {
          const progress = (e.loaded / e.total) * 100
          const currentTime = Date.now()
          const timeDiff = (currentTime - lastTime) / 1000 // seconds
          const progressDiff = e.loaded - lastProgress

          if (timeDiff > 0) {
            const speed = progressDiff / timeDiff // bytes per second
            const remaining = e.total - e.loaded
            const estimatedTime = speed > 0 ? remaining / speed : 0

            onProgress(progress, speed, estimatedTime)

            lastProgress = e.loaded
            lastTime = currentTime
          }
        }
      })
    }

    xhr.addEventListener('load', () => {
      if (xhr.status === 200) {
        resolve(JSON.parse(xhr.responseText))
      } else {
        reject(new Error(`Upload failed with status ${xhr.status}`))
      }
    })

    xhr.addEventListener('error', () => reject(new Error('Upload failed')))
    xhr.addEventListener('abort', () => reject(new Error('Upload cancelled')))

    xhr.open('POST', '/admin/storage/upload')
    xhr.setRequestHeader('Authorization', `Bearer ${localStorage.getItem('token') || ''}`)
    xhr.send(formData)
  })
}

export async function uploadMultiple(
  files: File[],
  destinationPath: string,
  onProgress?: (fileIndex: number, progress: number) => void
): Promise<FileMetadata[]> {
  const results: FileMetadata[] = []

  for (let i = 0; i < files.length; i++) {
    const result = await uploadFile(files[i], destinationPath, (progress) => {
      if (onProgress) onProgress(i, progress)
    })
    results.push(result)
  }

  return results
}

// ── Storage Management ───────────────────────────────────────────────────────

export async function getStorageStats(): Promise<StorageStats> {
  return apiGet('/admin/storage/stats')
}

export async function getStorageQuota(): Promise<StorageQuota> {
  return apiGet('/admin/storage/quota')
}

export async function getTopLargeFiles(limit: number = 10): Promise<{ fileName: string; size: number; path: string }[]> {
  return apiGet(`/admin/storage/large-files?limit=${limit}`)
}

export async function searchFiles(query: string, path: string = '/'): Promise<StorageFolder[]> {
  return apiGet(`/admin/storage/search?q=${encodeURIComponent(query)}&path=${encodeURIComponent(path)}`)
}

// ── Cleanup & Maintenance ────────────────────────────────────────────────────

export async function deleteOldFiles(daysOld: number): Promise<{ deleted: number; freedSpace: number }> {
  return apiPost(`/admin/storage/cleanup`, { daysOld })
}

export async function verifyIntegrity(): Promise<{ verified: boolean; issues: string[] }> {
  return apiPost(`/admin/storage/verify`, {})
}

export async function compressFiles(paths: string[]): Promise<{ success: boolean; archivePath: string }> {
  return apiPost(`/admin/storage/compress`, { paths })
}

export async function extractArchive(archivePath: string, destinationPath: string): Promise<{ success: boolean; extractedCount: number }> {
  return apiPost(`/admin/storage/extract`, { archivePath, destinationPath })
}
