'use client'

// React Imports
import { useEffect, useState } from 'react'

// Next Imports
import { useRouter } from 'next/navigation'

// MUI Imports
import CircularProgress from '@mui/material/CircularProgress'
import Box from '@mui/material/Box'

// Lib Imports
import { isAuthenticated, refresh, fetchProfile } from '@/lib/auth'

export default function AuthGuard({ children }: { children: React.ReactNode }) {
  const router = useRouter()
  const [ready, setReady] = useState(false)

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
      const profile = await fetchProfile()

      if (!profile) {
        router.replace('/login')

        return
      }

      setReady(true)
    }

    check()
  }, [router])

  if (!ready) {
    return (
      <Box display='flex' justifyContent='center' alignItems='center' minHeight='100dvh'>
        <CircularProgress />
      </Box>
    )
  }

  return <>{children}</>
}
