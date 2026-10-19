import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { api } from '@/api/client'
import type { Schemas } from '@/api/types'
import { unwrap } from '@/api/unwrap'

export type RecipeDto = Schemas['RecipeDto']
export type RecipeSummaryDto = Schemas['RecipeSummaryDto']
export type SaveRecipeRequest = Schemas['SaveRecipeRequest']

export const recipeKeys = {
  all: ['recipes'] as const,
  search: (q: string) => ['recipes', 'search', q] as const,
  detail: (id: string) => ['recipes', 'detail', id] as const,
}

export function useRecipeSearch(q: string) {
  return useQuery({
    queryKey: recipeKeys.search(q),
    queryFn: async () => unwrap(await api.GET('/api/recipes', { params: { query: { q: q || undefined, pageSize: 50 } } })),
    placeholderData: keepPreviousData,
  })
}

export function useRecipe(id: string | null) {
  return useQuery({
    queryKey: recipeKeys.detail(id ?? ''),
    queryFn: async () => unwrap(await api.GET('/api/recipes/{id}', { params: { path: { id: id! } } })),
    enabled: id !== null,
  })
}

/** Recipe values are computed on read (ADR-006), so plans and the shopping list that use the recipe change too. */
function useRecipeChange<TVariables, TResult>(mutationFn: (variables: TVariables) => Promise<TResult>) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn,
    onSuccess: () => Promise.all(['recipes', 'plan'].map((key) => queryClient.invalidateQueries({ queryKey: [key] }))),
  })
}

export const useSaveRecipe = () =>
  useRecipeChange(async ({ id, body }: { id?: string; body: SaveRecipeRequest }) =>
    id
      ? unwrap(await api.PUT('/api/recipes/{id}', { params: { path: { id } }, body }))
      : unwrap(await api.POST('/api/recipes', { body })))

export const useArchiveRecipe = () =>
  useRecipeChange(async (id: string) => {
    const { error, response } = await api.DELETE('/api/recipes/{id}', { params: { path: { id } } })
    if (!response.ok && response.status !== 404) {
      throw { ...(error as object), status: response.status }
    }
  })
