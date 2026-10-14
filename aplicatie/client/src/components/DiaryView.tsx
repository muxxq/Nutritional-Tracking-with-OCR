import { useState } from 'react'
import {
  Utensils,
  Plus,
  Trash2,
  Camera,
  Search,
  Check,
  ChevronRight
} from 'lucide-react'
import type { DiaryEntry, FoodItem, MealType } from '../types/nutrition'

interface DiaryViewProps {
  entries: DiaryEntry[]
  foodCatalog: FoodItem[]
  onAddEntry: (mealType: MealType, foodItem: FoodItem, quantityGrams: number) => void
  onDeleteEntry: (entryId: string) => void
  onOpenScanModal: () => void
}

export function DiaryView({
  entries,
  foodCatalog,
  onAddEntry,
  onDeleteEntry,
  onOpenScanModal,
}: DiaryViewProps) {
  // Modal state for adding food
  const [isAddModalOpen, setIsAddModalOpen] = useState(false)
  const [selectedMealType, setSelectedMealType] = useState<MealType>('breakfast')
  const [searchQuery, setSearchQuery] = useState('')
  const [selectedFood, setSelectedFood] = useState<FoodItem | null>(null)
  const [gramsInput, setGramsInput] = useState('100')

  // Calculated totals
  const targetCalories = 2200
  const targetProteins = 160
  const targetCarbs = 220
  const targetFats = 70

  const totalCalories = entries.reduce((acc, e) => acc + e.calculatedCalories, 0)
  const totalProteins = Math.round(entries.reduce((acc, e) => acc + e.calculatedProteins, 0))
  const totalCarbs = Math.round(entries.reduce((acc, e) => acc + e.calculatedCarbs, 0))
  const totalFats = Math.round(entries.reduce((acc, e) => acc + e.calculatedFats, 0))

  const remainingCalories = Math.max(0, targetCalories - Math.round(totalCalories))
  const progressPercent = Math.min(100, Math.round((totalCalories / targetCalories) * 100))

  const mealTypes: { type: MealType; label: string; icon: string }[] = [
    { type: 'breakfast', label: 'Mic Dejun', icon: '🍳' },
    { type: 'lunch', label: 'Prânz', icon: '🥗' },
    { type: 'dinner', label: 'Cină', icon: '🍲' },
    { type: 'snack', label: 'Gustări', icon: '🍎' },
  ]

  const openAddModal = (meal: MealType) => {
    setSelectedMealType(meal)
    setSelectedFood(foodCatalog[0] || null)
    setGramsInput('100')
    setSearchQuery('')
    setIsAddModalOpen(true)
  }

  const handleConfirmAdd = () => {
    if (!selectedFood) return
    const grams = parseFloat(gramsInput) || 100
    onAddEntry(selectedMealType, selectedFood, grams)
    setIsAddModalOpen(false)
  }

  // Filter food catalog
  const filteredFoods = foodCatalog.filter((f) =>
    f.name.toLowerCase().includes(searchQuery.toLowerCase()) ||
    (f.brand && f.brand.toLowerCase().includes(searchQuery.toLowerCase()))
  )

  // Preview macros for gramsInput
  const currentGrams = parseFloat(gramsInput) || 0
  const previewCalories = selectedFood ? Math.round((selectedFood.calories * currentGrams) / 100) : 0
  const previewProteins = selectedFood ? Math.round(((selectedFood.proteins * currentGrams) / 100) * 10) / 10 : 0
  const previewCarbs = selectedFood ? Math.round(((selectedFood.carbohydrates * currentGrams) / 100) * 10) / 10 : 0
  const previewFats = selectedFood ? Math.round(((selectedFood.fats * currentGrams) / 100) * 10) / 10 : 0

  return (
    <div className="space-y-6">
      
      {/* Top Banner / Breadcrumb */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3 pb-2 border-b border-slate-200 dark:border-slate-800">
        <div>
          <h2 className="text-xl sm:text-2xl font-extrabold text-slate-900 dark:text-white tracking-tight">
            Jurnal Nutrițional Zilnic
          </h2>
          <p className="text-xs sm:text-sm text-slate-600 dark:text-slate-400 font-medium">
            Înregistrează mesele consumate sau scanează automat etichetele produselor
          </p>
        </div>
        <div className="flex items-center gap-2">
          <button
            onClick={() => openAddModal('lunch')}
            className="flex items-center gap-1.5 px-3 py-1.5 rounded-lg bg-white dark:bg-slate-800 border border-slate-300 dark:border-slate-700 text-xs font-bold text-slate-700 dark:text-slate-200 shadow-xs hover:bg-slate-50 dark:hover:bg-slate-700 transition"
          >
            <Plus className="w-4 h-4 text-emerald-600" />
            <span>Adaugă Manual</span>
          </button>
          <button
            onClick={onOpenScanModal}
            className="flex items-center gap-1.5 px-3 py-1.5 rounded-lg bg-emerald-600 hover:bg-emerald-700 text-white text-xs font-bold shadow-xs transition"
          >
            <Camera className="w-4 h-4" />
            <span>Scanează OCR</span>
          </button>
        </div>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-12 gap-6">

        {/* LEFT COLUMN: Calorie Ring & Macro Breakdown */}
        <div className="lg:col-span-5 space-y-5">
          
          {/* Main Calorie Card */}
          <div className="rounded-2xl border border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900 p-5 sm:p-6 shadow-sm">
            <div className="flex items-center justify-between mb-4">
              <span className="text-xs font-bold uppercase tracking-wider text-slate-500 dark:text-slate-400">
                Bilanț Caloric
              </span>
              <span className="text-xs font-semibold px-2 py-0.5 rounded-md bg-emerald-100 text-emerald-800 dark:bg-emerald-950/80 dark:text-emerald-400">
                Obiectiv: {targetCalories} kcal
              </span>
            </div>

            <div className="flex flex-col sm:flex-row items-center justify-center gap-6">
              {/* SVG Ring */}
              <div className="relative w-36 h-36 sm:w-40 sm:h-40 flex items-center justify-center shrink-0">
                <svg className="w-full h-full transform -rotate-90" viewBox="0 0 100 100">
                  <circle
                    cx="50"
                    cy="50"
                    r="42"
                    className="stroke-slate-100 dark:stroke-slate-800"
                    strokeWidth="11"
                    fill="transparent"
                  />
                  <circle
                    cx="50"
                    cy="50"
                    r="42"
                    className="stroke-emerald-500 transition-all duration-700 ease-out"
                    strokeWidth="11"
                    strokeDasharray={264}
                    strokeDashoffset={264 - (264 * progressPercent) / 100}
                    strokeLinecap="round"
                    fill="transparent"
                  />
                </svg>
                <div className="absolute flex flex-col items-center text-center">
                  <span className="text-2xl sm:text-3xl font-black text-slate-900 dark:text-white tracking-tight">
                    {remainingCalories}
                  </span>
                  <span className="text-[10px] sm:text-xs font-bold uppercase tracking-wider text-slate-500 dark:text-slate-400">
                    kcal rămase
                  </span>
                </div>
              </div>

              {/* Stats detail */}
              <div className="space-y-2.5 w-full sm:w-auto text-xs">
                <div className="flex items-center justify-between sm:justify-start gap-4">
                  <span className="text-slate-500 dark:text-slate-400 font-medium">Consumat:</span>
                  <span className="font-bold text-slate-900 dark:text-white text-sm">
                    {Math.round(totalCalories)} kcal
                  </span>
                </div>
                <div className="flex items-center justify-between sm:justify-start gap-4">
                  <span className="text-slate-500 dark:text-slate-400 font-medium">Țintă zilnică:</span>
                  <span className="font-semibold text-slate-700 dark:text-slate-300">{targetCalories} kcal</span>
                </div>
                <div className="flex items-center justify-between sm:justify-start gap-4">
                  <span className="text-slate-500 dark:text-slate-400 font-medium">Progres:</span>
                  <span className="font-bold text-emerald-600 dark:text-emerald-400">{progressPercent}%</span>
                </div>
              </div>
            </div>

            {/* Macronutrient Bars */}
            <div className="mt-6 pt-5 border-t border-slate-100 dark:border-slate-800 space-y-3.5">
              <span className="text-xs font-bold uppercase tracking-wider text-slate-500 dark:text-slate-400">
                Macronutrienți Realizați
              </span>

              {/* Proteins */}
              <div className="space-y-1">
                <div className="flex justify-between text-xs font-semibold">
                  <span className="text-slate-700 dark:text-slate-200">Proteine</span>
                  <span className="text-slate-500 dark:text-slate-400 font-mono">
                    <strong className="text-blue-600 dark:text-blue-400 font-bold">{totalProteins}g</strong> / {targetProteins}g
                  </span>
                </div>
                <div className="w-full h-2.5 rounded-full bg-slate-100 dark:bg-slate-800 overflow-hidden">
                  <div
                    className="h-full bg-blue-500 rounded-full transition-all duration-500"
                    style={{ width: `${Math.min(100, Math.round((totalProteins / targetProteins) * 100))}%` }}
                  />
                </div>
              </div>

              {/* Carbs */}
              <div className="space-y-1">
                <div className="flex justify-between text-xs font-semibold">
                  <span className="text-slate-700 dark:text-slate-200">Carbohidrați</span>
                  <span className="text-slate-500 dark:text-slate-400 font-mono">
                    <strong className="text-amber-600 dark:text-amber-400 font-bold">{totalCarbs}g</strong> / {targetCarbs}g
                  </span>
                </div>
                <div className="w-full h-2.5 rounded-full bg-slate-100 dark:bg-slate-800 overflow-hidden">
                  <div
                    className="h-full bg-amber-500 rounded-full transition-all duration-500"
                    style={{ width: `${Math.min(100, Math.round((totalCarbs / targetCarbs) * 100))}%` }}
                  />
                </div>
              </div>

              {/* Fats */}
              <div className="space-y-1">
                <div className="flex justify-between text-xs font-semibold">
                  <span className="text-slate-700 dark:text-slate-200">Grăsimi</span>
                  <span className="text-slate-500 dark:text-slate-400 font-mono">
                    <strong className="text-rose-600 dark:text-rose-400 font-bold">{totalFats}g</strong> / {targetFats}g
                  </span>
                </div>
                <div className="w-full h-2.5 rounded-full bg-slate-100 dark:bg-slate-800 overflow-hidden">
                  <div
                    className="h-full bg-rose-500 rounded-full transition-all duration-500"
                    style={{ width: `${Math.min(100, Math.round((totalFats / targetFats) * 100))}%` }}
                  />
                </div>
              </div>
            </div>

          </div>

          {/* Quick OCR Helper Card */}
          <div className="rounded-2xl border border-emerald-300 dark:border-emerald-800/80 bg-emerald-50/80 dark:bg-emerald-950/30 p-4 sm:p-5 flex items-start gap-3.5 shadow-xs">
            <div className="p-2.5 rounded-xl bg-emerald-600 text-white shrink-0 shadow-sm">
              <Camera className="w-5 h-5" />
            </div>
            <div className="space-y-1">
              <h4 className="font-bold text-xs sm:text-sm text-slate-900 dark:text-emerald-100">
                Scanare Rapidă Etichetă (OCR)
              </h4>
              <p className="text-xs text-slate-700 dark:text-slate-300 leading-relaxed">
                Fotografiază tabelul nutrițional de pe orice produs din România/UE. Algoritmul detectează automat caloriile și macronutrienții per 100g.
              </p>
              <button
                onClick={onOpenScanModal}
                className="mt-2 inline-flex items-center gap-1 text-xs font-bold text-emerald-800 dark:text-emerald-300 hover:underline"
              >
                <span>Deschide scanerul</span>
                <ChevronRight className="w-3.5 h-3.5" />
              </button>
            </div>
          </div>

        </div>

        {/* RIGHT COLUMN: The 4 Meals Breakdown */}
        <div className="lg:col-span-7 space-y-4">
          <div className="flex items-center justify-between">
            <span className="text-xs font-bold uppercase tracking-wider text-slate-500 dark:text-slate-400">
              Mesele Zilei ({entries.length} alimente înregistrate)
            </span>
          </div>

          {mealTypes.map((meal) => {
            const mealEntries = entries.filter((e) => e.mealType === meal.type)
            const mealCalories = Math.round(mealEntries.reduce((acc, e) => acc + e.calculatedCalories, 0))

            return (
              <div
                key={meal.type}
                className="rounded-2xl border border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900 overflow-hidden shadow-xs"
              >
                {/* Meal Header */}
                <div className="p-3.5 sm:p-4 bg-slate-50 dark:bg-slate-800/60 border-b border-slate-200 dark:border-slate-800 flex items-center justify-between">
                  <div className="flex items-center gap-2.5">
                    <span className="text-lg">{meal.icon}</span>
                    <h3 className="font-bold text-sm text-slate-900 dark:text-white">{meal.label}</h3>
                  </div>

                  <div className="flex items-center gap-3">
                    <span className="text-xs font-bold font-mono text-slate-700 dark:text-slate-300 bg-white dark:bg-slate-900 px-2 py-1 rounded-md border border-slate-200 dark:border-slate-700">
                      {mealCalories} kcal
                    </span>
                    <button
                      onClick={() => openAddModal(meal.type)}
                      className="flex items-center gap-1 px-2.5 py-1 rounded-lg bg-emerald-100 hover:bg-emerald-200 dark:bg-emerald-950 dark:hover:bg-emerald-900 text-emerald-800 dark:text-emerald-300 text-xs font-bold transition"
                      title="Adaugă aliment la această masă"
                    >
                      <Plus className="w-3.5 h-3.5" />
                      <span>Adaugă</span>
                    </button>
                  </div>
                </div>

                {/* Meal Items List */}
                <div className="divide-y divide-slate-100 dark:divide-slate-800/60">
                  {mealEntries.length > 0 ? (
                    mealEntries.map((item) => (
                      <div
                        key={item.id}
                        className="p-3.5 flex items-center justify-between text-xs hover:bg-slate-50/70 dark:hover:bg-slate-800/30 transition group"
                      >
                        <div className="space-y-0.5">
                          <p className="font-bold text-slate-900 dark:text-slate-100 text-sm">
                            {item.foodItem.name}
                          </p>
                          <div className="flex items-center gap-2 text-slate-500 dark:text-slate-400">
                            <span className="font-semibold text-emerald-700 dark:text-emerald-400">
                              {item.quantityGrams} g
                            </span>
                            <span>•</span>
                            <span>P: {item.calculatedProteins}g</span>
                            <span>•</span>
                            <span>C: {item.calculatedCarbs}g</span>
                            <span>•</span>
                            <span>G: {item.calculatedFats}g</span>
                          </div>
                        </div>

                        <div className="flex items-center gap-3">
                          <span className="font-extrabold text-xs text-slate-900 dark:text-slate-100 font-mono">
                            {item.calculatedCalories} kcal
                          </span>
                          <button
                            onClick={() => onDeleteEntry(item.id)}
                            className="p-1.5 rounded-lg text-slate-400 hover:text-rose-600 hover:bg-rose-50 dark:hover:bg-rose-950/40 transition"
                            title="Șterge alimentul din jurnal"
                          >
                            <Trash2 className="w-3.5 h-3.5" />
                          </button>
                        </div>
                      </div>
                    ))
                  ) : (
                    <div className="p-4 text-center text-xs text-slate-500 dark:text-slate-400 italic">
                      Niciun aliment înregistrat la {meal.label.toLowerCase()}. Apasă pe „Adaugă” pentru a introduce primul ingredient.
                    </div>
                  )}
                </div>
              </div>
            )
          })}
        </div>

      </div>

      {/* MODAL: Adaugă Aliment în Jurnal */}
      {isAddModalOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-slate-900/60 backdrop-blur-xs animate-in fade-in duration-150">
          <div className="w-full max-w-lg rounded-2xl bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 shadow-2xl overflow-hidden">
            
            {/* Modal Header */}
            <div className="p-4 bg-slate-50 dark:bg-slate-800/60 border-b border-slate-200 dark:border-slate-800 flex items-center justify-between">
              <div className="flex items-center gap-2">
                <Utensils className="w-4 h-4 text-emerald-600" />
                <h3 className="font-bold text-sm text-slate-900 dark:text-white">
                  Adaugă Aliment la {mealTypes.find((m) => m.type === selectedMealType)?.label}
                </h3>
              </div>
              <button
                onClick={() => setIsAddModalOpen(false)}
                className="text-slate-400 hover:text-slate-600 dark:hover:text-slate-200 text-sm font-bold"
              >
                ✕
              </button>
            </div>

            {/* Modal Body */}
            <div className="p-5 space-y-4">
              
              {/* Search input */}
              <div className="relative">
                <Search className="w-4 h-4 absolute left-3 top-3 text-slate-400" />
                <input
                  type="text"
                  value={searchQuery}
                  onChange={(e) => setSearchQuery(e.target.value)}
                  placeholder="Caută în catalog (ex: pui, ovăz, somon, banană)..."
                  className="w-full bg-slate-50 dark:bg-slate-800 border border-slate-300 dark:border-slate-700 rounded-xl pl-9 pr-3 py-2 text-xs text-slate-900 dark:text-white placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-emerald-500"
                />
              </div>

              {/* Food selection list */}
              <div className="max-h-48 overflow-y-auto border border-slate-200 dark:border-slate-700 rounded-xl divide-y divide-slate-100 dark:divide-slate-800">
                {filteredFoods.map((food) => {
                  const isSelected = selectedFood?.id === food.id
                  return (
                    <div
                      key={food.id}
                      onClick={() => setSelectedFood(food)}
                      className={`p-2.5 flex items-center justify-between text-xs cursor-pointer transition ${
                        isSelected
                          ? 'bg-emerald-50 dark:bg-emerald-950/60 text-emerald-900 dark:text-emerald-100 font-bold'
                          : 'hover:bg-slate-50 dark:hover:bg-slate-800/40 text-slate-700 dark:text-slate-200'
                      }`}
                    >
                      <div>
                        <p className="font-bold">{food.name}</p>
                        <p className="text-[10px] text-slate-500 dark:text-slate-400">
                          {food.calories} kcal / 100g • P: {food.proteins}g • C: {food.carbohydrates}g • G: {food.fats}g
                        </p>
                      </div>
                      {isSelected && <Check className="w-4 h-4 text-emerald-600 shrink-0" />}
                    </div>
                  )
                })}
              </div>

              {/* Grams input & live preview */}
              {selectedFood && (
                <div className="bg-slate-50 dark:bg-slate-800/60 border border-slate-200 dark:border-slate-700 rounded-xl p-3.5 space-y-3">
                  <div className="flex items-center justify-between">
                    <label className="text-xs font-bold text-slate-700 dark:text-slate-300">
                      Cantitate consumată (în grame):
                    </label>
                    <div className="flex items-center gap-1.5 w-28">
                      <input
                        type="number"
                        min="1"
                        max="2000"
                        value={gramsInput}
                        onChange={(e) => setGramsInput(e.target.value)}
                        className="w-full bg-white dark:bg-slate-900 border border-slate-300 dark:border-slate-600 rounded-lg px-2.5 py-1 text-xs text-right font-bold text-slate-900 dark:text-white focus:outline-none focus:ring-1 focus:ring-emerald-500"
                      />
                      <span className="text-xs text-slate-500 font-semibold">g</span>
                    </div>
                  </div>

                  {/* Calculated live macros */}
                  <div className="grid grid-cols-4 gap-2 pt-2 border-t border-slate-200 dark:border-slate-700 text-center">
                    <div className="p-2 rounded-lg bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800">
                      <span className="text-[10px] text-slate-500 uppercase font-semibold">Calorii</span>
                      <p className="font-extrabold text-sm text-emerald-600">{previewCalories} kcal</p>
                    </div>
                    <div className="p-2 rounded-lg bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800">
                      <span className="text-[10px] text-slate-500 uppercase font-semibold">Proteine</span>
                      <p className="font-bold text-xs text-blue-600">{previewProteins}g</p>
                    </div>
                    <div className="p-2 rounded-lg bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800">
                      <span className="text-[10px] text-slate-500 uppercase font-semibold">Carbohidrați</span>
                      <p className="font-bold text-xs text-amber-600">{previewCarbs}g</p>
                    </div>
                    <div className="p-2 rounded-lg bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800">
                      <span className="text-[10px] text-slate-500 uppercase font-semibold">Grăsimi</span>
                      <p className="font-bold text-xs text-rose-600">{previewFats}g</p>
                    </div>
                  </div>
                </div>
              )}

            </div>

            {/* Modal Footer */}
            <div className="p-3.5 bg-slate-50 dark:bg-slate-800/40 border-t border-slate-200 dark:border-slate-800 flex justify-end gap-2">
              <button
                onClick={() => setIsAddModalOpen(false)}
                className="px-3.5 py-1.5 rounded-lg border border-slate-300 dark:border-slate-700 text-xs font-semibold text-slate-700 dark:text-slate-300 hover:bg-slate-100 dark:hover:bg-slate-800 transition"
              >
                Anulează
              </button>
              <button
                onClick={handleConfirmAdd}
                disabled={!selectedFood}
                className="px-4 py-1.5 rounded-lg bg-emerald-600 hover:bg-emerald-700 disabled:opacity-50 text-white text-xs font-bold shadow-xs transition"
              >
                Adaugă în Jurnal
              </button>
            </div>

          </div>
        </div>
      )}

    </div>
  )
}
