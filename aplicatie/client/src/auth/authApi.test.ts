import { afterEach, describe, expect, it, vi } from 'vitest'
import { CATALOG_CACHE, logout } from './authApi'
import { useSession } from './session'

describe('logout', () => {
  afterEach(() => vi.unstubAllGlobals())

  it("signs out and drops the cached catalog, even when the server can't be reached", async () => {
    // Arrange
    const deleteCache = vi.fn(async () => true)
    vi.stubGlobal('caches', { delete: deleteCache })
    vi.stubGlobal('fetch', vi.fn(async () => { throw new TypeError('offline') }))
    useSession.getState().signIn({
      accessToken: 't', accessTokenExpiresAtUtc: '2026-10-17T12:00:00Z',
      user: { id: '1', email: 'ana@exemplu.ro', firstName: 'Ana', lastName: 'Pop', isProfileComplete: true },
    })

    // Act
    await logout()

    // Assert
    expect(useSession.getState().status).toBe('signedOut')
    expect(deleteCache).toHaveBeenCalledWith(CATALOG_CACHE)
  })
})
