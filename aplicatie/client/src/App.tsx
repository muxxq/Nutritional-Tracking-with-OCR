import { useState } from 'react'
import {
  Flame,
  Camera,
  Plus,
  Calendar,
  Utensils,
  BookOpen,
  ShoppingBag,
  User,
  Sun,
  Moon,
  CheckCircle2,
  AlertCircle
} from 'lucide-react'

export function App() {
  const [darkMode, setDarkMode] = useState(false)
  const [isScanModalOpen, setIsScanModalOpen] = useState(false)
  const [activeTab, setActiveTab] = useState<'diary' | 'recipes' | 'planner' | 'profile'>('diary')

  const toggleTheme = () => {
    setDarkMode(!darkMode)
    document.documentElement.classList.toggle('dark')
  }

  // Calorie calculation
  const targetCalories = 2200
  const consumedCalories = 1420
  const remainingCalories = targetCalories - consumedCalories
  const progressPercent = Math.min(100, Math.round((consumedCalories / targetCalories) * 100))

  // Macros
  const macros = [
    { name: 'Proteine', current: 112, target: 160, unit: 'g', color: 'bg-blue-500' },
    { name: 'Carbohidrați', current: 145, target: 220, unit: 'g', color: 'bg-amber-500' },
    { name: 'Grăsimi', current: 42, target: 70, unit: 'g', color: 'bg-rose-500' },
  ]

  // Meals
  const meals = [
    {
      title: 'Mic Dejun',
      calories: 450,
      items: [
        { name: 'Fulgi de ovăz cu lapte de migdale', quantity: '250g', calories: 310 },
        { name: 'Banană proaspătă', quantity: '120g', calories: 140 },
      ]
    },
    {
      title: 'Prânz',
      calories: 620,
      items: [
        { name: 'Piept de pui la grătar', quantity: '200g', calories: 330 },
        { name: 'Orez basmati fiert', quantity: '150g', calories: 195 },
        { name: 'Salată verde cu lămâie', quantity: '100g', calories: 95 },
      ]
    },
    {
      title: 'Cină',
      calories: 350,
      items: [
        { name: 'File de somon la cuptor', quantity: '150g', calories: 280 },
        { name: 'Broccoli la abur', quantity: '150g', calories: 70 },
      ]
    },
    {
      title: 'Gustări',
      calories: 0,
      items: []
    }
  ]

  return (
    <div className={`min-h-screen transition-colors duration-200 ${darkMode ? 'dark bg-slate-950 text-slate-100' : 'bg-slate-50 text-slate-900'}`}>
      {/* Top Header */}
      <header className="sticky top-0 z-40 w-full border-b backdrop-blur bg-white/80 dark:bg-slate-900/80 border-slate-200 dark:border-slate-800">
        <div className="max-w-6xl mx-auto px-4 h-16 flex items-center justify-between">
          <div className="flex items-center gap-3">
            <div className="w-10 h-10 rounded-xl bg-emerald-500/10 text-emerald-500 flex items-center justify-center font-bold">
              <Flame className="w-6 h-6 fill-emerald-500 text-emerald-500" />
            </div>
            <div>
              <h1 className="font-bold text-lg tracking-tight leading-none">NutriScan</h1>
              <p className="text-xs text-muted-foreground">Monitorizare Nutrițională cu OCR</p>
            </div>
          </div>

          <div className="flex items-center gap-2">
            <div className="hidden sm:flex items-center gap-2 px-3 py-1.5 rounded-lg border border-slate-200 dark:border-slate-800 text-sm font-medium">
              <Calendar className="w-4 h-4 text-emerald-500" />
              <span>Astăzi, 14 Octombrie</span>
            </div>

            <button
              onClick={toggleTheme}
              className="p-2 rounded-lg border border-slate-200 dark:border-slate-800 hover:bg-slate-100 dark:hover:bg-slate-800"
              title="Schimbă tema"
            >
              {darkMode ? <Sun className="w-5 h-5 text-amber-400" /> : <Moon className="w-5 h-5 text-slate-600" />}
            </button>

            <button
              onClick={() => setIsScanModalOpen(true)}
              className="flex items-center gap-2 px-3 py-2 rounded-lg bg-emerald-600 hover:bg-emerald-700 text-white text-sm font-semibold shadow-sm transition"
            >
              <Camera className="w-4 h-4" />
              <span className="hidden sm:inline">Scanează Etichetă</span>
            </button>
          </div>
        </div>
      </header>

      {/* Main Content Layout */}
      <main className="max-w-6xl mx-auto px-4 py-6 pb-24 md:pb-8">
        <div className="grid grid-cols-1 lg:grid-cols-12 gap-6">
          
          {/* Left Column: Calories & Macros Dashboard */}
          <div className="lg:col-span-5 space-y-6">
            <div className="rounded-2xl border border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900 p-6 shadow-sm">
              <h2 className="text-base font-semibold mb-4 text-slate-700 dark:text-slate-300">Bilanț Caloric Zilnic</h2>
              
              <div className="flex flex-col sm:flex-row items-center gap-6 justify-center">
                {/* SVG Calorie Ring */}
                <div className="relative w-40 h-40 flex items-center justify-center">
                  <svg className="w-full h-full transform -rotate-90" viewBox="0 0 100 100">
                    <circle
                      cx="50"
                      cy="50"
                      r="42"
                      className="stroke-slate-100 dark:stroke-slate-800"
                      strokeWidth="10"
                      fill="transparent"
                    />
                    <circle
                      cx="50"
                      cy="50"
                      r="42"
                      className="stroke-emerald-500 transition-all duration-1000 ease-out"
                      strokeWidth="10"
                      strokeDasharray={264}
                      strokeDashoffset={264 - (264 * progressPercent) / 100}
                      strokeLinecap="round"
                      fill="transparent"
                    />
                  </svg>
                  <div className="absolute flex flex-col items-center">
                    <span className="text-3xl font-extrabold tracking-tight">{remainingCalories}</span>
                    <span className="text-xs uppercase font-semibold text-slate-400">kcal rămase</span>
                  </div>
                </div>

                {/* Numbers */}
                <div className="space-y-3 w-full sm:w-auto">
                  <div className="flex items-center justify-between sm:justify-start gap-4">
                    <span className="text-sm text-slate-500">Obiectiv:</span>
                    <span className="text-sm font-semibold">{targetCalories} kcal</span>
                  </div>
                  <div className="flex items-center justify-between sm:justify-start gap-4">
                    <span className="text-sm text-slate-500">Consumat:</span>
                    <span className="text-sm font-semibold text-emerald-500">{consumedCalories} kcal</span>
                  </div>
                  <div className="flex items-center justify-between sm:justify-start gap-4">
                    <span className="text-sm text-slate-500">Progres:</span>
                    <span className="text-sm font-semibold">{progressPercent}%</span>
                  </div>
                </div>
              </div>

              {/* Macro bars */}
              <div className="mt-6 pt-6 border-t border-slate-100 dark:border-slate-800 space-y-4">
                <h3 className="text-sm font-semibold text-slate-600 dark:text-slate-400">Macronutrienți</h3>
                {macros.map((m) => {
                  const pct = Math.min(100, Math.round((m.current / m.target) * 100))
                  return (
                    <div key={m.name} className="space-y-1.5">
                      <div className="flex justify-between text-xs font-medium">
                        <span>{m.name}</span>
                        <span className="text-slate-400">{m.current}{m.unit} / {m.target}{m.unit}</span>
                      </div>
                      <div className="w-full h-2 rounded-full bg-slate-100 dark:bg-slate-800 overflow-hidden">
                        <div
                          className={`h-full rounded-full ${m.color} transition-all duration-500`}
                          style={{ width: `${pct}%` }}
                        />
                      </div>
                    </div>
                  )
                })}
              </div>
            </div>

            {/* Quick OCR Banner */}
            <div className="rounded-2xl border border-emerald-200 dark:border-emerald-900/50 bg-emerald-50/50 dark:bg-emerald-950/20 p-5 flex items-start gap-4">
              <div className="p-3 rounded-xl bg-emerald-500 text-white shrink-0">
                <Camera className="w-6 h-6" />
              </div>
              <div className="space-y-1">
                <h3 className="font-semibold text-sm">Extragere Automată cu OCR</h3>
                <p className="text-xs text-slate-600 dark:text-slate-400">
                  Fotografiază tabelul nutrițional de pe orice ambalaj alimentar. Sistemul extrage automat valorile per 100g.
                </p>
                <button
                  onClick={() => setIsScanModalOpen(true)}
                  className="mt-2 inline-flex items-center gap-1.5 text-xs font-semibold text-emerald-600 dark:text-emerald-400 hover:underline"
                >
                  Deschide vizorul camerei ➔
                </button>
              </div>
            </div>
          </div>

          {/* Right Column: Meals Diary */}
          <div className="lg:col-span-7 space-y-4">
            <div className="flex items-center justify-between">
              <h2 className="text-lg font-bold">Jurnal Alimentar</h2>
              <span className="text-xs font-medium text-muted-foreground">{meals.reduce((acc, m) => acc + m.items.length, 0)} alimente adăugate</span>
            </div>

            {meals.map((meal) => (
              <div
                key={meal.title}
                className="rounded-2xl border border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900 overflow-hidden shadow-sm"
              >
                <div className="p-4 bg-slate-50/50 dark:bg-slate-800/40 border-b border-slate-100 dark:border-slate-800 flex items-center justify-between">
                  <div className="flex items-center gap-2">
                    <Utensils className="w-4 h-4 text-emerald-500" />
                    <span className="font-semibold text-sm">{meal.title}</span>
                  </div>
                  <div className="flex items-center gap-3">
                    <span className="text-xs font-semibold text-slate-500">{meal.calories} kcal</span>
                    <button
                      className="p-1.5 rounded-lg hover:bg-slate-200 dark:hover:bg-slate-700 text-slate-600 dark:text-slate-300"
                      title="Adaugă aliment"
                    >
                      <Plus className="w-4 h-4" />
                    </button>
                  </div>
                </div>

                <div className="divide-y divide-slate-100 dark:divide-slate-800/60">
                  {meal.items.length > 0 ? (
                    meal.items.map((item, idx) => (
                      <div key={idx} className="p-3.5 flex items-center justify-between text-sm hover:bg-slate-50/50 dark:hover:bg-slate-800/20 transition">
                        <div>
                          <p className="font-medium">{item.name}</p>
                          <p className="text-xs text-slate-400">{item.quantity}</p>
                        </div>
                        <span className="font-semibold text-xs">{item.calories} kcal</span>
                      </div>
                    ))
                  ) : (
                    <div className="p-4 text-center text-xs text-slate-400">
                      Niciun aliment înregistrat la această masă.
                    </div>
                  )}
                </div>
              </div>
            ))}
          </div>

        </div>
      </main>

      {/* OCR Scan Modal (HitL Preview) */}
      {isScanModalOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/70 backdrop-blur-sm animate-in fade-in duration-150">
          <div className="w-full max-w-lg rounded-2xl bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 shadow-2xl overflow-hidden">
            <div className="p-4 border-b border-slate-100 dark:border-slate-800 flex items-center justify-between">
              <div className="flex items-center gap-2">
                <Camera className="w-5 h-5 text-emerald-500" />
                <h3 className="font-bold text-sm">Scanare Etichetă Nutrițională (OCR)</h3>
              </div>
              <button
                onClick={() => setIsScanModalOpen(false)}
                className="text-slate-400 hover:text-slate-600 dark:hover:text-slate-200 text-sm font-semibold"
              >
                ✕
              </button>
            </div>

            <div className="p-6 space-y-4">
              {/* Camera Viewfinder Mock */}
              <div className="relative aspect-video rounded-xl bg-slate-950 flex flex-col items-center justify-center text-slate-400 overflow-hidden border-2 border-dashed border-emerald-500/50">
                <div className="absolute inset-4 border border-emerald-400/40 rounded-lg pointer-events-none flex flex-col justify-between p-2">
                  <span className="text-[10px] text-emerald-400 uppercase font-mono tracking-wider">Încadrați tabelul nutrițional</span>
                  <span className="text-[10px] text-emerald-400/60 font-mono text-right">Preprocesare OpenCV activă</span>
                </div>
                <Camera className="w-10 h-10 text-emerald-500/60 mb-2 animate-pulse" />
                <p className="text-xs font-medium">Vizor cameră foto pregătit</p>
              </div>

              {/* Human in the loop confirmation mock */}
              <div className="rounded-xl border border-slate-200 dark:border-slate-800 p-4 space-y-3 bg-slate-50 dark:bg-slate-800/40">
                <div className="flex items-center justify-between">
                  <span className="text-xs font-semibold text-slate-500">Rezultat Extracție (Per 100g)</span>
                  <span className="inline-flex items-center gap-1 text-[11px] font-semibold text-emerald-600 dark:text-emerald-400">
                    <CheckCircle2 className="w-3.5 h-3.5" /> Scor Încredere: 94%
                  </span>
                </div>

                <div className="grid grid-cols-2 gap-2 text-xs">
                  <div className="p-2 rounded-lg bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800">
                    <span className="text-slate-400">Energie:</span>
                    <p className="font-bold text-sm text-emerald-600">389 kcal</p>
                  </div>
                  <div className="p-2 rounded-lg bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800">
                    <span className="text-slate-400">Proteine:</span>
                    <p className="font-bold text-sm">16.9 g</p>
                  </div>
                  <div className="p-2 rounded-lg bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800">
                    <span className="text-slate-400">Carbohidrați:</span>
                    <p className="font-bold text-sm">66.3 g</p>
                  </div>
                  <div className="p-2 rounded-lg bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800">
                    <span className="text-slate-400">Grăsimi:</span>
                    <p className="font-bold text-sm">6.9 g</p>
                  </div>
                </div>

                <div className="flex items-center gap-2 text-[11px] text-amber-600 dark:text-amber-400">
                  <AlertCircle className="w-4 h-4 shrink-0" />
                  <span>Validare HitL: Poți edita manual orice valoare înainte de salvare.</span>
                </div>
              </div>
            </div>

            <div className="p-4 border-t border-slate-100 dark:border-slate-800 bg-slate-50/50 dark:bg-slate-800/30 flex justify-end gap-2">
              <button
                onClick={() => setIsScanModalOpen(false)}
                className="px-4 py-2 rounded-lg border border-slate-200 dark:border-slate-800 text-xs font-semibold hover:bg-slate-100 dark:hover:bg-slate-800"
              >
                Anulează
              </button>
              <button
                onClick={() => setIsScanModalOpen(false)}
                className="px-4 py-2 rounded-lg bg-emerald-600 hover:bg-emerald-700 text-white text-xs font-semibold"
              >
                Salvează în Jurnal
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Mobile Bottom Navigation Bar */}
      <nav className="md:hidden fixed bottom-0 left-0 right-0 z-40 bg-white/90 dark:bg-slate-900/90 backdrop-blur border-t border-slate-200 dark:border-slate-800">
        <div className="grid grid-cols-4 h-16">
          <button
            onClick={() => setActiveTab('diary')}
            className={`flex flex-col items-center justify-center gap-1 ${activeTab === 'diary' ? 'text-emerald-500 font-bold' : 'text-slate-400'}`}
          >
            <Utensils className="w-5 h-5" />
            <span className="text-[10px]">Jurnal</span>
          </button>
          <button
            onClick={() => setActiveTab('recipes')}
            className={`flex flex-col items-center justify-center gap-1 ${activeTab === 'recipes' ? 'text-emerald-500 font-bold' : 'text-slate-400'}`}
          >
            <BookOpen className="w-5 h-5" />
            <span className="text-[10px]">Rețete</span>
          </button>
          <button
            onClick={() => setActiveTab('planner')}
            className={`flex flex-col items-center justify-center gap-1 ${activeTab === 'planner' ? 'text-emerald-500 font-bold' : 'text-slate-400'}`}
          >
            <ShoppingBag className="w-5 h-5" />
            <span className="text-[10px]">Cumpărături</span>
          </button>
          <button
            onClick={() => setActiveTab('profile')}
            className={`flex flex-col items-center justify-center gap-1 ${activeTab === 'profile' ? 'text-emerald-500 font-bold' : 'text-slate-400'}`}
          >
            <User className="w-5 h-5" />
            <span className="text-[10px]">Profil</span>
          </button>
        </div>
      </nav>
    </div>
  )
}

export default App
