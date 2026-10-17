import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Route, Routes } from 'react-router'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { useSession } from '@/auth/session'
import { LoginPage } from './LoginPage'

const fetchMock = vi.fn<(input: Request) => Promise<Response>>()
const user = { id: '1', email: 'ana@exemplu.ro', firstName: 'Ana', lastName: 'Pop', isProfileComplete: true }

function renderLogin() {
  render(
    <MemoryRouter initialEntries={['/login']}>
      <Routes>
        <Route path="/login" element={<LoginPage />} />
        <Route path="/" element={<p>acasă</p>} />
      </Routes>
    </MemoryRouter>,
  )
}

async function submit(email: string, password: string) {
  await userEvent.type(screen.getByLabelText('Email'), email)
  await userEvent.type(screen.getByLabelText('Parolă'), password)
  await userEvent.click(screen.getByRole('button', { name: 'Intră în cont' }))
}

describe('LoginPage', () => {
  beforeEach(() => {
    vi.stubGlobal('fetch', fetchMock)
    useSession.getState().signOut()
  })

  afterEach(() => {
    fetchMock.mockReset()
    vi.unstubAllGlobals()
  })

  it('signs in and goes to the app', async () => {
    // Arrange
    fetchMock.mockResolvedValueOnce(
      new Response(JSON.stringify({ accessToken: 't', accessTokenExpiresAtUtc: '2026-10-17T12:00:00Z', user }), {
        status: 200, headers: { 'Content-Type': 'application/json' },
      }),
    )
    renderLogin()

    // Act
    await submit('ana@exemplu.ro', 'parola-sigura-123')

    // Assert
    expect(await screen.findByText('acasă')).toBeInTheDocument()
    expect(useSession.getState()).toMatchObject({ status: 'signedIn', accessToken: 't' })
    const request = fetchMock.mock.calls[0][0]
    expect(await request.json()).toEqual({ email: 'ana@exemplu.ro', password: 'parola-sigura-123' })
  })

  it("shows the API's message on wrong credentials", async () => {
    // Arrange
    fetchMock.mockResolvedValueOnce(
      new Response(JSON.stringify({ title: 'Email sau parolă greșită.', status: 401 }), {
        status: 401, headers: { 'Content-Type': 'application/problem+json' },
      }),
    )
    renderLogin()

    // Act
    await submit('ana@exemplu.ro', 'gresit')

    // Assert
    expect(await screen.findByRole('alert')).toHaveTextContent('Email sau parolă greșită.')
    expect(useSession.getState().status).toBe('signedOut')
  })

  it('explains the rate limit, whose 429 has no body', async () => {
    // Arrange
    fetchMock.mockResolvedValueOnce(new Response(null, { status: 429 }))
    renderLogin()

    // Act
    await submit('ana@exemplu.ro', 'x')

    // Assert
    expect(await screen.findByRole('alert')).toHaveTextContent('Prea multe încercări')
  })
})
