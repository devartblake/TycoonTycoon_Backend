/**
 * Storage Browser types
 */

export interface StorageFile {
  id: string
  name: string
  path: string
  type: 'file' | 'directory'
  size: number
  mimeType?: string
  createdAt: string
  modifiedAt: string
  permissions: string
}

export interface StorageFolder {
  id: string
  path: string
  name: string
  files: StorageFile[]
  subFolders: StorageFolder[]
  totalSize: number
  fileCount: number
}

export interface StorageStats {
  totalSize: number
  usedSize: number
  freeSize: number
  fileCount: number
  folderCount: number
  topLargeFiles: {
    name: string
    size: number
    path: string
  }[]
}

export interface FileUploadProgress {
  id: string
  fileName: string
  progress: number
  status: 'queued' | 'uploading' | 'completed' | 'failed'
  uploadedBytes: number
  totalBytes: number
  speed: number // bytes per second
  estimatedTime: number // seconds remaining
  error?: string
}

export interface FileMetadata {
  fileName: string
  size: number
  mimeType: string
  hash: string
  uploadedAt: string
  uploadedBy: string
}

export interface StorageQuota {
  maxSize: number
  usedSize: number
  freeSize: number
  warningThreshold: number // percentage
  quotaExceeded: boolean
}

export type SortBy = 'name' | 'size' | 'modified'
export type SortOrder = 'asc' | 'desc'
export type FileFilter = 'all' | 'images' | 'videos' | 'documents' | 'archives'
