'use client'

import Alert from '@mui/material/Alert'
import AlertTitle from '@mui/material/AlertTitle'
import Box from '@mui/material/Box'
import Button from '@mui/material/Button'
import Typography from '@mui/material/Typography'

import { logout } from '@/lib/auth'

interface PermissionDeniedProps {
  message?: string
}

const PermissionDenied = ({ message }: PermissionDeniedProps) => {
  return (
    <Box
      display='flex'
      flexDirection='column'
      alignItems='center'
      justifyContent='center'
      minHeight='60vh'
      gap={3}
      px={2}
    >
      <Alert severity='error' sx={{ maxWidth: 480, width: '100%' }}>
        <AlertTitle>Permission Denied</AlertTitle>
        {message || 'You do not have the required role or permissions to access this page.'}
      </Alert>
      <Typography variant='body2' color='text.secondary'>
        If you believe this is an error, contact your administrator or try signing in with a different account.
      </Typography>
      <Button variant='outlined' onClick={() => logout()}>
        Sign in with a different account
      </Button>
    </Box>
  )
}

export default PermissionDenied
