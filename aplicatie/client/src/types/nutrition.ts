export type MealType = 'breakfast' | 'lunch' | 'dinner' | 'snack'

export interface FoodItem {
  id: string
  name: string
  brand?: string
  calories: number // per 100g
  proteins: number // per 100g
  carbohydrates: number // per 100g
  fats: number // per 100g
  fiber?: number // per 100g
  category: 'Cereale' | 'Carne & Pește' | 'Lactate' | 'Fructe & Legume' | 'Băcănie' | 'Altele'
}

export interface DiaryEntry {
  id: string
  mealType: MealType
  foodItem: FoodItem
  quantityGrams: number
  calculatedCalories: number
  calculatedProteins: number
  calculatedCarbs: number
  calculatedFats: number
}

export interface RecipeIngredient {
  foodItem: FoodItem
  quantityGrams: number
}

export interface Recipe {
  id: string
  name: string
  description?: string
  servingsCount: number
  prepTimeMinutes?: number
  ingredients: RecipeIngredient[]
}

export interface PlannedMeal {
  id: string
  dayOfWeek: 'Luni' | 'Marți' | 'Miercuri' | 'Joi' | 'Vineri' | 'Sâmbătă' | 'Duminică'
  mealType: MealType
  name: string
  calories: number
  proteins: number
  carbs: number
  fats: number
  ingredientsSummary: string
  isTransferred?: boolean
}

export interface ShoppingItem {
  id: string
  name: string
  quantity: string
  category: string
  isPurchased: boolean
}
