import type { Schemas } from '@/api/types'

export const CATEGORY_LABELS: Record<Schemas['FoodCategory'], string> = {
  fruitsVegetables: 'Fructe și legume',
  meatFish: 'Carne și pește',
  dairy: 'Lactate și ouă',
  grains: 'Cereale',
  pantry: 'Băcănie',
  beverages: 'Băuturi',
  other: 'Altele',
}
