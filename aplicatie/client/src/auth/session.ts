import { create } from 'zustand'
import type { AuthResponse, UserDto } from '@/api/types'

export type SessionStatus = 'loading' | 'signedIn' | 'signedOut'

interface SessionState {
  status: SessionStatus
  /** Kept in memory only: never in localStorage, where an XSS payload could read it (ADR-002, ADR-011). */
  accessToken: string | null
  user: UserDto | null
  signIn: (auth: AuthResponse) => void
  updateUser: (user: UserDto) => void
  signOut: () => void
}

export const useSession = create<SessionState>()((set) => ({
  status: 'loading',
  accessToken: null,
  user: null,
  signIn: (auth) => set({ status: 'signedIn', accessToken: auth.accessToken, user: auth.user }),
  updateUser: (user) => set({ user }),
  signOut: () => set({ status: 'signedOut', accessToken: null, user: null }),
}))
