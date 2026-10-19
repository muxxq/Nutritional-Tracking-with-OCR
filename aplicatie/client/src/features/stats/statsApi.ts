import { keepPreviousData, useQuery } from '@tanstack/react-query'
import { api } from '@/api/client'
import type { Schemas } from '@/api/types'
import { unwrap } from '@/api/unwrap'

export type StatisticsDto = Schemas['StatisticsDto']
export type StatisticsDayDto = Schemas['StatisticsDayDto']

export function useStatistics(from: string, to: string) {
  return useQuery({
    queryKey: ['stats', from, to],
    queryFn: async () => unwrap(await api.GET('/api/stats', { params: { query: { from, to } } })),
    placeholderData: keepPreviousData,
  })
}
