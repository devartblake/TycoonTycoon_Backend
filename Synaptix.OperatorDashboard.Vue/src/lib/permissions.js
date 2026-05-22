export function hasPermission(requiredPermission, permissionSet = []) {
  return Array.isArray(permissionSet) && permissionSet.includes(requiredPermission)
}

export function canViewRoute(to, permissionSet = []) {
  const requiredPermission = to.meta?.requiredPermission
  if (!requiredPermission) return true
  return hasPermission(requiredPermission, permissionSet)
}
