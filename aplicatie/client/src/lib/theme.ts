import { useState } from 'react'

export type Theme = 'light' | 'dark'

const KEY = 'nutriscan-theme'

/** The stored choice, else the operating system's preference (index.html applies the same rule before first paint). */
export function initialTheme(): Theme {
  try {
    const stored = localStorage.getItem(KEY)
    if (stored === 'light' || stored === 'dark') {
      return stored
    }
  } catch {
    // Private mode or blocked storage: fall through to the system preference.
  }
  return window.matchMedia?.('(prefers-color-scheme: dark)').matches ? 'dark' : 'light'
}

export function useTheme() {
  const [theme, setTheme] = useState<Theme>(initialTheme)

  const toggle = () => {
    const next: Theme = theme === 'dark' ? 'light' : 'dark'
    setTheme(next)
    document.documentElement.classList.toggle('dark', next === 'dark')
    try {
      localStorage.setItem(KEY, next)
    } catch {
      // The choice then lasts only for this visit.
    }
  }

  return { theme, toggle }
}
