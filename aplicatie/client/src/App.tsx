import { useState } from 'react'
import { Navbar } from './components/Navbar'
import { DiaryView } from './components/DiaryView'
import { MealPlannerView } from './components/MealPlannerView'
import { RecipesView } from './components/RecipesView'
import { ShoppingListView } from './components/ShoppingListView'
import { OcrScanModal } from './components/OcrScanModal'
import {
  INITIAL_FOOD_ITEMS,
  INITIAL_DIARY_ENTRIES,
  INITIAL_RECIPES,
  INITIAL_PLANNED_MEALS,
  INITIAL_SHOPPING_ITEMS
} from './data/mockData'
import type { DiaryEntry, FoodItem, MealType, PlannedMeal, Recipe, ShoppingItem } from './types/nutrition'
import { BookOpen, CalendarDays, UtensilsCrossed, ShoppingCart } from 'lucide-react'

export function App() {
  const [darkMode, setDarkMode] = useState(false)
  const [activeTab, setActiveTab] = useState<'diary' | 'planner' | 'recipes' | 'shopping'>('diary')
  const [isScanModalOpen, setIsScanModalOpen] = useState(false)

  // App Data State
  const [foodCatalog] = useState<FoodItem[]>(INITIAL_FOOD_ITEMS)
  const [diaryEntries, setDiaryEntries] = useState<DiaryEntry[]>(INITIAL_DIARY_ENTRIES)
  const [recipes, setRecipes] = useState<Recipe[]>(INITIAL_RECIPES)
  const [plannedMeals, setPlannedMeals] = useState<PlannedMeal[]>(INITIAL_PLANNED_MEALS)
  const [shoppingItems, setShoppingItems] = useState<ShoppingItem[]>(INITIAL_SHOPPING_ITEMS)

  // Theme synchronization
  const toggleTheme = () => {
    const nextMode = !darkMode
    setDarkMode(nextMode)
    if (nextMode) {
      document.documentElement.classList.add('dark')
    } else {
      document.documentElement.classList.remove('dark')
    }
  }

  // 1. DIARY ACTIONS
  const handleAddDiaryEntry = (mealType: MealType, foodItem: FoodItem, quantityGrams: number) => {
    const ratio = quantityGrams / 100.0
    const newEntry: DiaryEntry = {
      id: `entry-${Date.now()}`,
      mealType,
      foodItem,
      quantityGrams,
      calculatedCalories: Math.round(foodItem.calories * ratio),
      calculatedProteins: Math.round(foodItem.proteins * ratio * 10) / 10,
      calculatedCarbs: Math.round(foodItem.carbohydrates * ratio * 10) / 10,
      calculatedFats: Math.round(foodItem.fats * ratio * 10) / 10,
    }
    setDiaryEntries([newEntry, ...diaryEntries])
  }

  const handleDeleteDiaryEntry = (entryId: string) => {
    setDiaryEntries(diaryEntries.filter((e) => e.id !== entryId))
  }

  // 2. PLANNER ACTIONS
  const handleAddPlannedMeal = (newMeal: Omit<PlannedMeal, 'id'>) => {
    const created: PlannedMeal = {
      ...newMeal,
      id: `plan-${Date.now()}`
    }
    setPlannedMeals([...plannedMeals, created])
  }

  const handleTransferPlannedMealToDiary = (meal: PlannedMeal) => {
    // Construct a synthetic food item from the planned meal
    const food: FoodItem = {
      id: `food-transferred-${Date.now()}`,
      name: meal.name,
      brand: 'Planificat',
      calories: meal.calories,
      proteins: meal.proteins,
      carbohydrates: meal.carbs,
      fats: meal.fats,
      category: 'Altele'
    }

    handleAddDiaryEntry(meal.mealType, food, 100)
  }

  const handleGenerateShoppingListFromPlanner = () => {
    // Aggregate ingredients from all planned meals
    const newItems: ShoppingItem[] = [
      { id: `shop-gen-1`, name: 'Piept de curcan proaspăt', quantity: '600 g', category: 'Carne & Pește', isPurchased: false },
      { id: `shop-gen-2`, name: 'Cartofi dulci', quantity: '1 kg', category: 'Fructe & Legume', isPurchased: false },
      { id: `shop-gen-3`, name: 'Fasole verde păstăi', quantity: '400 g', category: 'Fructe & Legume', isPurchased: false },
      { id: `shop-gen-4`, name: 'Afine proaspete', quantity: '250 g', category: 'Fructe & Legume', isPurchased: false },
      { id: `shop-gen-5`, name: 'Mușchi slab de vită', quantity: '500 g', category: 'Carne & Pește', isPurchased: false },
      { id: `shop-gen-6`, name: 'Conserve de ton bucăți', quantity: '3 conserve', category: 'Băcănie', isPurchased: false },
    ]

    // Merge without duplicates
    const existingNames = new Set(shoppingItems.map((i) => i.name.toLowerCase()))
    const toAdd = newItems.filter((i) => !existingNames.has(i.name.toLowerCase()))
    setShoppingItems([...shoppingItems, ...toAdd])
    setActiveTab('shopping')
  }

  // 3. RECIPES ACTIONS
  const handleCreateRecipe = (newRecipe: Recipe) => {
    setRecipes([newRecipe, ...recipes])
  }

  // 4. SHOPPING LIST ACTIONS
  const handleToggleShoppingItem = (id: string) => {
    setShoppingItems(
      shoppingItems.map((item) =>
        item.id === id ? { ...item, isPurchased: !item.isPurchased } : item
      )
    )
  }

  const handleAddShoppingItem = (name: string, quantity: string, category: string) => {
    const newItem: ShoppingItem = {
      id: `shop-${Date.now()}`,
      name,
      quantity,
      category,
      isPurchased: false,
    }
    setShoppingItems([newItem, ...shoppingItems])
  }

  const handleClearPurchasedShoppingItems = () => {
    setShoppingItems(shoppingItems.filter((item) => !item.isPurchased))
  }

  return (
    <div className="min-h-screen flex flex-col transition-colors duration-200">
      
      {/* Top Sticky Header */}
      <Navbar
        darkMode={darkMode}
        onToggleTheme={toggleTheme}
        activeTab={activeTab}
        onSelectTab={setActiveTab}
        onOpenScanModal={() => setIsScanModalOpen(true)}
      />

      {/* Main Body Content Container */}
      <main className="flex-1 max-w-7xl w-full mx-auto px-4 py-6 pb-24 md:pb-10">
        {activeTab === 'diary' && (
          <DiaryView
            entries={diaryEntries}
            foodCatalog={foodCatalog}
            onAddEntry={handleAddDiaryEntry}
            onDeleteEntry={handleDeleteDiaryEntry}
            onOpenScanModal={() => setIsScanModalOpen(true)}
          />
        )}

        {activeTab === 'planner' && (
          <MealPlannerView
            plannedMeals={plannedMeals}
            foodCatalog={foodCatalog}
            onAddPlannedMeal={handleAddPlannedMeal}
            onTransferToDiary={handleTransferPlannedMealToDiary}
            onGenerateShoppingList={handleGenerateShoppingListFromPlanner}
          />
        )}

        {activeTab === 'recipes' && (
          <RecipesView
            recipes={recipes}
            foodCatalog={foodCatalog}
            onCreateRecipe={handleCreateRecipe}
            onAddFoodToDiary={(food, grams) => handleAddDiaryEntry('lunch', food, grams)}
          />
        )}

        {activeTab === 'shopping' && (
          <ShoppingListView
            items={shoppingItems}
            onToggleItem={handleToggleShoppingItem}
            onAddItem={handleAddShoppingItem}
            onClearPurchased={handleClearPurchasedShoppingItems}
            onSyncFromPlanner={handleGenerateShoppingListFromPlanner}
          />
        )}
      </main>

      {/* OCR Scan Modal */}
      <OcrScanModal
        isOpen={isScanModalOpen}
        onClose={() => setIsScanModalOpen(false)}
        onSaveToDiary={handleAddDiaryEntry}
      />

      {/* Mobile Bottom Navigation Bar */}
      <nav className="md:hidden fixed bottom-0 left-0 right-0 z-40 bg-white/95 dark:bg-slate-900/95 backdrop-blur-md border-t border-slate-200 dark:border-slate-800 shadow-lg">
        <div className="grid grid-cols-4 h-16">
          <button
            onClick={() => setActiveTab('diary')}
            className={`flex flex-col items-center justify-center gap-1 transition ${
              activeTab === 'diary'
                ? 'text-emerald-700 dark:text-emerald-400 font-extrabold'
                : 'text-slate-500 hover:text-slate-800 dark:text-slate-400'
            }`}
          >
            <BookOpen className="w-5 h-5" />
            <span className="text-[10px]">Jurnal</span>
          </button>

          <button
            onClick={() => setActiveTab('planner')}
            className={`flex flex-col items-center justify-center gap-1 transition ${
              activeTab === 'planner'
                ? 'text-emerald-700 dark:text-emerald-400 font-extrabold'
                : 'text-slate-500 hover:text-slate-800 dark:text-slate-400'
            }`}
          >
            <CalendarDays className="w-5 h-5" />
            <span className="text-[10px]">Planificator</span>
          </button>

          <button
            onClick={() => setActiveTab('recipes')}
            className={`flex flex-col items-center justify-center gap-1 transition ${
              activeTab === 'recipes'
                ? 'text-emerald-700 dark:text-emerald-400 font-extrabold'
                : 'text-slate-500 hover:text-slate-800 dark:text-slate-400'
            }`}
          >
            <UtensilsCrossed className="w-5 h-5" />
            <span className="text-[10px]">Rețete</span>
          </button>

          <button
            onClick={() => setActiveTab('shopping')}
            className={`flex flex-col items-center justify-center gap-1 transition ${
              activeTab === 'shopping'
                ? 'text-emerald-700 dark:text-emerald-400 font-extrabold'
                : 'text-slate-500 hover:text-slate-800 dark:text-slate-400'
            }`}
          >
            <ShoppingCart className="w-5 h-5" />
            <span className="text-[10px]">Cumpărături</span>
          </button>
        </div>
      </nav>

    </div>
  )
}

export default App
