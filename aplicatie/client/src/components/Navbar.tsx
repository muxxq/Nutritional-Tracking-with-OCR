import { Flame, Camera, Sun, Moon, Calendar, BookOpen, CalendarDays, UtensilsCrossed, ShoppingCart } from 'lucide-react'

interface NavbarProps {
  darkMode: boolean
  onToggleTheme: () => void
  activeTab: 'diary' | 'planner' | 'recipes' | 'shopping'
  onSelectTab: (tab: 'diary' | 'planner' | 'recipes' | 'shopping') => void
  onOpenScanModal: () => void
}

export function Navbar({
  darkMode,
  onToggleTheme,
  activeTab,
  onSelectTab,
  onOpenScanModal,
}: NavbarProps) {
  return (
    <header className="sticky top-0 z-40 w-full border-b backdrop-blur-md bg-white/95 dark:bg-slate-900/95 border-slate-200 dark:border-slate-800 shadow-xs">
      <div className="max-w-7xl mx-auto px-4 h-16 flex items-center justify-between">
        
        {/* Brand */}
        <div className="flex items-center gap-3">
          <div className="w-10 h-10 rounded-xl bg-emerald-500 flex items-center justify-center text-white shadow-md shadow-emerald-500/20">
            <Flame className="w-6 h-6 fill-white text-white" />
          </div>
          <div>
            <div className="flex items-center gap-2">
              <span className="font-extrabold text-lg text-slate-900 dark:text-white tracking-tight leading-none">
                NutriScan
              </span>
              <span className="text-[10px] font-bold px-2 py-0.5 rounded-full bg-emerald-100 text-emerald-800 dark:bg-emerald-950/80 dark:text-emerald-400 border border-emerald-300 dark:border-emerald-800">
                Licență 2026
              </span>
            </div>
            <p className="text-xs text-slate-500 dark:text-slate-400 font-medium">Tracking Nutrițional & OCR</p>
          </div>
        </div>

        {/* Desktop Navigation Tabs */}
        <nav className="hidden md:flex items-center gap-1 bg-slate-100 dark:bg-slate-800/80 p-1.5 rounded-xl border border-slate-200 dark:border-slate-700">
          <button
            onClick={() => onSelectTab('diary')}
            className={`flex items-center gap-2 px-3.5 py-1.5 rounded-lg text-xs font-semibold transition-all ${
              activeTab === 'diary'
                ? 'bg-white dark:bg-slate-900 text-emerald-700 dark:text-emerald-400 shadow-xs'
                : 'text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-white'
            }`}
          >
            <BookOpen className="w-4 h-4" />
            <span>Jurnal</span>
          </button>

          <button
            onClick={() => onSelectTab('planner')}
            className={`flex items-center gap-2 px-3.5 py-1.5 rounded-lg text-xs font-semibold transition-all ${
              activeTab === 'planner'
                ? 'bg-white dark:bg-slate-900 text-emerald-700 dark:text-emerald-400 shadow-xs'
                : 'text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-white'
            }`}
          >
            <CalendarDays className="w-4 h-4" />
            <span>Planificator Mese</span>
          </button>

          <button
            onClick={() => onSelectTab('recipes')}
            className={`flex items-center gap-2 px-3.5 py-1.5 rounded-lg text-xs font-semibold transition-all ${
              activeTab === 'recipes'
                ? 'bg-white dark:bg-slate-900 text-emerald-700 dark:text-emerald-400 shadow-xs'
                : 'text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-white'
            }`}
          >
            <UtensilsCrossed className="w-4 h-4" />
            <span>Rețetar & Alimente</span>
          </button>

          <button
            onClick={() => onSelectTab('shopping')}
            className={`flex items-center gap-2 px-3.5 py-1.5 rounded-lg text-xs font-semibold transition-all ${
              activeTab === 'shopping'
                ? 'bg-white dark:bg-slate-900 text-emerald-700 dark:text-emerald-400 shadow-xs'
                : 'text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-white'
            }`}
          >
            <ShoppingCart className="w-4 h-4" />
            <span>Cumpărături</span>
          </button>
        </nav>

        {/* Right Actions */}
        <div className="flex items-center gap-2">
          <div className="hidden lg:flex items-center gap-2 px-3 py-1.5 rounded-lg bg-slate-100 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 text-xs font-medium text-slate-700 dark:text-slate-300">
            <Calendar className="w-3.5 h-3.5 text-emerald-600 dark:text-emerald-400" />
            <span>Astăzi, 14 Octombrie</span>
          </div>

          <button
            onClick={onToggleTheme}
            className="p-2 rounded-xl bg-slate-100 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 hover:bg-slate-200 dark:hover:bg-slate-700 text-slate-700 dark:text-slate-300 transition"
            title="Comută Mod Luminos / Întunecat"
          >
            {darkMode ? (
              <Sun className="w-4 h-4 text-amber-400" />
            ) : (
              <Moon className="w-4 h-4 text-slate-700" />
            )}
          </button>

          <button
            onClick={onOpenScanModal}
            className="flex items-center gap-2 px-3.5 py-2 rounded-xl bg-emerald-600 hover:bg-emerald-700 text-white text-xs font-bold shadow-md shadow-emerald-600/20 transition active:scale-95"
          >
            <Camera className="w-4 h-4" />
            <span className="hidden sm:inline">Scanare OCR</span>
          </button>
        </div>

      </div>
    </header>
  )
}
