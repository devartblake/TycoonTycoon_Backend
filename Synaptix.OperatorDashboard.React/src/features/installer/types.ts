/**
 * Backend Installer types
 */

export interface InstallationStep {
  id: string
  name: string
  description: string
  status: 'pending' | 'running' | 'completed' | 'failed'
  progress: number
  details?: string
  startedAt?: string
  completedAt?: string
  error?: string
}

export interface InstallerConfig {
  environment: 'development' | 'staging' | 'production'
  databaseUrl: string
  apiPort: number
  enableMonitoring: boolean
  enableSentry: boolean
  sentryDsn?: string
  maxConnections: number
  logLevel: 'debug' | 'info' | 'warn' | 'error'
  features: {
    authentication: boolean
    commerce: boolean
    notifications: boolean
    analytics: boolean
    compliance: boolean
  }
}

export interface InstallationStatus {
  currentStep: number
  totalSteps: number
  progress: number
  status: 'not-started' | 'in-progress' | 'completed' | 'failed'
  steps: InstallationStep[]
  startedAt?: string
  completedAt?: string
  logs: InstallationLog[]
}

export interface InstallationLog {
  id: string
  timestamp: string
  level: 'debug' | 'info' | 'warn' | 'error'
  message: string
  stepId: string
}

export interface BackendBundle {
  id: string
  name: string
  version: string
  fileName: string
  fileSize: number
  checksum: string
  releaseDate: string
  notes: string
  features: string[]
  breaking: boolean
}

export interface BundleUploadProgress {
  fileName: string
  progress: number
  status: 'uploading' | 'validating' | 'extracting' | 'completed' | 'failed'
  uploadedBytes: number
  totalBytes: number
  error?: string
}

export interface BackendHealth {
  status: 'healthy' | 'degraded' | 'unhealthy'
  uptime: number
  version: string
  database: {
    status: 'connected' | 'disconnected'
    latency: number
  }
  services: {
    name: string
    status: 'running' | 'stopped'
    cpu: number
    memory: number
  }[]
}

export interface InstallationRequirements {
  dotnetVersion: string
  postgresVersion: string
  redisVersion?: string
  diskSpace: number
  ramRequired: number
}
