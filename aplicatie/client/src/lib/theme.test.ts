import { act, renderHook } from '@testing-library/react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { initialTheme, useTheme } from './theme'

const systemPrefersDark = (dark: boolean) =>
  vi.stubGlobal('matchMedia', (query: string) => ({ matches: dark && query.includes('dark') }))

describe('theme', () => {
  afterEach(() => {
    localStorage.clear()
    document.documentElement.classList.remove('dark')
    vi.unstubAllGlobals()
  })

  it('follows the system preference on a first visit', () => {
    systemPrefersDark(true)
    expect(initialTheme()).toBe('dark')
  })

  it("a stored choice wins over the system's", () => {
    systemPrefersDark(true)
    localStorage.setItem('nutriscan-theme', 'light')
    expect(initialTheme()).toBe('light')
  })

  it('toggling applies the class and remembers the choice', () => {
    // Arrange
    systemPrefersDark(false)
    const { result } = renderHook(() => useTheme())

    // Act
    act(() => result.current.toggle())

    // Assert
    expect(result.current.theme).toBe('dark')
    expect(document.documentElement).toHaveClass('dark')
    expect(localStorage.getItem('nutriscan-theme')).toBe('dark')
  })
})
