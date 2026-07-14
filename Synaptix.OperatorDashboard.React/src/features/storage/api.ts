/**
 * Storage API — Django admin_storage_client + AdminStorageEndpoints.
 *
 *   GET  /admin/storage/prefixes
 *   GET  /admin/storage/objects?prefix=&cursor=&pageSize=
 *   GET  /admin/storage/objects/metadata?key=
 *   POST /admin/storage/upload-intent
 *   POST /admin/storage/upload-proxy  (multipart: key, overwrite, file)
 *
 * There is no /browse, /files, /folders, /stats, or /quota. This client adapts
 * the object-list model into the existing StorageFolder UI shape.
 */

import { apiGet } from '@/lib/api-client'
import { useAuthStore } from '@/features/auth/store'
import type { StorageFolder, StorageFile, StorageStats, StorageQuota, FileMetadata } from './types'

interface BackendPrefix {
  prefix?: string
  Prefix?: string
  name?: string
  Name?: string
  description?: string
  Description?: string
  maxBytes?: number
  MaxBytes?: number
}

interface BackendObjectItem {
  key?: string
  Key?: string
  size?: number
  Size?: number
  lastModified?: string
  LastModified?: string
  etag?: string
  ETag?: string
  isDir?: boolean
  IsDir?: boolean
}

interface BackendObjectList {
  items?: BackendObjectItem[]
  Items?: BackendObjectItem[]
  nextCursor?: string | null
  NextCursor?: string | null
  isTruncated?: boolean
  IsTruncated?: boolean
}

function authHeader(): Record<string, string> {
  const token = useAuthStore.getState().accessToken
  return token ? { Authorization: `Bearer ${token}` } : {}
}

function normalizePath(path: string): string {
  let p = path.replace(/\\/g, '/').replace(/^\/+/, '')
  if (p && !p.endsWith('/')) {
    // treat as prefix if it looks like a folder navigation
  }
  return p
}

function prefixFromPath(path: string): string {
  const p = normalizePath(path)
  if (!p || p === '/') return ''
  return p.endsWith('/') ? p : `${p}/`
}

function fileName(key: string): string {
  const parts = key.replace(/\/+$/, '').split('/')
  return parts[parts.length - 1] || key
}

function toStorageFile(item: BackendObjectItem): StorageFile {
  const key = item.key ?? item.Key ?? ''
  const isDir = Boolean(item.isDir ?? item.IsDir ?? key.endsWith('/'))
  const size = Number(item.size ?? item.Size ?? 0)
  const modified = String(item.lastModified ?? item.LastModified ?? new Date().toISOString())
  return {
    id: key,
    name: fileName(key),
    path: key,
    type: isDir ? 'directory' : 'file',
    size,
    createdAt: modified,
    modifiedAt: modified,
    permissions: 'rw',
  }
}

/**
 * List objects under a prefix and adapt to StorageFolder for the browser UI.
 * Root ("/") lists configured prefixes as virtual folders.
 */
export async function getStorageFolder(path: string = '/'): Promise<StorageFolder> {
  const prefix = prefixFromPath(path)

  if (!prefix) {
    const meta = await apiGet<{
      prefixes: BackendPrefix[]
    }>('/admin/storage/prefixes')
    const prefixes = meta.prefixes ?? []
    const subFolders: StorageFolder[] = prefixes.map((p) => {
      const pref = p.prefix ?? p.Prefix ?? ''
      const name = p.name ?? p.Name ?? pref
      return {
        id: pref,
        path: pref,
        name,
        files: [],
        subFolders: [],
        totalSize: 0,
        fileCount: 0,
      }
    })
    return {
      id: '/',
      path: '/',
      name: 'Root',
      files: [],
      subFolders,
      totalSize: 0,
      fileCount: 0,
    }
  }

  const res = await apiGet<BackendObjectList>(
    `/admin/storage/objects?prefix=${encodeURIComponent(prefix)}&pageSize=200`
  )
  const raw = res.items ?? res.Items ?? []
  const files: StorageFile[] = []
  const subFolderMap = new Map<string, StorageFolder>()

  for (const item of raw) {
    const key = item.key ?? item.Key ?? ''
    if (!key || key === prefix) continue
    const relative = key.startsWith(prefix) ? key.slice(prefix.length) : key
    const slash = relative.indexOf('/')
    if (slash >= 0 && slash < relative.length - 1) {
      // nested: first segment is a virtual subfolder
      const seg = relative.slice(0, slash + 1)
      const childPath = prefix + seg
      if (!subFolderMap.has(childPath)) {
        subFolderMap.set(childPath, {
          id: childPath,
          path: childPath,
          name: seg.replace(/\/$/, ''),
          files: [],
          subFolders: [],
          totalSize: 0,
          fileCount: 0,
        })
      }
      continue
    }
    files.push(toStorageFile(item))
  }

  const totalSize = files.reduce((s, f) => s + f.size, 0)
  return {
    id: prefix,
    path: prefix,
    name: fileName(prefix.replace(/\/$/, '')) || prefix,
    files,
    subFolders: Array.from(subFolderMap.values()),
    totalSize,
    fileCount: files.filter((f) => f.type === 'file').length,
  }
}

export async function getFileContent(path: string): Promise<Blob> {
  // No download route on admin storage; fetch public metadata/url if available
  const meta = await apiGet<Record<string, unknown>>(
    `/admin/storage/objects/metadata?key=${encodeURIComponent(normalizePath(path))}`
  )
  const url = (meta.url ?? meta.publicUrl ?? meta.Url) as string | undefined
  if (!url) throw new Error('Object has no download URL; use object storage console or public URL policy.')
  const response = await fetch(url)
  return response.blob()
}

export async function deleteFile(_path: string): Promise<{ success: boolean }> {
  void _path
  throw new Error('Object delete is not exposed on /admin/storage; manage lifecycle in MinIO/policy.')
}

export async function renameFile(_oldPath: string, _newName: string): Promise<FileMetadata> {
  void _oldPath
  void _newName
  throw new Error('Rename is not supported by the admin storage API.')
}

export async function moveFile(_sourcePath: string, _destinationPath: string): Promise<{ success: boolean }> {
  void _sourcePath
  void _destinationPath
  throw new Error('Move is not supported by the admin storage API.')
}

export async function createFolder(_path: string, _folderName: string): Promise<{ success: boolean }> {
  void _path
  void _folderName
  // Prefixes are fixed policy; "folders" appear when objects are uploaded under a key path.
  throw new Error('Folders are implied by object key prefixes. Upload a file under the desired prefix.')
}

export async function deleteFolder(_path: string, _recursive: boolean = false): Promise<{ success: boolean }> {
  void _path
  void _recursive
  throw new Error('Folder delete is not supported by the admin storage API.')
}

export async function uploadFile(
  file: File,
  destinationPath: string,
  onProgress?: (progress: number, speed: number, estimatedTime: number) => void
): Promise<FileMetadata> {
  const prefix = prefixFromPath(destinationPath)
  const key = `${prefix}${file.name}`.replace(/^\/+/, '')

  return new Promise((resolve, reject) => {
    const formData = new FormData()
    formData.append('file', file)
    formData.append('key', key)
    formData.append('overwrite', 'false')

    const xhr = new XMLHttpRequest()
    const startTime = Date.now()
    let lastProgress = 0
    let lastTime = startTime

    if (onProgress) {
      xhr.upload.addEventListener('progress', (e) => {
        if (!e.lengthComputable) return
        const progress = (e.loaded / e.total) * 100
        const currentTime = Date.now()
        const timeDiff = (currentTime - lastTime) / 1000
        const progressDiff = e.loaded - lastProgress
        if (timeDiff > 0) {
          const speed = progressDiff / timeDiff
          const remaining = e.total - e.loaded
          const estimatedTime = speed > 0 ? remaining / speed : 0
          onProgress(progress, speed, estimatedTime)
          lastProgress = e.loaded
          lastTime = currentTime
        }
      })
    }

    xhr.addEventListener('load', () => {
      if (xhr.status >= 200 && xhr.status < 300) {
        const body = JSON.parse(xhr.responseText || '{}') as { key?: string; url?: string }
        resolve({
          fileName: file.name,
          size: file.size,
          mimeType: file.type,
          hash: '',
          uploadedAt: new Date().toISOString(),
          uploadedBy: 'operator',
        })
        void body
      } else {
        reject(new Error(`Upload failed with status ${xhr.status}: ${xhr.responseText}`))
      }
    })
    xhr.addEventListener('error', () => reject(new Error('Upload failed')))
    xhr.addEventListener('abort', () => reject(new Error('Upload cancelled')))

    xhr.open('POST', '/admin/storage/upload-proxy')
    const headers = authHeader()
    for (const [k, v] of Object.entries(headers)) {
      xhr.setRequestHeader(k, v)
    }
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
      onProgress?.(i, progress)
    })
    results.push(result)
  }
  return results
}

/** No /stats — derive lightweight stats from prefixes + sample listing. */
export async function getStorageStats(): Promise<StorageStats> {
  try {
    const root = await getStorageFolder('/')
    return {
      totalSize: 0,
      usedSize: 0,
      freeSize: 0,
      fileCount: 0,
      folderCount: root.subFolders.length,
      topLargeFiles: [],
    }
  } catch {
    return {
      totalSize: 0,
      usedSize: 0,
      freeSize: 0,
      fileCount: 0,
      folderCount: 0,
      topLargeFiles: [],
    }
  }
}

/** No /quota endpoint — soft placeholder so UI does not hard-fail. */
export async function getStorageQuota(): Promise<StorageQuota> {
  return {
    maxSize: 0,
    usedSize: 0,
    freeSize: 0,
    warningThreshold: 90,
    quotaExceeded: false,
  }
}

export async function getTopLargeFiles(_limit: number = 10): Promise<{ fileName: string; size: number; path: string }[]> {
  void _limit
  return []
}

export async function searchFiles(query: string, path: string = '/'): Promise<StorageFolder[]> {
  const folder = await getStorageFolder(path)
  const q = query.toLowerCase()
  const matchedFiles = folder.files.filter((f) => f.name.toLowerCase().includes(q))
  if (matchedFiles.length === 0) return []
  return [
    {
      ...folder,
      files: matchedFiles,
      subFolders: [],
      fileCount: matchedFiles.length,
    },
  ]
}

export async function deleteOldFiles(_daysOld: number): Promise<{ deleted: number; freedSpace: number }> {
  void _daysOld
  throw new Error('Cleanup is not supported by the admin storage API.')
}

export async function verifyIntegrity(): Promise<{ verified: boolean; issues: string[] }> {
  throw new Error('Integrity verify is not supported by the admin storage API.')
}

export async function compressFiles(_paths: string[]): Promise<{ success: boolean; archivePath: string }> {
  void _paths
  throw new Error('Compress is not supported by the admin storage API.')
}

export async function extractArchive(
  _archivePath: string,
  _destinationPath: string
): Promise<{ success: boolean; extractedCount: number }> {
  void _archivePath
  void _destinationPath
  throw new Error('Extract is not supported by the admin storage API.')
}

/** Django list_storage_prefixes */
export async function listStoragePrefixes(): Promise<unknown> {
  return apiGet('/admin/storage/prefixes')
}

/** Django get_storage_object_metadata */
export async function getObjectMetadata(key: string): Promise<unknown> {
  return apiGet(`/admin/storage/objects/metadata?key=${encodeURIComponent(key)}`)
}
