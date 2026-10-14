import { useState } from 'react'
import {
  UtensilsCrossed,
  Search,
  Plus,
  Clock,
  ChevronDown,
  ChevronUp
} from 'lucide-react'
import type { Recipe, FoodItem } from '../types/nutrition'

interface RecipesViewProps {
  recipes: Recipe[]
  foodCatalog: FoodItem[]
  onCreateRecipe: (recipe: Recipe) => void
  onAddFoodToDiary: (food: FoodItem, grams: number) => void
}

export function RecipesView({
  recipes,
  foodCatalog,
  onCreateRecipe,
  onAddFoodToDiary,
}: RecipesViewProps) {
  const [activeSubTab, setActiveSubTab] = useState<'recipes' | 'catalog'>('recipes')
  const [searchQuery, setSearchQuery] = useState('')
  const [expandedRecipeId, setExpandedRecipeId] = useState<string | null>(recipes[0]?.id || null)

  // Modal create recipe state
  const [isCreateModalOpen, setIsCreateModalOpen] = useState(false)
  const [newRecipeName, setNewRecipeName] = useState('')
  const [newRecipeDesc, setNewRecipeDesc] = useState('')
  const [newServings, setNewServings] = useState(2)
  const [newPrepTime, setNewPrepTime] = useState(20)
  const [selectedIngredients, setSelectedIngredients] = useState<{ food: FoodItem; grams: number }[]>([
    { food: foodCatalog[0], grams: 200 },
    { food: foodCatalog[2], grams: 150 },
  ])

  const toggleExpand = (id: string) => {
    setExpandedRecipeId(expandedRecipeId === id ? null : id)
  }

  // Filtered lists
  const filteredRecipes = recipes.filter((r) =>
    r.name.toLowerCase().includes(searchQuery.toLowerCase()) ||
    (r.description && r.description.toLowerCase().includes(searchQuery.toLowerCase()))
  )

  const filteredCatalog = foodCatalog.filter((f) =>
    f.name.toLowerCase().includes(searchQuery.toLowerCase()) ||
    (f.brand && f.brand.toLowerCase().includes(searchQuery.toLowerCase())) ||
    f.category.toLowerCase().includes(searchQuery.toLowerCase())
  )

  // Calculations helper for a recipe
  const calculateRecipeTotals = (recipe: Recipe) => {
    let calories = 0
    let proteins = 0
    let carbs = 0
    let fats = 0

    recipe.ingredients.forEach((ing) => {
      const ratio = ing.quantityGrams / 100
      calories += ing.foodItem.calories * ratio
      proteins += ing.foodItem.proteins * ratio
      carbs += ing.foodItem.carbohydrates * ratio
      fats += ing.foodItem.fats * ratio
    })

    const servings = Math.max(1, recipe.servingsCount)
    return {
      totalCalories: Math.round(calories),
      perServingCalories: Math.round(calories / servings),
      perServingProteins: Math.round((proteins / servings) * 10) / 10,
      perServingCarbs: Math.round((carbs / servings) * 10) / 10,
      perServingFats: Math.round((fats / servings) * 10) / 10,
    }
  }

  const handleAddIngredientRow = () => {
    setSelectedIngredients([...selectedIngredients, { food: foodCatalog[0], grams: 100 }])
  }

  const handleRemoveIngredientRow = (idx: number) => {
    setSelectedIngredients(selectedIngredients.filter((_, i) => i !== idx))
  }

  const handleSaveRecipe = () => {
    if (!newRecipeName.trim()) return

    const newRec: Recipe = {
      id: `rec-${Date.now()}`,
      name: newRecipeName,
      description: newRecipeDesc || 'Rețetă personalizată creată în aplicație.',
      servingsCount: Math.max(1, newServings),
      prepTimeMinutes: Math.max(1, newPrepTime),
      ingredients: selectedIngredients.map((i) => ({
        foodItem: i.food,
        quantityGrams: i.grams,
      })),
    }

    onCreateRecipe(newRec)
    setIsCreateModalOpen(false)
    setNewRecipeName('')
    setNewRecipeDesc('')
  }

  return (
    <div className="space-y-6">
      
      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3 pb-2 border-b border-slate-200 dark:border-slate-800">
        <div>
          <h2 className="text-xl sm:text-2xl font-extrabold text-slate-900 dark:text-white tracking-tight flex items-center gap-2">
            <UtensilsCrossed className="w-6 h-6 text-emerald-600" />
            <span>Rețetar Inteligent & Catalog Alimentar</span>
          </h2>
          <p className="text-xs sm:text-sm text-slate-600 dark:text-slate-400 font-medium">
            Calcul agregat al valorilor nutritive per porție și catalog cu 12 alimente de referință (FR-09)
          </p>
        </div>

        <div className="flex items-center gap-2">
          {activeSubTab === 'recipes' && (
            <button
              onClick={() => setIsCreateModalOpen(true)}
              className="flex items-center gap-1.5 px-3.5 py-1.5 rounded-lg bg-emerald-600 hover:bg-emerald-700 text-white text-xs font-bold shadow-xs transition"
            >
              <Plus className="w-4 h-4" />
              <span>Creează Rețetă</span>
            </button>
          )}
        </div>
      </div>

      {/* Sub Tabs and Search */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3">
        
        {/* Toggle between Recipes and Catalog */}
        <div className="flex items-center gap-1 bg-slate-100 dark:bg-slate-800 p-1 rounded-xl border border-slate-200 dark:border-slate-700 w-fit">
          <button
            onClick={() => setActiveSubTab('recipes')}
            className={`px-3 py-1.5 rounded-lg text-xs font-bold transition ${
              activeSubTab === 'recipes'
                ? 'bg-white dark:bg-slate-900 text-emerald-700 dark:text-emerald-400 shadow-xs'
                : 'text-slate-600 dark:text-slate-400 hover:text-slate-900'
            }`}
          >
            Rețete Compuse ({recipes.length})
          </button>
          <button
            onClick={() => setActiveSubTab('catalog')}
            className={`px-3 py-1.5 rounded-lg text-xs font-bold transition ${
              activeSubTab === 'catalog'
                ? 'bg-white dark:bg-slate-900 text-emerald-700 dark:text-emerald-400 shadow-xs'
                : 'text-slate-600 dark:text-slate-400 hover:text-slate-900'
            }`}
          >
            Catalog Alimente & Valori per 100g ({foodCatalog.length})
          </button>
        </div>

        {/* Search */}
        <div className="relative w-full sm:w-72">
          <Search className="w-4 h-4 absolute left-3 top-2.5 text-slate-400" />
          <input
            type="text"
            value={searchQuery}
            onChange={(e) => setSearchQuery(e.target.value)}
            placeholder={activeSubTab === 'recipes' ? 'Caută rețetă...' : 'Caută aliment/brand/categorie...'}
            className="w-full bg-white dark:bg-slate-900 border border-slate-300 dark:border-slate-700 rounded-xl pl-9 pr-3 py-2 text-xs text-slate-900 dark:text-white placeholder-slate-400 focus:outline-none focus:ring-1 focus:ring-emerald-500 shadow-xs"
          />
        </div>
      </div>

      {/* VIEW 1: RECIPES LIST */}
      {activeSubTab === 'recipes' && (
        <div className="space-y-4">
          {filteredRecipes.map((recipe) => {
            const totals = calculateRecipeTotals(recipe)
            const isExpanded = expandedRecipeId === recipe.id

            return (
              <div
                key={recipe.id}
                className="rounded-2xl border border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900 overflow-hidden shadow-xs transition hover:border-slate-300 dark:hover:border-slate-700"
              >
                {/* Main Recipe Card Header */}
                <div
                  onClick={() => toggleExpand(recipe.id)}
                  className="p-4 sm:p-5 flex flex-col md:flex-row md:items-center justify-between gap-4 cursor-pointer hover:bg-slate-50/60 dark:hover:bg-slate-800/40 transition"
                >
                  <div className="space-y-1">
                    <div className="flex items-center gap-2">
                      <h3 className="font-extrabold text-base text-slate-900 dark:text-white">
                        {recipe.name}
                      </h3>
                      <span className="text-[10px] font-bold px-2 py-0.5 rounded-full bg-emerald-100 text-emerald-800 dark:bg-emerald-950 dark:text-emerald-300">
                        {recipe.servingsCount} {recipe.servingsCount === 1 ? 'porție' : 'porții'}
                      </span>
                    </div>
                    {recipe.description && (
                      <p className="text-xs text-slate-600 dark:text-slate-400">{recipe.description}</p>
                    )}
                    <div className="flex items-center gap-3 text-xs text-slate-500 font-medium pt-1">
                      <span className="flex items-center gap-1">
                        <Clock className="w-3.5 h-3.5" /> {recipe.prepTimeMinutes || 15} min
                      </span>
                      <span>•</span>
                      <span>{recipe.ingredients.length} ingrediente</span>
                    </div>
                  </div>

                  {/* Calculated stats preview */}
                  <div className="flex items-center gap-3 shrink-0">
                    <div className="text-right">
                      <span className="text-xs font-bold font-mono text-emerald-600 dark:text-emerald-400 bg-emerald-50 dark:bg-emerald-950/60 px-2.5 py-1 rounded-lg border border-emerald-200 dark:border-emerald-800 block text-center">
                        {totals.perServingCalories} kcal / porție
                      </span>
                      <div className="flex items-center gap-2 text-[11px] font-mono text-slate-500 dark:text-slate-400 mt-1">
                        <span>P: {totals.perServingProteins}g</span>
                        <span>•</span>
                        <span>C: {totals.perServingCarbs}g</span>
                        <span>•</span>
                        <span>G: {totals.perServingFats}g</span>
                      </div>
                    </div>

                    <button className="p-1 rounded-lg text-slate-400 hover:text-slate-600">
                      {isExpanded ? <ChevronUp className="w-5 h-5" /> : <ChevronDown className="w-5 h-5" />}
                    </button>
                  </div>
                </div>

                {/* Expanded Ingredients Details */}
                {isExpanded && (
                  <div className="px-5 pb-5 pt-3 border-t border-slate-100 dark:border-slate-800 bg-slate-50/50 dark:bg-slate-800/20 space-y-3">
                    <div className="flex items-center justify-between text-xs font-bold text-slate-700 dark:text-slate-300">
                      <span>Ingrediente Detaliate (Calcul Agregat: {totals.totalCalories} kcal total)</span>
                    </div>

                    <div className="grid grid-cols-1 sm:grid-cols-2 gap-2">
                      {recipe.ingredients.map((ing, idx) => {
                        const ingCalories = Math.round((ing.foodItem.calories * ing.quantityGrams) / 100)
                        return (
                          <div
                            key={idx}
                            className="p-2.5 rounded-xl bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 flex items-center justify-between text-xs"
                          >
                            <div>
                              <p className="font-bold text-slate-900 dark:text-slate-100">{ing.foodItem.name}</p>
                              <span className="text-slate-500 font-semibold">{ing.quantityGrams} grame</span>
                            </div>
                            <span className="font-bold font-mono text-slate-700 dark:text-slate-300">
                              {ingCalories} kcal
                            </span>
                          </div>
                        )
                      })}
                    </div>
                  </div>
                )}
              </div>
            )
          })}
        </div>
      )}

      {/* VIEW 2: FOOD CATALOG */}
      {activeSubTab === 'catalog' && (
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-3.5">
          {filteredCatalog.map((food) => (
            <div
              key={food.id}
              className="rounded-2xl border border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900 p-4 space-y-2.5 shadow-xs flex flex-col justify-between"
            >
              <div>
                <div className="flex items-start justify-between gap-2">
                  <div>
                    <span className="text-[10px] font-bold uppercase tracking-wider text-emerald-600 dark:text-emerald-400">
                      {food.category}
                    </span>
                    <h4 className="font-bold text-sm text-slate-900 dark:text-white leading-tight">
                      {food.name}
                    </h4>
                    <p className="text-[11px] text-slate-500">{food.brand || 'Generic'}</p>
                  </div>
                  <span className="font-black text-xs font-mono bg-slate-100 dark:bg-slate-800 px-2 py-1 rounded-md text-slate-900 dark:text-slate-100">
                    {food.calories} kcal
                  </span>
                </div>

                <div className="grid grid-cols-3 gap-1 pt-3 mt-2 border-t border-slate-100 dark:border-slate-800 text-center text-[11px] font-mono">
                  <div className="p-1 rounded-md bg-blue-50 dark:bg-blue-950/40 text-blue-700 dark:text-blue-300 font-semibold">
                    P: {food.proteins}g
                  </div>
                  <div className="p-1 rounded-md bg-amber-50 dark:bg-amber-950/40 text-amber-700 dark:text-amber-300 font-semibold">
                    C: {food.carbohydrates}g
                  </div>
                  <div className="p-1 rounded-md bg-rose-50 dark:bg-rose-950/40 text-rose-700 dark:text-rose-300 font-semibold">
                    G: {food.fats}g
                  </div>
                </div>
              </div>

              <div className="pt-2">
                <button
                  onClick={() => onAddFoodToDiary(food, 100)}
                  className="w-full py-1.5 px-3 rounded-xl bg-slate-100 hover:bg-emerald-50 dark:bg-slate-800 dark:hover:bg-emerald-950/40 text-slate-700 dark:text-slate-300 hover:text-emerald-700 dark:hover:text-emerald-300 text-xs font-bold transition flex items-center justify-center gap-1.5"
                >
                  <Plus className="w-3.5 h-3.5" />
                  <span>Adaugă 100g în Jurnal</span>
                </button>
              </div>
            </div>
          ))}
        </div>
      )}

      {/* MODAL: CREEAZĂ REȚETĂ NOUĂ */}
      {isCreateModalOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-slate-900/60 backdrop-blur-xs animate-in fade-in duration-150">
          <div className="w-full max-w-lg rounded-2xl bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 shadow-2xl overflow-hidden max-h-[90vh] flex flex-col">
            
            <div className="p-4 bg-slate-50 dark:bg-slate-800/60 border-b border-slate-200 dark:border-slate-800 flex items-center justify-between shrink-0">
              <h3 className="font-bold text-sm text-slate-900 dark:text-white">Creează Rețetă Nouă</h3>
              <button onClick={() => setIsCreateModalOpen(false)} className="text-slate-400 font-bold text-sm">✕</button>
            </div>

            <div className="p-5 space-y-4 overflow-y-auto">
              <div>
                <label className="text-xs font-bold text-slate-700 dark:text-slate-300 mb-1 block">Nume Rețetă:</label>
                <input
                  type="text"
                  value={newRecipeName}
                  onChange={(e) => setNewRecipeName(e.target.value)}
                  placeholder="ex: Bol Proteic cu Pui și Avocado"
                  className="w-full bg-slate-50 dark:bg-slate-800 border border-slate-300 dark:border-slate-700 rounded-xl px-3 py-2 text-xs text-slate-900 dark:text-white focus:outline-none focus:ring-1 focus:ring-emerald-500"
                />
              </div>

              <div className="grid grid-cols-2 gap-3">
                <div>
                  <label className="text-xs font-bold text-slate-700 dark:text-slate-300 mb-1 block">Număr Porții:</label>
                  <input
                    type="number"
                    min="1"
                    max="10"
                    value={newServings}
                    onChange={(e) => setNewServings(parseInt(e.target.value) || 1)}
                    className="w-full bg-slate-50 dark:bg-slate-800 border border-slate-300 dark:border-slate-700 rounded-xl px-3 py-2 text-xs font-bold"
                  />
                </div>
                <div>
                  <label className="text-xs font-bold text-slate-700 dark:text-slate-300 mb-1 block">Timp Preparare (min):</label>
                  <input
                    type="number"
                    min="5"
                    max="180"
                    value={newPrepTime}
                    onChange={(e) => setNewPrepTime(parseInt(e.target.value) || 15)}
                    className="w-full bg-slate-50 dark:bg-slate-800 border border-slate-300 dark:border-slate-700 rounded-xl px-3 py-2 text-xs font-bold"
                  />
                </div>
              </div>

              {/* Ingredients Selector */}
              <div className="space-y-2">
                <div className="flex items-center justify-between">
                  <label className="text-xs font-bold text-slate-700 dark:text-slate-300">Ingrediente Compozite:</label>
                  <button
                    onClick={handleAddIngredientRow}
                    className="text-xs text-emerald-600 font-bold hover:underline inline-flex items-center gap-1"
                  >
                    <Plus className="w-3.5 h-3.5" /> Adaugă rând
                  </button>
                </div>

                <div className="space-y-2 max-h-48 overflow-y-auto pr-1">
                  {selectedIngredients.map((ing, idx) => (
                    <div key={idx} className="flex items-center gap-2 bg-slate-50 dark:bg-slate-800 p-2 rounded-xl border border-slate-200 dark:border-slate-700">
                      <select
                        value={ing.food.id}
                        onChange={(e) => {
                          const found = foodCatalog.find((f) => f.id === e.target.value)
                          if (found) {
                            const copy = [...selectedIngredients]
                            copy[idx].food = found
                            setSelectedIngredients(copy)
                          }
                        }}
                        className="flex-1 bg-white dark:bg-slate-900 border border-slate-300 dark:border-slate-700 rounded-lg p-1.5 text-xs"
                      >
                        {foodCatalog.map((f) => (
                          <option key={f.id} value={f.id}>{f.name} ({f.calories} kcal)</option>
                        ))}
                      </select>

                      <div className="w-20 flex items-center gap-1">
                        <input
                          type="number"
                          value={ing.grams}
                          onChange={(e) => {
                            const copy = [...selectedIngredients]
                            copy[idx].grams = parseInt(e.target.value) || 0
                            setSelectedIngredients(copy)
                          }}
                          className="w-full bg-white dark:bg-slate-900 border border-slate-300 dark:border-slate-700 rounded-lg p-1.5 text-xs text-center font-bold"
                        />
                        <span className="text-xs text-slate-500 font-semibold">g</span>
                      </div>

                      <button
                        onClick={() => handleRemoveIngredientRow(idx)}
                        disabled={selectedIngredients.length <= 1}
                        className="text-slate-400 hover:text-rose-500 p-1"
                      >
                        ✕
                      </button>
                    </div>
                  ))}
                </div>
              </div>
            </div>

            <div className="p-4 bg-slate-50 dark:bg-slate-800/40 border-t border-slate-200 dark:border-slate-800 flex justify-end gap-2 shrink-0">
              <button
                onClick={() => setIsCreateModalOpen(false)}
                className="px-3 py-1.5 rounded-lg border border-slate-300 dark:border-slate-700 text-xs font-semibold"
              >
                Anulează
              </button>
              <button
                onClick={handleSaveRecipe}
                disabled={!newRecipeName.trim()}
                className="px-4 py-1.5 rounded-lg bg-emerald-600 hover:bg-emerald-700 disabled:opacity-50 text-white text-xs font-bold"
              >
                Salvează Rețeta
              </button>
            </div>

          </div>
        </div>
      )}

    </div>
  )
}
