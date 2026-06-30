/**
 * Permission gate component for RBAC
 * Conditionally renders content based on user permissions
 */

import React from 'react'
import type { Permission } from '@/types/auth'
import { usePermission, useAnyPermission, useAllPermissions } from '@/hooks/use-permission'

interface PermissionGateProps {
  children: React.ReactNode
  /** Single permission to check */
  permission?: Permission
  /** Check if user has ANY of these permissions */
  anyOf?: Permission[]
  /** Check if user has ALL of these permissions */
  allOf?: Permission[]
  /** Fallback component when permission is denied */
  fallback?: React.ReactNode
}

/**
 * Gate component for single permission
 */
export function PermissionGate({
  children,
  permission,
  anyOf,
  allOf,
  fallback = null,
}: PermissionGateProps) {
  const hasPermission = usePermission(permission ?? '' as Permission)
  const hasAnyPermission = useAnyPermission(anyOf ?? [])
  const hasAllPermissions = useAllPermissions(allOf ?? [])

  // Determine if user has access
  let hasAccess = true
  if (permission) {
    hasAccess = hasPermission
  } else if (anyOf) {
    hasAccess = hasAnyPermission
  } else if (allOf) {
    hasAccess = hasAllPermissions
  }

  if (!hasAccess) {
    return fallback
  }

  return <>{children}</>
}

/**
 * Hook for checking permissions in non-component code
 */
export function usePermissionCheck(
  permission?: Permission,
  anyOf?: Permission[],
  allOf?: Permission[]
): boolean {
  const hasPermission = usePermission(permission ?? '' as Permission)
  const hasAnyPermission = useAnyPermission(anyOf ?? [])
  const hasAllPermissions = useAllPermissions(allOf ?? [])

  if (permission) {
    return hasPermission
  }
  if (anyOf) {
    return hasAnyPermission
  }
  if (allOf) {
    return hasAllPermissions
  }

  return true
}
