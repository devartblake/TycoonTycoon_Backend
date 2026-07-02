import { Fragment as _Fragment, jsx as _jsx } from "react/jsx-runtime";
import { usePermission, useAnyPermission, useAllPermissions } from '@/hooks/use-permission';
/**
 * Gate component for single permission
 */
export function PermissionGate({ children, permission, anyOf, allOf, fallback = null, }) {
    const hasPermission = usePermission(permission ?? '');
    const hasAnyPermission = useAnyPermission(anyOf ?? []);
    const hasAllPermissions = useAllPermissions(allOf ?? []);
    // Determine if user has access
    let hasAccess = true;
    if (permission) {
        hasAccess = hasPermission;
    }
    else if (anyOf) {
        hasAccess = hasAnyPermission;
    }
    else if (allOf) {
        hasAccess = hasAllPermissions;
    }
    if (!hasAccess) {
        return fallback;
    }
    return _jsx(_Fragment, { children: children });
}
/**
 * Hook for checking permissions in non-component code
 */
export function usePermissionCheck(permission, anyOf, allOf) {
    const hasPermission = usePermission(permission ?? '');
    const hasAnyPermission = useAnyPermission(anyOf ?? []);
    const hasAllPermissions = useAllPermissions(allOf ?? []);
    if (permission) {
        return hasPermission;
    }
    if (anyOf) {
        return hasAnyPermission;
    }
    if (allOf) {
        return hasAllPermissions;
    }
    return true;
}
