import { useState } from 'react'
import type { DiaryEntry, FoodItem, MealType, PlannedMeal, ShoppingItem } from '@/types/nutrition'
import {
  INITIAL_DIARY_ENTRIES,
  INITIAL_FOOD_ITEMS,
  INITIAL_PLANNED_MEALS,
  INITIAL_SHOPPING_ITEMS,
} from './mockData'

/**
 * Transitional (Phase 3): the screens not yet connected to the API keep their local mock state here.
 * Each screen leaves this hook when it moves to TanStack Query; the hook is deleted with mockData.ts (NUT-46).
 */
export function useMockData(onShoppingListGenerated: () => void) {
  const [foodCatalog] = useState<FoodItem[]>(INITIAL_FOOD_ITEMS)
  const [diaryEntries, setDiaryEntries] = useState<DiaryEntry[]>(INITIAL_DIARY_ENTRIES)
  const [plannedMeals, setPlannedMeals] = useState<PlannedMeal[]>(INITIAL_PLANNED_MEALS)
  const [shoppingItems, setShoppingItems] = useState<ShoppingItem[]>(INITIAL_SHOPPING_ITEMS)

  const addDiaryEntry = (mealType: MealType, foodItem: FoodItem, quantityGrams: number) => {
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

  const deleteDiaryEntry = (entryId: string) => setDiaryEntries(diaryEntries.filter((e) => e.id !== entryId))

  const addPlannedMeal = (newMeal: Omit<PlannedMeal, 'id'>) =>
    setPlannedMeals([...plannedMeals, { ...newMeal, id: `plan-${Date.now()}` }])

  const transferPlannedMealToDiary = (meal: PlannedMeal) => {
    const food: FoodItem = {
      id: `food-transferred-${Date.now()}`,
      name: meal.name,
      brand: 'Planificat',
      calories: meal.calories,
      proteins: meal.proteins,
      carbohydrates: meal.carbs,
      fats: meal.fats,
      category: 'Altele',
    }
    addDiaryEntry(meal.mealType, food, 100)
  }

  const generateShoppingListFromPlanner = () => {
    const newItems: ShoppingItem[] = [
      { id: 'shop-gen-1', name: 'Piept de curcan proaspăt', quantity: '600 g', category: 'Carne & Pește', isPurchased: false },
      { id: 'shop-gen-2', name: 'Cartofi dulci', quantity: '1 kg', category: 'Fructe & Legume', isPurchased: false },
      { id: 'shop-gen-3', name: 'Fasole verde păstăi', quantity: '400 g', category: 'Fructe & Legume', isPurchased: false },
      { id: 'shop-gen-4', name: 'Afine proaspete', quantity: '250 g', category: 'Fructe & Legume', isPurchased: false },
      { id: 'shop-gen-5', name: 'Mușchi slab de vită', quantity: '500 g', category: 'Carne & Pește', isPurchased: false },
      { id: 'shop-gen-6', name: 'Conserve de ton bucăți', quantity: '3 conserve', category: 'Băcănie', isPurchased: false },
    ]
    const existingNames = new Set(shoppingItems.map((i) => i.name.toLowerCase()))
    setShoppingItems([...shoppingItems, ...newItems.filter((i) => !existingNames.has(i.name.toLowerCase()))])
    onShoppingListGenerated()
  }

  const toggleShoppingItem = (id: string) =>
    setShoppingItems(shoppingItems.map((item) => (item.id === id ? { ...item, isPurchased: !item.isPurchased } : item)))

  const addShoppingItem = (name: string, quantity: string, category: string) =>
    setShoppingItems([{ id: `shop-${Date.now()}`, name, quantity, category, isPurchased: false }, ...shoppingItems])

  const clearPurchasedShoppingItems = () => setShoppingItems(shoppingItems.filter((item) => !item.isPurchased))

  return {
    foodCatalog, diaryEntries, plannedMeals, shoppingItems,
    addDiaryEntry, deleteDiaryEntry, addPlannedMeal, transferPlannedMealToDiary, generateShoppingListFromPlanner,
    toggleShoppingItem, addShoppingItem, clearPurchasedShoppingItems,
  }
}

export type MockData = ReturnType<typeof useMockData>
