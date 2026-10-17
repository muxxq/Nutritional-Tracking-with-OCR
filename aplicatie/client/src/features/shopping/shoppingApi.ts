import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { api } from '@/api/client'
import type { Schemas } from '@/api/types'
import { unwrap } from '@/api/unwrap'

export type ShoppingListDto = Schemas['ShoppingListDto']
export type ShoppingListItemDto = Schemas['ShoppingListItemDto']
export type AddShoppingListItemRequest = Schemas['AddShoppingListItemRequest']

export const shoppingKey = ['shopping'] as const

export function useShoppingList() {
  return useQuery({
    queryKey: shoppingKey,
    queryFn: async () => unwrap(await api.GET('/api/shopping-list')),
  })
}

/** Item changes return the item, not the grouped list: refetch the list to keep counts and grouping right. */
function useShoppingMutation<TVariables>(mutationFn: (variables: TVariables) => Promise<unknown>) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: shoppingKey }),
  })
}

/** FR-12: replaces the generated lines with what the uneaten meals planned in the range need; manual lines stay. */
export function useGenerateShoppingList() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: async (range: { from: string; to: string }) => unwrap(await api.POST('/api/shopping-list/generate', { body: range })),
    onSuccess: (list) => queryClient.setQueryData(shoppingKey, list),
  })
}

export const useAddShoppingItem = () =>
  useShoppingMutation(async (body: AddShoppingListItemRequest) => unwrap(await api.POST('/api/shopping-list/items', { body })))

/**
 * Ticking is optimistic: in a shop with weak signal the box must react at once. The cached list is updated before
 * the request and restored if it fails; the server's list is fetched afterwards either way.
 */
export function useSetPurchased() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: async ({ id, isPurchased }: { id: string; isPurchased: boolean }) =>
      unwrap(await api.PATCH('/api/shopping-list/items/{id}', { params: { path: { id } }, body: { isPurchased } })),
    onMutate: async ({ id, isPurchased }) => {
      await queryClient.cancelQueries({ queryKey: shoppingKey })
      const previous = queryClient.getQueryData<ShoppingListDto>(shoppingKey)
      if (previous) {
        queryClient.setQueryData<ShoppingListDto>(shoppingKey, {
          ...previous,
          purchasedCount: previous.purchasedCount + (isPurchased ? 1 : -1),
          categories: previous.categories.map((c) => ({ ...c, items: c.items.map((i) => (i.id === id ? { ...i, isPurchased } : i)) })),
        })
      }
      return { previous }
    },
    onError: (_error, _variables, context) => queryClient.setQueryData(shoppingKey, context?.previous),
    onSettled: () => queryClient.invalidateQueries({ queryKey: shoppingKey }),
  })
}

export const useDeleteShoppingItem = () =>
  useShoppingMutation(async (id: string) => {
    const { error, response } = await api.DELETE('/api/shopping-list/items/{id}', { params: { path: { id } } })
    if (!response.ok && response.status !== 404) {
      throw { ...(error as object), status: response.status }
    }
  })

export const useClearPurchased = () =>
  useShoppingMutation(async () => {
    const { response } = await api.DELETE('/api/shopping-list/purchased')
    if (!response.ok) {
      throw { status: response.status }
    }
  })
