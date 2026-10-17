import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { api } from '@/api/client'
import type { Schemas } from '@/api/types'
import { unwrap } from '@/api/unwrap'

export type DiaryDayDto = Schemas['DiaryDayDto']
export type DiaryEntryDto = Schemas['DiaryEntryDto']
export type AddDiaryEntryRequest = Schemas['AddDiaryEntryRequest']
export type UpdateDiaryEntryRequest = Schemas['UpdateDiaryEntryRequest']

export const diaryKeys = {
  all: ['diary'] as const,
  day: (date: string) => ['diary', 'day', date] as const,
  recent: ['diary', 'recent'] as const,
}

export function useDiaryDay(date: string) {
  return useQuery({
    queryKey: diaryKeys.day(date),
    queryFn: async () => unwrap(await api.GET('/api/diary/days/{date}', { params: { path: { date } } })),
    // Keep yesterday on screen while today loads, instead of flashing an empty page.
    placeholderData: keepPreviousData,
  })
}

export function useRecentFoods() {
  return useQuery({
    queryKey: diaryKeys.recent,
    queryFn: async () => unwrap(await api.GET('/api/diary/recent-foods', { params: { query: { limit: 8 } } })),
  })
}

/** After any diary change: every cached day, the recent foods and the statistics are stale. */
function useDiaryMutation<TVariables, TResult>(mutationFn: (variables: TVariables) => Promise<TResult>) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn,
    onSuccess: () =>
      Promise.all([
        queryClient.invalidateQueries({ queryKey: diaryKeys.all }),
        queryClient.invalidateQueries({ queryKey: ['stats'] }),
      ]),
  })
}

export const useAddEntry = () =>
  useDiaryMutation(async (body: AddDiaryEntryRequest) => unwrap(await api.POST('/api/diary/entries', { body })))

export const useUpdateEntry = () =>
  useDiaryMutation(async ({ id, body }: { id: string; body: UpdateDiaryEntryRequest }) =>
    unwrap(await api.PUT('/api/diary/entries/{id}', { params: { path: { id } }, body })))

export const useDeleteEntry = () =>
  useDiaryMutation(async (id: string) => {
    const { error, response } = await api.DELETE('/api/diary/entries/{id}', { params: { path: { id } } })
    // 404: already deleted (double tap, another device) — the goal is reached either way.
    if (!response.ok && response.status !== 404) {
      throw { ...(error as object), status: response.status }
    }
  })
