import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { api } from '@/api/client'
import type { Schemas } from '@/api/types'
import { unwrap } from '@/api/unwrap'

export type FoodDto = Schemas['FoodDto']
export type FoodSummaryDto = Schemas['FoodSummaryDto']
export type SaveFoodRequest = Schemas['SaveFoodRequest']

export const foodKeys = {
  all: ['foods'] as const,
  search: (query: string) => ['foods', 'search', query] as const,
  detail: (id: string) => ['foods', 'detail', id] as const,
}

/** Searches the catalog; the server ignores case and diacritics ("ovaz" finds "Ovăz"). */
export function useFoodSearch(query: string) {
  return useQuery({
    queryKey: foodKeys.search(query),
    queryFn: async () =>
      unwrap(await api.GET('/api/foods', { params: { query: { query: query || undefined, pageSize: 20 } } })),
    placeholderData: keepPreviousData,
  })
}

export function useFood(id: string | null) {
  return useQuery({
    queryKey: foodKeys.detail(id ?? ''),
    queryFn: async () => unwrap(await api.GET('/api/foods/{id}', { params: { path: { id: id! } } })),
    enabled: id !== null,
  })
}

export function useCreateFood() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: async (body: SaveFoodRequest) => unwrap(await api.POST('/api/foods', { body })),
    onSuccess: (food) => {
      queryClient.setQueryData(foodKeys.detail(food.id), food)
      return queryClient.invalidateQueries({ queryKey: foodKeys.all })
    },
  })
}
