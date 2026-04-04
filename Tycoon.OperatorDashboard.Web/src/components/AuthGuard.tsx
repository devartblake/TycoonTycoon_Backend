'use client'

// React Imports
import { useEffect, useState } from 'react'

// Next Imports
import { useRouter } from 'next/navigation'

// MUI Imports
import CircularProgress from '@mui/material/CircularProgress'
import Box from '@mui/material/Box'

// Component Imports
import PermissionDenied from '@components/admin/PermissionDenied'

// Lib Imports
import { isAuthenticated, refresh, fetchProfile } from '@/lib/auth'
import { isApiError } from '@/lib/apiErrors'

type GuardState = 'loading' | 'ready' | 'forbidden'

export default function AuthGuard({ children }: { children: React.ReactNode }) {
  const router = useRouter()
  const [state, setState] = useState<GuardState>('loading')
  const [forbiddenMessage, setForbiddenMessage] = useState<string | undefined>()

  useEffect(() => {
    async function check() {
      if (!isAuthenticated()) {
        router.replace('/login')

        return
      }

      // Try to refresh the access token from stored refresh token
      const refreshed = await refresh()

      if (!refreshed) {
        router.replace('/login')

        return
      }

      // Validate the token by fetching the profile
      try {
        const profile = await fetchProfile()

        if (!profile) {
          router.replace('/login')

          return
        }

        setState('ready')
      } catch (err) {
        if (isApiError(err) && (err.code === 'FORBIDDEN' || err.status === 403)) {
          setForbiddenMessage(err.message)
          setState('forbidden')

          return
        }

        // Any other error — treat as unauthenticated
        router.replace('/login')
      }
    }

    check()
  }, [router])

  if (state === 'forbidden') {
    return <PermissionDenied message={forbiddenMessage} />
  }

  if (state === 'loading') {
    return (
      <Box display='flex' justifyContent='center' alignItems='center' minHeight='100dvh'>
        <CircularProgress />
      </Box>
    )
  }

  return <>{children}</>
}
