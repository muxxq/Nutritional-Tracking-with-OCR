import type { AuthResponse } from '@/api/types'
import { useSession } from './session'

const REFRESH_URL = '/api/auth/refresh'

let refreshInFlight: Promise<boolean> | null = null

/**
 * Exchanges the HttpOnly refresh cookie for a new access token. Concurrent callers share one request:
 * the server rotates the refresh token on every use and treats a second use of the old one as theft (ADR-002),
 * so two parallel refreshes would sign the user out.
 */
export function refreshSession(): Promise<boolean> {
  refreshInFlight ??= (async () => {
    try {
      const response = await fetch(REFRESH_URL, { method: 'POST', credentials: 'include' })
      if (!response.ok) {
        useSession.getState().signOut()
        return false
      }
      useSession.getState().signIn((await response.json()) as AuthResponse)
      return true
    } catch {
      useSession.getState().signOut()
      return false
    } finally {
      refreshInFlight = null
    }
  })()
  return refreshInFlight
}

function withAccessToken(request: Request): Request {
  const token = useSession.getState().accessToken
  if (!token) {
    return request
  }
  const headers = new Headers(request.headers)
  headers.set('Authorization', `Bearer ${token}`)
  return new Request(request, { headers })
}

const isAuthEndpoint = (request: Request) => new URL(request.url, window.location.origin).pathname.startsWith('/api/auth/')

/**
 * fetch for the typed API client: adds the access token and, when it has expired (401), refreshes the session
 * once and replays the request. Auth endpoints are never replayed: their 401 is the answer.
 */
export async function authFetch(request: Request): Promise<Response> {
  const replay = request.clone()
  const response = await fetch(withAccessToken(request))
  if (response.status !== 401 || isAuthEndpoint(request)) {
    return response
  }
  return (await refreshSession()) ? fetch(withAccessToken(replay)) : response
}
