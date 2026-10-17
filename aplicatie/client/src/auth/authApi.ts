import { api } from '@/api/client'
import type { Schemas } from '@/api/types'
import { useSession } from './session'

export type LoginRequest = Schemas['LoginRequest']
export type RegisterRequest = Schemas['RegisterRequest']

/** Throws the ProblemDetails body on failure, so callers can show the API's message. */
export async function login(body: LoginRequest) {
  const { data, error, response } = await api.POST('/api/auth/login', { body })
  if (!data) {
    // The status travels with the body: a 429 from the rate limiter has no body at all.
    throw { ...(error as object), status: response.status }
  }
  useSession.getState().signIn(data)
  return data
}

export async function register(body: RegisterRequest) {
  const { data, error, response } = await api.POST('/api/auth/register', { body })
  if (!data) {
    // The status travels with the body: a 429 from the rate limiter has no body at all.
    throw { ...(error as object), status: response.status }
  }
  useSession.getState().signIn(data)
  return data
}

/** Ends the session on this device only; the local state is cleared even if the server can't be reached. */
export async function logout() {
  try {
    await api.POST('/api/auth/logout')
  } finally {
    useSession.getState().signOut()
  }
}
