import { useOutletContext } from 'react-router'
import { MealPlannerView } from '@/components/MealPlannerView'
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
