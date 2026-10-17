import { useOutletContext } from 'react-router'
import { MealPlannerView } from '@/components/MealPlannerView'
import { RecipesView } from '@/components/RecipesView'
import { ShoppingListView } from '@/components/ShoppingListView'
import type { MockData } from '@/data/useMockData'

interface LayoutContext {
  mock: MockData
}

// Transitional: the screens below still run on mock data and are replaced one by one by API-backed pages.

export function PlannerRoute() {
  const { mock } = useOutletContext<LayoutContext>()
  return (
    <MealPlannerView
      plannedMeals={mock.plannedMeals}
      foodCatalog={mock.foodCatalog}
      onAddPlannedMeal={mock.addPlannedMeal}
      onTransferToDiary={mock.transferPlannedMealToDiary}
      onGenerateShoppingList={mock.generateShoppingListFromPlanner}
    />
  )
}

export function RecipesRoute() {
  const { mock } = useOutletContext<LayoutContext>()
  return (
    <RecipesView
      recipes={mock.recipes}
      foodCatalog={mock.foodCatalog}
      onCreateRecipe={mock.createRecipe}
      onAddFoodToDiary={(food, grams) => mock.addDiaryEntry('lunch', food, grams)}
    />
  )
}

export function ShoppingRoute() {
  const { mock } = useOutletContext<LayoutContext>()
  return (
    <ShoppingListView
      items={mock.shoppingItems}
      onToggleItem={mock.toggleShoppingItem}
      onAddItem={mock.addShoppingItem}
      onClearPurchased={mock.clearPurchasedShoppingItems}
      onSyncFromPlanner={mock.generateShoppingListFromPlanner}
    />
  )
}
