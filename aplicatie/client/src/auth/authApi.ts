import { api } from '@/api/client'
import type { Schemas } from '@/api/types'
import { unwrap } from '@/api/unwrap'
import { useSession } from './session'

export type LoginRequest = Schemas['LoginRequest']
export type RegisterRequest = Schemas['RegisterRequest']

/** Throws the ProblemDetails body on failure, so callers can show the API's message. */
export async function login(body: LoginRequest) {
  const data = unwrap(await api.POST('/api/auth/login', { body }))
  useSession.getState().signIn(data)
  return data
}

export async function register(body: RegisterRequest) {
  const data = unwrap(await api.POST('/api/auth/register', { body }))
  useSession.getState().signIn(data)
  return data
}

/** The service worker's cache of the food catalog (vite.config.ts); it may hold the user's own foods. */
export const CATALOG_CACHE = 'api-catalog'

/**
 * Ends the session on this device only; the local state is cleared even if the server can't be reached.
 * The cached catalog is dropped too, so the next person on a shared device doesn't see this user's own foods.
 */
export async function logout() {
  try {
    await api.POST('/api/auth/logout')
  } catch {
    // Offline: the server-side session expires on its own (7 days); signing out here must still work.
  } finally {
    useSession.getState().signOut()
    if ('caches' in window) {
      await caches.delete(CATALOG_CACHE)
    }
  }
}
