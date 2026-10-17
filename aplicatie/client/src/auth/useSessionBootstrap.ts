import { useEffect } from 'react'
import { refreshSession } from './authFetch'
import { useSession } from './session'

/**
 * On start-up the access token is gone (it lives in memory only), but the HttpOnly refresh cookie survives reloads:
 * one refresh restores the session without asking for the password again.
 */
export function useSessionBootstrap() {
  useEffect(() => {
    if (useSession.getState().status === 'loading') {
      void refreshSession()
    }
  }, [])
}
