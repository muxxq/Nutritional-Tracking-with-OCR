import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { authFetch, refreshSession } from './authFetch'
import { useSession } from './session'

const user = { id: '1', email: 'ana@exemplu.ro', firstName: 'Ana', lastName: 'Pop', isProfileComplete: true }
const auth = (accessToken: string) => ({ accessToken, accessTokenExpiresAtUtc: '2026-10-17T12:00:00Z', user })
const json = (body: unknown, status = 200) => new Response(JSON.stringify(body), { status })

const fetchMock = vi.fn<(input: RequestInfo | URL, init?: RequestInit) => Promise<Response>>()
const urlOf = (call: Parameters<typeof fetchMock>) => new URL(call[0] instanceof Request ? call[0].url : String(call[0]), 'http://localhost').pathname
const authHeaderOf = (call: Parameters<typeof fetchMock>) => (call[0] as Request).headers.get('Authorization')

describe('authFetch', () => {
  beforeEach(() => {
    vi.stubGlobal('fetch', fetchMock)
    useSession.getState().signIn(auth('old'))
  })

  afterEach(() => {
    fetchMock.mockReset()
    vi.unstubAllGlobals()
  })

  it('adds the access token as a bearer header', async () => {
    // Arrange
    fetchMock.mockResolvedValueOnce(json({}))

    // Act
    await authFetch(new Request('http://localhost/api/profile'))

    // Assert
    expect(authHeaderOf(fetchMock.mock.calls[0])).toBe('Bearer old')
  })

  it('on 401 refreshes once and replays the request with the new token', async () => {
    // Arrange
    fetchMock
      .mockResolvedValueOnce(json({}, 401))
      .mockResolvedValueOnce(json(auth('new')))
      .mockResolvedValueOnce(json({ ok: true }))

    // Act
    const response = await authFetch(new Request('http://localhost/api/foods', { method: 'POST', body: '{"name":"Ovăz"}' }))

    // Assert
    expect(response.status).toBe(200)
    expect(fetchMock.mock.calls.map(urlOf)).toEqual(['/api/foods', '/api/auth/refresh', '/api/foods'])
    const replay = fetchMock.mock.calls[2][0] as Request
    expect(replay.headers.get('Authorization')).toBe('Bearer new')
    expect(await replay.text()).toBe('{"name":"Ovăz"}')
  })

  it('shares one refresh between concurrent 401s, because the server rotates the refresh token', async () => {
    // Arrange
    fetchMock.mockImplementation(async (input) => {
      const path = urlOf([input])
      if (path === '/api/auth/refresh') {
        return json(auth('new'))
      }
      return (input as Request).headers.get('Authorization') === 'Bearer new' ? json({}) : json({}, 401)
    })

    // Act
    const responses = await Promise.all([
      authFetch(new Request('http://localhost/api/profile')),
      authFetch(new Request('http://localhost/api/diary/days/2026-10-17')),
      authFetch(new Request('http://localhost/api/plan?from=2026-10-12&to=2026-10-18')),
    ])

    // Assert
    expect(responses.map((r) => r.status)).toEqual([200, 200, 200])
    expect(fetchMock.mock.calls.filter((c) => urlOf(c) === '/api/auth/refresh')).toHaveLength(1)
  })

  it('signs out and returns the 401 when the refresh fails', async () => {
    // Arrange
    fetchMock.mockResolvedValueOnce(json({}, 401)).mockResolvedValueOnce(json({}, 401))

    // Act
    const response = await authFetch(new Request('http://localhost/api/profile'))

    // Assert
    expect(response.status).toBe(401)
    expect(useSession.getState().status).toBe('signedOut')
    expect(useSession.getState().accessToken).toBeNull()
  })

  it('does not refresh after a 401 from an auth endpoint (wrong password)', async () => {
    // Arrange
    fetchMock.mockResolvedValueOnce(json({ title: 'Email sau parolă greșită.' }, 401))

    // Act
    const response = await authFetch(new Request('http://localhost/api/auth/login', { method: 'POST', body: '{}' }))

    // Assert
    expect(response.status).toBe(401)
    expect(fetchMock).toHaveBeenCalledTimes(1)
  })
})

describe('refreshSession', () => {
  afterEach(() => {
    fetchMock.mockReset()
    vi.unstubAllGlobals()
  })

  it('restores the session from the refresh cookie on start-up', async () => {
    // Arrange
    vi.stubGlobal('fetch', fetchMock)
    useSession.setState({ status: 'loading', accessToken: null, user: null })
    fetchMock.mockResolvedValueOnce(json(auth('restored')))

    // Act
    const restored = await refreshSession()

    // Assert
    expect(restored).toBe(true)
    expect(useSession.getState()).toMatchObject({ status: 'signedIn', accessToken: 'restored', user })
    expect(fetchMock.mock.calls[0][1]).toMatchObject({ method: 'POST', credentials: 'include' })
  })
})
