import Chip from '@mui/material/Chip'

import type { ModerationStatus } from '@/lib/types/admin'
import { ModerationStatusLabel, ModerationStatusColor } from '@/lib/types/admin'

export interface StatusBadgeProps {
  status: ModerationStatus
  size?: 'small' | 'medium'
}

const StatusBadge = ({ status, size = 'small' }: StatusBadgeProps) => {
  const label = ModerationStatusLabel[status] ?? `Status ${status}`

  const color = ModerationStatusColor[status] ?? 'default'

  return <Chip label={label} size={size} color={color} variant='tonal' />
}

export default StatusBadge
