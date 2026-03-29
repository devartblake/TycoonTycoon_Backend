import type { ReactNode } from 'react'

// Next Imports
import NextLink from 'next/link'

// MUI Imports
import Box from '@mui/material/Box'
import Typography from '@mui/material/Typography'
import Breadcrumbs from '@mui/material/Breadcrumbs'
import Link from '@mui/material/Link'

export interface Breadcrumb {
  label: string
  href?: string
}

export interface PageHeaderProps {
  title: string
  breadcrumbs?: Breadcrumb[]
  actions?: ReactNode
}

const PageHeader = ({ title, breadcrumbs, actions }: PageHeaderProps) => {
  return (
    <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', mb: 4 }}>
      <Box>
        {breadcrumbs && breadcrumbs.length > 0 && (
          <Breadcrumbs sx={{ mb: 1 }}>
            {breadcrumbs.map((crumb, idx) =>
              crumb.href ? (
                <Link key={idx} component={NextLink} href={crumb.href} underline='hover' color='inherit'>
                  {crumb.label}
                </Link>
              ) : (
                <Typography key={idx} color='text.primary'>
                  {crumb.label}
                </Typography>
              )
            )}
          </Breadcrumbs>
        )}
        <Typography variant='h4'>{title}</Typography>
      </Box>
      {actions && <Box sx={{ display: 'flex', gap: 1 }}>{actions}</Box>}
    </Box>
  )
}

export default PageHeader
