import { useState } from 'react'
import type { ShoppingItem } from '@/types/nutrition'
import { INITIAL_SHOPPING_ITEMS } from './mockData'

/**
 * Transitional (Phase 3): the shopping list screen still keeps its local mock state here.
 * Deleted with mockData.ts once that screen moves to the API (NUT-46).
 */
export function useMockData() {
  const [shoppingItems, setShoppingItems] = useState<ShoppingItem[]>(INITIAL_SHOPPING_ITEMS)

  const toggleShoppingItem = (id: string) =>
    setShoppingItems(shoppingItems.map((item) => (item.id === id ? { ...item, isPurchased: !item.isPurchased } : item)))

  const addShoppingItem = (name: string, quantity: string, category: string) =>
    setShoppingItems([{ id: `shop-${Date.now()}`, name, quantity, category, isPurchased: false }, ...shoppingItems])

  const clearPurchasedShoppingItems = () => setShoppingItems(shoppingItems.filter((item) => !item.isPurchased))

  return { shoppingItems, toggleShoppingItem, addShoppingItem, clearPurchasedShoppingItems }
}

export type MockData = ReturnType<typeof useMockData>
