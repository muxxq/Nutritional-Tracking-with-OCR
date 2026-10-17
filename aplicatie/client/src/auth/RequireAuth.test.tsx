import { render, screen } from '@testing-library/react'
import { MemoryRouter, Route, Routes } from 'react-router'
import { beforeEach, describe, expect, it } from 'vitest'
import { RedirectIfSignedIn, RequireAuth } from './RequireAuth'
import { useSession } from './session'

const user = { id: '1', email: 'ana@exemplu.ro', firstName: 'Ana', lastName: 'Pop', isProfileComplete: true }

function renderAt(path: string) {
  render(
    <MemoryRouter initialEntries={[path]}>
      <Routes>
        <Route element={<RedirectIfSignedIn />}>
          <Route path="/login" element={<p>pagina de login</p>} />
        </Route>
        <Route element={<RequireAuth />}>
          <Route path="/diary" element={<p>jurnalul</p>} />
          <Route path="/" element={<p>acasă</p>} />
        </Route>
      </Routes>
    </MemoryRouter>,
  )
}

describe('RequireAuth', () => {
  beforeEach(() => useSession.setState({ status: 'loading', accessToken: null, user: null }))

  it('waits for the session restore instead of redirecting', () => {
    // Act
    renderAt('/diary')

    // Assert
    expect(screen.getByRole('status')).toBeInTheDocument()
    expect(screen.queryByText('pagina de login')).not.toBeInTheDocument()
  })

  it('sends a signed-out user to /login', () => {
    // Arrange
    useSession.getState().signOut()

    // Act
    renderAt('/diary')

    // Assert
    expect(screen.getByText('pagina de login')).toBeInTheDocument()
  })

  it('shows the page to a signed-in user', () => {
    // Arrange
    useSession.getState().signIn({ accessToken: 't', accessTokenExpiresAtUtc: '2026-10-17T12:00:00Z', user })

    // Act
    renderAt('/diary')

    // Assert
    expect(screen.getByText('jurnalul')).toBeInTheDocument()
  })

  it('sends a signed-in user away from /login', () => {
    // Arrange
    useSession.getState().signIn({ accessToken: 't', accessTokenExpiresAtUtc: '2026-10-17T12:00:00Z', user })

    // Act
    renderAt('/login')

    // Assert
    expect(screen.getByText('acasă')).toBeInTheDocument()
  })
})
