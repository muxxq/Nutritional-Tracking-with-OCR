import { useOutletContext } from 'react-router'
import { ShoppingListView } from '@/components/ShoppingListView'
import type { MockData } from '@/data/useMockData'

interface LayoutContext {
  mock: MockData
}

// Transitional: the screens below still run on mock data and are replaced one by one by API-backed pages.

export function ShoppingRoute() {
  const { mock } = useOutletContext<LayoutContext>()
  return (
    <ShoppingListView
      items={mock.shoppingItems}
      onToggleItem={mock.toggleShoppingItem}
      onAddItem={mock.addShoppingItem}
      onClearPurchased={mock.clearPurchasedShoppingItems}
      onSyncFromPlanner={() => {}}
    />
  )
}
