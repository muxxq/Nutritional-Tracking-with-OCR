import type { Schemas } from '@/api/types'

/** FR-10: a planned day against the calorie target, ±5% counting as on target. */
export const STATUS: Record<Schemas['PlanDayStatus'], { label: string; className: string }> = {
  under: { label: 'Sub țintă', className: 'bg-blue-100 text-blue-800 dark:bg-blue-950 dark:text-blue-300' },
  within: { label: 'În țintă (±5%)', className: 'bg-emerald-100 text-emerald-800 dark:bg-emerald-950 dark:text-emerald-300' },
  over: { label: 'Peste țintă', className: 'bg-amber-100 text-amber-800 dark:bg-amber-950 dark:text-amber-300' },
}
