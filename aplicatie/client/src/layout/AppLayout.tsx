import { useState } from 'react'
import { useQueryClient } from '@tanstack/react-query'
import { NavLink, Outlet, useNavigate } from 'react-router'
import { Navbar } from '@/components/Navbar'
import { logout } from '@/auth/authApi'
import { useSession } from '@/auth/session'
import { AddEntryDialog } from '@/features/diary/AddEntryDialog'
import { suggestedMeal } from '@/features/diary/labels'
import { today } from '@/lib/dates'
import { useTheme } from '@/lib/theme'
import { NAV_ITEMS } from './navigation'

/** The signed-in shell: top bar, the current screen, the scan dialog and the mobile bottom bar. */
export function AppLayout() {
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const user = useSession((s) => s.user)
  const { theme, toggle: toggleTheme } = useTheme()
  const [isScanModalOpen, setIsScanModalOpen] = useState(false)

  const handleLogout = async () => {
    await logout()
    // Another account may sign in on this device next: drop every cached response of this one.
    queryClient.clear()
    navigate('/login', { replace: true })
  }

  return (
    <div className="min-h-screen flex flex-col transition-colors duration-200">
      <Navbar
        darkMode={theme === 'dark'}
        onToggleTheme={toggleTheme}
        userName={user ? `${user.firstName} ${user.lastName}` : ''}
        onOpenScanModal={() => setIsScanModalOpen(true)}
        onLogout={handleLogout}
      />

      <main className="flex-1 max-w-7xl w-full mx-auto px-4 py-6 pb-24 md:pb-10">
        <Outlet />
      </main>

      {/* The top bar's scan button: label photo → "Add food" form → today's diary */}
      {isScanModalOpen && (
        <AddEntryDialog date={today()} meal={suggestedMeal()} startWithScan onClose={() => setIsScanModalOpen(false)} />
      )}

      {/* Mobile Bottom Navigation Bar */}
      <nav className="md:hidden fixed bottom-0 left-0 right-0 z-40 bg-white/95 dark:bg-slate-900/95 backdrop-blur-md border-t border-slate-200 dark:border-slate-800 shadow-lg">
        <div className="grid grid-cols-5 h-16">
          {NAV_ITEMS.map(({ to, shortLabel, icon: Icon }) => (
            <NavLink
              key={to}
              to={to}
              className={({ isActive }) =>
                `flex flex-col items-center justify-center gap-1 transition ${
                  isActive
                    ? 'text-emerald-700 dark:text-emerald-400 font-extrabold'
                    : 'text-slate-500 hover:text-slate-800 dark:text-slate-400'
                }`
              }
            >
              <Icon className="w-5 h-5" />
              <span className="text-[10px]">{shortLabel}</span>
            </NavLink>
          ))}
        </div>
      </nav>
    </div>
  )
}
