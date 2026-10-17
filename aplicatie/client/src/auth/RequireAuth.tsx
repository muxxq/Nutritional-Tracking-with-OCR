import { Loader2 } from 'lucide-react'
import { Navigate, Outlet, useLocation } from 'react-router'
import { useSession } from './session'

/** Guards the app's routes: waits for the start-up session restore, then sends signed-out users to /login. */
export function RequireAuth() {
  const status = useSession((s) => s.status)
  const location = useLocation()

  if (status === 'loading') {
    return (
      <div className="min-h-screen flex items-center justify-center" role="status" aria-label="Se încarcă sesiunea">
        <Loader2 className="w-8 h-8 animate-spin text-emerald-600" />
      </div>
    )
  }

  if (status === 'signedOut') {
    return <Navigate to="/login" replace state={{ from: location.pathname }} />
  }

  return <Outlet />
}

/** The login and register pages: a signed-in user goes straight to the app. */
export function RedirectIfSignedIn() {
  const status = useSession((s) => s.status)
  if (status === 'loading') {
    return null
  }
  return status === 'signedIn' ? <Navigate to="/" replace /> : <Outlet />
}
