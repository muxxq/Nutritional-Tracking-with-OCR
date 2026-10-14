import { useState } from 'react'
import {
  CalendarDays,
  Plus,
  ArrowRight,
  ShoppingCart,
  CheckCircle2,
  TrendingDown,
  TrendingUp
} from 'lucide-react'
import type { PlannedMeal, MealType, FoodItem } from '../types/nutrition'

interface MealPlannerViewProps {
  plannedMeals: PlannedMeal[]
  foodCatalog?: FoodItem[]
  onAddPlannedMeal: (meal: Omit<PlannedMeal, 'id'>) => void
  onTransferToDiary: (meal: PlannedMeal) => void
  onGenerateShoppingList: () => void
}

const DAYS_OF_WEEK: ('Luni' | 'Marți' | 'Miercuri' | 'Joi' | 'Vineri' | 'Sâmbătă' | 'Duminică')[] = [
  'Luni',
  'Marți',
  'Miercuri',
  'Joi',
  'Vineri',
  'Sâmbătă',
  'Duminică',
]

export function MealPlannerView({
  plannedMeals,
  foodCatalog: _foodCatalog,
  onAddPlannedMeal,
  onTransferToDiary,
  onGenerateShoppingList,
}: MealPlannerViewProps) {
  const [selectedDay, setSelectedDay] = useState<typeof DAYS_OF_WEEK[number]>('Luni')
  const [isAddModalOpen, setIsAddModalOpen] = useState(false)
  const [newMealName, setNewMealName] = useState('')
  const [newMealType, setNewMealType] = useState<MealType>('lunch')
  const [newMealCalories, setNewMealCalories] = useState('500')
  const [newMealProteins, setNewMealProteins] = useState('35')
  const [newMealCarbs, setNewMealCarbs] = useState('45')
  const [newMealFats, setNewMealFats] = useState('12')
  const [newMealIngredients, setNewMealIngredients] = useState('')
  const [transferSuccessMessage, setTransferSuccessMessage] = useState<string | null>(null)

  const targetDailyCalories = 2200

  // Filter meals for selected day
  const dayMeals = plannedMeals.filter((m) => m.dayOfWeek === selectedDay)
  const dayCalories = dayMeals.reduce((acc, m) => acc + m.calories, 0)
  const dayProteins = dayMeals.reduce((acc, m) => acc + m.proteins, 0)
  const dayCarbs = dayMeals.reduce((acc, m) => acc + m.carbs, 0)
  const dayFats = dayMeals.reduce((acc, m) => acc + m.fats, 0)

  // Status vs target
  const diffPercent = ((dayCalories - targetDailyCalories) / targetDailyCalories) * 100
  let targetStatus = { label: 'Echilibrat (±5%)', color: 'bg-emerald-100 text-emerald-800 dark:bg-emerald-950 dark:text-emerald-300', icon: CheckCircle2 }
  if (diffPercent < -5) {
    targetStatus = { label: 'Deficit caloric', color: 'bg-blue-100 text-blue-800 dark:bg-blue-950 dark:text-blue-300', icon: TrendingDown }
  } else if (diffPercent > 5) {
    targetStatus = { label: 'Surplus caloric', color: 'bg-amber-100 text-amber-800 dark:bg-amber-950 dark:text-amber-300', icon: TrendingUp }
  }

  const handleTransfer = (meal: PlannedMeal) => {
    onTransferToDiary(meal)
    setTransferSuccessMessage(`Masa „${meal.name}” a fost transferată în Jurnalul de astăzi!`)
    setTimeout(() => setTransferSuccessMessage(null), 3500)
  }

  const handleAddSubmit = () => {
    if (!newMealName.trim()) return

    onAddPlannedMeal({
      dayOfWeek: selectedDay,
      mealType: newMealType,
      name: newMealName,
      calories: parseInt(newMealCalories) || 400,
      proteins: parseInt(newMealProteins) || 30,
      carbs: parseInt(newMealCarbs) || 40,
      fats: parseInt(newMealFats) || 10,
      ingredientsSummary: newMealIngredients || 'Ingrediente planificate'
    })

    setIsAddModalOpen(false)
    setNewMealName('')
    setNewMealIngredients('')
  }

  return (
    <div className="space-y-6">
      
      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3 pb-2 border-b border-slate-200 dark:border-slate-800">
        <div>
          <h2 className="text-xl sm:text-2xl font-extrabold text-slate-900 dark:text-white tracking-tight flex items-center gap-2">
            <CalendarDays className="w-6 h-6 text-emerald-600" />
            <span>Planificator Săptămânal de Mese</span>
          </h2>
          <p className="text-xs sm:text-sm text-slate-600 dark:text-slate-400 font-medium">
            Organizează mesele în avans și generează automat lista completă de cumpărături (FR-10, FR-12)
          </p>
        </div>

        <div className="flex items-center gap-2">
          <button
            onClick={() => setIsAddModalOpen(true)}
            className="flex items-center gap-1.5 px-3 py-1.5 rounded-lg bg-white dark:bg-slate-800 border border-slate-300 dark:border-slate-700 text-xs font-bold text-slate-700 dark:text-slate-200 shadow-xs hover:bg-slate-50 dark:hover:bg-slate-700 transition"
          >
            <Plus className="w-4 h-4 text-emerald-600" />
            <span>Planifică Masă</span>
          </button>
          <button
            onClick={onGenerateShoppingList}
            className="flex items-center gap-1.5 px-3.5 py-1.5 rounded-lg bg-emerald-600 hover:bg-emerald-700 text-white text-xs font-bold shadow-xs transition"
          >
            <ShoppingCart className="w-4 h-4" />
            <span>Generează Listă Cumpărături</span>
          </button>
        </div>
      </div>

      {/* Success notification banner */}
      {transferSuccessMessage && (
        <div className="p-3.5 rounded-xl bg-emerald-100 dark:bg-emerald-950/80 border border-emerald-300 dark:border-emerald-800 flex items-center gap-2 text-xs font-bold text-emerald-800 dark:text-emerald-200 animate-in fade-in">
          <CheckCircle2 className="w-4 h-4 shrink-0" />
          <span>{transferSuccessMessage}</span>
        </div>
      )}

      {/* Days of the Week Selector */}
      <div className="flex items-center gap-1.5 overflow-x-auto pb-2 scrollbar-none">
        {DAYS_OF_WEEK.map((day) => {
          const isSelected = selectedDay === day
          const dayCount = plannedMeals.filter((m) => m.dayOfWeek === day).length
          return (
            <button
              key={day}
              onClick={() => setSelectedDay(day)}
              className={`flex flex-col items-center min-w-[76px] sm:min-w-[90px] py-2 px-3 rounded-xl border text-xs font-bold transition ${
                isSelected
                  ? 'bg-emerald-600 text-white border-emerald-600 shadow-sm shadow-emerald-600/30'
                  : 'bg-white dark:bg-slate-900 border-slate-200 dark:border-slate-800 text-slate-700 dark:text-slate-300 hover:bg-slate-50 dark:hover:bg-slate-800'
              }`}
            >
              <span>{day}</span>
              <span className={`text-[10px] mt-0.5 font-medium ${isSelected ? 'text-emerald-100' : 'text-slate-500 dark:text-slate-400'}`}>
                {dayCount} {dayCount === 1 ? 'masă' : 'mese'}
              </span>
            </button>
          )
        })}
      </div>

      {/* Day Overview Summary Card */}
      <div className="rounded-2xl border border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900 p-5 shadow-xs flex flex-col md:flex-row md:items-center justify-between gap-4">
        <div>
          <div className="flex items-center gap-2 mb-1">
            <span className="font-extrabold text-base text-slate-900 dark:text-white">Total Planificat: {selectedDay}</span>
            <span className={`text-[11px] font-bold px-2 py-0.5 rounded-md flex items-center gap-1 ${targetStatus.color}`}>
              <targetStatus.icon className="w-3.5 h-3.5" />
              <span>{targetStatus.label}</span>
            </span>
          </div>
          <p className="text-xs text-slate-500 dark:text-slate-400 font-medium">
            Comparație cu necesarul caloric zilnic de {targetDailyCalories} kcal calculat prin Mifflin–St Jeor
          </p>
        </div>

        {/* Nutritional Stats */}
        <div className="flex items-center gap-4 text-xs">
          <div className="p-2.5 rounded-xl bg-slate-50 dark:bg-slate-800/80 border border-slate-200 dark:border-slate-700 text-center min-w-[80px]">
            <span className="text-[10px] font-bold text-slate-500 uppercase">Calorii</span>
            <p className="font-black text-sm text-emerald-600">{dayCalories} kcal</p>
          </div>
          <div className="p-2.5 rounded-xl bg-slate-50 dark:bg-slate-800/80 border border-slate-200 dark:border-slate-700 text-center min-w-[65px]">
            <span className="text-[10px] font-bold text-slate-500 uppercase">Proteine</span>
            <p className="font-bold text-xs text-blue-600">{dayProteins}g</p>
          </div>
          <div className="p-2.5 rounded-xl bg-slate-50 dark:bg-slate-800/80 border border-slate-200 dark:border-slate-700 text-center min-w-[65px]">
            <span className="text-[10px] font-bold text-slate-500 uppercase">Carbi</span>
            <p className="font-bold text-xs text-amber-600">{dayCarbs}g</p>
          </div>
          <div className="p-2.5 rounded-xl bg-slate-50 dark:bg-slate-800/80 border border-slate-200 dark:border-slate-700 text-center min-w-[65px]">
            <span className="text-[10px] font-bold text-slate-500 uppercase">Grăsimi</span>
            <p className="font-bold text-xs text-rose-600">{dayFats}g</p>
          </div>
        </div>
      </div>

      {/* Meals Grid for the Day */}
      <div className="space-y-3.5">
        <h3 className="text-xs font-bold uppercase tracking-wider text-slate-500 dark:text-slate-400">
          Mese Planificate ({dayMeals.length})
        </h3>

        {dayMeals.length > 0 ? (
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            {dayMeals.map((meal) => (
              <div
                key={meal.id}
                className="rounded-2xl border border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900 p-4 space-y-3 shadow-xs hover:border-slate-300 dark:hover:border-slate-700 transition flex flex-col justify-between"
              >
                <div>
                  <div className="flex items-start justify-between gap-2">
                    <div>
                      <span className="text-[10px] font-bold uppercase tracking-wider text-emerald-600 dark:text-emerald-400">
                        {meal.mealType === 'breakfast' ? 'Mic Dejun' : meal.mealType === 'lunch' ? 'Prânz' : meal.mealType === 'dinner' ? 'Cină' : 'Gustare'}
                      </span>
                      <h4 className="font-bold text-sm text-slate-900 dark:text-white leading-tight">
                        {meal.name}
                      </h4>
                    </div>
                    <span className="text-xs font-black font-mono text-slate-900 dark:text-slate-100 bg-slate-100 dark:bg-slate-800 px-2 py-1 rounded-md shrink-0">
                      {meal.calories} kcal
                    </span>
                  </div>

                  <p className="text-xs text-slate-600 dark:text-slate-400 mt-2 line-clamp-2">
                    🥗 <strong>Ingrediente:</strong> {meal.ingredientsSummary}
                  </p>

                  <div className="flex items-center gap-3 text-xs font-mono pt-3 mt-3 border-t border-slate-100 dark:border-slate-800">
                    <span className="text-blue-600 font-semibold">P: {meal.proteins}g</span>
                    <span className="text-amber-600 font-semibold">C: {meal.carbs}g</span>
                    <span className="text-rose-600 font-semibold">G: {meal.fats}g</span>
                  </div>
                </div>

                <div className="pt-2">
                  <button
                    onClick={() => handleTransfer(meal)}
                    className="w-full flex items-center justify-center gap-1.5 py-1.5 px-3 rounded-xl bg-slate-100 hover:bg-emerald-50 dark:bg-slate-800 dark:hover:bg-emerald-950/40 text-slate-700 dark:text-slate-300 hover:text-emerald-700 dark:hover:text-emerald-300 text-xs font-bold transition border border-slate-200 dark:border-slate-700"
                  >
                    <span>Copiază în Jurnalul de azi</span>
                    <ArrowRight className="w-3.5 h-3.5" />
                  </button>
                </div>
              </div>
            ))}
          </div>
        ) : (
          <div className="rounded-2xl border border-dashed border-slate-300 dark:border-slate-800 p-8 text-center bg-white dark:bg-slate-900">
            <CalendarDays className="w-10 h-10 text-slate-400 mx-auto mb-2 opacity-60" />
            <p className="font-bold text-sm text-slate-800 dark:text-slate-200">Nu ai mese planificate pentru {selectedDay}</p>
            <p className="text-xs text-slate-500 dark:text-slate-400 mt-1 max-w-sm mx-auto">
              Apasă pe butonul de mai jos pentru a adăuga o masă planificată sau generează lista de cumpărături.
            </p>
            <button
              onClick={() => setIsAddModalOpen(true)}
              className="mt-4 inline-flex items-center gap-1.5 px-4 py-2 rounded-xl bg-emerald-600 text-white text-xs font-bold shadow-xs hover:bg-emerald-700 transition"
            >
              <Plus className="w-4 h-4" />
              <span>Adaugă prima masă pentru {selectedDay}</span>
            </button>
          </div>
        )}
      </div>

      {/* MODAL: Adaugă masă planificată */}
      {isAddModalOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-slate-900/60 backdrop-blur-xs animate-in fade-in duration-150">
          <div className="w-full max-w-md rounded-2xl bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 shadow-2xl overflow-hidden">
            <div className="p-4 bg-slate-50 dark:bg-slate-800/60 border-b border-slate-200 dark:border-slate-800 flex items-center justify-between">
              <h3 className="font-bold text-sm text-slate-900 dark:text-white">
                Planifică masă pentru {selectedDay}
              </h3>
              <button onClick={() => setIsAddModalOpen(false)} className="text-slate-400 font-bold text-sm">✕</button>
            </div>

            <div className="p-5 space-y-3.5">
              <div>
                <label className="text-xs font-bold text-slate-700 dark:text-slate-300 mb-1 block">Nume Preparat / Masă:</label>
                <input
                  type="text"
                  value={newMealName}
                  onChange={(e) => setNewMealName(e.target.value)}
                  placeholder="ex: Pui la cuptor cu cartofi dulci"
                  className="w-full bg-slate-50 dark:bg-slate-800 border border-slate-300 dark:border-slate-700 rounded-xl px-3 py-2 text-xs text-slate-900 dark:text-white focus:outline-none focus:ring-1 focus:ring-emerald-500"
                />
              </div>

              <div>
                <label className="text-xs font-bold text-slate-700 dark:text-slate-300 mb-1 block">Momentul Mesei:</label>
                <select
                  value={newMealType}
                  onChange={(e) => setNewMealType(e.target.value as MealType)}
                  className="w-full bg-slate-50 dark:bg-slate-800 border border-slate-300 dark:border-slate-700 rounded-xl px-3 py-2 text-xs text-slate-900 dark:text-white focus:outline-none focus:ring-1 focus:ring-emerald-500"
                >
                  <option value="breakfast">Mic Dejun</option>
                  <option value="lunch">Prânz</option>
                  <option value="dinner">Cină</option>
                  <option value="snack">Gustare</option>
                </select>
              </div>

              <div>
                <label className="text-xs font-bold text-slate-700 dark:text-slate-300 mb-1 block">Ingrediente Principale:</label>
                <input
                  type="text"
                  value={newMealIngredients}
                  onChange={(e) => setNewMealIngredients(e.target.value)}
                  placeholder="ex: Piept pui 200g, Cartofi dulci 200g, Salată"
                  className="w-full bg-slate-50 dark:bg-slate-800 border border-slate-300 dark:border-slate-700 rounded-xl px-3 py-2 text-xs text-slate-900 dark:text-white focus:outline-none focus:ring-1 focus:ring-emerald-500"
                />
              </div>

              <div className="grid grid-cols-4 gap-2 pt-1">
                <div>
                  <label className="text-[10px] font-bold text-slate-500 uppercase block mb-1">Kcal:</label>
                  <input
                    type="number"
                    value={newMealCalories}
                    onChange={(e) => setNewMealCalories(e.target.value)}
                    className="w-full bg-slate-50 dark:bg-slate-800 border border-slate-300 dark:border-slate-700 rounded-lg p-1.5 text-xs text-center font-bold"
                  />
                </div>
                <div>
                  <label className="text-[10px] font-bold text-slate-500 uppercase block mb-1">Proteine:</label>
                  <input
                    type="number"
                    value={newMealProteins}
                    onChange={(e) => setNewMealProteins(e.target.value)}
                    className="w-full bg-slate-50 dark:bg-slate-800 border border-slate-300 dark:border-slate-700 rounded-lg p-1.5 text-xs text-center font-bold"
                  />
                </div>
                <div>
                  <label className="text-[10px] font-bold text-slate-500 uppercase block mb-1">Carbi:</label>
                  <input
                    type="number"
                    value={newMealCarbs}
                    onChange={(e) => setNewMealCarbs(e.target.value)}
                    className="w-full bg-slate-50 dark:bg-slate-800 border border-slate-300 dark:border-slate-700 rounded-lg p-1.5 text-xs text-center font-bold"
                  />
                </div>
                <div>
                  <label className="text-[10px] font-bold text-slate-500 uppercase block mb-1">Grăsimi:</label>
                  <input
                    type="number"
                    value={newMealFats}
                    onChange={(e) => setNewMealFats(e.target.value)}
                    className="w-full bg-slate-50 dark:bg-slate-800 border border-slate-300 dark:border-slate-700 rounded-lg p-1.5 text-xs text-center font-bold"
                  />
                </div>
              </div>
            </div>

            <div className="p-3.5 bg-slate-50 dark:bg-slate-800/40 border-t border-slate-200 dark:border-slate-800 flex justify-end gap-2">
              <button
                onClick={() => setIsAddModalOpen(false)}
                className="px-3 py-1.5 rounded-lg border border-slate-300 dark:border-slate-700 text-xs font-semibold"
              >
                Anulează
              </button>
              <button
                onClick={handleAddSubmit}
                disabled={!newMealName.trim()}
                className="px-4 py-1.5 rounded-lg bg-emerald-600 hover:bg-emerald-700 disabled:opacity-50 text-white text-xs font-bold"
              >
                Salvează în Plan
              </button>
            </div>
          </div>
        </div>
      )}

    </div>
  )
}
