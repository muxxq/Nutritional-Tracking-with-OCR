import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { api } from '@/api/client'
import type { Schemas } from '@/api/types'
import { unwrap } from '@/api/unwrap'

export type PlanDto = Schemas['PlanDto']
export type PlanDayDto = Schemas['PlanDayDto']
export type PlannedMealDto = Schemas['PlannedMealDto']
export type CreatePlannedMealRequest = Schemas['CreatePlannedMealRequest']

export const planKeys = {
  all: ['plan'] as const,
  range: (from: string, to: string) => ['plan', from, to] as const,
}

export function usePlan(from: string, to: string) {
  return useQuery({
    queryKey: planKeys.range(from, to),
    queryFn: async () => unwrap(await api.GET('/api/plan', { params: { query: { from, to } } })),
    placeholderData: keepPreviousData,
  })
}

function usePlanMutation<TVariables, TResult>(mutationFn: (variables: TVariables) => Promise<TResult>, alsoStale: string[] = []) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn,
    onSuccess: () => Promise.all(['plan', ...alsoStale].map((key) => queryClient.invalidateQueries({ queryKey: [key] }))),
  })
}

export const useCreatePlannedMeal = () =>
  usePlanMutation(async (body: CreatePlannedMealRequest) => unwrap(await api.POST('/api/plan/items', { body })))

export const useDeletePlannedMeal = () =>
  usePlanMutation(async (id: string) => {
    const { error, response } = await api.DELETE('/api/plan/items/{id}', { params: { path: { id } } })
    if (!response.ok && response.status !== 404) {
      throw { ...(error as object), status: response.status }
    }
  })

/** "I ate this" (FR-11): the planned meal becomes a diary entry on its planned day and meal (ADR-007 D5). */
export const useEatPlannedMeal = () =>
  usePlanMutation(
    async (id: string) => unwrap(await api.POST('/api/plan/items/{id}/eat', { params: { path: { id } } })),
    ['diary', 'stats'],
  )
