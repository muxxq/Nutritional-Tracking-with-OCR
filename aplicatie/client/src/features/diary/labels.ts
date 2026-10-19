import type { Schemas } from '@/api/types'

export type MealType = Schemas['MealType']

export const MEALS: { type: MealType; label: string; icon: string }[] = [
  { type: 'breakfast', label: 'Mic dejun', icon: '🍳' },
  { type: 'lunch', label: 'Prânz', icon: '🥗' },
  { type: 'dinner', label: 'Cină', icon: '🍲' },
  { type: 'snack', label: 'Gustări', icon: '🍎' },
]

export const MEAL_LABELS = Object.fromEntries(MEALS.map((m) => [m.type, m.label])) as Record<MealType, string>

/** The meal a new entry most likely belongs to, from the time of day. */
export function suggestedMeal(now = new Date()): MealType {
  const hour = now.getHours()
  return hour < 11 ? 'breakfast' : hour < 16 ? 'lunch' : hour < 21 ? 'dinner' : 'snack'
}
